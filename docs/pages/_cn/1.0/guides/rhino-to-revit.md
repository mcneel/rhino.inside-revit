---
title: "Rhino to Revit"
subtitle: How to move geometry and data from Rhino into Revit
order: 31
thumbnail: /static/images/guides/rhino-to-revit.png
group: Essentials
---

{% capture link_note %}
这个章节我们将讲解如何利用 Grasshopper 将 Rhino的几何无损转入 {{ site.terms.revit }} 。
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/rhino-to-revit.png' %}

 {{ site.terms.rir }} 可以把 Rhino的造型与表单编码为 Revit 图元且进行分类，请注意仅将几何物件简单、快速的转入至Revit可能不是最好的方法，要确定 Revit 中形体的最终目标，可以提高最终Revit 数据结构的质量与项目效率。

Revit 数据模型是基于（族）分类系统，侦测最佳的分类与子分类图元将会被正确的绘制且赋予正确属性， 请注意我们这里每一种模式并不都适合所有的 Revit 类别。

这里将介绍三种主要的方式来分类与转移Rhino的几何图形移至 Revit, 越靠后的方式越会增加 BIM 模型的集成度，但也同时需要更多的提前规划，这三种模式分别为：

1. 使用 [DirectShapes](#rhino-objects-as-directshapes) ，这是最快转换模型的方法且不需要过多的组织，比较适合于竞赛或是方案设计阶段的演示，但不推荐用于设计方案深化与后期阶段
2. [**可载入族**](#rhino-objects-as-loadable-families) ， 适合于模型中的独立图元或是由独立制造商订购与加工的图元，作为一个族的部分，这些对象既可以是一个大型项目图形的一部分，也可以额外拥有自己的图形
3. **系统族**，这是[基于Rhino几何建立原生 Revit 图元](#using-revit-built-in-system-families) 的最好办法，Revit原生图元能最好的匹配 Revit 的工作环境，这些原生图元可以脱离 {{ site.terms.rir }}. 进行编辑。这个模式也会存在一些限制，不是所有几何物件都可以建立为 Revit 原生图元。

需要确定你要使用那种方法和类别时， [请参考此 Revit 类别列表以及它们可以从 {{ site.terms.rir }}接受的族类型](https://docs.google.com/spreadsheets/d/1l8koAQtsz0o9iK80gmpC0HqSAFqpqAb0rKYdXcQkBWE/edit?usp=sharing).

下图是Rhino模型使用 DirectShapes 模式快速在 Revit 中构建图形的一个竞赛模型：

![Competition model in Rhino]({{ "/static/images/guides/rhino-office-display.jpg" | prepend: site.baseurl }})

通过一个简单的 Grasshopper 脚本，既可以按照高度对物件进行归类

![A Quick Elevation in Revit]({{ "/static/images/guides/revit-office-elevation.jpg" | prepend: site.baseurl }})

使用类别控制的方式来控制的平面图

![A Quick Plan in Revit]({{ "/static/images/guides/revit-office-plan.jpg" | prepend: site.baseurl }})

## DirectShapes

[DirectShapes](https://www.rhino3d.com/inside/revit/1.0/guides/revit-directshapes) 是将Rhino模型最直接、最简单导入至Revit的方法，它也是建立通用的 Revit 图元，可以置于Revit模型中进行分类的非参数化图元，请注意它们都是非参数化的图元，Revit 并不知道它是如何建立，因此它也无法与其他原生图元进行交互。例如 Revit 原生的墙体无法延伸至 DirectShapes转换的图元。

DirectShapes 适用于下面的这些场合：

1. 用于比赛或早期方案设计阶段的快速模型；
2. 设计阶段还在调整、待确定的占位建筑构件，例如虽然地板已经完成，但Grasshopper中的外墙部分还在继续调整，这样使用 DirectShape 作为里面与其他设计阶段的图纸的占位建筑构件会更好；
3. 一个无法使用 Revit 原生族进行建模且完全定制零件或部件。

{% include youtube_player.html id="HAMPkiA5_Ug" %}

DirectShapes 可以放置在任何级别的类别，可以通过对象样式来控制图形与材料：

![Create a DirectShapes]({{ "/static/images/guides/rhino-to-revit-directshape.png" | prepend: site.baseurl }})

为了在同一个类别的图元之间进行额外的图形控制, 可以[基于规则的视图筛选](https://help.autodesk.com/view/RVT/2014/ENU/?guid=GUID-145815E2-5699-40FE-A358-FFC739DB7C46) 来自定义参数值，DirectShapes 不能直接放于子类别，但可以输入的方式将源几何导入族内且可以进一步进行子类别归纳（后面的章节会详细介绍）：

![Add a Shared Parameter for a filter]({{ "/static/images/guides/directshape-filter-gh.png" | prepend: site.baseurl }})

除了可以使用 DirectShapes将Rhino物件导入Reivt之外，还可以创建可重复多次插入的 DirectShapes类型：

![Insert multiple DirectShape instances]({{ "/static/images/guides/rhino-to-revit-directshape-instance.png" | prepend: site.baseurl }})

{% capture api_warning_note %}
由 Rhino 中顺滑NURBS曲面所创建DirectShapes 可以以顺滑的实体导入或由 Revit 转换为一个网格，如果NURBS被转换为网格，则说明 Revit 无法接受NURBS，由很多原因造成NURBS被 Revit 拒绝，但通常这些问题都可以在 Rhino 中修复。
{% endcapture %}
{% include ltr/warning_note.html note=api_warning_note %}

深入了解 Directshapes 及其使用方法请浏览 [DirectShape Guide](https://www.rhino3d.com/inside/revit/1.0/guides/revit-directshapes).

## 包含子类别的可载入族

Rhino 对象以表单的形式插入Revit的一个族，且允许作为一个对象插入多个实例也可以指派 [子类别](https://help.autodesk.com/view/RVT/2014/ENU/?guid=GUID-8C1F9882-E4AB-4E03-A735-8C44F19E194B). 你可以使用子类别的方式来控制其顶级类别中某个族部分的可见性和图形。

将 Rhino 几何包裹至可载入族有如下的几个优点：

* 可以多次插入重复的对象，这样可以正确的统筹与统计表单；
* 可载入族内的表单可以按需被 Revit 编辑；
* 放置在 族/类型中的表单可以放置再子类别中，便于进一步的图形控制与调度。

例如这里有一个范例，在Rhino中完成的一个走廊天蓬，这个结构件将由专业的建造商来承建。小的地基会在现场浇灌，其他的人行道将在上面组装。因此地基是族的一部分，而结构的其他部分是另外一个族的部分。

![An Exterior Walkway in Rhino]({{ "/static/images/guides/canopy-rhino.png" | prepend: site.baseurl }})

可以自动转换的方式将 Rhino的图层映射为 Revit 中的子类别， 在Revit 中可以以子类别与视图的方式来控制图层与材料：

![Plan view with Sub-categories]({{ "/static/images/guides/canopy-plan.png" | prepend: site.baseurl }})

![Elevation view with Sub-categories]({{ "/static/images/guides/canopy-elevation.png" | prepend: site.baseurl }})

关于如何创建子类别，请参考下面的视频教学

{% include youtube_player.html id="z57Ic0-4r2I" %}

以下是在 Revit 中创建新的可加载族定义的关键部分，这个族由黑色的 {% include ltr/comp.html uuid="82523911" %} 运算器创建。输入包括族模板、新族的名称以及所属的类别等，此外 {% include ltr/comp.html uuid='72fdc627' %} 运算器用于创建放置于族内的Forms，Forms 运算器接受每一个造型的 Brep 几何、可见性、子类别与材料等数据。如果子类别尚不存在，则可以使用 {% include ltr/comp.html uuid='8de336fb' %} 运算器来创建或分配子类别，然后将其发送至创建族的运算器：

![Creating subcategory]({{ "/static/images/guides/subcategory-rhino-revit-gh.png" | prepend: site.baseurl }})

如果子类别不存在，子类别运算器会创建一个新的子类别

可以在物件类别对话框编辑子类别属性：

![An Exterior Walkway in Rhino]({{ "/static/images/guides/revit-objectstyles.jpg" | prepend: site.baseurl }})

子类别也可以与[基于规则的图形挑选工具](https://help.autodesk.com/view/RVT/2024/ENU/?guid=GUID-145815E2-5699-40FE-A358-FFC739DB7C46)一起使用以实现额外的图形控制。

## 使用 Revit 内建系统族

使用内置的 Revit 系统族(例如墙、地板、天花板和屋顶)可能会更费精力，但它能提供很多额外的帮助，原生图元的优点有很多，包括：

1. 能更好的集成项目的BIM模式，包括最大程度的图形控制、动态内建参数值，能像原生图元一样访问所有常见项目的标准BIM参数；
2. 图元可以脱离 {{ site.terms.rir }} 进行编辑，也可以对其附加其他的尺寸，这些图元也可以托管其他的图元；
3. 便于与其他非 {{ site.terms.rir }} 用户交流，他们并不会意识到这些图元是基于 Rhino.Inside.Revit 所建立。

这里提供一个视频教学，讲解如何利用 {{ site.terms.rir }} 来建立原生的楼层、地面、立柱与幕墙面板:

{% include youtube_player.html id="cc3WLvGkWcc" %}

如何使用 Revit 图元的每个类别, 请查看 [Modeling section in Guides]({{ site.baseurl }}{% link _en/1.0/guides/index.md %}#modeling-in-revit)
