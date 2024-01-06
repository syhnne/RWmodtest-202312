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
    public PebblesSlugOption()
    {
        // KarmaUpdate = this.config.Bind<bool>("ConsumingKarmaFlower", true, ConfigurableInfo.Empty);
        ExplosionCapacity = this.config.Bind<int>("ExplosionCapacity", 10, ConfigurableInfo.Empty);
    }

    public override void Initialize()
    {
        InGameTranslator inGameTranslator = Custom.rainWorld.inGameTranslator;
        base.Initialize();
        OpTab opTab = new OpTab(this, "Options");
        this.Tabs = new OpTab[]
        {
                opTab
        };
        this._ExplosionCapacity = new OpSlider(PebblesSlugOption.ExplosionCapacity, new Vector2(220f, 320f), 300, false)
        {
            min = 5,
            max = 20,
            defaultValue = this.DefaultExplosionCapacity.ToString()
        };
        opTab.AddItems(new UIelement[]
            {
                new OpLabel(new Vector2(10f, 420f), new Vector2(200f, 24f), inGameTranslator.Translate("Explosion Capacity"), FLabelAlignment.Left, false, null),
                this._ExplosionCapacity
            });
    }
    private readonly int DefaultExplosionCapacity = 10;
    private OpSlider _ExplosionCapacity;
    public static Configurable<int> ExplosionCapacity;

}
