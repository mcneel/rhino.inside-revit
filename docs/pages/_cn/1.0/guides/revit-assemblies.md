---
title: Assemblies
order: 72
thumbnail: /static/images/guides/revit-assemblies.png
subtitle: Workflows for Revit Assemblies
group: Containers
---

## 查询部件

使用 {% include ltr/comp.html uuid='fd5b45c3' %} 运算器可以查询项目中既有的所有部件

![]({{ "/static/images/guides/revit-assembly-query.png" | prepend: site.baseurl }})

{% capture api_note %}
在 Revit API 中工作集通过 {% include api_type.html type='Autodesk.Revit.DB.AssemblyInstance' title='DB.AssemblyInstance' %}.
{% endcapture %} 来表达
{% include ltr/api_note.html note=api_note %}

## 查询指定部件

使用 {% include ltr/comp.html uuid='fd5b45c3' %} 运算器可以根据指定名字来查找部件

![]({{ "/static/images/guides/revit-assembly-query-typeName.png" | prepend: site.baseurl }})

## 读取部件图元

如果需要读取或设置部件图元，请使用 {% include ltr/comp.html uuid='33ead71b' %} 运算器.

![]({{ "/static/images/guides/revit-assembly-members.png" | prepend: site.baseurl }})

## 创建一个部件

如果需要创建一个新的部件，请使用e {% include ltr/comp.html uuid='6915b697' %} 运算器.

![]({{ "/static/images/guides/revit-assembly-create.png" | prepend: site.baseurl }})

## 添加部件至项目

使用 {% include ltr/comp.html uuid='26feb2e9' %} 运算器可以在项目中根据指定位置添加部件，

![]({{ "/static/images/guides/revit-assembly-add-location.png" | prepend: site.baseurl }})

## 拆卸部件

利用 {% include ltr/comp.html uuid='ff0f49ca' %} 运算器可以拆卸部件.

![]({{ "/static/images/guides/revit-assembly-disassemble.png" | prepend: site.baseurl }})

## 部件原点

利用 {% include ltr/comp.html uuid='1c1cc766' %} 运算器可以设置或获取部件原点.

![]({{ "/static/images/guides/revit-assembly-origin.png" | prepend: site.baseurl }})
