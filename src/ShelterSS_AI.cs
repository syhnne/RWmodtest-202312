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
/// TODO: 喵的 全是bug 好吧基本都是显示问题
/// 另外，我自己都不知道，原来联机的时候强制睡觉时需要所有活着的人都按住下键吗
/// 1.（不修了）修复玩家2不显示hud的问题，包括这个和gravitymeter
/// 2.（修好了，下辈子再也不重构代码）修复fp会把蛞蝓猫尸体扔出去的问题（洗脑大失败！最好顺便修复一下用矛打他他会还手的问题，不然只能狼狈地在mod简介写上“请不要攻击你自己的人偶”了）（错误的，他会这么干大概是因为我开始那个存档的时候这些代码还没被写出来，实际上在我删了档准备复刻这个难绷画面的时候，如果你拖着队友尸体进来，他会直接把你也整死（
/// 3.为各种条件下（比如普通难度下队友尸体在门外时）不能休眠添加食物条显示。
/// 4.（他自己就加上了，太好了）点动画，特指没开联机的情况下（因为开了联机之后按住下键可以到处睡觉）最好加个hud显示仪表，用来给你看你还要按多久才会睡觉
/// 5.（修好了）修复挨饿后休眠会使食物条显示被压到5的问题（是malnourished的问题，我得找另一个方法判断玩家上个雨循环是否挨饿）
/// 6.（修好了）修复forcesleep导致的业力花损失动画问题（我第一次知道，原来有业力花时挨饿睡觉会失去业力花啊（然后发现降雨计时器也有动画，捏麻麻的（好了，都修好了
/// 7.（不写了，没有shelterindex的话是追踪不了的）添加像正常避难所一样的物品追踪功能。。呃啊。。如果可以的话把通行证传送也加上。。呃啊这只是控制台的第一项能力。。我要鼠了。。
/// 



public static class ShelterSS_AI
{




    public static void Apply()
    {
        // On.HUD.FoodMeter.MoveSurvivalLimit += HUD_FoodMeter_MoveSurvivalLimit;
        IL.HUD.FoodMeter.GameUpdate += IL_HUD_Foodmeter_GameUpdate;
        On.HUD.FoodMeter.GameUpdate += HUD_Foodmeter_GameUpdate;
        IL.HUD.KarmaMeter.Draw += IL_HUD_KarmaMeter_Draw;
        // On.HUD.KarmaMeter.Update += HUD_KarmaMeter_Update;

        new Hook(
            typeof(KarmaMeter).GetProperty(nameof(KarmaMeter.Radius), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            KarmaMeter_Radius
            );
    }





    private delegate float orig_Radius(KarmaMeter self);
    private static float KarmaMeter_Radius(orig_Radius orig, KarmaMeter self)
    {
        var result = orig(self);
        if(self.hud.owner is Player && (self.hud.owner as Player).room != null && (self.hud.owner as Player).room.game.IsStorySession && (self.hud.owner as Player).room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            float forceSleep = (self.hud.owner as Player).FoodInStomach >= self.hud.foodMeter.survivalLimit ? 0f : self.hud.foodMeter.forceSleep;
            result = self.rad + (self.showAsReinforced ? (8f * (1f - Mathf.InverseLerp(0.2f, 0.4f, forceSleep))) : 0f);
        }
        return result;
    }





    private static void HUD_Foodmeter_GameUpdate(On.HUD.FoodMeter.orig_GameUpdate orig, FoodMeter self)
    {
        try
        {
            orig(self);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError(e);
        }
        
    }




    
    private static void HUD_KarmaMeter_Update(On.HUD.KarmaMeter.orig_Update orig, KarmaMeter self)
    {
        orig(self);
        Plugin.Log("karmameter showasreinforced:", self.showAsReinforced, "radius:", self.Radius);
    }




    // 防止吃饱了睡觉时有业力花丢失动画
    private static void IL_HUD_KarmaMeter_Draw(ILContext il)
    {
        // 119?
        ILCursor c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Callvirt),
            (i) => i.Match(OpCodes.Ldc_R4),
            (i) => i.Match(OpCodes.Ldc_R4),
            (i) => i.Match(OpCodes.Ldarg_0),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Callvirt),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Call)
            ))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, KarmaMeter, float>>((fl, self) =>
            {
                if (self.hud.owner is Player && (self.hud.owner as Player).room != null && (self.hud.owner as Player).room.game.IsStorySession && (self.hud.owner as Player).room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
                {
                    return (self.hud.owner as Player).FoodInStomach >= self.hud.foodMeter.survivalLimit ? 0f : fl;
                }
                return fl;
            });
        }
    }





    // 为了避免强制睡觉的时候显示消耗的食物增多
    private static void IL_HUD_Foodmeter_GameUpdate(ILContext il)
    {
        // 533
        ILCursor c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Ldarg_0),
            (i) => i.Match(OpCodes.Ldarg_0),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Conv_R4),
            (i) => i.Match(OpCodes.Ldarg_0),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Callvirt)
            ))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<int, FoodMeter, int>>((currentFood, self) =>
            {
                if (self.hud.owner is Player && (self.hud.owner as Player).room != null && (self.hud.owner as Player).room.game.IsStorySession && (self.hud.owner as Player).room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName) 
                {
                    // 这个malnourished有问题，他很。。智障，我吃饱了之后，他就变成false了，这个时候就会给我返回5，
                    return Math.Min(currentFood, self.survivalLimit);
                }
                return currentFood;
            });
        }
    }













    /*public static void Player_ctor(Player player, AbstractCreature abstractCreature, World world)
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
    }*/




    // 正常睡觉的第一环节
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
            Plugin.Log("force sleep counter: ", player.forceSleepCounter, " reinforcedKarma: ", player.room.game.GetStorySession.saveState.deathPersistentSaveData.reinforcedKarma);

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







    // 正常睡觉的第二环节
    // 纯属复制游戏代码，我想把他改的更阳间一点，但暂时先别改
    public static void ShelterDoorClose(Player self)
    {
        Plugin.Log("ShelterDoorClose");
        // 只能复制源代码了 改不了一点
        if (ModManager.CoopAvailable)
        {
            List<AbstractCreature> playersToProgressOrWin = self.room.game.PlayersToProgressOrWin;
            List<AbstractCreature> list = (from x in self.room.physicalObjects.SelectMany((List<PhysicalObject> x) => x).OfType<Player>()
                                           select x.abstractCreature).ToList<AbstractCreature>();
            bool flag = true;
            bool flag2 = false;
            bool flag3 = false;
            foreach (AbstractCreature abstractCreature in playersToProgressOrWin)
            {
                if (!list.Contains(abstractCreature))
                {
                    int playerNumber = (abstractCreature.state as PlayerState).playerNumber;
                    flag3 = true;
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
                }
                if (flag3)
                {
                    foreach (Player player in from x in list
                                              select x.realizedCreature as Player)
                    {
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
                Plugin.Log("jolly wingame");
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
                Plugin.Log("single player win");
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
