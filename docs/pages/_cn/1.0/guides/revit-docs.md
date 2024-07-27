---
title: "Revit: Documents & Links"
subtitle: Where all the elements are stored and shared
order: 25
group: Essentials
home: true
thumbnail: /static/images/guides/revit-docs.png
ghdef: revit-docs.ghx
---

{% capture link_note %}
这一章我们将会介绍如何利用 Revit 中的 Grasshopper 来与 Revit 文档与链接配合作业
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-docs.png' %}

## 查询打开的文档

利用 {% include ltr/comp.html uuid='5b935ca4' %} 运算器来查看已经在 Revit 中打开的文档:

![]({{ "/static/images/guides/revit-docs-opendocs.png" | prepend: site.baseurl }})

{% include ltr/comp.html uuid='ee033516' %} 运算器总会参照当前工作文档内容，这样你切换不同的 Revit 文档时它也会及时的更新，:

![]({{ "/static/images/guides/revit-docs-activedoc.png" | prepend: site.baseurl }})

{% include ltr/bubble_note.html note='注意文档运算器底部标签会显示目标文档名称' %}

使用 {% include ltr/comp.html uuid='94bd655c' %} 可以从当前执行文档中获取身份信息:

![]({{ "/static/images/guides/revit-docs-identity.png" | prepend: site.baseurl }})

## Document-Aware 运算器

Document-Aware  运算器可以工作与当前文档与指定文档，它还有隐藏的输入选项 {% include ltr/misc.html uuid='f3427d5c' title='Document' %} 可以放到运算器后增加参数：

![]({{ "/static/images/guides/revit-docs-docparam.gif" | prepend: site.baseurl }})

一旦增加好参数，就可以将任何的 Revit 文档作为其输入选项:

![]({{ "/static/images/guides/revit-docs-identityall.png" | prepend: site.baseurl }})

也可以配合其他的运算器查询多个文档，例如这里查询多个墙体:

![]({{ "/static/images/guides/revit-docs-docelements.png" | prepend: site.baseurl }})

## 文档属性

使用 {% include ltr/comp.html uuid='c1c15806' %} 运算器 来查看给定文档的属性：

![]({{ "/static/images/guides/revit-docs-docfile.png" | prepend: site.baseurl }})

使用 {% include ltr/comp.html uuid='f7d56db0' %} 来检查给定文档的工作共享属性：

![]({{ "/static/images/guides/revit-docs-wsidentity.png" | prepend: site.baseurl }})

使用 {% include ltr/comp.html uuid='3917adb2' %} 来获取文档中累积的持续（可审查）警告生成的失败消息列表

![]({{ "/static/images/guides/revit-document-warnings.png" | prepend: site.baseurl }})

使用 {% include ltr/comp.html uuid='825d7ab3' %} 运算器来读取 Revit 文档公差

![]({{ "/static/images/guides/revit-document-tolerance.png" | prepend: site.baseurl }})

使用 {% include ltr/comp.html uuid='ace507e5' %} 来获取 Revit 版本号

![]({{ "/static/images/guides/revit-document-revitVersion.png" | prepend: site.baseurl }})

使用 {% include ltr/comp.html uuid='4bfeb1ee' %} 来获取 Revit 用户属性

![]({{ "/static/images/guides/revit-document-revitUser.png" | prepend: site.baseurl }})

使用 {% include ltr/comp.html uuid='cb3d697e' %} 来获取默认文件路径

![]({{ "/static/images/guides/revit-document-fileLocations.png" | prepend: site.baseurl }})

## 保存文档

使用 Save Document 运算器将给定的文档另存为一个输出文档，输出文档可以给定一个路径字符，请注意给定路径中要包含正确的文件扩展名：

![]({{ "/static/images/guides/revit-docs-save.png" | prepend: site.baseurl }})

## 查询链接的文档

{% capture api_note %}
当Revit导入一个模型的同时也会导入所有的链接模型至内存，每一个Revit模型都经由一个 {% include api_type.html type='Autodesk.Revit.DB.Document' title='DB.Document' %}. 实例来表达, 因为 `DB.Document.IsLinked` 将会显示是否导入其他的链接文档, Revit不能在同一个场景同时开启两个相同的实例, 这是不导入主模型的情况下是无法编辑链接模型的主要原因。
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

{% include youtube_player.html id="UkIW-0U0-Yk" %}

可以使用 {% include ltr/comp.html uuid='ebccfdd8' %} 运算器来查询当前文档(或给定文档)的所有链接文档信息,

![]({{ "/static/images/guides/revit-links-doclinks-1.4.png" | prepend: site.baseurl }})

链接输出包含文档名称、位置、共享位置名称以及该链接的唯一实例 ID。

文档输出包含链接文档的名称，可以在查询运算器的文档输入中使用。

## 查询链接的图元

使用 {% include ltr/comp.html uuid='0f7da57e' %} 运算器可以直接查询给定文档的图元信息, 也可以输入一个连接文档,

![]({{ "/static/images/guides/revit-links-querywalls1.4.png" | prepend: site.baseurl }})

当使用 {% include ltr/comp.html uuid='0f7da57e' %}  在链接模型中的图元时，将会以它们在链接模型中的基本方向导入，且连接图元必须定位至主体项目中链接实例的位置。更多请参考下面的 [链接几何方向](#linked-geometry-orientation) 内容

## 链接几何方向

导入文件的图形图元的位置和其在原项目中的位置相同，有必要将这些几何定位至链接实例中，例如使用 Orient 运算器进行定位：

![]({{ "/static/images/guides/revit-links-doclinks-orient.png" | prepend: site.baseurl }})
