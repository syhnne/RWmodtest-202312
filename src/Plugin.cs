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





namespace PebblesSlug;

// 吾日三省吾身：加判定了吗？指针定位写对了吗？挂hook了吗？






[BepInPlugin(MOD_ID, "PebblesSlug", "0.1.0")]
class Plugin : BaseUnityPlugin
{
    internal const string MOD_ID = "PebblesSlug_by_syhnne";



    // 不要给猫改名！！！不要给猫改名！！！不要给猫改名！！！



    public static new ManualLogSource Logger { get; internal set; }

    public PebblesSlugOption option;


    internal bool IsInit;
    internal static readonly List<int> ColoredBodyParts = new List<int>() { 2, 3, 5, 6, 7, 8, 9, };

    // 以下自定义属性会覆盖slugbase的属性，我的建议是别改，现在json文件里已经没有饱食度数据了，但我不知道有了会导致什么后果
    internal static readonly int Cycles = 21;
    internal static readonly int MaxFood = 8;
    internal static readonly int MinFood = 5;
    internal int MinFoodNow = 5;

    internal static readonly Color32 bodyColor_hard = new Color32(254, 104, 202, 255);
    internal static readonly Color eyesColor_hard = new Color(1f, 1f, 1f);

    internal static readonly string SlugcatName = "PebblesSlug";
    internal static readonly SlugcatStats.Name SlugcatStatsName = new SlugcatStats.Name(SlugcatName);
    internal static readonly Oracle.OracleID oracleID = new Oracle.OracleID("PL");
    internal static readonly bool ShowLogs = true;
    internal static Plugin instance;
    


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








    // 加入钩子
    public void OnEnable()
    {

        try
        {
            option = new PebblesSlugOption();
            instance = this;

            CustomLore.Apply();
            SSOracleHooks.Apply();


            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

            On.Player.ClassMechanicsArtificer += Player_ClassMechanicsArtificer;
            On.Player.CraftingResults += Player_CraftingResults;
            On.Player.GraspsCanBeCrafted += Player_GraspsCanBeCrafted;
            // On.Player.SwallowObject += Player_SwallowObject;
            On.Player.SpitUpCraftedObject += Player_SpitUpCraftedObject_old;
            IL.Player.GrabUpdate += Player_GrabUpdate;
            On.Creature.Violence += Creature_Violence;
            On.Player.CanBeSwallowed += Player_CanBeSwallowed;
            On.Player.ThrownSpear += Player_ThrownSpear;
            On.Player.Jump += Player_Jump;

            On.Player.ctor += Player_ctor;
            On.RainWorldGame.ctor += RainWorldGame_ctor;
            On.Player.Update += Player_Update;
            On.Player.NewRoom += Player_NewRoom;
            On.Player.Die += Player_Die;
            On.Player.Destroy += Player_Destroy;

            // On.UnderwaterShock.Update += UnderwaterShock_Update;
            IL.ZapCoil.Update += ZapCoil_Update;
            IL.Centipede.Shock += Centipede_Shock;


            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;

            // IL.SlugcatStats.ctor += IL_SlugcatStats_ctor;
            On.SlugcatStats.SlugcatFoodMeter += SlugcatStats_SlugcatFoodMeter;



            // 急 下面这俩貌似是根本没挂上 他妈的怎么回事 这三个都挂不上我操
            IL.HUD.Map.CycleLabel.UpdateCycleText += HUD_Map_CycleLabel_UpdateCycleText;
            IL.HUD.SubregionTracker.Update += HUD_SubregionTracker_Update;
            // IL.ProcessManager.CreateValidationLabel += ProcessManager_CreateValidationLabel;
            IL.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += Menu_SlugcatSelectMenu_SlugcatPageContinue_ctor;
            // On.Menu.SlugcatSelectMenu.SlugcatPageContinue.Update += Menu_SlugcatSelectMenu_SlugcatPageContinue_Update;
            IL.ProcessManager.CreateValidationLabel += ProcessManager_CreateValidationLabel;


            new Hook(
            typeof(RedsIllness).GetProperty(nameof(RedsIllness.FoodToBeOkay), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            RedsIllness_FoodToBeOkay
            );

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
            



        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            base.Logger.LogError(ex);
        }

    }



    internal void LoadResources(RainWorld rainWorld)
    {
        try
        {
            MachineConnector.SetRegisteredOI("PebblesSlug_by_syhnne", this.option);
            
            bool isInit = this.IsInit;
            if (!isInit)
            {
                this.IsInit = true;
                Futile.atlasManager.LoadAtlas("atlases/fp_head");
                Futile.atlasManager.LoadAtlas("atlases/fp_tail");
                Futile.atlasManager.LoadAtlas("atlases/fp_arm");
            }
        }
        catch (Exception ex)
        {
            base.Logger.LogError(ex);
            throw;
        }
    }

    public static void Log(params object[] text)
    {
        if (ShowLogs)
        {
            string log = "";
            foreach (object s in text) 
            { 
                log += s.ToString(); 
            }
            Debug.Log("[PebblesSlug] " + log);
        }
            
    }


    #region 图形
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 因为根本不会C#所以把图形和技能全写一起了




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


























    #region 香菇病，雨循环倒计时，饱食度
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 从雨循环15开始，每隔5个雨循环涨一点休眠饱食度，再加上逐渐恶化的营养不良效果（
    // 我的建议是前7个雨循环内跑完剧情，亲测挂上了香菇病之后得至少两只秃鹫才能雨眠，而且最高矛伤会掉到1.2
    // 写真结局的时候还得给这一堆东西加判定。呃啊。。
















    internal delegate float orig_SlowFadeIn(SaveState self);
    internal float SaveState_SlowFadeIn(orig_SlowFadeIn orig, SaveState self)
    {
        var result = orig(self);
        if (self.saveStateNumber.value == SlugcatName)
        {
            result = Mathf.Max(self.malnourished ? 4f : 0.8f, (self.cycleNumber >= RedsIllness.RedsCycles(self.redExtraCycles) && !Custom.rainWorld.ExpeditionMode) ? Custom.LerpMap((float)self.cycleNumber, (float)RedsIllness.RedsCycles(false), (float)(RedsIllness.RedsCycles(false) + 5), 4f, 15f) : 0.8f);
        }
        return result;
    }




    // 从珍珠猫代码里抄的，总之这么写能跑，那就这么写吧（
    internal delegate float orig_RedsIllnessFoodFac(RedsIllness self);
    internal float RedsIllness_FoodFac(orig_RedsIllnessFoodFac orig, RedsIllness self)
    {
        var result = orig(self);
        if (self.player.slugcatStats.name.value == SlugcatName)
        {
            result = Mathf.Max(0.25f, 1f / (Mathf.Ceil(self.cycle * 0.25f) + 1f));
            Log("RedsIllness_FoodFac" + result.ToString());
        }
        return result;
    }



    // 这个好像不好使
    internal delegate int orig_RedsIllnessFoodToBeOkay(RedsIllness self);
    internal int RedsIllness_FoodToBeOkay(orig_RedsIllnessFoodToBeOkay orig, RedsIllness self)
    {
        
        var result = orig(self);
        if (self.player.slugcatStats.name.value == SlugcatName)
        {
            result = self.player.slugcatStats.foodToHibernate;
            Log("RedsIllness_FoodToBeOkay" + result.ToString());
        }
        return result;
    }




    internal delegate float orig_RedsIllnessTimeFactor(RedsIllness self);
    internal float RedsIllness_TimeFactor(orig_RedsIllnessTimeFactor orig, RedsIllness self)
    {
        var result = orig(self);
        if (self.player.slugcatStats.name.value == SlugcatName)
        {
            result = 1f - 0.9f * Mathf.Max(Mathf.Max(self.fadeOutSlow ? Mathf.Pow(Mathf.InverseLerp(0f, 0.5f, self.player.abstractCreature.world.game.manager.fadeToBlack), 0.65f) : 0f, Mathf.InverseLerp(40f * Mathf.Lerp(12f, 21f, self.Severity), 40f, (float)self.counter) * Mathf.Lerp(0.2f, 0.5f, self.Severity)), self.CurrentFitIntensity * 0.1f);
        }
        return result;
    }









    // 修改游戏界面显示的雨循环倒计时以及饱食度
    internal void Menu_SlugcatSelectMenu_SlugcatPageContinue_ctor(ILContext il)
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
            c.EmitDelegate<Func<bool, SlugcatStats.Name, bool>>((isRed, name) =>
            {
                return isRed || name.value == SlugcatName;
            });
        }

        ILCursor c2 = new ILCursor(il);
        // 189 修改食物条显示 不要动这个计算逻辑，他和楼下那个实际计算食物条的东西是完全一样的
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
                    int result = MinFood + (int)Math.Floor((float)cycle / Cycles * (MaxFood + 1 - MinFood));
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
    internal void HUD_SubregionTracker_Update(ILContext il)
    {
        Log("HUD_SubregionTracker_Update");
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
            Log("Match successfully! - HUD_SubregionTracker_Update - 164");
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate<Func<bool, Player, bool>>((isRed, player) =>
            {
                return isRed || player.slugcatStats.name.value == SlugcatName;
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
                if (player.slugcatStats.name.value == SlugcatName)
                {
                    return Cycles;
                }
                return RedsCycles;
            });
        }
    }



    // 不知道这个是干嘛的，但既然搜索搜出来了就改一下罢
    internal void HUD_Map_CycleLabel_UpdateCycleText(ILContext il)
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
                return isRed || player.slugcatStats.name.value == SlugcatName;
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
                if (player.slugcatStats.name.value == SlugcatName)
                {
                    return Cycles;
                }
                return redsCycles;
            });
        }
    }





    // 修改速通验证的循环数
    internal void ProcessManager_CreateValidationLabel(ILContext il)
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
            c.EmitDelegate<Func<bool, SlugcatStats.Name, bool>>((isRed, name) =>
            {
                return isRed || name.value == SlugcatName;
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



    #endregion






























    internal void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager processManager)
    {
        orig(self, processManager);
        /*
            if (self.session is StoryGameSession && ModManager.CoopAvailable)
            {
                Log("RainWorldGame_ctor, coop available");
                for (int i = 0; i<self.Players.Count; i++)
                {
                    Log("loop:",i.ToString());
                    if (self.Players[i].realizedCreature is not Player) { Log("not player"); continue; }
                    if ((self.Players[i].realizedCreature as Player).slugcatStats.name != SlugcatStatsName) { Log("not pebbles, next one"); continue; }
                    bool getModule = modules.TryGetValue((self.Players[i].realizedCreature as Player), out var module) && module.playerName == SlugcatStatsName;
                    if (getModule) { module.canControlGravity = true; Log("coop enabled, player who can control gravity:", i.ToString()); }
                    break;
                }
            }
        */
    }


    // 实际修改饱食度的函数
    internal IntVector2 SlugcatStats_SlugcatFoodMeter(On.SlugcatStats.orig_SlugcatFoodMeter orig, SlugcatStats.Name slugcat)
    {
        return (slugcat.value == SlugcatName) ? new IntVector2(MaxFood, MinFoodNow) : orig(slugcat);
    }





    // 随着游戏进度修改游戏内饱食度，不出意外的话，打真结局后就不会再改了
    internal void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);

        // 防止很多个人同时控制很多个重力？其实这玩意儿不应该写在这里。但是我懒得想了，
        /*
            if (!ModManager.CoopAvailable && self.slugcatStats.name == SlugcatStatsName)
            {
                bool getModule = modules.TryGetValue(self, out var module) && module.playerName == SlugcatStatsName;
                if (getModule) { module.canControlGravity = true; }
                Log("coop disabled, can control gravity");
            }
        */

        if (world.game.session is StoryGameSession && world.game.GetStorySession.characterStats.name.value == SlugcatName && self.slugcatStats.name.value == SlugcatName &&  !self.playerState.isGhost)
        {
            modules.Add(self, new PlayerModule(self));

            int cycle = (world.game.session as StoryGameSession).saveState.cycleNumber;
            bool altEnding = (world.game.session as StoryGameSession).saveState.deathPersistentSaveData.altEnding;
            Log(cycle.ToString(),"<=cycle  altEnding=>", altEnding.ToString());
            if (cycle > 5 && !altEnding)
            {
                self.redsIllness = new RedsIllness(self, cycle - 5);
            }

            // 这下应该没问题了。这可是我精心计算的函数，经验证刚好在6 11 16涨饱食度
            int result = MinFood + (int)Math.Floor((float)cycle / Cycles * (MaxFood + 1 - MinFood));
            if (!altEnding) MinFoodNow = (result < MaxFood) ? result : MaxFood;

            // 以防挨饿之后他覆盖挨饿的饱食度
            if (self.slugcatStats.foodToHibernate != self.slugcatStats.maxFood)
            {
                self.slugcatStats.foodToHibernate = (MinFoodNow < MaxFood) ? MinFoodNow : MaxFood;
                self.slugcatStats.maxFood = MaxFood;
            }


        }

        
    }




















    
    // 各种update
    // 小心蛞蝓猫钻管道的时候self.room会变成null。。
    internal void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        bool getModule = modules.TryGetValue(self, out var module) && module.playerName == SlugcatStatsName;

        if (getModule) module.Update(self, eu);

        if (getModule && self.slugcatStats.name == SlugcatStatsName && self.room != null && self.room.game.session is StoryGameSession)
        {
            module.gravityController?.Update(eu);
        }
        orig(self, eu);
        self.redsIllness?.Update();

        
    }

















    #region 玩家技能
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    


    // 重力控制：单独绑了个按键。这个功能刚玩会觉得很鸡肋，但我试了试，低重力让我能够飞行当中一矛命中蜥蜴身体，当场开饭。高重力让我随手召唤秃鹫，单矛随便杀。
    // 我开始理解fp为什么会说自己是神了。
    // 以后还是把他改成，解锁结局后才能全局操控重力吧。现在我先不改，太好玩了，我先玩
    // 特么的，出大问题。。
    internal class GravityController : UpdatableAndDeletable
    {
        public Player owner;
        public Player[] availablePlayers;
        public int gravityControlCounter = 0;
        public int gravityBonus = 10;
        public int gravityControlTime = 12;
        public float amountZeroG;
        public float amountBrokenZeroG;
        public bool enabled = true;

        internal GravityController(Player owner)
        {
            this.owner = owner;
        }


        // 见过屎山代码吗，如果你没见过，现在你见过了
        public override void Update(bool eu)
        {
            // 这个房间我搞不定
            if (!enabled || owner.room.abstractRoom.name == "SS_E08") return;
            if (!owner.room.abstractRoom.name.StartsWith("SS") && !PebblesSlugOption.GravityControlOutside.Value) return;

            base.Update(eu);
            // 这就是我不懂了，他这儿的effect amount还不是真正的重力，他加了个插值，他为什么要加，我真是一点也想不明白，这除了导致我修三个小时bug以外还有什么别的用处吗
            // 但是他这重力效果和室内灯光还是绑定的，我既不能访问这个AntiGravity的实例，又不能直接把它删了，我真的谢
            if (owner.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.ZeroG) != null)
            {
                if (gravityBonus != (int)Mathf.Round((1f - Mathf.InverseLerp(0f, 0.85f, 1f - owner.room.gravity)) * 10f))
                {
                    Log("gravity mismatch IN ZEROG AREA");
                    Log("-- room gravity: ", owner.room.gravity.ToString());
                    gravityBonus = (int)Mathf.Round((1f - Mathf.InverseLerp(0f, 0.85f, 1f - owner.room.gravity)) * 10f);
                }
            }
            else if (owner.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG) != null)
            {
                if (gravityBonus != (int)Mathf.Round((1f - Mathf.InverseLerp(0f, 0.85f, 1f - owner.room.gravity)) * 10f)
                && gravityBonus != (int)Mathf.Round(10f * 1f - owner.room.gravity))
                {
                    Log("gravity mismatch IN BROKEN ZEROG AREA");
                    Log("-- room gravity: ", owner.room.gravity.ToString());
                    gravityBonus = (int)Mathf.Round(10f * owner.room.gravity);
                }
            }
            else if (gravityBonus != (int)Mathf.Round(10f * owner.room.gravity))
            {
                Log("gravity mismatch or coop player changing gravity: ");
                Log("-- room gravity: ", owner.room.gravity.ToString());
                gravityBonus = (int)Mathf.Round(10f * owner.room.gravity);
            }

            if (Input.GetKey(instance.option.GravityControlKey.Value))
            {
                owner.Blink(5);
            }

            if (owner.Consious && !owner.dead && owner.stun == 0
                && owner.input[0].y != 0 && Input.GetKey(instance.option.GravityControlKey.Value)
                && owner.bodyMode != Player.BodyModeIndex.CorridorClimb && owner.animation != Player.AnimationIndex.HangFromBeam && owner.animation != Player.AnimationIndex.ClimbOnBeam && owner.bodyMode != Player.BodyModeIndex.WallClimb && owner.animation != Player.AnimationIndex.AntlerClimb && owner.animation != Player.AnimationIndex.VineGrab && owner.animation != Player.AnimationIndex.ZeroGPoleGrab && owner.onBack == null)
            {
                gravityControlCounter++;
                if (gravityControlCounter >= gravityControlTime)
                {
                    gravityBonus += owner.input[0].y;
                    owner.input[0].y = 0;
                    if (gravityBonus >= 0)
                    {

                        if (owner.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.ZeroG) != null || owner.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG) != null)
                        {
                            Log("HAS GRAVITY EFFECT");
                            // 如果有类似效果，由于我猜这两效果不能大于1，所以还得钳制范围
                            if (gravityBonus <= 10)
                            {
                                owner.room.gravity = 1f - Mathf.Lerp(0f, 0.85f, 1f - gravityBonus * 0.1f);
                                // 找到并修改zeroG这个效果。roomeffects竟然没有一个能让我直接找到对应效果的函数，还得我自己写for循环……
                                for (int i = 0; i < owner.room.roomSettings.effects.Count; i++)
                                {
                                    if (owner.room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.ZeroG || owner.room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.BrokenZeroG)
                                    {
                                        owner.room.roomSettings.effects[i].amount = 0.1f * (10 - gravityBonus);
                                        Log("zeroG amount: ", owner.room.roomSettings.effects[i].amount);
                                    }
                                }
                                
                            }
                            else { gravityBonus = 10; }
                        }

                        else
                        {
                            owner.room.gravity = 0.1f * gravityBonus;
                        }

                    }
                    else { gravityBonus = 0; }

                    Log("player gravity control RESULT" + owner.room.gravity);
                    gravityControlCounter = 0;
                }
            }
        }


        public void NewRoom()
        {
            if (!enabled)
            {
                if (owner.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.ZeroG) != null || owner.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG) != null)
                {
                    gravityBonus = (int)Mathf.Round((1f - Mathf.InverseLerp(0f, 0.85f, 1f - owner.room.gravity)) * 10f);
                }
                else { gravityBonus = (int)Mathf.Round(10f * owner.room.gravity); }
                return;
            }
            if (owner.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.ZeroG) != null || owner.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG) != null)
            {
                if (gravityBonus <= 10)
                {
                    owner.room.gravity = 0.1f * gravityBonus;
                    // 找到并修改zeroG这个效果
                    bool z = false;
                    bool b = false;
                    for (int i = 0; i < owner.room.roomSettings.effects.Count; i++)
                    {
                        if (owner.room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.ZeroG)
                        {
                            amountZeroG = owner.room.roomSettings.effects[i].amount;
                            owner.room.roomSettings.effects[i].amount = 0.1f * (10 - gravityBonus);
                            Log("room effect set - newroom - z, amount:",amountZeroG," -to- ", owner.room.roomSettings.effects[i].amount);
                            z = true;
                            break;
                        }
                        else if (owner.room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.BrokenZeroG)
                        {
                            amountBrokenZeroG = owner.room.roomSettings.effects[i].amount;
                            owner.room.roomSettings.effects[i].amount = 0.1f * (10 - gravityBonus);
                            Log("room effect set - newroom - b, amount:", amountBrokenZeroG, " -to- ", owner.room.roomSettings.effects[i].amount);
                            b = true;
                            break;
                        }
                    }
                    if (!z) amountZeroG = 0f;
                    if (!b) amountBrokenZeroG = 0f;
                    Log("NewRoom ! z,b: ", amountZeroG.ToString(), amountBrokenZeroG.ToString());
                }
                else
                {
                    Log("gravityBonus out of range, cleared");
                    gravityBonus = (int)Mathf.Round(10f * owner.room.gravity);
                }
                
            }
            else
            {
                owner.room.gravity = gravityBonus * 0.1f;
            }
        }



        public void Die()
        {
            if (owner.room == null) return;
            if (owner.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.ZeroG) != null || owner.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG) != null)
            {
                for (int i = 0; i < owner.room.roomSettings.effects.Count; i++)
                {
                    if (owner.room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.ZeroG)
                    {
                        owner.room.roomSettings.effects[i].amount = amountZeroG;
                        Log("room effect set to original value because player died - ZeroG ", owner.room.roomSettings.effects[i].amount);
                        break;
                    }
                    else if (owner.room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.BrokenZeroG)
                    {
                        owner.room.roomSettings.effects[i].amount = amountBrokenZeroG;
                        Log("room effect set to original value because player died - BrokenZeroG ", owner.room.roomSettings.effects[i].amount);
                        break;
                    }
                }
            }
            else
            {
                owner.room.gravity = 1f;
                gravityBonus = 10;
                Log("gravity set to 1.0 because player died");
            }
            
        }


        public override void Destroy()
        {
            base.Destroy();
        }




    }























    // 垃圾回收
    internal void Player_Destroy(On.Player.orig_Destroy orig, Player self)
    {
        bool getModule = modules.TryGetValue(self, out var module) && module.playerName == SlugcatStatsName;
        if (getModule)
        {
            module.gravityController?.Destroy();
        }
        orig(self);
    }




    // 防止你那倒霉的联机队友在你死了之后顶着3倍重力艰难行走。我知道队友有可能也会控制重力，但是我懒得加判断
    internal void Player_Die(On.Player.orig_Die orig, Player self)
    {
        bool getModule = modules.TryGetValue(self, out var module) && module.playerName == SlugcatStatsName;
        if (getModule && self.slugcatStats.name == SlugcatStatsName)
        {
            module.gravityController.Die();
        }
        orig(self);

    }





    // 房间发生变化时保留重力变化
    internal void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
    {
        orig(self, newRoom);
        bool getModule = modules.TryGetValue(self, out var module) && module.playerName == SlugcatStatsName;
        if (getModule && self.slugcatStats.name == SlugcatStatsName)
        {
            module.gravityController.NewRoom();
        }
    }








    // 不能吃神经元。我想这个应该用不着调用原版方法了吧。。
    internal bool Player_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
    {
        if (self.slugcatStats.name.value == SlugcatName)
        {
            return (!ModManager.MSC || !(self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear)) && (testObj is Rock || testObj is DataPearl || testObj is FlareBomb || testObj is Lantern || testObj is FirecrackerPlant || (testObj is VultureGrub && !(testObj as VultureGrub).dead) || (testObj is Hazer && !(testObj as Hazer).dead && !(testObj as Hazer).hasSprayed) || testObj is FlyLure || testObj is ScavengerBomb || testObj is PuffBall || testObj is SporePlant || testObj is BubbleGrass || testObj is OracleSwarmer || testObj is NSHSwarmer || testObj is OverseerCarcass || (ModManager.MSC && testObj is FireEgg && self.FoodInStomach >= self.MaxFoodInStomach) || (ModManager.MSC && testObj is SingularityBomb && !(testObj as SingularityBomb).activateSingularity && !(testObj as SingularityBomb).activateSucktion));
        }
        else { return orig(self, testObj); }
    }




    internal void Player_Jump(On.Player.orig_Jump orig, Player self)
    {
        orig(self);
        if (self.slugcatStats.name.value == SlugcatName)
        { 
            self.jumpBoost *= 1.2f;
        }
    }



    // 一矛超人，只要不使用二段跳，就是常驻2倍矛伤。使用二段跳会导致这个伤害发生衰减，最低不低于0.5。修改slugbase的基础矛伤可以使所有的值发生变化
    internal void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
    {
        orig(self, spear);
        if (self.slugcatStats.name.value == SlugcatName)
        {
            float spearDmgBonus = 1.5f;
            if (self.pyroJumpCounter > 0)
            {
                spearDmgBonus /= self.pyroJumpCounter;
            }
            spear.spearDamageBonus *= (0.5f + spearDmgBonus);
            Log("spearDmgBonus: " + (0.5f+spearDmgBonus) + "  result: " + spear.spearDamageBonus);
        }
        
    }



    



    // 除了特效以外，数值跟炸猫差不多，因为我不知道那堆二段跳的数值怎么改。我想改得小一点，让他没有那么强的机动性，不然太超模了（
    // 因为这个电击在水下是有伤害的（痛击你的队友。jpg）我不是故意的，我是真的写不出来那个判定。我不知道他为什么会闪退。。
    // 我大概应该用原版方法，然后做ilhooking。但是，说真的，想想那个工作量吧（汗）我都不太清楚自己究竟改了些什么
    internal void Player_ClassMechanicsArtificer(On.Player.orig_ClassMechanicsArtificer orig, Player self)
    {
        // Log("slugcat name: "+ self.slugcatStats.name.value);
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
                // self.room.PlaySound(SoundID.Flare_Bomb_Burn, pos2);
                // self.room.PlaySound(SoundID.Zapper_Zap, pos2, 1f, 0.2f + 0.25f * Random.value);
                self.room.PlaySound(SoundID.Zapper_Zap, pos2, 1f, 1f + 0.25f * Random.value);
                // self.room.PlaySound(SoundID.Fire_Spear_Explode, pos2, 0.3f + Random.value * 0.3f, 0.5f + Random.value * 2f);
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






    internal AbstractPhysicalObject.AbstractObjectType Player_CraftingResults(On.Player.orig_CraftingResults orig, Player self)
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
                if (grasps[0] != null && grasps[0].grabbed is Spear)
                {
                    AbstractPhysicalObject spear = self.grasps[0].grabbed.abstractPhysicalObject;

                    if (!((spear as AbstractSpear).electric && (spear as AbstractSpear).electricCharge > 0))
                    {
                        return AbstractPhysicalObject.AbstractObjectType.Spear;
                    }

                }
                if (grasps[0] == null && grasps[1] != null && grasps[1].grabbed is Spear && self.objectInStomach == null)
                {
                    AbstractPhysicalObject spear = self.grasps[1].grabbed.abstractPhysicalObject;
                    if (!((spear as AbstractSpear).electric && (spear as AbstractSpear).electricCharge > 0))
                    {
                        return AbstractPhysicalObject.AbstractObjectType.Spear;
                    }
                }

            }
            return null;
        }
        else { return orig(self); }
    }






    internal bool Player_GraspsCanBeCrafted(On.Player.orig_GraspsCanBeCrafted orig, Player self)
    {

        if (self.slugcatStats.name.value == SlugcatName && (self.CraftingResults() != null))
        {
            return true;
        }
        return orig(self);
    }






    // 下面这个暂时没用，但先留着以防我日后突然想给他加点别的能力
    internal void Player_SwallowObject(On.Player.orig_SwallowObject orig, Player self, int grasp)
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
            if (ModManager.MMF && self.room.game.session is StoryGameSession)
            {
                (self.room.game.session as StoryGameSession).RemovePersistentTracker(self.objectInStomach);
            }
            self.ReleaseGrasp(grasp);
            self.objectInStomach.realizedObject.RemoveFromRoom();
            self.objectInStomach.Abstractize(self.abstractCreature.pos);
            self.objectInStomach.Room.RemoveEntity(self.objectInStomach);
            Debug.Log("==== swallowobject");
            if (self.FoodInStomach > 0)
            {
                if (abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.Spear && !(abstractPhysicalObject as AbstractSpear).explosive && !(abstractPhysicalObject as AbstractSpear).electric)
                {
                    // 这应该是生成矛的代码。那么它为什么不起作用呢（恼
                    Debug.Log("==== swallowobject: holding spear but why isn't this working");
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








    internal void Player_SpitUpCraftedObject(ILContext il)
    {
        ILCursor c = new(il);
        // 37 劫持炸矛判定，在此判定电矛。所以有没有人告诉我match到底该怎么写，我不想写这么大一坨，很累的
        if (c.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Call),
            (i) => i.Match(OpCodes.Brfalse),
            (i) => i.MatchLdloc(2),
            (i) => i.Match(OpCodes.Isinst),
            (i) => i.Match(OpCodes.Ldfld)
            ))
        {
            Log("Match successfully! - Player_SpitUpCraftedObject");
        }
    }







    internal void Player_SpitUpCraftedObject_old(On.Player.orig_SpitUpCraftedObject orig, Player self)
    {
        if (self.slugcatStats.name.value == SlugcatName)
        {
            // 表示玩家在房间中位置的Vector2（二维向量）实例
            var vector = self.mainBodyChunk.pos;

            // 我要写一个craftingtutorial，并且单独绑一个变量，因为它内容跟原来的不一样
            self.room.PlaySound(SoundID.Slugcat_Swallow_Item, self.mainBodyChunk);

            for (int i = 0; i < self.grasps.Length; i++)
            {
                if (self.grasps[i] != null)
                {
                    AbstractPhysicalObject abstractPhysicalObject = self.grasps[i].grabbed.abstractPhysicalObject;
                    // 这应该是具体的生成规则，我不知道这里有没有bug。。他完全没说把什么矛合成什么矛，这些东西都是分开的，放在不同的函数里的。
                    // 错误的，他做炸矛的时候压根没调用这个函数，给我cpu干烧了。他是哪一步做出来的？？
                    if (abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.Spear && !((abstractPhysicalObject as AbstractSpear).electric && (abstractPhysicalObject as AbstractSpear).electricCharge > 0))
                    {
                        if ((abstractPhysicalObject as AbstractSpear).explosive)
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
                        else
                        {
                            self.ReleaseGrasp(i);
                            abstractPhysicalObject.realizedObject.RemoveFromRoom();
                            self.room.abstractRoom.RemoveEntity(abstractPhysicalObject);
                            // 对了，生成矛是这个。。我可能是那个眼瞎。。
                            self.SubtractFood(2);
                            AbstractSpear abstractSpear = new AbstractSpear(self.room.world, null, self.abstractCreature.pos, self.room.game.GetNewID(), false, true);
                            self.room.abstractRoom.AddEntity(abstractSpear);
                            abstractSpear.RealizeInRoom();
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








    // 啊！！终于把原来那一坨复制粘贴删了！！感觉像新年的第一天穿上新内裤一样清爽啊！！
    internal void Player_GrabUpdate(ILContext il)
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
                if (self.slugcatStats.name.value == SlugcatName)
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
                if (self.slugcatStats.name.value == SlugcatName)
                {
                    return true;
                }
                else { return isArtificer; }
                    
            });
        }
        
    }












    // 被电不仅不会死，还会吃饱（？
    internal void ZapCoil_Update(ILContext il)
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
                    (physicalObj as Player).Stun(200);
                    if (PebblesSlugOption.AddFoodOnShock.Value)
                    {
                        int maxfood = (physicalObj as Player).MaxFoodInStomach;
                        int food = (physicalObj as Player).FoodInStomach;
                        Log("food:" + food + " maxfood: " + maxfood);
                        (physicalObj as Player).AddFood(maxfood - food);
                    }
                    return null;
                }
                else { return physicalObj; }
            });
        }
    }







    // 同理，现在可以免疫蜈蚣的电击，甚至吃上一顿
    internal void Centipede_Shock(ILContext il)
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
            Log("Match successfully! - CentipedeShock");
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<float, PhysicalObject, float>>((centipedeMass, physicalObj) =>
            {
                Log("Match successfully! - CentipedeShock, centipede mass: "+ centipedeMass);
                if (physicalObj is Player && (physicalObj as Player).slugcatStats.name.value == SlugcatName) 
                {
                    if (PebblesSlugOption.AddFoodOnShock.Value && (physicalObj as Player).FoodInStomach < (physicalObj as Player).MaxFoodInStomach)
                    {
                        (physicalObj as Player).AddFood(1);
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
    internal void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        if (self is Player && (self as Player).slugcatStats.name.value == SlugcatName)
        {
            if (type == Creature.DamageType.Electric)
            {
                damage = 0.1f * damage;
                stunBonus = 0.1f * stunBonus;
            }
        }
        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
    }
    #endregion








}











