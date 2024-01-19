using SlugBase.SaveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;




namespace PebblesSlug;
// 有没有人行行好，讲一讲怎么保存游戏数据啊
// 我花了一整天尝试看懂雨世界的存档系统，最后没有看懂，我只能抄珍珠猫代码了orz
public class PebblesSlugEnums
{

    public static SlugcatStats.Name PebblesSlug;
    public static void RegisterValues()
    {
        PebblesSlug = new SlugcatStats.Name("PebblesSlug", true);
    }

    public static void UnregisterValues()
    {
        if (PebblesSlug != null) { PebblesSlug.Unregister(); PebblesSlug = null; }
    }
}