Use this template on GitHub or [download the code](https://github.com/SlimeCubed/ExampleSlugBaseMod/archive/refs/heads/master.zip), whichever is easiest.

Links:
- [Template Walkthrough](https://slimecubed.github.io/slugbase/articles/template.html) for a guide to this template.
- [SlugBase Docs](https://slimecubed.github.io/slugbase/) for information regarding custom slugcats.
- [Modding Wiki](https://rainworldmodding.miraheze.org/wiki/Downpour_Reference/Mod_Directories) for `modinfo.json` documentation.

任何口嗨终将变成烂尾工程，除非我不自己写剧情

只要……只要能到达那个地方……（指做完技能去lofter摇人帮忙

To do list:
- （已完成）对于玩家有没有dlc的验证。比我想的简单多了，在slugbase的modinfo里面写上就行
- 画皮肤，我想画个围巾，能随风飘动的那种。这恐怕比下面说的改一些剧情行为还要费劲。。
- 画封面图。我不想画画哼哼啊啊啊啊啊啊啊啊啊啊啊啊啊（突发恶疾）
- （已完成）修改技能，下面是我目前对技能的想法，基本上是魔改一下炸猫。其他的基本属性我没怎么想，反正那玩意儿改着方便，回头再说。
	- 消耗2格饱食度，把普通矛和没电的电矛变成充满电的电矛，对于有电的电矛不起作用（https://rainworld.miraheze.org/wiki/Electric_Spear/zh-hans）
	- 尝试用炸矛进行制作会发生爆炸并造成晕眩。新年夜写这个代码，也算是一种放烟花吧……
	- 拥有很高的电击抗性（不能免疫fp那个电网以及蜈蚣）
	- 同时按住跳跃和抓取键，可以弹射起步，如果靠近水边会有电击伤害
	- 在上述操作的同时按住下键，可以对周围生物造成晕眩和致盲效果，可以在水下释放造成电击伤害（最超模的一集）
	- （可能会做）吞下垃圾消耗1格食物把它们变成闪光果。不打算做了，有上面那个技能谁还要闪光果。。
- 修改地图（可能要用到regionkit）
- 修改剧情，包括但不限于和moon的对话文本（可能要用到iteratorkit），飞升cg
- 修改引路的监视者，并且让他们打一些特殊的小广告来代替台词。这听上去太难了，我不如自己做一个dependency（
- 拿了珍珠就能读的功能，这估计需要重写珍珠文本（扶额）
- 一个有关剧情的想法，既难写又难做，估计只能口嗨一下。fp变成蛞蝓猫之后需要在20（？）个雨循环之内找到变回去的方法（可能是去找月姐？我没有想过，好麻烦）否则他的巨构建筑就会像月姐那样倒塌，这涉及到一个更新地图的问题，原作没有任何同一只蛞蝓猫经历地图更新这种事，所以我恐怕做不到（）

我找到一个非常好的教程：https://rwmoddingch.github.io/ChModdingWiki/

他解决了我看不懂代码的疑惑，我觉得我又行了。