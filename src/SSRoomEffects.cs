using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PebblesSlug;



/// <summary>
/// 在打完结局前关闭五卵石内部的任何特效
/// </summary>
/// 
/// 下一作说不定还要用这些代码（
internal class SSRoomEffects


{
    public static void Apply()
    {
        On.GravityDisruptor.Update += GravityDisruptor_Update;
        On.CoralBrain.SSMusicTrigger.Trigger += CoralBrain_SSMusicTrigger_Trigger;
        // On.CoralBrain.CoralNeuronSystem.Update += CoralBrain_CoralNeuronSystem_Update;
        On.ZapCoil.Update += ZapCoil_Update;
        On.ZapCoilLight.Update += ZapCoilLight_Update;
        // On.RoomSettings.LoadEffects += RoomSettings_LoadEffects;
        // On.RoomRealizer.RealizeAndTrackRoom += RoomRealizer_RealizeAndTrackRoom;
        On.AbstractRoom.RealizeRoom += AbstractRoom_RealizeRoom;
        On.SSLightRod.Update += SSLightRod_Update;
        On.Room.Loaded += Room_Loaded;
        // On.SSLightRod.InitiateSprites += SSLightRod_InitiateSprites;

        /*new Hook(
            typeof(CoralBrain.CoralNeuronSystem).GetProperty(nameof(CoralBrain.CoralNeuronSystem.Frozen), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            CoralNeuronSystem_Frozen
            );*/

        new Hook(
            typeof(SSOracleSwarmer.Behavior).GetProperty(nameof(SSOracleSwarmer.Behavior.Dead), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            SSOracleSwarmer_Behavior_Dead
            );


    }












    private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
    {
        if (self.game != null && self.game.IsStorySession && self.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName && !self.game.GetStorySession.saveState.deathPersistentSaveData.altEnding
            && self.roomSettings != null && self.roomSettings.placedObjects.Count > 0)
        {

            for (int i = 0; i < self.roomSettings.placedObjects.Count; i++)
            {
                PlacedObject obj = self.roomSettings.placedObjects[i];
                if (obj.type == PlacedObject.Type.ProjectedStars)
                {
                    obj.active = false;
                }
            }
        }
        orig(self);
    }




    private static void ZapCoilLight_Update(On.ZapCoilLight.orig_Update orig, ZapCoilLight self, bool eu)
    {
        orig(self, eu);
        if (self.room != null && self.room.game.IsStorySession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            self.lightSource.alpha = 0f;
        }
    }





    // 估计是那几个函数写的太晚了，或者由于SS_AI是出生点，调用的不是他
    // 啊？？咋还是不行啊？？
    private static void AbstractRoom_RealizeRoom(On.AbstractRoom.orig_RealizeRoom orig, AbstractRoom self, World world, RainWorldGame game)
    {
        orig(self, world, game);
        if (self.realizedRoom == null) { return; }
        if (game.IsStorySession && game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            RoomSettings settings = self.realizedRoom.roomSettings;

            settings.RemoveEffect(RoomSettings.RoomEffect.Type.SSMusic);
            if (!self.world.game.GetStorySession.saveState.deathPersistentSaveData.altEnding)
            {
                settings.RemoveEffect(RoomSettings.RoomEffect.Type.ProjectedScanLines);
                settings.RemoveEffect(RoomSettings.RoomEffect.Type.SuperStructureProjector);
                // 下一作里把下面这个启用，可以直接让房间不生成神经元
                // settings.RemoveEffect(RoomSettings.RoomEffect.Type.SSSwarmers);


                /*settings.RemoveAmbientSound(AmbientSound.Type.Spot, "SO_IND-Turbine.ogg");
                settings.RemoveAmbientSound(AmbientSound.Type.Spot, "SO_IND-Deep50hz.ogg");*/
                // 擦 原来gravityDisrupter那个巨大的动静是底下这个 怪不得老挪不掉 看名字谁看得出来啊（。
                settings.RemoveAmbientSound(AmbientSound.Type.Spot, "SO_SFX-Escape.ogg");

                foreach (AmbientSound sound in settings.ambientSounds)
                {
                    Plugin.Log("room:", self.name, "ambientSound:", sound.type.ToString(), sound.sample);
                }

            }
        }
    }







    private static void SSLightRod_Update(On.SSLightRod.orig_Update orig, SSLightRod self, bool eu)
    {
        orig(self, eu);
        if (self.room != null && self.room.game.IsStorySession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            self.lights = new List<SSLightRod.LightVessel>();
            self.color = new Color(0.1f, 0.1f, 0.1f);
        }

    }




    // 都没用，算了，不管了
    private delegate bool orig_Dead(SSOracleSwarmer.Behavior self);
    private static bool SSOracleSwarmer_Behavior_Dead(orig_Dead orig, SSOracleSwarmer.Behavior self)
    {
        var result = orig(self);
        if (self.leader.room != null && self.leader.room.game.IsStorySession && self.leader.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName && !self.leader.room.game.GetStorySession.saveState.deathPersistentSaveData.altEnding)
        {
            result = true;
        }
        return result;
    }





    private static void CoralBrain_CoralNeuronSystem_Update(On.CoralBrain.CoralNeuronSystem.orig_Update orig, CoralBrain.CoralNeuronSystem self, bool eu)
    {
        orig(self, eu);
        if (self.room != null && self.room.game.IsStorySession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            // 我怀疑这个有问题，先逝逝
            self.wind = new UnityEngine.Vector2(0,0);
        }
    }



    // 这个挂不得 这会让神经元直接停止update
    private delegate bool orig_Frozen(CoralBrain.CoralNeuronSystem self);
    private static bool CoralNeuronSystem_Frozen(orig_Frozen orig, CoralBrain.CoralNeuronSystem self)
    {
        var result = orig(self);
        if (self.room != null && self.room.game.IsStorySession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName && !self.room.game.GetStorySession.saveState.deathPersistentSaveData.altEnding)
        {
            result = true;
        }
        return result;
    }











    // 怪了，这个ssmusic死活去不掉，只有在这才能去掉，但是这加不了判定（恼
    private static void RoomSettings_LoadEffects(On.RoomSettings.orig_LoadEffects orig, RoomSettings self, string[] s)
    {
        orig(self, s);
        self.RemoveEffect(RoomSettings.RoomEffect.Type.SSMusic);
        
    }





    // TODO: 修好这个东西（我不想修了，反正他卡bug的也就那么几帧，无脑catch完事
    // 关掉！必须要关掉！
    private static void ZapCoil_Update(On.ZapCoil.orig_Update orig, ZapCoil self, bool eu)
    {
        try
        {
            if (self.room.game.IsStorySession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
            {
                self.powered = false;
            }
            orig(self, eu);
        }
        catch
        {
            // base.Logger.LogError(ex);
        }

    }




    private static void GravityDisruptor_Update(On.GravityDisruptor.orig_Update orig, GravityDisruptor self, bool eu)
    {
        orig(self, eu);
        if (self.room != null && self.room.game.IsStorySession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            self.power = 0f;

        }
    }






    // 啊啊啊啊啊啊啊啊啊别放音乐了
    private static void CoralBrain_SSMusicTrigger_Trigger(On.CoralBrain.SSMusicTrigger.orig_Trigger orig, CoralBrain.SSMusicTrigger self)
    {
        if (self.room.game.IsStorySession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            return;
        }
        orig(self);
    }




}
