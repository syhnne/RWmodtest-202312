using System;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using System.Drawing.Text;
using RWCustom;
using Noise;
using System.Collections.Generic;
using MoreSlugcats;
using BepInEx.Logging;
using Smoke;
using Random = UnityEngine.Random;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Menu.Remix.MixedUI;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlugBase.DataTypes;
using MonoMod.RuntimeDetour;
using System.Reflection;
using SlugBase.SaveData;
using static PebblesSlug.PlayerHooks;
using System.Runtime.InteropServices;
using static MonoMod.InlineRT.MonoModRule;
using System.Runtime.CompilerServices;
using IL.Menu;
using HUD;
using MonoMod.Utils;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Globalization;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace PebblesSlug;


// 喜欢我全生物-90好感吗
// 如果以后要合并四个模组的话，我要顺便重构一下这堆代码，这里面有的东西是我刚学c#的时候写的，拥有长达3个月的悠久历史，写的可谓是达芬奇少了个奇
// 看这进度飞快的代码和一字没动的剧情，估计是迟早要合并了。还是找个清明节或者五一放假之类的良辰吉日办这件事吧，那将会是长达3天的“明明我只是把它复制过来怎么突然就不好使了”环节





/// <summary>
/// 吾日三省吾身：orig(player)写了吗？null检查了吗？hook挂上了吗？
/// </summary>


[BepInPlugin(MOD_ID, "PebblesSlug", "0.1.0")]
class Plugin : BaseUnityPlugin
{
    internal const string MOD_ID = "PebblesSlug_by_syhnne";
    public static ConditionalWeakTable<Player, PlayerModule> playerModules = new ConditionalWeakTable<Player, PlayerModule>();
    public static ConditionalWeakTable<Oracle, OracleModule> oracleModules = new ConditionalWeakTable<Oracle, OracleModule>();



    // 不要给猫改名！！！不要给猫改名！！！不要给猫改名！！！
    // 没事了，我终于懂了。slugbase json里面的id，是下面那个slugcatstatsname.value里面的value，要求完全一致，才能检查的到。
    // 那个name则是显示在游戏界面里的 The xxx 那个名字
    // TODO: 所以如何去掉这个The呢



    public static new ManualLogSource Logger { get; internal set; }

    public PebblesSlugOption option;


    internal bool IsInit = false;
    

    // 以下自定义属性会覆盖slugbase的属性，我的建议是别改，现在json文件里已经没有饱食度数据了，但我不知道有了会导致什么后果
    internal static readonly int Cycles = 21;
    internal static readonly int MaxFood = 8;
    internal static readonly int MinFood = 5;
    internal int MinFoodNow = MinFood;



    internal static readonly string SlugcatName = "PebblesSlug";
    internal static readonly SlugcatStats.Name SlugcatStatsName = new SlugcatStats.Name(SlugcatName);
    internal static readonly bool ShowLogs = true;
    internal static Plugin instance;

    /// <summary>
    /// 以防我测试的时候不能使用一些功能，发布的时候就改成false
    /// </summary>
    public static bool DevMode = true;

    /// <summary>
    /// 被蜈蚣和线圈电的时候会不会恢复饱食度。我想不好，写个变量放在这方便改吧
    /// </summary>
    private static readonly bool shockFood = false;



    // 方便我发布之前一股脑扔给chatgpt让他帮我翻译
    public static readonly string[] strings =
    {
        "Press G and up&down arrow keys to adjust the gravity in room.",//0
        "Explosion capacity",//1
        "Explosion capacity ",//2
        "Add food by electric means",//3
        "Add food when electrocuted by centipedes and zapcoils or crafting with electric spears",//4
        "Crafting key",//5
        "The key to be pressed when crafting electric spears (if unspecified, hold [pickup] to craft)",//6
        "oracle console",//7
        "(WIP)The key to toggle SS_AI console. For some reason this doesn't work right now, but you can use Tab to toggle the console.",//8
        "Gravity control key",//9
        "The key to be pressed when controlling gravity",//10
        "Gravity control toggle",//11
        "(WIP)The key to toggle gravity control",//12
        "Options",//13
        "Gravity Control",//14

    };


    public void OnEnable()
    {

        try
        {
            option = new PebblesSlugOption();
            instance = this;

            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

            MachineConnector.SetRegisteredOI("PebblesSlug_by_syhnne", option);

            PlayerHooks.Apply();
            CustomPlayerGraphics.Apply();
            CustomLore.Apply();
            SSOracleHooks.Apply();
            ShelterSS_AI.Apply();
            SSRoomEffects.Apply();
            CustomOverseerHolograms.Apply();
            SLOracleHooks.Apply();

            On.SlugcatStats.ctor += SlugcatStats_ctor;
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.Player.NewRoom += Player_NewRoom;

            On.RegionGate.customKarmaGateRequirements += RegionGate_customKarmaGateRequirements;
            On.Creature.Violence += Creature_Violence;
            IL.ZapCoil.Update += IL_ZapCoil_Update;
            IL.Centipede.Shock += IL_Centipede_Shock;

            RedsIllnessModules.Apply();

            if (!Futile.atlasManager.DoesContainElementWithName("fp_tail"))
            {
                Futile.atlasManager.LoadAtlas("atlases/fp_tail");
            }
            if (!Futile.atlasManager.DoesContainElementWithName("fp_HeadA0"))
            {
                Futile.atlasManager.LoadAtlas("atlases/fp_head");
            }
            if (!Futile.atlasManager.DoesContainElementWithName("fp_PlayerArm0"))
            {
                Futile.atlasManager.LoadAtlas("atlases/fp_arm");
            }
            if (!Futile.atlasManager.DoesContainElementWithName("overseerHolograms/PebblesSlugHologram"))
            {
                Futile.atlasManager.LoadImage("overseerHolograms/PebblesSlugHologram");
            }


            


            Plugin.Log("INIT");
        }
        catch (Exception ex)
        {
            base.Logger.LogError(ex);
            throw;
        }

    }



    public void OnDisable()
    {
        try
        {
            PlayerHooks.Disable();
            CustomPlayerGraphics.Disable();
            CustomLore.Disable();
            SSOracleHooks.Disable();
            ShelterSS_AI.Disable();
            SSRoomEffects.Disable();
            CustomOverseerHolograms.Disable();
            SLOracleHooks.Disable();

            On.SlugcatStats.ctor -= SlugcatStats_ctor;
            On.Player.ctor -= Player_ctor;
            On.Player.Update -= Player_Update;
            On.Player.NewRoom -= Player_NewRoom;

            On.RegionGate.customKarmaGateRequirements -= RegionGate_customKarmaGateRequirements;
            On.Creature.Violence -= Creature_Violence;
            IL.ZapCoil.Update -= IL_ZapCoil_Update;
            IL.Centipede.Shock -= IL_Centipede_Shock;

            RedsIllnessModules.Disable();

            option = null;
            instance = null;
        }
        catch (Exception ex)
        {
            base.Logger.LogError(ex);
            throw;
        }
    }



    private void LoadResources(RainWorld rainWorld)
    {

    }


    /// <summary>
    /// 输出日志。搜索的时候带上后面的冒号
    /// </summary>
    /// <param name="text"></param>
    public static void Log(params object[] text)
    {
        if (ShowLogs)
        {
            string log = "";
            foreach (object s in text) 
            { 
                log += s.ToString(); 
                log += " ";
            }
            Debug.Log("[PebblesSlug] : " + log);
        }
            
    }

    /// <summary>
    /// 用来输出一些我暂时用不到，但测试时可能有用的日志，后面没有那个冒号，这样我不想搜索的时候就搜不到
    /// </summary>
    /// <param name="text"></param>
    public static void LogStat(params object[] text)
    {
        if (ShowLogs)
        {
            string log = "";
            foreach (object s in text)
            {
                log += s.ToString();
                log += " ";
            }
            Debug.Log("[PebblesSlug] " + log);
        }

    }







    #region 对话……
    internal static void LogConversation(Conversation.TextEvent textEvent)
    {
        Debug.Log("owner:" + textEvent.owner.ToString() + " initWait:" + textEvent.initialWait.ToString() + " text:" + textEvent.text + " textLinger:" + textEvent.textLinger.ToString());
    }

    internal static void LogConversation(Conversation owner, int initialWait, string eventName)
    {
        Debug.Log("owner:" + owner.ToString() + " initWait:" + initialWait.ToString() + " special event:" + eventName);
    }

    internal static void LogConversation(Conversation owner, int initialWait, string text, int textLinger)
    {
        Debug.Log("owner:"+ owner.ToString() + " initWait:"+initialWait.ToString() + " text:"+text+" textLinger:"+textLinger.ToString());
    }



    internal static void LogAllConversations(Conversation convo, int fileName)
    {
        LogAllConversations(convo, fileName, false, 0);
    }


    internal static void LogAllConversations(Conversation convo, int fileName, bool oneRandomLine, int randomSeed)
    {
        LogAllConversations(convo, fileName, convo.currentSaveFile, oneRandomLine, randomSeed);
    }


    internal static void LogAllConversations(Conversation convo, int fileName, SlugcatStats.Name saveFile, bool oneRandomLine, int randomSeed)
    {
        Plugin.Log("======= logging all convos from" + fileName.ToString() + "===========================================");
        InGameTranslator.LanguageID languageID = convo.interfaceOwner.rainWorld.inGameTranslator.currentLanguage;
        string text;
        for (; ; )
        {
            text = AssetManager.ResolveFilePath(convo.interfaceOwner.rainWorld.inGameTranslator.SpecificTextFolderDirectory(languageID) + Path.DirectorySeparatorChar.ToString() + fileName.ToString() + ".txt");
            if (saveFile != null)
            {
                string text2 = text;
                text = AssetManager.ResolveFilePath(string.Concat(new string[]
                {
                    convo.interfaceOwner.rainWorld.inGameTranslator.SpecificTextFolderDirectory(languageID),
                    Path.DirectorySeparatorChar.ToString(),
                    fileName.ToString(),
                    "-",
                    saveFile.value,
                    ".txt"
                }));
                if (!File.Exists(text))
                {
                    text = text2;
                }
            }
            if (File.Exists(text))
            {
                string text3 = File.ReadAllText(text, Encoding.UTF8);
                if (text3[0] != '0')
                {
                    text3 = Custom.xorEncrypt(text3, 54 + fileName + (int)convo.interfaceOwner.rainWorld.inGameTranslator.currentLanguage * 7);
                }
                string[] array = Regex.Split(text3, "\r\n");
                try
                {
                    if (Regex.Split(array[0], "-")[1] == fileName.ToString())
                    {
                        if (oneRandomLine)
                        {
                            List<Conversation.TextEvent> list = new List<Conversation.TextEvent>();
                            for (int i = 1; i < array.Length; i++)
                            {
                                string[] array2 = LocalizationTranslator.ConsolidateLineInstructions(array[i]);
                                if (array2.Length == 3)
                                {

                                    LogConversation(convo, int.Parse(array2[0], NumberStyles.Any, CultureInfo.InvariantCulture), array2[2], int.Parse(array2[1], NumberStyles.Any, CultureInfo.InvariantCulture));
                                }
                                else if (array2.Length == 1 && array2[0].Length > 0)
                                {
                                    LogConversation(convo, 0, array2[0], 0);
                                }
                            }
                            if (list.Count > 0)
                            {
                                Random.State state = Random.state;
                                Random.InitState(randomSeed);
                                Conversation.TextEvent item = list[Random.Range(0, list.Count)];
                                Random.state = state;
                                LogConversation(item);
                            }
                        }
                        else
                        {
                            for (int j = 1; j < array.Length; j++)
                            {
                                string[] array3 = LocalizationTranslator.ConsolidateLineInstructions(array[j]);
                                if (array3.Length == 3)
                                {
                                    int num;
                                    int num2;
                                    if (ModManager.MSC && !int.TryParse(array3[1], NumberStyles.Any, CultureInfo.InvariantCulture, out num) && int.TryParse(array3[2], NumberStyles.Any, CultureInfo.InvariantCulture, out num2))
                                    {
                                        LogConversation(convo, int.Parse(array3[0], NumberStyles.Any, CultureInfo.InvariantCulture), array3[1], int.Parse(array3[2], NumberStyles.Any, CultureInfo.InvariantCulture));
                                    }
                                    else
                                    {
                                        LogConversation(convo, int.Parse(array3[0], NumberStyles.Any, CultureInfo.InvariantCulture), array3[2], int.Parse(array3[1], NumberStyles.Any, CultureInfo.InvariantCulture));
                                    }
                                }
                                else if (array3.Length == 2)
                                {
                                    if (array3[0] == "SPECEVENT")
                                    {
                                        LogConversation(convo, 0, array3[1]);
                                    }
                                    else if (array3[0] == "PEBBLESWAIT")
                                    {
                                        // convo.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(convo, null, int.Parse(array3[1], NumberStyles.Any, CultureInfo.InvariantCulture)));
                                        Debug.Log("PEBBLESWAIT");
                                    }
                                }
                                else if (array3.Length == 1 && array3[0].Length > 0)
                                {
                                    LogConversation(convo, 0, array3[0], 0);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    Debug.Log("TEXT ERROR");
                }
            }
            Debug.Log("NOT FOUND " + text);
            if (languageID == InGameTranslator.LanguageID.English)
            {
                break;
            }
            Debug.Log("RETRY WITH ENGLISH");
            languageID = InGameTranslator.LanguageID.English;
        }
        Plugin.Log("============== log end here ===============================");
    
    }


    #endregion





















    private void SlugcatStats_ctor(On.SlugcatStats.orig_ctor orig, SlugcatStats self, SlugcatStats.Name slugcat, bool malnourished)
    {
        orig(self, slugcat, malnourished);
        if (slugcat == Plugin.SlugcatStatsName)
        {
            self.foodToHibernate = MinFoodNow;
        }
    }








    // 房间发生变化时保留重力变化
    private void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
    {
        orig(self, newRoom);
        bool getModule = playerModules.TryGetValue(self, out var module) && module.playerName == SlugcatStatsName;
        if (getModule && self.slugcatStats.name == SlugcatStatsName)
        {
            bool isMyStory = newRoom.game.IsStorySession && newRoom.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName;
            module.gravityController?.NewRoom(isMyStory);


            if (newRoom.abstractRoom.name == "SS_AI" && CustomLore.DPSaveData != null && CustomLore.DPSaveData.saveStateNumber == Plugin.SlugcatStatsName)
            {
                CustomLore.DPSaveData.CyclesFromLastEnterSSAI = 0;
                Plugin.LogStat("CustomLore.DPSaveData.CyclesFromLastEnterSSAI CLEEARED");
            }

            if (self.room == null) { return; }
            if (module.console != null)
            {
                if (self.room.abstractRoom.name != "SS_AI")
                {
                    module.console.isActive = false;
                }
                else if (isMyStory && newRoom.game.GetStorySession.saveState.deathPersistentSaveData.altEnding)
                {
                    module.console.Enter();
                }
            }


            // logs
            if (!DevMode) return;

            Plugin.LogStat("ROOM: ", self.room.abstractRoom.name, " SHELTER INDEX: ", self.room.abstractRoom.shelterIndex);
            if (self.room.abstractRoom.isAncientShelter) { Plugin.LogStat("IS ANCIENT SHELTER"); }

            Plugin.Log("CustomLore.DPSaveData.CyclesFromLastEnterSSAI:", CustomLore.DPSaveData.CyclesFromLastEnterSSAI, CustomLore.DPSaveData.saveStateNumber.value);

            Plugin.Log("self.slugcatStats.foodToHibernate:", self.slugcatStats.foodToHibernate);
        }
    }








    // 小心蛞蝓猫钻管道的时候self.room会变成null。。
    private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        bool getModule = playerModules.TryGetValue(self, out var module) && module.playerName == SlugcatStatsName;

        if (getModule) module.Update(self, eu);

        
        orig(self, eu);
        self.redsIllness?.Update();

        // 一站式判定，从此告别烦恼。。我恨这个room。。
        if (self.room == null || self.dead) return;
        bool isMyStory = self.room.game.IsStorySession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName;
        if (getModule && self.room.game != null && self.room.game.IsStorySession)
        {
            module.gravityController?.Update(eu, isMyStory);
        }

        if (isMyStory && self.room.abstractRoom.name == "SS_AI" && self.AI == null && !self.dead && !self.Sleeping && self.room.game.GetStorySession.saveState.deathPersistentSaveData.altEnding)
        {
            ShelterSS_AI.Player_Update(self);
        }

        

    }









    // 随着游戏进度修改游戏内饱食度，不出意外的话，打真结局后就不会再改了
    private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {


        orig(self, abstractCreature, world);
        

        if (world.game.IsStorySession && self.slugcatStats.name == SlugcatStatsName)
        {
            Plugin.Log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            playerModules.Add(self, new PlayerModule(self));
            


            // 判定加在这了，上面那个东西是为了保证他在别人档里也能控制重力
            if (world.game.GetStorySession.saveStateNumber != SlugcatStatsName) return;


            // 这东西不会把无人机显示出来罢。。我只是想进大都会看看
            // 悲报：会
            // (world.game.session as StoryGameSession).saveState.hasRobo = true;
            int cycle = (world.game.session as StoryGameSession).saveState.cycleNumber;
            bool altEnding = (world.game.session as StoryGameSession).saveState.deathPersistentSaveData.altEnding;
            bool ascended = (world.game.session as StoryGameSession).saveState.deathPersistentSaveData.ascended;

            LogStat("Player_ctor - cycle: ", cycle," altEnding: ", altEnding, "ascended:", ascended);



            // 懂了，在挂香菇病效果之前计算，如果这时候就有malnourished，说明是挨饿的，否则就不是
            MinFoodNow = MinFood;
            self.slugcatStats.maxFood = MaxFood;
            if (self.Malnourished)
            {
                MinFoodNow = MaxFood;
            }
            else if (!altEnding && !ascended)
            {
                MinFoodNow = Math.Min(RedsIllnessModules.CycleGetFood(cycle), MaxFood);
            }




            if (!altEnding && !ascended && cycle > 5)
            {
                self.redsIllness = new RedsIllness(self, cycle - 5);
            }
            else if (altEnding && !ascended && CustomLore.DPSaveData != null && CustomLore.DPSaveData.CyclesFromLastEnterSSAI > 5)
            {
                // 在想打完altending之后还要不要加饱食度变化 加的话会很麻烦 对于我和玩家来说都很麻烦（。
                self.redsIllness = new RedsIllness(self, CustomLore.DPSaveData.CyclesFromLastEnterSSAI - 5);
            }


            

            if (!altEnding) 
            {
                // 他妈的，放着不修的下场就是他不知道什么时候变成了foodToHibernate永远都是8，测试的时候差点没给我折磨死
                // 见鬼的slugcatstats为什么在我playerctor前后都要调用，还要调用好几次
                // 今天跟你爆了
                self.slugcatStats.foodToHibernate = MinFoodNow;
                Plugin.LogStat("Player_ctor - minfoodnow: ", MinFoodNow, "food to hibernate(after): ", self.slugcatStats.foodToHibernate, " maxfood: ", MaxFood);
            }
            
            
        }

        
    }





























    #region 其他技能
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////







    // 纯属复制粘贴游戏代码，只为绕过香菇病效果（
    public static void CustomAddFood(Player player, int add)
    {
        if (player == null) { return; }
        add = Math.Min(add, player.MaxFoodInStomach - player.playerState.foodInStomach);
        if (ModManager.CoopAvailable && player.abstractCreature.world.game.IsStorySession && player.abstractCreature.world.game.Players[0] != player.abstractCreature && !player.isNPC)
        {
            PlayerState playerState = player.abstractCreature.world.game.Players[0].state as PlayerState;
            add = Math.Min(add, Math.Max(player.MaxFoodInStomach - playerState.foodInStomach, 0));
            Plugin.LogStat(string.Format("Player add food {0}. Amount to add {1}", player.playerState.playerNumber, add), false);
            playerState.foodInStomach += add;
        }
        if (player.abstractCreature.world.game.IsStorySession && player.AI == null)
        {
            player.abstractCreature.world.game.GetStorySession.saveState.totFood += add;
        }
        player.playerState.foodInStomach += add;
        if (player.FoodInStomach >= player.MaxFoodInStomach)
        {
            player.playerState.quarterFoodPoints = 0;
        }
        if (player.slugcatStats.malnourished && player.playerState.foodInStomach >= ((player.redsIllness != null) ? player.redsIllness.FoodToBeOkay : player.slugcatStats.maxFood))
        {
            if (player.redsIllness != null)
            {
                Plugin.LogStat("FoodToBeOkay: ", player.redsIllness.FoodToBeOkay);
                player.redsIllness.GetBetter();
                return;
            }
            if (!player.isSlugpup)
            {
                player.SetMalnourished(false);
            }
            if (player.playerState is PlayerNPCState)
            {
                (player.playerState as PlayerNPCState).Malnourished = false;
            }
        }
    }





    // 能进大都会
    // 其实这个方法有点简陋了，只是我找不到那种无条件的门应该怎么写
    private void RegionGate_customKarmaGateRequirements(On.RegionGate.orig_customKarmaGateRequirements orig, RegionGate self)
    {
        orig(self);
        if (self.room.abstractRoom.name == "GATE_UW_LC" && self.room.game.IsStorySession && self.room.game.GetStorySession.saveStateNumber == SlugcatStatsName)
        {
            self.karmaRequirements[0] = RegionGate.GateRequirement.OneKarma;
        }
    }









    // 被电不仅不会死，还会吃饱（？
    // 错误的，线圈全断电了，其实没啥用（。
    private void IL_ZapCoil_Update(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        // 182，还是那个劫持判定
        if (c.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Stfld),
            (i) => i.MatchLdarg(0),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Ldloc_1),
            (i) => i.Match(OpCodes.Ldelem_Ref),
            (i) => i.Match(OpCodes.Ldloc_2),
            (i) => i.Match(OpCodes.Callvirt)
            ))
        {
            c.EmitDelegate<Func<PhysicalObject, PhysicalObject>>((physicalObj) =>
            {
                if (physicalObj is Player && (physicalObj as Player).slugcatStats.name.value == SlugcatName)
                {
                    // 抄的蜈蚣代码
                    (physicalObj as Player).Stun(200);
                    physicalObj.room.AddObject(new CreatureSpasmer(physicalObj as Player, false, (physicalObj as Player).stun));
                    (physicalObj as Player).LoseAllGrasps();
                    if (PebblesSlugOption.AddFoodOnShock.Value && shockFood)
                    {
                        int maxfood = (physicalObj as Player).MaxFoodInStomach;
                        int food = (physicalObj as Player).FoodInStomach;
                        Log("Zapcoil - food:" + food + " maxfood: " + maxfood);
                        CustomAddFood(physicalObj as Player, maxfood - food);
                        // (physicalObj as Player).AddFood(maxfood - food);
                    }
                    return null;
                }
                else { return physicalObj; }
            });
        }
    }







    // 同理，现在可以免疫蜈蚣的电击，甚至吃上一顿
    private void IL_Centipede_Shock(ILContext il)
    {
         ILCursor c = new ILCursor(il);
        // 226，还是那个劫持判定，修改蜈蚣的体重让他无论如何都会小于玩家体重
        if (c.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Br),
            (i) => i.Match(OpCodes.Ldarg_1),
            (i) => i.Match(OpCodes.Callvirt),
            (i) => i.Match(OpCodes.Ldarg_0),
            (i) => i.Match(OpCodes.Call)
            ))
        {
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<float, PhysicalObject, float>>((centipedeMass, physicalObj) =>
            {
                Log("Match successfully! - CentipedeShock, centipede mass: "+ centipedeMass);
                if (physicalObj is Player && (physicalObj as Player).slugcatStats.name.value == SlugcatName) 
                {
                    if (PebblesSlugOption.AddFoodOnShock.Value && shockFood)
                    {
                        CustomAddFood(physicalObj as Player, 1);
                    }
                    return 0;
                }
                else { return centipedeMass; }
            });
        }
    }







    // 不能免疫蜈蚣的电击，但我认为这不是我的问题，是蜈蚣的问题。
    // 错了，好像是我的问题，这个violence怎么说
    // 没事了，确实是蜈蚣的问题，大蜈蚣电击致死是硬编码的
    // 修复：为了防止雨循环结束后在下悬架被电的求生不能求死不得，这个效果在开始下雨之后会逐步失效
    private void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        if (self is Player && (self as Player).slugcatStats.name.value == SlugcatName)
        {
            if (type == Creature.DamageType.Electric)
            {
                
                damage = Mathf.Lerp(1f, 0.1f, self.room.world.rainCycle.RainApproaching) * damage;
                stunBonus = Mathf.Lerp(1f, 0.1f, self.room.world.rainCycle.RainApproaching) * stunBonus;
                
            }
        }
        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
    }



    #endregion








}











