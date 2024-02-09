using HUD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static PebblesSlug.PlayerHooks;
using RWCustom;
using Random = UnityEngine.Random;
using BepInEx;

namespace PebblesSlug;

/// <summary>
/// 打完结局后解锁的控制台，可以让你使用一些迭代器特有的奇妙能力（？
/// </summary>


// 我要是真把这玩意儿做出来了，我就要请自己吃一顿大餐

// 这个玩意儿挂在oracle那里，而不是在player身上。这需要游戏里必须有一只本模组的猫（拜托。不会有人玩模组猫不用模组猫吧。不会吧不会吧。
public class SSOracleConsole : UpdatableAndDeletable
{

    public Player player;
    public Oracle oracle;
    public bool isActive = false;
    public bool lastIsActive = false;
    public SSOracleBehavior behavior;
    public Vector2 destination;
    private static int moveSpeed = 2;
    public SSOracleConsoleHUD hud;

    public SSOracleConsole(Oracle oracle) 
    { 
        if (oracle.ID != Oracle.OracleID.SS) return;
        this.oracle = oracle;
        this.behavior = oracle.oracleBehavior as SSOracleBehavior;
        if (oracle.room.game.FirstAlivePlayer.realizedCreature != null)
        {
            bool findPlayer = false;
            foreach (AbstractCreature player in oracle.room.game.Players)
            {
                if (player.realizedCreature is Player && (player.realizedCreature as Player).slugcatStats.name == Plugin.SlugcatStatsName)
                {
                    Plugin.Log("SSOracleConsole - owner player: ", (player.realizedCreature as Player).slugcatStats.name.value);
                    this.player = player.realizedCreature as Player;
                    findPlayer = true;
                    break;
                }
            }
            if (!findPlayer)
            {
                Plugin.Log("SSOracleConsole - WARNING: pebbles slugcat not in game");
                return;
            }
            bool getModule = Plugin.playerModules.TryGetValue(player, out var module) && module.playerName == Plugin.SlugcatStatsName;
            if (getModule)
            {
                module.console = this;
            }
        }
        destination = new Vector2(350, 350);
    }








    public override void Update(bool eu)
    {
        // 离开房间自动关闭在Player_NewRoom那里
        if (player == null || player.room == null || player.room.abstractRoom.name != "SS_AI") return;
        Vector2 vector = Vector2.zero;
        if (oracle.room != null)
        {
            vector = oracle.room.game.cameras[0].pos;
            vector.x += 170;
        }
        destination = player.mainBodyChunk.pos - vector;
        behavior.movementBehavior = SSOracleBehavior.MovementBehavior.Meditate;
        if (!isActive) 
        {
            behavior.floatyMovement = true;
            return; 
        }
        



        base.Update(eu);
        // 准备让他别动了 不然好鬼畜（
        /*behavior.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
        behavior.floatyMovement = false;
       
        behavior.SetNewDestination(destination);
        behavior.currentGetTo = destination;*/
        behavior.lookPoint = destination;

        
        

    }





    // 进房间时显示食物（就像进庇护所一样
    public void Enter()
    {
        if (player.room == null || player.room.abstractRoom.name != "SS_AI") return;

        if (!player.room.game.cameras[0].hud.showKarmaFoodRain)
        {
            player.showKarmaFoodRainTime = 40;
        }
    }












    public override void Destroy()
    {
        base.Destroy();
        hud = null;
    }



}

























public class SSOracleConsoleHUD : HudPart
{
    private readonly SSOracleConsole owner;
    private HUDCircle dstCircle;
    public float fade;
    public float lastFade;
    public Vector2 pos;
    public Vector2 lastPos;

    public SSOracleConsoleHUD(HUD.HUD hud, FContainer fContainer, SSOracleConsole owner) : base(hud)
    {
        this.owner = owner;
        owner.hud = this;
        dstCircle = new HUDCircle(hud, HUDCircle.SnapToGraphic.smallEmptyCircle, fContainer, 0)
        {
            visible = true,
            rad = 40f,
            thickness = 2f
        };
        dstCircle.sprite.isVisible = true;
    }

    private bool Show
    {
        get
        {
            return (owner != null && owner.isActive && owner.player != null && owner.player.room != null && owner.player.room.abstractRoom.name == "SS_AI");
        }
    }


    public override void Update()
    {
        if (owner == null) return;
        base.Update();
        lastPos = pos;
        lastFade = fade;
        if (Show)
        {
            fade = Mathf.Min(1f, fade + 0.066666666f);
        }
        else
        {
            fade = Mathf.Max(0f, fade - 0.1f);
        }
        dstCircle.Update();
        dstCircle.fade = fade;
        dstCircle.pos = owner.destination;
        dstCircle.pos.x += 170;
        
    }


    public override void Draw(float timeStacker)
    {
        if (hud.rainWorld.processManager.currentMainLoop is not RainWorldGame) return;
        if (owner == null) return;
        dstCircle.Draw(timeStacker);
    }

    public Vector2 DrawPos(float timeStacker)
    {
        return Vector2.Lerp(lastPos, pos, timeStacker);
    }



    public override void ClearSprites()
    {
        base.ClearSprites();
        dstCircle = null;
    }

}
















public class OracleModule
{
    public readonly WeakReference<Oracle> oracleRef;
    public SSOracleConsole console;
    public readonly SlugcatStats.Name ownerSlugcatName = Plugin.SlugcatStatsName;


    public OracleModule(Oracle oracle)
    {
        oracleRef = new WeakReference<Oracle>(oracle);
        console = new SSOracleConsole(oracle);
        if (console != null && oracle.room.world.game != null && oracle.room.world.game.cameras != null && oracle.room.world.game.cameras[0].hud != null)
        {
            oracle.room.world.game.cameras[0].hud.AddPart(new SSOracleConsoleHUD(oracle.room.world.game.cameras[0].hud, oracle.room.world.game.cameras[0].hud.fContainers[1], console));
            Plugin.Log("oracleconsole HUD owner: ", oracle.room.world.game.cameras[0].hud.owner);
        }
        else { Plugin.Log("oracleconsole HUD NOT FOUND !!!"); }

    }
}