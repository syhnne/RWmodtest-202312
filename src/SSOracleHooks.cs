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





namespace PebblesSlug;




public class SSOracleHooks
{


    public static void Apply()
    {
        // IL.SSOracleBehavior.Update += IL_SSOracleBehavior_Update;
        On.SSOracleBehavior.ctor += SSOracleBehavior_ctor;
        On.Oracle.ctor += Oracle_ctor;
        On.SSOracleBehavior.SeePlayer += SSOracleBehavior_SeePlayer;
        On.SSOracleBehavior.Update += SSOracleBehavior_Update;
        On.Oracle.Destroy += Oracle_Destroy;
        // On.OracleBehavior.UnconciousUpdate += OracleBehavior_UnconciousUpdate;
        On.SSOracleBehavior.UnconciousUpdate += SSOracleBehavior_UnconciousUpdate;
        On.PebblesPearl.Update += PebblesPearl_Update;

        // IL.Oracle.ctor += IL_Oracle_ctor;
        // On.OracleGraphics.ctor += OracleGraphics_ctor;

        new Hook(
            typeof(Oracle).GetProperty(nameof(Oracle.Consious), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            Oracle_Consious
            );

        new Hook(
            typeof(SSOracleBehavior.SubBehavior).GetProperty(nameof(SSOracleBehavior.SubBehavior.LowGravity), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            SSOracleBehavior_SubBehavior_lowGravity
            );

        new Hook(
            typeof(SSOracleBehavior).GetProperty(nameof(SSOracleBehavior.EyesClosed), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            SSOracleBehavior_EyesClosed
            );
    }


    // 这恐怕是唯一修改重力的办法了。。我很怀疑他会不会使得一些性能不太好的电脑掉帧
    private delegate bool orig_EyesClosed(SSOracleBehavior self);
    private static bool SSOracleBehavior_EyesClosed(orig_EyesClosed orig, SSOracleBehavior self)
    {
        var result = orig(self);
        if (self.oracle.room.game != null && self.oracle.room.game.session is StoryGameSession && self.oracle.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            bool getModule = Plugin.oracleModules.TryGetValue(self.oracle, out var module) && module.ownerSlugcatName == Plugin.SlugcatStatsName;
            if (getModule && module.console != null && module.console.player != null)
            {
                result = !module.console.isActive;
            }
        }
        return result;
    }





    // 珍珠会绕着猫转
    // 但是效果没有我想象中那么好（。
    private static void PebblesPearl_Update(On.PebblesPearl.orig_Update orig, PebblesPearl self, bool eu)
    {
        orig(self, eu);
        if (self.hoverPos == null && self.oracle != null && self.oracle.room == self.room && self.oracle.room.game.session is StoryGameSession && self.oracle.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            bool getModule = Plugin.oracleModules.TryGetValue(self.oracle, out var module) && module.ownerSlugcatName == Plugin.SlugcatStatsName;
            if (getModule && module.console != null && module.console.player != null && module.console.player.room == self.room)
            {
                if (!self.oracle.Consious) self.orbitObj = null;
                // else if (module.console.isActive) self.orbitObj = module.console.player;
                // else self.orbitObj = self.oracle;
            }

        }

    }






    // 鉴于我最近挂的ilhook全都莫名其妙地失败了，我准备直接绕过原版方法（谁闲的没事会动这个函数啊。。
    private static void SSOracleBehavior_UnconciousUpdate(On.SSOracleBehavior.orig_UnconciousUpdate orig, SSOracleBehavior self)
    {
        if (self.oracle.room.game != null && self.oracle.room.game.session is StoryGameSession && self.oracle.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName && self.oracle.ID == Oracle.OracleID.SS)
        {
            self.FindPlayer();
            /*for (int i = 0; i < self.oracle.room.game.cameras.Length; i++)
            {
                if (self.oracle.room.game.cameras[i].room == self.oracle.room && !self.oracle.room.game.cameras[i].AboutToSwitchRoom)
                {
                    self.oracle.room.game.cameras[i].ChangeBothPalettes(10, 26, 0.51f + Mathf.Sin(self.unconciousTick * 0.25707963f) * 0.35f);
                }
            }*/

            if (self.oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.DarkenLights) != null)
            {
                self.oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.DarkenLights).amount = Mathf.Lerp(0f, 1f, 0.2f +Mathf.Sin(self.unconciousTick * 0.15f));
            }
            if (self.oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Darkness) != null)
            {
                self.oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Darkness).amount = Mathf.Lerp(0f, 0.4f, 0.2f + Mathf.Sin(self.unconciousTick * 0.15f));
            }
            if (self.oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Contrast) != null)
            {
                self.oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Contrast).amount = Mathf.Lerp(0f, 0.3f, 0.2f + Mathf.Sin(self.unconciousTick * 0.15f));
            }

            self.unconciousTick += 1f;
            self.oracle.setGravity(0.9f);

            bool getModule = Plugin.oracleModules.TryGetValue(self.oracle, out var module) && module.ownerSlugcatName == Plugin.SlugcatStatsName;
            if (getModule && module.console != null && module.console.player != null)
            {
                bool getModulep = Plugin.playerModules.TryGetValue(module.console.player, out var modulep) && modulep.playerName == Plugin.SlugcatStatsName;
                if (getModulep && modulep.gravityController != null)
                {
                    self.oracle.room.gravity = modulep.gravityController.gravityBonus * 0.1f;
                }
            }
        }
        else { orig(self); }
    }









    // 这恐怕是唯一修改重力的办法了。。我很怀疑他会不会使得一些性能不太好的电脑掉帧
    private delegate float orig_lowGravity(SSOracleBehavior.SubBehavior self);
    private static float SSOracleBehavior_SubBehavior_lowGravity(orig_lowGravity orig, SSOracleBehavior.SubBehavior self)
    {
        var result = orig(self);
        if (self.oracle.room.game != null && self.oracle.room.game.session is StoryGameSession && self.oracle.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            bool getModule = Plugin.oracleModules.TryGetValue(self.oracle, out var module) && module.ownerSlugcatName == Plugin.SlugcatStatsName;
            if (getModule && module.console != null && module.console.player != null)
            {
                bool getModulep = Plugin.playerModules.TryGetValue(module.console.player, out var modulep) && modulep.playerName == Plugin.SlugcatStatsName;
                if (getModulep && modulep.gravityController != null)
                {
                    result = modulep.gravityController.gravityBonus * 0.1f;
                }
            }
        }
        return result;
    }






    // 防止fp修改房间重力
    // tmd，这个为啥不生效啊（恼）这比东西也不输出日志，他到底想干嘛
    private static void IL_SSOracleBehavior_Update(ILContext il)
    {
        Plugin.Log("IL_SSOracleBehavior_Update");
        // 2240 插入label
        ILLabel label = null;
        ILCursor c6 = new ILCursor(il);
        if (c6.TryGotoNext(MoveType.After,
            i => i.MatchLdfld<SSOracleBehavior>("currSubBehavior"),
            i => i.Match(OpCodes.Callvirt),
            i => i.MatchStfld<Room>("gravity")
            ))
        {
            label = c6.MarkLabel();
        }
        else
        {
            Plugin.Log("!!! IL_SSOracleBehavior_Update not found !!!");
        }

        // 2241，执行判断
        ILCursor c7 = new ILCursor(il);
        if (c7.TryGotoNext(MoveType.After,
            i => i.MatchLdfld<SSOracleBehavior>("currSubBehavior"),
            i => i.Match(OpCodes.Callvirt),
            i => i.MatchStfld<Room>("gravity"),
            i => i.Match(OpCodes.Ret)
            ) && label != null)
        {
            Plugin.Log("SS_AI gravity");
            c7.Emit(OpCodes.Ldarg_0);
            c7.EmitDelegate<Func<SSOracleBehavior, bool>>((self) =>
            {
                bool getModule = Plugin.oracleModules.TryGetValue(self.oracle, out var module) && module.ownerSlugcatName == Plugin.SlugcatStatsName;
                if (getModule && module.console != null && module.console.isActive)
                {
                    
                    return true;
                }
                return false;
            });
            c7.Emit(OpCodes.Brtrue, label);
        }

    }












    private static void SSOracleBehavior_Update(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
    {
        orig(self, eu);
        bool getModule = Plugin.oracleModules.TryGetValue(self.oracle, out var module) && module.ownerSlugcatName == Plugin.SlugcatStatsName;
        if (getModule) 
        {
            module.console?.Update(eu);
            /*// 没辙了，我要使用一个非常烂的办法
            if (module.console.player != null)
            {
                bool getModulep = Plugin.playerModules.TryGetValue(module.console.player, out var modulep) && modulep.playerName == Plugin.SlugcatStatsName;
                if (getModulep && modulep.gravityController.isAbleToUse)
                {
                    self.getToWorking = 1 - modulep.gravityController.gravityBonus * 0.1f;
                }
            }*/
            
        }
        
    }





    // 让fp无视你的大部分行为（这不包括用武器攻击他
    private static void SSOracleBehavior_SeePlayer(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
    {
        bool getModule = Plugin.oracleModules.TryGetValue(self.oracle, out var module) && module.ownerSlugcatName == Plugin.SlugcatStatsName;
        if (getModule) return;
        else { orig(self); }
    }







    // 婴儿般的睡眠。jpg
    private delegate bool orig_Consious(Oracle self);
    private static bool Oracle_Consious(orig_Consious orig, Oracle self)
    {
        var result = orig(self);
        if (self.room.game.session is StoryGameSession && self.ID == Oracle.OracleID.SS && (self.room.game.session as StoryGameSession).saveState.saveStateNumber == Plugin.SlugcatStatsName && (self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.altEnding != true)
        {
            result = false;
        }
        return result;
    }






    private static void SSOracleBehavior_ctor(On.SSOracleBehavior.orig_ctor orig, SSOracleBehavior self, Oracle oracle)
    {
        orig(self, oracle);
        if (oracle.room.game.session is not StoryGameSession || oracle.room.game.GetStorySession.saveState.saveStateNumber != Plugin.SlugcatStatsName) return;
        self.movementBehavior = SSOracleBehavior.MovementBehavior.Meditate;


    }






    // 在这里挂模组（万物起源，如果有问题就关它
    private static void Oracle_ctor(On.Oracle.orig_ctor orig, Oracle self, AbstractPhysicalObject abstractPhysicalObject, Room room) 
    {
        orig(self, abstractPhysicalObject, room);
        if (self.room.game.session is StoryGameSession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            if (self.ID == Oracle.OracleID.SS)
            {
                Plugin.oracleModules.Add(self, new OracleModule(self));
            }
        }
        

    }





    // 垃圾回收
    private static void Oracle_Destroy(On.Oracle.orig_Destroy orig, Oracle self)
    {
        if (self.ID == Oracle.OracleID.SS) 
        {
            bool getModule = Plugin.oracleModules.TryGetValue(self, out var module) && module.ownerSlugcatName == Plugin.SlugcatStatsName;
            if (getModule)
            {
                module.console.Destroy();
                module.console.hud.ClearSprites();
                module.console.hud = null;
                module.console = null;
            }
        }
    }









}
