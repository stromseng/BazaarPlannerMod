using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BazaarGameClient.Domain.Models.Cards;
using BazaarGameShared.Domain.Core.Types;
using BazaarGameShared.Domain.Players;
using BazaarGameShared.Infra.Messages;
using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using TheBazaar;
using UnityEngine;

namespace BazaarPlannerMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private readonly Harmony _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    private static DateTime _lastSentTime = DateTime.MinValue;
    private static readonly TimeSpan SendInterval = TimeSpan.FromSeconds(2);
    private static string _runId;


    private void Awake()
    {
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        _harmony.PatchAll();
    }
    
    static List<RunInfo.CardInfo> GetCardInfo(List<Card> cards)
    {
        List<RunInfo.CardInfo> cardInfos = new List<RunInfo.CardInfo>();
        foreach (var card in cards)
        {
                cardInfos.Add(new RunInfo.CardInfo
                {
                    TemplateId = card.TemplateId,
                    Tier = card.Tier,
                    Left = card.LeftSocketId,
                    Instance = card.GetInstanceId(),
                    Attributes = card.Attributes,
                    Name = card.Template?.InternalName
                    
                });
        }
        return cardInfos;
    }

    [HarmonyPatch(typeof(BoardManager), "Update")]
    class Update
    {

        [HarmonyPrefix]
        static void Prefix()
        {
            if (!Input.GetKeyDown(KeyCode.P))
            {
                return;
            }

            if (DateTime.Now - _lastSentTime < SendInterval)
            {
                return;
            }

            _lastSentTime = DateTime.Now;

            RunInfo runInfo = new RunInfo
            {
                Wins = Data.Run.Victories,
                Character = Data.Run.Player.Hero.ToString(),
                Day = (int)Data.Run.Day,
                Cards = GetCardInfo(GetItemsAsCards(Data.Run.Player.Hand)),
                Stash = GetCardInfo(GetItemsAsCards(Data.Run.Player.Stash)),
                Skills = GetSkillInfo(Data.Run.Player.Skills),
                OppCards = GetCardInfo(GetItemsAsCards(Data.Run.Opponent?.Hand)),
                OppStash = GetCardInfo(GetItemsAsCards(Data.Run.Opponent?.Stash)),
                OppSkills = GetSkillInfo(Data.Run.Opponent?.Skills),
                Health = Data.Run.Player.GetAttributeValue(EPlayerAttributeType.HealthMax),
                Regen = Data.Run.Player.GetAttributeValue(EPlayerAttributeType.HealthRegen),
                Level = Data.Run.Player.GetAttributeValue(EPlayerAttributeType.Level),
                Name = Data.Profile?.Username,
                OppHealth = Data.Run.Opponent?.GetAttributeValue(EPlayerAttributeType.HealthMax),
                OppRegen = Data.Run.Opponent?.GetAttributeValue(EPlayerAttributeType.HealthRegen),
                RunId = _runId
            };
            
            Task.Run(() => SendRunInfo(runInfo));
        }

        private static List<Card> GetItemsAsCards(IPlayerInventory container)
        {
            return container.Container.GetSocketables()
                .Cast<Card>() // Cast all items to Card (throws InvalidCastException if any object is incompatible)
                .ToList<Card>();
        }

        static List<RunInfo.SkillInfo> GetSkillInfo(IEnumerable<SkillCard> skills)
        {
            List<RunInfo.SkillInfo> skillInfos = new List<RunInfo.SkillInfo>();
            foreach (var skill in skills)
            {
                if (skill.Template != null)
                    skillInfos.Add(new RunInfo.SkillInfo
                    {
                        TemplateId = skill.TemplateId,
                        Tier = skill.Tier,
                        Name = skill.Template.InternalName
                    });
            }

            return skillInfos;
        }

        static void SendRunInfo(RunInfo runInfo)
        {
            try
            {
                string json = JsonConvert.SerializeObject(runInfo);

                Console.WriteLine(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending RunInfo: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(AppState), "OnRunInitializedMessageReceived")]
    public static class OnRunInitializedMessageReceived
    {
        [HarmonyPrefix]
        static void Prefix(NetMessageRunInitialized obj)
        {
            _runId = obj.RunId;
        }
    }
}
