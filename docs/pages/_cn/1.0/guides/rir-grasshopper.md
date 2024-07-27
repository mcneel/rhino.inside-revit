---
title: Grasshopper in Revit
subtitle: How to use Grasshopper inside Revit
order: 11
group: Essentials
thumbnail: /static/images/guides/rir-grasshopper.png
ghdef: rir-grasshopper.ghx
---

{% include youtube_player.html id="VsE5uWQ-_oM" %}

## Revit-Aware 运算器

Revit-Aware 运算器图标能便于识别组件的功能，如下图所示， 基于颜色的不同而显示不同的操作类型（查询、分析、修改与创建）。也有一系列类似图标的徽章， 以显示组件所使用的数据类型、身份等：

![]({{ "/static/images/guides/rir-grasshopper-conventions@2x.png" | prepend: site.baseurl }}){: class="small-image"}

例如用于 {% include ltr/comp.html uuid='15ad6bf9' %} 的参数、查询、分析与创建组件的显示如下：

![]({{ "/static/images/guides/rir-grasshopper-compcolors@2x.png" | prepend: site.baseurl }}){: class="small-image"}

### 传递运算器

有些场合下需要用到一类特别的传递运算器（Pass-throught）将分析、修改与创建等操作整合到一个运算器中，这样有助于减少运算器的使用数量且能避免界面混乱的问题， 这些运算器会在后台进行拆分， 例如 {% include ltr/comp.html uuid='4cadc9aa' %} 或 {% include ltr/comp.html uuid='222b42df' %}:

![]({{ "/static/images/guides/rir-grasshopper-passthrucomps@2x.png" | prepend: site.baseurl }}){: class="small-image"}

下面以 {% include ltr/comp.html uuid='222b42df' %}为例， 这个运算左端有两组输入，第一个输入参数为 Revit 图元，在这个案例中用来处理材质 {% include ltr/comp.html uuid='b18ef2cc' %}，接着下面的一些输入参数用来更改 Revit 图元：

![]({{ "/static/images/guides/rir-grasshopper-passthruinputs.png" | prepend: site.baseurl }})

运算器的右端是一些常用的输出参数，**请注意** 输入与输出参数并不是总是相同的，Revit 图元会根据需要创建一些不同的属性，通常会有一些不同的图元与不同属性设置的需求，且有一些输出属性是基于图元计算才能计算出输出参数（例如墙体不能把体积作为输入，但可以作为输出参数），另外不是所有的图元属性都可以通过 Revit API 进行调整：

![]({{ "/static/images/guides/rir-grasshopper-passthruoutputs.png" | prepend: site.baseurl }})

传递运算器也可以自定义输出选项，可以用来处理 Revit 输出图元，例如这里填入 {% include ltr/comp.html uuid='b18ef2cc' %}:

![]({{ "/static/images/guides/rir-grasshopper-passthruhidden.gif" | prepend: site.baseurl }})

现在你应该能理解什么是传递运算器， 它们用来传递输入图元至输出图元以便于修改或分析这些图元，通常都是串联这些动作而不是并联，这样才能很好的保证操作顺序，因为这些目标图元都属于 Revit，Grasshopper 无法确定这些操作的全部含义：

![]({{ "/static/images/guides/rir-grasshopper-multiplepassthru.png" | prepend: site.baseurl }})

### 事件运算器

文档要知道那些运算器每一次的 Grasshopper 脚本执行中被调整而导致文档修改, 就变得很重要的, 这样可以更好的理解和管理事件,也能更好的执行事件 ( 例如开发者可能会调整图形逻辑组合更多的事件用以提供性能)

当这些组件执行具体的事件时会显示深色背景:

![]({{ "/static/images/guides/rir-grasshopper-transcomps.png" | prepend: site.baseurl }})

**提醒:** 如果输入参数与目标图元没有任何的调整, 下一次执行 Grasshopper 脚本时组件不会有任何的变更, 且组件的背景色将会显示会默认的灰色

您还可以使用 Grasshopper **Trigger** 运算器来控制这些运算器的执行时间：

![]({{ "/static/images/guides/rir-grasshopper-transcompstriggered.png" | prepend: site.baseurl }})

## 几何预览

你既可以使用 Grasshopper 运算器上的预览开关来切换预览打开或关闭 Revit 的显示, 也可以从 Revit 的 Rhinoceros 选项卡全局切换预览:

![]({{ "/static/images/guides/rir-grasshopper-preview.png" | prepend: site.baseurl }})

## 暂停(停止)计算

你可以在 Revit 的 Rhinoceros 选项卡中暂停(停止) Grasshopper 定义的执行, 这对于减少 Revit 大模型的等待事件非常有用:

![]({{ "/static/images/guides/rir-grasshopper-solver.png" | prepend: site.baseurl }})

## 图元追踪

Element Tracking 追踪功能可以让 Grasshopper 替换之前所创建的 Revit 图元, 及时是之前保存过的文件之间也可以替换. 每个运算器的输入都会记住它添加过那些Revit图元, 且能避免重复创建. 要注意,仅仅 Grasshopper 中 Add 运算器支持图元追踪功能. Grasshopper 文件关闭后再次打开会记住这些图元

详情如下面的视频示范:

{% include vimeo_player.html id="574667912" %}

可以通过鼠标右键点击运算器来控制图元追踪模式,

![]({{ "/static/images/guides/tracking-modes.png" | prepend: site.baseurl }})

提供三种不同的图元追踪模式:

1. **Disabled** - 关闭追踪所有创建的 Revit 图元, 这样可能会造成 Revit 中出现重复创建的图元
2. **Enabled: Replace** - 每次运行 Grasshopper 都会在 Revit 中建立新的图元替代之前创建过的图元
3. **Enabled: Update** - 该选项为默认值, Grasshopper 会尽力修改当前 Revit 中的图元(如果 Revit API 允许修改类型), 否在,运算器将会和 **Enabled: Replace** 一样建立新的图元

每个运算器的输出端也可以额外增加控制来管理追踪：

![]({{ "/static/images/guides/tracking-tools.png" | prepend: site.baseurl }}){: class="small-image"}

1. **Highlight** - 选择且会高亮由此输出端所建立的 Revit 图元
2. **Unpin** - 移除任何经由此输出端创建且被 Pin 的 Revit 图元， 还有一个 Unpin 运算器也具有类似功能
3. **Delete** - 删除任何由此输出端追踪的图元
4. **Release** - 忘记所有的输出对象且不会最终这些输出对象，要注意的是如果再次运行 Grasshopper 脚本文件可能会重复建立对象

## 单位系统

在 Revit 界面数字发生改变时系统会提醒选择一个系统单位， Revit在后台都是以英尺为单位，如果Rhino和Revit 的单位不统一时， Rhino会提醒修改单位系统与Revit 的显示单位保持一致。

![]({{ "/static/images/guides/unit-convertion.jpg" | prepend: site.baseurl }}){: class="small-image"}

这时无任你选择哪个选项，几何图像都会转换为两种制式中正确的尺寸。

请注意，在Rhino中缩放模型会影响到公差（下面即将讨论），如果模型被放大，例如毫米改为米都会对公差产生不好的影响。

## 公差

Revit 可以处理经由 Rhino所创建的 BREP（NURBS）与网格几何对象，任何几何物件都有一个很重要的特性 - [几何公差](https://wiki.mcneel.com/rhino/faqtolerances) ，Revit 在进行几何转换时也会多方面的受到公差的影响， 请注意：

* 理论上 Rhino的Brep可以直接转换至 Revit, 但如果公差设置不正确会造成转换失败。
* 无法直接转换的几何物件会透过 SAT 格式来进行转换，但SAT的转换会很慢，通常需要用到SAT转换的运算器都会显示警告
  ![]({{ "/static/images/guides/directshape-use-sat.jpg" | prepend: site.baseurl }}){: class="small-image"}
* 如果无法使用常规或SAT方式转换的 DirectShape 图元可以使用带有密集黑色边缘的网格模型输入至 Revit
* 族类仅能接受 NURBS 几何物件，所以必须注意公差问题，如果某些几何物件无法转换为族类型会显示一个错误提醒
* 远离原点的物件可能无法保证一定的的公差精度，隐藏会造成输入Revit 失败

理论上一个Rhino模型和一个Revit模型都可以具有相同精度的公差。

所有线段长度小于1mm的曲线都无法被 Revit 接受，也包括修剪过的曲面边缘，长度包括：

* 1/256（0.0039）英尺
* 3/64（0.047）英寸
* 大概1毫米

所有 NURBS 的公差 Rhino都会与Revit 内置公差设置一致，可以通过下拉菜单 工具>选项>单位来进行设置， 以匹配 Revit 的单位类型与公差：

* 0.1 毫米
* 0.0001 米
* 0.006英寸
* 0.0005 英尺

很多时候无法保证 Rhino 模型公差总是与Revit 的一致， 例如模型经由其他软件创建而仅仅只是在 Rhino中被打开， 处理方法如下：

1. 在 Rhino中执行 Selbadobject 以搜索这些坏的物件，然后修复或替换之 ，浏览[查找与修复损坏物件的过程](https://wiki.mcneel.com/rhino/badobjects)
2. 参考前面的方法设置公差，物件的公差也会被重置

修复带有公差问题的模型过程有一些复杂， 为其重置正确公差的方法如下；

1. 通过 Explode 指令将多重曲面炸开为多个曲面；
2. 选择所有已经炸开的曲面，然后执行 RebuildEdge 指令，然后套用默认设置重建曲面边缘；
3. 执行 Join 指令以组合所有被重建边缘的曲面；
4. 运行 ShowEdge 指令， 检测所有外露边缘，所有超出公差的部分都会以外露边缘进行显示。

## Grasshopper 性能

请注意以下的一些提醒会影响 Grasshopper 定义的执行性能：

- Grasshopper 是以插件方式运行在 Revit 的内部， 因此当 Revit 运行比较慢时（例如打开大的模型、开启太多视图…) 会让 Grasshopper 无法获得更多的资源进行计算或预览显示
- Revit 中显示 Grasshopper 的内容都需要进行几何转换，预览数据过多时也会降低 Revit 的视图查看速度，你可以通过切换 Revit 的 Rhinoceros 栏内的全局预览开关，选择性的预览所需要显示的Grasshopper 内容
- 在单个图元上运行多个事件会比多个图元上执行单个事件要慢，因此在 Revit 中设计 Grasshopper 脚本时请尽力在执行单个事件时一次性对多个图元进行修改与调整
- 关闭 Grasshopper 脚本计算能减少大型 Revit 模型的等待时间
- 低质量与超出公差范围的模型会增加转换时间，特别是公差超出的问题