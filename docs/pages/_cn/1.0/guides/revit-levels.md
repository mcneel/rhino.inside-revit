---
title: Levels
order: 46
thumbnail: /static/images/guides/revit-levels.png
subtitle: Workflows for Revit Levels
group: Modeling
---

## 查询标高

{% capture api_note %}
Revit API 中使用 {% include api_type.html type='Autodesk.Revit.DB.Level' title='DB.Level' %} 来表达所有的标高类型，{{ site.terms.rir }} 中使用 {% include ltr/comp.html uuid='3238f8bc' %} 运算器来表达一个标高。
{% include ltr/api_note.html note=api_note %}

### 选择现有的标高

可以使用 {% include ltr/comp.html uuid="bd6a74f3-" %} 运算器来选择项目中的所有标高,

![]({{ "/static/images/guides/revit-level-picker.png" | prepend: site.baseurl }})

也可以使用 {% include ltr/comp.html uuid="3238f8bc-" %}  运算器的标高参数来进行筛选，鼠标右键点击{% include ltr/comp.html uuid="3238f8bc-" %}选择不同的标高功能参数即可

![]({{ "/static/images/guides/revit-level-component-rc.png" | prepend: site.baseurl }})

### 以条件筛选标高

使用 {% include ltr/comp.html uuid="87715caf-" %} 运算器可以依据标高系统参数来筛选,

![]({{ "/static/images/guides/revit-level-query.png" | prepend: site.baseurl }})

## 查询标高类型

请浏览 [Modifying Types]({{ site.baseurl }}{% link _en/1.0/guides/revit-types.md %}) 来了解如何查询标高类型信息

## 分析标高

### 标高标识

可以使用 {% include ltr/comp.html uuid="e996b34d-" %} 运算器来检查标准标高属性

![]({{ "/static/images/guides/revit-level-identity.png" | prepend: site.baseurl }})

### 获取标高平面

可以将 Level 运算器链接至工作平面运算器以获取标高所在高度的XY平面

![]({{ "/static/images/guides/revit-level-component.png" | prepend: site.baseurl }})

## 创建标高

可以使用 {% include ltr/comp.html uuid="c6dec111-" %} 运算器在项目中新增一个标高

![]({{ "/static/images/guides/revit-level-add.png" | prepend: site.baseurl }})
