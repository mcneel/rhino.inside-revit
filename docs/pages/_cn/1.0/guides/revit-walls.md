---
title: Walls (Basic & Stacked)
order: 40
group: Modeling
home: true
thumbnail: /static/images/guides/revit-walls.png
subtitle: Workflows for Revit Basic and Stacked Walls
ghdef: revit-walls.ghx
---

## Querying Wall Types

{% capture api_note %}
Revit API 中墙体类型由 {% include api_type.html type='Autodesk.Revit.DB.WallType' title='DB.WallType' %}来表示，墙有三个主要的系统族，经由 {% include api_type.html type='Autodesk.Revit.DB.WallKind' title='DB.WallKind' %} 来进行枚举且由 `DB.WallType.Kind来检查与确定，  {{ site.terms.rir }}为了保持一致性，使用了墙体系统族这一术语， {% include ltr/comp.html uuid='fe427d04' %} 可以代表一个墙体类型。
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

可以使用 {% include ltr/comp.html uuid="d08f7ab1-" %} 与 {% include ltr/comp.html uuid="7b00f940-" %} 来查询 Revit 模型中所有有的墙体类型:

![]({{ "/static/images/guides/revit-walls-querywalltypes.png" | prepend: site.baseurl }})

## 查询墙体

{% capture api_note %}
在 Revit 的API 中使用 {% include api_type.html type='Autodesk.Revit.DB.Wall' title='DB.Wall' %}来表达墙体，{{ site.terms.rir }} 中的 {% include ltr/comp.html uuid='15ad6bf9' %} 可以表达所有类型的墙体。
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### 查询所有墙体

联合 {% include ltr/comp.html uuid="d08f7ab1-" %} 与 {% include ltr/comp.html uuid="0f7da57e-" %} 运算器来查询一个 Revit 模型中所选择的墙体实例:

![]({{ "/static/images/guides/revit-walls-querywalls.png" | prepend: site.baseurl }})

{% include ltr/warning_note.html note='使用上面的流程 Revit API 也会返回 Stacked Wall 上的各部分墙' %}

### 墙系统族

建议基于墙系统族的方式来查询基础墙，使用 {% include ltr/comp.html uuid="15545e80-" %} 运算器来挑选Revit 中内置的基础墙、叠层墙或是幕墙或是组合墙，然后输入至 {% include ltr/comp.html uuid="118f5744-" %} 进行查询，结果如下图所示：

![]({{ "/static/images/guides/revit-walls-querybysystem.png" | prepend: site.baseurl }})

### 墙体类型

墙体类型的查询非常容易，例如 [Data Model: Elements & Instances]({{ site.baseurl }}{% link _en/1.0/guides/revit-elements.md %}#instances) 中介绍过的流程：

![]({{ "/static/images/guides/revit-walls-querywalltype.png" | prepend: site.baseurl }})

## 墙体类型分析

### 读取类型参数

一旦你使用上面的一些工作流程完成了墙体类型的筛选，你就可以查询其参数与修改参数。浏览 [Document Model: Parameters]({{ site.baseurl }}{% link _en/1.0/guides/revit-params.md %}) 详细的介绍如何修改一个图元类型的参数

### 分析基础墙

基础墙是 Revit 中一种特殊的墙系统族，由一组定义为墙类型的图层构成，也有一些独特的选项，例如 **Wrapping at Inserts** . 使用 {% include ltr/comp.html uuid="00a650ed-" %}  运算器可以显示与分析 Revit 文档中基础墙类型信息：

![]({{ "/static/images/guides/revit-walls-analyzebasictype.png" | prepend: site.baseurl }})

有些特定的输出项（例如 **Wrapping at Inserts** 与 **Wrapping at Ends** )会返回一个与Revit API 枚举像对应的整数值， 你可以使用 {% include ltr/comp.html uuid='141f0da4' %} 与 {% include ltr/comp.html uuid='c84653dd' %} （位于上图中参数数值面板前）来确定在参数上设置的值且能给筛选源墙类型， 例如下面示范如何利用这些运算器来进行 *Wrapping* 与 *Function* 筛选：

![]({{ "/static/images/guides/revit-walls-analyzebasictype-filter.png" | prepend: site.baseurl }})

### 基础墙结构

{% capture api_note %}
在 Revit API 中使用 {% include api_type.html type='Autodesk.Revit.DB.CompoundStructure' title='DB.CompoundStructure' %}来表示允许配置类型的结构定义，例如基础墙、地板、屋顶与复合天花板等， `DB.CompoundStructure` 可以访问 {% include api_type.html type='Autodesk.Revit.DB.CompoundStructureLayer' title='DB.CompoundStructureLayer' %} 的每个独立层。
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

前面示范了 {% include ltr/comp.html uuid="00a650ed-" %} 运算器如何访问**基础墙** 类型的**复合结构定义** ，也可以使用 {% include ltr/comp.html uuid="120090a3-" %} 运算器来显示与提取 **复合结构定义** ，和上面的比较相似, {% include ltr/comp.html uuid='55b31952' %} 与 {% include ltr/comp.html uuid='8d73d533' %}  运算器可以用来比较与筛选结构图层：

![]({{ "/static/images/guides/revit-walls-compstruct.png" | prepend: site.baseurl }})

如上面所示，图层从**外部** 到**内部** 排序，与 Revit GUI 图层结构匹配，下面示范如何以序号索引来访问每一个图层：

![]({{ "/static/images/guides/revit-walls-compstructlayer.png" | prepend: site.baseurl }})

### 基础墙结构层

使用 {% include ltr/comp.html uuid="6b32703e-" %} 运算器提取每个复合结构层的信息，还可以使用 {% include ltr/comp.html uuid='439ba763' %} 与 {% include ltr/comp.html uuid='db470316' %} 运算器来进行对比：

![]({{ "/static/images/guides/revit-walls-analyzecompstructlayer.png" | prepend: site.baseurl }})

### 叠层墙结构

{% include ltr/warning_note.html image='/static/images/guides/revit-walls-stackedwallstruct.png' note=' 当前的 Revit API 并不支持直接访问叠层墙结构数据， 但你可以使用 Analyse Stacked Wall 运算器来提取内置的 基础墙实例，且能解析每一个图层结构：' %}

## 分析墙体

### 读取实例参数

如果你使用上面的一些方法筛选到所需的墙体实例，继而可以查询器参数值且可以赋予新的参数值， 浏览 [Document Model: Parameters]({{ site.baseurl }}{% link _en/1.0/guides/revit-params.md %}) 了解如何编辑一个图元的参数值。

### 常见墙体属性

如下图所示使用 {% include ltr/comp.html uuid="1169ceb6-" %} 运算器可以获取所有墙系统族的常见属性， {% include ltr/comp.html uuid='15545e80' %} 与 {% include ltr/comp.html uuid='1f3053c0' %} 运算器还可以对比参数值：

![]({{ "/static/images/guides/revit-walls-analyzewall.png" | prepend: site.baseurl }})

{% include ltr/api_note.html note="Slant Angle 属性仅在 Revit 2021 或更高的版本上能支持" %}

下面示范如何使用 **Wall Structural Usage 值列表** 运算器来筛选 **Shear** 墙：

![]({{ "/static/images/guides/revit-walls-analyzewall-filter.png" | prepend: site.baseurl }})

输出选项 **Orientation** 用来显示墙的原始向量

![]({{ "/static/images/guides/revit-walls-analyzewall-orient.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-walls-analyzewall-orientvectors.png" | prepend: site.baseurl }})

### 墙定位线

{% capture api_note %}
在 Revit API 中使用 {% include api_type.html type='Autodesk.Revit.DB.WallLocationLine' title='DB.WallLocationLine' %} 来枚举一个基础墙或叠层墙的定位线，且其墙体实例中的参数储存在`DB.BuiltInParameter.WALL_KEY_REF_PARAM` 参数中
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

基准墙与叠层墙有一个称之为定位线的概念，用来定义墙体实例的垂直参考平面，当墙被翻转或它的结构被修改时会被维持固定在这个垂直参考平面上。运算器 {% include ltr/comp.html uuid="4c5260c3-" %} 可以提取墙体的定位线信息，它会返回墙体的中心线、位置线设置、位置线、偏移与偏移方向等信息：

![]({{ "/static/images/guides/revit-walls-walllocation.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-walls-walllocationlines.png" | prepend: site.baseurl }})

{% include ltr/comp.html uuid='a4eb9313' %} 运算器也提供基于定位线（**Location Line** ）筛选功能：

![]({{ "/static/images/guides/revit-walls-walllocation-filter.png" | prepend: site.baseurl }})

如果你仅需要提取墙体的中心线，Grasshopper 中最简单的方法是直接使用 Curve 运算器来筛选即可：

![]({{ "/static/images/guides/revit-walls-convertcurve.png" | prepend: site.baseurl }})

### 墙体截面

使用 {% include ltr/comp.html uuid="7ce0bd56-" %} 运算器也可以提取基础墙与叠层墙图元的界面取消， 请注意这些截面曲线会沿着中心平面提取：

![]({{ "/static/images/guides/revit-walls-profile.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-walls-profilelines.png" | prepend: site.baseurl }})

### 墙体几何

你可以通过 {% include ltr/comp.html uuid="b3bcbf5b-" %} 运算器来获取一个墙体实例的基础几何内容：

![]({{ "/static/images/guides/revit-walls-geometry.png" | prepend: site.baseurl }})

### 墙体几何结构

{% capture api_note %}
通常在 Revit API 中任何一个图元的几何信息都可以使用`DB.Element.Geometry` p来提取，对于墙而言，提取的几何信息并不包括墙体结构层， 这里提供一些临时的快速方法。 浏览  [different method described here](https://thebuildingcoder.typepad.com/blog/2011/10/retrieving-detailed-wall-layer-geometry.html)了解提取几何图层信息的更多方法。
{% endcapture %}
{% include ltr/warning_note.html note=api_note %}

 {% include ltr/comp.html uuid="3dbaaae8-" %} 运算器可以用来提取一个基础墙的图层几何信息:

![]({{ "/static/images/guides/revit-walls-walllayers.png" | prepend: site.baseurl }})

这个运算器也可以作业于叠层墙，它可以提取给定叠层墙的所有基础墙的结构图层信息：

![]({{ "/static/images/guides/revit-walls-stackedwalllayers.png" | prepend: site.baseurl }})

比较推荐的方法是先从叠层墙抽离基础墙，然后再充基础墙中提取其图层几何图形，这个方法所得到的数据结构能更吻合其图层顺序结构：

![]({{ "/static/images/guides/revit-walls-everybasicwall.png" | prepend: site.baseurl }})

为了让图层中提取的几何列表于处理图层的其他运算器保持相同顺序，你可以依据墙体方向向量的距离对几何列表进行排序，这个方法最适合平台的墙体， 也可以使用类似的方法对其他墙体几何进行排序（想象下堆叠在不同结构的叠层墙实例中的基本墙体）：

![]({{ "/static/images/guides/revit-walls-layersinorder.png" | prepend: site.baseurl }})

&nbsp;

![]({{ "/static/images/guides/revit-walls-layersinorder.gif" | prepend: site.baseurl }})

### 修改截面

{% capture api_note %}
当前版本 Revit API 中并不支持修改墙体截面曲线
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### 创建叠层墙类型

{% include ltr/warning_note.html note='当前版本 Revit API 并不支持创建叠层墙类型' %}

## 创建墙体

### 基于基线

使用 {% include ltr/comp.html uuid='37a8c46f' %} 运算器可以基于给定的曲线创建新的墙体， 下图中{% include ltr/comp.html uuid='ef607c2a' %}运算器用来从 Revit 模型中抓取一些直线：

![]({{ "/static/images/guides/revit-walls-bycurve.png" | prepend: site.baseurl }})

### 基于截面

使用 {% include ltr/comp.html uuid='78b02ae8' %}运算器基于给定的截面线来创建新的墙体，请注意截面线必须是封闭且与工作平面垂直的平面曲线，下图中使用了 **Join Curve** 运算器来组合拾取到的曲线， {% include ltr/comp.html uuid='ef607c2a' %} 运算器从 Revit 中抓取了一些直线， 这些直线都是与当前工作平面垂直且是处于同一个平面上：

![]({{ "/static/images/guides/revit-walls-byprofile.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-walls-byprofilescap.png" | prepend: site.baseurl }})

<!-- https://github.com/mcneel/rhino.inside-revit/issues/46 -->