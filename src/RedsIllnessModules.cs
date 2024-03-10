using MonoMod.Cil;
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

// 从雨循环15开始，每隔5个雨循环涨一点休眠饱食度，再加上逐渐恶化的营养不良效果（
// 我的建议是前7个雨循环内跑完剧情，亲测挂上了香菇病之后得至少两只秃鹫才能雨眠，而且最高矛伤会掉到1.2

internal class RedsIllnessModules
{

    public static void Disable()
    {
        IL.Player.EatMeatUpdate -= IL_Player_EatMeatUpdate;
        On.HUD.FoodMeter.SleepUpdate -= HUD_FoodMeter_SleepUpdate;
        On.SlugcatStats.SlugcatFoodMeter -= SlugcatStats_SlugcatFoodMeter;
        IL.HUD.Map.CycleLabel.UpdateCycleText -= IL_HUD_Map_CycleLabel_UpdateCycleText;
        IL.HUD.SubregionTracker.Update -= IL_HUD_SubregionTracker_Update;
        IL.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor -= Menu_SlugcatSelectMenu_SlugcatPageContinue_ctor;
        IL.ProcessManager.CreateValidationLabel -= ProcessManager_CreateValidationLabel;
    }



    public static void Apply()
    {
        try
        {
            IL.Player.EatMeatUpdate += IL_Player_EatMeatUpdate;
            On.HUD.FoodMeter.SleepUpdate += HUD_FoodMeter_SleepUpdate;
            On.SlugcatStats.SlugcatFoodMeter += SlugcatStats_SlugcatFoodMeter;
            IL.HUD.Map.CycleLabel.UpdateCycleText += IL_HUD_Map_CycleLabel_UpdateCycleText;
            IL.HUD.SubregionTracker.Update += IL_HUD_SubregionTracker_Update;
            IL.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += Menu_SlugcatSelectMenu_SlugcatPageContinue_ctor;
            IL.ProcessManager.CreateValidationLabel += ProcessManager_CreateValidationLabel;

            // 这仨有问题，先不挂了，除了让游戏变难以外没影响
            // TODO: 
            /*new Hook(
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
                );*/
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError(ex);
        }
    }





    private static void IL_Player_EatMeatUpdate(ILContext il)
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
    private static float SaveState_SlowFadeIn(orig_SlowFadeIn orig, SaveState self)
    {
        var result = orig(self);
        if (self.saveStateNumber == Plugin.SlugcatStatsName)
        {
            result = Mathf.Max(self.malnourished ? 4f : 0.8f, (self.cycleNumber >= RedsIllness.RedsCycles(self.redExtraCycles) && !self.deathPersistentSaveData.altEnding && !Custom.rainWorld.ExpeditionMode) ? Custom.LerpMap((float)self.cycleNumber, (float)RedsIllness.RedsCycles(false), (float)(RedsIllness.RedsCycles(false) + 5), 4f, 15f) : 0.8f);
        }
        return result;
    }




    // 从珍珠猫代码里抄的，总之这么写能跑，那就这么写吧（
    private delegate float orig_FoodFac(RedsIllness self);
    private static float RedsIllness_FoodFac(orig_FoodFac orig, RedsIllness self)
    {
        var result = orig(self);
        if (self.player.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            result = Mathf.Max(0.2f, 1f / ((float)self.cycle * 0.25f + 2f));
        }
        return result;
    }







    private delegate float orig_TimeFactor(RedsIllness self);
    private static float RedsIllness_TimeFactor(orig_TimeFactor orig, RedsIllness self)
    {
        var result = orig(self);
        if (self.player.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            result = 1f - 0.9f * Mathf.Max(Mathf.Max(self.fadeOutSlow ? Mathf.Pow(Mathf.InverseLerp(0f, 0.5f, self.player.abstractCreature.world.game.manager.fadeToBlack), 0.65f) : 0f, Mathf.InverseLerp(40f * Mathf.Lerp(12f, 21f, self.Severity), 40f, (float)self.counter) * Mathf.Lerp(0.2f, 0.5f, self.Severity)), self.CurrentFitIntensity * 0.1f);
        }
        return result;
    }









    // 修改游戏界面显示的雨循环倒计时以及饱食度
    private static void Menu_SlugcatSelectMenu_SlugcatPageContinue_ctor(ILContext il)
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
                return isRed || (name == Plugin.SlugcatStatsName && !menu.saveGameData.altEnding);
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
                if (name == Plugin.SlugcatStatsName)
                {
                    int cycle = menu.saveGameData.cycle;
                    int result = Plugin.MinFood;
                    if (!menu.saveGameData.altEnding)
                    {
                        result = CycleGetFood(cycle);
                    }
                    return Math.Min(result, Plugin.MaxFood);
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
                if (name == Plugin.SlugcatStatsName)
                {
                    return Plugin.Cycles;
                }
                return redCycles;
            });
        }
    }







    // 修改游戏内显示的雨循环倒计时
    private static void IL_HUD_SubregionTracker_Update(ILContext il)
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
                    return Plugin.Cycles;
                }
                return RedsCycles;
            });
        }
    }






    // 不知道这个是干嘛的，但既然搜索搜出来了就改一下罢
    private static void IL_HUD_Map_CycleLabel_UpdateCycleText(ILContext il)
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
                return player.slugcatStats.name == Plugin.SlugcatStatsName ? Plugin.Cycles : redsCycles;
            });
        }
    }





    // 修改速通验证的循环数
    private static void ProcessManager_CreateValidationLabel(ILContext il)
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
                return isRed || (name == Plugin.SlugcatStatsName && !saveGameData.altEnding);
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
                return name == Plugin.SlugcatStatsName ? Plugin.Cycles : redsCycles;
            });
        }
    }





    // 在雨眠页面上做个食物条移动动画
    private static void HUD_FoodMeter_SleepUpdate(On.HUD.FoodMeter.orig_SleepUpdate orig, HUD.FoodMeter self)
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
                if (CycleGetFood(package.saveState.cycleNumber) == Plugin.MinFood + 1)
                { owner.forceWatchAnimation = true; }
                self.survivalLimit = CycleGetFood(package.saveState.cycleNumber);

            }
        }
        orig(self);
    }






    // 实际修改饱食度的函数
    private static IntVector2 SlugcatStats_SlugcatFoodMeter(On.SlugcatStats.orig_SlugcatFoodMeter orig, SlugcatStats.Name slugcat)
    {
        return (slugcat == Plugin.SlugcatStatsName) ? new IntVector2(Plugin.MaxFood, Plugin.instance.MinFoodNow) : orig(slugcat);
    }






    // 这个东西被用的太多了，写个函数
    public static int CycleGetFood(int cycle)
    {
        int result = Plugin.MinFood + (int)Math.Floor((float)cycle / Plugin.Cycles * (Plugin.MaxFood + 1 - Plugin.MinFood));
        return result;
    }




    

}
