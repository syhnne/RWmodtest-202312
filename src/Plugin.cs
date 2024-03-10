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
// using ImprovedInput;
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


    internal bool IsInit;
    internal static readonly List<int> ColoredBodyParts = new List<int>() { 2, 3, 5, 6, 7, 8, 9, };

    // 以下自定义属性会覆盖slugbase的属性，我的建议是别改，现在json文件里已经没有饱食度数据了，但我不知道有了会导致什么后果
    internal static readonly int Cycles = 21;
    internal static readonly int MaxFood = 8;
    internal static readonly int MinFood = 5;
    internal int MinFoodNow = MinFood;

    internal static readonly Color32 bodyColor_hard = new Color32(254, 104, 202, 255);
    internal static readonly Color eyesColor_hard = new Color(1f, 1f, 1f);

    internal static readonly string SlugcatName = "PebblesSlug";
    internal static readonly SlugcatStats.Name SlugcatStatsName = new SlugcatStats.Name(SlugcatName);
    internal static readonly Oracle.OracleID oracleID = new Oracle.OracleID("PL");
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


    /*
     * 0: "BodyA"
     * 1: "HipsA"
     * 2: tail
     * 3: "HeadA0"
     * 4: "LegsA0"
     * 5: "PlayerArm0", sLeaser.sprites[5].scaleY = -1f;
     * 6: "PlayerArm0"
     * 7: "OnTopOfTerrainHand"
     * 8: "OnTopOfTerrainHand", sLeaser.sprites[8].scaleX = -1f;
     * 9: "FaceA0"
     * 10: "Futile_White", sLeaser.sprites[10].shader = rCam.game.rainWorld.Shaders["FlatLight"];
     * 11: "pixel"
     */








    public void OnEnable()
    {

        try
        {
            option = new PebblesSlugOption();
            instance = this;

            


            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

            CustomLore.Apply();
            SSOracleHooks.Apply();
            PlayerHooks.Apply();
            RoomSpecificScripts.Apply();
            ShelterSS_AI.Apply();
            SSRoomEffects.Apply();
            OverseerHolograms_.Apply();
            SLOracleHooks.Apply();

            On.Player.ClassMechanicsArtificer += Player_ClassMechanicsArtificer;
            On.Player.CraftingResults += Player_CraftingResults;
            On.Player.GraspsCanBeCrafted += Player_GraspsCanBeCrafted;
            // On.Player.SwallowObject += Player_SwallowObject;
            On.Player.SpitUpCraftedObject += Player_SpitUpCraftedObject;
            // On.Player.GrabUpdate += Player_GrabUpdate;
            IL.Player.GrabUpdate += IL_Player_GrabUpdate;
            On.Creature.Violence += Creature_Violence;
            On.Player.CanBeSwallowed += Player_CanBeSwallowed;
            On.Player.ThrownSpear += Player_ThrownSpear;
            On.Player.Jump += Player_Jump;
            On.SlugcatStats.ctor += SlugcatStats_ctor;
            // On.Player.AddFood += Player_AddFood;

            IL.Player.EatMeatUpdate += IL_Player_EatMeatUpdate;
            // On.Player.UpdateAnimation += Player_UpdateAnimation;
            // IL.Player.BiteEdibleObject += IL_Player_BiteEdibleObject;
            On.Player.BiteEdibleObject += Player_BiteEdibleObject;

            On.Player.ctor += Player_ctor;
            // On.RainWorldGame.ctor += RainWorldGame_ctor;
            On.Player.Update += Player_Update;
            On.Player.NewRoom += Player_NewRoom;
            On.Player.Die += Player_Die;
            On.Player.Destroy += Player_Destroy;
            On.Player.ObjectCountsAsFood += Player_ObjectCountsAsFood;
            On.HUD.FoodMeter.SleepUpdate += HUD_FoodMeter_SleepUpdate;
            // On.Player.AddFood += Player_AddFood;
            // On.RoomPreparer.Update += RoomPreparer_Update;
            // On.RoomRealizer.Update += RoomRealizer_Update;

            // On.UnderwaterShock.Update += UnderwaterShock_Update;
            IL.ZapCoil.Update += IL_ZapCoil_Update;
            
            IL.Centipede.Shock += IL_Centipede_Shock;

            


            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;

            // IL.SlugcatStats.ctor += IL_SlugcatStats_ctor;
            On.SlugcatStats.SlugcatFoodMeter += SlugcatStats_SlugcatFoodMeter;

            On.RegionGate.customKarmaGateRequirements += RegionGate_customKarmaGateRequirements;

            IL.HUD.Map.CycleLabel.UpdateCycleText += IL_HUD_Map_CycleLabel_UpdateCycleText;
            IL.HUD.SubregionTracker.Update += IL_HUD_SubregionTracker_Update;
            // IL.ProcessManager.CreateValidationLabel += ProcessManager_CreateValidationLabel;
            IL.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += Menu_SlugcatSelectMenu_SlugcatPageContinue_ctor;
            // On.Menu.SlugcatSelectMenu.SlugcatPageContinue.Update += Menu_SlugcatSelectMenu_SlugcatPageContinue_Update;
            IL.ProcessManager.CreateValidationLabel += ProcessManager_CreateValidationLabel;

            // 好了，改完了。简单粗暴（
            /*new Hook(
            typeof(RedsIllness).GetProperty(nameof(RedsIllness.FoodToBeOkay), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            RedsIllness_FoodToBeOkay
            );*/

            new Hook(
            typeof(RedsIllness).GetProperty(nameof(RedsIllness.FoodFac), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            RedsIllness_FoodFac
            );

            new Hook(
            typeof(RedsIllness).GetProperty(nameof(RedsIllness.TimeFactor), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            RedsIllness_TimeFactor
            );

            
            new Hook(
            typeof(SaveState).GetProperty(nameof(SaveState.SlowFadeIn), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            SaveState_SlowFadeIn
            );

            new Hook(
            typeof(SSOracleSwarmer).GetProperty(nameof(SSOracleSwarmer.Edible), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            SSOracleSwarmer_Edible
            );

            new Hook(
            typeof(SLOracleSwarmer).GetProperty(nameof(SLOracleSwarmer.Edible), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            SLOracleSwarmer_Edible
            );

            



        }
        catch (Exception ex)
        {
            base.Logger.LogError(ex);
            throw;
        }

    }



    private void LoadResources(RainWorld rainWorld)
    {
        try
        {
            MachineConnector.SetRegisteredOI("PebblesSlug_by_syhnne", option);
            
            bool isInit = IsInit;
            if (!isInit)
            {
                LogStat("INIT");
                IsInit = true;
                Futile.atlasManager.LoadAtlas("atlases/fp_head");
                Futile.atlasManager.LoadAtlas("atlases/fp_tail");
                Futile.atlasManager.LoadAtlas("atlases/fp_arm");

                Futile.atlasManager.LoadImage("overseerHolograms/PebblesSlugHologram");
                // Futile.atlasManager.LogAllElementNames();
            }
        }
        catch (Exception ex)
        {
            base.Logger.LogError(ex);
            throw;
        }
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


    #region 图形
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////




    internal void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig(self, sLeaser, rCam);
        if (self.player.slugcatStats.name.value == SlugcatName)
        {
            FAtlas fatlas = Futile.atlasManager.LoadAtlas("atlases/fp_tail");
            TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
            {
                new TriangleMesh.Triangle(0, 1, 2),
                new TriangleMesh.Triangle(1, 2, 3),
                new TriangleMesh.Triangle(2, 3, 4),
                new TriangleMesh.Triangle(3, 4, 5),
                new TriangleMesh.Triangle(4, 5, 6),
                new TriangleMesh.Triangle(5, 6, 7),
                new TriangleMesh.Triangle(6, 7, 8),
                new TriangleMesh.Triangle(7, 8, 9),
                new TriangleMesh.Triangle(8, 9, 10),
                new TriangleMesh.Triangle(9, 10, 11),
                new TriangleMesh.Triangle(10, 11, 12),
                new TriangleMesh.Triangle(11, 12, 13),
                new TriangleMesh.Triangle(12, 13, 14),
            };
            TriangleMesh triangleMesh = new TriangleMesh("fp_tail", tris, false, false);
            triangleMesh.UVvertices[0] = fatlas._elementsByName["fp_tail"].uvBottomLeft;
            triangleMesh.UVvertices[1] = fatlas._elementsByName["fp_tail"].uvTopLeft;
            triangleMesh.UVvertices[13] = fatlas._elementsByName["fp_tail"].uvTopRight;
            triangleMesh.UVvertices[14] = fatlas._elementsByName["fp_tail"].uvBottomRight;
            float num = (triangleMesh.UVvertices[13].x - triangleMesh.UVvertices[1].x) / 6f;
            for (int i = 2; i < 14; i += 2)
            {
                triangleMesh.UVvertices[i].x = (float)((double)fatlas._elementsByName["fp_tail"].uvBottomLeft.x + (double)num * 0.5 * (double)i);
                triangleMesh.UVvertices[i].y = fatlas._elementsByName["fp_tail"].uvBottomLeft.y;
            }
            for (int j = 3; j < 13; j += 2)
            {
                triangleMesh.UVvertices[j].x = (float)((double)fatlas._elementsByName["fp_tail"].uvTopLeft.x + (double)num * 0.5 * (double)(j - 1));
                triangleMesh.UVvertices[j].y = fatlas._elementsByName["fp_tail"].uvTopLeft.y;
            }
            sLeaser.sprites[2] = triangleMesh;

            self.AddToContainer(sLeaser, rCam, null);
        }
    }




    internal void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (self.player.slugcatStats.name.value == SlugcatName)
        {
            // 理论上这个代码能简化一下，但我要先让它跑起来，剩下的我不敢动
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                if (i == 2)
                {
                    sLeaser.sprites[2].element = Futile.atlasManager.GetElementWithName("fp_tail");
                }
                else
                {
                    if (sLeaser.sprites[i].element.name.StartsWith(sLeaser.sprites[i].element.name))
                    {
                        if (Futile.atlasManager.DoesContainElementWithName("fp_" + sLeaser.sprites[i].element.name))
                        {
                            
                            sLeaser.sprites[i].element = Futile.atlasManager.GetElementWithName(sLeaser.sprites[i].element.name.Replace(sLeaser.sprites[i].element.name, "fp_" + sLeaser.sprites[i].element.name));
                        }

                    }
                }


            }
        }
    }



    internal void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);
        if (self.player.slugcatStats.name.value == SlugcatName)
        {
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                // 2是尾巴，9是眼睛，56是手，除此以外都涂成粉色。。
                // 这属于硬编码禁止玩家改颜色了，虽然我完全可以把所有颜色都换成贴图来解决这个问题，但我不想（
                if (ColoredBodyParts.Contains(i))
                {
                    sLeaser.sprites[i].color = eyesColor_hard;
                }
                else
                {
                    sLeaser.sprites[i].color = bodyColor_hard;
                }
            }
        }
    }


    #endregion







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











    #region 香菇病，雨循环倒计时，饱食度
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 从雨循环15开始，每隔5个雨循环涨一点休眠饱食度，再加上逐渐恶化的营养不良效果（
    // 我的建议是前7个雨循环内跑完剧情，亲测挂上了香菇病之后得至少两只秃鹫才能雨眠，而且最高矛伤会掉到1.2

    private void Player_AddFood(On.Player.orig_AddFood orig, Player self, int add)
    {
        orig(self, add);
        if (self.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            Plugin.Log("addfood:", add);
        }
    }


    /*private void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
    {
        orig(self, eu);
        if (self.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            for (int i = 0; i<2; i++)
            {
                if (self.input[0].pckp && self.grasps[i] != null && self.grasps[i].grabbed is Creature && self.CanEatMeat(self.grasps[i].grabbed as Creature) && (self.grasps[i].grabbed as Creature).Template.meatPoints > 0)
                {
                    Plugin.Log("meatpoints:", (self.grasps[i].grabbed as Creature).Template.meatPoints);
                }
            }
        }
    }*/


    // 经测试，这个饱食度设定只在自己存档，或者自己是玩家1的时候生效，所以我需要自己重写一遍
    // 悲伤的是这个东西不生效
    private void IL_Player_EatMeatUpdate(ILContext il)
    {
        ILCursor c = new(il);
        // 654 修改判定
        if (c.TryGotoNext(MoveType.After,
            i => i.Match(OpCodes.Ldsfld),
            i => i.Match(OpCodes.Call),
            i => i.Match(OpCodes.Brtrue_S),
            i => i.Match(OpCodes.Ldarg_0),
            i => i.Match(OpCodes.Ldfld),
            i => i.Match(OpCodes.Ldsfld),
            i => i.Match(OpCodes.Call)
            ))
        {
            Plugin.Log("match successfully - eatmeatupdate");
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<bool, SlugcatStats.Name, bool>>((isGourmand, name) =>
            {
                return isGourmand || (name == Plugin.SlugcatStatsName);
            });
        }
    }










    private delegate float orig_SlowFadeIn(SaveState self);
    private float SaveState_SlowFadeIn(orig_SlowFadeIn orig, SaveState self)
    {
        var result = orig(self);
        if (self.saveStateNumber.value == SlugcatName)
        {
            result = Mathf.Max(self.malnourished ? 4f : 0.8f, (self.cycleNumber >= RedsIllness.RedsCycles(self.redExtraCycles) && !self.deathPersistentSaveData.altEnding && !Custom.rainWorld.ExpeditionMode) ? Custom.LerpMap((float)self.cycleNumber, (float)RedsIllness.RedsCycles(false), (float)(RedsIllness.RedsCycles(false) + 5), 4f, 15f) : 0.8f);
        }
        return result;
    }




    // 从珍珠猫代码里抄的，总之这么写能跑，那就这么写吧（
    private delegate float orig_FoodFac(RedsIllness self);
    private float RedsIllness_FoodFac(orig_FoodFac orig, RedsIllness self)
    {
        var result = orig(self);
        if (self.player.slugcatStats.name.value == SlugcatName)
        {
            // 还是那个同款函数
            /*int r = 1 + (int)Math.Floor((float)(player.cycle+5) / Cycles * (MaxFood + 1 - MinFood));
            result = 1f / (float)r;*/
            result = Mathf.Max(0.2f, 1f / ((float)self.cycle * 0.25f + 2f));
        }
        return result;
    }



    // 他确实挂上了，但怪异的是，我必须先吃饱，才能让这个数变成我想要的数。
    private delegate int orig_FoodToBeOkay(RedsIllness self);
    private int RedsIllness_FoodToBeOkay(orig_FoodToBeOkay orig, RedsIllness self)
    {
        int result = orig(self);
        if (self.player.slugcatStats.name == SlugcatStatsName)
        {
            result = MinFoodNow;
            Plugin.LogStat("RedsIllness - Minfoodnow: ", MinFoodNow, " foodToHibernate: ", self.player.slugcatStats.foodToHibernate, " food to be okay: ", result);
        }
        return result;
    }




    private delegate float orig_TimeFactor(RedsIllness self);
    private float RedsIllness_TimeFactor(orig_TimeFactor orig, RedsIllness self)
    {
        var result = orig(self);
        if (self.player.slugcatStats.name == SlugcatStatsName)
        {
            result = 1f - 0.9f * Mathf.Max(Mathf.Max(self.fadeOutSlow ? Mathf.Pow(Mathf.InverseLerp(0f, 0.5f, self.player.abstractCreature.world.game.manager.fadeToBlack), 0.65f) : 0f, Mathf.InverseLerp(40f * Mathf.Lerp(12f, 21f, self.Severity), 40f, (float)self.counter) * Mathf.Lerp(0.2f, 0.5f, self.Severity)), self.CurrentFitIntensity * 0.1f);
        }
        return result;
    }









    // 修改游戏界面显示的雨循环倒计时以及饱食度
    private void Menu_SlugcatSelectMenu_SlugcatPageContinue_ctor(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        // 247 修改判定
        if (c.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Dup),
            (i) => i.Match(OpCodes.Ldc_I4_4),
            (i) => i.Match(OpCodes.Ldarg_S),
            (i) => i.Match(OpCodes.Ldsfld),
            (i) => i.Match(OpCodes.Call)
            ))
        {
            c.Emit(OpCodes.Ldarg, 4);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<bool, SlugcatStats.Name, Menu.SlugcatSelectMenu.SlugcatPageContinue, bool>>((isRed, name, menu) =>
            {
                return isRed || (name.value == SlugcatName && !menu.saveGameData.altEnding);
            });
        }

        ILCursor c2 = new ILCursor(il);
        // 189 修改食物条显示
        if (c2.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Ldarg_S),
            (i) => i.Match(OpCodes.Call),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Ldarg_S),
            (i) => i.Match(OpCodes.Call),
            (i) => i.Match(OpCodes.Ldfld)
            ))
        {
            c2.Emit(OpCodes.Ldarg, 4);
            c2.Emit(OpCodes.Ldarg_0);
            c2.EmitDelegate<Func<int, SlugcatStats.Name, Menu.SlugcatSelectMenu.SlugcatPageContinue, int>>((foodToHibernate, name, menu) =>
            {
                if (name.value == SlugcatName)
                {
                    int cycle = menu.saveGameData.cycle;
                    int result = MinFood;
                    if (!menu.saveGameData.altEnding)
                    {
                        result = CycleGetFood(cycle);
                    }
                    return Math.Min(result, MaxFood);
                }
                return foodToHibernate;
            });
        }

        ILCursor c3 = new ILCursor(il);
        // 256 修改雨循环显示数字
        if (c3.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Br_S),
            (i) => i.Match(OpCodes.Ldarg_0),
            (i) => i.Match(OpCodes.Call),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Call)
            ))
        {
            c3.Emit(OpCodes.Ldarg, 4);
            c3.EmitDelegate<Func<int, SlugcatStats.Name, int>>((redCycles, name) =>
            {
                if (name.value == SlugcatName)
                {
                    return Cycles;
                }
                return redCycles;
            });
        }
    }







    // 修改游戏内显示的雨循环倒计时
    private void IL_HUD_SubregionTracker_Update(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        // 164 修改是否是红猫的判定
        if (c.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Isinst),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Ldsfld),
            (i) => i.Match(OpCodes.Call)
            ))
        {
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate<Func<bool, Player, bool>>((isRed, player) =>
            {
                return isRed || (player.room.game.IsStorySession && player.room.game.GetStorySession.saveState.saveStateNumber == Plugin.SlugcatStatsName && !player.room.game.GetStorySession.saveState.deathPersistentSaveData.altEnding);
            });
        }

        ILCursor c2 = new ILCursor(il);
        // 175 修改RedsCycles函数返回值 啊 我恨死这个静态函数了
        if (c2.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Ldloc_0),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Callvirt),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Call)
            ))
        {
            c2.Emit(OpCodes.Ldloc_0);
            c2.EmitDelegate<Func<int, Player, int>>((RedsCycles, player) =>
            {
                if (player.room.game.IsStorySession && player.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
                {
                    return Cycles;
                }
                return RedsCycles;
            });
        }
    }






    // 不知道这个是干嘛的，但既然搜索搜出来了就改一下罢
    private void IL_HUD_Map_CycleLabel_UpdateCycleText(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        // 23 修改判定
        if (c.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Isinst),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Ldsfld),
            (i) => i.Match(OpCodes.Call)
            ))
        {
            c.Emit(OpCodes.Ldloc_0); //Player
            c.EmitDelegate<Func<bool, Player, bool>>((isRed, player) =>
            {
                return isRed || (player.room.game.IsStorySession && player.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName && !player.abstractCreature.world.game.GetStorySession.saveState.deathPersistentSaveData.altEnding);
            });
        }
        ILCursor c2 = new ILCursor(il);
        // 32 改数值
        if (c2.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Callvirt),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Callvirt),
            (i) => i.Match(OpCodes.Callvirt),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Call)
            ))
        {
            c2.Emit(OpCodes.Ldloc_0); //Player
            c2.EmitDelegate<Func<int, Player, int>>((redsCycles, player) =>
            {
                return player.slugcatStats.name.value == SlugcatName ? Cycles : redsCycles;
            });
        }
    }





    // 修改速通验证的循环数
    private void ProcessManager_CreateValidationLabel(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        // 25 修改判定
        if (c.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Ldloc_2),
            (i) => i.Match(OpCodes.Brfalse),
            (i) => i.Match(OpCodes.Ldloc_1),
            (i) => i.Match(OpCodes.Ldsfld),
            (i) => i.Match(OpCodes.Call)
            ))
        {
            c.Emit(OpCodes.Ldloc_1);
            c.Emit(OpCodes.Ldloc_2);
            c.EmitDelegate<Func<bool, SlugcatStats.Name, Menu.SlugcatSelectMenu.SaveGameData, bool>>((isRed, name, saveGameData) =>
            {
                return isRed || (name.value == SlugcatName && !saveGameData.altEnding);
            });
        }

        ILCursor c2 = new ILCursor(il);
        // 32 修改Cycles数值
        if (c2.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Ldloc_2),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Br_S),
            (i) => i.Match(OpCodes.Ldloc_2),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Call)
            ))
        {
            c2.Emit(OpCodes.Ldloc_1);
            c2.EmitDelegate<Func<int, SlugcatStats.Name, int>>((redsCycles, name) =>
            {
                return name.value == SlugcatName ? Cycles : redsCycles;
            });
        }
    }





    // 在雨眠页面上做个食物条移动动画
    private void HUD_FoodMeter_SleepUpdate(On.HUD.FoodMeter.orig_SleepUpdate orig, HUD.FoodMeter self)
    {
        if (self.hud.owner is Menu.SleepAndDeathScreen && (self.hud.owner as Menu.KarmaLadderScreen).myGamePackage.saveState.saveStateNumber == Plugin.SlugcatStatsName && !(self.hud.owner as Menu.KarmaLadderScreen).myGamePackage.saveState.deathPersistentSaveData.altEnding)
        {
            // 太好了，这个game package里面基本上够用了
            Menu.KarmaLadderScreen.SleepDeathScreenDataPackage package = (self.hud.owner as Menu.KarmaLadderScreen).myGamePackage;
            Menu.SleepAndDeathScreen owner = (self.hud.owner as Menu.SleepAndDeathScreen);
            if (CycleGetFood(package.saveState.cycleNumber - 1) < CycleGetFood(package.saveState.cycleNumber))
            {
                // Plugin.LogStat("HUD_FoodMeter_SleepUpdate - FOOD CHANGING survival limit: ", player.survivalLimit, " start malnourished: ", owner.startMalnourished);
                owner.startMalnourished = true;
                // 强制玩家观看动画。反正占不了他们几秒，但我可是做了一下午，都给我看（
                if (CycleGetFood(package.saveState.cycleNumber) == MinFood + 1)
                { owner.forceWatchAnimation = true; }
                self.survivalLimit = CycleGetFood(package.saveState.cycleNumber);

            }
        }
        orig(self);
    }






    // 实际修改饱食度的函数
    private IntVector2 SlugcatStats_SlugcatFoodMeter(On.SlugcatStats.orig_SlugcatFoodMeter orig, SlugcatStats.Name slugcat)
    {
        return (slugcat.value == SlugcatName) ? new IntVector2(MaxFood, MinFoodNow) : orig(slugcat);

    }






    // 这个东西被用的太多了，写个函数
    private static int CycleGetFood(int cycle)
    {
        int result = MinFood + (int)Math.Floor((float)cycle / Cycles * (MaxFood + 1 - MinFood));
        return result;
    }






    // 纯属复制粘贴游戏代码，只为绕过香菇病效果（
    private void CustomAddFood(Player player, int add)
    {
        if (player == null) { return; }
        add = Math.Min(add, player.MaxFoodInStomach - player.playerState.foodInStomach);
        if (ModManager.CoopAvailable && player.abstractCreature.world.game.IsStorySession && player.abstractCreature.world.game.Players[0] != player.abstractCreature && !player.isNPC)
        {
            PlayerState playerState = player.abstractCreature.world.game.Players[0].state as PlayerState;
            add = Math.Min(add, Math.Max(player.MaxFoodInStomach - playerState.foodInStomach, 0));
            Log(string.Format("Player add food {0}. Amount to add {1}", player.playerState.playerNumber, add), false);
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
                Log("FoodToBeOkay: ", player.redsIllness.FoodToBeOkay);
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
                MinFoodNow = Math.Min(CycleGetFood(cycle), MaxFood);
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
                // 以防挨饿之后他覆盖挨饿的饱食度
                // 我有一个猜想，redsillness会给玩家挂上malnourished属性，这导致在后来的某个函数里，foodToHibernate又变成了最大食物数
                // 然而游戏里又藏了一些别的代码，使得我达到foodToBeOkay之后这个值又恢复正常了。。
                // 总之，我不管了，玩家可能会自己发现这个让他们高兴的事实：即便你前一天晚上挨饿睡觉，第二天也不用吃8个食物就能恢复正常
                // TODO: 但是能修还是修一下

                // 他妈的，放着不修的下场就是他不知道什么时候变成了foodToHibernate永远都是8，测试的时候差点没给我折磨死
                // 见鬼的slugcatstats为什么在我playerctor前后都要调用，还要调用好几次
                // 今天跟你爆了
                self.slugcatStats.foodToHibernate = MinFoodNow;
                Plugin.LogStat("Player_ctor - minfoodnow: ", MinFoodNow, "food to hibernate(after): ", self.slugcatStats.foodToHibernate, " maxfood: ", MaxFood);
            }
            
            
        }

        
    }
















    #region 重力控制
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////













    // 垃圾回收
    private void Player_Destroy(On.Player.orig_Destroy orig, Player self)
    {
        bool getModule = playerModules.TryGetValue(self, out var module) && module.playerName == SlugcatStatsName;
        if (getModule)
        {
            module.gravityController?.Destroy();
            module.gravityController = null;
        }
        orig(self);
    }




    // 防止你那倒霉的联机队友在你死了之后顶着3倍重力艰难行走。我知道队友有可能也会控制重力，但是我懒得加判断
    private void Player_Die(On.Player.orig_Die orig, Player self)
    {
        bool getModule = playerModules.TryGetValue(self, out var module) && module.playerName == SlugcatStatsName;
        if (getModule && self.slugcatStats.name == SlugcatStatsName)
        {
            module.gravityController.Die();
        }
        orig(self);

    }





    






    // 这才是设定上真正修改重力的家伙
    // 卧槽，他卡bug
    /*private void GravityDisruptor_Update(On.GravityDisruptor.orig_Update orig, GravityDisruptor player, bool eu)
    {
        try
        {
            orig(player, eu);
            if (player.room != null && player.room.game != null
                && player.room.game.session is StoryGameSession && player.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName
                && player.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG) == null)
            {
                player.power = 1f - player.room.gravity;
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }*/




    #endregion












    #region 其他技能
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////






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












    // 还是那个不能吃神经元
    private bool Player_ObjectCountsAsFood(On.Player.orig_ObjectCountsAsFood orig, Player self, PhysicalObject obj)
    {
        bool result = orig(self, obj);
        if ( self.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            result = result && !(obj is OracleSwarmer);
        }
        return result;
    }



    private delegate bool orig_SLOracleSwarmerEdible(SLOracleSwarmer self);
    private bool SLOracleSwarmer_Edible(orig_SLOracleSwarmerEdible orig, SLOracleSwarmer self)
    {
        var result = orig(self);
        if (self.grabbedBy.Count > 0 && self.grabbedBy[0] != null && self.grabbedBy[0].grabber is Player && (self.grabbedBy[0].grabber as Player).slugcatStats.name == Plugin.SlugcatStatsName)
        {
            result = false;
        }
        return result;
    }



    private delegate bool orig_SSOracleSwarmerEdible(SSOracleSwarmer self);
    private bool SSOracleSwarmer_Edible(orig_SSOracleSwarmerEdible orig, SSOracleSwarmer self)
    {
        var result = orig(self);
        if (self.grabbedBy.Count > 0 && self.grabbedBy[0] != null && self.grabbedBy[0].grabber is Player && (self.grabbedBy[0].grabber as Player).slugcatStats.name == Plugin.SlugcatStatsName)
        {
            result = false;
        }
        return result;
    }








    // 不能吃神经元。我想这个应该用不着调用原版方法了吧。。
    private bool Player_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
    {
        if (self.slugcatStats.name.value == SlugcatName)
        {
            return (!ModManager.MSC || !(self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear)) && (testObj is Rock || testObj is DataPearl || testObj is FlareBomb || testObj is Lantern || testObj is FirecrackerPlant || (testObj is VultureGrub && !(testObj as VultureGrub).dead) || (testObj is Hazer && !(testObj as Hazer).dead && !(testObj as Hazer).hasSprayed) || testObj is FlyLure || testObj is ScavengerBomb || testObj is PuffBall || testObj is SporePlant || testObj is BubbleGrass || testObj is OracleSwarmer || testObj is NSHSwarmer || testObj is OverseerCarcass || (ModManager.MSC && testObj is FireEgg && self.FoodInStomach >= self.MaxFoodInStomach) || (ModManager.MSC && testObj is SingularityBomb && !(testObj as SingularityBomb).activateSingularity && !(testObj as SingularityBomb).activateSucktion));
        }
        else { return orig(self, testObj); }
    }













    // 增加跳跃能力
    private void Player_Jump(On.Player.orig_Jump orig, Player self)
    {
        orig(self);
        if (self.slugcatStats.name.value == SlugcatName)
        { 
            self.jumpBoost *= 1.2f;
        }
    }














    // 一矛超人，只要不使用二段跳，就是常驻2倍矛伤。使用二段跳会导致这个伤害发生衰减，最低不低于0.5。修改slugbase的基础矛伤可以使所有的值发生变化
    private void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
    {
        orig(self, spear);
        if (self.slugcatStats.name.value == SlugcatName)
        {
            float spearDmgBonus = 1f;
            if (self.pyroJumpCounter > 0)
            {
                spearDmgBonus /= self.pyroJumpCounter;
            }
            spear.spearDamageBonus *= (0.5f + spearDmgBonus);
            LogStat("spearDmgBonus: " + (0.5f+spearDmgBonus) + "  result: " + spear.spearDamageBonus);
        }
        
    }






    // TODO: 给这一坨东西单独做一个类
    // 除了特效以外，数值跟炸猫差不多，因为我不知道那堆二段跳的数值怎么改。我想改得小一点，让他没有那么强的机动性，不然太超模了（
    // 因为这个电击在水下是有伤害的（痛击你的队友。jpg）我不是故意的，我是真的写不出来那个判定。我不知道他为什么会闪退。。
    // 我大概应该用原版方法，然后做ilhooking。但是，说真的，想想那个工作量吧（汗）我都不太清楚自己究竟改了些什么
    private void Player_ClassMechanicsArtificer(On.Player.orig_ClassMechanicsArtificer orig, Player self)
    {
        // Log("slugcat name: "+ player.slugcatStats.name.value);
        if (self.slugcatStats.name.value == SlugcatName)
        {
            Room room = self.room;
            bool flag = self.wantToJump > 0 && self.input[0].pckp;
            bool flag2 = self.eatMeat >= 20 || self.maulTimer >= 15;
            int explosionCapacity = PebblesSlugOption.ExplosionCapacity.Value;
            int num = Mathf.Max(1, explosionCapacity - 5);
            // 这是怎么回事？
            orig(self);
            if (self.pyroJumpCounter > 0 && (self.Consious || self.dead))
            {
                self.pyroJumpCooldown -= 1f;
                if (self.pyroJumpCooldown <= 0f)
                {
                    if (self.pyroJumpCounter >= num)
                    {
                        self.pyroJumpCooldown = 40f;
                    }
                    else
                    {
                        self.pyroJumpCooldown = 60f;
                    }
                    self.pyroJumpCounter--;
                }
            }
            self.pyroParryCooldown -= 1f;
            if (self.pyroJumpCounter >= num)
            {
                if (Random.value < 0.25f)
                {
                    // 这应该是炸多了的冒烟效果
                    self.room.AddObject(new Explosion.ExplosionSmoke(self.mainBodyChunk.pos, Custom.RNV() * 2f * Random.value, 1f));
                }
                if (Random.value < 0.5f)
                {
                    self.room.AddObject(new Spark(self.mainBodyChunk.pos, Custom.RNV(), Color.white, null, 4, 8));
                }
            }

            if (flag
                && !self.pyroJumpped
                && self.canJump <= 0 && !flag2
                && (self.input[0].y >= 0 || (self.input[0].y < 0 && (self.bodyMode == Player.BodyModeIndex.ZeroG || self.gravity <= 0.1f)))
                && self.Consious && self.bodyMode != Player.BodyModeIndex.Crawl
                && self.bodyMode != Player.BodyModeIndex.CorridorClimb
                && self.bodyMode != Player.BodyModeIndex.ClimbIntoShortCut
                && self.animation != Player.AnimationIndex.HangFromBeam
                && self.animation != Player.AnimationIndex.ClimbOnBeam
                && self.bodyMode != Player.BodyModeIndex.WallClimb
                && self.bodyMode != Player.BodyModeIndex.Swimming
                && self.animation != Player.AnimationIndex.AntlerClimb
                && self.animation != Player.AnimationIndex.VineGrab
                && self.animation != Player.AnimationIndex.ZeroGPoleGrab
                && self.onBack == null)
            {
                self.pyroJumpped = true;
                self.pyroJumpDropLock = 40;
                self.noGrabCounter = 5;
                Vector2 pos = self.firstChunk.pos;
                // 这是正经爆炸效果罢
                // 哦不，这是有烟无伤的二段跳
                // 现在有伤害了，只要你在水里起跳，就会让附近生物触电，只不过没什么伤害


                for (int i = 0; i < 8; i++)
                {
                    Vector2 vector = Custom.DegToVec(360f * Random.value);
                    self.room.AddObject(new MouseSpark(pos + vector * 9f, self.firstChunk.vel + vector * 36f * Random.value, 20f, new Color(0.7f, 1f, 1f)));
                }
                self.room.AddObject(new Explosion.ExplosionLight(pos, 200f, 1f, 4, new Color(0.7f, 1f, 1f)));
                for (int j = 0; j < 10; j++)
                {
                    Vector2 vector = Custom.RNV();
                    self.room.AddObject(new Spark(pos + vector * Random.value * 40f, vector * Mathf.Lerp(4f, 30f, Random.value), Color.white, null, 4, 18));
                }
                self.room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, self.firstChunk.pos);
                self.room.InGameNoise(new InGameNoise(pos, 8000f, self, 1f));
                int num2 = Mathf.Max(1, explosionCapacity - 3);
                if (self.Submersion <= 0.5f)
                {
                    self.room.AddObject(new UnderwaterShock(self.room, self, pos, 10, 500f, 0.5f, self, new Color(0.8f, 0.8f, 1f)));
                }
                if (self.bodyMode == Player.BodyModeIndex.ZeroG || self.room.gravity == 0f || self.gravity == 0f)
                {
                    float num3 = (float)self.input[0].x;
                    float num4 = (float)self.input[0].y;
                    while (num3 == 0f && num4 == 0f)
                    {
                        num3 = (float)(((double)Random.value <= 0.33) ? 0 : (((double)Random.value <= 0.5) ? 1 : -1));
                        num4 = (float)(((double)Random.value <= 0.33) ? 0 : (((double)Random.value <= 0.5) ? 1 : -1));
                    }
                    self.bodyChunks[0].vel.x = 9f * num3;
                    self.bodyChunks[0].vel.y = 9f * num4;
                    self.bodyChunks[1].vel.x = 8f * num3;
                    self.bodyChunks[1].vel.y = 8f * num4;
                    self.pyroJumpCooldown = 150f;
                    self.pyroJumpCounter++;
                }
                else
                {
                    if (self.input[0].x != 0)
                    {
                        self.bodyChunks[0].vel.y = Mathf.Min(self.bodyChunks[0].vel.y, 0f) + 8f;
                        self.bodyChunks[1].vel.y = Mathf.Min(self.bodyChunks[1].vel.y, 0f) + 7f;
                        self.jumpBoost = 6f;
                    }
                    if (self.input[0].x == 0 || self.input[0].y == 1)
                    {
                        if (self.pyroJumpCounter >= num2)
                        {
                            self.bodyChunks[0].vel.y = 16f;
                            self.bodyChunks[1].vel.y = 15f;
                            self.jumpBoost = 10f;
                        }
                        else
                        {
                            self.bodyChunks[0].vel.y = 11f;
                            self.bodyChunks[1].vel.y = 10f;
                            self.jumpBoost = 8f;
                        }
                    }
                    if (self.input[0].y == 1)
                    {
                        self.bodyChunks[0].vel.x = 8f * (float)self.input[0].x;
                        self.bodyChunks[1].vel.x = 6f * (float)self.input[0].x;
                    }
                    else
                    {
                        self.bodyChunks[0].vel.x = 14f * (float)self.input[0].x;
                        self.bodyChunks[1].vel.x = 12f * (float)self.input[0].x;
                    }
                    self.animation = Player.AnimationIndex.Flip;
                    self.pyroJumpCounter++;
                    self.pyroJumpCooldown = 150f;
                    self.bodyMode = Player.BodyModeIndex.Default;
                }
                if (self.pyroJumpCounter >= num2)
                {
                    self.Stun(60 * (self.pyroJumpCounter - (num2 - 1)));
                }
                if (self.pyroJumpCounter >= explosionCapacity)
                {
                    self.room.AddObject(new ShockWave(pos, 200f, 0.2f, 6, false));
                    self.room.AddObject(new Explosion(self.room, self, pos, 7, 350f, 26.2f, 2f, 280f, 0.35f, self, 0.7f, 160f, 1f));
                    self.room.ScreenMovement(new Vector2?(pos), default(Vector2), 1.3f);
                    self.room.InGameNoise(new InGameNoise(pos, 9000f, self, 1f));
                    self.Die();
                }

            }


            else if (flag

                && !flag2
                && (self.input[0].y < 0 || self.bodyMode == Player.BodyModeIndex.Crawl)
                && (self.canJump > 0 || self.input[0].y < 0) && self.Consious && !self.pyroJumpped && self.pyroParryCooldown <= 0f)
            {
                if (self.canJump <= 0)
                {
                    self.pyroJumpped = true;
                    self.bodyChunks[0].vel.y = 8f;
                    self.bodyChunks[1].vel.y = 6f;
                    self.jumpBoost = 6f;
                    self.forceSleepCounter = 0;
                }
                if (self.pyroJumpCounter <= num)
                {
                    self.pyroJumpCounter += 2;
                }
                else
                {
                    self.pyroJumpCounter++;
                }
                self.pyroParryCooldown = 40f;
                self.pyroJumpCooldown = 150f;

                Vector2 pos2 = self.firstChunk.pos;

                for (int i = 0; i < 8; i++)
                {
                    Vector2 vector3 = Custom.DegToVec(360f * Random.value);
                    self.room.AddObject(new MouseSpark(pos2 + vector3 * 9f, self.firstChunk.vel + vector3 * 36f * Random.value, 20f, new Color(0.7f, 1f, 1f)));
                }


                self.room.AddObject(new Explosion.ExplosionLight(pos2, 200f, 1f, 4, new Color(0.7f, 1f, 1f)));

                for (int l = 0; l < 8; l++)
                {
                    Vector2 vector2 = Custom.RNV();
                    self.room.AddObject(new Spark(pos2 + vector2 * Random.value * 40f, vector2 * Mathf.Lerp(4f, 30f, Random.value), Color.white, null, 4, 18));
                }
                self.room.AddObject(new ShockWave(pos2, 200f, 0.2f, 6, false));
                self.room.AddObject(new ZapCoil.ZapFlash(pos2, 10f));
                // player.room.PlaySound(SoundID.Flare_Bomb_Burn, pos2);
                // player.room.PlaySound(SoundID.Zapper_Zap, pos2, 1f, 0.2f + 0.25f * Random.value);
                self.room.PlaySound(SoundID.Zapper_Zap, pos2, 1f, 1f + 0.25f * Random.value);
                // player.room.PlaySound(SoundID.Fire_Spear_Explode, pos2, 0.3f + Random.value * 0.3f, 0.5f + Random.value * 2f);
                self.room.InGameNoise(new InGameNoise(pos2, 8000f, self, 1f));

                if (self.room.Darkness(pos2) > 0f)
                {
                    self.room.AddObject(new LightSource(pos2, false, new Color(0.7f, 1f, 1f), self));
                }

                List<Weapon> list = new List<Weapon>();
                for (int m = 0; m < self.room.physicalObjects.Length; m++)
                {
                    for (int n = 0; n < self.room.physicalObjects[m].Count; n++)
                    {
                        if (self.room.physicalObjects[m][n] is Weapon)
                        {
                            Weapon weapon = self.room.physicalObjects[m][n] as Weapon;
                            if (weapon.mode == Weapon.Mode.Thrown && Custom.Dist(pos2, weapon.firstChunk.pos) < 300f)
                            {
                                list.Add(weapon);
                            }
                        }
                        bool flag3;
                        if (ModManager.CoopAvailable && !Custom.rainWorld.options.friendlyFire)
                        {
                            Player player = self.room.physicalObjects[m][n] as Player;
                            flag3 = (player == null || player.isNPC);
                        }
                        else
                        {
                            flag3 = true;
                        }
                        bool flag4 = flag3;
                        if (self.room.physicalObjects[m][n] is Creature && self.room.physicalObjects[m][n] != self && flag4)
                        {
                            Creature creature = self.room.physicalObjects[m][n] as Creature;
                            if (Custom.Dist(pos2, creature.firstChunk.pos) < 200f && (Custom.Dist(pos2, creature.firstChunk.pos) < 60f || self.room.VisualContact(self.abstractCreature.pos, creature.abstractCreature.pos)))
                            {
                                self.room.socialEventRecognizer.WeaponAttack(null, self, creature, true);
                                creature.SetKillTag(self.abstractCreature);
                                if (creature is Scavenger)
                                {
                                    (creature as Scavenger).HeavyStun(80);
                                    creature.Blind(400);
                                }
                                else
                                {
                                    creature.Stun(80);
                                    creature.Blind(400);
                                }
                                creature.firstChunk.vel = Custom.DegToVec(Custom.AimFromOneVectorToAnother(pos2, creature.firstChunk.pos)) * 30f;
                                if (creature is TentaclePlant)
                                {
                                    for (int num5 = 0; num5 < creature.grasps.Length; num5++)
                                    {
                                        creature.ReleaseGrasp(num5);
                                    }
                                }
                            }
                        }
                    }
                }
                if (self.Submersion <= 0.5f)
                {
                    self.room.AddObject(new UnderwaterShock(self.room, self, pos2, 10, 800f, 2f, self, new Color(0.8f, 0.8f, 1f)));
                }
                if (list.Count > 0 && self.room.game.IsArenaSession)
                {
                    self.room.game.GetArenaGameSession.arenaSitting.players[0].parries++;
                }
                for (int num6 = 0; num6 < list.Count; num6++)
                {
                    list[num6].ChangeMode(Weapon.Mode.Free);
                    list[num6].firstChunk.vel = Custom.DegToVec(Custom.AimFromOneVectorToAnother(pos2, list[num6].firstChunk.pos)) * 20f;
                    list[num6].SetRandomSpin();
                }
                int num7 = Mathf.Max(1, explosionCapacity - 3);
                if (self.pyroJumpCounter >= num7)
                {
                    self.Stun(60 * (self.pyroJumpCounter - (num7 - 1)));
                }
                if (self.pyroJumpCounter >= explosionCapacity)
                {
                    self.room.AddObject(new ShockWave(pos2, 200f, 0.2f, 6, false));
                    self.room.AddObject(new Explosion(self.room, self, pos2, 7, 350f, 26.2f, 2f, 280f, 0.35f, self, 0.7f, 160f, 1f));
                    self.room.ScreenMovement(new Vector2?(pos2), default(Vector2), 1.3f);
                    self.room.InGameNoise(new InGameNoise(pos2, 9000f, self, 1f));
                    self.Die();
                }
            }


            if (self.canJump > 0
                || !self.Consious
                || self.Stunned
                || self.animation == Player.AnimationIndex.HangFromBeam
                || self.animation == Player.AnimationIndex.ClimbOnBeam
                || self.bodyMode == Player.BodyModeIndex.WallClimb
                || self.animation == Player.AnimationIndex.AntlerClimb
                || self.animation == Player.AnimationIndex.VineGrab
                || self.animation == Player.AnimationIndex.ZeroGPoleGrab
                || self.bodyMode == Player.BodyModeIndex.Swimming
                || ((self.bodyMode == Player.BodyModeIndex.ZeroG || self.room.gravity <= 0.5f || self.gravity <= 0.5f) && (self.wantToJump == 0 || !self.input[0].pckp)))
            {
                self.pyroJumpped = false;
            }
        }
        else { orig(self); }

    }






    private AbstractPhysicalObject.AbstractObjectType Player_CraftingResults(On.Player.orig_CraftingResults orig, Player self)
    {
        if (self.slugcatStats.name.value == SlugcatName)
        {
            if (self.FoodInStomach > 1)
            {
                Creature.Grasp[] grasps = self.grasps;
                for (int i = 0; i < grasps.Length; i++)
                {
                    if (grasps[i] != null && grasps[i].grabbed is IPlayerEdible && (grasps[i].grabbed as IPlayerEdible).Edible)
                    {
                        return null;
                    }
                }
                //要实现的效果：只拦截有电的电矛。没电的电矛、炸矛、普通矛都不拦截
                // 没事了，现在电矛能吃（大雾
                if (grasps[0] != null && grasps[0].grabbed is Spear)
                {
                    return AbstractPhysicalObject.AbstractObjectType.Spear;

                }
                if (grasps[0] == null && grasps[1] != null && grasps[1].grabbed is Spear && self.objectInStomach == null)
                {
                    return AbstractPhysicalObject.AbstractObjectType.Spear;
                }

            }
            else if (PebblesSlugOption.AddFoodOnShock.Value && self.grasps[0] != null && self.grasps[0].grabbed is ElectricSpear && (self.grasps[0].grabbed as Spear).abstractSpear.electricCharge > 0)
            {
                return AbstractPhysicalObject.AbstractObjectType.Spear;
            }
            else if (PebblesSlugOption.AddFoodOnShock.Value && self.grasps[0] == null && self.grasps[1] != null && self.grasps[1].grabbed is ElectricSpear && self.objectInStomach == null && (self.grasps[0].grabbed as Spear).abstractSpear.electricCharge > 0)
            {
                return AbstractPhysicalObject.AbstractObjectType.Spear;
            }
            return null;
        }
        else { return orig(self); }
    }






    private bool Player_GraspsCanBeCrafted(On.Player.orig_GraspsCanBeCrafted orig, Player self)
    {

        if (self.slugcatStats.name.value == SlugcatName && (self.CraftingResults() != null))
        {
            return true;
        }
        return orig(self);
    }






    // 下面这个暂时没用，但先留着以防我日后突然想给他加点别的能力
    private void Player_SwallowObject(On.Player.orig_SwallowObject orig, Player self, int grasp)
    {
        if (self.slugcatStats.name.value == SlugcatName)
        {
            if (grasp < 0 || self.grasps[grasp] == null)
            {
                return;
            }
            AbstractPhysicalObject abstractPhysicalObject = self.grasps[grasp].grabbed.abstractPhysicalObject;
            if (abstractPhysicalObject is AbstractSpear)
            {
                (abstractPhysicalObject as AbstractSpear).stuckInWallCycles = 0;
            }
            self.objectInStomach = abstractPhysicalObject;
            if (ModManager.MMF && self.room.game.IsStorySession)
            {
                (self.room.game.session as StoryGameSession).RemovePersistentTracker(self.objectInStomach);
            }
            self.ReleaseGrasp(grasp);
            self.objectInStomach.realizedObject.RemoveFromRoom();
            self.objectInStomach.Abstractize(self.abstractCreature.pos);
            self.objectInStomach.Room.RemoveEntity(self.objectInStomach);
            if (self.FoodInStomach > 0)
            {
                if (abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.Spear && !(abstractPhysicalObject as AbstractSpear).explosive && !(abstractPhysicalObject as AbstractSpear).electric)
                {
                    // 这应该是生成矛的代码。那么它为什么不起作用呢（恼
                    abstractPhysicalObject = new AbstractSpear(self.room.world, null, self.room.GetWorldCoordinate(self.mainBodyChunk.pos), self.room.game.GetNewID(), true);
                    // 经测试，生成炸矛和下面的代码无关（1）那么我加这hook有用吗？一会把它删了逝逝
                    self.SubtractFood(1);
                }
            }
            self.objectInStomach = abstractPhysicalObject;
            self.objectInStomach.Abstractize(self.abstractCreature.pos);
            BodyChunk mainBodyChunk = self.mainBodyChunk;
            mainBodyChunk.vel.y = mainBodyChunk.vel.y + 2f;
            self.room.PlaySound(SoundID.Slugcat_Swallow_Item, self.mainBodyChunk);
        }
        else { orig(self, grasp); }
    }











    // 修改合成结果
    private void Player_SpitUpCraftedObject(On.Player.orig_SpitUpCraftedObject orig, Player self)
    {
        if (self.slugcatStats.name.value == SlugcatName)
        {
            var vector = self.mainBodyChunk.pos;

            // TODO: 我要写一个craftingtutorial，并且单独绑一个变量，因为它内容跟原来的不一样
            self.room.PlaySound(SoundID.Slugcat_Swallow_Item, self.mainBodyChunk);

            for (int i = 0; i < self.grasps.Length; i++)
            {
                if (self.grasps[i] != null)
                {
                    AbstractPhysicalObject grabbed = self.grasps[i].grabbed.abstractPhysicalObject;
                    if (grabbed is AbstractSpear)
                    {
                        AbstractSpear spear = grabbed as AbstractSpear;
                        if (spear.explosive)
                        {
                            ExplosiveSpear explosiveSpear = self.grasps[i].grabbed as ExplosiveSpear;
                            self.room.AddObject(new SootMark(self.room, vector, 50f, false));
                            self.room.AddObject(new Explosion(self.room, explosiveSpear, vector, 5, 50f, 4f, 0.1f, 60f, 0.3f, explosiveSpear.thrownBy, 0.8f, 0f, 0.7f));
                            for (int g = 0; g < 14; g++)
                            {
                                self.room.AddObject(new Explosion.ExplosionSmoke(vector, Custom.RNV() * 5f * Random.value, 1f));
                            }
                            self.room.AddObject(new Explosion.ExplosionLight(vector, 160f, 1f, 3, explosiveSpear.explodeColor));
                            self.room.AddObject(new ExplosionSpikes(self.room, vector, 9, 4f, 5f, 5f, 90f, explosiveSpear.explodeColor));
                            self.room.AddObject(new ShockWave(vector, 60f, 0.045f, 4, false));
                            for (int j = 0; j < 20; j++)
                            {
                                Vector2 vector2 = Custom.RNV();
                                self.room.AddObject(new Spark(vector + vector2 * Random.value * 40f, vector2 * Mathf.Lerp(4f, 30f, Random.value), explosiveSpear.explodeColor, null, 4, 18));
                            }
                            self.room.ScreenMovement(new Vector2?(vector), default(Vector2), 0.7f);
                            for (int k = 0; k < 2; k++)
                            {
                                Smolder smolder = null;
                                if (explosiveSpear.stuckInObject != null)
                                {
                                    smolder = new Smolder(self.room, explosiveSpear.stuckInChunk.pos, explosiveSpear.stuckInChunk, explosiveSpear.stuckInAppendage);
                                }
                                else
                                {
                                    Vector2? vector3 = SharedPhysics.ExactTerrainRayTracePos(self.room, explosiveSpear.firstChunk.pos, explosiveSpear.firstChunk.pos + ((k == 0) ? (explosiveSpear.rotation * 20f) : (Custom.RNV() * 20f)));
                                    if (vector3 != null)
                                    {
                                        smolder = new Smolder(self.room, vector3.Value + Custom.DirVec(vector3.Value, explosiveSpear.firstChunk.pos) * 3f, null, null);
                                    }
                                }
                                if (smolder != null)
                                {
                                    self.room.AddObject(smolder);
                                }
                            }
                            self.Stun(200);
                            explosiveSpear.abstractPhysicalObject.LoseAllStuckObjects();
                            explosiveSpear.room.PlaySound(SoundID.Fire_Spear_Explode, vector);
                            explosiveSpear.room.InGameNoise(new InGameNoise(vector, 8000f, explosiveSpear, 1f));
                            explosiveSpear.Destroy();
                        }
                        // 好了，谁能告诉我拿着有电的电矛的时候吐了两格是什么情况
                        else if (PebblesSlugOption.AddFoodOnShock.Value && spear.electric && spear.electricCharge > 0)
                        {
                            // 其实电矛是一种用来储存食物的工具（大雾
                            // 绕过香菇病的一种加食物方法
                            Log("HOLDING ELECTRIC SPEAR");
                            CustomAddFood(self, spear.electricCharge);
                            spear.electricCharge = 0;
                        }
                        else
                        {
                            self.ReleaseGrasp(i);
                            grabbed.realizedObject.RemoveFromRoom();
                            self.room.abstractRoom.RemoveEntity(grabbed);
                            // 对了，生成矛是这个。。我可能是那个眼瞎。。
                            self.SubtractFood(2);
                            AbstractSpear abstractSpear = new AbstractSpear(self.room.world, null, self.abstractCreature.pos, self.room.game.GetNewID(), false, true);
                            self.room.abstractRoom.AddEntity(abstractSpear);
                            abstractSpear.RealizeInRoom();
                            abstractSpear.electricCharge = 2;
                            if (self.FreeHand() != -1)
                            {
                                self.SlugcatGrab(abstractSpear.realizedObject, self.FreeHand());
                            }
                        }
                        return;
                    }
                }
            }
        }
        else { orig(self); }
    }






    private void Player_BiteEdibleObject(On.Player.orig_BiteEdibleObject orig, Player self, bool eu)
    {
        if (self.slugcatStats.name == Plugin.SlugcatStatsName && self.grasps[0] != null && self.grasps[1] != null
            && (self.grasps[0].grabbed is SSOracleSwarmer || self.grasps[0].grabbed is SLOracleSwarmer) && self.grasps[1].grabbed is IPlayerEdible)
        {
            if ((self.grasps[1].grabbed as IPlayerEdible).BitesLeft == 1 && self.SessionRecord != null)
            {
                self.SessionRecord.AddEat(self.grasps[1].grabbed);
            }
            if (self.grasps[1].grabbed is Creature)
            {
                (self.grasps[1].grabbed as Creature).SetKillTag(self.abstractCreature);
            }
            if (self.graphicsModule != null)
            {
                (self.graphicsModule as PlayerGraphics).BiteFly(1);
            }
            (self.grasps[1].grabbed as IPlayerEdible).BitByPlayer(self.grasps[1], eu);
            return;
        }
        else { orig(self, eu); }

    }




    // 这代码看得我两眼一黑
    // 我现在就要
    // 重构代码（扯衣服。jpg
    // 卧槽。什么情况。为什么一执行委托就报错。我寻思我也妹打错字啊。
    private void IL_Player_BiteEdibleObject(ILContext il)
    {
        ILCursor c3 = new ILCursor(il);
        // 13
        if (c3.TryGotoNext(MoveType.After,
            (i) => i.MatchCall<Creature>("get_grasps"),
            (i) => i.Match(OpCodes.Ldloc_0),
            (i) => i.Match(OpCodes.Ldelem_Ref),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Isinst)
            ))
        {
            /*c3.Emit(OpCodes.Ldarg_0);
            c3.Emit(OpCodes.Ldloc_0);
            c3.EmitDelegate<Func<bool, Player, int, bool>>((edible, player, grasp) =>
            {
                if (player.slugcatStats.name == SlugcatStatsName)
                {
                    bool isNotOracleSwarmer = player.grasps[grasp] != null && player.grasps[grasp].grabbed != null && player.grasps[grasp].grabbed is not OracleSwarmer;
                    return (edible && isNotOracleSwarmer);
                }
                else
                {
                    return edible;
                }
            });*/
            c3.EmitDelegate<Func<bool, bool>>((edible) =>
            {
                return edible;
            });
        }
    }






    // 修改神经元的可食用性和合成判定
    private void IL_Player_GrabUpdate(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        // 337末尾，修改神经元的可食用性。没错，我要借用一下他自带的那个brfalse。。因为我自己不会写！！
        if (c.TryGotoNext(MoveType.After,
            (i) => i.MatchCall<Creature>("get_grasps"),
            (i) => i.Match(OpCodes.Ldloc_S),
            (i) => i.Match(OpCodes.Ldelem_Ref),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Isinst),
            (i) => i.Match(OpCodes.Callvirt)
            ))
        {
            // 啊哈！我是天才！
            // 怎么解决brfalse无法定位的问题：不要用自己的brfalse，直接绑架一个他原本的判断，然后把那个判断的输出结果和我加的判断绑在一起。反正他们是and关系
            // 这样一来，比原本直接改player_grabupdate的方法更加方便，因为另一个地方我就不用改了。
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 13);
            c.EmitDelegate<Func<bool, Player, int, bool>>((edible, self, grasp) =>
            {
                if (self.slugcatStats.name == SlugcatStatsName)
                {
                    bool isNotOracleSwarmer = !(self.grasps[grasp].grabbed is OracleSwarmer);
                    return (edible && isNotOracleSwarmer);
                }
                else
                {
                    return edible;
                }
                
            });
        }

        // 533末尾，骗代码说我是工匠，让我合成
        // 有的人没发现自己指针定位错了，白修一个小时bug，我不说是谁
        ILCursor c2 = new ILCursor(il);
        if (c2.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Ldc_I4_M1),
            (i) => i.Match(OpCodes.Beq_S),
            (i) => i.Match(OpCodes.Ldarg_0),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Ldsfld),
            (i) => i.Match(OpCodes.Call)
            ))
        {
            c2.Emit(OpCodes.Ldarg_0);
            c2.EmitDelegate<Func<bool, Player, bool>>((isArtificer, self) =>
            {
                if (self.slugcatStats.name == SlugcatStatsName)
                {
                    // 这么做是为了防止误触，因为我自己特么的误触好几次了，我想吃东西来着结果吃到刚才用来鲨人的矛，反倒吐了两格
                    if (Plugin.instance.option.CraftKey.Value == KeyCode.None) return true;
                    else if (Input.GetKey(Plugin.instance.option.CraftKey.Value)) return true;
                    else return false;
                }
                else { return isArtificer; }

            });
        }

    }















    // 被电不仅不会死，还会吃饱（？
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

        /*// 我测。为什么你要访问房间重力啊。
        // 506 
        // 怪不得没人做重力控制。。我真是踩着雷区了。。
        ILCursor c2 = new ILCursor(il);
        if (c2.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Callvirt),
            (i) => i.Match(OpCodes.Brfalse_S),
            (i) => i.MatchLdarg(0),
            (i) => i.MatchLdarg(0),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.MatchLdarg(0),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Ldfld)
            ))
        {
            Log("match! - zapcoil");
            c2.Emit(OpCodes.Ldarg_0);
            c2.EmitDelegate<Func<float, Room, float>>((gravity, room) =>
            {
                if (room!=null && room.game.session is StoryGameSession && room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
                {
                    return 0f;
                }
                return gravity;
            });
        }*/
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











