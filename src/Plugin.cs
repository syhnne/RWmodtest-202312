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






namespace PebblesSlug;








[BepInPlugin(MOD_ID, "PebblesSlug", "0.1.0")]
class Plugin : BaseUnityPlugin
{
    private const string MOD_ID = "PebblesSlug_by_syhnne";



    // 不要给猫改名！！！不要给猫改名！！！不要给猫改名！！！

    private static readonly Color32 bodyColor_hard = new Color32(254, 104, 202, 255);
    private static readonly Color eyesColor_hard = new Color(1f, 1f, 1f);

    public static readonly PlayerFeature<Color> BodyColor = PlayerColor("Body");
    public static readonly PlayerFeature<Color> EyesColor = PlayerColor("Eyes");

    public static new ManualLogSource Logger { get; private set; }

    public PebblesSlugOption option;
    private bool IsInit;
    private static List<int> ColoredBodyParts = new List<int>() { 2, 3, 5, 6, 7, 8, 9, };

    /*
     * 0: "BodyA"
     * 1: "HipsA"
     * 2: tail
     * 3: "HeadA0"
     * 4: "LegsA0"
     * 5: "PlayerArm0", sLeaser.sprites[5].scaleY = -1f;
     * 6: "PlayerArm0"
     * 7: "OnTopOfTerrainHand"
     * 8: "OnTopOfTerrainHand", sLeaser.sprites[8].scaleX = -1f;
     * 9: "FaceA0"
     * 10: "Futile_White", sLeaser.sprites[10].shader = rCam.game.rainWorld.Shaders["FlatLight"];
     * 11: "pixel"
     */



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
            On.Player.SpitUpCraftedObject += Player_SpitUpCraftedObject_old;
            // IL.Player.SpitUpCraftedObject += Player_SpitUpCraftedObject;
            IL.Player.GrabUpdate += Player_GrabUpdate;
            On.Creature.Violence += Creature_Violence;
            On.Player.CanBeSwallowed += Player_CanBeSwallowed;

            // On.Room.PlaySound_SoundID_Vector2 += Room_PlaySound_SoundID_Vector2;
            // On.UnderwaterShock.Update += UnderwaterShock_Update;



            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;


        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            base.Logger.LogError(ex);
        }

    }




    // 因为根本不会C#所以把图形和技能全写一起了
    private void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig(self, sLeaser, rCam);
        if (self.player.slugcatStats.name.value == "PebblesSlug")
        {
            FAtlas fatlas = Futile.atlasManager.LoadAtlas("atlases/fp_tail");
            TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
            {
                new TriangleMesh.Triangle(0, 1, 2),
                new TriangleMesh.Triangle(1, 2, 3),
                new TriangleMesh.Triangle(2, 3, 4),
                new TriangleMesh.Triangle(3, 4, 5),
                new TriangleMesh.Triangle(4, 5, 6),
                new TriangleMesh.Triangle(5, 6, 7),
                new TriangleMesh.Triangle(6, 7, 8),
                new TriangleMesh.Triangle(7, 8, 9),
                new TriangleMesh.Triangle(8, 9, 10),
                new TriangleMesh.Triangle(9, 10, 11),
                new TriangleMesh.Triangle(10, 11, 12),
                new TriangleMesh.Triangle(11, 12, 13),
                new TriangleMesh.Triangle(12, 13, 14),
            };
            TriangleMesh triangleMesh = new TriangleMesh("fp_tail", tris, false, false);
            triangleMesh.UVvertices[0] = fatlas._elementsByName["fp_tail"].uvBottomLeft;
            triangleMesh.UVvertices[1] = fatlas._elementsByName["fp_tail"].uvTopLeft;
            triangleMesh.UVvertices[13] = fatlas._elementsByName["fp_tail"].uvTopRight;
            triangleMesh.UVvertices[14] = fatlas._elementsByName["fp_tail"].uvBottomRight;
            float num = (triangleMesh.UVvertices[13].x - triangleMesh.UVvertices[1].x) / 6f;
            for (int i = 2; i < 14; i += 2)
            {
                triangleMesh.UVvertices[i].x = (float)((double)fatlas._elementsByName["fp_tail"].uvBottomLeft.x + (double)num * 0.5 * (double)i);
                triangleMesh.UVvertices[i].y = fatlas._elementsByName["fp_tail"].uvBottomLeft.y;
            }
            for (int j = 3; j < 13; j += 2)
            {
                triangleMesh.UVvertices[j].x = (float)((double)fatlas._elementsByName["fp_tail"].uvTopLeft.x + (double)num * 0.5 * (double)(j - 1));
                triangleMesh.UVvertices[j].y = fatlas._elementsByName["fp_tail"].uvTopLeft.y;
            }
            sLeaser.sprites[2] = triangleMesh;

            self.AddToContainer(sLeaser, rCam, null);
        }
    }

    private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (self.player.slugcatStats.name.value == "PebblesSlug")
        {
            // 我知道了，问题大概在这里，我只给尾巴重新改了材质，剩下的材质都是加载了但是没用上。
            // 理论上这个代码能简化一下，但我要先让它跑起来，剩下的我不敢动
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                if (i == 2)
                {
                    sLeaser.sprites[2].element = Futile.atlasManager.GetElementWithName("fp_tail");
                }
                else
                {
                    if (sLeaser.sprites[i].element.name.StartsWith(sLeaser.sprites[i].element.name))
                    {
                        if (Futile.atlasManager.DoesContainElementWithName("fp_" + sLeaser.sprites[i].element.name))
                        {
                            sLeaser.sprites[i].element = Futile.atlasManager.GetElementWithName(sLeaser.sprites[i].element.name.Replace(sLeaser.sprites[i].element.name, "fp_" + sLeaser.sprites[i].element.name));
                        }

                    }
                }


            }
        }
    }

    private void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);
        if (self.player.slugcatStats.name.value == "PebblesSlug")
        {
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                // 2是尾巴，9是眼睛，56是手，除此以外都涂成粉色。。
                // 这属于硬编码禁止玩家改颜色了，虽然我完全可以把所有颜色都换成贴图来解决这个问题，但我不想（
                if (ColoredBodyParts.Contains(i))
                {
                    sLeaser.sprites[i].color = eyesColor_hard;
                }
                else
                {
                    sLeaser.sprites[i].color = bodyColor_hard;
                }
            }
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
                Futile.atlasManager.LoadAtlas("atlases/fp_head");
                Futile.atlasManager.LoadAtlas("atlases/fp_tail");
                Futile.atlasManager.LoadAtlas("atlases/fp_arm");
            }
        }
        catch (Exception ex)
        {
            base.Logger.LogError(ex);
            throw;
        }
    }







    // 不能吃神经元。我想这个应该用不着调用原版方法了吧。。
    private bool Player_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
    {
        if (self.slugcatStats.name.value == "PebblesSlug")
        {
            return (!ModManager.MSC || !(self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear)) && (testObj is Rock || testObj is DataPearl || testObj is FlareBomb || testObj is Lantern || testObj is FirecrackerPlant || (testObj is VultureGrub && !(testObj as VultureGrub).dead) || (testObj is Hazer && !(testObj as Hazer).dead && !(testObj as Hazer).hasSprayed) || testObj is FlyLure || testObj is ScavengerBomb || testObj is PuffBall || testObj is SporePlant || testObj is BubbleGrass || testObj is OracleSwarmer || testObj is NSHSwarmer || testObj is OverseerCarcass || (ModManager.MSC && testObj is FireEgg && self.FoodInStomach >= self.MaxFoodInStomach) || (ModManager.MSC && testObj is SingularityBomb && !(testObj as SingularityBomb).activateSingularity && !(testObj as SingularityBomb).activateSucktion));
        }
        else { return orig(self, testObj); }
    }




    // 不能免疫蜈蚣的电击，但我认为这不是我的问题，是蜈蚣的问题。
    // 算了吧，要是连这都免疫，那就太超模了（
    private void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        if (self is Player && (self as Player).slugcatStats.name.value == "PebblesSlug")
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








    // 除了特效以外，数值跟炸猫差不多，因为我不知道那堆二段跳的数值怎么改。我想改得小一点，让他没有那么强的机动性，不然太超模了（
    // 因为这个电击在水下是有伤害的（痛击你的队友。jpg）我不是故意的，我是真的写不出来那个判定。我不知道他为什么会闪退。。
    // 我大概应该用原版方法，然后做ilhooking。啊？什么？我用了？
    private void Player_ClassMechanicsArtificer(On.Player.orig_ClassMechanicsArtificer orig, Player self)
    {
        if (self.slugcatStats.name.value == "PebblesSlug")
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
                // self.room.AddObject(new ExplosionEffect());
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
        if (self.slugcatStats.name.value == "PebblesSlug")
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
        else { return orig(self); }
    }


    private bool Player_GraspsCanBeCrafted(On.Player.orig_GraspsCanBeCrafted orig, Player self)
    {

        if (self.slugcatStats.name.value == "PebblesSlug" && (self.CraftingResults() != null))
        {
            return true;
        }
        return orig(self);
    }




    // 下面这个暂时没用，但先留着以防我日后突然想给他加点别的能力
    private void Player_SwallowObject(On.Player.orig_SwallowObject orig, Player self, int grasp)
    {
        if (self.slugcatStats.name.value == "PebblesSlug")
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




    private void Player_SpitUpCraftedObject(ILContext il)
    {
        ILCursor c = new(il);
        // 37 劫持炸矛判定，在此判定电矛。所以有没有人告诉我match到底该怎么写，我不想写这么大一坨，很累的
        if (c.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Call),
            (i) => i.Match(OpCodes.Brfalse),
            (i) => i.MatchLdloc(2),
            (i) => i.MatchIsinst("AbstractSpear"),
            (i) => i.Match(OpCodes.Ldfld)
            ))
        {
            Debug.Log("====++ Match successfully! - Player_SpitUpCraftedObject");
        }
    }


    private void Player_SpitUpCraftedObject_old(On.Player.orig_SpitUpCraftedObject orig, Player self)
    {
        if (self.slugcatStats.name.value == "PebblesSlug")
        {
            // 表示玩家在房间中位置的Vector2（二维向量）实例
            var vector = self.mainBodyChunk.pos;

            // 我要写一个craftingtutorial，并且单独绑一个变量，因为它内容跟原来的不一样
            self.room.PlaySound(SoundID.Slugcat_Swallow_Item, self.mainBodyChunk);

            for (int i = 0; i < self.grasps.Length; i++)
            {
                if (self.grasps[i] != null)
                {
                    AbstractPhysicalObject abstractPhysicalObject = self.grasps[i].grabbed.abstractPhysicalObject;
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
        }
        else { orig(self); }
    }








    // 啊！！终于把原来那一坨复制粘贴删了！！感觉像新年的第一天穿上新内裤一样清爽啊！！
    private void Player_GrabUpdate(ILContext il)
    {
        // 我试的那个判定不行，所以这咋判定？
        // 好吧，只能这样了。


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
            Debug.Log("====++ match successfully: neuron fly");
            // 啊哈！我是天才！
            // 怎么解决brfalse无法定位的问题：不要用自己的brfalse，直接绑架一个他原本的判断，然后把那个判断的输出结果和我加的判断绑在一起。反正他们是and关系
            // 这样一来，比原本直接改player_grabupdate的方法更加方便，因为另一个地方我就不用改了。
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 13);
            c.EmitDelegate<Func<bool, Player, int, bool>>((edible, self, grasp) =>
            {
                if (self.slugcatStats.name.value == "PebblesSlug")
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
        // 这里可能有问题，我试过联机队友是求生者的话没bug，但要是饕餮或者工匠，我不好说
        ILCursor c2 = new ILCursor(il);
        if (c2.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Brfalse_S),
            (i) => i.MatchLdarg(0),
            (i) => i.Match(OpCodes.Call),
            (i) => i.Match(OpCodes.Ldc_I4_M1)
            ))
        {
            Debug.Log("====++ CMON WHY ISNT IT WORKING");
            c2.EmitDelegate<Func<bool, bool>>((isArtificer) => 
            { 
                return true; 
            });
        }
    }
}











