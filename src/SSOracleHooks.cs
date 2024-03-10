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





namespace PebblesSlug;



/// <summary>
/// 我尝试给fp洗脑让他相信自己是一只蛞蝓猫，但是现在已经明显地失败啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊
/// </summary>
/// 
/// 讲个笑话：我突然意识到用ssoraclebehavior可以直接从oracle访问到player
/// 所以我压根没必要把这个类挂在oracle上面
/// 绷不住了，我真懒得把这一坨删了重写。。。。
/// 
public class SSOracleHooks
{


    public static void Apply()
    {
        // IL.SSOracleBehavior.Update += IL_SSOracleBehavior_Update;
        On.SSOracleBehavior.ctor += SSOracleBehavior_ctor;
        On.Oracle.ctor += Oracle_ctor;
        On.SSOracleBehavior.SeePlayer += SSOracleBehavior_SeePlayer;
        On.SSOracleBehavior.Update += SSOracleBehavior_Update;
        On.Oracle.Destroy += Oracle_Destroy;
        On.SSOracleBehavior.NewAction += SSOracleBehavior_NewAction;
        // On.OracleBehavior.UnconciousUpdate += OracleBehavior_UnconciousUpdate;
        On.SSOracleBehavior.UnconciousUpdate += SSOracleBehavior_UnconciousUpdate;
        On.SSOracleBehavior.storedPearlOrbitLocation += SSOracleBehavior_storedPearlOrbitLocation;
        On.PebblesPearl.Update += PebblesPearl_Update;

        // IL.Oracle.ctor += IL_Oracle_ctor;
        // On.OracleGraphics.ctor += OracleGraphics_ctor;

        new Hook(
            typeof(Oracle).GetProperty(nameof(Oracle.Consious), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            Oracle_Consious
            );

        /*new Hook(
            typeof(SSOracleBehavior.SubBehavior).GetProperty(nameof(SSOracleBehavior.SubBehavior.LowGravity), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            SSOracleBehavior_SubBehavior_lowGravity
            );*/

        new Hook(
            typeof(SSOracleBehavior).GetProperty(nameof(SSOracleBehavior.EyesClosed), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            SSOracleBehavior_EyesClosed
            );
    }




    #region 结局前


    // 婴儿般的睡眠。jpg
    private delegate bool orig_Consious(Oracle self);
    private static bool Oracle_Consious(orig_Consious orig, Oracle self)
    {
        var result = orig(self);
        if (self.room.game.session is StoryGameSession && self.ID == Oracle.OracleID.SS && (self.room.game.session as StoryGameSession).saveState.saveStateNumber == Plugin.SlugcatStatsName && (self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.altEnding != true)
        {
            result = false;
        }
        return result;
    }






    // 鉴于我最近挂的ilhook全都莫名其妙地失败了，我准备直接绕过原版方法（谁闲的没事会动这个函数啊。。
    private static void SSOracleBehavior_UnconciousUpdate(On.SSOracleBehavior.orig_UnconciousUpdate orig, SSOracleBehavior self)
    {
        if (self.oracle.room.game != null && self.oracle.room.game.session is StoryGameSession && self.oracle.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName && self.oracle.ID == Oracle.OracleID.SS)
        {
            self.FindPlayer();
            /*for (int i = 0; i < self.oracle.room.game.cameras.Length; i++)
            {
                if (self.oracle.room.game.cameras[i].room == self.oracle.room && !self.oracle.room.game.cameras[i].AboutToSwitchRoom)
                {
                    self.oracle.room.game.cameras[i].ChangeBothPalettes(10, 26, 0.51f + Mathf.Sin(self.unconciousTick * 0.25707963f) * 0.35f);
                }
            }*/

            if (self.oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.DarkenLights) != null)
            {
                self.oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.DarkenLights).amount = Mathf.Lerp(0f, 1f, 0.2f + Mathf.Sin(self.unconciousTick * 0.15f));
            }
            if (self.oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Darkness) != null)
            {
                self.oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Darkness).amount = Mathf.Lerp(0f, 0.4f, 0.2f + Mathf.Sin(self.unconciousTick * 0.15f));
            }
            if (self.oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Contrast) != null)
            {
                self.oracle.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Contrast).amount = Mathf.Lerp(0f, 0.3f, 0.2f + Mathf.Sin(self.unconciousTick * 0.15f));
            }

            self.unconciousTick += 1f;
            self.oracle.setGravity(0.9f);

            bool getModule = Plugin.oracleModules.TryGetValue(self.oracle, out var module) && module.ownerSlugcatName == Plugin.SlugcatStatsName;
            if (getModule && module.console != null && module.console.player != null)
            {
                bool getModulep = Plugin.playerModules.TryGetValue(module.console.player, out var modulep) && modulep.playerName == Plugin.SlugcatStatsName;
                if (getModulep && modulep.gravityController != null && modulep.gravityController.enabled)
                {
                    self.oracle.room.gravity = modulep.gravityController.gravityBonus * 0.1f;
                }
            }
        }
        else { orig(self); }
    }



    #endregion









    private static void SSOracleBehavior_ctor(On.SSOracleBehavior.orig_ctor orig, SSOracleBehavior self, Oracle oracle)
    {
        orig(self, oracle);
        if (oracle.room.game.session is not StoryGameSession || oracle.room.game.GetStorySession.saveState.saveStateNumber != Plugin.SlugcatStatsName) return;
        self.movementBehavior = SSOracleBehavior.MovementBehavior.Meditate;
    }






    // 在这里挂模组（万物起源，如果有问题就关它
    private static void Oracle_ctor(On.Oracle.orig_ctor orig, Oracle self, AbstractPhysicalObject abstractPhysicalObject, Room room)
    {
        orig(self, abstractPhysicalObject, room);
        if (self.room.game.session is StoryGameSession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            if (self.ID == Oracle.OracleID.SS)
            {
                Plugin.oracleModules.Add(self, new OracleModule(self));
            }
        }


    }





    // 垃圾回收
    private static void Oracle_Destroy(On.Oracle.orig_Destroy orig, Oracle self)
    {
        orig(self);
        if (self.ID == Oracle.OracleID.SS)
        {
            bool getModule = Plugin.oracleModules.TryGetValue(self, out var module) && module.ownerSlugcatName == Plugin.SlugcatStatsName;
            if (getModule)
            {
                module.console.Destroy();
                module.console.hud.ClearSprites();
                module.console.hud = null;
                module.console = null;
            }
        }
    }












    // 储存珍珠。纯复制粘贴。。
    private static Vector2 SSOracleBehavior_storedPearlOrbitLocation(On.SSOracleBehavior.orig_storedPearlOrbitLocation orig, SSOracleBehavior self, int index)
    {
        if (self.oracle.room.game.IsStorySession && self.oracle.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            float num = 5f;
            float num2 = (float)index % num;
            float num3 = Mathf.Floor((float)index / num);
            float num4 = num2 * 0.5f;
            return new Vector2(615f, 100f) + new Vector2(num2 * 26f, (num3 + num4) * 18f);
        }
        return orig(self, index);
    }







    // 我决定让fp呆在原地不要动了，不然有点鬼畜
    private delegate bool orig_EyesClosed(SSOracleBehavior self);
    private static bool SSOracleBehavior_EyesClosed(orig_EyesClosed orig, SSOracleBehavior self)
    {
        var result = orig(self);
        bool getModule = Plugin.oracleModules.TryGetValue(self.oracle, out var module) && module.ownerSlugcatName == Plugin.SlugcatStatsName;
        if (getModule && module.console != null && module.console.player != null)
        {
            result = !module.console.isActive;
        }
        return result;
    }





    // 珍珠会绕着猫转
    // 但是效果没有我想象中那么好（。）所以先关了
    private static void PebblesPearl_Update(On.PebblesPearl.orig_Update orig, PebblesPearl self, bool eu)
    {
        orig(self, eu);
        if (self.hoverPos == null && self.oracle != null && self.oracle.room == self.room)
        {
            bool getModule = Plugin.oracleModules.TryGetValue(self.oracle, out var module) && module.ownerSlugcatName == Plugin.SlugcatStatsName;
            if (getModule && module.console != null && module.console.player != null && module.console.player.room == self.room)
            {
                if (!self.oracle.Consious) self.orbitObj = null;
                // else if (module.console.isActive) self.orbitObj = module.console.player;
                // else self.orbitObj = self.oracle;
            }

        }

    }








    // 这恐怕是唯一修改重力的办法了。。我很怀疑他会不会使得一些性能不太好的电脑掉帧
    // 没事了 我找到另一个办法
    private delegate float orig_lowGravity(SSOracleBehavior.SubBehavior self);
    private static float SSOracleBehavior_SubBehavior_lowGravity(orig_lowGravity orig, SSOracleBehavior.SubBehavior self)
    {
        var result = orig(self);
        bool getModule = Plugin.oracleModules.TryGetValue(self.oracle, out var module) && module.ownerSlugcatName == Plugin.SlugcatStatsName;
        if (getModule && module.console != null && module.console.player != null)
        {
            bool getModulep = Plugin.playerModules.TryGetValue(module.console.player, out var modulep) && modulep.playerName == Plugin.SlugcatStatsName;
            if (getModulep && modulep.gravityController != null)
            {
                result = modulep.gravityController.gravityBonus * 0.1f;
            }
        }
        return result;
    }






    // 防止fp修改房间重力
    // tmd，这个为啥不生效啊（恼）这比东西也不输出日志，他到底想干嘛
    private static void IL_SSOracleBehavior_Update(ILContext il)
    {
        Plugin.Log("IL_SSOracleBehavior_Update");
        // 2240 插入label
        ILLabel label = null;
        ILCursor c6 = new ILCursor(il);
        if (c6.TryGotoNext(MoveType.After,
            i => i.MatchLdfld<SSOracleBehavior>("currSubBehavior"),
            i => i.Match(OpCodes.Callvirt),
            i => i.MatchStfld<Room>("gravity")
            ))
        {
            label = c6.MarkLabel();
        }
        else
        {
            Plugin.Log("!!! IL_SSOracleBehavior_Update not found !!!");
        }

        // 2241，执行判断
        ILCursor c7 = new ILCursor(il);
        if (c7.TryGotoNext(MoveType.After,
            i => i.MatchLdfld<SSOracleBehavior>("currSubBehavior"),
            i => i.Match(OpCodes.Callvirt),
            i => i.MatchStfld<Room>("gravity"),
            i => i.Match(OpCodes.Ret)
            ) && label != null)
        {
            Plugin.Log("SS_AI gravity");
            c7.Emit(OpCodes.Ldarg_0);
            c7.EmitDelegate<Func<SSOracleBehavior, bool>>((self) =>
            {
                bool getModule = Plugin.oracleModules.TryGetValue(self.oracle, out var module) && module.ownerSlugcatName == Plugin.SlugcatStatsName;
                if (getModule && module.console != null && module.console.isActive)
                {
                    
                    return true;
                }
                return false;
            });
            c7.Emit(OpCodes.Brtrue, label);
        }

    }











    // 你说的对，但是
    private static void SSOracleBehavior_Update(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
    {
        orig(self, eu);
        bool getModule = Plugin.oracleModules.TryGetValue(self.oracle, out var module) && module.ownerSlugcatName == Plugin.SlugcatStatsName;
        if (getModule && self.oracle.ID == Oracle.OracleID.SS && self.oracle.room.game.IsStorySession && self.oracle.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName) 
        {
            module.console?.Update(eu);
            /*// 这是另一种改变房间重力的方式，它好在可以显示出来重力变化（？）要不要这么干取决于我如何设计那个控制台
            if (module.console.player != null)
            {
                bool getModulep = Plugin.playerModules.TryGetValue(module.console.player, out var modulep) && modulep.playerName == Plugin.SlugcatStatsName;
                if (getModulep && modulep.gravityController.isAbleToUse)
                {
                    self.getToWorking = 1 - modulep.gravityController.gravityBonus * 0.1f;
                }
            }*/
            
        }
        
    }





    // 让fp无视你的大部分行为（这不包括用武器攻击他
    private static void SSOracleBehavior_SeePlayer(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
    {
        bool getModule = Plugin.oracleModules.TryGetValue(self.oracle, out var module) && module.ownerSlugcatName == Plugin.SlugcatStatsName;
        if (getModule && self.oracle.ID == Oracle.OracleID.SS)
        {
            self.NewAction(ActionID);
            self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad = 0;
        }
        else { orig(self); }
    }






    private static void SSOracleBehavior_NewAction(On.SSOracleBehavior.orig_NewAction orig, SSOracleBehavior self, SSOracleBehavior.Action nextAction)
    {
        if (nextAction == ActionID)
        {
            if (self.currSubBehavior.ID == SubBehavID) return;


            SSOracleBehavior.SubBehavior subBehavior = null;
            for (int i = 0; i < self.allSubBehaviors.Count; i++)
            {
                if (self.allSubBehaviors[i].ID == SubBehavID)
                {
                    subBehavior = self.allSubBehaviors[i];
                    break;
                }
            }
            if (subBehavior == null)
            {
                subBehavior = new SSOracleSubBehavior(self);
                self.allSubBehaviors.Add(subBehavior);
            }
            subBehavior.Activate(self.action, nextAction);
            self.currSubBehavior.Deactivate();
            Plugin.LogStat("Switching subbehavior to: " + subBehavior.ID.ToString() + " from: " + self.currSubBehavior.ID.ToString());
            self.currSubBehavior = subBehavior;
            self.inActionCounter = 0;
            self.action = nextAction;
            return;
        }
        else if (self.oracle.room.game.IsStorySession && self.oracle.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            
            if (nextAction == self.action) return;
            Plugin.LogStat("old action:", self.action.ToString(), "new action:", nextAction.ToString());

            // 防止一切洗脑失败的情况（。）直接给你堵死。乐
            nextAction = ActionID;

        }
        orig(self, nextAction);
    }















    public static SSOracleBehavior.Action ActionID = new("PebblesSlug_Action", false);
    public static SSOracleBehavior.SubBehavior.SubBehavID SubBehavID = new("PebblesSlug_SubBehavior", false);
    public static Conversation.ID ConversationID = new("PebblesSlug_Conversation", false);

}






// 好好好，我写，我写行了吧
// 我该给这个类起什么名字。。
// 啊这，难道拼写错误也是代码命名规则的一部分嘛……
// 我猜想这样写完之后能通过调整mod的启用顺序，来解决在我的存档里玩机猫会触发机猫剧情的问题（？
public class SSOracleSubBehavior : SSOracleBehavior.ConversationBehavior
{
    public bool firstMetOnThisCycle;

    public float lastGetToWork;

    public float tagTimer;
    private PlayerModule PlayerModule;


    // 千万别调用convoID，因为它是我瞎写的占位符，啥也不是
    public SSOracleSubBehavior(SSOracleBehavior owner) : base(owner, SSOracleHooks.SubBehavID, SSOracleHooks.ConversationID)
    {
        if (oracle.ID != Oracle.OracleID.SS) return;
        Plugin.Log("SSoracleBehavior - subBehavior ctor");
        this.owner.TurnOffSSMusic(true);

        if (base.player != null && base.player.room != null && base.player.room == this.owner.oracle.room) 
        {
            PlayerModule = (Plugin.playerModules.TryGetValue(base.player, out var module) && module.playerName == Plugin.SlugcatStatsName) ? module : null;
        }
        if (this.owner.conversation != null)
        {
            this.owner.conversation.Destroy();
            this.owner.conversation = null;
        }
        this.owner.movementBehavior = SSOracleBehavior.MovementBehavior.Meditate;

    }


    public override void Update()
    {
        base.Update();
        if (base.player == null)
        {
            return;
        }
        owner.movementBehavior = SSOracleBehavior.MovementBehavior.Meditate;
        if (tagTimer > 0f && owner.inspectPearl != null)
        {
            owner.killFac = Mathf.Clamp(tagTimer / 120f, 0f, 1f);
            tagTimer -= 1f;
            if (tagTimer <= 0f)
            {
                for (int i = 0; i < 20; i++)
                {
                    oracle.room.AddObject(new Spark(owner.inspectPearl.firstChunk.pos, Custom.RNV() * Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                }
                oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, owner.inspectPearl.firstChunk.pos, 1f, 0.5f + Random.value * 0.5f);
                owner.killFac = 0f;
            }
        }
    }




    public override Vector2? LookPoint
    {
        get 
        {
            if (base.player == null) return null;
            if (PlayerModule != null && PlayerModule.console != null && PlayerModule.console.isActive)
            {
                return base.player.mainBodyChunk.pos;
            }
            return null;
        }
    }




    public override float LowGravity
    {
        get
        {
            if (PlayerModule != null && PlayerModule.gravityController != null)
            {
                return PlayerModule.gravityController.gravityBonus * 0.1f;
            }
            return -1f;
        }
    }









    public override void Deactivate()
    {
        base.Deactivate();
    }



    public override void Activate(SSOracleBehavior.Action oldAction, SSOracleBehavior.Action newAction)
    {
        base.Activate(oldAction, newAction);
    }



    /*public override void NewAction(SSOracleBehavior.Action oldAction, SSOracleBehavior.Action newAction)
    {
        base.NewAction(oldAction, newAction);
        if (newAction == SSOracleBehavior.Action.ThrowOut_KillOnSight && this.owner.conversation != null)
        {
            this.owner.conversation.Destroy();
            this.owner.conversation = null;
        }
    }*/


}
