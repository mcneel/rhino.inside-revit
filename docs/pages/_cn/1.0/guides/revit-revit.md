---
title: "Overview"
subtitle: Understanding Revit's data model
order: 10
thumbnail: /static/images/guides/revit-revit.png
group: Essentials
---

首先需要了解 Revit 如何创建与储存建筑信息， 如果你准备利用 Revit 建立与管理数据这是非常关键的一步。在本指南我们会详细讲解 Revit 数据模式，也会介绍如何利用 Grasshopper 中的专用 Revit 运算器组来处理数据模型。

## 图元的 DNA

下面这个图片显示了单一{% include ltr/misc.html uuid='11f05ff5' title="Revit Element" %}**Revit 图元**的DNA，它就像一台小设备，输入数据、处理数据、建立几何物件与数据然后再输出它们。请注意，并不是所有的图元都包含几何信息，有一些图元仅仅只是记录信息。

![]({{ "/static/images/guides/revit-revit-element-dna.svg" | prepend: site.baseurl }})

因此，实际上我们是将类型与族实例信息输入至族函数中，用以生成图元元数据（包括计算属性）与几何体。请注意，我们在 Revit **族** 定义中所提供的数据、族**类型** 、族**实例** 参数与系列逻辑一同用于生成 BIM 数据结构。

![]({{ "/static/images/guides/revit-revit-element-func.svg" | prepend: site.baseurl }})

然后所有建立的图元都会存储在一个 Revit **文档**中，它们也由一系列的**容器（Container）** 组成，每个容器都有各自的用途。

![]({{ "/static/images/guides/revit-revit-containers.svg" | prepend: site.baseurl }})

顺便介绍一下**子类** （**Subcategories** ), 看名字好像是**类** 的下一级组织，实际上建议把它当作几何属性而不是组织层级。当一个**族** 函数生成几何时，它可以将它们分组为主要类别的子类别，这样就能更好的控制几何图形中的各部分图形。

## 图元与实例

常常有被问到，**到底什么是图元** ？**图元** 是 Revit 数字模型中的基本构建图块，**图元** 被组织成**类别** ，**类别** 列表内置在每个 Revit 版本内且不能修改。 **图元都**带有 [参数]({{ site.baseurl }}{% link _en/1.0/guides/revit-params.md %}) 且会保持相关数据。**图元** 基于其所属**类别** 可以获得一系列内置参数，也能接受用户定义的自定义参数。有些**图元** 是几何物件，例如墙（3D）或细节组件（2D),也有一些图元不包含任何几何，例如项目信息（对，即使它也是 Revit 数据模型中的一个图元，但不可以被选择。介于 Revit 视图是围绕几何图元而设计，因此 Revit 提供了一个自定义窗口来编辑项目信息）。在 Revit 模型中图元都包含了很多定义图元行为方式的族[类型]({{ site.baseurl }}{% link _en/1.0/guides/revit-types.md %}) 。

{% capture api_note %}
Revit API 中**图元** 通常由 {% include api_type.html type='Autodesk.Revit.DB.Element' title='DB.Element' %} 来表示，每个图元的参数由 {% include api_type.html type='Autodesk.Revit.DB.Parameter' title='DB.Parameter' %} 来表示，  {% include api_type.html type='Autodesk.Revit.DB.Element' title='DB.Element' %} 提供多种方法以方便访问其属性集合。

&nbsp;

每个图元都有一个整数值的 Id (`DB.Element.Id`)， 但图元并不是固定不变得，在更新与工作设置相关操作（例如居中保存）会发生改变。通常通过图元唯一的 Id (`DB.Element.UniqueId`) 更安全，特别是你准备在 Revit 模型外部引用一个图元，例如外部数据库。请注意， `DB.Element.UniqueId` 并不是一个 UUID 编号，特别是你准备把这些信息发送给外部数据库时，要特别注意这个问题。
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

{% capture link_note %}
请浏览 [Revit: Elements & Instances]({{ site.baseurl }}{% link _en/1.0/guides/revit-elements.md %}) 指南部分，了解在 Revit 中如何利用 Grasshopper 如何来操作图元与实例
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-elements.png' %}

## 参数

参数是图元携带的元数据，储存和传达模型中所有图元的信息。例如高度是一个墙体图元的属性，高度值由高度参数携带。

参数有多种类型，包括:

1. 内置参数
2. 实例或类型的项目/共享参数
3. 全局参数

让我们来看下使用 Revit 图元时所遇到的各种参数类型。

### 内置参数

内置参数是基于不同类型的图元且内置于 Revit 内部最明显的参数集，例如一个墙体或是房间图元含有一个名为**体积** 的参数，这个参数不会影响 2D 填充图案图元当然也和其无任何关联。

Revit 在“图元属性”面板中显示内置参数列表

![]({{ "/static/images/guides/revit-params-parampanel.png" | prepend: site.baseurl }})

默认情况下，内置参数通常在每个 Revit 项目中定义，无法更改这些定义，只能读取或写入它们的值

{% capture api_note %}
Revit API 中所有的内置参数都由 {% include api_type.html type='Autodesk.Revit.DB.BuiltInParameter' title='DB.BuiltInParameter' %} 枚举表示
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### 项目 / 共享参数

Revit 允许用户创建一系列自定义参数并将它们整体应用到选定的类别。图元属性面板还会显示附加到所选图元的项目参数。

项目参数可以附加到图元类型或图元实例。

要创建项目参数，必须首先定义一个参数设置。这是一个模板，概述了参数的名称、数据类型、组和可选的 ID (Guid)。使用该设置后，可以将参数添加到项目中。添加到项目后，参数实例将附加到所有图元实例、类型或全局到项目。然后，每个 参数 实例都可以存储特定数据类型的唯一值。

共享参数只是项目参数，其参数定义可以通过共享参数文件从一个项目传输到另一个项目。多个项目可以包含公用参数定义。需要注意的是，仅共享定义，而不共享参数本身内的值。共享参数具有唯一的 ID (Guid)，而项目参数则没有。

![]({{ "/static/images/guides/revit-params-projshared.png" | prepend: site.baseurl }})

### 全局参数

全局参数是与类别无关的参数，可以将其用于许多不同 Revit 类别中的一系列实例或类型参数

![]({{ "/static/images/guides/revit-params-global.png" | prepend: site.baseurl }})

&nbsp;

{% capture link_note %}
请浏览 [Revit: Parameters]({{ site.baseurl }}{% link _en/1.0/guides/revit-params.md %}) 指南部分，以了解如何在 Revit 中利用 Grasshopper 来查询、读取与写入图元实例与类型参数。
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-params.png' %}

## 类别、族与类型

现在我们知道 Revit 将建筑组件组织为**类别** 、**族** 与**类型** ，下面将详细讨论一下它们

### 类别

![]({{ "/static/images/guides/revit-types-categories.svg" | prepend: site.baseurl }})

**类别** 是最高级别的组织，这些类别都内置于 Revit 中，根据各自不同的功能松散的组织各自的组件。一个 Revit 模型中通常都会同事存在多组不同的类别：

- 模型类别，例如墙、门、楼板与屋顶等。
- 分析类别，例如表面分析与结构载荷等。
- 注解类别，例如标签、尺寸等。

其实每个类别中都存在多种不同的类别，有人认为类别其实比较高级别的组织，实际上遵循类、族与类型的组织系统能更好的理解和记住它们。

{% capture api_note %}
Revit API 中所有的内置类都由 {% include api_type.html type='Autodesk.Revit.DB.BuiltInCategory' title='DB.BuiltInCategory' %} 枚举表示, 所有内置类别都由 {% include api_type.html type='Autodesk.Revit.DB.CategoryType' title='DB.CategoryType' %} 枚举表示
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### 类型

![]({{ "/static/images/guides/revit-types-types.svg" | prepend: site.baseurl }})

在讨论**族** 之前需要先了解什么是 Revit 中的**类型** ，正如前面提及到的每个 Revit 类别下面都会包含多种类型的图元，例如一个 3ft*7ft 的单开门、是位于门**类** 下的一个**类型，** 一个 2*4 的木柱是**结构柱**类别下的柱**类型**

每个类型都有一系列的类型相关的参数，这些参数用来修改该特定类型的相关操作。在使用 Revit 时我们更加倾向定义或修改各种**类型** 且将这些类型的**实例** 放入模型中，例如我们可以定义一个 3ft*7ft 单页门的**类型** ，然后再模型中的不同位置放置多个这样**类型** 的**实例** ，所有的这些实例都会遵循其特定类型的指定逻辑，当然类型定义也允许修改某些**实例参数** 以调整指定实例的图形等。

### 族

![]({{ "/static/images/guides/revit-types-families.svg" | prepend: site.baseurl }})

理解什么是**类型** 后我们开始讨论**族** ，初次接触 Revit 的用户都很难理解什么是**类别** 与**类型** ，Revit 模型中存在很多不同的**类型** ，而且他们彼此之间差异很大。例如我们可以拥有数百种具有不同设计与尺寸的门，其中车库门与单页门完全不同，因此我们需要一个方法来组织这些类型至相关群组的方法：

- **系统族** 都命名为**群组** ，例如 **风管系统** 、**基本墙体** 等…

- **自定义族**(或 [可载入族](https://help.autodesk.com/view/RVT/2024/ENU/?guid=GUID-7AEC5D66-C2E0-40E2-9504-3CC13781B87A)) 要更加复杂，可以使用多种自定义的方式来自行设计与创建。例如你可以创建一个看起来像太空飞船一样的新桌子族，可以将其悬空在地板上，然后根据场景设计需要摆放 6 到 12 张椅子。Revit 中的**族编辑器** 可以根据族模板文件 (`*.rft`)*来定义一个新的**自定义族** ，**自定义族** 可以存储在外部文件* (`*.rfa`) 中，而且可以与其他 Revit 用户共享. 内建族是自定义族的简化版，用于模型中使用一些受限的几何图形。

{% include ltr/warning_note.html note='**系统族** 这个名字容易让 Revit 用户混淆，请记住**系统族** 只是相关**类型组** 的一个名字，它们与自定义族完全不同且不能储存在外部文件中。通常 Revit 用户或是开发人员都不需要处理**系统族** ，而且 Revit 的 API 至今也不支持创建或是修改任何的**系统族** ，因此通常 Revit 用户讨论**族** 时都是指的**自定义族** ' %}

{% capture api_note %}
在 Revit API 中，**自定义族** 由 {% include api_type.html type='Autodesk.Revit.DB.Family' title='DB.Family' %}来表示，它们衍生的各种类型由 {% include api_type.html type='Autodesk.Revit.DB.FamilySymbol' title='DB.FamilySymbol' %}来表示，其每个实例都由 {% include api_type.html type='Autodesk.Revit.DB.FamilyInstance' title='DB.FamilyInstance' %}来表示
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

定义一个新的**自定义族** 并不是一件容易的事情，尤其当它需要智能、灵活的适应多种模型条件时，**自定义族** 无疑时 Revit 中最重要的自动化主题之一。很多公司都会为模型中经常使用的各种组件创建一套专有的自定义族，当然也有一些第三方的公司专门从事定制自定义族的业务。

Revit 为了方便用户入门，会在安装时会根据用户的系统配置建立一组默认的**自定义族** ，例如*公制与英制* ，还提供了很多模板以帮助用户一步步创建自己的**自定义族** 。

{% capture link_note %}
浏览 [Revit: Types & Families]({{ site.baseurl }}{% link _en/1.0/guides/revit-types.md %}) 指南部分，了解如何在 Revit 中利用 Grasshopper 来进行 Revit 类型与族的相关操作 
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-types.png' %}

## 容器

**Revit 容器** （例如工作集、设计选项等）是一种将一系列图元进行逻辑分组的机制，每个容器都有特定的用途，例如工作集允许导入部分的建筑物，因此协调与碰撞检车变得更加方便。加载指定的工作集时不会加载不属于该工作集的其他图元。

## 文档与链接

**Revit** 的**文档** 其实就是 **Revit 图元** 的合集，Revit 文档既可以是建筑模型（**Revit 项目** ）也可以表示为自定义族 （**Revit 族** ）

{% capture link_note %}
浏览  [Revit: Documents]({{ site.baseurl }}{% link _en/1.0/guides/revit-docs.md %}) 章节了解如何使用 Revit 的文档以及在 Revit 中使用 Grasshopper的文档
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-docs.png' %}
