---
title: "Revit: Types & Families"
subtitle: Revit's categorization system, Categories, Types, ...
order: 23
group: Essentials
home: true
thumbnail: /static/images/guides/revit-types.png
---

{% capture link_note %}
在与 Revit 或 Revit API 打交道时我们时常需要处理 Revit 的**类型**与自**定义族** , 在这个章节将向你展示如何通过不同的 Grasshopper 运算器来查询与创建不同的**类型** 与**族** 。如果想了解如何在 Revit 中组织图元，请浏览  [Revit: Types & Families]({{ site.baseurl }}{% link _en/1.0/guides/revit-revit.md %}#categories-families--types)
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-types.png' %}

## 查询类型

如果你想统计一个 Revit 族中类型的情况，可以组合一组类别拾取运算器，例如 {% include ltr/comp.html uuid="af9d949f-" %} + {% include ltr/comp.html uuid="d08f7ab1-" %} + {% include ltr/comp.html uuid="7b00f940-" %} 等运算器：

![]({{ "/static/images/guides/revit-families01.png" | prepend: site.baseurl }})

还可以给 {% include ltr/comp.html uuid="7b00f940-" %} 运算器增加更多筛选条件：

![]({{ "/static/images/guides/revit-families02.png" | prepend: site.baseurl }})

### 查询类型信息

使用 {% include ltr/comp.html uuid='7dea1ba3' %} 运算器来查询 Revit 类型信息，但要注意**族名称**参数，对于**系统类型**与**自定义族**名称下的自定义类型查询会返回**系统族名称** :

![]({{ "/static/images/guides/revit-families02a.png" | prepend: site.baseurl }})

## 访问一个类型的族

当查询 一个 Revit 模型中的自定义类型时，我们可以找到包含每种类型逻辑的自定义族定义，使用 {% include ltr/comp.html uuid="742836d7" %} 运算器来获取该组件中的每种类型的系列，

![]({{ "/static/images/guides/revit-families03.png" | prepend: site.baseurl }})

**注意** ，如果被查询的族类型没有关联自定义族定义， {% include ltr/comp.html uuid="742836d7" %} 运算器会返回一个 `null` 值

![]({{ "/static/images/guides/revit-families04.png" | prepend: site.baseurl }})

## 选择特定类型

{% include ltr/comp.html uuid="af9d949f-" %} 可以选择一个特定的模型类别，例如 墙（Walls),

将其连接至{% include ltr/comp.html uuid="d3fb53d3-9" %} 运算器，然后从族中选择一个指定的类型：

![]({{ "/static/images/guides/revit-families05.png" | prepend: site.baseurl }})

## 确定默认类型

当启动构建工具 （例如放置一扇门），Revit 会自动选择上一次使用的指定类别（例如使用放置门工具所选择的门），称之为该类别的**默认类型** ，这对于使用 API 来创建图元很有帮助, 使用 {% include ltr/comp.html uuid='d67b341f' %} 运算器来查看选定类别的默认类型：

![]({{ "/static/images/guides/revit-types-defaulttype.png" | prepend: site.baseurl }})

这个运算器在当前这个例子中会返回默认的 `DB.FamilySymbol` ：

![]({{ "/static/images/guides/revit-types-defaultsymbol.png" | prepend: site.baseurl }})

## 修改类型

一旦你筛选到需要的类型，你就可以查询其相关参数且可以修改参数。浏览  [Document Model: Parameters]({{ site.baseurl }}{% link _en/1.0/guides/revit-params.md %}) 了解如何编辑一个图元的参数. 图参数运算器也适用于图元类型。

![]({{ "/static/images/guides/revit-families06.png" | prepend: site.baseurl }})

## 提取类别几何

一旦你筛选到需要的类型，你就可以使用 {% include ltr/comp.html uuid="b3bcbf5b-" %} 来提取图元的几何图形，运算器 {% include ltr/comp.html uuid="b078e48a-" %} 很容易为参数输入正确的 LOD值 

![]({{ "/static/images/guides/revit-families07.png" | prepend: site.baseurl }})

{% include ltr/comp.html uuid="b3bcbf5b-" %} 运算器也会自动的在 Rhino 视窗中预览几何图形

![]({{ "/static/images/guides/revit-families08.png" | prepend: site.baseurl }})

## 按照类别提取几何图形

<!-- https://github.com/mcneel/rhino.inside-revit/issues/93 -->

从族实例内按照类别来提取几何图形是个比较常见的操作，这里分享一个专用的工具 **Element Geometry By SubCategory** 运算器， 还可以利用它抓取到族内子类别的定义信息，范例如下图所示，

![]({{ "/static/images/guides/revit-families08a.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-families08b.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Element Geometry By SubCategory.ghuser' name='Element Geometry By SubCategory' %}

## 创建新的类型

你也可以基于现有的类型来建立新的类型，例如利用 {% include ltr/comp.html uuid="5ed7e612-" %} 运算器复制一个现有的类型，然后赋予新的名字，调整相关属性值，

![]({{ "/static/images/guides/revit-families09.png" | prepend: site.baseurl }})

Revit 项目浏览器会在族下面显示新的类型

## 移除类型

如果需要移除类型请使用{% include ltr/comp.html uuid="3ffc2cb2-" %} 运算器， 请注意被删除的类型也会连同其相关实例一起被删除，如果你不希望实例也被删除，请在删除之前找到这些实例，然后修改为其他类型

![]({{ "/static/images/guides/revit-families09d.png" | prepend: site.baseurl }})

## 导入族

你可以使用 {% include ltr/comp.html uuid="0e244846-" %} 运算器导入一个新的族文件至模型

![]({{ "/static/images/guides/revit-families10.png" | prepend: site.baseurl }})

Revit 浏览器现在会在族下面列出新的族*

## 保存族

使用 {% include ltr/comp.html uuid="c2b9b045-" %} 运算器将一个族保存至外部文件

![]({{ "/static/images/guides/revit-families11.png" | prepend: site.baseurl }})

## 新建族

在当前的 {{ site.terms.rir }} 中，可以使用 {% include ltr/comp.html uuid='82523911' %} 运算器建立新的 Revit 族或是将新的几何物件加入至某个选定的族中，请注意，运算器需要赋予正确的模板文件，

![]({{ "/static/images/guides/revit-families12.png" | prepend: site.baseurl }})

Revit 浏览器现在会在族下面列出新的族

你也可以继续给 {% include ltr/comp.html uuid='82523911' %} 运算器接入 **Generic Model** 模板，且使用 {% include ltr/comp.html uuid="af9d949f-" %} 运算器手动设置类别

![]({{ "/static/images/guides/revit-families13.png" | prepend: site.baseurl }})

可以利用位于 Revit > Family 面板下的一些运算器来帮助我们建立用于Revit 族的几何

- {% include ltr/comp.html uuid='8a51d5a1' %}
- {% include ltr/comp.html uuid='f0887ad5' %}
- {% include ltr/comp.html uuid='6fbb9200' %}
- {% include ltr/comp.html uuid='72fdc627' %}

上面这些运算器可以设置构成新的族的图元的可见性、子类别与材质。更多介绍请浏览 [Rhino Objects as Loadable Families guide]({{ site.baseurl }}{% link _en/1.0/guides/rhino-to-revit.md%}#rhino-objects-as-loadable-families).

![Creating subcategory]({{ "/static/images/guides/subcategory-rhino-revit-gh.png" | prepend: site.baseurl }})

如上面的范例，你可以使用 {% include ltr/comp.html uuid='10ea29d4' %} 运算器来控制待创建几何的可见性，这个运算器提供了所有原生控制Revit族几何物件可见性/图形编辑的选项

![]({{ "/static/images/guides/revit-families15.png" | prepend: site.baseurl }})

## 编辑族

你也可以使用 {% include ltr/comp.html uuid='82523911' %}  运算器类编辑当前的族，只需要接入合适的模板与族名称、新的几何，而且 {% include ltr/comp.html uuid='82523911' %} 运算器会自动找到当前的族，然后代替相关内容且重新导入族只 Revit 模型中，请注意，*OverrideFamily* 需要设置为`True` 且 *OverrideParameters* 设置为根据需要覆盖族参数

![]({{ "/static/images/guides/revit-families16.png" | prepend: site.baseurl }})

## 建立新的族模板

Revit 附带一系列 Revit 族模板 (.RFT) 文件。这些包含为特定类别创建可载入族所需的基本默认参数。有趣的是，许多最受欢迎的类别都没有为它们构建的模板。这些类别的示例包括墙壁、屋顶、地板、窗户、HardScape 等。按照以下步骤，可以在这些常用的类别中创建族模板并创建可载入族。 [这个电子表格](https://docs.google.com/spreadsheets/d/1l8koAQtsz0o9iK80gmpC0HqSAFqpqAb0rKYdXcQkBWE/edit?usp=sharing) 中列出了支持客户 RFT 文件的默认模板和类别列表。

{% include vimeo_player.html id="726191224" %}

综上所述，创建一个新的族模板步骤如下：

1. 利用所需的类别创建一个就地运算器，例如楼梯，
2. 绘制一个对象，
3. 群组这些对象，
4. 右键点击 Group 且 Save Group,
5. 保存文件为 RFA,
6. 开启 FRA 文件且删除里面的所有物件，
7. 保存文件，
8. 在文件浏览器中重新命名这个 RFA 文件为 RFT 文件，
9. 在新的运算器族中使用这个 RFT 文件来建立一个新的族。
