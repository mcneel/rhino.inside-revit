---
title: Schedules & Reports
order: 65
thumbnail: /static/images/guides/revit-schedules.png
subtitle: Workflows for Revit Schedules
group: Documentation
---

## 查询明细表

{% capture api_note %}
在 Revit 的API 中明细表是一种视图 ({% include api_type.html type='Autodesk.Revit.DB.View' title='DB.View' %}) 类型，所以使用 {% include api_type.html type='Autodesk.Revit.DB.ViewSchedule' title='DB.ViewSchedule' %}. 来表达，明细表有不同的呈现方式，它们并不是以图形方式显示图元，而是根据进度设置以电子表格样式列出图元及其属性。在 Rhino.Inside.Revit 中使用 {% include ltr/comp.html uuid='2dc4b866' %} primitive in {{ site.terms.rir }} 运算器来表达所有类型视图。
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

![]({{ "/static/images/guides/revit-schedules01.png" | prepend: site.baseurl }})

{% include ltr/warning_note.html note='在 Revit 中主图例（虽然名为图例）实际上是明细表，你可以通过检查 `DB.ViewSchedule.IsInternalKeynoteSchedule` 属性来查看明细表是否是关键进度' %}

## 查询明细表类型

因为明细表其实是视图，所以我们可以使用与[视图]({{ site.baseurl }}{% link _en/1.0/guides/revit-views.md %})相同的流程来查询项目中的明细表类型

![]({{ "/static/images/guides/revit-schedules02.png" | prepend: site.baseurl }})

### 查找指定明细表类型

![]({{ "/static/images/guides/revit-schedules03.png" | prepend: site.baseurl }})

### 以类型筛选明细表

![]({{ "/static/images/guides/revit-schedules04.png" | prepend: site.baseurl }})

### 查找指定明细表

![]({{ "/static/images/guides/revit-schedules05.png" | prepend: site.baseurl }})
