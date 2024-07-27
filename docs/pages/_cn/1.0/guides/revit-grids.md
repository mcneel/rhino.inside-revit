---
title: Grids
order: 45
thumbnail: /static/images/guides/revit-grids.png
subtitle: Workflows for Revit Grids
group: Modeling

---

## 查询轴网

{% capture api_note %}
Revit API 中使用 {% include api_type.html type='Autodesk.Revit.DB.Grid' title='DB.Grid' %}来表达所有的 轴网类型，{{ site.terms.rir }}中使用 {% include ltr/comp.html uuid='7d2fb886' %} 来表达一个轴网。
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### 选择现有轴网

可以使用 {% include ltr/comp.html uuid="218fdacd-" %} 运算器查询项目中的所有轴网，可以重新定义其名字、高度与输入过滤等。

![]({{ "/static/images/guides/revit-grid-query.png" | prepend: site.baseurl }})

可以使用轴网参数来进行筛选，右键点击 {% include ltr/comp.html uuid="7d2fb886-" %} 运算器可以选择不同的轴网参数,

![]({{ "/static/images/guides/revit-grid_right_click.png" | prepend: site.baseurl }})

## 查询轴网类型

请浏览 [Modifying Types]({{ site.baseurl }}{% link _en/1.0/guides/revit-types.md %}) 来了解如何查询轴网类型信息

## 分析轴网

### 提取轴网曲线

将一个 Grid 运算器连接至 Curve 运算器，可以直接提取这个 Grid 的曲线：

![]({{ "/static/images/guides/revit-grid_to_curve.png" | prepend: site.baseurl }})

## 创建轴网

可以利用 {% include ltr/comp.html uuid="cec2b3df-" %} 运算器来创建一个轴网，需要输入一条直线或圆弧、定义名字与类型等参数即可；

![]({{ "/static/images/guides/revit-grid-add-by-curve.png" | prepend: site.baseurl }})
