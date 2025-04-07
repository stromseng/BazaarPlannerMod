using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BazaarGameClient.Domain.Models.Cards;
using BazaarGameShared.Domain.Core.Types;
using BazaarGameShared.Domain.Players;
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
                    Name = card.Template?.InternalName,
                    Enchant = card.GetEnchantment().ToString()                    
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
            if (!Input.GetKeyDown(KeyCode.B))
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
                OppName = Data.Run.Opponent?.Hero.ToString()
            };
            
            Task.Run(() => OpenInBazaarPlanner(runInfo));
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

        private static string CreateBazaarPlannerJson(RunInfo runInfo)
        {
            var result = new List<object>();

            // Add player and opponent data objects (unchanged)
            result.Add(new
            {
                name = "_b_b",
                health = runInfo.Health,
                regen = runInfo.Regen,
                playerName = runInfo.Name ?? "Unknown",
                skills = runInfo.Skills?.Select(s => new
                {
                    name = s.Name,
                    tier = s.Tier
                }).ToList(),
            });

            result.Add(new
            {
                name = "_b_t",
                health = runInfo.OppHealth,
                regen = runInfo.OppRegen,
                playerName = runInfo.OppName ?? "Unknown",
                skills = runInfo.OppSkills?.Select(s => new
                {
                    name = s.Name,
                    tier = s.Tier
                }).ToList()
            });
            result.Add(new 
            {
                name = "_b_backpack"
            });

            // Helper function to create card object with conditional attributes
            object CreateCardObject(RunInfo.CardInfo card, string board)
            {
                var cardDict = new Dictionary<string, object>
                {
                    ["name"] = card.Enchant.Length > 0 ? card.Enchant + " " + card.Name  : card.Name,
                    ["startIndex"] = card.Left,
                    ["board"] = board,
                    ["tier"] = card.Tier
                };

                if (card.Attributes?.ContainsKey(ECardAttributeType.SellPrice) == true)
                    cardDict["valueFinal"] = card.Attributes[ECardAttributeType.SellPrice];
                
                if (card.Attributes?.ContainsKey(ECardAttributeType.HealAmount) == true)
                    cardDict["healFinal"] = card.Attributes[ECardAttributeType.HealAmount];
                
                if (card.Attributes?.ContainsKey(ECardAttributeType.Cooldown) == true)
                    cardDict["cooldown"] = card.Attributes[ECardAttributeType.CooldownMax]/1000;
                
                if (card.Attributes?.ContainsKey(ECardAttributeType.CritChance) == true)
                    cardDict["critFinal"] = card.Attributes[ECardAttributeType.CritChance];
                
                if (card.Attributes?.ContainsKey(ECardAttributeType.BurnApplyAmount) == true)
                    cardDict["burnFinal"] = card.Attributes[ECardAttributeType.BurnApplyAmount];
                if (card.Attributes?.ContainsKey(ECardAttributeType.ShieldApplyAmount) == true)
                    cardDict["shieldFinal"] = card.Attributes[ECardAttributeType.ShieldApplyAmount];
                if (card.Attributes?.ContainsKey(ECardAttributeType.PoisonApplyAmount) == true)
                    cardDict["poisonFinal"] = card.Attributes[ECardAttributeType.PoisonApplyAmount];
                if (card.Attributes?.ContainsKey(ECardAttributeType.DamageAmount) == true)
                    cardDict["damageFinal"] = card.Attributes[ECardAttributeType.DamageAmount];
                if (card.Attributes?.ContainsKey(ECardAttributeType.Lifesteal) == true)
                    cardDict["lifestealFinal"] = card.Attributes[ECardAttributeType.Lifesteal];
                if (card.Attributes?.ContainsKey(ECardAttributeType.RegenApplyAmount) == true)
                    cardDict["regenFinal"] = card.Attributes[ECardAttributeType.RegenApplyAmount];
                if (card.Attributes?.ContainsKey(ECardAttributeType.AmmoMax) == true)
                    cardDict["maxAmmoFinal"] = card.Attributes[ECardAttributeType.AmmoMax];
                return cardDict;
            }

            // Add player cards
            if (runInfo.Cards != null)
            {
                foreach (var card in runInfo.Cards)
                {
                    result.Add(CreateCardObject(card, "b"));
                }
            }

            // Add opponent cards
            if (runInfo.OppCards != null)
            {
                foreach (var card in runInfo.OppCards)
                {
                    result.Add(CreateCardObject(card, "t"));
                }
            }
            if(runInfo.Stash != null)
            {
                foreach (var card in runInfo.Stash)
                {
                    result.Add(CreateCardObject(card, "backpack"));
                }
            }

            return JsonConvert.SerializeObject(result);
        }

        static void OpenInBazaarPlanner(RunInfo runInfo)
        {
            try
            {
                string json = CreateBazaarPlannerJson(runInfo);
                string compressed = LZString.CompressToEncodedURIComponent(json);
                string url = $"https://www.bazaarplanner.com/#{compressed}";
                
                Application.OpenURL(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening BazaarPlanner: {ex.Message}");
            }
        }
    }
}
