using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PebblesSlug;

internal class SLOracleHooks
{

    public static void Disable()
    {
        // On.SLOracleBehaviorHasMark.InitateConversation -= SLOracleBehaviorHasMark_InitateConversation;
        On.SLOracleBehaviorHasMark.MoonConversation.AddEvents -= MoonConversation_AddEvents;
    }


    public static void Apply()
    {
        // On.SLOracleBehaviorHasMark.InitateConversation += SLOracleBehaviorHasMark_InitateConversation;
        On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += MoonConversation_AddEvents;
    }

    
    

    private static void MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
    {
        if (self.myBehavior.oracle.room.game.IsStorySession && self.myBehavior.oracle.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            Plugin.Log("moon conversation:", self.id.ToString(), self.State.neuronsLeft.ToString());

            if (self.id == Conversation.ID.MoonFirstPostMarkConversation)
            {
                switch (Mathf.Clamp(self.State.neuronsLeft, 0, 5))
                {
                    // 不会有雨鹿绞尽脑汁绕过我加的食性限制还要吃神经元罢（
                    // 应当是不会的罢 所以我不写了（
                    case 0:
                        break;
                    case 1:
                        self.events.Add(new Conversation.TextEvent(self, 40, "...", 10));
                        return;
                    case 2:
                        self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("Get... get away... white.... thing."), 10));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Please... thiss all I have left."), 10));
                        return;
                    case 3:
                        self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("You!"), 10));
                        self.events.Add(new Conversation.TextEvent(self, 60, self.Translate("...you ate... me. Please go away. I won't speak... to you.<LINE>I... CAN'T speak to you... because... you ate...me..."), 0));
                        return;
                    case 4:
                        // 哈？这两个文件是啥啊
                        /*Plugin.LogAllConversations(self, 35);
                        Plugin.LogAllConversations(self, 37);*/
                        self.LoadEventsFromFile(35);
                        self.LoadEventsFromFile(37);
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I'm still angry at you, but it is good to have someone to talk to after all self time.<LINE>The scavengers aren't exactly good listeners. They do bring me things though, occasionally..."), 0));
                        return;
                    case 5:

                        // 会编程真是方便啊。jpg
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Hello <PlayerName>."), 0));
                        /*self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I think I know what you're coming here for."), 0));
                        for (int i = 1; i <= 57; i++)
                        {
                            Plugin.LogAllConversations(self, i);
                        }
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Here are all the conversations in game."), 0));*/
                        return;
                    default:
                        return;

                }
            }



        }
        else { orig(self); }
    }


    private static void SLOracleBehaviorHasMark_InitateConversation(On.SLOracleBehaviorHasMark.orig_InitateConversation orig, SLOracleBehaviorHasMark self)
    {
        if (self.oracle.room.game.IsStorySession && self.oracle.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            if (!self.State.SpeakingTerms)
            {
                self.dialogBox.NewMessage("...", 10);
                return;
            }
            int swarmers = 0;
            for (int i = 0; i < self.player.grasps.Length; i++)
            {
                if (self.player.grasps[i] != null && self.player.grasps[i].grabbed is SSOracleSwarmer)
                {
                    swarmers++;
                }
            }
            if (self.State.playerEncountersWithMark <= 0)
            {
                if (self.State.playerEncounters < 0)
                {
                    self.State.playerEncounters = 0;
                }



                self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(Conversation.ID.MoonFirstPostMarkConversation, self, SLOracleBehaviorHasMark.MiscItemType.NA);
                return;
            }
            else
            {
                if (swarmers > 0)
                {
                    self.PlayerHoldingSSNeuronsGreeting();
                    return;
                }
                if (self.State.playerEncountersWithMark != 1)
                {
                    self.ThirdAndUpGreeting();
                    return;
                }
                self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(Conversation.ID.MoonSecondPostMarkConversation, self, SLOracleBehaviorHasMark.MiscItemType.NA);
                return;
            }



        }
        else { orig(self); }
    }



}
