using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using Random = UnityEngine.Random;
using Color = UnityEngine.Color;
using System.Xml;
using System.Xml.Linq;
using SlugBase.DataTypes;
using System.Runtime.CompilerServices;


namespace PebblesSlug;



// 参考钢蛞蝓代码
// 我还是那个只会复制粘贴。。
public class PebblesSlugGraphics
{
    public PebblesSlugGraphics(PlayerGraphics owner)
    {
        this.ownerRef = new WeakReference<PlayerGraphics>(owner);

    }

    private WeakReference<PlayerGraphics> ownerRef;
    private static PlayerColor EyeColor = new("Eyes");
    private static PlayerColor BodyColor = new("Body");


    



    public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {

    }



    public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        // sLeaser.sprites[this.startLength].color = this.color;
    }




    public void DrawSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {

    }



    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        PlayerGraphics playerGraphics = null;
        if (ownerRef.TryGetTarget(out playerGraphics))
        {
            startLength = sLeaser.sprites.Length;
            Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + scarfLength);

            for (int i = 0; i < scarfLength; i++)
            {
                sLeaser.sprites[Scarf(i)] = new FSprite("Scarf", true);
            }
            
            AddToContainer(sLeaser, rCam, null);

            /*
            Array.Resize<FSprite>(ref sLeaser.sprites, sLeaser.sprites.Length + this.eyeLength + this.beakLength + this.feathersLength + this.tailFeathersLength);
            sLeaser.sprites[this.startLength] = new FSprite("Circle20", true);
            sLeaser.sprites[this.startLength].scale = 0.15f;
            sLeaser.sprites[this.startLength].color = this.color;
            for (int i = 0; i < this.beakLength; i++)
            {
                sLeaser.sprites[this.Beak(i)] = new FSprite("MirosSlug_Beak", true);
                sLeaser.sprites[this.Beak(i)].anchorX = 0f;
                sLeaser.sprites[this.Beak(i)].scale = 0.5f;
                sLeaser.sprites[this.Beak(i)].scaleY *= (float)((i == 0) ? 1 : -1);
            }
            this.PlayerGraphics_AddToContainer(sLeaser, rCam, null);
            */

            // PlayerModule playerModule;
            // int num = Plugin.modules.TryGetValue(self.player, out playerModule) ? playerModule.Index : 0;

            // playerModule.Index = sLeaser.sprites.Length;

        }
    }

    



    








        private int Scarf(int i)
    {
        return startLength + i + 1;
    }



    private int startLength = 0;

    private int scarfLength = 5;
}



    /*
     * 完全没看懂这是用来干嘛的，但大家都写了，那我也写吧
    private static void Player_AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        orig.Invoke(self, sLeaser, rCam, newContatiner);

        if (!AddNewImagine.AreYourCat(self))
        {
            PlayerModule playerModule;
            int num = Plugin.modules.TryGetValue(self.player, out playerModule) ? playerModule.Index : 0;

            if (!(playerModule.Index > 0 && sLeaser.sprites.Length > num))
            {
                FContainer fcontainer = newContatiner ?? rCam.ReturnFContainer("Midground");
                for (int i = playerModule.Index; i <= playerModule.Index + 5; i++)
                {
                    fcontainer.AddChild(sLeaser.sprites[i]);
                    sLeaser.sprites[i].MoveBehindOtherNode(sLeaser.sprites[2]);
                }
            }
        }
    }
    */




