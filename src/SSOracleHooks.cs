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





namespace PebblesSlug;




internal class SSOracleHooks
{


    internal static void Apply()
    {

        On.SSOracleBehavior.ctor += SSOracleBehavior_ctor;
        // On.Oracle.ctor += Oracle_ctor;
        // IL.Oracle.ctor += IL_Oracle_ctor;
        // On.OracleGraphics.ctor += OracleGraphics_ctor;

        new Hook(
            typeof(Oracle).GetProperty(nameof(Oracle.Consious), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            Oracle_Consious
            );
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




    private static void Oracle_ctor(On.Oracle.orig_ctor orig, Oracle self, AbstractPhysicalObject abstractPhysicalObject, Room room) 
    {
        orig(self, abstractPhysicalObject, room);
        if (self.room.game.session is StoryGameSession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            if (self.room.abstractRoom.name.StartsWith("SS") && self.room.abstractRoom.name != "SS_AI")
            {
                self.ID = Plugin.oracleID;
                self.oracleBehavior = new OracleBehavior(self);
            }
        }
        

    }

    // 这个东西不知道为啥，不太好使。要不还是算了。
    private static void IL_Oracle_ctor(ILContext il)
    {
        ILCursor c = new(il);
        if (c.TryGotoNext(MoveType.After,
            i => i.Match(OpCodes.Call),
            i => i.Match(OpCodes.Brtrue_S),
            i => i.Match(OpCodes.Ldsfld),
            i => i.Match(OpCodes.Br_S),
            i => i.Match(OpCodes.Ldsfld)
            // i => i.MatchLdsfld<Oracle.OracleID>("SS")
            ))
        {
            Plugin.Log("match successfully! - IL_Oracle_ctor");
            c.Emit(OpCodes.Ldarg_2);
            c.EmitDelegate<Func<Oracle.OracleID, Room, Oracle.OracleID>>((oracleID, room) =>
            {
                if (room.game.session is StoryGameSession && room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName && room.abstractRoom.name.StartsWith("SS") && room.abstractRoom.name != "SS_AI")
                    return Plugin.oracleID;
                return oracleID;
            });
        }
    }





    private static void OracleGraphics_ctor(On.OracleGraphics.orig_ctor orig, OracleGraphics self, PhysicalObject ow)
    {
        if (ow is Player && (ow as Player).slugcatStats.name == Plugin.SlugcatStatsName)
        {
            Random.State state = Random.state;
            Random.InitState(56);
            self.totalSprites = 0;

            self.halo = new OracleGraphics.Halo(self, self.totalSprites);
            self.totalSprites += self.halo.totalSprites;
            
            Random.state = state;
            return;
        }
        orig(self, ow);
    }











}
