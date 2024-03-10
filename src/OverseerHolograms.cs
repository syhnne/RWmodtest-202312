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
using OverseerHolograms;


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
using MonoMod.Utils;
using System.IO;

namespace PebblesSlug;






/// <summary>
/// 原来是这么解决的吗？太离谱了
/// </summary>
internal class CustomOverseerHolograms
{

    public static void Disable()
    {
        On.OverseerCommunicationModule.ReevaluateConcern -= OverseerCommunicationModule_ReevaluateConcern;
        On.OverseerHolograms.OverseerImage.HoloImage.DrawSprites -= HoloImage_DrawSprites;
        On.AbstractRoom.RealizeRoom -= AbstractRoom_RealizeRoom;
    }


    public static void Apply()
    {
        On.OverseerCommunicationModule.ReevaluateConcern += OverseerCommunicationModule_ReevaluateConcern;
        // On.Overseer.TryAddHologram += OverSeer_TryAddHologram;

        // On.OverseerHolograms.OverseerImage.HoloImage.Update += HoloImage_Update;
        // On.OverseerHolograms.OverseerImage.HoloImage.ctor += HoloImage_ctor;
        // On.OverseerHolograms.OverseerImage.HoloImage.InitiateSprites += HoloImage_InitiateSprites;

        On.OverseerHolograms.OverseerImage.HoloImage.DrawSprites += HoloImage_DrawSprites;
        // On.OverseerHolograms.OverseerImage.ctor += OverSeerImage_ctor;

        // On.OverseerAbstractAI.AbstractBehavior += OverseerAbstractAI_AbstractBehavior;

        On.AbstractRoom.RealizeRoom += AbstractRoom_RealizeRoom;

    }











    // TODO: 这地方有bug，虽然不影响运行但还是修一下
    private static void AbstractRoom_RealizeRoom(On.AbstractRoom.orig_RealizeRoom orig, AbstractRoom self, World world, RainWorldGame game)
    {
        orig(self, world, game);
        if (self.realizedRoom == null || !game.IsStorySession || game.GetStorySession.saveStateNumber != Plugin.SlugcatStatsName) { return; }
        if (self.name == "SL_AI")
        {

            
            try
            {
                RoomSettings settings = self.realizedRoom.roomSettings;

                PlacedObject pos = new(PlacedObject.Type.ProjectedImagePosition, null);
                pos.pos = new Vector2(1401, 319);
                settings.placedObjects.Add(pos);

                // 下面的内容会卡bug

                EventTrigger trigger = new EventTrigger(EventTrigger.TriggerType.SeeCreature);


                TriggeredEvent projEvent = new TriggeredEvent(TriggeredEvent.EventType.ShowProjectedImageEvent);
                // 这个after encounter决定了投影是月姐说完话之前还是之后放。我觉得之后比较好，但我不知道改了月姐行为之后这个时间会不会跟着变，先这样吧
                // 喵的 这个延迟高得要命 还是改成之前吧
                if (projEvent is ShowProjectedImageEvent)
                {
                    (projEvent as ShowProjectedImageEvent).afterEncounter = false;
                    (projEvent as ShowProjectedImageEvent).onlyWhenShowingDirection = false;
                    (projEvent as ShowProjectedImageEvent).fromCycle = 0;
                }


                // trigger.delay = 500;
                if (trigger is SpotTrigger)
                {
                    trigger.tEvent = projEvent;
                    trigger.multiUse = false;
                    (trigger as SpotTrigger).pos = new Vector2(1401, 260);
                    trigger.panelPosition = new Vector2(1401, 319);
                    trigger.fireChance = 1;
                    trigger.entrance = 1;
                    trigger.karma = 0;
                }
                settings.triggers = new List<EventTrigger> { trigger };

            }
            catch (Exception e) { Plugin.Logger.LogError(e); }
        }

    }



    








    // TODO: 写个函数在房间里放一个projected image position

    private static void HoloImage_Update(On.OverseerHolograms.OverseerImage.HoloImage.orig_Update orig, OverseerImage.HoloImage self)
    {
        orig(self);
        Plugin.Log("pos:", self.hologram.overseer.mainBodyChunk.pos, "ownerIterator:", (self.hologram.overseer.abstractCreature.abstractAI as OverseerAbstractAI).ownerIterator, "holoimage:", self.imageOwner.CurrImage );
    }



    







    // 好好好，我懂了。把这个材质稍微改一下，然后把我要放的那几张图，放在原材质那几张图的位置，就成了。
    private static void HoloImage_DrawSprites(On.OverseerHolograms.OverseerImage.HoloImage.orig_DrawSprites orig, OverseerImage.HoloImage self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 partPos, Vector2 headPos, float useFade, float popOut, Color useColor)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos, partPos, headPos, useFade, popOut, useColor);
        if (self.imageOwner != null && self.hologram != null && self.hologram.room != null && self.hologram.room.game.IsStorySession && self.hologram.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName && self.hologram.room.abstractRoom.name == "SL_AI")
        {
            sLeaser.sprites[self.firstSprite].element = Futile.atlasManager.GetElementWithName("overseerHolograms/PebblesSlugHologram");
            // Plugin.Log("HoloImage_DrawSprites: atlas added");
        }
        else if (self.imageOwner != null && self.hologram != null && self.hologram.room != null && self.hologram.room.game.IsStorySession && self.hologram.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName && self.hologram.room.abstractRoom.name == "SL_A15") 
        { 
            sLeaser.sprites[self.firstSprite].element = Futile.atlasManager.GetElementWithName("STR_PROJ"); 
        }

    }












    // 懂了，这个函数只有在playerguide身上被调用
    // 修改playerguide的current concern 并且让它投屏
    private static void OverseerCommunicationModule_ReevaluateConcern(On.OverseerCommunicationModule.orig_ReevaluateConcern orig, OverseerCommunicationModule self, Player player)
    {
        bool flag = false;
        if (self.player.room != null && self.player.room.game.IsStorySession && self.player.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName
            && !self.overseerAI.overseer.forceShelterNeed && self.room.abstractRoom.name == "SL_AI" && self.overseerAI.overseer.PlayerGuide)
        {
            OverseerCommunicationModule.PlayerConcern concern = self.currentConcern;
            Oracle oracle = null;
            for (int j = 0; j < self.room.physicalObjects.Length; j++)
            {
                for (int k = 0; k < self.room.physicalObjects[j].Count; k++)
                {
                    if (self.room.physicalObjects[j][k] is Oracle)
                    {
                        oracle = (self.room.physicalObjects[j][k] as Oracle);
                        break;
                    }
                }
                if (oracle != null)
                {
                    break;
                }
            }

            if (oracle != null && oracle.oracleBehavior is SLOracleBehaviorHasMark)
            {
                SLOracleBehaviorHasMark Behavior = oracle.oracleBehavior as SLOracleBehaviorHasMark;
                // 不是，这是什么脑瘫代码。我复制一下原代码粘在这里。一整个绷不住了。
                // if (Behavior.State.playerEncounters <= 1 && !Behavior.protest && Behavior.protest)

                if (Behavior.State.playerEncounters <= 1 && !Behavior.protest)
                {
                    self.currentConcern = OverseerCommunicationModule.PlayerConcern.ShowPlacedScene;
                    self.currentConcernWeight = 1f;
                    // Plugin.Log("current concern changed", self.currentConcern, self.currentConcernWeight);
                    flag = true;
                    self.overseerAI.overseer.TryAddHologram(OverseerHologram.Message.GateScene, player, 100f);

                    // 我服了爸爸 合着你这监视者投屏和message写的啥完全没关系啊
                }
            }

        }
        if (!flag) { orig(self, player); }


        // Plugin.Log("pos:", self.overseerAI.overseer.mainBodyChunk.pos, "ownerIterator:", (self.overseerAI.overseer.abstractCreature.abstractAI as OverseerAbstractAI).ownerIterator, "currconcern:", self.currentConcern, self.currentConcernWeight);
    }









}


