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




internal class SSOracleHooks
{


    internal static void Apply()
    {

        On.SSOracleBehavior.ctor += SSOracleBehavior_ctor;
        On.Oracle.ctor += Oracle_ctor;
        On.SSOracleBehavior.SeePlayer += SSOracleBehavior_SeePlayer;
        On.SSOracleBehavior.Update += SSOracleBehavior_Update;
        On.Oracle.Destroy += Oracle_Destroy;
        IL.SSOracleBehavior.Update += IL_SSOracleBehavior_Update;
        // IL.Oracle.ctor += IL_Oracle_ctor;
        // On.OracleGraphics.ctor += OracleGraphics_ctor;

        new Hook(
            typeof(Oracle).GetProperty(nameof(Oracle.Consious), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            Oracle_Consious
            );
    }



    // 防止fp修改房间重力
    // tmd，这个为啥不生效啊（恼）这比东西也不输出日志，他到底想干嘛
    private static void IL_SSOracleBehavior_Update(ILContext il)
    {
        // 2240 插入label
        ILLabel label = null;
        ILCursor c2 = new ILCursor(il);
        if (c2.TryGotoNext(MoveType.After,
            i => i.MatchLdfld<SSOracleBehavior>("currSubBehavior"),
            i => i.Match(OpCodes.Callvirt),
            i => i.MatchStfld<Room>("gravity")
            ))
        {
            label = c2.MarkLabel();
        }
        else
        {
            Plugin.Log("!!! IL_SSOracleBehavior_Update not found !!!");
        }

        // 2241，执行判断
        ILCursor c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            i => i.MatchLdfld<SSOracleBehavior>("currSubBehavior"),
            i => i.Match(OpCodes.Callvirt),
            i => i.MatchStfld<Room>("gravity"),
            i => i.Match(OpCodes.Ret)
            ) && label != null)
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<SSOracleBehavior, bool>>((self) =>
            {
                bool getModule = Plugin.oracleModules.TryGetValue(self.oracle, out var module) && module.ownerSlugcatName == Plugin.SlugcatStatsName;
                if (getModule && module.console != null && module.console.isActive)
                {
                    Plugin.Log("return");
                    return true;
                }
                return false;
            });
            c.Emit(OpCodes.Brtrue, label);
        }

    }




    private static void SSOracleBehavior_Update(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
    {
        orig(self, eu);
        bool getModule = Plugin.oracleModules.TryGetValue(self.oracle, out var module) && module.ownerSlugcatName == Plugin.SlugcatStatsName;
        if (getModule) 
        {
            if (module.console != null && module.console.isActive)
            {
                module.console.Update(eu);
            }
            else if (module.console != null)
            {
                self.movementBehavior = SSOracleBehavior.MovementBehavior.Meditate;
            }

            
            // 没辙了，我要使用一个非常烂的办法
            if (module.console.player != null)
            {
                bool getModulep = Plugin.playerModules.TryGetValue(module.console.player, out var modulep) && modulep.playerName == Plugin.SlugcatStatsName;
                if (getModulep && modulep.gravityController.isAbleToUse)
                {
                    self.getToWorking = 1 - modulep.gravityController.gravityBonus * 0.1f;
                }
            }
            
        }
        
    }





    // 
    private static void SSOracleBehavior_SeePlayer(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
    {
        bool getModule = Plugin.oracleModules.TryGetValue(self.oracle, out var module) && module.ownerSlugcatName == Plugin.SlugcatStatsName;
        if (getModule) return;
        else { orig(self); }
    }







    // 这个修改导致整个模组的性质发生了一些微妙的变化
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
        if (!(oracle.room.game.session is StoryGameSession)) return;
        if (oracle.room.game.GetStorySession.saveState.saveStateNumber != Plugin.SlugcatStatsName) return;
        


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
                module.console = null;
            }
        }
    }









}
