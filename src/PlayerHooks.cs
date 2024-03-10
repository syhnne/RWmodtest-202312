using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using System.Drawing.Text;
using RWCustom;
using Noise;
using MoreSlugcats;
using BepInEx.Logging;
using Smoke;
using Random = UnityEngine.Random;
// using ImprovedInput;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Menu.Remix.MixedUI;
using System.ComponentModel;
using SlugBase.DataTypes;
using MonoMod.RuntimeDetour;
using System.Reflection;
using SlugBase.SaveData;
using SlugBase;
using System.Runtime.InteropServices;
using static PebblesSlug.PlayerHooks;














namespace PebblesSlug;


// 受不了了，进行一个重构
internal class PlayerHooks
{

    public static void Disable()
    {
        On.HUD.HUD.InitSinglePlayerHud -= HUD_InitSinglePlayerHud;
        On.Player.MovementUpdate -= Player_MovementUpdate;
        // 重力控制
        On.Player.Die -= Player_Die;
        On.Player.Destroy -= Player_Destroy;
        // 合成，二段跳
        On.Player.Jump -= Player_Jump;
        On.Player.ClassMechanicsArtificer -= Player_ClassMechanicsArtificer;
        On.Player.CraftingResults -= Player_CraftingResults;
        On.Player.GraspsCanBeCrafted -= Player_GraspsCanBeCrafted;
        On.Player.SpitUpCraftedObject -= Player_SpitUpCraftedObject;
        On.Player.ThrownSpear -= Player_ThrownSpear;
        // 不能吃神经元
        IL.Player.GrabUpdate -= IL_Player_GrabUpdate;
        On.Player.BiteEdibleObject -= Player_BiteEdibleObject;
        On.Player.CanBeSwallowed -= Player_CanBeSwallowed;
        On.Player.ObjectCountsAsFood -= Player_ObjectCountsAsFood;
    }






    internal static void Apply()
    {
        On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
        On.Player.MovementUpdate += Player_MovementUpdate;

        // 重力控制
        On.Player.Die += Player_Die;
        On.Player.Destroy += Player_Destroy;


        // 合成，二段跳
        On.Player.Jump += Player_Jump;
        On.Player.ClassMechanicsArtificer += Player_ClassMechanicsArtificer;
        On.Player.CraftingResults += Player_CraftingResults;
        On.Player.GraspsCanBeCrafted += Player_GraspsCanBeCrafted;
        // On.Player.SwallowObject += Player_SwallowObject;
        On.Player.SpitUpCraftedObject += Player_SpitUpCraftedObject;
        On.Player.ThrownSpear += Player_ThrownSpear;




        // 不能吃神经元
        IL.Player.GrabUpdate += IL_Player_GrabUpdate;
        On.Player.BiteEdibleObject += Player_BiteEdibleObject;
        On.Player.CanBeSwallowed += Player_CanBeSwallowed;
        On.Player.ObjectCountsAsFood += Player_ObjectCountsAsFood;
        new Hook(
            typeof(SSOracleSwarmer).GetProperty(nameof(SSOracleSwarmer.Edible), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            SSOracleSwarmer_Edible
            );
        new Hook(
            typeof(SLOracleSwarmer).GetProperty(nameof(SLOracleSwarmer.Edible), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            SLOracleSwarmer_Edible
            );

    }




    public class PlayerModule
    {
        public readonly WeakReference<Player> playerRef;
        internal readonly SlugcatStats.Name playerName;
        internal readonly SlugcatStats.Name storyName;
        internal readonly bool isPebbles;

        public bool lockInput = false;

        internal SSOracleConsole console;
        internal GravityController gravityController;
        internal int SS_AIsleepCounter = 60;


        public PlayerModule(Player player)
        {
            playerRef = new WeakReference<Player>(player);
            playerName = player.slugcatStats.name;
            if (player.room.game.session is StoryGameSession)
            {
                storyName = player.room.game.GetStorySession.saveStateNumber;
            }
            else { storyName = null; }
            isPebbles = playerName == Plugin.SlugcatStatsName && storyName == Plugin.SlugcatStatsName;


            if (playerName == Plugin.SlugcatStatsName && storyName != null)
            {
                Plugin.LogStat("gravityController added!");
                gravityController = new GravityController(player);
            }
            if (isPebbles)
            {
                

            }
                
        }









        // 启用控制台
        public void Update(Player player, bool eu)
        {
            if (console != null && player.room.abstractRoom.name == "SS_AI" && player.room.game.GetStorySession.saveState.deathPersistentSaveData.altEnding
                && Input.GetKeyDown(Plugin.instance.option.fpConsoleKey.Value))
            {

                // 控制台功能我懒得做了，先关掉（
                /*Plugin.LogStat("toggle console active: ", console.isActive);
                console.isActive = !console.isActive;*/


                // 没想好怎么让他移动，先不关这个了
                // lockInput = console.isActive;
                
                
            }

            /*if (gravityController != null)
            {
                lockInput = gravityController.isAbleToUse;
            }*/

        }

    }







    // 启用控制台或者重力控制时阻止玩家输入
    private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
    {
        bool getModule = Plugin.playerModules.TryGetValue(self, out var module) && module.playerName == Plugin.SlugcatStatsName;
        if (getModule)
        {
            if (module.lockInput)
            {
                self.input[0] = new Player.InputPackage();
            }
            else if (module.gravityController != null && module.gravityController.isAbleToUse)
            {
                module.gravityController.inputY = self.input[0].y;
                self.input[0].y = 0;
            }
        }
        orig(self, eu);
    }









    // 重力控制hud
    private static void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
    {
        orig(self, cam);
        if ((self.owner as Player).slugcatStats.name == Plugin.SlugcatStatsName)
        {
            bool getModule = Plugin.playerModules.TryGetValue((self.owner as Player), out var module) && module.playerName == Plugin.SlugcatStatsName;
            if (getModule)
            {
                Plugin.LogStat("HUD add GravityMeter");
                self.AddPart(new GravityMeter(self, self.fContainers[1], module.gravityController));
            }

        }

    }







    
















    #region 重力控制
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



    // 垃圾回收
    private static void Player_Destroy(On.Player.orig_Destroy orig, Player self)
    {
        bool getModule = Plugin.playerModules.TryGetValue(self, out var module) && module.playerName == Plugin.SlugcatStatsName;
        if (getModule)
        {
            module.gravityController?.Destroy();
            module.gravityController = null;
        }
        orig(self);
    }




    // 防止你那倒霉的联机队友在你死了之后顶着3倍重力艰难行走。我知道队友有可能也会控制重力，但是我懒得加判断
    private static void Player_Die(On.Player.orig_Die orig, Player self)
    {
        bool getModule = Plugin.playerModules.TryGetValue(self, out var module) && module.playerName == Plugin.SlugcatStatsName;
        if (getModule && self.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            module.gravityController.Die();
        }
        orig(self);

    }



    #endregion





    #region 合成，二段跳



    // 增加跳跃能力（不要改，改了之后开局可能出不去ssai
    private static void Player_Jump(On.Player.orig_Jump orig, Player self)
    {
        orig(self);
        if (self.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            self.jumpBoost *= 1.2f;
        }
    }





    // 一矛超人，只要不使用二段跳，就是常驻2倍矛伤。使用二段跳会导致这个伤害发生衰减，最低不低于0.5。修改slugbase的基础矛伤可以使所有的值发生变化
    private static void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
    {
        orig(self, spear);
        if (self.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            float spearDmgBonus = 1f;
            if (self.pyroJumpCounter > 0)
            {
                spearDmgBonus /= self.pyroJumpCounter;
            }
            spear.spearDamageBonus *= (0.5f + spearDmgBonus);
            Plugin.LogStat("spearDmgBonus: " + (0.5f + spearDmgBonus) + "  result: " + spear.spearDamageBonus);
        }

    }






    // TODO: 给这一坨东西单独做一个类
    // 除了特效以外，数值跟炸猫差不多，因为我不知道那堆二段跳的数值怎么改。我想改得小一点，让他没有那么强的机动性，不然太超模了（
    // 因为这个电击在水下是有伤害的（痛击你的队友。jpg）我不是故意的，我是真的写不出来那个判定。我不知道他为什么会闪退。。
    // 我大概应该用原版方法，然后做ilhooking。但是，说真的，想想那个工作量吧（汗）我都不太清楚自己究竟改了些什么
    private static void Player_ClassMechanicsArtificer(On.Player.orig_ClassMechanicsArtificer orig, Player self)
    {
        if (self.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            Room room = self.room;
            bool flag = self.wantToJump > 0 && self.input[0].pckp;
            bool flag2 = self.eatMeat >= 20 || self.maulTimer >= 15;
            int explosionCapacity = PebblesSlugOption.ExplosionCapacity.Value;
            int num = Mathf.Max(1, explosionCapacity - 5);
            // 这是怎么回事？
            orig(self);
            if (self.pyroJumpCounter > 0 && (self.Consious || self.dead))
            {
                self.pyroJumpCooldown -= 1f;
                if (self.pyroJumpCooldown <= 0f)
                {
                    if (self.pyroJumpCounter >= num)
                    {
                        self.pyroJumpCooldown = 40f;
                    }
                    else
                    {
                        self.pyroJumpCooldown = 60f;
                    }
                    self.pyroJumpCounter--;
                }
            }
            self.pyroParryCooldown -= 1f;
            if (self.pyroJumpCounter >= num)
            {
                if (Random.value < 0.25f)
                {
                    // 这应该是炸多了的冒烟效果
                    self.room.AddObject(new Explosion.ExplosionSmoke(self.mainBodyChunk.pos, Custom.RNV() * 2f * Random.value, 1f));
                }
                if (Random.value < 0.5f)
                {
                    self.room.AddObject(new Spark(self.mainBodyChunk.pos, Custom.RNV(), Color.white, null, 4, 8));
                }
            }

            if (flag
                && !self.pyroJumpped
                && self.canJump <= 0 && !flag2
                && (self.input[0].y >= 0 || (self.input[0].y < 0 && (self.bodyMode == Player.BodyModeIndex.ZeroG || self.gravity <= 0.1f)))
                && self.Consious && self.bodyMode != Player.BodyModeIndex.Crawl
                && self.bodyMode != Player.BodyModeIndex.CorridorClimb
                && self.bodyMode != Player.BodyModeIndex.ClimbIntoShortCut
                && self.animation != Player.AnimationIndex.HangFromBeam
                && self.animation != Player.AnimationIndex.ClimbOnBeam
                && self.bodyMode != Player.BodyModeIndex.WallClimb
                && self.bodyMode != Player.BodyModeIndex.Swimming
                && self.animation != Player.AnimationIndex.AntlerClimb
                && self.animation != Player.AnimationIndex.VineGrab
                && self.animation != Player.AnimationIndex.ZeroGPoleGrab
                && self.onBack == null)
            {
                self.pyroJumpped = true;
                self.pyroJumpDropLock = 40;
                self.noGrabCounter = 5;
                Vector2 pos = self.firstChunk.pos;
                // 这是正经爆炸效果罢
                // 哦不，这是有烟无伤的二段跳
                // 现在有伤害了，只要你在水里起跳，就会让附近生物触电，只不过没什么伤害


                for (int i = 0; i < 8; i++)
                {
                    Vector2 vector = Custom.DegToVec(360f * Random.value);
                    self.room.AddObject(new MouseSpark(pos + vector * 9f, self.firstChunk.vel + vector * 36f * Random.value, 20f, new Color(0.7f, 1f, 1f)));
                }
                self.room.AddObject(new Explosion.ExplosionLight(pos, 200f, 1f, 4, new Color(0.7f, 1f, 1f)));
                for (int j = 0; j < 10; j++)
                {
                    Vector2 vector = Custom.RNV();
                    self.room.AddObject(new Spark(pos + vector * Random.value * 40f, vector * Mathf.Lerp(4f, 30f, Random.value), Color.white, null, 4, 18));
                }
                self.room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, self.firstChunk.pos);
                self.room.InGameNoise(new InGameNoise(pos, 8000f, self, 1f));
                int num2 = Mathf.Max(1, explosionCapacity - 3);
                if (self.Submersion <= 0.5f)
                {
                    self.room.AddObject(new UnderwaterShock(self.room, self, pos, 10, 500f, 0.5f, self, new Color(0.8f, 0.8f, 1f)));
                }
                if (self.bodyMode == Player.BodyModeIndex.ZeroG || self.room.gravity == 0f || self.gravity == 0f)
                {
                    float num3 = (float)self.input[0].x;
                    float num4 = (float)self.input[0].y;
                    while (num3 == 0f && num4 == 0f)
                    {
                        num3 = (float)(((double)Random.value <= 0.33) ? 0 : (((double)Random.value <= 0.5) ? 1 : -1));
                        num4 = (float)(((double)Random.value <= 0.33) ? 0 : (((double)Random.value <= 0.5) ? 1 : -1));
                    }
                    self.bodyChunks[0].vel.x = 9f * num3;
                    self.bodyChunks[0].vel.y = 9f * num4;
                    self.bodyChunks[1].vel.x = 8f * num3;
                    self.bodyChunks[1].vel.y = 8f * num4;
                    self.pyroJumpCooldown = 150f;
                    self.pyroJumpCounter++;
                }
                else
                {
                    if (self.input[0].x != 0)
                    {
                        self.bodyChunks[0].vel.y = Mathf.Min(self.bodyChunks[0].vel.y, 0f) + 8f;
                        self.bodyChunks[1].vel.y = Mathf.Min(self.bodyChunks[1].vel.y, 0f) + 7f;
                        self.jumpBoost = 6f;
                    }
                    if (self.input[0].x == 0 || self.input[0].y == 1)
                    {
                        if (self.pyroJumpCounter >= num2)
                        {
                            self.bodyChunks[0].vel.y = 16f;
                            self.bodyChunks[1].vel.y = 15f;
                            self.jumpBoost = 10f;
                        }
                        else
                        {
                            self.bodyChunks[0].vel.y = 11f;
                            self.bodyChunks[1].vel.y = 10f;
                            self.jumpBoost = 8f;
                        }
                    }
                    if (self.input[0].y == 1)
                    {
                        self.bodyChunks[0].vel.x = 8f * (float)self.input[0].x;
                        self.bodyChunks[1].vel.x = 6f * (float)self.input[0].x;
                    }
                    else
                    {
                        self.bodyChunks[0].vel.x = 14f * (float)self.input[0].x;
                        self.bodyChunks[1].vel.x = 12f * (float)self.input[0].x;
                    }
                    self.animation = Player.AnimationIndex.Flip;
                    self.pyroJumpCounter++;
                    self.pyroJumpCooldown = 150f;
                    self.bodyMode = Player.BodyModeIndex.Default;
                }
                if (self.pyroJumpCounter >= num2)
                {
                    self.Stun(60 * (self.pyroJumpCounter - (num2 - 1)));
                }
                if (self.pyroJumpCounter >= explosionCapacity)
                {
                    self.room.AddObject(new ShockWave(pos, 200f, 0.2f, 6, false));
                    self.room.AddObject(new Explosion(self.room, self, pos, 7, 350f, 26.2f, 2f, 280f, 0.35f, self, 0.7f, 160f, 1f));
                    self.room.ScreenMovement(new Vector2?(pos), default(Vector2), 1.3f);
                    self.room.InGameNoise(new InGameNoise(pos, 9000f, self, 1f));
                    self.Die();
                }

            }


            else if (flag

                && !flag2
                && (self.input[0].y < 0 || self.bodyMode == Player.BodyModeIndex.Crawl)
                && (self.canJump > 0 || self.input[0].y < 0) && self.Consious && !self.pyroJumpped && self.pyroParryCooldown <= 0f)
            {
                if (self.canJump <= 0)
                {
                    self.pyroJumpped = true;
                    self.bodyChunks[0].vel.y = 8f;
                    self.bodyChunks[1].vel.y = 6f;
                    self.jumpBoost = 6f;
                    self.forceSleepCounter = 0;
                }
                if (self.pyroJumpCounter <= num)
                {
                    self.pyroJumpCounter += 2;
                }
                else
                {
                    self.pyroJumpCounter++;
                }
                self.pyroParryCooldown = 40f;
                self.pyroJumpCooldown = 150f;

                Vector2 pos2 = self.firstChunk.pos;

                for (int i = 0; i < 8; i++)
                {
                    Vector2 vector3 = Custom.DegToVec(360f * Random.value);
                    self.room.AddObject(new MouseSpark(pos2 + vector3 * 9f, self.firstChunk.vel + vector3 * 36f * Random.value, 20f, new Color(0.7f, 1f, 1f)));
                }


                self.room.AddObject(new Explosion.ExplosionLight(pos2, 200f, 1f, 4, new Color(0.7f, 1f, 1f)));

                for (int l = 0; l < 8; l++)
                {
                    Vector2 vector2 = Custom.RNV();
                    self.room.AddObject(new Spark(pos2 + vector2 * Random.value * 40f, vector2 * Mathf.Lerp(4f, 30f, Random.value), Color.white, null, 4, 18));
                }
                self.room.AddObject(new ShockWave(pos2, 200f, 0.2f, 6, false));
                self.room.AddObject(new ZapCoil.ZapFlash(pos2, 10f));
                // player.room.PlaySound(SoundID.Flare_Bomb_Burn, pos2);
                // player.room.PlaySound(SoundID.Zapper_Zap, pos2, 1f, 0.2f + 0.25f * Random.value);
                self.room.PlaySound(SoundID.Zapper_Zap, pos2, 1f, 1f + 0.25f * Random.value);
                // player.room.PlaySound(SoundID.Fire_Spear_Explode, pos2, 0.3f + Random.value * 0.3f, 0.5f + Random.value * 2f);
                self.room.InGameNoise(new InGameNoise(pos2, 8000f, self, 1f));

                if (self.room.Darkness(pos2) > 0f)
                {
                    self.room.AddObject(new LightSource(pos2, false, new Color(0.7f, 1f, 1f), self));
                }

                List<Weapon> list = new List<Weapon>();
                for (int m = 0; m < self.room.physicalObjects.Length; m++)
                {
                    for (int n = 0; n < self.room.physicalObjects[m].Count; n++)
                    {
                        if (self.room.physicalObjects[m][n] is Weapon)
                        {
                            Weapon weapon = self.room.physicalObjects[m][n] as Weapon;
                            if (weapon.mode == Weapon.Mode.Thrown && Custom.Dist(pos2, weapon.firstChunk.pos) < 300f)
                            {
                                list.Add(weapon);
                            }
                        }
                        bool flag3;
                        if (ModManager.CoopAvailable && !Custom.rainWorld.options.friendlyFire)
                        {
                            Player player = self.room.physicalObjects[m][n] as Player;
                            flag3 = (player == null || player.isNPC);
                        }
                        else
                        {
                            flag3 = true;
                        }
                        bool flag4 = flag3;
                        if (self.room.physicalObjects[m][n] is Creature && self.room.physicalObjects[m][n] != self && flag4)
                        {
                            Creature creature = self.room.physicalObjects[m][n] as Creature;
                            if (Custom.Dist(pos2, creature.firstChunk.pos) < 200f && (Custom.Dist(pos2, creature.firstChunk.pos) < 60f || self.room.VisualContact(self.abstractCreature.pos, creature.abstractCreature.pos)))
                            {
                                self.room.socialEventRecognizer.WeaponAttack(null, self, creature, true);
                                creature.SetKillTag(self.abstractCreature);
                                if (creature is Scavenger)
                                {
                                    (creature as Scavenger).HeavyStun(80);
                                    creature.Blind(400);
                                }
                                else
                                {
                                    creature.Stun(80);
                                    creature.Blind(400);
                                }
                                creature.firstChunk.vel = Custom.DegToVec(Custom.AimFromOneVectorToAnother(pos2, creature.firstChunk.pos)) * 30f;
                                if (creature is TentaclePlant)
                                {
                                    for (int num5 = 0; num5 < creature.grasps.Length; num5++)
                                    {
                                        creature.ReleaseGrasp(num5);
                                    }
                                }
                            }
                        }
                    }
                }
                if (self.Submersion <= 0.5f)
                {
                    self.room.AddObject(new UnderwaterShock(self.room, self, pos2, 10, 800f, 2f, self, new Color(0.8f, 0.8f, 1f)));
                }
                if (list.Count > 0 && self.room.game.IsArenaSession)
                {
                    self.room.game.GetArenaGameSession.arenaSitting.players[0].parries++;
                }
                for (int num6 = 0; num6 < list.Count; num6++)
                {
                    list[num6].ChangeMode(Weapon.Mode.Free);
                    list[num6].firstChunk.vel = Custom.DegToVec(Custom.AimFromOneVectorToAnother(pos2, list[num6].firstChunk.pos)) * 20f;
                    list[num6].SetRandomSpin();
                }
                int num7 = Mathf.Max(1, explosionCapacity - 3);
                if (self.pyroJumpCounter >= num7)
                {
                    self.Stun(60 * (self.pyroJumpCounter - (num7 - 1)));
                }
                if (self.pyroJumpCounter >= explosionCapacity)
                {
                    self.room.AddObject(new ShockWave(pos2, 200f, 0.2f, 6, false));
                    self.room.AddObject(new Explosion(self.room, self, pos2, 7, 350f, 26.2f, 2f, 280f, 0.35f, self, 0.7f, 160f, 1f));
                    self.room.ScreenMovement(new Vector2?(pos2), default(Vector2), 1.3f);
                    self.room.InGameNoise(new InGameNoise(pos2, 9000f, self, 1f));
                    self.Die();
                }
            }


            if (self.canJump > 0
                || !self.Consious
                || self.Stunned
                || self.animation == Player.AnimationIndex.HangFromBeam
                || self.animation == Player.AnimationIndex.ClimbOnBeam
                || self.bodyMode == Player.BodyModeIndex.WallClimb
                || self.animation == Player.AnimationIndex.AntlerClimb
                || self.animation == Player.AnimationIndex.VineGrab
                || self.animation == Player.AnimationIndex.ZeroGPoleGrab
                || self.bodyMode == Player.BodyModeIndex.Swimming
                || ((self.bodyMode == Player.BodyModeIndex.ZeroG || self.room.gravity <= 0.5f || self.gravity <= 0.5f) && (self.wantToJump == 0 || !self.input[0].pckp)))
            {
                self.pyroJumpped = false;
            }
        }
        else { orig(self); }

    }






    private static AbstractPhysicalObject.AbstractObjectType Player_CraftingResults(On.Player.orig_CraftingResults orig, Player self)
    {
        if (self.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            if (self.FoodInStomach > 1)
            {
                Creature.Grasp[] grasps = self.grasps;
                for (int i = 0; i < grasps.Length; i++)
                {
                    if (grasps[i] != null && grasps[i].grabbed is IPlayerEdible && (grasps[i].grabbed as IPlayerEdible).Edible)
                    {
                        return null;
                    }
                }
                //要实现的效果：只拦截有电的电矛。没电的电矛、炸矛、普通矛都不拦截
                // 没事了，现在电矛能吃（大雾
                if (grasps[0] != null && grasps[0].grabbed is Spear)
                {
                    return AbstractPhysicalObject.AbstractObjectType.Spear;

                }
                if (grasps[0] == null && grasps[1] != null && grasps[1].grabbed is Spear && self.objectInStomach == null)
                {
                    return AbstractPhysicalObject.AbstractObjectType.Spear;
                }

            }
            else if (PebblesSlugOption.AddFoodOnShock.Value && self.grasps[0] != null && self.grasps[0].grabbed is ElectricSpear && (self.grasps[0].grabbed as Spear).abstractSpear.electricCharge > 0)
            {
                return AbstractPhysicalObject.AbstractObjectType.Spear;
            }
            else if (PebblesSlugOption.AddFoodOnShock.Value && self.grasps[0] == null && self.grasps[1] != null && self.grasps[1].grabbed is ElectricSpear && self.objectInStomach == null && (self.grasps[0].grabbed as Spear).abstractSpear.electricCharge > 0)
            {
                return AbstractPhysicalObject.AbstractObjectType.Spear;
            }
            return null;
        }
        else { return orig(self); }
    }






    private static bool Player_GraspsCanBeCrafted(On.Player.orig_GraspsCanBeCrafted orig, Player self)
    {
        if (self.slugcatStats.name == Plugin.SlugcatStatsName && (self.CraftingResults() != null))
        {
            return true;
        }
        return orig(self);
    }






    // 下面这个暂时没用，但先留着以防我日后突然想给他加点别的能力
    private static void Player_SwallowObject(On.Player.orig_SwallowObject orig, Player self, int grasp)
    {
        if (self.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            if (grasp < 0 || self.grasps[grasp] == null)
            {
                return;
            }
            AbstractPhysicalObject abstractPhysicalObject = self.grasps[grasp].grabbed.abstractPhysicalObject;
            if (abstractPhysicalObject is AbstractSpear)
            {
                (abstractPhysicalObject as AbstractSpear).stuckInWallCycles = 0;
            }
            self.objectInStomach = abstractPhysicalObject;
            if (ModManager.MMF && self.room.game.IsStorySession)
            {
                (self.room.game.session as StoryGameSession).RemovePersistentTracker(self.objectInStomach);
            }
            self.ReleaseGrasp(grasp);
            self.objectInStomach.realizedObject.RemoveFromRoom();
            self.objectInStomach.Abstractize(self.abstractCreature.pos);
            self.objectInStomach.Room.RemoveEntity(self.objectInStomach);
            if (self.FoodInStomach > 0)
            {
                if (abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.Spear && !(abstractPhysicalObject as AbstractSpear).explosive && !(abstractPhysicalObject as AbstractSpear).electric)
                {
                    // 这应该是生成矛的代码。那么它为什么不起作用呢（恼
                    abstractPhysicalObject = new AbstractSpear(self.room.world, null, self.room.GetWorldCoordinate(self.mainBodyChunk.pos), self.room.game.GetNewID(), true);
                    // 经测试，生成炸矛和下面的代码无关（1）那么我加这hook有用吗？一会把它删了逝逝
                    self.SubtractFood(1);
                }
            }
            self.objectInStomach = abstractPhysicalObject;
            self.objectInStomach.Abstractize(self.abstractCreature.pos);
            BodyChunk mainBodyChunk = self.mainBodyChunk;
            mainBodyChunk.vel.y = mainBodyChunk.vel.y + 2f;
            self.room.PlaySound(SoundID.Slugcat_Swallow_Item, self.mainBodyChunk);
        }
        else { orig(self, grasp); }
    }











    // 修改合成结果
    private static void Player_SpitUpCraftedObject(On.Player.orig_SpitUpCraftedObject orig, Player self)
    {
        if (self.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            var vector = self.mainBodyChunk.pos;

            // TODO: 我要写一个craftingtutorial，并且单独绑一个变量，因为它内容跟原来的不一样
            self.room.PlaySound(SoundID.Slugcat_Swallow_Item, self.mainBodyChunk);

            for (int i = 0; i < self.grasps.Length; i++)
            {
                if (self.grasps[i] != null)
                {
                    AbstractPhysicalObject grabbed = self.grasps[i].grabbed.abstractPhysicalObject;
                    if (grabbed is AbstractSpear)
                    {
                        AbstractSpear spear = grabbed as AbstractSpear;
                        if (spear.explosive)
                        {
                            ExplosiveSpear explosiveSpear = self.grasps[i].grabbed as ExplosiveSpear;
                            self.room.AddObject(new SootMark(self.room, vector, 50f, false));
                            self.room.AddObject(new Explosion(self.room, explosiveSpear, vector, 5, 50f, 4f, 0.1f, 60f, 0.3f, explosiveSpear.thrownBy, 0.8f, 0f, 0.7f));
                            for (int g = 0; g < 14; g++)
                            {
                                self.room.AddObject(new Explosion.ExplosionSmoke(vector, Custom.RNV() * 5f * Random.value, 1f));
                            }
                            self.room.AddObject(new Explosion.ExplosionLight(vector, 160f, 1f, 3, explosiveSpear.explodeColor));
                            self.room.AddObject(new ExplosionSpikes(self.room, vector, 9, 4f, 5f, 5f, 90f, explosiveSpear.explodeColor));
                            self.room.AddObject(new ShockWave(vector, 60f, 0.045f, 4, false));
                            for (int j = 0; j < 20; j++)
                            {
                                Vector2 vector2 = Custom.RNV();
                                self.room.AddObject(new Spark(vector + vector2 * Random.value * 40f, vector2 * Mathf.Lerp(4f, 30f, Random.value), explosiveSpear.explodeColor, null, 4, 18));
                            }
                            self.room.ScreenMovement(new Vector2?(vector), default(Vector2), 0.7f);
                            for (int k = 0; k < 2; k++)
                            {
                                Smolder smolder = null;
                                if (explosiveSpear.stuckInObject != null)
                                {
                                    smolder = new Smolder(self.room, explosiveSpear.stuckInChunk.pos, explosiveSpear.stuckInChunk, explosiveSpear.stuckInAppendage);
                                }
                                else
                                {
                                    Vector2? vector3 = SharedPhysics.ExactTerrainRayTracePos(self.room, explosiveSpear.firstChunk.pos, explosiveSpear.firstChunk.pos + ((k == 0) ? (explosiveSpear.rotation * 20f) : (Custom.RNV() * 20f)));
                                    if (vector3 != null)
                                    {
                                        smolder = new Smolder(self.room, vector3.Value + Custom.DirVec(vector3.Value, explosiveSpear.firstChunk.pos) * 3f, null, null);
                                    }
                                }
                                if (smolder != null)
                                {
                                    self.room.AddObject(smolder);
                                }
                            }
                            self.Stun(200);
                            explosiveSpear.abstractPhysicalObject.LoseAllStuckObjects();
                            explosiveSpear.room.PlaySound(SoundID.Fire_Spear_Explode, vector);
                            explosiveSpear.room.InGameNoise(new InGameNoise(vector, 8000f, explosiveSpear, 1f));
                            explosiveSpear.Destroy();
                        }
                        // 好了，谁能告诉我拿着有电的电矛的时候吐了两格是什么情况
                        else if (PebblesSlugOption.AddFoodOnShock.Value && spear.electric && spear.electricCharge > 0)
                        {
                            // 其实电矛是一种用来储存食物的工具（大雾
                            // 绕过香菇病的一种加食物方法
                            Plugin.Log("HOLDING ELECTRIC SPEAR");
                            Plugin.CustomAddFood(self, spear.electricCharge);
                            spear.electricCharge = 0;
                        }
                        else
                        {
                            self.ReleaseGrasp(i);
                            grabbed.realizedObject.RemoveFromRoom();
                            self.room.abstractRoom.RemoveEntity(grabbed);
                            // 对了，生成矛是这个。。我可能是那个眼瞎。。
                            self.SubtractFood(2);
                            AbstractSpear abstractSpear = new AbstractSpear(self.room.world, null, self.abstractCreature.pos, self.room.game.GetNewID(), false, true);
                            self.room.abstractRoom.AddEntity(abstractSpear);
                            abstractSpear.RealizeInRoom();
                            abstractSpear.electricCharge = 2;
                            if (self.FreeHand() != -1)
                            {
                                self.SlugcatGrab(abstractSpear.realizedObject, self.FreeHand());
                            }
                        }
                        return;
                    }
                }
            }
        }
        else { orig(self); }
    }


    #endregion





    #region 不能吃神经元



    // 修改神经元的可食用性和合成判定
    private static void IL_Player_GrabUpdate(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        // 337末尾，修改神经元的可食用性。没错，我要借用一下他自带的那个brfalse。。因为我自己不会写！！
        if (c.TryGotoNext(MoveType.After,
            (i) => i.MatchCall<Creature>("get_grasps"),
            (i) => i.Match(OpCodes.Ldloc_S),
            (i) => i.Match(OpCodes.Ldelem_Ref),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Isinst),
            (i) => i.Match(OpCodes.Callvirt)
            ))
        {
            // 啊哈！我是天才！
            // 怎么解决brfalse无法定位的问题：不要用自己的brfalse，直接绑架一个他原本的判断，然后把那个判断的输出结果和我加的判断绑在一起。反正他们是and关系
            // 这样一来，比原本直接改player_grabupdate的方法更加方便，因为另一个地方我就不用改了。
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 13);
            c.EmitDelegate<Func<bool, Player, int, bool>>((edible, self, grasp) =>
            {
                if (self.slugcatStats.name == Plugin.SlugcatStatsName)
                {
                    bool isNotOracleSwarmer = !(self.grasps[grasp].grabbed is OracleSwarmer);
                    return (edible && isNotOracleSwarmer);
                }
                else
                {
                    return edible;
                }

            });
        }

        // 533末尾，骗代码说我是工匠，让我合成
        // 有的人没发现自己指针定位错了，白修一个小时bug，我不说是谁
        ILCursor c2 = new ILCursor(il);
        if (c2.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Ldc_I4_M1),
            (i) => i.Match(OpCodes.Beq_S),
            (i) => i.Match(OpCodes.Ldarg_0),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Ldsfld),
            (i) => i.Match(OpCodes.Call)
            ))
        {
            c2.Emit(OpCodes.Ldarg_0);
            c2.EmitDelegate<Func<bool, Player, bool>>((isArtificer, self) =>
            {
                if (self.slugcatStats.name == Plugin.SlugcatStatsName)
                {
                    // 这么做是为了防止误触，因为我自己特么的误触好几次了，我想吃东西来着结果吃到刚才用来鲨人的矛，反倒吐了两格
                    if (Plugin.instance.option.CraftKey.Value == KeyCode.None) return true;
                    else if (Input.GetKey(Plugin.instance.option.CraftKey.Value)) return true;
                    else return false;
                }
                else { return isArtificer; }

            });
        }

    }








    private static void Player_BiteEdibleObject(On.Player.orig_BiteEdibleObject orig, Player self, bool eu)
    {
        if (self.slugcatStats.name == Plugin.SlugcatStatsName && self.grasps[0] != null && self.grasps[1] != null
            && (self.grasps[0].grabbed is SSOracleSwarmer || self.grasps[0].grabbed is SLOracleSwarmer) && self.grasps[1].grabbed is IPlayerEdible)
        {
            if ((self.grasps[1].grabbed as IPlayerEdible).BitesLeft == 1 && self.SessionRecord != null)
            {
                self.SessionRecord.AddEat(self.grasps[1].grabbed);
            }
            if (self.grasps[1].grabbed is Creature)
            {
                (self.grasps[1].grabbed as Creature).SetKillTag(self.abstractCreature);
            }
            if (self.graphicsModule != null)
            {
                (self.graphicsModule as PlayerGraphics).BiteFly(1);
            }
            (self.grasps[1].grabbed as IPlayerEdible).BitByPlayer(self.grasps[1], eu);
            return;
        }
        else { orig(self, eu); }

    }





    private static bool Player_ObjectCountsAsFood(On.Player.orig_ObjectCountsAsFood orig, Player self, PhysicalObject obj)
    {
        bool result = orig(self, obj);
        if (self.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            result = result && !(obj is OracleSwarmer);
        }
        return result;
    }



    private delegate bool orig_SLOracleSwarmerEdible(SLOracleSwarmer self);
    private static bool SLOracleSwarmer_Edible(orig_SLOracleSwarmerEdible orig, SLOracleSwarmer self)
    {
        var result = orig(self);
        if (self.grabbedBy.Count > 0 && self.grabbedBy[0] != null && self.grabbedBy[0].grabber is Player && (self.grabbedBy[0].grabber as Player).slugcatStats.name == Plugin.SlugcatStatsName)
        {
            result = false;
        }
        return result;
    }



    private delegate bool orig_SSOracleSwarmerEdible(SSOracleSwarmer self);
    private static bool SSOracleSwarmer_Edible(orig_SSOracleSwarmerEdible orig, SSOracleSwarmer self)
    {
        var result = orig(self);
        if (self.grabbedBy.Count > 0 && self.grabbedBy[0] != null && self.grabbedBy[0].grabber is Player && (self.grabbedBy[0].grabber as Player).slugcatStats.name == Plugin.SlugcatStatsName)
        {
            result = false;
        }
        return result;
    }









    private static bool Player_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
    {
        if (self.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            return (!ModManager.MSC || !(self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear)) && (testObj is Rock || testObj is DataPearl || testObj is FlareBomb || testObj is Lantern || testObj is FirecrackerPlant || (testObj is VultureGrub && !(testObj as VultureGrub).dead) || (testObj is Hazer && !(testObj as Hazer).dead && !(testObj as Hazer).hasSprayed) || testObj is FlyLure || testObj is ScavengerBomb || testObj is PuffBall || testObj is SporePlant || testObj is BubbleGrass || testObj is OracleSwarmer || testObj is NSHSwarmer || testObj is OverseerCarcass || (ModManager.MSC && testObj is FireEgg && self.FoodInStomach >= self.MaxFoodInStomach) || (ModManager.MSC && testObj is SingularityBomb && !(testObj as SingularityBomb).activateSingularity && !(testObj as SingularityBomb).activateSucktion));
        }
        else { return orig(self, testObj); }
    }








    #endregion





}
