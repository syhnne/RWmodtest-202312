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

namespace PebblesSlug
{
    public class ExplosionEffect: UpdatableAndDeletable, IDrawable
    {
        public ExplosionEffect() 
        {

        }


        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            /*
                Vector2 vector = Vector2.Lerp(base.firstChunk.lastPos, base.firstChunk.pos, timeStacker);
                if (this.vibrate > 0)
                {
                    vector += Custom.DegToVec(Random.value * 360f) * 2f * Random.value;
                }
                sLeaser.sprites[0].x = vector.x - camPos.x;
                sLeaser.sprites[0].y = vector.y - camPos.y;
                if (this.burning == 0f)
                {
                    sLeaser.sprites[2].shader = rCam.room.game.rainWorld.Shaders["FlatLightBehindTerrain"];
                    sLeaser.sprites[2].x = vector.x - camPos.x;
                    sLeaser.sprites[2].y = vector.y - camPos.y;
                }
                else
                {
                    sLeaser.sprites[2].shader = rCam.room.game.rainWorld.Shaders["FlareBomb"];
                    sLeaser.sprites[2].x = vector.x - camPos.x + Mathf.Lerp(this.lastFlickerDir.x, this.flickerDir.x, timeStacker);
                    sLeaser.sprites[2].y = vector.y - camPos.y + Mathf.Lerp(this.lastFlickerDir.y, this.flickerDir.y, timeStacker);
                    sLeaser.sprites[2].scale = Mathf.Lerp(this.lastFlashRad, this.flashRad, timeStacker) / 16f;
                    sLeaser.sprites[2].alpha = Mathf.Lerp(this.lastFlashAlpha, this.flashAplha, timeStacker);
                }
                if (base.mode == Weapon.Mode.Thrown)
                {
                    sLeaser.sprites[1].isVisible = true;
                    Vector2 vector2 = Vector2.Lerp(this.tailPos, base.firstChunk.lastPos, timeStacker);
                    Vector2 vector3 = Custom.PerpendicularVector((vector - vector2).normalized);
                    (sLeaser.sprites[1] as TriangleMesh).MoveVertice(0, vector + vector3 * 3f - camPos);
                    (sLeaser.sprites[1] as TriangleMesh).MoveVertice(1, vector - vector3 * 3f - camPos);
                    (sLeaser.sprites[1] as TriangleMesh).MoveVertice(2, vector2 - camPos);
                    (sLeaser.sprites[1] as TriangleMesh).verticeColors[2] = this.color;
                }
                else
                {
                    sLeaser.sprites[1].isVisible = false;
                }
                if (base.slatedForDeletetion || this.room != rCam.room)
                {
                    sLeaser.CleanSpritesAndRemove();
                }
            */
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[3];
            sLeaser.sprites[0] = new FSprite("Pebble5", true);
            TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
            {
            new TriangleMesh.Triangle(0, 1, 2)
            };
            TriangleMesh triangleMesh = new TriangleMesh("Futile_White", tris, true, false);
            sLeaser.sprites[1] = triangleMesh;
            sLeaser.sprites[2] = new FSprite("Futile_White", true);
            sLeaser.sprites[2].scale = 2.5f;
            this.AddToContainer(sLeaser, rCam, null);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            sLeaser.sprites[0].color = new Color(1f, 1f, 1f);
            sLeaser.sprites[2].color = new Color(0.7f, 1f, 1f);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
        {
            if (newContainer == null)
            {
                newContainer = rCam.ReturnFContainer("Items");
            }
            newContainer.AddChild(sLeaser.sprites[1]);
            newContainer.AddChild(sLeaser.sprites[0]);
            rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[2]);
        }
    }
}
