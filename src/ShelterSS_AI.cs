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
using JollyCoop;





namespace PebblesSlug;




/// <summary>
/// 让fp的演算室变成避难所！！！
/// 我tm到处睡觉。jpg
/// </summary>

/// 我又在搞一些自己搞不定的东西了
/// 有以下几个办法：第一个是从头改，从worldloader到abstractroom到room一路加上shelter属性，最后爆改一个没声音没图像的shelterdoor安在这里
/// 第二个是半路修改一个shelter index出来。危险的地方在于，我不知道这个shelter index在什么地方被调用，而且容易和自定义区域撞车。
/// 第三个是不改以上任何内容，只在player那里动点手脚
/// 我要用第三种了
/// 


public static class ShelterSS_AI
{




    public static void Apply()
    {
        // On.HUD.FoodMeter.MoveSurvivalLimit += HUD_FoodMeter_MoveSurvivalLimit;
    }


    private static void HUD_FoodMeter_GameUpdate(On.HUD.FoodMeter.orig_GameUpdate orig, FoodMeter self)
    {
        orig(self);
    }


    // 为了避免强制睡觉的时候显示消耗的食物增多。。好像会引起别的bug。。
    private static void HUD_FoodMeter_MoveSurvivalLimit(On.HUD.FoodMeter.orig_MoveSurvivalLimit orig,  FoodMeter self, float to, bool smooth) 
    { 
        if (self.hud.owner is Player && (self.hud.owner as Player).room != null && (self.hud.owner as Player).room.game.IsStorySession && (self.hud.owner as Player).room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName && !smooth) 
        {
            // to = Mathf.Min(to, self.survivalLimit);
        }
        orig(self, to, smooth);
    }




    public static void Player_ctor(Player player, AbstractCreature abstractCreature, World world)
    {
        if (world.GetAbstractRoom(abstractCreature.pos.room).name == "SS_AI")
        {
            player.sleepCounter = 100;
            for (int j = 0; j < world.GetAbstractRoom(abstractCreature.pos.room).creatures.Count; j++)
            {
                if (world.GetAbstractRoom(abstractCreature.pos.room).creatures[j].creatureTemplate.type != CreatureTemplate.Type.Slugcat 
                    && (!ModManager.MSC || world.GetAbstractRoom(abstractCreature.pos.room).creatures[j].creatureTemplate.type != MoreSlugcatsEnums.CreatureTemplateType.SlugNPC))
                {
                    player.sleepCounter = 0;
                }
            }
        }
    }





    public static void Player_Update(Player player)
    {
        // Plugin.Log("sleepCounter: ", player.sleepCounter, "touchedNoInputCounter: ", player.touchedNoInputCounter);


        // 删除了msc判断，反正有依赖
        bool pupFood = true;
        for (int i = 0; i < player.room.game.cameras[0].hud.foodMeter.pupBars.Count; i++)
        {
            FoodMeter foodMeter = player.room.game.cameras[0].hud.foodMeter.pupBars[i];
            if (!foodMeter.PupHasDied && foodMeter.abstractPup.Room == player.room.abstractRoom && (foodMeter.PupInDanger || foodMeter.CurrentPupFood < foodMeter.survivalLimit))
            {
                pupFood = false;
                break;
            }
        }

        bool canSleep = false;
        bool canStarve = false;

        if (pupFood && player.FoodInRoom(player.room, false) >= (player.abstractCreature.world.game.GetStorySession.saveState.malnourished ? player.slugcatStats.maxFood : player.slugcatStats.foodToHibernate))
        { canSleep = true; }
        else if ((!pupFood && player.abstractCreature.world.game.GetStorySession.saveState.malnourished && player.FoodInRoom(player.room, false) >= player.MaxFoodInStomach) 
            || (!player.abstractCreature.world.game.GetStorySession.saveState.malnourished && player.FoodInRoom(player.room, false) > 0 && player.FoodInRoom(player.room, false) < player.slugcatStats.foodToHibernate)) // 玩家和猫崽都没饱 || 猫崽没饱
        {  canStarve = true; }


        bool starveForceSleep = false;
        if (player.forceSleepCounter > 260)
        {
            Plugin.Log("forceSleep! pupFood: ", pupFood);
            Plugin.Log("food: ", player.FoodInRoom(player.room, false) >= (player.abstractCreature.world.game.GetStorySession.saveState.malnourished ? player.slugcatStats.maxFood : player.slugcatStats.foodToHibernate));
            Plugin.Log("stillInStartShelter:", player.stillInStartShelter);

            // player.forceSleepCounter = 0;

            if (canSleep)
            {
                Plugin.Log("forceSleep! - readyForWin");
                // 第一种情况：吃饱了睡的
                player.readyForWin = true;
            }
            else if (canStarve)
            {
                Plugin.Log("forceSleep! - starve");
                // 第二种情况：没吃饱睡的
                starveForceSleep = true;
            }
        }
        // 同时按住下键和拾取键才能睡觉
        else if ((canSleep || canStarve) && player.input[0].y < 0 && !player.input[0].jmp && !player.input[0].thrw && !player.input[0].pckp 
            && player.IsTileSolid(1, 0, -1) && (player.input[0].x == 0 || ((!player.IsTileSolid(1, -1, -1) || !player.IsTileSolid(1, 1, -1)) && player.IsTileSolid(1, player.input[0].x, 0))))
        {
            Plugin.Log("force sleep counter: ", player.forceSleepCounter);
            player.forceSleepCounter++;
            player.showKarmaFoodRainTime = 40;
        }
        else
        {
            player.forceSleepCounter = 0;
        }
        


        

        if (player.Stunned)
        {
            player.readyForWin = false;
        }

        if (player.readyForWin)
        {
            Plugin.Log("readyForWin !!!");
            if (ModManager.CoopAvailable)
            {
                player.ReadyForWinJolly = true;
            }
            ShelterDoorClose(player);
        }
        else if (starveForceSleep)
        {
            Plugin.Log("readyForStarve !!!");
            if (ModManager.CoopAvailable)
            {
                player.ReadyForStarveJolly = true;
            }
            player.sleepCounter = -24;
            ShelterDoorClose(player);
        }
    }








    // 纯属复制游戏代码，我想把他改的更阳间一点，但暂时先别改
    public static void ShelterDoorClose(Player self)
    {
        Plugin.Log("ShelterDoorClose");
        if (ModManager.CoopAvailable)
        {
            List<AbstractCreature> playersToProgressOrWin = self.room.game.PlayersToProgressOrWin;
            List<AbstractCreature> list = (from x in self.room.physicalObjects.SelectMany((List<PhysicalObject> x) => x).OfType<Player>()
                                           select x.abstractCreature).ToList();
            bool flag = true;
            bool flag2 = false;
            foreach (AbstractCreature abstractCreature in playersToProgressOrWin)
            {
                if (!list.Contains(abstractCreature))
                {
                    int playerNumber = (abstractCreature.state as PlayerState).playerNumber;
                    flag = false;
                    flag2 = false;
                    if (self.room.BeingViewed)
                    {
                        try
                        {
                            self.room.game.cameras[0].hud.jollyMeter.playerIcons[playerNumber].blinkRed = 20;
                        }
                        catch
                        {
                        }
                    }
                    foreach (AbstractCreature p in list)
                    {
                        Player player = p.realizedCreature as Player;
                        player.forceSleepCounter = 0;
                        player.sleepCounter = 0;
                        player.touchedNoInputCounter = 0;
                    }
                }
                if (!abstractCreature.state.dead)
                {
                    Player player2 = abstractCreature.realizedCreature as Player;
                    if (!player2.ReadyForWinJolly)
                    {
                        flag = false;
                    }
                    if (player2.ReadyForStarveJolly)
                    {
                        flag2 = true;
                    }
                }
            }
            if (!flag && !flag2)
            {
                return;
            }
        }
        if (!self.room.game.rainWorld.progression.miscProgressionData.regionsVisited.ContainsKey(self.room.world.name))
        {
            self.room.game.rainWorld.progression.miscProgressionData.regionsVisited.Add(self.room.world.name, new List<string>());
        }
        if (!self.room.game.rainWorld.progression.miscProgressionData.regionsVisited[self.room.world.name].Contains(self.room.game.GetStorySession.saveStateNumber.value))
        {
            self.room.game.rainWorld.progression.miscProgressionData.regionsVisited[self.room.world.name].Add(self.room.game.GetStorySession.saveStateNumber.value);
        }




        bool winGame = true;
        if (ModManager.CoopAvailable)
        {
            List<PhysicalObject> list = (from x in self.room.physicalObjects.SelectMany((List<PhysicalObject> x) => x)
                                         where x is Player
                                         select x).ToList();
            int playerCount = list.Count();
            int foodInRoom = 0;
            int y = SlugcatStats.SlugcatFoodMeter(self.room.game.StoryCharacter).y;
            winGame = (playerCount >= self.room.game.PlayersToProgressOrWin.Count);
            JollyCustom.Log("Player(s) in shelter: " + playerCount.ToString() + " Survived: " + winGame.ToString(), false);
            if (winGame)
            {
                foreach (PhysicalObject physicalObject in list)
                {
                    foodInRoom = Math.Max((physicalObject as Player).FoodInRoom(self.room, false), foodInRoom);
                }
                JollyCustom.Log("Survived!, food in room " + foodInRoom.ToString(), false);
                foreach (AbstractCreature abstractCreature in self.room.game.Players)
                {
                    if (abstractCreature.Room != self.room.abstractRoom)
                    {
                        try
                        {
                            JollyCustom.WarpAndRevivePlayer(abstractCreature, self.room.abstractRoom, self.room.LocalCoordinateOfNode(0));
                        }
                        catch (Exception arg)
                        {
                            JollyCustom.Log(string.Format("Could not warp and revive player {0} [{1}]", abstractCreature, arg), false);
                        }
                    }
                }
                self.room.game.Win(foodInRoom < y);
            }
            else
            {
                self.room.game.GoToDeathScreen();
            }
        }
        else
        {
            for (int i = 0; i < self.room.game.Players.Count; i++)
            {
                if (!self.room.game.Players[i].state.alive)
                {
                    winGame = false;
                }
            }
            if (winGame)
            {
                self.room.game.Win((self.room.game.Players[0].realizedCreature as Player).FoodInRoom(self.room, false) < (self.room.game.Players[0].realizedCreature as Player).slugcatStats.foodToHibernate);
            }
            else 
            {
                self.room.game.GoToDeathScreen();
            }
            
        }


    }













/*    public static void Player_SpitOutOfShortCut()
    {
        player.stillInStartShelter = false;
        if (newRoom.game.session is StoryGameSession && newRoom.world.region != null && player.AI == null)
        {
            if (!(newRoom.game.session as StoryGameSession).saveState.regionStates[newRoom.world.region.regionNumber].roomsVisited.Contains(newRoom.abstractRoom.name))
            {
                (newRoom.game.session as StoryGameSession).saveState.regionStates[newRoom.world.region.regionNumber].roomsVisited.Add(newRoom.abstractRoom.name);
            }
            if (newRoom.abstractRoom.shelter)
            {
                newRoom.game.rainWorld.progression.TempDiscoverShelter(newRoom.abstractRoom.name);
            }
        }
    }
*/







/*
    public static void Player_UpdateMSC()
    {
        if (player.timeSinceSpawned == 5 && base.abstractCreature.world.game.IsStorySession && player.AI == null)
        {
            if (ModManager.MSC && !player.room.abstractRoom.shelter && player.room.game.globalRain.drainWorldFlood > 0f)
            {
                if (RainWorld.ShowLogs)
                {
                    Debug.Log("Drainworld force cancel due to no shelter");
                }
                player.room.game.globalRain.drainWorldFlood = 10f;
            }
            if (ModManager.MMF && player.room.abstractRoom.shelter)
            {
                for (int i = 0; i < player.room.physicalObjects.Length; i++)
                {
                    for (int j = 0; j < player.room.physicalObjects[i].Count; j++)
                    {
                        if (player.room.physicalObjects[i][j].abstractPhysicalObject.type != AbstractPhysicalObject.AbstractObjectType.Creature)
                        {
                            player.room.physicalObjects[i][j].firstChunk.pos = base.firstChunk.pos;
                        }
                    }
                }
            }
        }
    }*/










}
