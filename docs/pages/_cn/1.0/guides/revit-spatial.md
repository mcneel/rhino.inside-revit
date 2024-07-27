---
title: Spatial Elements
order: 43
thumbnail: /static/images/guides/revit-spatial.png
subtitle: Workflows for Revit Spatial Elements (Rooms, Areas, and Spaces)
group: Modeling
---

{% capture api_note %}
在 Revit API中使用 {% include api_type.html type='Autodesk.Revit.DB.SpatialElement' title='DB.SpatialElement' %} 来表示，它也可以用于创建自定义空间类，例如 *Rooms* ({% include api_type.html type='Autodesk.Revit.DB.Architecture.Room' title='DB.Architecture.Room' %}), *Spaces* ({% include api_type.html type='Autodesk.Revit.DB.Mechanical.Space' title='DB.Mechanical.Space' %}), 与 *Areas* ({% include api_type.html type='Autodesk.Revit.DB.Area' title='DB.Area' %}). `DB.Space` 可以被 *HVAC Zones* ({% include api_type.html type='Autodesk.Revit.DB.Mechanical.Zone' title='DB.Mechanical.Zone' %}所群组)
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

## 查询房间

使用 {% include ltr/comp.html uuid="5ddcb816-" %}运算器可以查询或按照条件过滤所有房间 
![]({{ "/static/images/guides/Revit-Spatial-Query-Rooms.png" | prepend: site.baseurl }})

## 增加房间分割线

 {% include ltr/comp.html uuid="34186815-" %} 运算器可以向视图添加单独的分割线
![]({{ "/static/images/guides/Revit-Spatial-Add-Room-Seperation.png" | prepend: site.baseurl }})

## 增加房间

 {% include ltr/comp.html uuid="de5e832b-" %} 运算器可以根据一个视图的给定位置添加房间
![]({{ "/static/images/guides/Revit-Spatial-Add-Room.png" | prepend: site.baseurl }})

&nbsp;

## 查询空间

利用 {% include ltr/comp.html uuid="a1ccf034-" %} 运算器可以查询或已经条件筛选空间

![]({{ "/static/images/guides/Revit-Spatial-Query-Spaces.png" | prepend: site.baseurl }})

## 增加空间分割线

 {% include ltr/comp.html uuid="dea31165-" %}  运算器可以向视图添加独立的空间分割线

![]({{ "/static/images/guides/Revit-Spatial-Add-Space-Seperation.png" | prepend: site.baseurl }})

## 增加空间

{% include ltr/comp.html uuid="07711559-" %} 运算器可以向一个视图的给定位置增加空间

![]({{ "/static/images/guides/Revit-Spatial-Add-Space.png" | prepend: site.baseurl }})

&nbsp;

## 查询区域计划

利用 {% include ltr/comp.html uuid="3e2a753b-" %} 可以查询或依据条件筛选区域计划
![]({{ "/static/images/guides/Revit-Spatial-Query-Area-Schemes.png" | prepend: site.baseurl }})

## 查询区域

使用 {% include ltr/comp.html uuid="d1940eb3-" %} 运算器可以查询或依据条件筛选所有区域
![]({{ "/static/images/guides/Revit-Spatial-Query-Areas.png" | prepend: site.baseurl }})

## 增加区域边界线

使用 {% include ltr/comp.html uuid="34d68cdc-" %} 运算器来添加单独的区域边界线

![]({{ "/static/images/guides/Revit-Spatial-Add-Area-Boundary.png" | prepend: site.baseurl }})

## 添加区域

使用 {% include ltr/comp.html uuid="2ee360f3-" %} 向视图添加区域

![]({{ "/static/images/guides/Revit-Spatial-Add-Area.png" | prepend: site.baseurl }})

&nbsp;

## 分析实例空间

在一个族（例如门）中开启计算房间计算点的 Revit 选项时，利用 {% include ltr/comp.html uuid="6ac37380-" %} 运算器可以返回关联的空间信息

![]({{ "/static/images/guides/Revit-Spatial-Analyze-Instance-space.png" | prepend: site.baseurl }})

## 空间图元标识

利用 {% include ltr/comp.html uuid="e3d32938-" %} 运算器可以获取空间图元属性

![]({{ "/static/images/guides/Revit-Spatial-Spatial-Element-Identity.png" | prepend: site.baseurl }})

## 空间图元边界

利用 {% include ltr/comp.html uuid="419062df-" %} 运算器可以获取空间图元边界

![]({{ "/static/images/guides/Revit-Spatial-Spatial-Element-Boundary.png" | prepend: site.baseurl }})

## 空间图元几何

利用 {% include ltr/comp.html uuid="a1878f3d-" %} 运算器可以获取空间图元几何

![]({{ "/static/images/guides/Revit-Spatial-Spatial-Element-Geometry.png" | prepend: site.baseurl }})