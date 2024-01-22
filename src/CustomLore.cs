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
        // On.RainWorldGame.BeatGameMode += RainWorldGame_BeatGameMode;

        On.RainWorldGame.Win += RainWorldGame_Win;

        On.Menu.StoryGameStatisticsScreen.CommunicateWithUpcomingProcess += Menu_StoryGameStatisticsScreen_CommunicateWithUpcomingProcess;
        On.Menu.SlugcatSelectMenu.UpdateStartButtonText += Menu_SlugcatSelectMenu_UpdateStartButtonText;
        On.Menu.SlugcatSelectMenu.ContinueStartedGame += Menu_SlugcatSelectMenu_ContinueStartedGame;
        // On.Menu.SlugcatSelectMenu.ctor += Menu_SlugcatSelectMenu_ctor;
        // On.Menu.SlugcatSelectMenu.MineForSaveData += Menu_SlugcatSelectMenu_MineForSaveData;

        IL.SaveState.LoadGame += SaveState_LoadGame;

        On.RainWorldGame.GoToRedsGameOver += RainWorldGame_GoToRedsGameOver;
        
        
    }



    // 游戏界面
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 又是一个我动不了所以只能挨个堵调用方的函数（目死




    // 用于看完统计数据后回到主界面
    private static void Menu_StoryGameStatisticsScreen_CommunicateWithUpcomingProcess(On.Menu.StoryGameStatisticsScreen.orig_CommunicateWithUpcomingProcess orig, StoryGameStatisticsScreen self,  MainLoopProcess nextProcess)
    {
        orig(self, nextProcess);
        if (nextProcess is SlugcatSelectMenu && (RainWorld.lastActiveSaveSlot == Plugin.SlugcatStatsName))
        {
            SlugcatSelectMenu menu = nextProcess as SlugcatSelectMenu;
            // 本来他调用的是一个ComingFromRedsStatistics。但我完全可以直接写这里，没必要修改那个函数。
            menu.slugcatPageIndex = menu.indexFromColor(Plugin.SlugcatStatsName);
            menu.saveGameData[Plugin.SlugcatStatsName].redsDeath = true;
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
            Debug.Log("====++ MAIN MENU ascended:" + ascended + "  altEnding: " + altEnding + "  redsDeath: " + redsDeath);
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
        
        StoryGameSession story = self.GetStorySession;
        if (story.saveStateNumber.value == Plugin.SlugcatName)
        {
            Debug.Log("====++ RainWorldGame_Win: cycle: " + story.saveState.cycleNumber);
            if (story.saveState.cycleNumber >= Plugin.Cycles)
            {
                Debug.Log("====++ PebblesSlug Game Over !!! ++==== cycle:"+ story.saveState.cycleNumber);
                self.GetStorySession.saveState.deathPersistentSaveData.redsDeath = true;
                self.GoToRedsGameOver();
                return;
            }
        }
        orig(self, malnourished);
    }






    // 目前只用来挂gameover结局，其他的结局……应该能找到别的函数来挂吧（
    private static void RainWorldGame_GoToRedsGameOver(On.RainWorldGame.orig_GoToRedsGameOver orig, RainWorldGame self)
    {
        if (self.GetStorySession.saveState.saveStateNumber == Plugin.SlugcatStatsName)
        {
            Debug.Log("====++ RainWorldGame_GoToRedsGameOver ++====");


            // self.manager.rainWorld.progression.currentSaveState.deathPersistentSaveData.redsDeath = true;

            // 怪事。redsDeath死活挂不上，底下那ilhook也没写错，但log是一点反应都没有。剩下两个结局就没毛病。事已至此，只能启动planB了！
            self.GetStorySession.saveState.deathPersistentSaveData.redsDeath = true;
            self.GetStorySession.saveState.deathPersistentSaveData.ascended = false;

            Debug.Log("====++ " + self.GetStorySession.saveState.deathPersistentSaveData.redsDeath);

            if (self.manager.upcomingProcess != null) return;
            self.manager.musicPlayer?.FadeOutAllSongs(20f);
            if (ModManager.CoopAvailable)
            {
                int num = 0;
                using (IEnumerator<Player> enumerator = (from x in self.session.game.Players
                                                         select x.realizedCreature as Player).GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Player player = enumerator.Current;
                        self.GetStorySession.saveState.AppendCycleToStatistics(player, self.GetStorySession, true, num);
                        num++;
                    }
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
        orig(self);
    }







    private static void SaveState_LoadGame(ILContext il)
    {
        Debug.Log("====++ SaveState_LoadGame hooked");
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
            Debug.Log("====++ Match successfully! - SaveState_LoadGame - 1326");
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









    private static void RainWorldGame_BeatGameMode(On.RainWorldGame.orig_BeatGameMode orig, RainWorldGame game, bool standardVoidSea)
    {
        orig(game, standardVoidSea);
        if (game.GetStorySession.saveState.saveStateNumber.value == "PebblesSlug")
        {
            if (standardVoidSea)
            {
                string str = "====++ Beat Game Mode(void sea ending) : ";
                SaveState saveState = game.GetStorySession.saveState;
                Debug.Log(str + ((saveState != null) ? saveState.ToString() : null));
                game.GetStorySession.saveState.deathPersistentSaveData.ascended = true;
                game.GetStorySession.saveState.deathPersistentSaveData.altEnding = true;
                // game.rainWorld.progression.miscProgressionData.beaten_（） = true;
                if (ModManager.CoopAvailable)
                {
                    int count = 0;
                    using (IEnumerator<Player> enumerator = (from x in game.GetStorySession.game.Players
                                                             select x.realizedCreature as Player).GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Player player = enumerator.Current;
                            game.GetStorySession.saveState.AppendCycleToStatistics(player, game.GetStorySession, true, count);
                            count++;
                        }
                    }
                }
                else
                {
                    game.GetStorySession.saveState.AppendCycleToStatistics(game.Players[0].realizedCreature as Player, game.GetStorySession, true, 0);
                }

                return;
            }

            string roomName = "SS_AI";
            // game.GetStorySession.saveState.deathPersistentSaveData.altEnding = true;
            // game.GetStorySession.saveState.deathPersistentSaveData.ascended = false;
            // game.rainWorld.progression.miscProgressionData.beaten_（） = true;
            game.GetStorySession.saveState.deathPersistentSaveData.karma = game.GetStorySession.saveState.deathPersistentSaveData.karmaCap;

            RainWorldGame.ForceSaveNewDenLocation(game, roomName, false);

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








internal class RoomSpecificScripts
{

    internal void RoomSpecificScriptsApply()
    {
        On.MoreSlugcats.MSCRoomSpecificScript.AddRoomSpecificScript += MSCRoomSpecificScript_AddRoomSpecificScript;
    }

    private void MSCRoomSpecificScript_AddRoomSpecificScript(On.MoreSlugcats.MSCRoomSpecificScript.orig_AddRoomSpecificScript orig, Room room)
    {
        string name = room.abstractRoom.name;


        // 感觉这么写不对。这无所谓，这取决于结局到底是什么样的
        if (name == "SS_AI" && room.game.IsStorySession && room.game.GetStorySession.saveState.saveStateNumber.value == "PebblesSlug" && !room.game.GetStorySession.saveState.deathPersistentSaveData.altEnding)
        {

        }
    }



    // public class OE_GourmandEnding : UpdatableAndDeletable 明天写这个
    // 这应该是正经结局，但我得先摇个人来问问结局咋写
    private class SS_PebblesAltEnding : UpdatableAndDeletable
    {
        public bool endingTriggered;
        public int endingTriggerTime;


        private SS_PebblesAltEnding(Room room)
        {
            this.room = room;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            // 测试的时候用这个粗略判断一下，防止刚一开始就结束了
            // if (this.GetStorySession.saveState.miscWorldSaveData.EverMetMoon)


            if (this.endingTriggered)
            {
                this.endingTriggerTime++;
                if (this.endingTriggerTime == 80)
                {
                    this.room.game.GetStorySession.saveState.deathPersistentSaveData.reinforcedKarma = false;
                    this.room.game.GoToRedsGameOver();
                    RainWorldGame.BeatGameMode(this.room.game, false);
                }
            }

            RainWorldGame.BeatGameMode(this.room.game, false);
        }


    }


    


}