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
internal class OverseerHolograms_
{


    public static void Apply()
    {
        On.OverseerCommunicationModule.ReevaluateConcern += OverseerCommunicationModule_ReevaluateConcern;
        // On.Overseer.TryAddHologram += OverSeer_TryAddHologram;

        On.OverseerHolograms.OverseerImage.HoloImage.Update += HoloImage_Update;
        // On.OverseerHolograms.OverseerImage.HoloImage.ctor += HoloImage_ctor;
        // On.OverseerHolograms.OverseerImage.HoloImage.InitiateSprites += HoloImage_InitiateSprites;

        On.OverseerHolograms.OverseerImage.HoloImage.DrawSprites += HoloImage_DrawSprites;
        // On.OverseerHolograms.OverseerImage.ctor += OverSeerImage_ctor;

        // On.OverseerAbstractAI.AbstractBehavior += OverseerAbstractAI_AbstractBehavior;
        // On.Room.Loaded += Room_Loaded;

        On.AbstractRoom.RealizeRoom += AbstractRoom_RealizeRoom;

    }





    private static void AbstractRoom_RealizeRoom(On.AbstractRoom.orig_RealizeRoom orig, AbstractRoom self, World world, RainWorldGame game)
    {
        orig(self, world, game);
        if (self.realizedRoom == null || !game.IsStorySession || game.GetStorySession.saveStateNumber != Plugin.SlugcatStatsName) { return; }
        if (self.name == "SL_AI")
        {

            
            try
            {
                PlacedObject pos = new(PlacedObject.Type.ProjectedImagePosition, null);
                pos.pos = new Vector2(1401, 319);


                RoomSettings settings = self.realizedRoom.roomSettings;
                EventTrigger trigger = new EventTrigger(EventTrigger.TriggerType.Spot);
                TriggeredEvent projEvent = new TriggeredEvent(TriggeredEvent.EventType.ShowProjectedImageEvent);
                // 这个after encounter决定了投影是月姐说完话之前还是之后放。我觉得之后比较好，但我不知道改了月姐行为之后这个时间会不会跟着变，先这样吧
                (projEvent as ShowProjectedImageEvent).afterEncounter = true;
                (projEvent as ShowProjectedImageEvent).onlyWhenShowingDirection = false;
                (projEvent as ShowProjectedImageEvent).fromCycle = 0;
                trigger.tEvent = projEvent;
                trigger.multiUse = true;
                trigger.activeToCycle = -1;
                trigger.activeFromCycle = 0;
                trigger.delay = 0;
                trigger.panelPosition = new Vector2(1401, 319);
                trigger.fireChance = 1;
                trigger.entrance = 1;
                trigger.karma = 0;
                Plugin.Log("trigger event added");
                settings.triggers.Add(trigger);
                settings.placedObjects.Add(pos);
            }
            catch (Exception e) { Plugin.Logger.LogError(e); }
        }

    }


    private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
    {
        orig(self);

        if (self.abstractRoom.name == "SL_AI" && self.game.IsStorySession && self.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            Plugin.Log("Room_Loaded: SL_AI add projectedimageposition");
            // self.AddObject(new PlacedObject(PlacedObject.Type.ProjectedImagePosition, new PlacedObject.Data()));
        }
        
        // 在这里加入roomspecificscript。救命。没人讲过开发者工具怎么用，结果我就把那个SS_AI房间的什么着色效果给搞坏了，现在每次进去都会被橙色闪瞎狗眼。。
        // 好了，修好了。开发者工具是在world文件夹对应的房间里面创建了一个新的文件，把那个删了就恢复正常了。。

    }








    // TODO: 写个函数在房间里放一个projected image position

    private static void HoloImage_Update(On.OverseerHolograms.OverseerImage.HoloImage.orig_Update orig, OverseerImage.HoloImage self)
    {
        orig(self);
        Plugin.Log("pos:", self.hologram.overseer.mainBodyChunk.pos, "ownerIterator:", (self.hologram.overseer.abstractCreature.abstractAI as OverseerAbstractAI).ownerIterator, "holoimage:", self.imageOwner.CurrImage );
    }



    










    private static void OverSeerImage_ctor(On.OverseerHolograms.OverseerImage.orig_ctor orig, OverseerImage self, Overseer overseer, OverseerHologram.Message message, Creature communicateWith, float importance)
    {
        orig(self, overseer, message, communicateWith, importance);
        if (overseer.room != null && overseer.room.game.IsStorySession && overseer.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName
            && overseer.room.abstractRoom.name == "SL_AI")
        {
            for (int i = 0; i < overseer.room.roomSettings.placedObjects.Count; i++)
            {
                if (overseer.room.roomSettings.placedObjects[i].type == PlacedObject.Type.ProjectedImagePosition)
                {
                    self.showPos = overseer.room.roomSettings.placedObjects[i];
                    break;
                }
            }
            if (overseer.AI.communication == null) return;


            self.images = new List<OverseerImage.ImageID>
            {
                Image_SS_AIStart,
                Image_SS_labSlugcat,
                Image_HoldingNeuron,
                Image_Sleep,
                Image_EnergyCell,
                Image_Diving,
                Image_SlugcatStun,
                Image_DLL_1,
                Image_DLL_2
            };
            self.timeOnEachImage = 20;
            self.showTime = 180;
            self.holoImagePart = new OverseerImage.HoloImage(self, self.totalSprites, self);
            self.AddPart(self.holoImagePart);
            self.AddPart(new OverseerImage.Frame(self, self.totalSprites));
            Plugin.Log("OverSeerImage_ctor: custom image added");

        }
    }












    // 好好好，我懂了。把这个材质稍微改一下，然后把我要放的那几张图，放在原材质那几张图的位置，就成了。
    private static void HoloImage_DrawSprites(On.OverseerHolograms.OverseerImage.HoloImage.orig_DrawSprites orig, OverseerImage.HoloImage self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 partPos, Vector2 headPos, float useFade, float popOut, Color useColor)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos, partPos, headPos, useFade, popOut, useColor);
        if (self.imageOwner != null && self.hologram != null && self.hologram.room != null && self.hologram.room.game.IsStorySession && self.hologram.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName && self.hologram.room.abstractRoom.name == "SL_AI")
        {
            sLeaser.sprites[self.firstSprite].element = Futile.atlasManager.GetElementWithName("overseerHolograms/PebblesSlugHologram");
            Plugin.Log("HoloImage_DrawSprites: atlas added");
        }
        else if (self.imageOwner != null && self.hologram != null && self.hologram.room != null && self.hologram.room.game.IsStorySession && self.hologram.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName && self.hologram.room.abstractRoom.name == "SL_A15") 
        { 
            sLeaser.sprites[self.firstSprite].element = Futile.atlasManager.GetElementWithName("STR_PROJ"); 
        }

    }








    private static void OverSeer_TryAddHologram(On.Overseer.orig_TryAddHologram orig, Overseer self, OverseerHologram.Message message, Creature communicateWith, float importance)
    {
        

        if (communicateWith != null && communicateWith is Player && (communicateWith as Player).slugcatStats.name == Plugin.SlugcatStatsName)
        {
            if (self.dead)
            {
                return;
            }
            if (self.hologram != null)
            {
                if (self.hologram.message == message)
                {
                    return;
                }
                if (self.hologram.importance >= importance && importance != 3.4028235E+38f)
                {
                    return;
                }
                self.hologram.stillRelevant = false;
                self.hologram = null;
            }
            if (self.room != null)
            {
                if (message == OverseerHologram.Message.Shelter)
                {
                    self.hologram = new OverseerHologram.ShelterPointer(self, message, communicateWith, importance);
                }
                else if (message == OverseerHologram.Message.DangerousCreature)
                {
                    self.hologram = new OverseerHologram.CreaturePointer(self, message, communicateWith, importance);
                }
                else if (message == OverseerHologram.Message.ProgressionDirection)
                {
                    self.hologram = new OverseerHologram.DirectionPointer(self, message, communicateWith, importance);
                }
                else if (message == OverseerHologram.Message.ForcedDirection)
                {
                    self.hologram = new OverseerHologram.ForcedDirectionPointer(self, message, communicateWith, importance);
                }


                // 在这里加载图像
                else if (message == MeetMoon_1)
                {
                    // Plugin.Log("overseer message: MeetMoon_1");
                    // self.hologram = new OverseerImage(self, OverseerHologram.Message.GateScene, communicateWith, importance);
                    self.hologram = new OverseerImage(self, message, communicateWith, importance);
                    
                }


                if (ModManager.MSC && message == MoreSlugcatsEnums.OverseerHologramMessage.Advertisement)
                {
                    OverseerImage overseerImage = new OverseerImage(self, message, communicateWith, importance);
                    overseerImage.setAdvertisement();
                    self.hologram = overseerImage;
                }
                // self.room.AddObject(self.hologram);
                if (self.room.abstractRoom.name.StartsWith("SL") && self.room.abstractRoom.name != "SL_AI") return;
            }

        }
        
        // 确认了 这个message确实跟投屏内容毫无关系 骗我改了一下午
        // Plugin.Log("message(before):", message, importance);
        orig(self, message, communicateWith, importance);
        // Plugin.Log("message(after):", message, importance);
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
                    Plugin.Log("current concern changed", self.currentConcern, self.currentConcernWeight);
                    flag = true;
                    self.overseerAI.overseer.TryAddHologram(OverseerHologram.Message.GateScene, player, 100f);

                    // 我服了爸爸 合着你这监视者投屏和message写的啥完全没关系啊
                }
            }

        }
        if (!flag) { orig(self, player); }


        // Plugin.Log("pos:", self.overseerAI.overseer.mainBodyChunk.pos, "ownerIterator:", (self.overseerAI.overseer.abstractCreature.abstractAI as OverseerAbstractAI).ownerIterator, "currconcern:", self.currentConcern, self.currentConcernWeight);
    }








    public static OverseerHologram.Message MeetMoon_1;


    public static OverseerImage.ImageID Image_SS_AIStart;
    public static OverseerImage.ImageID Image_SS_labSlugcat;
    public static OverseerImage.ImageID Image_HoldingNeuron;
    public static OverseerImage.ImageID Image_Sleep;
    public static OverseerImage.ImageID Image_EnergyCell;
    public static OverseerImage.ImageID Image_Diving;
    public static OverseerImage.ImageID Image_SlugcatStun;
    public static OverseerImage.ImageID Image_DLL_1;
    public static OverseerImage.ImageID Image_DLL_2;

}


