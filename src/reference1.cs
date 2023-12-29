using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//ͨ�����ѧϰ���дhook�������ģ��ĵ�ѹ��û�н̸����κ����֪ʶ����ֻ��ȥ���⹤�������ֺ������������ҿ�Դ��mod��ѧ�ˡ���
namespace Nutils.hook
{
    using MonoMod.Cil;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Mono.Cecil.Cil;
    using UnityEngine;
    using SlugTemplate;


    /// <summary>
    /// ������ӻ��ֹèʳ�ò�����Ʒ
    /// </summary>
    public class CustomEdibleData
    {
        public SlugcatStats.Name name;

        public FoodData[] edibleDatas;

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="name">è������</param>
        /// <param name="edibleDatas">��ʳ�����ֹʳ�����ã��ɴ��ݶ���</param>
        public CustomEdibleData(SlugcatStats.Name name, params FoodData[] edibleDatas)
        {
            this.name = name;
            this.edibleDatas = edibleDatas;
        }

        public class FoodData
        {
            public AbstractPhysicalObject.AbstractObjectType edibleType;
            public AbstractPhysicalObject.AbstractObjectType forbidType;
            public int food;
            public int qFood;

            /// <summary>
            /// ����µĽ�ֹ��
            /// </summary>
            /// <param name="forbidType">��ֹ��ʳ�������</param>
            public FoodData(AbstractPhysicalObject.AbstractObjectType forbidType)
            {
                this.forbidType = forbidType;
                food = qFood = -1;
            }


            /// <summary>
            /// ����µĿ�ʳ����
            /// </summary>
            /// <param name="edibleType">��ʳ�õ���������</param>
            /// <param name="food">ʳ�ûظ���������ʳ��</param>
            /// <param name="quarterFood">ʳ�ûظ���С����ʳ��(1/4��)</param>
            public FoodData(AbstractPhysicalObject.AbstractObjectType edibleType, int food, int quarterFood)
            {
                this.edibleType = edibleType;
                this.food = food;
                this.qFood = quarterFood;
            }
        }
    }

    public static class CustomEdible
    {
        
        public static void Register(CustomEdibleData data)
        {
            CustomEdibleHook.OnModInit();
            if (!edibleDatas.ContainsKey(data.name))
            {
                edibleDatas.Add(data.name, data);
            }
            else
            {
                Plugin.Log("Already register for cat : " + data.name);
            }
        }

        public static Dictionary<SlugcatStats.Name, CustomEdibleData> edibleDatas =
            new Dictionary<SlugcatStats.Name, CustomEdibleData>();
    }

    static class CustomEdibleHook
    {
        static bool isLoaded;
        public static void OnModInit()
        {
            //��������������mod��û�м��س�����
            if (!isLoaded)
            {
                IL.Player.GrabUpdate += Player_GrabUpdate_EdibleIL;
                On.Player.BiteEdibleObject += Player_BiteEdibleObject;
                isLoaded = true;
            }
        }


        private static void Player_GrabUpdate_EdibleIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                ILLabel label = c.DefineLabel();
                ILLabel label2 = c.DefineLabel();
                c.GotoNext(MoveType.Before, i => i.MatchLdarg(0),
                                           i => i.MatchCall<Creature>("get_grasps"),
                                           i => i.MatchLdloc(13),
                                           i => i.MatchLdelemRef(),
                                           i => i.MatchLdfld<Creature.Grasp>("grabbed"),
                                           i => i.MatchIsinst<IPlayerEdible>());
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_S, (byte)13);
                c.EmitDelegate<Func<Player, int, bool>>(EdibleForCat);
                c.Emit(OpCodes.Brtrue_S, label);

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_S, (byte)13);
                c.EmitDelegate<Func<Player, int, bool>>((self, index) =>
                {
                    if (CustomEdible.edibleDatas.ContainsKey(self.slugcatStats.name) &&
                        CustomEdible.edibleDatas[self.slugcatStats.name].edibleDatas.Any(i =>
                            i.forbidType == self.grasps[index].grabbed.abstractPhysicalObject.type))
                        return false;
                    return true;
                });
                c.Emit(OpCodes.Brfalse_S, label2);
                c.GotoNext(MoveType.Before, i => i.MatchLdloc(13),
                                            i => i.MatchStloc(6),
                                            i => i.MatchLdloc(13));
                c.MarkLabel(label);
                c.GotoNext(MoveType.After, i => i.MatchStloc(6));
                c.MarkLabel(label2);

            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        static bool EdibleForCat(Player player, int index)
        {
            if (!CustomEdible.edibleDatas.ContainsKey(player.slugcatStats.name))
                return false;
            var grasp = player.grasps[index];

            if (grasp != null)
            {
                if (CustomEdible.edibleDatas[player.slugcatStats.name].edibleDatas.
                    Any(i => i.edibleType == grasp.grabbed.abstractPhysicalObject.type))
                    return true;
            }

            return false;
        }
        private static void Player_BiteEdibleObject(On.Player.orig_BiteEdibleObject orig, Player self, bool eu)
        {
            bool canBitOther = self.grasps.All(i => !(i?.grabbed is IPlayerEdible));
            orig(self, eu);
            if (canBitOther)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (self.grasps[i] != null && CustomEdible.edibleDatas.ContainsKey(self.slugcatStats.name) &&
                        CustomEdible.edibleDatas[self.slugcatStats.name].edibleDatas.
                            Any(d => d.edibleType == self.grasps[i].grabbed.abstractPhysicalObject.type))
                    {
                        var data = CustomEdible.edibleDatas[self.slugcatStats.name].edibleDatas.
                            First(d => d.edibleType == self.grasps[i].grabbed.abstractPhysicalObject.type);
                        if (self.SessionRecord != null)
                        {
                            self.SessionRecord.AddEat(self.grasps[i].grabbed);
                        }
                        (self.graphicsModule as PlayerGraphics)?.BiteFly(i);
                        self.AddFood(data.food);
                        for (int j = 0; j < data.qFood; j++)
                            self.AddQuarterFood();
                        var obj = self.grasps[i].grabbed;
                        self.grasps[i].Release();
                        obj.Destroy();
                    }
                }
            }
        }

    }
}