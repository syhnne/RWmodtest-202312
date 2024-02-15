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
        On.OverseerHolograms.OverseerImage.HoloImage.ctor += HoloImage_ctor;
        // On.OverseerHolograms.OverseerImage.HoloImage.InitiateSprites += HoloImage_InitiateSprites;

        // On.OverseerHolograms.OverseerImage.HoloImage.DrawSprites += HoloImage_DrawSprites;
        // On.OverseerHolograms.OverseerImage.ctor += OverSeerImage_ctor;

        // On.OverseerAbstractAI.AbstractBehavior += OverseerAbstractAI_AbstractBehavior;




    }


    private static void OverseerAbstractAI_AbstractBehavior(On.OverseerAbstractAI.orig_AbstractBehavior orig, OverseerAbstractAI self, int time)
    {
        orig(self, time);
        if (self.parent == null || self.parent.realizedCreature == null || self.parent.realizedCreature.room == null) { return; }
        Plugin.Log("pos:", self.parent.realizedCreature.mainBodyChunk.pos, "ownerIterator:", self.ownerIterator, "moonHelper:", self.moonHelper, "guide:", self.playerGuide);

        /*if (self.parent.Room.world.game.IsStorySession && self.parent.Room.world.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName
            && self.moonHelper && self.parent.Room.index != self.parent.Room.world.GetAbstractRoom("SL_AI").index
            && self.parent.realizedCreature.room.game.FirstRealizedPlayer != null && self.parent.realizedCreature.room.game.FirstRealizedPlayer.room != null && self.parent.realizedCreature.room.game.FirstRealizedPlayer.room.abstractRoom.name == "SL_AI")
        {
            Plugin.Log("overseer go to moon chamber");
            self.ResetTargetCreature();
            self.GoToRandomDestinationInMoonChamber(false);
        }*/
        /*if (self.parent.Room.world.game.IsStorySession && self.parent.Room.world.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName
            && self.playerGuide && self.parent.Room.world.game.IsMoonActive() && self.parent.Room.index != self.parent.Room.world.GetAbstractRoom("SL_AI").index)
        {
            self.ResetTargetCreature();
            self.GoToRandomDestinationInMoonChamber(false);
            self.SetTargetCreature(self.RelevantPlayer);
            Plugin.Log("OverseerAbstractAI - go to moon chamber");
        }*/
        /*else if (self.parent.Room.world.game.IsStorySession && self.parent.Room.world.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName
            && self.playerGuide && self.parent.realizedCreature != null && self.parent.Room.name == "SL_AI" && !Custom.InsideRect((int)self.parent.realizedCreature.mainBodyChunk.pos.x, (int)self.parent.realizedCreature.mainBodyChunk.pos.y, new IntRect(1180, 100, 1940, 690)))
        {
            
            self.ResetTargetCreature();
            self.GoToRandomDestinationInMoonChamber(true);
            self.SetTargetCreature(self.RelevantPlayer);
            Plugin.Log("OverseerAbstractAI - in moon chamber");
        }*/
        
    }






    private static void HoloImage_Update(On.OverseerHolograms.OverseerImage.HoloImage.orig_Update orig, OverseerImage.HoloImage self)
    {
        orig(self);
        Plugin.Log("pos:", self.hologram.overseer.mainBodyChunk.pos, "ownerIterator:", (self.hologram.overseer.abstractCreature.abstractAI as OverseerAbstractAI).ownerIterator, "holoimage:", self.imageOwner.CurrImage );
    }



    // 懂了，这个函数只有在playerguide身上被调用
    private static void OverseerCommunicationModule_ReevaluateConcern(On.OverseerCommunicationModule.orig_ReevaluateConcern orig, OverseerCommunicationModule self, Player player)
    {
        bool flag = false;
        if (self.player.room != null && self.player.room.game.IsStorySession && self.player.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {

            if (!self.overseerAI.overseer.forceShelterNeed && self.room.abstractRoom.name == "SL_AI" && self.overseerAI.overseer.PlayerGuide)
            {
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
                        /*self.currentConcern = MMFEnums.PlayerConcern.ProtectMoon;
                        self.currentConcernWeight = 1f;*/
                        self.currentConcern = OverseerCommunicationModule.PlayerConcern.ShowPlacedScene;
                        self.currentConcernWeight = 1f;
                        Plugin.Log("current concern changed ++====");
                        flag = true;
                        self.overseerAI.overseer.TryAddHologram(OverseerHologram.Message.GateScene, player, 100f);
                    }
                }
            }



        }
        if (!flag) { orig(self, player); } 


        Plugin.Log("pos:", self.overseerAI.overseer.mainBodyChunk.pos, "ownerIterator:", (self.overseerAI.overseer.abstractCreature.abstractAI as OverseerAbstractAI).ownerIterator, "currconcern:", self.currentConcern, self.currentConcernWeight );
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













    private static void HoloImage_DrawSprites(On.OverseerHolograms.OverseerImage.HoloImage.orig_DrawSprites orig, OverseerImage.HoloImage self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 partPos, Vector2 headPos, float useFade, float popOut, Color useColor)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos, partPos, headPos, useFade, popOut, useColor);
        /*if (self.imageOwner != null && self.hologram != null && self.hologram.room != null && self.hologram.room.game.IsStorySession && self.hologram.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            sLeaser.sprites[self.firstSprite].element = Futile.atlasManager.GetElementWithName("overseerHolograms/PebblesSlugHologram");
            Plugin.Log("HoloImage_DrawSprites: atlas added");
        }*/

    }


    #region 不行，还是得用这个




    // 修改sprites数量
    // 这个hook有点难挂，我挺想重写一个类的，但那样需要我复制大量update函数里的代码，如果这玩意卡bug卡到我不得不放弃的话我也就只能复制粘贴了
    // 没事了，我成小丑了
    private static void HoloImage_ctor(On.OverseerHolograms.OverseerImage.HoloImage.orig_ctor orig, OverseerImage.HoloImage self, OverseerHologram hologram, int firstSprite, IOwnAHoloImage imageOwner)
    {
        orig(self, hologram, firstSprite, imageOwner);
        Plugin.Log("HoloImage_ctor:", self, hologram, firstSprite, imageOwner);
    }



    private static void HoloImage_InitiateSprites(On.OverseerHolograms.OverseerImage.HoloImage.orig_InitiateSprites orig, OverseerImage.HoloImage self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig(self, sLeaser, rCam);
        if (self.hologram.overseer != null && self.hologram.overseer.room != null && self.hologram.overseer.room.game.IsStorySession && self.hologram.overseer.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            sLeaser.sprites[2] = new FSprite("", true);
        }
    }




    #endregion








    private static void OverSeer_TryAddHologram(On.Overseer.orig_TryAddHologram orig, Overseer self, OverseerHologram.Message message, Creature communicateWith, float importance)
    {
        orig(self, message, communicateWith, importance);

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
                    self.hologram = new OverseerImage(self, message, communicateWith, 1000f);
                    // 喵的 他根本不投屏 我真的栓q 我去抄点水猫那边的代码
                }


                if (ModManager.MSC && message == MoreSlugcatsEnums.OverseerHologramMessage.Advertisement)
                {
                    OverseerImage overseerImage = new OverseerImage(self, message, communicateWith, importance);
                    overseerImage.setAdvertisement();
                    self.hologram = overseerImage;
                }
                self.room.AddObject(self.hologram);
            }

        }
        else
        {

        }
    }




















    // 啊啊啊啊啊啊啊啊啊啊哇啊啊啊啊啊原来这个才是没用的吗 这下已经明显地失败
    #region OverseerHologram

    public class MoonFirstMeetHologram_1 : OverseerHologram, IOwnAHoloImage
    {



        public int CurrImageIndex
        {
            get
            {
                return 0;
            }
        }

        public int ShowTime
        {
            get
            {
                return 0;
            }
        }

        public OverseerImage.ImageID CurrImage
        {
            get
            {
                if (counter <= 70)
                {
                    return Image_SS_AIStart;
                }
                else if (counter <= 90)
                {
                    return (counter % 8 < 4 ? Image_SS_AIStart : Image_SS_labSlugcat);
                }
                else
                {
                    return Image_SS_labSlugcat;
                }
            }
        }

        public float ImmediatelyToContent
        {
            get
            {
                return 1f;
            }
        }

        public OverseerImage.HoloImage image;
        public OverseerImage.Frame frame;
        public int counter;
        public int showTime;


        public MoonFirstMeetHologram_1(Overseer overseer, Message message, Creature communicateWith, float importance) : base(overseer, message, communicateWith, importance)
        {
            image = new OverseerImage.HoloImage(this, totalSprites, this);
            base.AddPart(image);
            frame = new OverseerImage.Frame(this, totalSprites);
            base.AddPart(frame);
        }







        public override void Update(bool eu)
        {
            base.Update(eu);
            counter++;
            (overseer.abstractCreature.abstractAI as OverseerAbstractAI).goToPlayer = true;
            showTime++;
            if (image.myAlpha > 0.9f && image.randomFlicker < 0.1f)
            {
                showTime++;
            }
            if (showTime > 800)
            {
                stillRelevant = false;

            }
        }




        public override float InfluenceHoverScoreOfTile(IntVector2 testPos, float f)
        {
            if (communicateWith == null)
            {
                return f;
            }
            f += Vector2.Distance(room.MiddleOfTile(testPos), communicateWith.DangerPos) * Mathf.Lerp(0.1f, 1.9f, Random.value);
            return f;
        }

        public override float DisplayPosScore(IntVector2 testPos)
        {
            if (communicateWith != null)
            {
                return Vector2.Distance(room.MiddleOfTile(testPos), communicateWith.DangerPos);
            }
            return Random.value;
        }


    }


    #endregion








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



// 绷不住了，这个不要了
#region HoloImage



public class CustomHoloImage : OverseerHologram.HologramPart
{

    public CustomHoloImage(OverseerHologram hologram, int firstSprite, IOwnAHoloImage imageOwner) : base(hologram, firstSprite)
    {
        this.imageOwner = imageOwner;
        totalSprites = 1;
    }



    public int lastImg;
    public IntVector2 lastShowImg;
    public Vector2 panPos;
    public Vector2 lastPanPos;
    public Vector2 panVel;
    public Vector2 panCenter;
    public float myAlpha;
    public float lastMyAlpha;

    public IOwnAHoloImage imageOwner;
    public PositionedSoundEmitter sound;


    // 纯复制粘贴
    public override void Update()
    {
        base.Update();
        if (this.sound == null)
        {
            this.sound = new PositionedSoundEmitter(this.hologram.pos, 0f, 1f);
            this.hologram.room.PlaySound(SoundID.Overseer_Image_LOOP, this.sound, true, 0f, 1f, false);
            this.sound.requireActiveUpkeep = true;
        }
        else
        {
            this.sound.alive = true;
            this.sound.pos = this.hologram.pos;
            this.sound.volume = Mathf.Pow(this.partFade * this.hologram.fade, 0.25f) * this.myAlpha;
            if (this.sound.slatedForDeletetion && !this.sound.soundStillPlaying)
            {
                this.sound = null;
            }
        }
        bool flag = false;
        if (this.imageOwner.CurrImageIndex != this.lastImg)
        {
            this.lastImg = this.imageOwner.CurrImageIndex;
            flag = true;
        }
        IntVector2 intVector = new IntVector2(this.imageOwner.CurrImage.Index, 1);
        if (intVector.x < 0)
        {
            intVector.x = 0;
        }
        if (intVector != this.lastShowImg && this.hologram.fade * this.partFade * this.myAlpha > 0.2f)
        {
            this.hologram.room.PlaySound(flag ? SoundID.Overseer_Image_Big_Flicker : SoundID.Overseer_Image_Small_Flicker, this.hologram.pos, this.hologram.fade * this.partFade * this.myAlpha, 1f);
        }
        this.lastShowImg = intVector;
        this.lastPanPos = this.panPos;
        this.panPos += this.panVel;
        this.panPos = Vector2.Lerp(Vector2.ClampMagnitude(this.panPos, 1f), this.panCenter, 0.05f);
        this.panVel *= 0.8f;
        this.panVel += Custom.RNV() * 0.1f * Random.value * Random.value * Random.value * Random.value;
        this.panVel += (this.panCenter - this.panPos) * 0.01f;
        this.lastMyAlpha = this.myAlpha;
        float num = Mathf.InverseLerp(0.5f, 1f, this.partFade * this.hologram.fade);
        this.myAlpha = Mathf.Min(num, Custom.LerpAndTick(this.myAlpha, num, 0.01f, 0.008333334f));
        this.myAlpha = Mathf.Lerp(this.myAlpha, num, this.imageOwner.ImmediatelyToContent * 0.5f);
        num = Mathf.InverseLerp(1f, 0.5f, this.myAlpha);
        if (this.hologram.overseer.AI.communication != null && this.myAlpha > 0.9f)
        {
            this.hologram.overseer.AI.communication.showedImageTime++;
        }
    }



    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        base.InitiateSprites(sLeaser, rCam);

        try
        {
            sLeaser.sprites[firstSprite] = new FSprite("overseerHolograms/PebblesSlugHologram", true);
            Plugin.Log("Hologram sprite init");
            // sLeaser.sprites[firstSprite].element = Futile.atlasManager.GetElementWithName("overseerHolograms/PebblesSlugHologram");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError(e);
        }

    }




    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 partPos, Vector2 headPos, float useFade, float popOut, Color useColor)
    {
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos, partPos, headPos, useFade, popOut, useColor);

        FSprite sprite = sLeaser.sprites[this.firstSprite];
        if (useFade == 0f)
        {
            sprite.isVisible = false;
            return;
        }
        sprite.isVisible = true;
        partPos = Vector3.Lerp(headPos, partPos, popOut);
        sprite.x = partPos.x - camPos.x;
        sprite.y = partPos.y - camPos.y;
        sprite.shader = rCam.game.rainWorld.Shaders["HologramImage"];
        sprite.element = Futile.atlasManager.GetElementWithName("overseerHolograms/PebblesSlugHologram");


        sprite.rotation = 0f;
        sprite.scaleY = 0.1f * Mathf.Lerp(0.5f, 1f, useFade);
        sprite.scaleX = 0.1f * Mathf.Lerp(0.5f, 1f, useFade);

        sprite.color = new Color(0.5f + 0.5f * Mathf.Lerp(this.lastPanPos.x, this.panPos.x, timeStacker), 0.5f + 0.5f * Mathf.Lerp(this.lastPanPos.y, this.panPos.y, timeStacker), (float)16 / 25f);

        float num2 = Custom.SCurve(Mathf.Pow(useFade, 2f) * Mathf.Lerp(this.lastMyAlpha, this.myAlpha, timeStacker), 0.4f);
        sprite.alpha = num2;


    }

}



#endregion





