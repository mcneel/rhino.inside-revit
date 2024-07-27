---
title: Element Geometry
order: 58
thumbnail: /static/images/guides/revit-modify.png
subtitle: Workflows for Modifying Revit Elements
group: Modeling
---

## 反转

可以使用下面分享的 Flipped 运算器来检查一个图元是否被反转（Revit 支持不同的反转类型），

![]({{ "/static/images/guides/revit-modify01.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Flipped.ghuser' name='Flipped' %}

### 查询反转过的图元

你可以使用收集运算器来查询任何类别的图元，将它们接入至 Flipped 运算器且根据需要的过滤结果：

![]({{ "/static/images/guides/revit-modify02.png" | prepend: site.baseurl }})
