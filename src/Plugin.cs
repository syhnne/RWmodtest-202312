using System;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using System.Drawing.Text;
using RWCustom;
using Noise;
using System.Collections.Generic;

namespace SlugTemplate
{
    [BepInPlugin(MOD_ID, "Slugcat Test-01", "0.1.0")]
    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "Emily1212.slugtemplate";

        public static readonly PlayerFeature<bool> IsMyCat = PlayerBool("test01/is_my_cat?????");
        public static readonly PlayerFeature<float> SuperJump = PlayerFloat("test01/super_jump");
        public static readonly PlayerFeature<bool> ExplodeOnDeath = PlayerBool("test01/explode_on_death");

        // 我加的
        public static readonly PlayerFeature<int> ExplosionCapacity = PlayerInt("test01/explosion_capacity");


        // 内部变量，不要管他


        




        // 加入钩子
        public void OnEnable()
        {
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

            On.Player.Die += Player_Die;
            On.Player.ClassMechanicsArtificer += Player_ClassMechanicsArtificer;
        }
        
        // 加载资源。我不知道怎么加载资源，没人说过（汗）
        private void LoadResources(RainWorld rainWorld)
        {
        }







        // 我焯啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊
        // 好，已经明显地胜出！！明天改具体的技能
        private void Player_ClassMechanicsArtificer(On.Player.orig_ClassMechanicsArtificer orig, Player self) 
        {

            Room room = self.room;
            bool flag = self.wantToJump > 0 && self.input[0].pckp;
            bool flag2 = self.eatMeat >= 20 || self.maulTimer >= 15;
            ExplosionCapacity.TryGet(self, out int explosionCapacity);
            int num = Mathf.Max(1, explosionCapacity - 5);

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
                if (UnityEngine.Random.value < 0.25f)
                {
                    // 啊 这是爆炸效果 下次一定（目移）
                    self.room.AddObject(new Explosion.ExplosionSmoke(self.mainBodyChunk.pos, Custom.RNV() * 2f * UnityEngine.Random.value, 1f));
                }
                if (UnityEngine.Random.value < 0.5f)
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
                for (int i = 0; i < 8; i++)
                {
                    self.room.AddObject(new Explosion.ExplosionSmoke(pos, Custom.RNV() * 5f * UnityEngine.Random.value, 1f));
                }
                self.room.AddObject(new Explosion.ExplosionLight(pos, 160f, 1f, 3, Color.white));
                for (int j = 0; j < 10; j++)
                {
                    Vector2 vector = Custom.RNV();
                    self.room.AddObject(new Spark(pos + vector * UnityEngine.Random.value * 40f, vector * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white, null, 4, 18));
                }
                self.room.PlaySound(SoundID.Fire_Spear_Explode, pos, 0.3f + UnityEngine.Random.value * 0.3f, 0.5f + UnityEngine.Random.value * 2f);
                self.room.InGameNoise(new InGameNoise(pos, 8000f, self, 1f));
                int num2 = Mathf.Max(1, explosionCapacity - 3);
                if (self.bodyMode == Player.BodyModeIndex.ZeroG || self.room.gravity == 0f || self.gravity == 0f)
                {
                    float num3 = (float)self.input[0].x;
                    float num4 = (float)self.input[0].y;
                    while (num3 == 0f && num4 == 0f)
                    {
                        num3 = (float)(((double)UnityEngine.Random.value <= 0.33) ? 0 : (((double)UnityEngine.Random.value <= 0.5) ? 1 : -1));
                        num4 = (float)(((double)UnityEngine.Random.value <= 0.33) ? 0 : (((double)UnityEngine.Random.value <= 0.5) ? 1 : -1));
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
                        self.bodyChunks[0].vel.x = 10f * (float)self.input[0].x;
                        self.bodyChunks[1].vel.x = 8f * (float)self.input[0].x;
                    }
                    else
                    {
                        self.bodyChunks[0].vel.x = 15f * (float)self.input[0].x;
                        self.bodyChunks[1].vel.x = 13f * (float)self.input[0].x;
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
                    self.PyroDeath();
                }
                // room.AddObject(new ExplosionSpikes(room, pos, 14, 30f, 9f, 7f, 170f, Color.white));
                // 这只是一个标志物，表示现在应该执行跳跃。我回头再写具体执行的代码，因为我得先处理一下这个麻烦的条件问题（扶额
                // 算了，我先全都用炸猫的代码。接下来这个猫应该会像炸猫一样二段跳
            }
            else if (flag 
                && !self.submerged 
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
                for (int k = 0; k < 8; k++)
                {
                    self.room.AddObject(new Explosion.ExplosionSmoke(pos2, Custom.RNV() * 5f * UnityEngine.Random.value, 1f));
                }
                self.room.AddObject(new Explosion.ExplosionLight(pos2, 160f, 1f, 3, Color.white));
                for (int l = 0; l < 10; l++)
                {
                    Vector2 vector2 = Custom.RNV();
                    self.room.AddObject(new Spark(pos2 + vector2 * UnityEngine.Random.value * 40f, vector2 * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white, null, 4, 18));
                }
                self.room.AddObject(new ShockWave(pos2, 200f, 0.2f, 6, false));
                self.room.PlaySound(SoundID.Fire_Spear_Explode, pos2, 0.3f + UnityEngine.Random.value * 0.3f, 0.5f + UnityEngine.Random.value * 2f);
                self.room.InGameNoise(new InGameNoise(pos2, 8000f, self, 1f));
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
                                }
                                else
                                {
                                    creature.Stun(80);
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
                    self.PyroDeath();
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








        // 尸体会爆炸
        private void Player_Die(On.Player.orig_Die orig, Player self)
        {
            bool wasDead = self.dead;

            orig(self);

            if(!wasDead
                && self.dead
                && ExplodeOnDeath.TryGet(self, out bool explosionCapacity)
                && explosionCapacity)
            {

                // Adapted from ScavengerBomb.explosionCapacity

                // 表示玩家所在在房间的Room类的实例
                var room = self.room;
                // 表示玩家在房间中位置的Vector2（二维向量）实例
                var pos = self.mainBodyChunk.pos;
                // 表示玩家的标志颜色的Color类实例
                var color = self.ShortCutColor();

                room.AddObject(new Explosion(room, self, pos, 7, 250f, 6.2f, 2f, 280f, 0.25f, self, 0.7f, 160f, 1f));
                room.AddObject(new Explosion.ExplosionLight(pos, 280f, 1f, 7, color));
                room.AddObject(new Explosion.ExplosionLight(pos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
                room.AddObject(new ExplosionSpikes(room, pos, 14, 30f, 9f, 7f, 170f, color));
                room.AddObject(new ShockWave(pos, 330f, 0.045f, 5, false));

                self.mainBodyChunk.vel = Vector2.up * 3000f;

                room.ScreenMovement(pos, default, 1.3f);
                room.PlaySound(SoundID.Bomb_Explode, pos);
                room.InGameNoise(new Noise.InGameNoise(pos, 9000f, self, 1f));


            }
        }

        







        /*
        self.input[0].pckp 这应该是我要的炸弹跳的按键，我猜的，因为那个词是p打头（pyro）
        错了。。好像很多input都是这个，我得试。。。
        好消息！pyroJumpCounter是Player里头的一个变量，我可以直接用（喜）
        啊，还有那套运算逻辑我也可以直接复制。看来我那天口嗨的内容某种意义上是正确的

        啊？为什么我不能hook

        没事了。他报错只是单纯地因为，我写了这个函数，但还没用上它。我操。。我捯饬俩小时。。
        */


    }
}