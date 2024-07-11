---
title: Rhino.Inside.Revit 入门指南
layout: ltr/page-h2-toc
---

## 什么是 {{ site.terms.rir }}

[Rhino.Inside](https://github.com/mcneel/rhino.inside) 是由 {{ site.terms.mcneel }} 开发的一项新技术，允许将  {{ site.terms.rhino }} 嵌入至其他程序内. Rhino.Inside 正在被嵌入来之各个不同领域的程序中。

{{ site.terms.rir }}就是 Rhino.Inside 这项新技术中最典型的一个项目。RIR是 {{ site.terms.revit }}的一个附加模块，如同其他的 Revit 附加模块一样，允许将{{ site.terms.rhino }} 整个导入至 Revit 的缓存中。

{{ site.terms.rir }} 将 {{ site.terms.rhino }} 与 Grasshopper 的强大功能带入 {{ site.terms.revit }} 环境中

<!-- intro video -->

{% include youtube_player.html id="OktVivsMulc" %}

## 安装

请从下面的链接下载 {{ site.terms.rir }} 与 {{ site.terms.rhino }} {{ site.terms.rhino_versions }} 

<!-- download links -->

{% include ltr/download_buttons.html version=site.versions.one %}

也可以从 [Food4Rhino Website]({{ site.foodrhino_url }})下载 {{ site.terms.rir }} 安装包

安装{{ site.terms.rir }}需要 Revit {{ site.terms.revit_versions }} 的支持

- [Revit 2020.0](https://knowledge.autodesk.com/support/revit-products/troubleshooting/caas/downloads/content/autodesk-revit-2020-product-updates.html)
- [Revit 2021.1](https://knowledge.autodesk.com/support/revit-products/troubleshooting/caas/downloads/content/autodesk-revit-2021-product-updates.html)
- [Revit 2022.1](https://knowledge.autodesk.com/support/revit-products/troubleshooting/caas/downloads/content/autodesk-revit-2022-product-updates.html)
- [Revit 2023.0](https://knowledge.autodesk.com/support/revit-products/troubleshooting/caas/downloads/content/autodesk-revit-2023-product-updates.html)
- [Revit 2024.0](https://knowledge.autodesk.com/support/revit-products/troubleshooting/caas/downloads/content/autodesk-revit-2024-product-updates.html)

请先安装 {{ site.terms.rhino }} 

- 运行下载好的安装程序，根据提示进行安装，直到 {{ site.terms.rhino }} 完全被安装
- 运行 {{ site.terms.rhino }} 且确保你有授权来正常运行

然后安装  {{ site.terms.rir }}

- 运行下载好的安装程序，根据提示进行安装，直到 {{ site.terms.rir }} 完全被安装

现在已经我们已经安装好了两个必须项目，可以开始加载  {{ site.terms.rir }} 了

## 加载 {{ site.terms.rir }}

运行 {{ site.terms.revit }}. 你会被询问是否需要加载 {{ site.terms.rir }}，请确定选择  **Always Load | 总是加载** 以避开后面再次选择的麻烦。

![]({{ "/static/images/started/revit-prompt.png" | prepend: site.baseurl }})

加载完成后，你会在 Revit 界面下看到新的  *Rhino.Inside* 选项卡

![]({{ "/static/images/started/rir-addon.png" | prepend: site.baseurl }})

在新加载的{{ site.terms.rir }}选项卡内点击 *Start* 按钮，将会加载 {{ site.terms.rhino }} 嵌入至 Revit 的缓存中，且确定它有被授权。处理完加载过程后你会在工具列中看到新的 *Rhinoceros* 与 *Grasshopper* 面板。

![]({{ "/static/images/ribbon/ribbon.png" | prepend: site.baseurl }})

新的工具列包含了很多功能按钮，可以访问 

- {{ site.terms.rhino }} 
- Script Editor (支持访问 Revit API)
- Grasshopper (包含 Revit 运算器)

请浏览 [{{ site.terms.rir }} Interface]({{ site.baseurl }}{% link _en/1.0/reference/rir-interface.md %}) 以了解 *Rhinoceros* 面板下所有的按钮列表

如果此时你有遇到任何错误，请参考 [Known Issues]({{ site.baseurl }}{% link _en/1.0/reference/known-issues.md %}) 页面来查找相关已知错误的临时解决方案。

## 嵌入Revit中的Grasshopper

{{ site.terms.rir }} 是嵌入 Grasshopper中最重要的功能之一，点击 *Rhinoceros*选项卡下的 Grasshopper 按钮即可开始载入 Grasshopper, 请注意 Grasshopper 窗口内额外的*Revit*选项卡内有很多 Revit 相关的运算器： 

![]({{ "/static/images/started/rir-gh01.png" | prepend: site.baseurl }})

在默认的*Params*选项卡下也有一组 *Revit Primitives* 面板:

![]({{ "/static/images/started/rir-gh02.png" | prepend: site.baseurl }})

Revit 系列工具组图标能让用户比较直观的了解其功能，如下图所示基于颜色的分类操作（包括查询、分析、修改与创建等）。这一系列的应用图标，用来显示不同的类型、标识或其他设计相关的数据类型，包括：

![]({{ "/static/images/started/rir-icons.png" | prepend: site.baseurl }}){: class="small-image"}

请浏览 [Basic Interactions Guide]({{ site.baseurl }}{% link _en/1.0/guides/rir-grasshopper.md %}) 了解更多 Revit 专属运算器的介绍。

## 提取 Revit 几何物件

先教大家如何在 Grasshopper 中利用一个简单的脚本来提取 Revit 图元几何，Grasshopper 是目前 Rhino 模块中最棒的模组之一，且作为 {{ site.terms.rir }} 项目的一部分，能显著的改善 {{ site.terms.revit }} 的设计与文档处理能力。

首先在 Revit 中建立一个简单的单一墙体。

![]({{ "/static/images/started/revit-doc.png" | prepend: site.baseurl }})

从 *Params > Revit* 面板中抓取一个  {% include ltr/comp.html uuid="ef607c2a-" %} 参数

![]({{ "/static/images/started/rir-gcomp1.png" | prepend: site.baseurl }})

现在右键点击运算器且在弹出菜单中点击 **Select One Revit Graphical Element**，Grasshopper 会切换至 Revit 窗口且会询问你选择一个 Revit 图元，请选择前面创建的墙体图元。

![]({{ "/static/images/started/rir-gcomp2.png" | prepend: site.baseurl }})

现在拖拽一个 *Panel* 运算器且链接至 {% include ltr/comp.html uuid="ef607c2a-" %} 输出端作为它的输入，你会看到这个参数中现在包含选择的墙体图元：

![]({{ "/static/images/started/rir-gcomp3.png" | prepend: site.baseurl }})

可以使用 Revit 专属的运算器来获取墙体几何物件，从 *Revit > Elements* 下拖拽一个  {% include ltr/comp.html uuid="b3bcbf5b-" %} 运算器，

![]({{ "/static/images/started/rir-gcomp5.png" | prepend: site.baseurl }})

将 {% include ltr/comp.html uuid="ef607c2a-" %} 的输出链接至 {% include ltr/comp.html uuid="b3bcbf5b-" %}的输入,新的 Revit 专属运算器使用 Revit API 从墙体图元中提取到墙体几何物件， 然后转换为 Rhino 的Brep 物件（方便其他非 Revit 运算器识别这个物件），进而可以在 Grasshopper 中做更多的操作。

![]({{ "/static/images/started/rir-gcomp6.png" | prepend: site.baseurl }})

和其他的 Grasshopper 几何运算器一样，最终输出的几何物件会同时显示在 Rhino 与 Revit 的视窗中。

![]({{ "/static/images/started/rir-gcomp7.png" | prepend: site.baseurl }})

现在你会发现使用 {{ site.terms.rir }} 是如此的直观且简单，Revit专属运算器是 {{ site.terms.rir }} 项目最重要的环节，Grasshopper 脚本运算器(Python 与 C#) 还可以同时访问 Rhino 与 Revit 的 API，还可以根据你的设计或项目需要在 Grasshopper 中自定义符合您工作流程的运算器。

## 创建 Revit 图元

前面示范如何利用 Grasshopper 中的 Revit 组件来转换 Revit 几何对象为 Rhino 几何物件，Grasshopper 中还内置了很多专门为 Revit 定制的运算器，下面将示范如何利用这些运算器来创建 Revit 构件。

 在 Revit 中创建一个简单的墙体会需要用到下面这些运算器：

- 建立墙体直线（用起点与终点来定义）
- 墙体类型
- 放置墙体的楼层
- 墙体高度

首先从 Revit 中打开 Rhino，并绘制一条直线。

![]({{ "/static/images/started/rir-rhino1.png" | prepend: site.baseurl }})

从 Revit 中打开 Grasshopper，使用Curve运算器将前面的直线载入Grasshopper

![]({{ "/static/images/started/rir-rhino2.png" | prepend: site.baseurl }})

现在从  *Revit > Input* 面板中找到  {% include ltr/comp.html uuid="af9d949f-" %}、{% include ltr/comp.html uuid="d3fb53d3-9" %} 与 {% include ltr/comp.html uuid="bd6a74f3-" %} 等运算器:

![]({{ "/static/images/started/rir-rhino3.png" | prepend: site.baseurl }})

最后我们添加一个 Grasshopper 的 Slider 来调整新建墙体的高度

![]({{ "/static/images/started/rir-rhino6.png" | prepend: site.baseurl }})

为了创建墙体，我们要使用一个自定义的 Grasshopper节点，该节点可以通过曲线创建墙体。从 *Revit > Build* 面板中找到 {% include ltr/comp.html uuid="37a8c46f-" %} 运算器，

![]({{ "/static/images/started/rir-rhino7.png" | prepend: site.baseurl }})

现在所有的运算器都已经抓如至 Grasshopper 脚本中，让我们把这些运算器组织起来

![]({{ "/static/images/started/rir-rhino8.png" | prepend: site.baseurl }})

从 {% include ltr/comp.html uuid="af9d949f-" %}运算器的类别列表中选择 **Walls** 类别

现在链接 {% include ltr/comp.html uuid="af9d949f-" %} 的输出至 {% include ltr/comp.html uuid="d3fb53d3-9" %} 的输入，请注意输入参数默认不可见，将箭头拖动到预期输入参数所在的组件左侧。

![]({{ "/static/images/started/rir-rhino9.png" | prepend: site.baseurl }})

{% include ltr/comp.html uuid="d3fb53d3-9" %} 现在会显示从模型中读取的墙体类型列，挑选一个基础墙体类型用来建立新的墙体 

现在链接其他的一些运算器，最终效果入下图所示

![]({{ "/static/images/started/rir-rhino10.png" | prepend: site.baseurl }})

现在 {% include ltr/comp.html uuid="37a8c46f-" %} 运算器提供了所有在 Revit 中创建墙体所需要的参数。

![]({{ "/static/images/started/rir-rhino11.png" | prepend: site.baseurl }})

相同的墙体几何物件也会在 Rhino 视窗中显示

![]({{ "/static/images/started/rir-rhino12.png" | prepend: site.baseurl }})

## Grasshopper 动态交互

对于 Grasshopper 这样的图形化编程工具来说，其最大特色就是动态交互， {{ site.terms.rir }} 将这一特色完全带入 Revit 运行环境中，这样可以让建筑师、工程师更加有效的探索设计空间并找到更多新颖的解决方案。

打开我们前面做的小练习，您现在可以动态调整一些参数，如下图所示。

![]({{ "/static/images/started/rir-ghinter.gif" | prepend: site.baseurl }})

无限可能！

## GHPython in Revit

Rhino.Inside.Revit 是让 Rhino 在 Revit 内部运行，因而 Rhino 和 Grasshopper 的插件也都可以访问 Revit API，因此 Python 的脚本可以同时使用 Rhino API、Grasshopper API 与 Revit API，进而会让 Rhino 与 Grasshopper 中的 Python 脚本功能成倍提高。除了这些 API 之外 {{ site.terms.rir }} 的附加模块还提供了很多额外的功能，主要用于将 Rhino/Grasshopper 的数据离线转换为 Revit, 或是将 Revit 的数据转换为 Rhino/Grasshopper.

来看看下面这个 Python 脚本范例， 它会把前面提到的所有 API 的符号导入脚本。

{% highlight python %}

# adding references to the System, RhinoInside

import clr
clr.AddReference('System.Core')
clr.AddReference('RevitAPI') 
clr.AddReference('RevitAPIUI')
clr.AddReference('RhinoInside.Revit')

# now we can import symbols from various APIs

from System import Enum

# rhinoscript

import rhinoscriptsyntax as rs

# rhino API

import Rhino

# grasshopper API

import Grasshopper

# revit API

from Autodesk.Revit import DB

# rhino.inside utilities

import RhinoInside API
from RhinoInside.Revit import Revit, Convert

# add extensions methods as well

# this allows calling .ToXXX() convertor methods on Revit objects

clr.ImportExtensions(Convert.Geometry)

# getting active Revit document

doc = Revit.ActiveDBDocument
{% endhighlight %}

为了运行上面这个脚本文件，我们需要增加下面这段代码，使用 Revit API (`.Geometry[DB.Options()]`) 来读取 Revit 图元  (`E`)，然后经由{{ site.terms.rir }} API 将 Revit 的几何物件转为为 Rhino (`Convert.ToRhino()`)且最后经由 Grasshopper 输出 Rhino 几何物件

{% highlight python %}
G = [x.ToBrep() for x in E.Geometry[DB.Options()]]
{% endhighlight %}

![]({{ "/static/images/started/rir-ghpy.png" | prepend: site.baseurl }})

{{ site.terms.rir }}已经是一个非常强大的工具，现在使用 Python 与 C# 可以扩展无限可能...

## 下一步

导航上 *Guides|指南* 部分是作为你进一步了解如何使用 {{ site.terms.rir }}来解决 {{ site.terms.revit }} 中的诸多设计与文案挑战的学习手册，该页下面提供了许多有关如何创建 Grasshopper脚本与如何编写自定义脚本的范例。

浏览  [Discover]({{ site.baseurl }}{% link _en/1.0/discover/index.md %}) 页面，了解如何在 Revit 工作流程中使用 Rhino 与 Grasshopper。

如果你遇到可能需要更新的运算器，或需要详细解释的特殊情况，请联系{{ site.terms.rir }}的开发者，也可以在[论坛]({{ site.forum_url }})上与其他用户交流。