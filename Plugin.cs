#pragma warning disable CS0436 // Type conflicts with imported type
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
using System.Net.Http;
using System.Text;
using BepInEx.Configuration;
using System.IO;


namespace BazaarPlannerMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private readonly Harmony _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    private static DateTime _lastSentTime = DateTime.MinValue;
    private static readonly TimeSpan SendInterval = TimeSpan.FromSeconds(2);
    private static string FirebaseApiKey = "AIzaSyCrDTf9_S8PURED8DZBDbbEsJuMA1poduw";
    private static string _runId;
    private static ConfigEntry<string> UidConfig;
    private static ConfigEntry<string> TokenConfig;
    private static ConfigEntry<string> RefreshTokenConfig;
    private static ConfigEntry<string> TokenExpiryConfig;
    private static ConfigFile BPConfig;
   // private static AuthHandler _authHandler;
   // private static CefBrowserHost _browserHost;
   // private static GameObject _browserGameObject;

    private static async Task SaveToFirebase(string runId, string battleName, string compressedData)
    {
        Console.WriteLine("Saving to BazaarPlanner...");
        string uid = UidConfig.Value;
        Console.WriteLine($"UID from config: {(string.IsNullOrEmpty(uid) ? "empty" : uid)}");
        string token = "";
        Console.WriteLine("Attempting to get valid token...");
        try 
        {
            token = await GetValidToken();
            Console.WriteLine($"Token result: {(string.IsNullOrEmpty(token) ? "empty/null" : "received")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in GetValidToken: {ex.GetType().Name}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return;
        }
        
        if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Cannot save to BazaarPlanner: Missing UID or token");
            return;
        }
        var data = new
        {
            name = battleName,
            data = compressedData,
            t = ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds().ToString()
        };
        
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"https://bazaarplanner-default-rtdb.firebaseio.com/users/{uid}/runs/{runId}/encounters.json?auth={token}");
        httpRequest.Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8,"application/json");
        var httpClient = new HttpClient();
        var httpResponse = await httpClient.SendAsync(httpRequest);
        if (httpResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"Run {runId} saved to BazaarPlanner");
        }
        else
        {
            Console.WriteLine($"Failed to save run {runId} to BazaarPlanner: {httpResponse.ReasonPhrase}");
        }
    }
     private static RunInfo getRunInfo() {
        return new RunInfo
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
            OppName = Data.Run.Opponent?.Hero.ToString()=="Common" ? Data.Run.Opponent.CombatantId.ToString() : Data.Run.Opponent?.Hero.ToString(),
            RunId = _runId
        };
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

    static void OpenInBazaarPlanner(string compressedData)
    {
        try
        {
    
            string url = $"https://www.bazaarplanner.com/#{compressedData}";
            
            Application.OpenURL(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening BazaarPlanner: {ex.Message}");
        }
    }
    
    protected virtual void Awake()
    {
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        _harmony.PatchAll();
        
        // Load config
        BPConfig = new ConfigFile(Path.Combine(Paths.ConfigPath, "BazaarPlanner.cfg"), true);
        try
        {
            Console.WriteLine("Initializing configurations...");
            UidConfig = BPConfig.Bind("Authentication", "Uid", "", "Firebase User ID");
            TokenExpiryConfig = BPConfig.Bind("Authentication", "TokenExpiry", DateTime.MinValue.ToString(), "Token Expiration Time");
            TokenConfig = BPConfig.Bind("Authentication", "Token", "", "Firebase ID Token");
            RefreshTokenConfig = BPConfig.Bind("Authentication", "RefreshToken", "", "Firebase Refresh Token");
            
            Console.WriteLine("Configurations initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing configurations: {ex.Message}");
        }
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

            RunInfo runInfo = getRunInfo();
            string json = CreateBazaarPlannerJson(runInfo);
            string compressed = LZString.CompressToEncodedURIComponent(json);
            Task.Run(() => OpenInBazaarPlanner(compressed));
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

    private static async Task<string> GetValidToken()
    {
        try 
        {
            Console.WriteLine("Checking config values...");
            
            DateTime tokenExpiry;
            if (!DateTime.TryParse(TokenExpiryConfig.Value, out tokenExpiry))
            {
                tokenExpiry = DateTime.MinValue;
            }

            Console.WriteLine($"Current token expires: {tokenExpiry}");
            if (DateTime.Now < tokenExpiry && !string.IsNullOrEmpty(TokenConfig.Value))
            {
                Console.WriteLine("Using existing valid token");
                return TokenConfig.Value;
            }

            if (string.IsNullOrEmpty(RefreshTokenConfig.Value))
            {
                Console.WriteLine("No refresh token found in config");
                return null;
            }

            Console.WriteLine("All config values present, attempting to refresh token...");
            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "refresh_token", RefreshTokenConfig.Value }
                });
                Console.WriteLine("Content: " + content + " and " + FirebaseApiKey);

                var response = await client.PostAsync(
                    $"https://securetoken.googleapis.com/v1/token?key={FirebaseApiKey}",
                    content
                );

                Console.WriteLine($"Token refresh response status: {response.StatusCode}");
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error response content: {errorContent}");
                }

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                        await response.Content.ReadAsStringAsync()
                    );

                    TokenConfig.Value = result["id_token"];
                    RefreshTokenConfig.Value = result["refresh_token"];
                    TokenExpiryConfig.Value = DateTime.Now.AddHours(1).ToString();

                    return TokenConfig.Value;
                } else {
                    Console.WriteLine("Failed to refresh token: " + response.ReasonPhrase);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetValidToken: {ex.GetType().Name} - {ex.Message}");
            return null;
        }
        return null;
    }
    /*

    [HarmonyPatch(typeof(BoardManager), "PlayRevealAnimations")]
    public static class BoardManagerPlayRevealAnimationsPatch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
                Console.WriteLine("Combat cards revealed!");
                
                RunInfo runInfo = getRunInfo();
                string json = CreateBazaarPlannerJson(runInfo);
                string compressed = LZString.CompressToEncodedURIComponent(json);
                Task.Run(() => SaveToFirebase(runInfo.RunId, $"Day {Data.Run.Day} - {Data.Run.Opponent?.Hero.ToString()}", compressed));
        }
    }*/
    [HarmonyPatch(typeof(CombatState), "OnExit")]
    class CombatStateOnExit
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            RunInfo runInfo = getRunInfo();
            string json = CreateBazaarPlannerJson(runInfo);
            string compressed = LZString.CompressToEncodedURIComponent(json);
            //Task.Run(() => SaveToFirebase(runInfo.RunId, $"Day {Data.Run.Day} - {runInfo.OppName}", compressed));        
            Task.Run(() => OpenInBazaarPlanner(compressed));
        }
        
    }
}