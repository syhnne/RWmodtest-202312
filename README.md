安装方法：下载mod文件夹，把它放在 C:\Program Files (x86)\Steam\steamapps\common\Rain World\RainWorld_Data\StreamingAssets\mods

需要dlc和slugbase



我还是适合敲代码啊。毕竟代码只分能跑和不能跑两种，但剧情与美术的好坏标准因人而异，放在我身上更是因时而异，今天写的/画的明天就会觉得烂，然后陷入西西弗斯推石头搬的来回修改。。。

To do list:
- （已完成）对于玩家有没有dlc的验证。比我想的简单多了，在modinfo里面写上就行
- （已完成）画皮肤，不要围巾了，懒得搞
- （已完成）修改技能，下面是我目前对技能的想法，基本上是魔改一下炸猫。其他的基本属性我没怎么想，反正那玩意儿改着方便，回头再说。
	- 消耗2格饱食度，把普通矛和没电的电矛变成充满电的电矛，对于有电的电矛不起作用
	- 尝试用炸矛进行制作会发生爆炸并造成晕眩。新年夜写这个代码，也算是一种放烟花吧……
	- 拥有很高的电击抗性，免疫蜈蚣和线圈的致死电击（或许可以靠这个吃饱。如果后期游戏难度太大，我会把吃饱的功能加回来）
	- 同时按住跳跃和抓取键，可以弹射起步，如果靠近水边会有电击伤害
	- 在上述操作的同时按住下键，可以对周围生物造成晕眩和致盲效果，可以在水下释放造成电击伤害（最超模的一集）
	- 跳跃计数器为0时常驻二倍矛伤。如果使用了二段跳，会导致矛伤快速衰减，不会低于0.5
- 水下痛击队友的bug不修了，这是特性（你
- 随着剩余的雨循环减少，雨眠所需的食物数量会发生变化
- 你一共有21个雨循环，循环耗尽会彻底死亡。其实这东西比真结局难打多了，因为到后期根本吃不饱（
- （下次一定）改香菇病特效，改成恐怖的蓝色
- 添加一个根据雨循环倒计时来改变贴图的功能
- 虽然我极度不想画画，但是加一点自定义梦境来说明一下发生了什么。。
- 修改引路的监视者，并且让他们打一些特殊的小广告来代替台词。
- 做一个SS_AI控制面板。主要是为了酷炫（
- 画各种cg。要不还是喊人画吧。
- 修改剧情，包括但不限于和moon的对话文本。喊人来写吧。。
- 拿了珍珠就能读的功能，这估计需要重写珍珠文本（扶额）要不还是不做了罢，或许某一天机猫可以做到这件事（


已知问题：
- 和部分mod不适配，因为有些函数我绕过了原版的方法。在修了在修了，ILhook这玩意儿简直不是给人写的（汗
- 如果启用mod后贴图显示为白色，并且没有任何技能，禁用并重新启用mod即可。这是我修改了判定方式导致的，现在我又修改了一种判定方式，不知是否有望解决那个怪bug
- 不要用猫崽体型游玩，因为我发现猫崽的头和大猫不是一个材质，还得重新画（瘫
