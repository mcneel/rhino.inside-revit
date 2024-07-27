---
title: Drafting
order: 62
thumbnail: /static/images/guides/revit-drafting.png
subtitle: Workflows for Drafting in Revit
group: Documentation
---

## 创建详图线

可以使用 {% include ltr/comp.html uuid='5a94ea62' %} 运算器来创建一条详图线，输入端 View 接入一个你准备创建详图线图元的视图，然后选择输入一条开发的线条作为输入即可：

![]({{ "/static/images/guides/revit-drafting02.png" | prepend: site.baseurl }})

## 新增区域

使用 {% include ltr/comp.html uuid='ad88cf11' %} 运算器可以创建一个区域，通过 View 参数选择你准备创建区域的视图，且输入配置文件至 Type，请注意轮廓必须是**水平**且共平面的封闭平面，

由于我们无法直接通过 Revit API 来创建遮罩区域，但可以通过创建一个完全内接于另外一个区域的区域，它将成为外部区域的遮罩。

![]({{ "/static/images/guides/revit-anno-addFillRegion.png" | prepend: site.baseurl }})

## 创建文字

借助 {% include ltr/comp.html uuid='49acc84c' %} 运算器可以增加文本，使用 View 参数选择你准备放置文本图元的视窗，然后将适当的内容输入至 Conten 输入即可：

![]({{ "/static/images/guides/revit-drafting03.png" | prepend: site.baseurl }})

## 增加详图项

可以使用  {% include ltr/comp.html uuid='fe258116' %} 运算器放置 2D 详图项族，这个运算器需要输入视图，工作平面或点与详图族类型

![]({{ "/static/images/guides/revit-anno-addDetail-item.png" | prepend: site.baseurl }})

## 增加符号

可以使用 {% include ltr/comp.html uuid='2beb60ba' %} 运算器来放置 Revit 符号，这个运算器还需要输入视图、工作平面或点以及符号族类型.

![]({{ "/static/images/guides/revit-anno-addSymbol.png" | prepend: site.baseurl }})

## 尺寸标注

{% capture api_note %}
在 Revit API 中使用{% include api_type.html type='Autodesk.Revit.DB.Dimension' title='DB.Dimension' %}来表达所有类型的尺寸标注， {{ site.terms.rir }} 中使用 {% include ltr/comp.html uuid='bc546b0c' %} 参数来表达一个尺寸标注
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### 创建长度标注

可以利用 {% include ltr/comp.html uuid='df47c980' %} 运算器为基于给出的参考物件进行长度尺寸标注， 也可以选择一条直线来放置尺寸标注：

![]({{ "/static/images/guides/revit-anno-dim-linear.png" | prepend: site.baseurl }})

### 创建角度标注

可以使用{% include ltr/comp.html uuid='0dbe67e7' %} 运算器为给出参考物件进行角度尺寸标准，可以选择一个圆弧来放置标注：

![]({{ "/static/images/guides/revit-anno-dim-angular.png" | prepend: site.baseurl }})

### 创建高程点坐标

可以利用 {% include ltr/comp.html uuid='449b853b' %} 运算器来基于给定的参考创建高程点坐标：

![]({{ "/static/images/guides/revit-anno-dim-spotCoordinate.png" | prepend: site.baseurl }})

### 创建高程点

可以使用 {% include ltr/comp.html uuid='00c729f1' %} 运算器来创建高程点，要放置有效视图（非草图或未锁定3D），需要 Revit 的参考与位置，鼠标右键点击运算器来设置有效的参考。

![]({{ "/static/images/guides/revit-anno-dim-spotElevation.png" | prepend: site.baseurl }})

## 标记

### 增加面积标记

在区域平面中使用 {% include ltr/comp.html uuid='ff951e5d' %} 运算器来增加标记，这个运算器至少需要一个输入 Revit 面积参数。

![]({{ "/static/images/guides/revit-anno-tag-area.png" | prepend: site.baseurl }})

### 增加材质标记

利用 {% include ltr/comp.html uuid='11424062' %} 运算器可以创建材质标记，但需要提供一个带有材质与要标记的有效视图图元。

![]({{ "/static/images/guides/revit-anno-tag-material.png" | prepend: site.baseurl }})

### 增加多类别标记

利用 {% include ltr/comp.html uuid='e6e4a2ee' %} 创建多类别标记，需要输入要标记的图元与有效视图。

![]({{ "/static/images/guides/revit-anno-tag-material.png" | prepend: site.baseurl }})

## 修订

### 查询修订

利用 {% include ltr/comp.html uuid='8ead987d' %} 运算器可以获取所有文档的修订

![]({{ "/static/images/guides/revit-revisions-query.png" | prepend: site.baseurl }})

### 图纸修订

使用 {% include ltr/comp.html uuid='2120c0fb' %} 运算器可以获取所有的图纸修订。

![]({{ "/static/images/guides/revit-revisions-sheets.png" | prepend: site.baseurl }})

### 增加云线批注

利用 {% include ltr/comp.html uuid='8ff70eef' %} 运算器可以建立云线批注

![]({{ "/static/images/guides/revit-revisions-addCloud.png" | prepend: site.baseurl }})

## 图片

### 增加图片

利用 {% include ltr/comp.html uuid='506d5c19' %} 运算器可以增加一个图片源

![]({{ "/static/images/guides/revit-image-add.png" | prepend: site.baseurl }})

### 增加图片类型

利用 {% include ltr/comp.html uuid='09bd0aa8' %} 运算器可以从图片源增加一种图片类型

![]({{ "/static/images/guides/revit-image-add-type.png" | prepend: site.baseurl }})