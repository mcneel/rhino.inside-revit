---
title: "Revit: Elements & Instances"
subtitle: What is an Element in Revit
order: 21
group: Essentials
home: true
thumbnail: /static/images/guides/revit-elements.png
---

{% capture link_note %}
这里会介绍如何手动参照一个指定的图元且将其引入 Grasshopper 定义文件，后面的部分会讨论如何收集各种不同类型的图元的方法。要查看 Revit 图元和实例组织方式，请查看 [Revit: Elements & Instances]({{ site.baseurl }}{% link _en/1.0/guides/revit-revit.md %}#elements--instances) 概叙.
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-elements.png' %}

## 参照图元

有很多方法选择各种类型的 Revit 图元，在本章我们将会讲解如何手动指定一个图元且将其参照至你的 Grasshopper 脚本文件中， 后面的章节详细的讲解更多的参照图元的方法。

### 以选择的方式

鼠标右击点击运算器 {% include ltr/comp.html uuid='ef607c2a' %} 参数，然后从 Revit 中选择你所需要的图元至 Grasshopper 定义文件,

![]({{ "/static/images/guides/revit-elements-select.gif" | prepend: site.baseurl }})

### 以图元ID号方式

从 Revit 中读取图元的ID号，然后鼠标邮件点击 {% include ltr/comp.html uuid='f3ea4a9c' %} 运算器，将读取到的ID号填入至 Manage Revit Element Collection 即可

![]({{ "/static/images/guides/revit-elements-byid.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-elements-byid.gif" | prepend: site.baseurl }})

## 实例

实例是放置在 Revit 中单个图像/几何图元，例如单个的墙或门，或其他类型的单一图元，作为 Revit 图元的子集， 实例会从其他的类别与类型继承一系列的参数，也可能包含仅限于当前实例特有的一些属性参数

## 查询筛选

Grasshopper 可以通过筛选运算器依据多种不同的属性选择特定的 Revit 图元，也可以组合多个不同的过滤器来进行一些复杂的挑选.

通常会建立过滤器然后将其发送到 Grasshopper 中 [Query Components]({{ site.baseurl }}{% link _en/1.0/guides/rir-grasshopper.md%}#revit-aware-components) 的某一个

![]({{ "/static/images/guides/filter-basic.png" | prepend: site.baseurl }})

### 类别筛选

以选择类别的方式筛选所有的选择对象

![]({{ "/static/images/guides/filter-category.png" | prepend: site.baseurl }})

### 类型筛选

以选择类型的方式筛选所有的选择对象，将运算器 {% include ltr/comp.html uuid='af9d949f' %} 连接至 {% include ltr/comp.html uuid="d3fb53d3-9" %}、{% include ltr/comp.html uuid='4434c470' %} 与 {% include ltr/comp.html uuid='0f7da57e' %} 来查询所有选择实例中的类型，下面示范如何从选择实例中筛选指定类型的窗户

![]({{ "/static/images/guides/filter-type.png" | prepend: site.baseurl }})

### 参数值筛选

你可以使用 {% include ltr/comp.html uuid='e6a1f501' %} 运算器与筛选规则 (例如 {% include ltr/comp.html uuid='05bbaedd' %} 或 {% include ltr/comp.html uuid='0f9139ac' %} ) 结合使用，按照图元的参数值来筛选所需要图元，

![]({{ "/static/images/guides/revit-elements-querybyparam.png" | prepend: site.baseurl }})

运算器 {% include ltr/comp.html uuid='e6a1f501' %}用来从所选图元中筛选指定的参数值， [Filter Rules](#filter-rules) 是对指定参数执行类型的比较， 要注意, Revit 中的参数名与类型列表非常长。 {% include ltr/comp.html uuid='c1d96f56' %} 是最好的参数筛选方式，它还会在选择器中列举出该参数的附加详细信息。

![]({{ "/static/images/guides/parameter-rule-filter.png" | prepend: site.baseurl }})

#### 筛选规则

筛选规则可以使用  {% include ltr/comp.html uuid='e6a1f501' %} 运算器进行参数值对比，下面这个范例演示了如何使用一个图元中的参数值来查找模型中具有相同参数值的所有其他图元：

![]({{ "/static/images/guides/parameter-rule-element.png" | prepend: site.baseurl }})

### 楼层筛选

可以使用{% include ltr/comp.html uuid="b534489b-" %} 运算器以楼层的方式筛选项目图元

![]({{ "/static/images/guides/revit-level-filters.png" | prepend: site.baseurl }})

如果需要指定楼层来筛选图元，最好是使用{% include ltr/comp.html uuid='bd6a74f3' %} 运算器

![]({{ "/static/images/guides/filter-level.png" | prepend: site.baseurl }})

### API Class 筛选

还可以筛选 [Revit API Class names](https://www.revitapidocs.com/2015/eb16114f-69ea-f4de-0d0d-f7388b105a16.htm) 的方式在项目中过滤图元，这样既可以输入{% include ltr/comp.html uuid='f432d672' %} 进行选择也可以类别字串符，

![]({{ "/static/images/guides/filter-class.png" | prepend: site.baseurl }})

## 更多查询筛选工具

### Filter Element

**Filter Element** 运算器会读取之前筛选的 Revit 图元，与筛选匹配图元进行对比然后返回一个是否匹配的 Ture/False 的列表。 例如一组在 {% include ltr/comp.html uuid='ef607c2a' %} 中预先筛选的图元，可以使用 {% include ltr/comp.html uuid='af9d949f' %}  输入至 {% include ltr/comp.html uuid='36180a9e' %} 中进行筛选， The {% include ltr/comp.html uuid='36180a9e' %} 运算器将会返沪一个包含 True/False 的列表，这样可以使用 Cull Pattern 运算器对原始图元进行筛选：

![]({{ "/static/images/guides/filter-elements.png" | prepend: site.baseurl }})

### Exclusion Filter

{% include ltr/comp.html uuid='396f7e91' %} 运算器可以从查询结果中排除一组特定的图元，下面这个范例，示范如何从查询的所有墙体中剔除两个手动选择的墙：

![]({{ "/static/images/guides/filter-exclude-elements.png" | prepend: site.baseurl }})

### Exclude Types Filter

{% include ltr/comp.html uuid='f69d485f' %} 运算器可以从一个列表中排除筛选某些类型，它是利用 *[WhereElementIsNotElementType](https://www.revitapidocs.com/2015/061cbbb9-26f1-a8f8-a4b2-3d7ff0105199.htm)*的方式来实现。

### Bounding Box Filter

{% include ltr/comp.html uuid='3b8be676' %} 是使用几何边框匹配Revit图元的方式来进行筛选，初始的几何对象既可以来自 Rhino 也可以是 Revit, 输入参数包括：

* Bounding Box (Geometry) - 用于查询边框方块对齐世界坐标
* Union (Boolean) -  是否合并所有的目标边框方块
* Strict (Boolean) - 是否严格的包含图元
* Tolerance (Number) - 查询容差范围
* Inverted (Boolean) - 是否需要反转查询结果

### Intersects Brep Filter

{% include ltr/comp.html uuid='a8889824' %} 用于筛选与 NURBS Brep相交的图元

### Intersects Element

{% include ltr/comp.html uuid='d1e4c98d' %} 用于筛选与其他图元相交的图元

### Intersects Mesh Filter

{% include ltr/comp.html uuid='09f9e451' %} 用于筛选与Rhino网格对象相交的图元

### Design Option Filter

{% include ltr/comp.html uuid='1b197e82' %} 用于匹配属于指定设计选项的 Revit 图元

### Owner View Filter

{% include ltr/comp.html uuid='cfb42d90' %} 用于筛选隶属于某一些指定视图的图元， 这个运算器通常与从模型返回视图选择器搭配使用，

![]({{ "/static/images/guides/filter-view.png" | prepend: site.baseurl }})

### Phase Status Filter

{% include ltr/comp.html uuid='805c21ee' %} 用于匹配给定阶段窗台关联的元素， 可以使用鼠标右键找到阶段与状态

![]({{ "/static/images/guides/filter-phase.png" | prepend: site.baseurl }})

### 可选择的 View Filter

{% include ltr/comp.html uuid='ac546f16' %} 用于将可选元素匹配到给定视图中

## 组合查询筛选

### Logical And Filter

可以使用 {% include ltr/comp.html uuid='0e534afb' %}来合并所有的筛选工具，所有的图元都必须被筛选，

![]({{ "/static/images/guides/filter-and.png" | prepend: site.baseurl }})

注意，可以使用放大的方式增加更多的筛选输入

### Logical Or Filter

使用 {% include ltr/comp.html uuid='3804757f' %} 群组多个筛选工具，图元可以从输入的任何一个筛选工具进行过滤

## 保存筛选或选择

### Add Parameter Filter

{% include ltr/comp.html uuid='01e86d7c' %} 可以Revit 模型中参建立基于 数的筛选工具，也可以当作 Grasshopper 中的参数过滤工具来使用

### Add Selection Filter

{% include ltr/comp.html uuid='29618f71' %} 可以在Revit模型中创建一个选择过滤器，也可以同时在 Grasshopper 脚本文件中使用

## 提取实例几何

{% include ltr/comp.html uuid='b3bcbf5b' %} 可以提取实例中的几何，例如下图中示范如何将一个 Stacked Wall 实例中的几何体完全的提取, {% include ltr/comp.html uuid='b078e48a' %} 用来指定提取物件位于具体的楼层：

![]({{ "/static/images/guides/revit-elements-getgeom.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-elements-getgeomscap.png" | prepend: site.baseurl }})

### Instance Base Curve

基于基准线（例如 Basic Walls) 创建的图元你可以使用 {% include ltr/comp.html uuid='dcc82eca' %} 来获取或设置基准线

![]({{ "/static/images/guides/revit-elements-basecurve.png" | prepend: site.baseurl }})

### Instance Bounding Box

通过 Grasshopper Box 运算器轻而易举的可以获取一个实例几何体的边框方块：

![]({{ "/static/images/guides/revit-elements-getbbox.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-elements-getbboxscap.png" | prepend: site.baseurl }})

### Instance Bounding Geometry

{% include ltr/warning_note.html note='当前 Bounding Geometry 运算器仅对墙体起作业，未来会扩展至更多的 Revit 类别' %}

很多时候回需要提取一个实例的边框几何（Bounding Geometry), 它会尽力包裹实例且会遵循实例的几何拓扑，你可以使用 {% include ltr/comp.html uuid='3396dbc4' %} 运算器来获取它，例如下面示范如何抽离一个 Stacked Wall 的边框几何， 请注意抽离出来的几何物件与 Stacked Wall 一样厚：

![]({{ "/static/images/guides/revit-elements-getboundinggeom.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-elements-getboundinggeomscap.png" | prepend: site.baseurl }})

#### 调整实例类型

使用 {% include ltr/comp.html uuid='fe427d04' %} 运算器既可以查询一个实例的类型也可以将修改为其他类型，

![]({{ "/static/images/guides/revit-elements-gettype.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-elements-changetype.png" | prepend: site.baseurl }})

## 放置实例类型

使用 {% include ltr/comp.html uuid='0c642d7d' %} 运算器可以将一个类型的实例放置至 Revit 模型空间，

![]({{ "/static/images/guides/revit-elements-placeinst.png" | prepend: site.baseurl }})

对于需要指定依附主体（输入参数 Host）的类型，可以使用 {% include ltr/comp.html uuid='0c642d7d' %} 运算器来实现

![]({{ "/static/images/guides/revit-elements-placeinstonhost.png" | prepend: site.baseurl }})

它会将给定的类型尽可能的靠近其依附主体，例如下图中绿色球体是给定窗户原来的实际位置，当指定墙体为窗户的依附主体时，它会将窗户放置在距离绿色球体最近的墙体上

![]({{ "/static/images/guides/revit-elements-placeinstonhostscap.png" | prepend: site.baseurl }})
