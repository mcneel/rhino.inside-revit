---
title: "Revit: Categories"
subtitle: All working with Categories
order: 24
group: Essentials
home: true
thumbnail: /static/images/guides/revit-categories.png
ghdef: 
---

{% capture link_note %}

类别是最高级别的组织，这些类别都内置于 Revit 中，根据各自不同的功能松散的组织各自的组件。无法在 Revit 中删除或添加类别，但提供许多不同的方式组织类别。
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-categories.png' %}

一个 Revit 模型中通常都会存在多组不同的类别:

- 模型类别，例如墙、门、楼板与屋顶等
- 分析类别，例如表面分析与结构载荷等
- 注解类别，例如标签、尺寸等
- 内部类比，例如各种标签.

模型类别可以包含的图元各不相同， 开发和了解常用类别的使用方式对于使用 Revit 很重要，类别可能包含也可能不包含:

- Directshapes

- 可载入族， 例如家具、通用模型与场地等

- 系统族/类型，例如墙体、门、楼板与屋顶等
  
  请 [Rhino To Revit]({{ site.baseurl }}{% link _en/1.0/guides/rhino-to-revit.md%}) 章节来了解如何利用 Rhino 与 Grasshopper 来添加图元。

## 查询类别

有两个主要的方法来查询一个文档中的类别， 

使用{% include ltr/comp.html uuid="d150e40e" %} 运算器来查询一个当前项目文档中的主要类别与子类别，还可以利用 {% include ltr/comp.html uuid="5ffb1339" %} 运算器来进一步的过滤与筛选某些特定的类型或使用一个布尔开关来选择主要的类别或子类别，

![]({{ "/static/images/guides/gh-query-category.png" | prepend: site.baseurl }})

第二个方法是使用 {% include ltr/comp.html uuid="af9d949f" %}. 这个选取器会列出每个 Revit 文档中所有的主要类别与子类别，可以先选择一个所需要的类别然后通过 {% include ltr/comp.html uuid="d794361e" %} 运算器来输出类别标识的部分

![]({{ "/static/images/guides/gh-built-in-category.png" | prepend: site.baseurl }})

{% include ltr/bubble_note.html note=' 别选择器支持搜索，双击运算器标题然后在框内输入准备查询类别的名称，如果名称有效则会列出所有与之匹配的类别。支持多模式进行查询，甚至是表达式 ' %}

## 访问类别

 {% include ltr/comp.html uuid="d794361e" %} 运算器可以用来建立各种参数类型（模型、注解与分析）名称，包括主类别于子类别。

![]({{ "/static/images/guides/gh-built-in-category.png" | prepend: site.baseurl }})

可以创建属于某个类别的参数列表，以帮助建立用于创建计划或将其设置于某个适当的列表中。例如下面使用  {% include ltr/comp.html uuid="af9d949f" %} 运算器来选择一个门的类别，然后利用  {% include ltr/comp.html uuid="189f0a94" %} 运算器来查找合格项目中所有门的附加参数，

![]({{ "/static/images/guides/gh-category-parameter.png" | prepend: site.baseurl }})

通过 {% include ltr/comp.html uuid="ca3c1cf9" %} 运算器来获取与设置一个类别的图形类型

![]({{ "/static/images/guides/gh-category-graphic-style.png" | prepend: site.baseurl }})

## 扩展类别

主要类别都是被内置且无法被修改，尽管可以在大多数类别中添加子类别以进一步组织和重构控制图元

## 子类别

利用 {% include ltr/comp.html uuid="8de336fb" %} 运算器可以增加一个子类别，如果子类别已经存在这个运算器会间的返回当前已经存在的子类别

![]({{ "/static/images/guides/gh-sub-category.png" | prepend: site.baseurl }})

使用 {% include ltr/comp.html uuid="4915ab87" %} 运算器来创建一组指定类别的子类别，

![]({{ "/static/images/guides/gh-category-subcategory.png" | prepend: site.baseurl }})

使用 {% include ltr/comp.html uuid="495330db" %} 运算器来获取-设置图元的子类别

![]({{ "/static/images/guides/revit-element-subcategory.png" | prepend: site.baseurl }})
