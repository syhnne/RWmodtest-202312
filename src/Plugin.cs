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


using Menu.Remix.MixedUI;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;





namespace PebblesSlug;








[BepInPlugin(MOD_ID, "PebblesSlug", "0.1.0")]
class Plugin : BaseUnityPlugin
{
    private const string MOD_ID = "PebblesSlug_by_syhnne";



    // 发布之前记得把这个IsMyCat改成一个什么别的东西。这玩意儿肯定有其他的猫要用，我怕有冲突
    // 没事了，我以后再也不要用这个了。。



    public static readonly PlayerFeature<bool> IsMyCat = PlayerBool("slugtemplate/is_my_cat");


    public static new ManualLogSource Logger { get; private set; }

    public PebblesSlugOption option;
    private bool IsInit;





    // 加入钩子
    public void OnEnable()
    {
        try
        {
            this.option = new PebblesSlugOption();

            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

            On.Player.ClassMechanicsArtificer += Player_ClassMechanicsArtificer;
            On.Player.CraftingResults += Player_CraftingResults;
            On.Player.GraspsCanBeCrafted += Player_GraspsCanBeCrafted;
            // On.Player.SwallowObject += Player_SwallowObject;
            On.Player.SpitUpCraftedObject += Player_SpitUpCraftedObject;
            On.Player.GrabUpdate += Player_GrabUpdate;
            On.Creature.Violence += Creature_Violence;
            // On.Room.PlaySound_SoundID_Vector2 += Room_PlaySound_SoundID_Vector2;
            // On.UnderwaterShock.Update += UnderwaterShock_Update;

            On.Player.CanBeSwallowed += Player_CanBeSwallowed;
            // On.PlayerGraphics.ColoredBodyPartList += PlayerGraphics_ColoredBodyPartList;

            
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            base.Logger.LogError(ex);
        }

    }









    // 噢噢噢哦哦哦哦哦哦哦哦！！（恍然大悟）
    private void LoadResources(RainWorld rainWorld)
    {
        try
        {
            MachineConnector.SetRegisteredOI("PebblesSlug_by_syhnne", this.option);
            bool isInit = this.IsInit;
            if (!isInit)
            {
                this.IsInit = true;
                Futile.atlasManager.LoadAtlas("atlases/head");
                Futile.atlasManager.LoadAtlas("atlases/tail");
                Futile.atlasManager.LoadAtlas("atlases/face");
                Futile.atlasManager.LoadAtlas("atlases/scarf");
            }
        }
        catch (Exception ex)
        {
            base.Logger.LogError(ex);
            throw;
        }
    }
    



    /*
    private List<string> PlayerGraphics_ColoredBodyPartList(On.PlayerGraphics.orig_ColoredBodyPartList orig, SlugcatStats.Name slugcatID)
    {
        if ()
        {
            List<string> list = new() { "Body", "Eyes", "Scarf" };
            return list;
        }
        else { return orig(slugcatID); }
    }
    */




    // 不能吃神经元
    private bool Player_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
    {
        if (IsMyCat.TryGet(self, out bool is_my_cat) && is_my_cat)
        {
            return (!ModManager.MSC || !(self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear)) && (testObj is Rock || testObj is DataPearl || testObj is FlareBomb || testObj is Lantern || testObj is FirecrackerPlant || (testObj is VultureGrub && !(testObj as VultureGrub).dead) || (testObj is Hazer && !(testObj as Hazer).dead && !(testObj as Hazer).hasSprayed) || testObj is FlyLure || testObj is ScavengerBomb || testObj is PuffBall || testObj is SporePlant || testObj is BubbleGrass || testObj is OracleSwarmer || testObj is NSHSwarmer || testObj is OverseerCarcass || (ModManager.MSC && testObj is FireEgg && self.FoodInStomach >= self.MaxFoodInStomach) || (ModManager.MSC && testObj is SingularityBomb && !(testObj as SingularityBomb).activateSingularity && !(testObj as SingularityBomb).activateSucktion));
        }
        else { return orig(self, testObj); }
    }




    // 不能免疫蜈蚣的电击，但我认为这不是我的问题，是蜈蚣的问题。
    // 算了吧，要是连这都免疫，那就太超模了（
    private void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        if (self is Player && IsMyCat.TryGet((Player)self, out bool is_my_cat) && is_my_cat)
        {
            if (type == Creature.DamageType.Electric)
            {
                Debug.Log("==== DAMAGE TYPE: electric");
                // 现在是0做测试用，检测哪些死法是通过这个函数传进来的
                // damage = 0;
                // stunBonus = 0;
                damage = 0.1f * damage;
                stunBonus = 0.1f * stunBonus;
            }
        }
        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
    }






    private void UnderwaterShock_Update(On.UnderwaterShock.orig_Update orig, UnderwaterShock self, bool eu)
    {
        if (self.killTagHolder is Player && IsMyCat.TryGet((Player)self.killTagHolder, out bool is_my_cat) && is_my_cat)
        {
            self.Update(eu);
            float num = self.rad * (0.25f + 0.75f * Mathf.Sin(Mathf.InverseLerp(0f, (float)self.lifeTime, (float)self.frame) * 3.1415927f));
            for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
            {
                /*
                // 上一次写这种连环死亡大判定时卡了一下午的bug
                // 噢 我真不该说这句话
                // 没事了，不是我的问题，我加了这个hook就会卡顿，不知道为啥
                if (ModManager.CoopAvailable && !Custom.rainWorld.options.friendlyFire && self.room.abstractRoom.creatures[i].realizedCreature != null && self.room.abstractRoom.creatures[i].realizedCreature is Player && (self.room.abstractRoom.creatures[i].realizedCreature as Player).isNPC)
                {
                    Debug.Log("============== FRIENDLY FIRE =================== this should be working ");
                }
                */

                if (self.room.abstractRoom.creatures[i].realizedCreature != null && self.room.abstractRoom.creatures[i].realizedCreature != self.expemtObject && self.room.abstractRoom.creatures[i].realizedCreature.Submersion > 0f)
                {
                    float num2 = 0f;
                    for (int j = 0; j < self.room.abstractRoom.creatures[i].realizedCreature.bodyChunks.Length; j++)
                    {
                        if (Custom.DistLess(self.pos, self.room.abstractRoom.creatures[i].realizedCreature.bodyChunks[j].pos, num))
                        {
                            num2 = Mathf.Max(num2, Custom.LerpMap(Vector2.Distance(self.pos, self.room.abstractRoom.creatures[i].realizedCreature.bodyChunks[j].pos), num / 2f, num, 1f, 0f, 0.5f) * self.room.abstractRoom.creatures[i].realizedCreature.bodyChunks[j].submersion);
                        }
                    }
                    if (self.room.abstractRoom.shelter)
                    {
                        num2 = 0f;
                    }
                    if (num2 > 0f)
                    {
                        for (int k = 0; k < self.room.abstractRoom.creatures[i].realizedCreature.bodyChunks.Length; k++)
                        {
                            self.room.abstractRoom.creatures[i].realizedCreature.bodyChunks[k].vel += Custom.RNV() * num2 * Mathf.Min(5f, self.room.abstractRoom.creatures[i].realizedCreature.bodyChunks[k].rad);
                        }
                        if (Random.value < 0.25f)
                        {
                            self.room.AddObject(new UnderwaterShock.Flash(self.room.abstractRoom.creatures[i].realizedCreature.bodyChunks[Random.Range(0, self.room.abstractRoom.creatures[i].realizedCreature.bodyChunks.Length)].pos, self.room.abstractRoom.creatures[i].realizedCreature.TotalMass * 60f * num2 + 140f, Mathf.Pow(num2, 0.2f), self.lifeTime - self.frame, self.color));
                        }
                        self.room.abstractRoom.creatures[i].realizedCreature.Violence(null, null, self.room.abstractRoom.creatures[i].realizedCreature.bodyChunks[Random.Range(0, self.room.abstractRoom.creatures[i].realizedCreature.bodyChunks.Length)], null, Creature.DamageType.Electric, self.damage * num2, self.damage * num2 * 240f + 30f);
                    }
                }
            }
            self.frame++;
            if (self.frame > self.lifeTime)
            {
                self.Destroy();
            }
        }
        else { orig(self, eu); }
    }






    // 除了特效以外，数值跟炸猫差不多，因为我不知道那堆二段跳的数值怎么改。我想改得小一点，让他没有那么强的机动性，不然太超模了（
    // 因为这个电击在水下是有伤害的（痛击你的队友。jpg）我不是故意的，我是真的写不出来那个判定。我不知道他为什么会闪退。。
    private void Player_ClassMechanicsArtificer(On.Player.orig_ClassMechanicsArtificer orig, Player self)
    {
        if (IsMyCat.TryGet(self, out bool is_my_cat) && is_my_cat)
        {
            Room room = self.room;
            bool flag = self.wantToJump > 0 && self.input[0].pckp;
            bool flag2 = self.eatMeat >= 20 || self.maulTimer >= 15;
            int explosionCapacity = PebblesSlugOption.ExplosionCapacity.Value;
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

                // for (int i = 0; i < 8; i++)
                // { self.room.AddObject(new Explosion.ExplosionSmoke(pos, Custom.RNV() * 5f * Random.value, 1f)); }
                for (int i = 0; i < 8; i++)
                {
                    Vector2 vector = Custom.DegToVec(360f * Random.value);
                    self.room.AddObject(new MouseSpark(pos + vector * 9f, self.firstChunk.vel + vector * 36f * Random.value, 20f, new Color(0.7f, 1f, 1f)));
                }
                self.room.AddObject(new Explosion.ExplosionLight(pos, 200f, 1f, 4, new Color(0.7f, 1f, 1f)));
                // self.room.AddObject(new Explosion.ExplosionLight(pos, 160f, 1f, 3, Color.white));
                for (int j = 0; j < 10; j++)
                {
                    Vector2 vector = Custom.RNV();
                    self.room.AddObject(new Spark(pos + vector * Random.value * 40f, vector * Mathf.Lerp(4f, 30f, Random.value), Color.white, null, 4, 18));
                }
                self.room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, self.firstChunk.pos);
                // self.room.PlaySound(SoundID.Fire_Spear_Explode, pos, 0.3f + Random.value * 0.3f, 0.5f + Random.value * 2f);
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
                    // Player没有有关被电死的代码，而且我找不到fp里面那个能电死猫的电池代码在哪（扶额
                    self.PyroDeath();
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
                // for (int k = 0; k < 8; k++)
                // { self.room.AddObject(new Explosion.ExplosionSmoke(pos2, Custom.RNV() * 5f * Random.value, 1f)); }

                self.room.AddObject(new Explosion.ExplosionLight(pos2, 200f, 1f, 4, new Color(0.7f, 1f, 1f)));
                // self.room.AddObject(new Explosion.ExplosionLight(pos2, 160f, 1f, 3, Color.white));

                for (int l = 0; l < 8; l++)
                {
                    Vector2 vector2 = Custom.RNV();
                    self.room.AddObject(new Spark(pos2 + vector2 * Random.value * 40f, vector2 * Mathf.Lerp(4f, 30f, Random.value), Color.white, null, 4, 18));
                }
                self.room.AddObject(new ShockWave(pos2, 200f, 0.2f, 6, false));
                self.room.PlaySound(SoundID.Flare_Bomb_Burn, pos2);
                // self.room.PlaySound(SoundID.Zapper_Zap, pos2, 1f, 0.2f + 0.25f * Random.value);
                // self.room.PlaySound(SoundID.Fire_Spear_Explode, pos2, 0.3f + Random.value * 0.3f, 0.5f + Random.value * 2f);
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




    private AbstractPhysicalObject.AbstractObjectType Player_CraftingResults(On.Player.orig_CraftingResults orig, Player self)
    {
        if (IsMyCat.TryGet(self, out bool is_my_cat) && is_my_cat)
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
                if (grasps[0] != null && grasps[0].grabbed is Spear)
                {
                    AbstractPhysicalObject spear = self.grasps[0].grabbed.abstractPhysicalObject;

                    if (!((spear as AbstractSpear).electric && (spear as AbstractSpear).electricCharge > 0))
                    {
                        return AbstractPhysicalObject.AbstractObjectType.Spear;
                    }

                }
                if (grasps[0] == null && grasps[1] != null && grasps[1].grabbed is Spear && self.objectInStomach == null)
                {
                    AbstractPhysicalObject spear = self.grasps[1].grabbed.abstractPhysicalObject;
                    if (!((spear as AbstractSpear).electric && (spear as AbstractSpear).electricCharge > 0))
                    {
                        return AbstractPhysicalObject.AbstractObjectType.Spear;
                    }
                }

            }
            return null;
        }
        else { orig(self); }
        return null;
    }


    private bool Player_GraspsCanBeCrafted(On.Player.orig_GraspsCanBeCrafted orig, Player self)
    {

        if (IsMyCat.TryGet(self, out bool is_my_cat) && is_my_cat && (self.CraftingResults() != null))
        {
            return (self.CraftingResults() != null);
        }
        orig(self);
        return orig(self);
    }




    // 下面这个暂时没用，但先留着以防我日后突然想给他加点别的能力
    private void Player_SwallowObject(On.Player.orig_SwallowObject orig, Player self, int grasp)
    {
        if (IsMyCat.TryGet(self, out bool is_my_cat) && is_my_cat)
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
            if (ModManager.MMF && self.room.game.session is StoryGameSession)
            {
                (self.room.game.session as StoryGameSession).RemovePersistentTracker(self.objectInStomach);
            }
            self.ReleaseGrasp(grasp);
            self.objectInStomach.realizedObject.RemoveFromRoom();
            self.objectInStomach.Abstractize(self.abstractCreature.pos);
            self.objectInStomach.Room.RemoveEntity(self.objectInStomach);
            Debug.Log("==== swallowobject");
            if (self.FoodInStomach > 0)
            {
                if (abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.Spear && !(abstractPhysicalObject as AbstractSpear).explosive && !(abstractPhysicalObject as AbstractSpear).electric)
                {
                    // 这应该是生成矛的代码。那么它为什么不起作用呢（恼
                    Debug.Log("==== swallowobject: holding spear but why isn't this working");
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





    private void Player_SpitUpCraftedObject(On.Player.orig_SpitUpCraftedObject orig, Player self)
    {
        if (IsMyCat.TryGet(self, out bool is_my_cat) && is_my_cat)
        {
            // 表示玩家所在在房间的Room类的实例
            var room = self.room;
            // 表示玩家在房间中位置的Vector2（二维向量）实例
            var vector = self.mainBodyChunk.pos;
            // 表示玩家的标志颜色的Color类实例
            var color = self.ShortCutColor();

            // 我要写一个craftingtutorial，并且单独绑一个变量，因为它内容跟原来的不一样
            self.room.PlaySound(SoundID.Slugcat_Swallow_Item, self.mainBodyChunk);

            for (int i = 0; i < self.grasps.Length; i++)
            {
                if (self.grasps[i] != null)
                {
                    AbstractPhysicalObject abstractPhysicalObject = self.grasps[i].grabbed.abstractPhysicalObject;

                    Debug.Log("==== spit up crafted object : !");
                    // 这应该是具体的生成规则，我不知道这里有没有bug。。他完全没说把什么矛合成什么矛，这些东西都是分开的，放在不同的函数里的。
                    // 错误的，他做炸矛的时候压根没调用这个函数，给我cpu干烧了。他是哪一步做出来的？？
                    if (abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.Spear && !((abstractPhysicalObject as AbstractSpear).electric && (abstractPhysicalObject as AbstractSpear).electricCharge > 0))
                    {
                        if ((abstractPhysicalObject as AbstractSpear).explosive)
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
                        else
                        {
                            self.ReleaseGrasp(i);
                            abstractPhysicalObject.realizedObject.RemoveFromRoom();
                            self.room.abstractRoom.RemoveEntity(abstractPhysicalObject);
                            // 对了，生成矛是这个。。我可能是那个眼瞎。。
                            self.SubtractFood(2);
                            AbstractSpear abstractSpear = new AbstractSpear(self.room.world, null, self.abstractCreature.pos, self.room.game.GetNewID(), false, true);
                            self.room.abstractRoom.AddEntity(abstractSpear);
                            abstractSpear.RealizeInRoom();
                            if (self.FreeHand() != -1)
                            {
                                self.SlugcatGrab(abstractSpear.realizedObject, self.FreeHand());
                            }
                        }
                        return;
                    }
                }
            }

            AbstractPhysicalObject abstractPhysicalObject2 = null;

            if (abstractPhysicalObject2 != null && self.FreeHand() != -1)
            {
                self.SlugcatGrab(abstractPhysicalObject2.realizedObject, self.FreeHand());
            }
        }
        else { orig(self); }
    }







    // 我操，这是什么地狱代码。

    // 接下来就看hook这玩意儿运行速度如何了，希望我在这里写的东西不会让运算量呈指数级增长
    // 两种改动：1.删除有关【这个猫是不是工匠】的判断，因为我调的工匠代码，某种意义上它一定是工匠 2.删除有关【这个猫是不是……】的判断，因为它一定不是
    // 除此以外都没改。突出一个无脑。我本不想全复制一遍的，但我看不懂。。
    // 还需要改一些有关不能吃神经元的代码
    private void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
    {
        if (IsMyCat.TryGet(self, out bool is_my_cat) && is_my_cat)
        {
            if (self.spearOnBack != null)
            {
                self.spearOnBack.Update(eu);
            }
            if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null)
            {
                self.slugOnBack.Update(eu);
            }
            bool flag = ((self.input[0].x == 0 && self.input[0].y == 0 && !self.input[0].jmp && !self.input[0].thrw) || (ModManager.MMF && self.input[0].x == 0 && self.input[0].y == 1 && !self.input[0].jmp && !self.input[0].thrw && (self.bodyMode != Player.BodyModeIndex.ClimbingOnBeam || self.animation == Player.AnimationIndex.BeamTip || self.animation == Player.AnimationIndex.StandOnBeam))) && (self.mainBodyChunk.submersion < 0.5f || self.isRivulet);
            bool flag2 = false;
            bool flag3 = false;
            self.craftingObject = false;
            int num = -1;
            int num2 = -1;
            bool flag4 = false;
            if (ModManager.MSC && !self.input[0].pckp && self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear)
            {
                PlayerGraphics.TailSpeckles tailSpecks = (self.graphicsModule as PlayerGraphics).tailSpecks;
                if (tailSpecks.spearProg > 0f)
                {
                    tailSpecks.setSpearProgress(Mathf.Lerp(tailSpecks.spearProg, 0f, 0.05f));
                    if (tailSpecks.spearProg < 0.025f)
                    {
                        tailSpecks.setSpearProgress(0f);
                    }
                }
                else
                {
                    self.smSpearSoundReady = false;
                }
            }
            if (self.input[0].pckp && !self.input[1].pckp && self.switchHandsProcess == 0f && !self.isSlugpup)
            {
                bool flag5 = self.grasps[0] != null || self.grasps[1] != null;
                if (self.grasps[0] != null && (self.Grabability(self.grasps[0].grabbed) == Player.ObjectGrabability.TwoHands || self.Grabability(self.grasps[0].grabbed) == Player.ObjectGrabability.Drag))
                {
                    flag5 = false;
                }
                if (flag5)
                {
                    if (self.switchHandsCounter == 0)
                    {
                        self.switchHandsCounter = 15;
                    }
                    else
                    {
                        self.room.PlaySound(SoundID.Slugcat_Switch_Hands_Init, self.mainBodyChunk);
                        self.switchHandsProcess = 0.01f;
                        self.wantToPickUp = 0;
                        self.noPickUpOnRelease = 20;
                    }
                }
                else
                {
                    self.switchHandsProcess = 0f;
                }
            }
            if (self.switchHandsProcess > 0f)
            {
                float num3 = self.switchHandsProcess;
                self.switchHandsProcess += 0.083333336f;
                if (num3 < 0.5f && self.switchHandsProcess >= 0.5f)
                {
                    self.room.PlaySound(SoundID.Slugcat_Switch_Hands_Complete, self.mainBodyChunk);
                    self.SwitchGrasps(0, 1);
                }
                if (self.switchHandsProcess >= 1f)
                {
                    self.switchHandsProcess = 0f;
                }
            }
            int num4 = -1;
            int num5 = -1;
            int num6 = -1;
            if (flag)
            {
                int num7 = -1;
                if (ModManager.MSC)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (self.grasps[i] != null)
                        {
                            if (self.grasps[i].grabbed is JokeRifle)
                            {
                                num2 = i;
                            }
                            else if (JokeRifle.IsValidAmmo(self.grasps[i].grabbed))
                            {
                                num = i;
                            }
                        }
                    }
                }
                int num8 = 0;
                // 这下猫不会吃神经元了
                while (num5 < 0 && num8 < 2 && (!ModManager.MSC || self.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear))
                {
                    if (self.grasps[num8] != null && !(self.grasps[num8].grabbed is OracleSwarmer) && self.grasps[num8].grabbed is IPlayerEdible && (self.grasps[num8].grabbed as IPlayerEdible).Edible)
                    {
                        num5 = num8;
                    }
                    num8++;
                }
                if ((num5 == -1 || (self.FoodInStomach >= self.MaxFoodInStomach && !(self.grasps[num5].grabbed is KarmaFlower) && !(self.grasps[num5].grabbed is Mushroom))) && (self.objectInStomach == null || self.CanPutSpearToBack || self.CanPutSlugToBack))
                {
                    int num9 = 0;
                    while (num7 < 0 && num4 < 0 && num6 < 0 && num9 < 2)
                    {
                        if (self.grasps[num9] != null)
                        {
                            if ((self.CanPutSlugToBack && self.grasps[num9].grabbed is Player && !(self.grasps[num9].grabbed as Player).dead) || self.CanIPutDeadSlugOnBack(self.grasps[num9].grabbed as Player))
                            {
                                num6 = num9;
                            }
                            else if (self.CanPutSpearToBack && self.grasps[num9].grabbed is Spear)
                            {
                                num4 = num9;
                            }
                            else if (self.CanBeSwallowed(self.grasps[num9].grabbed))
                            {
                                num7 = num9;
                            }
                        }
                        num9++;
                    }
                }
                if (num5 > -1 && self.noPickUpOnRelease < 1)
                {
                    if (!self.input[0].pckp)
                    {
                        int num10 = 1;
                        while (num10 < 10 && self.input[num10].pckp)
                        {
                            num10++;
                        }
                        if (num10 > 1 && num10 < 10)
                        {
                            self.PickupPressed();
                        }
                    }
                }
                else if (self.input[0].pckp && !self.input[1].pckp)
                {
                    self.PickupPressed();
                }
                if (self.input[0].pckp)
                {
                    if (ModManager.MSC && self.GraspsCanBeCrafted())
                    {
                        self.craftingObject = true;
                        flag3 = true;
                        num5 = -1;
                    }
                    if (num6 > -1 || self.CanRetrieveSlugFromBack)
                    {
                        self.slugOnBack.increment = true;
                    }
                    else if (num4 > -1 || self.CanRetrieveSpearFromBack)
                    {
                        self.spearOnBack.increment = true;
                    }
                    else if ((num7 > -1 || self.objectInStomach != null || self.isGourmand) && (!ModManager.MSC || self.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear))
                    {
                        flag3 = true;
                    }
                    if (num > -1 && num2 > -1)
                    {
                        flag4 = true;
                    }
                    // 这里本来有一段，可能是矛大师生产矛的代码
                }
                if (num5 > -1 && self.wantToPickUp < 1 && (self.input[0].pckp || self.eatCounter <= 15) && self.Consious && Custom.DistLess(self.mainBodyChunk.pos, self.mainBodyChunk.lastPos, 3.6f))
                {
                    if (self.graphicsModule != null)
                    {
                        (self.graphicsModule as PlayerGraphics).LookAtObject(self.grasps[num5].grabbed);
                    }
                    if (ModManager.MSC && self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && (self.KarmaCap == 9 || (self.room.game.IsArenaSession && self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.gameType != MoreSlugcatsEnums.GameTypeID.Challenge) || (self.room.game.session is ArenaGameSession && self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.gameType == MoreSlugcatsEnums.GameTypeID.Challenge && self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.challengeMeta.ascended)) && self.grasps[num5].grabbed is Fly && self.eatCounter < 1)
                    {
                        self.room.PlaySound(SoundID.Snail_Pop, self.mainBodyChunk, false, 1f, 1.5f + Random.value);
                        self.eatCounter = 30;
                        self.room.AddObject(new ShockWave(self.grasps[num5].grabbed.firstChunk.pos, 25f, 0.8f, 4, false));
                        for (int l = 0; l < 5; l++)
                        {
                            self.room.AddObject(new Spark(self.grasps[num5].grabbed.firstChunk.pos, Custom.RNV() * 3f, Color.yellow, null, 25, 90));
                        }
                        self.grasps[num5].grabbed.Destroy();
                        self.grasps[num5].grabbed.abstractPhysicalObject.Destroy();
                        if (self.room.game.IsArenaSession)
                        {
                            self.AddFood(1);
                        }
                    }
                    flag2 = true;
                    if (self.FoodInStomach < self.MaxFoodInStomach || self.grasps[num5].grabbed is KarmaFlower || self.grasps[num5].grabbed is Mushroom)
                    {
                        flag3 = false;
                        if (self.spearOnBack != null)
                        {
                            self.spearOnBack.increment = false;
                        }
                        if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null)
                        {
                            self.slugOnBack.increment = false;
                        }
                        if (self.eatCounter < 1)
                        {
                            self.eatCounter = 15;
                            self.BiteEdibleObject(eu);
                        }
                    }
                    else if (self.eatCounter < 20 && self.room.game.cameras[0].hud != null)
                    {
                        self.room.game.cameras[0].hud.foodMeter.RefuseFood();
                    }
                }
            }
            else if (self.input[0].pckp && !self.input[1].pckp)
            {
                self.PickupPressed();
            }
            else
            {
                if (self.CanPutSpearToBack)
                {
                    for (int m = 0; m < 2; m++)
                    {
                        if (self.grasps[m] != null && self.grasps[m].grabbed is Spear)
                        {
                            num4 = m;
                            break;
                        }
                    }
                }
                if (self.CanPutSlugToBack)
                {
                    for (int n = 0; n < 2; n++)
                    {
                        if (self.grasps[n] != null && self.grasps[n].grabbed is Player && !(self.grasps[n].grabbed as Player).dead)
                        {
                            num6 = n;
                            break;
                        }
                    }
                }
                if (self.input[0].pckp && (num6 > -1 || self.CanRetrieveSlugFromBack))
                {
                    self.slugOnBack.increment = true;
                }
                if (self.input[0].pckp && (num4 > -1 || self.CanRetrieveSpearFromBack))
                {
                    self.spearOnBack.increment = true;
                }
            }
            int num11 = 0;
            if (ModManager.MMF && (self.grasps[0] == null || !(self.grasps[0].grabbed is Creature)) && self.grasps[1] != null && self.grasps[1].grabbed is Creature)
            {
                num11 = 1;
            }
            if (ModManager.MSC && SlugcatStats.SlugcatCanMaul(self.SlugCatClass))
            {
                if (self.input[0].pckp && self.grasps[num11] != null && self.grasps[num11].grabbed is Creature && (self.CanMaulCreature(self.grasps[num11].grabbed as Creature) || self.maulTimer > 0))
                {
                    self.maulTimer++;
                    (self.grasps[num11].grabbed as Creature).Stun(60);
                    self.MaulingUpdate(num11);
                    if (self.spearOnBack != null)
                    {
                        self.spearOnBack.increment = false;
                        self.spearOnBack.interactionLocked = true;
                    }
                    if (self.slugOnBack != null)
                    {
                        self.slugOnBack.increment = false;
                        self.slugOnBack.interactionLocked = true;
                    }
                    if (self.grasps[num11] != null && self.maulTimer % 40 == 0)
                    {
                        self.room.PlaySound(SoundID.Slugcat_Eat_Meat_B, self.mainBodyChunk);
                        self.room.PlaySound(SoundID.Drop_Bug_Grab_Creature, self.mainBodyChunk, false, 1f, 0.76f);
                        if (RainWorld.ShowLogs)
                        {
                            Debug.Log("Mauled target");
                        }
                        if (!(self.grasps[num11].grabbed as Creature).dead)
                        {
                            for (int num12 = Random.Range(8, 14); num12 >= 0; num12--)
                            {
                                self.room.AddObject(new WaterDrip(Vector2.Lerp(self.grasps[num11].grabbedChunk.pos, self.mainBodyChunk.pos, Random.value) + self.grasps[num11].grabbedChunk.rad * Custom.RNV() * Random.value, Custom.RNV() * 6f * Random.value + Custom.DirVec(self.grasps[num11].grabbed.firstChunk.pos, (self.mainBodyChunk.pos + (self.graphicsModule as PlayerGraphics).head.pos) / 2f) * 7f * Random.value + Custom.DegToVec(Mathf.Lerp(-90f, 90f, Random.value)) * Random.value * self.EffectiveRoomGravity * 7f, false));
                            }
                            Creature creature = self.grasps[num11].grabbed as Creature;
                            creature.SetKillTag(self.abstractCreature);
                            creature.Violence(self.bodyChunks[0], new Vector2?(new Vector2(0f, 0f)), self.grasps[num11].grabbedChunk, null, Creature.DamageType.Bite, 1f, 15f);
                            creature.stun = 5;
                            if (creature.abstractCreature.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.Inspector)
                            {
                                creature.Die();
                            }
                        }
                        self.maulTimer = 0;
                        self.wantToPickUp = 0;
                        if (self.grasps[num11] != null)
                        {
                            self.TossObject(num11, eu);
                            self.ReleaseGrasp(num11);
                        }
                        self.standing = true;
                    }
                    return;
                }
                if (self.grasps[num11] != null && self.grasps[num11].grabbed is Creature && (self.grasps[num11].grabbed as Creature).Consious && !self.IsCreatureLegalToHoldWithoutStun(self.grasps[num11].grabbed as Creature))
                {
                    if (RainWorld.ShowLogs)
                    {
                        Debug.Log("Lost hold of live mauling target");
                    }
                    self.maulTimer = 0;
                    self.wantToPickUp = 0;
                    self.ReleaseGrasp(num11);
                    return;
                }
            }
            if (self.input[0].pckp && self.grasps[num11] != null && self.grasps[num11].grabbed is Creature && self.CanEatMeat(self.grasps[num11].grabbed as Creature) && (self.grasps[num11].grabbed as Creature).Template.meatPoints > 0)
            {
                self.eatMeat++;
                self.EatMeatUpdate(num11);
                if (!ModManager.MMF)
                {
                }
                if (self.spearOnBack != null)
                {
                    self.spearOnBack.increment = false;
                    self.spearOnBack.interactionLocked = true;
                }
                if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null)
                {
                    self.slugOnBack.increment = false;
                    self.slugOnBack.interactionLocked = true;
                }
                if (self.grasps[num11] != null && self.eatMeat % 80 == 0 && ((self.grasps[num11].grabbed as Creature).State.meatLeft <= 0 || self.FoodInStomach >= self.MaxFoodInStomach))
                {
                    self.eatMeat = 0;
                    self.wantToPickUp = 0;
                    self.TossObject(num11, eu);
                    self.ReleaseGrasp(num11);
                    self.standing = true;
                }
                return;
            }
            if (!self.input[0].pckp && self.grasps[num11] != null && self.eatMeat > 60)
            {
                self.eatMeat = 0;
                self.wantToPickUp = 0;
                self.TossObject(num11, eu);
                self.ReleaseGrasp(num11);
                self.standing = true;
                return;
            }
            self.eatMeat = Custom.IntClamp(self.eatMeat - 1, 0, 50);
            self.maulTimer = Custom.IntClamp(self.maulTimer - 1, 0, 20);
            if (!ModManager.MMF || self.input[0].y == 0)
            {
                if (flag2 && self.eatCounter > 0)
                {
                    if (ModManager.MSC)
                    {
                        if (num5 <= -1 || self.grasps[num5] == null || !(self.grasps[num5].grabbed is GooieDuck) || (self.grasps[num5].grabbed as GooieDuck).bites != 6 || self.timeSinceSpawned % 2 == 0)
                        {
                            self.eatCounter--;
                        }
                        if (num5 > -1 && self.grasps[num5] != null && self.grasps[num5].grabbed is GooieDuck && (self.grasps[num5].grabbed as GooieDuck).bites == 6 && self.FoodInStomach < self.MaxFoodInStomach)
                        {
                            (self.graphicsModule as PlayerGraphics).BiteStruggle(num5);
                        }
                    }
                    else
                    {
                        self.eatCounter--;
                    }
                }
                else if (!flag2 && self.eatCounter < 40)
                {
                    self.eatCounter++;
                }
            }
            if (flag4 && self.input[0].y == 0)
            {
                self.reloadCounter++;
                if (self.reloadCounter > 40)
                {
                    (self.grasps[num2].grabbed as JokeRifle).ReloadRifle(self.grasps[num].grabbed);
                    BodyChunk mainBodyChunk = self.mainBodyChunk;
                    mainBodyChunk.vel.y = mainBodyChunk.vel.y + 4f;
                    self.room.PlaySound(SoundID.Gate_Clamp_Lock, self.mainBodyChunk, false, 0.5f, 3f + Random.value);
                    AbstractPhysicalObject abstractPhysicalObject = self.grasps[num].grabbed.abstractPhysicalObject;
                    self.ReleaseGrasp(num);
                    abstractPhysicalObject.realizedObject.RemoveFromRoom();
                    abstractPhysicalObject.Room.RemoveEntity(abstractPhysicalObject);
                    self.reloadCounter = 0;
                }
            }
            else
            {
                self.reloadCounter = 0;
            }
            if (ModManager.MMF && self.mainBodyChunk.submersion >= 0.5f)
            {
                flag3 = false;
            }
            if (flag3)
            {
                if (self.craftingObject)
                {
                    self.swallowAndRegurgitateCounter++;
                    if (self.swallowAndRegurgitateCounter > 105)
                    {
                        self.SpitUpCraftedObject();
                        self.swallowAndRegurgitateCounter = 0;
                    }
                }
                else if (!ModManager.MMF || self.input[0].y == 0)
                {
                    self.swallowAndRegurgitateCounter++;
                    if ((self.objectInStomach != null || self.isGourmand || (ModManager.MSC && self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear)) && self.swallowAndRegurgitateCounter > 110)
                    {
                        bool flag6 = false;
                        if (self.isGourmand && self.objectInStomach == null)
                        {
                            flag6 = true;
                        }
                        if (!flag6 || (flag6 && self.FoodInStomach >= 1))
                        {
                            if (flag6)
                            {
                                self.SubtractFood(1);
                            }
                            self.Regurgitate();
                        }
                        else
                        {
                            self.firstChunk.vel += new Vector2(Random.Range(-1f, 1f), 0f);
                            self.Stun(30);
                        }
                        if (self.spearOnBack != null)
                        {
                            self.spearOnBack.interactionLocked = true;
                        }
                        if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null)
                        {
                            self.slugOnBack.interactionLocked = true;
                        }
                        self.swallowAndRegurgitateCounter = 0;
                    }
                    else if (self.objectInStomach == null && self.swallowAndRegurgitateCounter > 90)
                    {
                        for (int num13 = 0; num13 < 2; num13++)
                        {
                            if (self.grasps[num13] != null && self.CanBeSwallowed(self.grasps[num13].grabbed))
                            {
                                self.bodyChunks[0].pos += Custom.DirVec(self.grasps[num13].grabbed.firstChunk.pos, self.bodyChunks[0].pos) * 2f;
                                self.SwallowObject(num13);
                                if (self.spearOnBack != null)
                                {
                                    self.spearOnBack.interactionLocked = true;
                                }
                                if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null)
                                {
                                    self.slugOnBack.interactionLocked = true;
                                }
                                self.swallowAndRegurgitateCounter = 0;
                                (self.graphicsModule as PlayerGraphics).swallowing = 20;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (self.swallowAndRegurgitateCounter > 0)
                    {
                        self.swallowAndRegurgitateCounter--;
                    }
                    if (self.eatCounter > 0)
                    {
                        self.eatCounter--;
                    }
                }
            }
            else
            {
                self.swallowAndRegurgitateCounter = 0;
            }
            for (int num14 = 0; num14 < self.grasps.Length; num14++)
            {
                if (self.grasps[num14] != null && self.grasps[num14].grabbed.slatedForDeletetion)
                {
                    self.ReleaseGrasp(num14);
                }
            }
            if (self.grasps[0] != null && self.Grabability(self.grasps[0].grabbed) == Player.ObjectGrabability.TwoHands)
            {
                self.pickUpCandidate = null;
            }
            else
            {
                PhysicalObject physicalObject = (self.dontGrabStuff < 1) ? self.PickupCandidate(20f) : null;
                if (self.pickUpCandidate != physicalObject && physicalObject != null && physicalObject is PlayerCarryableItem)
                {
                    (physicalObject as PlayerCarryableItem).Blink();
                }
                self.pickUpCandidate = physicalObject;
            }
            if (self.switchHandsCounter > 0)
            {
                self.switchHandsCounter--;
            }
            if (self.wantToPickUp > 0)
            {
                self.wantToPickUp--;
            }
            if (self.wantToThrow > 0)
            {
                self.wantToThrow--;
            }
            if (self.noPickUpOnRelease > 0)
            {
                self.noPickUpOnRelease--;
            }
            if (self.input[0].thrw && !self.input[1].thrw && (!ModManager.MSC || !self.monkAscension))
            {
                self.wantToThrow = 5;
            }
            if (self.wantToThrow > 0)
            {
                if (!(ModManager.MSC && MMF.cfgOldTongue.Value && self.grasps[0] == null && self.grasps[1] == null && self.SaintTongueCheck()))
                {
                    for (int num15 = 0; num15 < 2; num15++)
                    {
                        if (self.grasps[num15] != null && self.IsObjectThrowable(self.grasps[num15].grabbed))
                        {
                            self.ThrowObject(num15, eu);
                            self.wantToThrow = 0;
                            break;
                        }
                    }
                }
                if ((ModManager.MSC || ModManager.CoopAvailable) && self.wantToThrow > 0 && self.slugOnBack != null && self.slugOnBack.HasASlug)
                {
                    Player slugcat = self.slugOnBack.slugcat;
                    self.slugOnBack.SlugToHand(eu);
                    self.ThrowObject(0, eu);
                    float num16 = (self.ThrowDirection >= 0) ? Mathf.Max(self.bodyChunks[0].pos.x, self.bodyChunks[1].pos.x) : Mathf.Min(self.bodyChunks[0].pos.x, self.bodyChunks[1].pos.x);
                    for (int num17 = 0; num17 < slugcat.bodyChunks.Length; num17++)
                    {
                        slugcat.bodyChunks[num17].pos.y = self.firstChunk.pos.y + 20f;
                        if (self.ThrowDirection < 0)
                        {
                            if (slugcat.bodyChunks[num17].pos.x > num16 - 8f)
                            {
                                slugcat.bodyChunks[num17].pos.x = num16 - 8f;
                            }
                            if (slugcat.bodyChunks[num17].vel.x > 0f)
                            {
                                slugcat.bodyChunks[num17].vel.x = 0f;
                            }
                        }
                        else if (self.ThrowDirection > 0)
                        {
                            if (slugcat.bodyChunks[num17].pos.x < num16 + 8f)
                            {
                                slugcat.bodyChunks[num17].pos.x = num16 + 8f;
                            }
                            if (slugcat.bodyChunks[num17].vel.x < 0f)
                            {
                                slugcat.bodyChunks[num17].vel.x = 0f;
                            }
                        }
                    }
                }
            }
            if (self.wantToPickUp > 0)
            {
                bool flag7 = true;
                if (self.animation == Player.AnimationIndex.DeepSwim)
                {
                    if (self.grasps[0] == null && self.grasps[1] == null)
                    {
                        flag7 = false;
                    }
                    else
                    {
                        for (int num18 = 0; num18 < 10; num18++)
                        {
                            if (self.input[num18].y > -1 || self.input[num18].x != 0)
                            {
                                flag7 = false;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    for (int num19 = 0; num19 < 5; num19++)
                    {
                        if (self.input[num19].y > -1)
                        {
                            flag7 = false;
                            break;
                        }
                    }
                }
                if (ModManager.MSC)
                {
                    if (self.grasps[0] != null && self.grasps[0].grabbed is EnergyCell && self.mainBodyChunk.submersion > 0f)
                    {
                        flag7 = false;
                    }
                    else if (self.grasps[0] != null && self.grasps[0].grabbed is EnergyCell && self.canJump <= 0 && self.bodyMode != Player.BodyModeIndex.Crawl && self.bodyMode != Player.BodyModeIndex.CorridorClimb && self.bodyMode != Player.BodyModeIndex.ClimbIntoShortCut && self.animation != Player.AnimationIndex.HangFromBeam && self.animation != Player.AnimationIndex.ClimbOnBeam && self.animation != Player.AnimationIndex.AntlerClimb && self.animation != Player.AnimationIndex.VineGrab && self.animation != Player.AnimationIndex.ZeroGPoleGrab)
                    {
                        (self.grasps[0].grabbed as EnergyCell).Use(false);
                    }
                }
                if (!ModManager.MMF && self.grasps[0] != null && self.HeavyCarry(self.grasps[0].grabbed))
                {
                    flag7 = true;
                }
                if (flag7)
                {
                    int num20 = -1;
                    for (int num21 = 0; num21 < 2; num21++)
                    {
                        if (self.grasps[num21] != null)
                        {
                            num20 = num21;
                            break;
                        }
                    }
                    if (num20 > -1)
                    {
                        self.wantToPickUp = 0;
                        if (!ModManager.MSC || self.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Artificer || !(self.grasps[num20].grabbed is Scavenger))
                        {
                            self.pyroJumpDropLock = 0;
                        }
                        if (self.pyroJumpDropLock == 0 && (!ModManager.MSC || self.wantToJump == 0))
                        {
                            self.ReleaseObject(num20, eu);
                            return;
                        }
                    }
                    else
                    {
                        if (self.spearOnBack != null && self.spearOnBack.spear != null && self.mainBodyChunk.ContactPoint.y < 0)
                        {
                            self.room.socialEventRecognizer.CreaturePutItemOnGround(self.spearOnBack.spear, self);
                            self.spearOnBack.DropSpear();
                            return;
                        }
                        if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null && self.slugOnBack.slugcat != null && self.mainBodyChunk.ContactPoint.y < 0)
                        {
                            self.room.socialEventRecognizer.CreaturePutItemOnGround(self.slugOnBack.slugcat, self);
                            self.slugOnBack.DropSlug();
                            self.wantToPickUp = 0;
                            return;
                        }
                        if (ModManager.MSC && self.room != null && self.room.game.IsStorySession && self.room.game.GetStorySession.saveState.wearingCloak && self.AI == null)
                        {
                            self.room.game.GetStorySession.saveState.wearingCloak = false;
                            AbstractConsumable abstractConsumable = new AbstractConsumable(self.room.game.world, MoreSlugcatsEnums.AbstractObjectType.MoonCloak, null, self.room.GetWorldCoordinate(self.mainBodyChunk.pos), self.room.game.GetNewID(), -1, -1, null);
                            self.room.abstractRoom.AddEntity(abstractConsumable);
                            abstractConsumable.pos = self.abstractCreature.pos;
                            abstractConsumable.RealizeInRoom();
                            (abstractConsumable.realizedObject as MoonCloak).free = true;
                            for (int num22 = 0; num22 < abstractConsumable.realizedObject.bodyChunks.Length; num22++)
                            {
                                abstractConsumable.realizedObject.bodyChunks[num22].HardSetPosition(self.mainBodyChunk.pos);
                            }
                            self.dontGrabStuff = 15;
                            self.wantToPickUp = 0;
                            self.noPickUpOnRelease = 20;
                            return;
                        }
                    }
                }
                else if (self.pickUpCandidate != null)
                {
                    if (self.pickUpCandidate is Spear && self.CanPutSpearToBack && ((self.grasps[0] != null && self.Grabability(self.grasps[0].grabbed) >= Player.ObjectGrabability.BigOneHand) || (self.grasps[1] != null && self.Grabability(self.grasps[1].grabbed) >= Player.ObjectGrabability.BigOneHand) || (self.grasps[0] != null && self.grasps[1] != null)))
                    {
                        Debug.Log("spear straight to back");
                        self.room.PlaySound(SoundID.Slugcat_Switch_Hands_Init, self.mainBodyChunk);
                        self.spearOnBack.SpearToBack(self.pickUpCandidate as Spear);
                    }
                    else if (self.CanPutSlugToBack && self.pickUpCandidate is Player && (!(self.pickUpCandidate as Player).dead || self.CanIPutDeadSlugOnBack(self.pickUpCandidate as Player)) && ((self.grasps[0] != null && (self.Grabability(self.grasps[0].grabbed) > Player.ObjectGrabability.BigOneHand || self.grasps[0].grabbed is Player)) || (self.grasps[1] != null && (self.Grabability(self.grasps[1].grabbed) > Player.ObjectGrabability.BigOneHand || self.grasps[1].grabbed is Player)) || (self.grasps[0] != null && self.grasps[1] != null) || self.bodyMode == Player.BodyModeIndex.Crawl))
                    {
                        Debug.Log("slugpup/player straight to back");
                        self.room.PlaySound(SoundID.Slugcat_Switch_Hands_Init, self.mainBodyChunk);
                        self.slugOnBack.SlugToBack(self.pickUpCandidate as Player);
                    }
                    else
                    {
                        int num23 = 0;
                        for (int num24 = 0; num24 < 2; num24++)
                        {
                            if (self.grasps[num24] == null)
                            {
                                num23++;
                            }
                        }
                        if (self.Grabability(self.pickUpCandidate) == Player.ObjectGrabability.TwoHands && num23 < 4)
                        {
                            for (int num25 = 0; num25 < 2; num25++)
                            {
                                if (self.grasps[num25] != null)
                                {
                                    self.ReleaseGrasp(num25);
                                }
                            }
                        }
                        else if (num23 == 0)
                        {
                            for (int num26 = 0; num26 < 2; num26++)
                            {
                                if (self.grasps[num26] != null && self.grasps[num26].grabbed is Fly)
                                {
                                    self.ReleaseGrasp(num26);
                                    break;
                                }
                            }
                        }
                        int num27 = 0;
                        while (num27 < 2)
                        {
                            if (self.grasps[num27] == null)
                            {
                                if (self.pickUpCandidate is Creature)
                                {
                                    self.room.PlaySound(SoundID.Slugcat_Pick_Up_Creature, self.pickUpCandidate.firstChunk, false, 1f, 1f);
                                }
                                else if (self.pickUpCandidate is PlayerCarryableItem)
                                {
                                    for (int num28 = 0; num28 < self.pickUpCandidate.grabbedBy.Count; num28++)
                                    {
                                        if (self.pickUpCandidate.grabbedBy[num28].grabber.room == self.pickUpCandidate.grabbedBy[num28].grabbed.room)
                                        {
                                            self.pickUpCandidate.grabbedBy[num28].grabber.GrabbedObjectSnatched(self.pickUpCandidate.grabbedBy[num28].grabbed, self);
                                        }
                                        else
                                        {
                                            string str = "Item theft room mismatch? ";
                                            AbstractPhysicalObject abstractPhysicalObject2 = self.pickUpCandidate.grabbedBy[num28].grabbed.abstractPhysicalObject;
                                            Debug.Log(str + ((abstractPhysicalObject2 != null) ? abstractPhysicalObject2.ToString() : null));
                                        }
                                        self.pickUpCandidate.grabbedBy[num28].grabber.ReleaseGrasp(self.pickUpCandidate.grabbedBy[num28].graspUsed);
                                    }
                                    (self.pickUpCandidate as PlayerCarryableItem).PickedUp(self);
                                }
                                else
                                {
                                    self.room.PlaySound(SoundID.Slugcat_Pick_Up_Misc_Inanimate, self.pickUpCandidate.firstChunk, false, 1f, 1f);
                                }
                                self.SlugcatGrab(self.pickUpCandidate, num27);
                                if (self.pickUpCandidate.graphicsModule != null && self.Grabability(self.pickUpCandidate) < (Player.ObjectGrabability)5)
                                {
                                    self.pickUpCandidate.graphicsModule.BringSpritesToFront();
                                    break;
                                }
                                break;
                            }
                            else
                            {
                                num27++;
                            }
                        }
                    }
                    self.wantToPickUp = 0;
                }
            }
        }
        else { orig(self, eu); }
    }

}














