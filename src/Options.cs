using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;

namespace PebblesSlug;

public class PebblesSlugOption : OptionInterface
{

    internal readonly int DefaultExplosionCapacity = 10;
    public static Configurable<int> ExplosionCapacity;
    public static Configurable<bool> AddFoodOnShock;
    /*public static Configurable<bool> GravityControlOutside;*/
    public Configurable<KeyCode> GravityControlKey;
    public Configurable<KeyCode> GravityControlToggle;
    public Configurable<KeyCode> CraftKey;
    public Configurable<KeyCode> fpConsoleKey;


    public PebblesSlugOption()
    {
        ExplosionCapacity = config.Bind<int>("ExplosionCapacity", 10);
        AddFoodOnShock = config.Bind<bool>("AddFoodOnShock", false);
        GravityControlKey = config.Bind<KeyCode>("GravityControlKey", KeyCode.G);
        GravityControlToggle = config.Bind<KeyCode>("GravityControlToggle", KeyCode.F);
        /*// 这个还是拿剧情卡一下吧。回头再改，等我玩腻了再说（
        GravityControlOutside = config.Bind<bool>("GravityControlOutside", false);*/
        CraftKey = config.Bind<KeyCode>("CraftKey", KeyCode.None);
        fpConsoleKey = config.Bind<KeyCode>("fpConsoleKey", KeyCode.Tab);
    }



    // 这次抄的是：https://github.com/SlimeCubed/DevConsole/blob/master/DevConsole/Config/ConsoleConfig.cs
    public override void Initialize()
    {
        base.Initialize();
        InGameTranslator inGameTranslator = Custom.rainWorld.inGameTranslator;
        float yspacing = 50f;
        float xposLabel = 20f;
        float xposOpt = 200f;
        float xmax = 600f;
        float ymax = 600f;
        
        Tabs = new OpTab[]
        {
            new OpTab(this, Plugin.strings[13]),
            new OpTab(this, Plugin.strings[14])
        };


        string desc = Plugin.strings[2];
        Tabs[0].AddItems(
            new OpLabel(xposLabel, ymax - yspacing, inGameTranslator.Translate(Plugin.strings[1]))
            { description = inGameTranslator.Translate(desc) },
            new OpSlider(ExplosionCapacity, new Vector2(xposOpt, ymax - yspacing), 360, false)
            {
                min = 5,
                max = 20,
                defaultValue = DefaultExplosionCapacity.ToString(),
                description = inGameTranslator.Translate(desc)
            }
        );

        desc = Plugin.strings[4];
        Tabs[0].AddItems(
            new OpLabel(xposLabel, ymax - yspacing - 50f, inGameTranslator.Translate(Plugin.strings[3]))
            { description = inGameTranslator.Translate(desc) },
            new OpCheckBox(AddFoodOnShock, xposOpt, ymax - yspacing - 50f)
            { description = inGameTranslator.Translate(desc) }
        );


        desc = Plugin.strings[6];
        Tabs[0].AddItems(
            new OpLabel(xposLabel, ymax - yspacing - 100f, inGameTranslator.Translate(Plugin.strings[5]), false)
            { description = inGameTranslator.Translate(desc) },
            new OpKeyBinder(CraftKey, new Vector2(xposOpt, ymax - yspacing - 100f), new Vector2(50f, 10f), true, OpKeyBinder.BindController.AnyController)
            { description = inGameTranslator.Translate(desc) }
        );



        // 我怎么你了？怎么改不了啊？
        desc = Plugin.strings[8];
        Tabs[0].AddItems(
            new OpLabel(xposLabel, ymax - yspacing - 150f, inGameTranslator.Translate(Plugin.strings[7]), false)
            { description = inGameTranslator.Translate(desc) }/*,
            new OpKeyBinder(fpConsoleKey, new Vector2(xposOpt, ymax - yspacing - 150f), new Vector2(50f, 10f), true, OpKeyBinder.BindController.AnyController)
            { description = desc }*/
        );





        desc = Plugin.strings[10];
        Tabs[1].AddItems(
            new OpLabel(xposLabel, ymax - yspacing, inGameTranslator.Translate(Plugin.strings[9]), false)
            { description = inGameTranslator.Translate(desc) },
            new OpKeyBinder(GravityControlKey, new Vector2(xposOpt, ymax - yspacing), new Vector2(50f, 10f), true, OpKeyBinder.BindController.AnyController)
            { description = inGameTranslator.Translate(desc) }
        );

        desc = Plugin.strings[12];
        Tabs[1].AddItems(
            new OpLabel(xposLabel, ymax - yspacing - 50f, inGameTranslator.Translate(Plugin.strings[11]), false)
            { description = inGameTranslator.Translate(desc) },
            new OpKeyBinder(GravityControlToggle, new Vector2(xposOpt, ymax - yspacing - 50f), new Vector2(50f, 10f), true, OpKeyBinder.BindController.AnyController)
            { description = inGameTranslator.Translate(desc) }
        );

    }

}
