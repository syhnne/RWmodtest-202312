Use this template on GitHub or [download the code](https://github.com/SlimeCubed/ExampleSlugBaseMod/archive/refs/heads/master.zip), whichever is easiest.

Links:
- [Template Walkthrough](https://slimecubed.github.io/slugbase/articles/template.html) for a guide to this template.
- [SlugBase Docs](https://slimecubed.github.io/slugbase/) for information regarding custom slugcats.
- [Modding Wiki](https://rainworldmodding.miraheze.org/wiki/Downpour_Reference/Mod_Directories) for `modinfo.json` documentation.

这是我两天前和朋友口嗨的fp蛞蝓猫，由于这个设定非常杀软，我觉得必须要在一个月之内做出来，不然我就会全删了。所以最好是速战速决，用这个寒假至少解决代码的问题。
剩下的部分（特指剧情）准备摇人帮我做（

区区C#，不足为惧。这应该比写剧情简单吧。。。（心虚）

To do list:
- 对于玩家有没有dlc的验证。主要是因为需要制作电矛，但我不知道这个物品具体在哪个扩展里头。可以去创意工坊找找有没有那种需要dlc且开源的mod，然后照搬他的代码
- 食物偏好（我不知道，lof有人这么设定，但我懒得思考了，这些数值什么的照抄工匠吧）
- 画皮肤，我想画个围巾。那个先驱者好像有围巾来着，但我看了那mod不是开源的，我还得再找找参考，这玩意儿怕是比技能什么的难多了
- 画封面图，呃啊！我不想画画！！！哼哼啊啊啊啊啊啊啊啊啊啊啊啊啊（突发恶疾）
- 修改技能，下面是我目前对技能的想法，基本上是魔改一下炸猫（
	- 消耗1格饱食度，给没电的电矛充电。对于有电的电矛不起作用（参考：https://rainworld.miraheze.org/wiki/Electric_Spear/zh-hans）
	- 消耗3（？）格饱食度，把普通矛变成充满电的电矛
	- 尝试用炸矛进行制作会发生爆炸并造成晕眩
	- 免疫电击带来的伤害和晕眩（这稍微有点超模了，因为他相当于免疫所有蜈蚣，那样的话我最好把蜈蚣的行为也改一改）
	- 同时按住跳跃和抓取键，可以对周围的生物造成电击，晕眩（还是致盲？）他们
	- 在上述操作的同时按住下键，可以进行一次范围更大，且能造成伤害的电击
	- （说白了就是删掉了炸猫的炸弹跳功能，但有正常的肺活量）
	- 剩下的属性让我很纠结，我不是很想直接用炸猫的属性，回头再说吧。。
- 修改地图（可能要用到regionkit）
- 修改剧情，包括但不限于和moon的对话文本（可能要用到iteratorkit），飞升cg
- 拿了珍珠就能读的功能，这估计需要重写珍珠文本（扶额）
- 一个有关剧情的想法，既难写又难做，估计只能口嗨一下。fp变成蛞蝓猫之后需要在20（？）个雨循环之内找到变回去的方法（可能是去找月姐？我没有想过，好麻烦）否则他的巨构建筑就会像月姐那样倒塌，这涉及到一个更新地图的问题，原作没有任何同一只蛞蝓猫经历地图更新这种事，所以我恐怕做不到（）



hook参考：https://github.com/pkuyo/Nutils/tree/master
https://github.com/Zeldack974/Silkslug/tree/master/src


20231230
方便起见，我还是准备把所有笔记都写这里。我找到一个非常好的教程：https://rwmoddingch.github.io/ChModdingWiki/
他解决了我看不懂代码的疑惑，我觉得我又行了。
按照它的说法，我要先用自然语言把执行技能的流程描述一遍。我可以不这么做，前提是我找得到炸猫的代码。
啊，我好像理解为什么炸猫易溶于水了——因为这样不用给水下爆炸技能写额外代码。
但我这边还算方便，水下电击有一个单独的方法，我应该大概或许可以直接拿来用。

woc，json不让注释？？
以后做完了再把这个加回去，我受不了fp门口那个零重力空间了
"start_room": "SS_D07",