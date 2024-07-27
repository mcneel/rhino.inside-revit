---
title: Materials
order: 50
group: Modeling
home: true
thumbnail: /static/images/guides/revit-materials.png
subtitle: Workflows for Revit Materials, Material Graphics and Assets
ghdef: revit-materials.ghx
---

Revit 中的材质是一种比较复杂的数据类型，通常被用于 :

- 赋予给Revit图元的图形属性（例如浴室墙壁上的瓷砖图案），
- 模型中嵌入建筑施工信息以便于不同阶段读取使用，
- 建筑可视化的建筑外表面显示属性，
- 用于各种模拟分析的物理与热度属性，

因从 Revit 中的每一个材质都会具备下面5个主要的特性：

- Identity 标识
- Graphics 图形
- Appearance Properties 外观
- Physical Properties 物理
- Thermal Properties 热度

在 Revit 材质编辑器窗口中有对应的选项卡：

![]({{ "/static/images/guides/revit-materials-editortabs.png" | prepend: site.baseurl }})

在 Rhino.Inside.Revit 中我们也提供了对应的5个主要的运算器:

- {% include ltr/comp.html uuid='222b42df' %}
- {% include ltr/comp.html uuid='8c5cd6fb' %}
- {% include ltr/comp.html uuid='5b18389b' %}
- {% include ltr/comp.html uuid='ec93f8e0' %}
- {% include ltr/comp.html uuid='c3be363d' %}

这些运算器如下图所示。The {% include ltr/comp.html uuid='1f644064' %} 运算器用于提取材质的相关特性（外观物理与热度），且可以使用上面列出的运算器对每一项特性进行深入的分析，展开 [Material Assets](#material-assets) 章节了解更多的相关内容：

![]({{ "/static/images/guides/gh-material-parts.png" | prepend: site.baseurl }})

在下面的章节我们将会讨论如何使用 {{ site.terms.rir }} 来处理这5个方面的相关问题

{% include youtube_player.html id="8CdhieEi6Os" %}

## 查询材质

{% capture api_note %}
在 Revit API 中使用 {% include api_type.html type='Autodesk.Revit.DB.Material' title='DB.Material' %}来表示材质，这个类别用来处理材料的标识与图形，且提供查询与修改外观、物理与热度属性的方法。
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

第一个挑战是能在一个模型中查询可用的材质或是找到一个想要使用的材质，通常建议使用  {% include ltr/comp.html uuid='94af13c1-' %} 运算器， 默认情况下这个运算器能找到且输出一个模型中所有的材质，也可以选择性的输入不同的过滤方式、名称与类的方式来过滤现有材质，也可以自定义更多的过滤方法:

![]({{ "/static/images/guides/revit-materials-query.png" | prepend: site.baseurl }})

{% include ltr/filter_note.html note='在 Class 与 Name 输入项中还可以使用字串来进行过滤定义' %}

![]({{ "/static/images/guides/revit-materials-filter.png" | prepend: site.baseurl }})

## 材质标识

可以使用 {% include ltr/comp.html uuid='222b42df-' %} 运算器来获取材质标识：

![]({{ "/static/images/guides/revit-materials-id.png" | prepend: site.baseurl }})

也可以它来修改一个材质的标识属性：

![]({{ "/static/images/guides/revit-materials-id-mod.png" | prepend: site.baseurl }})

## 材质图形

使用 {% include ltr/comp.html uuid='8c5cd6fb-' %} 运算器来获取材质图形：

![]({{ "/static/images/guides/revit-materials-graphics.png" | prepend: site.baseurl }})

你也可以使用相同的运算器来调整图形属性，请浏览  [Styles and Patterns]({{ site.baseurl }}{% link _en/1.0/guides/revit-styles.md %}#find-specific-fill-pattern) 页面了解  **Find Fill Pattern** 运算器:

![]({{ "/static/images/guides/revit-materials-graphics-mod.png" | prepend: site.baseurl }})

## 创建材质

在Revit 模型中使用 {% include ltr/comp.html uuid='3aedba3c-' %} 文档感知运算器来创建一个新的材质，记住要为新建材给定一个唯一的名字：

![]({{ "/static/images/guides/revit-materials-create.png" | prepend: site.baseurl }})

然后可以继续使用 {% include ltr/comp.html uuid='8c5cd6fb-' %}, {% include ltr/comp.html uuid='222b42df-' %}, 或 {% include ltr/comp.html uuid='2f1ec561-' %} 来为新的材质定义图形属性等：

![]({{ "/static/images/guides/revit-materials-create-color.png" | prepend: site.baseurl }})

## 材质资产

前面介绍了如何分析材质标识与图形，如何创建简易材质，如果要充分的掌握 Revit 中的材质，还需要进一步了解材质背后的其他三个基本概念：外观、物理与热度。

### 资产

**资产** 是 Revit 中材质的外观、物理与热度方面背后的基本概念， {{ site.terms.rir }} 也提供了一些对应的运算器以Grasshopper 的方式来创建、修改与分析这些**资产** ，也提供了一些用于提取与替换一个材质中的这些内容的运算器。

请注意**资产**与**材质**是不同的数据类型，每一个 Revit 的材质都包含标识与图形属性，也会赋予用于**外观** 、**物理** 与**热度** 等资产至材质，**热度资产** 是完全可以选的。

{% capture api_note %}
Revitalizing API 对资产的支持非常有限，我们尝试在这个章节描述清楚 Revit Visual API 的内部工作原理

&nbsp;

**外观资产**

所有的外观资产都是{% include api_type.html type='Autodesk.Revit.DB.Visual.Asset' title='DB.Visual.Asset' %} 类且是一个带有一个名称，例如 `generic_diffuse`, 一个类型和一个值的视觉属性的集合。 {% include api_type.html type='Autodesk.Revit.DB.Visual.Asset' title='DB.Visual.Asset' %} 具有查找和返回这些属性的方法，这些属性都经由 Revit API 中的 {% include api_type.html type='Autodesk.Revit.DB.Visual.AssetProperty' title='DB.Visual.AssetProperty' %}类封装，这个类型也支持从属性中提取值。

&nbsp;

Revit 中有很多不同的外观资产, 例如 **Generic** , **Ceramic** , **Metal** , **Layered** , **Glazing** 等，每个资产都有一个不同的属性设置。要使用这些外观资产，我们就需要一种方法来了解每种资产类型可用的属性名称。 Revit API 提供具有静态只读字符串属性的静态类，这些属性提供了一种简单的方法(?)来获取这些属性的名称, 例如 `GenericDiffuse` 的 {% include api_type.html type='Autodesk.Revit.DB.Visual.Generic' title='DB.Visual.Generic' %}, 属性返回名称 `generic_diffuse` 它是 Generic 财产的漫反射属性的名称。

&nbsp;

外观资产由 {% include api_type.html type='Autodesk.Revit.DB.AppearanceAssetElement' title='DB.AppearanceAssetElement' %} 封装，以便可以将它们赋予给一个材质({% include api_type.html type='Autodesk.Revit.DB.Material' title='DB.Material' %})

&nbsp;

**物理与热度资产**

物理资产与热度资产完全不同，经过其运作方式都和外观资产非常相似，它们依然是属性的集合，这些属性都会作为 Revit 参数 ({% include api_type.html type='Autodesk.Revit.DB.Parameter' title='DB.Parameter' %}) 模型且由 {% include api_type.html type='Autodesk.Revit.DB.PropertySetElement' title='DB.PropertySetElement' %}. 实例收集, 不是将静态类作为名称的访问器，而是必须通过内置的 Revit 参数来访问，例如 `THERMAL_MATERIAL_PARAM_REFLECTIVITY` 的 {% include api_type.html type='Autodesk.Revit.DB.BuiltInParameter' title='DB.BuiltInParameter' %}

&nbsp;

Revit API 提供 {% include api_type.html type='Autodesk.Revit.DB.StructuralAsset' title='DB.StructuralAsset' %} 与 {% include api_type.html type='Autodesk.Revit.DB.ThermalAsset' title='DB.ThermalAsset' %} 类以提供对物理与热属性的快速访问，但并非所有属性都包含在这些类型中，而且也不会检查属性值的有效性。

&nbsp;

**Grasshopper as Playground**

这里提供包含 Python 的 Grasshopper 定义文件， 可以帮助你查询这些资产的属性：

&nbsp;

![]({{ "/static/images/guides/revit-materials-assetpg.png" | prepend: site.baseurl }})

&nbsp;

{% include ltr/download_def.html archive='/static/ghdefs/AssetsPlayground.ghx' name='Assets Playground' %}

{% endcapture %}
{% include ltr/api_note.html note=api_note %}

使用 {% include ltr/comp.html uuid='1f644064-' %} 来提取一个材质的资产：

![]({{ "/static/images/guides/revit-materials-explode.png" | prepend: site.baseurl }})

如果要将材料资产替换为其他不同的资产，请使用 {% include ltr/comp.html uuid='2f1ec561-' %} 运算器:

![]({{ "/static/images/guides/revit-materials-replace.png" | prepend: site.baseurl }})

## 外观资产

外观资产在其他一些产品中叫渲染材质，Rhino.Inside.Revit 中 Grasshopper 提供许多运算器来处理外观资产,

{% include youtube_player.html id="0fZVXCWRPr0" %}

Revit API 中有很多外观资产，例如你可以使用 {% include ltr/comp.html uuid='0f251f87-' %} 来创建一个 Generic 外观资产且通过 {% include ltr/comp.html uuid='2f1ec561-' %} 赋予给一个 Revit 材质：

![]({{ "/static/images/guides/revit-materials-appasset-create.png" | prepend: site.baseurl }})

{% include ltr/comp.html uuid='5b18389b-' %} 与 {% include ltr/comp.html uuid='73b2376b-' %} 可以用于简化一个当前资产，或分析与提取其已知属性值：

![]({{ "/static/images/guides/revit-materials-appasset-mod.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-materials-appasset-analyze.png" | prepend: site.baseurl }})

## 纹理资产

外观资产具有一系列可以接受嵌套资产（本指南把它叫纹理资产）的属性，例如一个 Generic 外观资产既然可以包含一个颜色值，也可以是链接至另外一中位图类型的资产（或其他纹理资产）。

{{ site.terms.rir }} 提供一些运算器来构建与拆解这些资产类型，外观资产也可以接受适当的纹理资产，例如使用 {% include ltr/comp.html uuid='37b63660-' %} 与 {% include ltr/comp.html uuid='77b391db-' %}  来构造与拆解**位图**资产

![]({{ "/static/images/guides/revit-materials-appasset-create-texture.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-materials-appasset-create-texture.gif" | prepend: site.baseurl }})

{% capture param_note %}
请注意 Generic 外观运算器的 {% include ltr/misc.html uuid='49a94c44-' title='Glossiness' %} 与 {% include ltr/misc.html uuid='c2fc2e60-' title='Bump' %}参数分别接受浮动值与颜色值或纹理贴图， 请注意显示浮点值或颜色值与棋盘格图的参数图标
{% endcapture %}
{% include ltr/bubble_note.html note=param_note %}

{% capture construct_note %}
请注意 构建与拆解纹理运算器仅传递包含纹理资源的数据结构，并不会在 Revit 模型中建立任何内容，实际上创建纹理资源（当接入输入参数时）且将其链接到正在创建外观资源属性的是 Create Appearance Asset 运算器，这个行为反映了 Revit API 中‘链接’（嵌套）资源的内部工作方式。
{% endcapture %}
{% include ltr/api_note.html note=construct_note %}

## 物理资产

使用 {% include ltr/comp.html uuid='af2678c8-' %} 文档感知运算器来创建物理资产且使用 {% include ltr/comp.html uuid='2f1ec561-' %} 运算器赋予给一个材质，使用 {% include ltr/comp.html uuid='6f5d09c7-' %} 与 {% include ltr/comp.html uuid='c907b51e-' %} 作为输入，然后分别设置物理资产的类型与行为：

![]({{ "/static/images/guides/revit-materials-phyasset-create.png" | prepend: site.baseurl }})

和前面讨论的修改与分析运算器类似 {% include ltr/comp.html uuid='ec93f8e0-' %} 与 {% include ltr/comp.html uuid='67a74d31-' %} 运算器可以用来修改与分析当前物理资产

## 热度资产

使用 {% include ltr/comp.html uuid='bd9164c4-' %} 文档感知运算器来创建热度资产且使用 {% include ltr/comp.html uuid='2f1ec561-' %} 运算器将其赋予给材质，使用 {% include ltr/comp.html uuid='9d9d0211-' %} 与 {% include ltr/comp.html uuid='c907b51e-' %} 作为输入，分别设置热度资产的类型与行为：

![]({{ "/static/images/guides/revit-materials-therasset-create.png" | prepend: site.baseurl }})

类似前面讨论的修改与分析运算器，你可以使用{% include ltr/comp.html uuid='c3be363d-' %} 与 {% include ltr/comp.html uuid='2c8f541a-' %} 来修改与分析当前的热度资产

## 填色

使用 {% include ltr/comp.html uuid="2a4a95d5" %} 运算器可以将一个材质赋予给一个 Revit 的面对象

![]({{ "/static/images/guides/revit-element-facePaint.png" | prepend: site.baseurl }})