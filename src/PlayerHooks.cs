﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using System.Drawing.Text;
using RWCustom;
using Noise;
using MoreSlugcats;
using BepInEx.Logging;
using Smoke;
using Random = UnityEngine.Random;
// using ImprovedInput;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Menu.Remix.MixedUI;
using System.ComponentModel;
using SlugBase.DataTypes;
using MonoMod.RuntimeDetour;
using System.Reflection;
using SlugBase.SaveData;
using SlugBase;
using System.Runtime.InteropServices;














namespace PebblesSlug;


// 复制的机猫代码
// 我没学过C#根本不知道，原来还可以自己在类里面加新的东西啊……这就方便多了
internal class PlayerHooks
{



    internal static void Apply()
    {
        // On.RainWorld.Update += RainWorld_Update;
        On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
        On.Player.MovementUpdate += Player_MovementUpdate;
        // On.RoomCamera.FireUpSinglePlayerHUD += RoomCamera_FireUpSinglePlayerHUD;
    }




    public class PlayerModule
    {
        public readonly WeakReference<Player> playerRef;
        internal readonly SlugcatStats.Name playerName;
        internal readonly SlugcatStats.Name storyName;
        internal readonly bool isPebbles;

        public bool lockInput = false;

        internal SSOracleConsole console;
        internal GravityController gravityController;


        public PlayerModule(Player player)
        {
            playerRef = new WeakReference<Player>(player);
            playerName = player.slugcatStats.name;
            if (player.room.game.session is StoryGameSession)
            {
                storyName = player.room.game.GetStorySession.saveStateNumber;
            }
            else { storyName = null; }
            isPebbles = playerName == Plugin.SlugcatStatsName && storyName == Plugin.SlugcatStatsName;


            if (playerName == Plugin.SlugcatStatsName && storyName != null)
            {
                gravityController = new GravityController(player);
            }
            if (isPebbles)
            {
                Plugin.Log("playermodule!");
                
                // oracle = new Oracle(new AbstractPhysicalObject(player.room.world, AbstractPhysicalObject.AbstractObjectType.Oracle, null, new WorldCoordinate(player.room.abstractRoom.index, 15, 15, -1), player.room.game.GetNewID()), player.room);
                // player.room.AddObject(oracle);
                // oracleGraphics = new OracleGraphics(player);
            }
                
        }










        public void Update(Player player, bool eu)
        {
            if (console != null && player.room.abstractRoom.name == "SS_AI"
                && Input.GetKeyDown(Plugin.instance.option.fpConsoleKey.Value))
            {
                console.isActive = !console.isActive;
                // 没想好怎么让他移动，先不关这个了
                // lockInput = console.isActive;
                
                Plugin.Log("toggle console active: ", console.isActive);
            }

        }

    }







    // 启用控制台时阻止玩家输入
    private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
    {
        bool getModule = Plugin.playerModules.TryGetValue(self, out var module) && module.playerName == Plugin.SlugcatStatsName;
        if (getModule && module.lockInput)
        {
            self.input[0] = new Player.InputPackage();
        }
        orig(self, eu);
    }









    // 各种hud
    private static void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
    {
        orig(self, cam);
        if ((self.owner as Player).slugcatStats.name == Plugin.SlugcatStatsName)
        {
            bool getModule = Plugin.playerModules.TryGetValue((self.owner as Player), out var module) && module.playerName == Plugin.SlugcatStatsName;
            if (getModule)
            {
                Plugin.Log("HUD add part");
                self.AddPart(new GravityMeter(self, self.fContainers[1], module.gravityController));
            }
            // 没办法了，这是一种让我在SS_AI房间加载出来的时候也能访问到hud的阴招
            Plugin.instance.Hud = self;

        }

    }











}
