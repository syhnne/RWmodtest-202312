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
    public static Configurable<bool> GravityControlOutside;
    public Configurable<KeyCode> GravityControlKey;

    UIelement[] settings;

    public PebblesSlugOption()
    {
        ExplosionCapacity = config.Bind<int>("ExplosionCapacity", 10);
        AddFoodOnShock = config.Bind<bool>("AddFoodOnShock", false);
        GravityControlKey = config.Bind<KeyCode>("GravityControlKey", KeyCode.G);
        // 底下这个还没绑定。以后可能不会这么写
        GravityControlOutside = config.Bind<bool>("GravityControlOutside", false);
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
            new OpTab(this, "Options")
        };

        string desc = "Explosion capacity";
        Tabs[0].AddItems(
            new OpLabel(xposLabel, ymax - yspacing, inGameTranslator.Translate("Explosion capacity"))
            { description = desc },
            new OpSlider(ExplosionCapacity, new Vector2(xposOpt, ymax - yspacing), 360, false)
            {
                min = 5,
                max = 20,
                defaultValue = DefaultExplosionCapacity.ToString(),
                description = desc
            }
        );

        desc = "Add food when electrocuted by centipedes and zapcoils";
        Tabs[0].AddItems(
            new OpLabel(xposLabel, ymax - yspacing - 50f, inGameTranslator.Translate("Add food on electric shock"))
            { description = desc },
            new OpCheckBox(AddFoodOnShock, xposOpt, ymax - yspacing - 50f)
            { description = desc }
        );

        desc = "(WIP)The key to be pressed when controlling gravity";
        Tabs[0].AddItems(
            new OpLabel(xposLabel, ymax - yspacing - 100f, inGameTranslator.Translate("Gravity control key"), false)
            { description = desc },
            new OpKeyBinder(GravityControlKey, new Vector2(xposOpt, ymax - yspacing - 100f), new Vector2(50f, 10f), true, OpKeyBinder.BindController.AnyController)
            { description = desc }
        );

        desc = "(WIP)Enable this to control gravity in any room";
        Tabs[0].AddItems(
            new OpLabel(xposLabel, ymax - yspacing - 150f, inGameTranslator.Translate("Gravity control for all rooms"), false)
            { description = desc },
            new OpCheckBox(GravityControlOutside, xposOpt, ymax - yspacing - 150f)
            { description = desc }
        );



    }

}
