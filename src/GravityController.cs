using HUD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using JetBrains.Annotations;

namespace PebblesSlug;







// 重力控制：单独绑了个按键。这个功能刚玩会觉得很鸡肋，但我试了试，低重力让我能够飞行当中一矛命中蜥蜴身体，当场开饭。高重力让我随手召唤秃鹫，单矛随便杀。
// 我开始理解fp为什么会说自己是神了。
// 以后还是把他改成，解锁结局后才能全局操控重力吧。现在我先不改，太好玩了，我先玩
// 特么的，出大问题。。
public class GravityController : UpdatableAndDeletable
{
    public Player player;
    private bool unlocked;
    
    public int gravityControlCounter = 0;
    public int gravityBonus = 10;
    private static readonly int gravityControlTime = 12;
    public float amountZeroG;
    public float amountBrokenZeroG;
    public bool enabled = true;
    private readonly bool loadRoomBefore = false;
    public Dictionary<RoomRealizer.RealizedRoomTracker, float> realizedRoomGravity;
    public bool isAbleToUse = false;


    public GravityController(Player player)
    {
        this.player = player;
        unlocked = player.room.game.session is StoryGameSession && player.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName && player.room.game.GetStorySession.saveState.deathPersistentSaveData.altEnding;

    }


    // 见过屎山代码吗，如果你没见过，现在你见过了
    public override void Update(bool eu)
    {
        // 这个房间我搞不定。SS_AI那里回头再单独写。。。
        if (!enabled || player.room.abstractRoom.name == "SS_E08" || player.room.abstractRoom.name == "SS_AI") return;
        // 回头在这里改成altEnding的判定。
        if (!player.room.abstractRoom.name.StartsWith("SS") && !Plugin.GravityControlUnlock && !unlocked) return;

        base.Update(eu);
        // 这就是我不懂了，他这儿的effect amount还不是真正的重力，他加了个插值，他为什么要加，我真是一点也想不明白，这除了导致我修三个小时bug以外还有什么别的用处吗
        // 但是他这重力效果和室内灯光还是绑定的，我既不能访问这个AntiGravity的实例，又不能直接把它删了，我真的谢
        if (player.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.ZeroG) != null)
        {
            if (gravityBonus != (int)Mathf.Round((1f - Mathf.InverseLerp(0f, 0.85f, 1f - player.room.gravity)) * 10f))
            {
                Plugin.Log("gravity mismatch IN ZEROG AREA");
                Plugin.Log("-- room gravity: ", player.room.gravity);
                gravityBonus = (int)Mathf.Round((1f - Mathf.InverseLerp(0f, 0.85f, 1f - player.room.gravity)) * 10f);
            }
        }
        else if (player.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG) != null)
        {
            if (gravityBonus != (int)Mathf.Round((1f - Mathf.InverseLerp(0f, 0.85f, 1f - player.room.gravity)) * 10f)
            && gravityBonus != (int)Mathf.Round(10f * 1f - player.room.gravity))
            {
                Plugin.Log("gravity mismatch IN BROKEN ZEROG AREA");
                Plugin.Log("-- room gravity: ", player.room.gravity);
                gravityBonus = (int)Mathf.Round(10f * player.room.gravity);
            }
        }
        else if (gravityBonus != (int)Mathf.Round(10f * player.room.gravity))
        {
            Plugin.Log("gravity mismatch or coop player changing gravity: ");
            Plugin.Log("-- room gravity: ", player.room.gravity);
            gravityBonus = (int)Mathf.Round(10f * player.room.gravity);
        }

        if (player.Consious && !player.dead && player.stun == 0
            && Input.GetKey(Plugin.instance.option.GravityControlKey.Value)
            && player.bodyMode != Player.BodyModeIndex.CorridorClimb && player.animation != Player.AnimationIndex.HangFromBeam && player.animation != Player.AnimationIndex.ClimbOnBeam && player.bodyMode != Player.BodyModeIndex.WallClimb && player.animation != Player.AnimationIndex.AntlerClimb && player.animation != Player.AnimationIndex.VineGrab && player.animation != Player.AnimationIndex.ZeroGPoleGrab && player.onBack == null)
        {
            player.Blink(5);
            isAbleToUse = true;
        }
        else { isAbleToUse = false; }

        if (isAbleToUse
            && player.input[0].y != 0 )
        {
            gravityControlCounter++;
            if (gravityControlCounter >= gravityControlTime)
            {
                gravityBonus += player.input[0].y;
                player.input[0].y = 0;
                if (gravityBonus >= 0)
                {

                    if (player.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.ZeroG) != null || player.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG) != null)
                    {
                        Plugin.Log("HAS GRAVITY EFFECT");
                        // 如果有类似效果，由于我猜这两效果不能大于1，所以还得钳制范围
                        if (gravityBonus <= 10)
                        {
                            player.room.gravity = 1f - Mathf.Lerp(0f, 0.85f, 1f - gravityBonus * 0.1f);
                            // 找到并修改zeroG这个效果。roomeffects竟然没有一个能让我直接找到对应效果的函数，还得我自己写for循环……
                            for (int i = 0; i < player.room.roomSettings.effects.Count; i++)
                            {
                                if (player.room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.ZeroG || player.room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.BrokenZeroG)
                                {
                                    player.room.roomSettings.effects[i].amount = 0.1f * (10 - gravityBonus);
                                    Plugin.Log("zeroG amount: ", player.room.roomSettings.effects[i].amount);
                                }
                            }

                        }

                        else { gravityBonus = 10; }

                    }
                    // 由于颜色显示有上限，所以再加个钳制……
                    else if (gravityBonus <= 80)
                    {
                        player.room.gravity = 0.1f * gravityBonus;
                    }
                    else { gravityBonus = 80; }

                }
                else { gravityBonus = 0; }

                Plugin.Log("player gravity control RESULT" + player.room.gravity);
                gravityControlCounter = 0;
            }
        }
    }



    public void KillRoom(AbstractRoom room)
    {

    }


    public void LoadRoomUpdate(AbstractRoom room)
    {
        if (!enabled || !loadRoomBefore || !unlocked || room.realizedRoom == null) return;

        if (room.realizedRoom.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.ZeroG) != null
            || room.realizedRoom.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG) != null)
        {
            // 这部分我不是很懂了，因为我懒得写存储。不是1的我可就不管了（目移
            if (room.realizedRoom.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.ZeroG) != 1f
                || room.realizedRoom.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.BrokenZeroG) != 1f)
            {
                Plugin.Log("WARNING!!!!!!!   gravity effect != 1f");
            }
            if (gravityBonus <= 10)
            {
                room.realizedRoom.gravity = 0.1f * gravityBonus;
                Plugin.Log("LoadRoomUpdate to: ", room.realizedRoom.gravity);
                for (int i = 0; i < room.realizedRoom.roomSettings.effects.Count; i++)
                {
                    if (room.realizedRoom.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.ZeroG
                        || room.realizedRoom.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.BrokenZeroG)
                    {
                        room.realizedRoom.roomSettings.effects[i].amount = 0.1f * (10 - gravityBonus);
                        Plugin.Log("LoadRoomUpdate has effect, amount:", amountZeroG, " -to- ", room.realizedRoom.roomSettings.effects[i].amount);
                        break;
                    }
                }
            }
            else
            {
                Plugin.Log("LoadRoomUpdate: gravityBonus out of range");
            }

        }
        else
        {
            room.realizedRoom.gravity = gravityBonus * 0.1f;
        }
    }



    public void NewRoom()
    {
        if (!enabled || !unlocked)
        {
            if (player.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.ZeroG) != null || player.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG) != null)
            {
                gravityBonus = (int)Mathf.Round((1f - Mathf.InverseLerp(0f, 0.85f, 1f - player.room.gravity)) * 10f);
            }
            else { gravityBonus = (int)Mathf.Round(10f * player.room.gravity); }
            return;
        }
        if (player.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.ZeroG) != null || player.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG) != null)
        {
            if (gravityBonus <= 10)
            {
                player.room.gravity = 0.1f * gravityBonus;
                // 找到并修改zeroG这个效果
                bool z = false;
                bool b = false;
                for (int i = 0; i < player.room.roomSettings.effects.Count; i++)
                {
                    if (player.room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.ZeroG)
                    {
                        amountZeroG = player.room.roomSettings.effects[i].amount;
                        player.room.roomSettings.effects[i].amount = 0.1f * (10 - gravityBonus);
                        Plugin.Log("room effect set - newroom - z, amount:", amountZeroG, " -to- ", player.room.roomSettings.effects[i].amount);
                        z = true;
                        break;
                    }
                    else if (player.room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.BrokenZeroG)
                    {
                        amountBrokenZeroG = player.room.roomSettings.effects[i].amount;
                        player.room.roomSettings.effects[i].amount = 0.1f * (10 - gravityBonus);
                        Plugin.Log("room effect set - newroom - b, amount:", amountBrokenZeroG, " -to- ", player.room.roomSettings.effects[i].amount);
                        b = true;
                        break;
                    }
                }
                if (!z) amountZeroG = 0f;
                if (!b) amountBrokenZeroG = 0f;
                Plugin.Log("NewRoom ! z,b: ", amountZeroG, amountBrokenZeroG);
            }
            else
            {
                Plugin.Log("gravityBonus out of range, cleared");
                gravityBonus = (int)Mathf.Round(10f * player.room.gravity);
            }

        }
        else
        {
            player.room.gravity = gravityBonus * 0.1f;
        }
    }


    // 其实disable也要调用这个函数，是不是应该给他改个名
    public void Die()
    {
        if (player.room == null || !enabled) return;
        if (player.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.ZeroG) != null || player.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG) != null)
        {
            for (int i = 0; i < player.room.roomSettings.effects.Count; i++)
            {
                if (player.room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.ZeroG)
                {
                    // 我要摆烂了，现在这个地图上的重力应该不是0就是1罢（心虚）
                    player.room.roomSettings.effects[i].amount = loadRoomBefore ? 1f : amountZeroG;
                    Plugin.Log("room effect set to original value because player died - ZeroG ", player.room.roomSettings.effects[i].amount);
                    break;
                }
                else if (player.room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.BrokenZeroG)
                {
                    player.room.roomSettings.effects[i].amount = loadRoomBefore ? 1f : amountBrokenZeroG;
                    Plugin.Log("room effect set to original value because player died - BrokenZeroG ", player.room.roomSettings.effects[i].amount);
                    break;
                }
            }
        }
        else
        {
            player.room.gravity = 1f;
            gravityBonus = 10;
            Plugin.Log("gravity set to 1.0 because player died");
        }

    }


    public override void Destroy()
    {
        this.Die();
        base.Destroy();
    }






}




public class GravityMeter : HudPart
{
    private GravityController owner;
    public Vector2 pos;
    public Vector2 lastPos;
    public float fade;
    public float lastFade;
    public HUDCircle[] circles;
    public HUDCircle[] rows;

    public static Color[] GravityMeterColors = new Color[]
        {
            new Color32(255, 255, 255, 255),
            new Color32(190, 255, 231, 255),
            new Color32(85, 243, 239, 255),
            new Color32(6, 164, 219, 255),
            new Color32(5, 29, 243, 255),
            new Color32(27, 5, 142, 255),
            new Color32(206, 3, 180, 255),
            new Color32(255, 0, 0, 255)
        };


    public GravityMeter(HUD.HUD hud, FContainer fContainer, GravityController owner) : base(hud)
    {
        circles = new HUDCircle[10];
        rows = new HUDCircle[7];
        this.owner = owner;
        pos = owner.player.mainBodyChunk.pos - Vector2.zero;
        lastPos = pos;
        fade = 0f;
        lastFade = 0f;
        for (int i = 0; i < circles.Length; i++)
        {
            circles[i] = new HUDCircle(hud, HUDCircle.SnapToGraphic.smallEmptyCircle, fContainer, 0);
            circles[i].sprite.isVisible = true;
            circles[i].rad = 3f;
            circles[i].thickness = 1f;
            circles[i].visible = true;
        }
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = new HUDCircle(hud, HUDCircle.SnapToGraphic.smallEmptyCircle, fContainer, 0);
            rows[i].sprite.isVisible = true;
            rows[i].rad = 40f + (float)i * 3f;
            rows[i].thickness = 1f;
            rows[i].visible = true;
            rows[i].pos = owner.player.mainBodyChunk.pos - Vector2.zero;
        }
    }


    private bool Show
    {
        get
        {
            return (owner != null && owner.isAbleToUse);
        }
    }



    public override void Update()
    {

        base.Update();
        lastPos = pos;
        lastFade = fade;
        Vector2 vector = Vector2.zero;
        if (owner.player.room != null)
        {
            vector = owner.player.room.game.cameras[0].pos;
        }
        pos = owner.player.mainBodyChunk.pos - vector;
        int gravityInt = owner.gravityBonus % 10;
        int gravityLevel = owner.gravityBonus / 10;
        if (gravityLevel >= 7) gravityLevel = 7;
        /*if (gravityLevel != 0 && gravityInt == 0)
        {
            gravityInt = 10;
        }*/

        if (Show)
        {
            fade = Mathf.Min(1f, fade + 0.033333335f);
        }
        else
        {
            fade = Mathf.Max(0f, fade - 0.1f);
        }



        for (int i = 0; i < gravityInt; i++)
        {
            circles[i].thickness = 5f;
        }
        for (int i = gravityInt; i < circles.Length; i++)
        {
            circles[i].thickness = 1f;
        }
        for (int i = 0; i < circles.Length; i++)
        {
            circles[i].Update();
            circles[i].fade = fade;
            circles[i].pos = pos + new Vector2((float)i * 21.6f, 0f);
            circles[i].visible = true;
            circles[i].pos = pos + Custom.DegToVec((1f - (float)i / (float)circles.Length) * 360f * Custom.SCurve(Mathf.Pow(fade, 1.5f - ((float)i / (float)(circles.Length - 1))), 0.6f)) * (32f);
        }

        for (int i = 0; i < gravityLevel; i++)
        {
            rows[i].visible = true;
        }
        for (int i = gravityLevel; i < rows.Length; i++)
        {
            rows[i].visible = false;
        }
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i].Update();
            rows[i].fade = fade;
            rows[i].pos = pos;
        }

    }



    public override void Draw(float timeStacker)
    {
        if (hud.rainWorld.processManager.currentMainLoop is not RainWorldGame) return;
        for (int i = 0; i < circles.Length; i++)
        {
            circles[i].Draw(timeStacker);
        }
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i].Draw(timeStacker);
        }
    }

    public Vector2 DrawPos(float timeStacker)
    {
        return Vector2.Lerp(lastPos, pos, timeStacker);
    }



    public override void ClearSprites()
    {
        base.ClearSprites();
    }
}
