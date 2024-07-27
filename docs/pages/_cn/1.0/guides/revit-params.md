---
title: "Revit: Parameters"
subtitle: All the different types of Parameters
order: 22
group: Essentials
home: true
thumbnail: /static/images/guides/revit-params.png
ghdef: revit-params.ghx
---

{% capture link_note %}
在这一章我们将会讲解如何读取使用 Grasshopper 所建立的 Revit 图元的参数，如果想了解 Revit 中的参数是如何组织的，请浏览  [Revit Elements:Parameters Guide]({{ site.baseurl }}{% link _en/1.0/guides/revit-revit.md %}#parameters)
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-params.png' %}

## 检查参数

如果你需要检查某一个图元的属性，你可以使用 {% include ltr/comp.html uuid='fad33c4b' %} 来查看

![]({{ "/static/images/guides/revit-params-inspect.png" | prepend: site.baseurl }})

现在请按住 {% include ltr/kb_key.html key='Shift' %} 键+鼠标双击 {% include ltr/comp.html uuid='fad33c4b' %} 运算器，这样你可以查看所有和该元素相关的参数列表。

![]({{ "/static/images/guides/revit-params-inspect-expanded.png" | prepend: site.baseurl }})

你可以从输出端连接一些查看运算器来查看相关的属性信息, 例如Panel，然后你可以按住 {% include ltr/kb_key.html key='Ctrl' %}  键 +鼠标双击 {% include ltr/comp.html uuid='fad33c4b' %} 运算器来恢复默认显示大小，输出端有连接其他运算器的端口会被继续保留

![]({{ "/static/images/guides/revit-params-inspect-collapsed.png" | prepend: site.baseurl }})

要检查现有参数的定义，请使用 {% include ltr/comp.html uuid='3bde5890' %} 运算器：

![]({{ "/static/images/guides/param-identity.png" | prepend: site.baseurl }})

### 查找图元参数

要查找与一个图元相关的参数，请将图元与参数名称接入 {% include ltr/comp.html uuid='44515a6b' %} 运算器来进行查询：

![]({{ "/static/images/guides/param-find-builtin.png" | prepend: site.baseurl }})

### 参数范围

参数可以被附加到图元类型或单个实例，将 {% include ltr/comp.html uuid='ef607c2a' %} 直接接入至 {% include ltr/comp.html uuid='fad33c4b' %} 运算器以检查两个实例参数，将  {% include ltr/comp.html uuid='fe427d04' %}  运算器 链接至 {% include ltr/comp.html uuid='fad33c4b' %} 运算器以获取类型参数，你会发现可用参数的差异。

![]({{ "/static/images/guides/revit-params-instance-type.png" | prepend: site.baseurl }})

### 查找内置参数

可以使用 {% include ltr/comp.html uuid='c1d96f56' %} 运算器来检索 Revit 内建参数，双击标题可以搜索部分参数名称：

![]({{ "/static/images/guides/revit-params-querybuiltin.png" | prepend: site.baseurl }})

## 读取参数值

如果你需要查询特定的参数值，建议你使用 Revit Parameters 面板中的 {% include ltr/comp.html uuid='a550f532' %}  运算器：

![]({{ "/static/images/guides/revit-params-paramkeycomp.png" | prepend: site.baseurl }})

继续鼠标右键点击这个运算器，然后选择你需要查询的一些参数项

![]({{ "/static/images/guides/revit-params-paramkey.png" | prepend: site.baseurl }})

运算器输出端连接 {% include ltr/comp.html uuid='f568d3e7' %} 来查询 这个值

![]({{ "/static/images/guides/revit-params-getfromkey.png" | prepend: site.baseurl }})

也可以使用 {% include ltr/comp.html uuid='f568d3e7' %}  以指定参数名的方式来读取这个参数值

![]({{ "/static/images/guides/revit-params-getfromname.png" | prepend: site.baseurl }})

{% include ltr/locale_note.html note='我们是以特定的语言来指定参数名称，因此如果利用不同语言版本的 Revit 打开这个 Grasshopper 脚本文件可能会出现中断错误' %}

使用共享参数时还可以用 UUID的作为查询输入值

![]({{ "/static/images/guides/revit-params-getfromuuid.png" | prepend: site.baseurl }})

## 设置参数值

可以使用 {% include ltr/comp.html uuid='f568d3e7' %} 运算器来设置一个 Revit 图元的参数值，要注意有些参数值为只读而不可以被覆盖

![]({{ "/static/images/guides/revit-params-setfromname.png" | prepend: site.baseurl }})

注意 {% include ltr/comp.html uuid='f568d3e7' %} 运算器仅以一个参照方式引入 Revit 图元，因此当基于这个运算器的参数值被更新时，它会把所有引入的图元运算器参数进行更新，这个操作可能不同于常规的 Grasshopper 运算器。

![]({{ "/static/images/guides/revit-params-setverify.png" | prepend: site.baseurl }})

## 创建参数

创建参数的运算器位于 Grasshopper > Parameter 面板下，可以利用它再 Revit 中建立新的参数，通常流程如下：

1. 首先必须定义一个参数，
2. 然后增加这个参数至文档，
3. 对于项目参数，需要设置附加类别和组（可选），
4. 然后进行图元、类型或是全局参数设置。

参数定义可以使用 {% include ltr/comp.html uuid='134b7171' %} c运算器来创建或是直接从一个 [共享参数文件](#Shared_Parameters_File)读取。

一旦建立定义，可使用 {% include ltr/comp.html uuid='84ab6f3c' %}  运算器将参数增加至项目，如果是参数范围是针对 [全局参数](#Global_Parameters), 则使用 {% include ltr/comp.html uuid='32e77d86' %}  运算器进行设置参数值，请在使用前设置 [项目参数](#Adding_a_Project_parameter) 的额外属性。

![]({{ "/static/images/guides/param-global-create.png" | prepend: site.baseurl }})

### 新增项目参数

相对一个基础参数而言项目参数会有很多额外的属性，项目参数属于某些类别，且起参数值也因图元不同而有所不同。

![]({{ "/static/images/guides/revit-params-definekeycomp.png" | prepend: site.baseurl }})

通过连接参数名至 {% include ltr/comp.html uuid='134b7171' %} 运算器就可以来创建一个新的参数，然后使用 {% include ltr/comp.html uuid='8ab856c6' %} 与 {% include ltr/comp.html uuid='5d331b12' %} 来设置参数类型与输入群组， {% include ltr/comp.html uuid='134b7171' %}  运算器将会建立一个新的参数定义，这个定义可以连接至 {% include ltr/comp.html uuid='84ab6f3c' %} 运算器在 Revit 中创建参数，你也可以使用 {% include ltr/comp.html uuid='3bde5890' %} 来检查你所创建的参数。

![]({{ "/static/images/guides/revit-params-createshared.png" | prepend: site.baseurl }})

{% include ltr/warning_note.html note='当前 Revit API 并不支持直接创建项目参数，因此需要利用 Grasshopper 的共享项目参数来创建项目参数' %}

下面是共享参数中的参数配置：

![]({{ "/static/images/guides/revit-params-sharedwindow.png" | prepend: site.baseurl }})

可以后续将其参数名连接至 {% include ltr/comp.html uuid='f568d3e7' %} 运算器来读取或设置其参数值，也可以使用 {% include ltr/comp.html uuid='fad33c4b' %} 运算器与 {% include ltr/comp.html uuid='3bde5890' %} 运算器来检查参数值：

![]({{ "/static/images/guides/revit-params-valueinfo.png" | prepend: site.baseurl }})

## 共享参数文件

使用 {% include ltr/comp.html uuid='7844b410' %} 运算器来读取共享参数文件，将会返回当前文件的路径、群组与参数定义等。这个文件仅仅包含定义内容，所以必须将参数添加至当前项目中才能后续设置参数值，使用 {% include ltr/comp.html uuid='84ab6f3c' %} 来添加参数且使用 {% include ltr/comp.html uuid='f568d3e7' %} 来设置图元的参数值。

![]({{ "/static/images/guides/param-shared-file.png" | prepend: site.baseurl }})

## 全局参数

如果要查询当前项目的全局参数必须使用 {% include ltr/comp.html uuid='d82d9fc3' %} 运算器， {% include ltr/comp.html uuid='32e77d86' %}  运算器可以帮助获取全局参数值：

![]({{ "/static/images/guides/param-global.png" | prepend: site.baseurl }})

可以使用通用的 [添加参数进程](#creating-parameters) 来为一个项目建立全局参数。

## 参数公式

使用尺寸与参数中的公式来驱动与控制模型中的参数内容， {% include ltr/comp.html uuid='21f9f9c6' %} 运算器可以使用 [Valid Formula Syntax and Abbreviations](https://help.autodesk.com/view/RVT/2024/ENU/?guid=GUID-B37EA687-2BDF-4712-9951-2088B2A8E523) 中涵盖的语法来创建参数公式。