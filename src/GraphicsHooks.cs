using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PebblesSlug;



public static class GraphicsHooks
{
    public static void OnModInit()
    {
        On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
        On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
        // On.GraphicsHooks.allGraphics = new ConditionalWeakTable<PlayerGraphics, PebblesSlugGraphics>();
    }

    public static ConditionalWeakTable<PlayerGraphics, PebblesSlugGraphics> allGraphics;






    private static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        orig(self, sLeaser, rCam, newContatiner);
    }


    private static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);
    }


    private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        // 没看懂，下次一定
        if (Plugin.IsMyCat.TryGet(self.player, out bool is_my_cat) && is_my_cat)
        {
            for (int i = 0; i < 10; i++)
            {
                
                if (sLeaser.sprites[i].element.name.StartsWith("SunHead"))
                {
                    sLeaser.sprites[i].color = new Color(0.92342f, 0.87058f, 0.29019f);
                }
                else if (sLeaser.sprites[i].element.name.StartsWith("similar-"))
                {
                    sLeaser.sprites[i].element = Futile.atlasManager.GetElementWithName("similar-" + sLeaser.sprites[i].element.name);
                }
                    
                
            }
        }

        
    }


    private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig(self, sLeaser, rCam);
        



        
    }












}
