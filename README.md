安装方法：下载mod文件夹，把它放在 C:\Program Files (x86)\Steam\steamapps\common\Rain World\RainWorld_Data\StreamingAssets\mods

需要dlc和slugbase


Use this template on GitHub or [download the code](https://github.com/SlimeCubed/ExampleSlugBaseMod/archive/refs/heads/master.zip), whichever is easiest.

Links:
- [Template Walkthrough](https://slimecubed.github.io/slugbase/articles/template.html) for a guide to this template.
- [SlugBase Docs](https://slimecubed.github.io/slugbase/) for information regarding custom slugcats.
- [Modding Wiki](https://rainworldmodding.miraheze.org/wiki/Downpour_Reference/Mod_Directories) for `modinfo.json` documentation.

我还是适合敲代码啊。毕竟代码只分能跑和不能跑两种，但剧情与美术的好坏标准因人而异，放在我身上更是因时而异，今天写的/画的明天就会觉得烂，然后陷入西西弗斯推石头搬的来回修改。。。

To do list:
- （已完成）对于玩家有没有dlc的验证。比我想的简单多了，在modinfo里面写上就行
- （已完成）画皮肤，不要围巾了，懒得搞
- （已完成）修改技能，下面是我目前对技能的想法，基本上是魔改一下炸猫。其他的基本属性我没怎么想，反正那玩意儿改着方便，回头再说。
	- 消耗2格饱食度，把普通矛和没电的电矛变成充满电的电矛，对于有电的电矛不起作用
	- 尝试用炸矛进行制作会发生爆炸并造成晕眩。新年夜写这个代码，也算是一种放烟花吧……
	- 拥有很高的电击抗性（不能免疫fp那个电网以及蜈蚣）
	- 同时按住跳跃和抓取键，可以弹射起步，如果靠近水边会有电击伤害
	- 在上述操作的同时按住下键，可以对周围生物造成晕眩和致盲效果，可以在水下释放造成电击伤害（最超模的一集）
	- （可能会做）吞下垃圾消耗1格食物把它们变成闪光果。不打算做了，有上面那个技能谁还要闪光果。。
- 如果可以的话，修一下水下痛击队友的bug。其实队友无所谓，我主要是担心这个bug会电死猫崽
- 修改引路监视者
- 添加一个根据雨循环倒计时来改变贴图的功能
- 修改引路的监视者，并且让他们打一些特殊的小广告来代替台词。
- 画各种cg。我不想画画哼哼啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊
- 修改剧情，包括但不限于和moon的对话文本。我不要写剧情啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊
- 拿了珍珠就能读的功能，这估计需要重写珍珠文本（扶额）要不还是不做了罢。
