using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine.Diagnostics;

namespace PebblesSlug;

public class CustomDeathPersistentSaveData
{
    
    public string TotalHeader = Plugin.SlugcatName;
    public SlugcatStats.Name saveStateNumber;

    // 这样调用起来会方便点吗（
    public int CyclesFromLastEnterSSAI = 0;
    public bool TestData = false;
    public List<string> saveStrings = new List<string>()
    {
        "CyclesFromLastEnterSSAI",
        // "TestData"
    };


    public CustomDeathPersistentSaveData(SlugcatStats.Name saveStateNumber)
    {
        this.saveStateNumber = saveStateNumber;
    }


    public void ClearData(SlugcatStats.Name newName)
    {
        saveStateNumber = newName;
        CyclesFromLastEnterSSAI = 0;
        TestData = false;
    }



    // TODO: 这个字符串会越堆越多，以后改改
    // 噢 是unrecognized那儿没删掉导致的
    public string SaveToString(string res)
    {
        Plugin.LogStat("DPSaveData - SaveToString", CyclesFromLastEnterSSAI);
        res += TotalHeader + saveStrings[0] + "<dpB>" + CyclesFromLastEnterSSAI.ToString() + "<dpA>";
        // res += TotalHeader + saveStrings[1] + "<dpB>" + TestData.ToString() + "<dpA>";



        

        return res;
    }



    // 这确实是个烂方法，好在要存的数据不多，暂且复制粘贴一下罢
    public void FromString(List<string> datas)
    {
        foreach (var d in datas)
        {

            string[] data = Regex.Split(d, "<dpB>");


            if (data[0].Contains(saveStrings[0]))
            {
                CyclesFromLastEnterSSAI = int.Parse(data[1]);
                // Plugin.Log("data:", saveStrings[0], CyclesFromLastEnterSSAI);
            }
            /*if (data[0].Contains(saveStrings[1]))
            {
                TestData = bool.Parse(data[1]);
                Plugin.Log("data:", saveStrings[1], TestData);
            }*/
        }

    }


}
