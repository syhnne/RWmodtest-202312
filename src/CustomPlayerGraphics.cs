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
using static MonoMod.InlineRT.MonoModRule;
using System.Runtime.CompilerServices;
using IL.Menu;
using HUD;
using MonoMod.Utils;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Globalization;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace PebblesSlug;



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




internal class CustomPlayerGraphics
{

    private static readonly Color32 bodyColor_hard = new Color32(254, 104, 202, 255);
    private static readonly Color eyesColor_hard = new Color(1f, 1f, 1f);
    private static readonly List<int> ColoredBodyParts = new List<int>() { 2, 3, 5, 6, 7, 8, 9, };


    public static void Disable()
    {
        On.PlayerGraphics.InitiateSprites -= PlayerGraphics_InitiateSprites;
        On.PlayerGraphics.DrawSprites -= PlayerGraphics_DrawSprites;
        On.PlayerGraphics.ApplyPalette -= PlayerGraphics_ApplyPalette;
    }


    public static void Apply()
    {
        On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
    }








    private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig(self, sLeaser, rCam);
        if (self.player.slugcatStats.name == Plugin.SlugcatStatsName)
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




    private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (self.player.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            // 理论上这个代码能简化一下，但我要先让它跑起来，剩下的我不敢动
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                if (i == 2)
                {
                    sLeaser.sprites[2].element = Futile.atlasManager.GetElementWithName("fp_tail");
                }
                else
                {
                    if (Futile.atlasManager.DoesContainElementWithName("fp_" + sLeaser.sprites[i].element.name))
                    {
                        Plugin.Log("element:          ", sLeaser.sprites[i].element.name);
                        sLeaser.sprites[i].element = Futile.atlasManager.GetElementWithName(sLeaser.sprites[i].element.name.Replace(sLeaser.sprites[i].element.name, "fp_" + sLeaser.sprites[i].element.name));
                    }
                }


            }
        }
    }



    private static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);
        if (self.player.slugcatStats.name == Plugin.SlugcatStatsName)
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




}
