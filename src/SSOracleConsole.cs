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
        if (player == null || player.room == null || !isActive || player.room.abstractRoom.name != "SS_AI") return;
        base.Update(eu);
        behavior.movementBehavior = SSOracleBehavior.MovementBehavior.Meditate;
        behavior.floatyMovement = false;
        Vector2 vector = Vector2.zero;
        if (oracle.room != null)
        {
            vector = oracle.room.game.cameras[0].pos;
            vector.x += 170;
        }
        destination = player.mainBodyChunk.pos - vector;
        behavior.SetNewDestination(destination);
        behavior.currentGetTo = destination;
        behavior.lookPoint = destination;

        
        

    }












    // 纯属复制游戏代码，巧的，除了把private改成public之外一个字都没改
    public Vector2 ClampVectorInRoom(Vector2 v)
    {
        Vector2 vector = v;
        vector.x = Mathf.Clamp(vector.x, this.oracle.arm.cornerPositions[0].x + 10f, this.oracle.arm.cornerPositions[1].x - 10f);
        vector.y = Mathf.Clamp(vector.y, this.oracle.arm.cornerPositions[2].y + 10f, this.oracle.arm.cornerPositions[1].y - 10f);
        return vector;
    }



    public override void Destroy()
    {
        base.Destroy();
        hud = null;
    }



}









public class SSOracleConsoleHUD : HudPart
{
    private SSOracleConsole owner;
    private HUDCircle dstCircle;
    public float fade;
    public float lastFade;
    public Vector2 pos;
    public Vector2 lastPos;

    public SSOracleConsoleHUD(HUD.HUD hud, FContainer fContainer, SSOracleConsole owner) : base(hud)
    {
        this.owner = owner;
        owner.hud = this;
        dstCircle = new HUDCircle(hud, HUDCircle.SnapToGraphic.smallEmptyCircle, fContainer, 0);
        dstCircle.visible = Plugin.DevMode;
        dstCircle.fade = 1f;
        dstCircle.rad = 20f;
        dstCircle.thickness = 2f;
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
        if (owner == null || !owner.isActive) return;
        base.Update();
        lastPos = pos;
        lastFade = fade;


        // 做完player input manager之后，获取owner的输入


        dstCircle.Update();
        dstCircle.visible = Show;
        dstCircle.pos = owner.destination;
        dstCircle.pos.x += 170;
        dstCircle.fade = 1f;
        dstCircle.rad = 20f;
        dstCircle.thickness = 2f;
    }


    public override void Draw(float timeStacker)
    {
        if (hud.rainWorld.processManager.currentMainLoop is not RainWorldGame) return;
        if (owner == null || !owner.isActive) return;
        dstCircle.Draw(timeStacker);
    }

    public Vector2 DrawPos(float timeStacker)
    {
        return Vector2.Lerp(lastPos, pos, timeStacker);
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
        if (Plugin.instance != null && Plugin.instance.Hud != null)
        {
            Plugin.instance.Hud.AddPart(new SSOracleConsoleHUD(Plugin.instance.Hud, Plugin.instance.Hud.fContainers[1], console));
            Plugin.Log("oracleconsole HUD added successfully!!!");
        }
        else { Plugin.Log("oracleconsole HUD NOT FOUND !!!"); }

    }
}