---
title: Styles and Patterns
order: 91
group: Settings
thumbnail: /static/images/guides/revit-styles.png
subtitle: Workflows for Revit Styles, Line & Fill Patterns
ghdef: revit-styles.ghx
---

## Line Patterns

{% capture api_note %}
在 Revit API 中使用 {% include api_type.html type='Autodesk.Revit.DB.LinePatternElement' title='DB.LinePatternElement' %}来表达线型图案， `Solid` 是一个非常特别的线型图案且 Revit 不会为此特殊的线型图案返回一个正常的 API 类型。{{ site.terms.rir }} 中使用 {% include ltr/comp.html uuid='eb5ab657' %} primitive in {{ site.terms.rir }}表达线型图案。
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### 查找指定线型图案

在 {% include ltr/comp.html uuid='eb5ab657' %} 运算器的弹出菜单中可以找到一个指定的线型图案:

![]({{ "/static/images/guides/revit-styles-linepattern-select.png" | prepend: site.baseurl }})
![]({{ "/static/images/guides/revit-styles-linepatterns.png" | prepend: site.baseurl }})

## 填充图案

{% capture api_note %}
在 Revit 中使用 {% include api_type.html type='Autodesk.Revit.DB.FillPatternElement' title='DB.FillPatternElement' %}来表达填充图案，`Solid` 是一个非常特别的填充图案且 Revit 不会为此特殊的填充图案返回一个正常的 API 类型。
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### 查找指定填充图案

![]({{ "/static/images/guides/revit-styles-fillpattern-select.png" | prepend: site.baseurl }})
![]({{ "/static/images/guides/revit-styles-fillpatterns.png" | prepend: site.baseurl }})

### 查询填充图案

使用 {% include ltr/comp.html uuid='71c06438' %} 运算器可以根据类型与名称来查询填充图案

![]({{ "/static/images/guides/revit-pattern-query-fill.png" | prepend: site.baseurl }})

### Curve Line-Style

利用 {% include ltr/comp.html uuid='60be53c5' %} 运算器来获取与设置一个 Curve Line-Style

![]({{ "/static/images/guides/revit-curve-lineStyle.png" | prepend: site.baseurl }})