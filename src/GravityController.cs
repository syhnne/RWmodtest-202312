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







// 重力控制：什么质量稀释电池，简直弱爆了好不好
// 以后还是把他改成，解锁结局后才能全局操控重力，或者是直接禁用。现在我先不改，太好玩了，我先玩
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
    public bool lastRoomHasEffect = true;
    public bool RoomHasEffect = true;

    public GravityController(Player player)
    {
        this.player = player;
        unlocked = (player.room.game.session is StoryGameSession && player.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName && player.room.game.GetStorySession.saveState.deathPersistentSaveData.altEnding) || Plugin.DevMode;

    }


    // 见过屎山代码吗，如果你没见过，现在你见过了
    public override void Update(bool eu)
    {
        // 这个房间我搞不定。SS_AI那里回头再单独写。。。
        if (!enabled || player.room.abstractRoom.name == "SS_E08") return;
        if (!player.room.abstractRoom.name.StartsWith("SS") && !unlocked) return;

        base.Update(eu);
        // 哼哼啊啊啊啊啊啊啊啊
        if (player.room.abstractRoom.name == "SS_AI")
        {

        }
        // 这就是我不懂了，他这儿的effect amount还不是真正的重力，他加了个插值，他为什么要加，我真是一点也想不明白，这除了导致我修三个小时bug以外还有什么别的用处吗
        // 但是他这重力效果和室内灯光还是绑定的，我既不能访问这个AntiGravity的实例，又不能直接把它删了，我真的谢
        else if (player.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.ZeroG) != null)
        {
            if (gravityBonus != (int)Mathf.Round((1f - Mathf.InverseLerp(0f, 0.85f, 1f - player.room.gravity)) * 10f))
            {
                Plugin.LogStat("gravity mismatch IN ZEROG AREA");
                Plugin.LogStat("-- room gravity: ", player.room.gravity);
                gravityBonus = (int)Mathf.Round((1f - Mathf.InverseLerp(0f, 0.85f, 1f - player.room.gravity)) * 10f);
            }
        }
        else if (player.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG) != null)
        {
            if (gravityBonus != (int)Mathf.Round((1f - Mathf.InverseLerp(0f, 0.85f, 1f - player.room.gravity)) * 10f)
            && gravityBonus != (int)Mathf.Round(10f * 1f - player.room.gravity))
            {
                Plugin.LogStat("gravity mismatch IN BROKEN ZEROG AREA");
                Plugin.LogStat("-- room gravity: ", player.room.gravity);
                gravityBonus = (int)Mathf.Round(10f * player.room.gravity);
            }
        }
        else if (gravityBonus != (int)Mathf.Round(10f * player.room.gravity))
        {
            Plugin.LogStat("gravity mismatch or coop player changing gravity: ");
            Plugin.LogStat("-- room gravity: ", player.room.gravity);
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

                    if (player.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.ZeroG) != null || player.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG) != null || player.room.abstractRoom.name == "SS_AI")
                    {
                        Plugin.LogStat("HAS GRAVITY EFFECT");
                        // 如果有类似效果，由于我猜这两效果不能大于1，所以还得钳制范围
                        if (gravityBonus <= 10)
                        {
                            if (player.room.abstractRoom.name == "SS_AI") { }
                            else
                            {
                                player.room.gravity = 1f - Mathf.Lerp(0f, 0.85f, 1f - gravityBonus * 0.1f);
                                // 找到并修改zeroG这个效果。roomeffects竟然没有一个能让我直接找到对应效果的函数，还得我自己写for循环……
                                for (int i = 0; i < player.room.roomSettings.effects.Count; i++)
                                {
                                    if (player.room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.ZeroG || player.room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.BrokenZeroG)
                                    {
                                        player.room.roomSettings.effects[i].amount = 0.1f * (10 - gravityBonus);
                                        Plugin.LogStat("zeroG amount: ", player.room.roomSettings.effects[i].amount);
                                    }
                                }
                            }
                            

                        }

                        else { gravityBonus = 10; }

                    }
                    // 由于颜色显示有上限，所以再加个钳制……
                    // 没事了，颜色显示的方案失效了，我在想要不要把这个钳制去了，让闲的没事的人试试整个屏幕全是圆圈的感觉
                    else if (gravityBonus <= 80)
                    {
                        player.room.gravity = 0.1f * gravityBonus;
                    }
                    else { gravityBonus = 80; }

                }
                else { gravityBonus = 0; }

                Plugin.LogStat("player gravity control RESULT" + player.room.gravity);
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
                Plugin.LogStat("WARNING!!!!!!!   gravity effect != 1f");
            }
            if (gravityBonus <= 10)
            {
                room.realizedRoom.gravity = 0.1f * gravityBonus;
                Plugin.LogStat("LoadRoomUpdate to: ", room.realizedRoom.gravity);
                for (int i = 0; i < room.realizedRoom.roomSettings.effects.Count; i++)
                {
                    if (room.realizedRoom.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.ZeroG
                        || room.realizedRoom.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.BrokenZeroG)
                    {
                        room.realizedRoom.roomSettings.effects[i].amount = 0.1f * (10 - gravityBonus);
                        Plugin.LogStat("LoadRoomUpdate has effect, amount:", amountZeroG, " -to- ", room.realizedRoom.roomSettings.effects[i].amount);
                        break;
                    }
                }
            }
            else
            {
                Plugin.LogStat("LoadRoomUpdate: gravityBonus out of range");
            }

        }
        else
        {
            room.realizedRoom.gravity = gravityBonus * 0.1f;
        }
    }



    public void NewRoom()
    {
        lastRoomHasEffect = RoomHasEffect;
        if (!enabled || !unlocked)
        {
            if (player.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.ZeroG) != null || player.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG) != null || player.room.abstractRoom.name == "SS_AI")
            {
                RoomHasEffect = true;
                gravityBonus = (int)Mathf.Round((1f - Mathf.InverseLerp(0f, 0.85f, 1f - player.room.gravity)) * 10f);
            }
            else 
            { 
                RoomHasEffect = false;
                gravityBonus = (int)Mathf.Round(10f * player.room.gravity); 
            }
            return;
        }
        if (player.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.ZeroG) != null || player.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG) != null || player.room.abstractRoom.name == "SS_AI")
        {
            RoomHasEffect = true;
            if (gravityBonus <= 10)
            {
                // 防止从其他地方传送进五卵石内部的时候，由于携带了原本的重力而摔死
                if (!lastRoomHasEffect || player.room.abstractRoom.name == "SS_AI") { return; }
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
                        Plugin.LogStat("room effect set - newroom - z, amount:", amountZeroG, " -to- ", player.room.roomSettings.effects[i].amount);
                        z = true;
                        break;
                    }
                    else if (player.room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.BrokenZeroG)
                    {
                        amountBrokenZeroG = player.room.roomSettings.effects[i].amount;
                        player.room.roomSettings.effects[i].amount = 0.1f * (10 - gravityBonus);
                        Plugin.LogStat("room effect set - newroom - b, amount:", amountBrokenZeroG, " -to- ", player.room.roomSettings.effects[i].amount);
                        b = true;
                        break;
                    }
                }
                if (!z) amountZeroG = 0f;
                if (!b) amountBrokenZeroG = 0f;
                Plugin.LogStat("NewRoom ! z,b: ", amountZeroG, amountBrokenZeroG);
            }
            else
            {
                Plugin.LogStat("gravityBonus out of range, cleared");
                gravityBonus = (int)Mathf.Round(10f * player.room.gravity);
            }
        }
        else
        {
            RoomHasEffect = false;
            if (lastRoomHasEffect) { return; }
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
                    Plugin.LogStat("room effect set to original value because player died - ZeroG ", player.room.roomSettings.effects[i].amount);
                    break;
                }
                else if (player.room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.BrokenZeroG)
                {
                    player.room.roomSettings.effects[i].amount = loadRoomBefore ? 1f : amountBrokenZeroG;
                    Plugin.LogStat("room effect set to original value because player died - BrokenZeroG ", player.room.roomSettings.effects[i].amount);
                    break;
                }
            }
        }
        else
        {
            player.room.gravity = 1f;
            gravityBonus = 10;
            Plugin.LogStat("gravity set to 1.0 because player died");
        }

    }


    public override void Destroy()
    {
        this.Die();
        base.Destroy();
    }






}



// 这个东西其实不是很好看。首先我不会写那种丝滑的显示效果，我怕我一写他就坏掉。
// 其次，我尝试过通过修改圆圈的颜色来区分不同的重力倍数，但他最后只会给我显示成黑白灰，我不知道为什么。
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
        if (owner.RoomHasEffect && gravityLevel != 0 && gravityInt == 0)
        {
            gravityLevel--;
            gravityInt = 10;
        }

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
            circles[i].thickness = Math.Min(5f, circles[i].thickness + 1f);
        }
        for (int i = gravityInt; i < circles.Length; i++)
        {
            circles[i].thickness = Math.Max(1f, circles[i].thickness - 1f);
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
