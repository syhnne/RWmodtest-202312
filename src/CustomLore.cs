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
using MonoMod.Cil;
using System.Linq;
using System.Runtime.CompilerServices;
using Menu;




namespace PebblesSlug;




internal class CustomLore
{


    internal static void Apply()
    {

        On.RainWorldGame.Win += RainWorldGame_Win;

        On.Menu.StoryGameStatisticsScreen.CommunicateWithUpcomingProcess += Menu_StoryGameStatisticsScreen_CommunicateWithUpcomingProcess;
        On.Menu.SlugcatSelectMenu.UpdateStartButtonText += Menu_SlugcatSelectMenu_UpdateStartButtonText;
        On.Menu.SlugcatSelectMenu.ContinueStartedGame += Menu_SlugcatSelectMenu_ContinueStartedGame;
        // On.Menu.SlugcatSelectMenu.ctor += Menu_SlugcatSelectMenu_ctor;
        // On.Menu.SlugcatSelectMenu.MineForSaveData += Menu_SlugcatSelectMenu_MineForSaveData;


        // 防止玩家归乡，发布的时候记得取消注释
        // On.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.Update += MSCRoomSpecificScript_GourmandEnding_Update;


        IL.SaveState.LoadGame += SaveState_LoadGame;
        On.RainWorldGame.GoToRedsGameOver += RainWorldGame_GoToRedsGameOver;
        On.RainWorldGame.BeatGameMode += RainWorldGame_BeatGameMode;
        // On.Room.Loaded += Room_Loaded;

    }





    // 游戏界面
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 又是一个我动不了所以只能挨个堵调用方的函数（目死




    // 用于看完统计数据后回到主界面
    private static void Menu_StoryGameStatisticsScreen_CommunicateWithUpcomingProcess(On.Menu.StoryGameStatisticsScreen.orig_CommunicateWithUpcomingProcess orig, StoryGameStatisticsScreen self, MainLoopProcess nextProcess)
    {
        orig(self, nextProcess);
        if (nextProcess is SlugcatSelectMenu && (RainWorld.lastActiveSaveSlot == Plugin.SlugcatStatsName))
        {
            SlugcatSelectMenu menu = nextProcess as SlugcatSelectMenu;
            // 本来他调用的是一个ComingFromRedsStatistics。但我完全可以直接写这里，没必要修改那个函数。
            menu.slugcatPageIndex = menu.indexFromColor(Plugin.SlugcatStatsName);
            menu.UpdateSelectedSlugcatInMiscProg();
        }
    }



    // 用来把“继续”改成“数据统计”
    private static void Menu_SlugcatSelectMenu_UpdateStartButtonText(On.Menu.SlugcatSelectMenu.orig_UpdateStartButtonText orig, SlugcatSelectMenu self)
    {
        orig(self);
        if (self.slugcatPages[self.slugcatPageIndex].slugcatNumber == Plugin.SlugcatStatsName)
        {
            if (self.saveGameData[Plugin.SlugcatStatsName] == null) return;


            bool redsDeath = self.GetSaveGameData(self.slugcatPageIndex).redsDeath;
            bool altEnding = self.GetSaveGameData(self.slugcatPageIndex).altEnding;
            bool ascended = self.GetSaveGameData(self.slugcatPageIndex).ascended;
            int cycles = self.GetSaveGameData(self.slugcatPageIndex).cycle;
            Plugin.Log("MAIN MENU ascended:" + ascended + "  altEnding: " + altEnding + "  redsDeath: " + redsDeath);
            if ((!altEnding && cycles > Plugin.Cycles) || redsDeath || (!altEnding && ascended))
            {
                self.startButton.menuLabel.text = self.Translate("STATISTICS");
            }
            if (self.restartChecked)
            {
                self.startButton.menuLabel.text = self.Translate("DELETE SAVE").Replace(" ", "\r\n");
            }

        }
    }



    // 用于打开统计界面
    // 对于没打真结局的玩家来说，飞升了和死了一样，都不能再点开了
    private static void Menu_SlugcatSelectMenu_ContinueStartedGame(On.Menu.SlugcatSelectMenu.orig_ContinueStartedGame orig, SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
    {
        if (storyGameCharacter == Plugin.SlugcatStatsName)
        {
            if (self.saveGameData[storyGameCharacter] == null) return;

            bool redsDeath = self.GetSaveGameData(self.slugcatPageIndex).redsDeath;
            bool altEnding = self.GetSaveGameData(self.slugcatPageIndex).altEnding;
            bool ascended = self.GetSaveGameData(self.slugcatPageIndex).ascended;
            int cycles = self.GetSaveGameData(self.slugcatPageIndex).cycle;
            if ((!altEnding && cycles > Plugin.Cycles) || redsDeath || (!altEnding && ascended))
            {
                self.redSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(Plugin.SlugcatStatsName, null, self.manager.menuSetup, false);
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
                self.PlaySound(SoundID.MENU_Switch_Page_Out);
                return;
            }

        }
        orig(self, storyGameCharacter);
    }




    // menuscene什么的下次再说（）这不会引起什么bug罢











    // 死亡结局结算
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////




    // 防止玩家在循环耗尽的时候正常睡觉
    private static void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
    {
        if (self.manager.upcomingProcess != null) return;

        SaveState save = self.GetStorySession.saveState;
        if (self.GetStorySession.saveStateNumber.value == Plugin.SlugcatName)
        {
            Plugin.Log("RainWorldGame_Win: cycle: " + save.cycleNumber);
            if (!save.deathPersistentSaveData.altEnding && save.cycleNumber >= Plugin.Cycles)
            {
                Plugin.Log("PebblesSlug Game Over !!! ++==== cycle:" + save.cycleNumber);
                save.deathPersistentSaveData.redsDeath = true;
                save.deathPersistentSaveData.ascended = false;
                self.GoToRedsGameOver();
                return;
            }
        }
        orig(self, malnourished);
    }





    private static void SaveState_LoadGame(ILContext il)
    {
        Plugin.Log("SaveState_LoadGame hooked");
        ILCursor c = new ILCursor(il);
        // 我敲 不会真是他导致的罢 我怎么没见这个函数挂上去呢
        if (c.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Brfalse_S),
            (i) => i.Match(OpCodes.Ldarg_0),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Ldarg_0),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Call)
            ))
        {
            Plugin.Log("Match successfully! - SaveState_LoadGame - 1326");
            c.Emit(OpCodes.Ldarg_2);
            c.EmitDelegate<Func<int, RainWorldGame, int>>((redsCycles, game) =>
            {
                return (game != null && game.IsStorySession && game.GetStorySession.saveStateNumber.value == Plugin.SlugcatName) ? Plugin.Cycles : redsCycles;
            });
        }
    }






    private static void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
    {

    }

    private static void SlideShow_ctor(On.Menu.SlideShow.orig_ctor orig, Menu.SlideShow self, ProcessManager manager, Menu.SlideShow.SlideShowID slideShowID)
    {

    }




    // 所有结局都在这，但altending和飞升还有那个beatgamemode
    private static void RainWorldGame_GoToRedsGameOver(On.RainWorldGame.orig_GoToRedsGameOver orig, RainWorldGame self)
    {
        if (self.GetStorySession.saveState.saveStateNumber == Plugin.SlugcatStatsName)
        {
            Plugin.Log("RainWorldGame_GoToRedsGameOver ++====");
            Plugin.Log("redsDeath:", self.GetStorySession.saveState.deathPersistentSaveData.redsDeath.ToString());
            Plugin.Log("altEnding:", self.GetStorySession.saveState.deathPersistentSaveData.altEnding.ToString());
            Plugin.Log("ascended:", self.GetStorySession.saveState.deathPersistentSaveData.ascended.ToString());
            // self.manager.rainWorld.progression.currentSaveState.deathPersistentSaveData.redsDeath = true;

            // 怪事。redsDeath死活挂不上，底下那ilhook也没写错，但log是一点反应都没有。剩下两个结局就没毛病。事已至此，只能启动planB了！

            if (self.GetStorySession.saveState.deathPersistentSaveData.redsDeath)
            {
                if (self.manager.upcomingProcess != null) return;
                self.manager.musicPlayer?.FadeOutAllSongs(20f);
                if (ModManager.CoopAvailable)
                {
                    int num = 0;
                    using IEnumerator<Player> enumerator = (from x in self.session.game.Players
                                                            select x.realizedCreature as Player).GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        Player player = enumerator.Current;
                        self.GetStorySession.saveState.AppendCycleToStatistics(player, self.GetStorySession, true, num);
                        num++;
                    }
                }
                else self.GetStorySession.saveState.AppendCycleToStatistics(self.Players[0].realizedCreature as Player, self.GetStorySession, true, 0);

                // 一个阴间小技巧。既然主界面读不到数据，那么就读雨循环吧，读到负数+没结局+没飞升，就是gameover了。
                self.GetStorySession.saveState.SessionEnded(self, true, false);

                self.manager.rainWorld.progression.SaveWorldStateAndProgression(false);
                self.manager.statsAfterCredits = true;
                // 准备加点slideshow，这样玩家才知道自己已经寄啦
                // self.manager.nextSlideshow = DroneMasterEnums.DroneMasterAltEnd;

            }

            else if (self.GetStorySession.saveState.deathPersistentSaveData.altEnding)
            {
                self.manager.statsAfterCredits = true;
                // 暂时代替一下。虽然但是，归乡的时候这个也不会显示
                self.manager.nextSlideshow = MoreSlugcatsEnums.SlideShowID.GourmandAltEnd;
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SlideShow);
                return;
            }

        }
        orig(self);
    }




    private static void RainWorldGame_BeatGameMode(On.RainWorldGame.orig_BeatGameMode orig, RainWorldGame game, bool standardVoidSea)
    {
        orig(game, standardVoidSea);
        if (game.GetStorySession.saveState.saveStateNumber.value == "PebblesSlug")
        {
            if (standardVoidSea)
            {
                Plugin.Log("Beat Game Mode(void sea ending) : ", (game.GetStorySession.saveState?.ToString()));
                game.GetStorySession.saveState.deathPersistentSaveData.ascended = true;
                // game.rainWorld.progression.miscProgressionData.beaten_（） = true;
                if (ModManager.CoopAvailable)
                {
                    int count = 0;
                    using IEnumerator<Player> enumerator = (from x in game.GetStorySession.game.Players
                                                            select x.realizedCreature as Player).GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        Player player = enumerator.Current;
                        game.GetStorySession.saveState.AppendCycleToStatistics(player, game.GetStorySession, true, count);
                        count++;
                    }
                }
                else
                {
                    game.GetStorySession.saveState.AppendCycleToStatistics(game.Players[0].realizedCreature as Player, game.GetStorySession, true, 0);
                }

                return;
            }

            string roomName = "SS_AI";
            Plugin.Log("Beat Game Mode(alt ending) : ", (game.GetStorySession.saveState?.ToString()));
            // game.rainWorld.progression.miscProgressionData.beaten_（） = true;
            // 下面这个不要了，不出意外的话打完真结局会有类似功能
            // game.GetStorySession.saveState.deathPersistentSaveData.karma = game.GetStorySession.saveState.deathPersistentSaveData.karmaCap;
            game.GetStorySession.saveState.deathPersistentSaveData.altEnding = true;
            game.GetStorySession.saveState.BringUpToDate(game);
            AbstractCreature abstractCreature = game.FirstAlivePlayer;
            abstractCreature ??= game.FirstAnyPlayer;
            game.GetStorySession.saveState.AppendCycleToStatistics(abstractCreature.realizedCreature as Player, game.GetStorySession, false, 0);
            RainWorldGame.ForceSaveNewDenLocation(game, roomName, false);

        }





    }






    // 防止玩家用特殊手段归乡（别想在酒吧点炒饭
    // 我暂且先禁用这个函数，因为我正经结局没做好，先拿归乡结局代替一下（目移
    private static void MSCRoomSpecificScript_GourmandEnding_Update(On.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.orig_Update orig, MSCRoomSpecificScript.OE_GourmandEnding self, bool eu)
    {
        if (!ModManager.CoopAvailable)
        {
            if (self.room.game.Players.Count > 0 && self.room.game.Players[0].realizedCreature != null && self.room.game.Players[0].realizedCreature.room == self.room && (self.room.game.Players[0].realizedCreature as Player).slugcatStats.name == Plugin.SlugcatStatsName)
            {
                return;
            }
        }
        else
        {
            if (self.room.PlayersInRoom.Count > 0 && self.room.PlayersInRoom[0] != null && self.room.PlayersInRoom[0].room == self.room && self.room.PlayersInRoom[0].slugcatStats.name == Plugin.SlugcatStatsName)
            {
                return;
            }
        }
        orig(self, eu);
    }











    private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
    {
        orig(self);
        // 在这里加入roomspecificscript。救命。没人讲过开发者工具怎么用，结果我就把那个SS_AI房间的什么着色效果给搞坏了，现在每次进去都会被橙色闪瞎狗眼。。
        Plugin.Log("Room_Loaded: ", self.abstractRoom.name);
    }





    



}








internal class RoomSpecificScripts
{

    internal void RoomSpecificScriptsApply()
    {
        On.MoreSlugcats.MSCRoomSpecificScript.AddRoomSpecificScript += MSCRoomSpecificScript_AddRoomSpecificScript;
    }

    private void MSCRoomSpecificScript_AddRoomSpecificScript(On.MoreSlugcats.MSCRoomSpecificScript.orig_AddRoomSpecificScript orig, Room room)
    {
        string name = room.abstractRoom.name;

        // 只能触发一次
        if (name == "SS_AI" && room.game.IsStorySession && room.game.GetStorySession.saveState.saveStateNumber.value == "PebblesSlug" && !room.game.GetStorySession.saveState.deathPersistentSaveData.altEnding)
        {
            room.AddObject(new SS_PebblesAltEnding(room));
        }

    }



    // public class OE_GourmandEnding : UpdatableAndDeletable 改的这个
    // 这应该是正经结局，好了，那么问题来了，fp猫猫是怎么把自己整活的。回头我得想想，现在的内容是只要进了这个房间且掉在地板上（y<400f？）过几秒就触发结局
    internal class SS_PebblesAltEnding : UpdatableAndDeletable
    {
        public bool endingTriggered;
        public int endingTriggerTime;
        private Player foundPlayer;
        private bool setController;
        public FadeOut fadeOut;
        private bool doneFinalSave;

        internal SS_PebblesAltEnding(Room room)
        {
            this.room = room;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            // 测试的时候用这个粗略判断一下，防止刚一开始就结束了
            if (room.game.session is not StoryGameSession 
                || room.game.GetStorySession.saveStateNumber != Plugin.SlugcatStatsName
                || !room.game.GetStorySession.saveState.miscWorldSaveData.EverMetMoon ) return;

            if (!ModManager.CoopAvailable)
            {
                if (foundPlayer == null && room.game.Players.Count > 0 && room.game.Players[0].realizedCreature != null && room.game.Players[0].realizedCreature.room == room)
                {
                    foundPlayer = (room.game.Players[0].realizedCreature as Player);
                }
                if (foundPlayer == null || foundPlayer.inShortcut || room.game.Players[0].realizedCreature.room != room)
                {
                    return;
                }
            }
            else
            {
                if (foundPlayer == null && room.PlayersInRoom.Count > 0 && room.PlayersInRoom[0] != null && room.PlayersInRoom[0].room == room)
                {
                    foundPlayer = room.PlayersInRoom[0];
                }
                if (foundPlayer == null || foundPlayer.inShortcut || foundPlayer.room != room)
                {
                    return;
                }
                room.game.cameras[0].EnterCutsceneMode(foundPlayer.abstractCreature, RoomCamera.CameraCutsceneType.Oracle);
            }
            if (foundPlayer.firstChunk.pos.y < 500f && !setController)
            {
                Plugin.Log("Ending cutscene triggered");
                RainWorld.lockGameTimer = true;
                // 应该没必要控制玩家行为。。
                // setController = true;
                // foundPlayer.controller = new EndingController(this);
            }
            if (foundPlayer.firstChunk.pos.y < 500f && !endingTriggered)
            {
                endingTriggerTime++;
                if (endingTriggerTime > 20)
                {
                    endingTriggered = true;
                    // 这是不是过场动画？
                    room.game.manager.sceneSlot = room.game.GetStorySession.saveStateNumber;

                    if (fadeOut == null)
                    {
                        fadeOut = new FadeOut(room, Color.black, 200f, false);
                        room.AddObject(fadeOut);
                    }
                }
            }
            if (fadeOut != null && fadeOut.IsDoneFading() && !doneFinalSave)
            {
                Plugin.Log("PebblesSlug Alt Ending !!!");
                // 这句话对我来说没用吧
                room.game.GetStorySession.saveState.miscWorldSaveData.SSaiThrowOuts = 0;
                // 好了，真的问题来了。我得把下面这个函数复现一遍，因为我没法往里传参数，但隔壁还有一个东西要用这函数
                // 好吧我想到了。在这里挂altending，还是在那个函数里判断吧
                room.game.GetStorySession.saveState.deathPersistentSaveData.altEnding = true;
                room.game.GoToRedsGameOver();
                RainWorldGame.BeatGameMode(room.game, false);
                doneFinalSave = true;
            }




        }




        public Player.InputPackage GetInput()
        {
            // 好家伙 替你控制玩家了这是 回头做控制面板的时候对这玩意儿下毒就行
            return new Player.InputPackage(true, Options.ControlSetup.Preset.None, 0, 0, false, false, false, false, false);
        }


        public class EndingController : Player.PlayerController
        {

            private SS_PebblesAltEnding owner;
            public EndingController(SS_PebblesAltEnding owner)
            {
                this.owner = owner;
            }

            public override Player.InputPackage GetInput()
            {
                return owner.GetInput();
            }

        }


    }




    // 呃，这应该是开头播放的一段动画之类，总之先空出来。。。真正的难点要开始了
    // 搜索：public class DS_RIVSTARTcutscene : UpdatableAndDeletable
    internal class SSPebblesStartCutscene : UpdatableAndDeletable
    {
        internal SSPebblesStartCutscene(Room room)
        {

        }
        public override void Update(bool eu)
        {
            if (this.timer >= 90)
            {
                this.Destroy();
                return;
            }
            AbstractCreature firstAlivePlayer = this.room.game.FirstAlivePlayer;
            if (this.room.game.session is StoryGameSession && this.room.game.Players.Count > 0 && firstAlivePlayer != null && firstAlivePlayer.realizedCreature != null && firstAlivePlayer.realizedCreature.room == this.room && this.room.game.GetStorySession.saveState.cycleNumber == 0)
            {
                Player player = firstAlivePlayer.realizedCreature as Player;

                // 不知道这个会不会有bug，碰见问题先把他注释了
                player.objectInStomach = new AbstractPhysicalObject(this.room.world, AbstractPhysicalObject.AbstractObjectType.SSOracleSwarmer, null, new WorldCoordinate(this.room.abstractRoom.index, -1, -1, 0), this.room.game.GetNewID());
                player.SuperHardSetPosition(new Vector2(500f, 500f));
                player.mainBodyChunk.vel = new Vector2(0f, -5f);
                player.Stun(100);
            }
            this.timer++;
        }
        private int timer;
    }


}