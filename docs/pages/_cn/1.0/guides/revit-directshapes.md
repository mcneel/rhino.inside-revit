---
title: Directshapes
order: 59
thumbnail: /static/images/guides/revit-directshapes.png
subtitle: Workflows for Revit DirectShapes
group: Modeling
---

在 Revit 文档中 DirectShape 图元用于存储几何形状，包括点与封闭的线、实体或是网格。DirectShapes 可以在模型中快速放置非预置族的图元，也可以部分的包含一些可加载族、类型的信息，这里提供一个别表供你参考，

<i class="fa fa-envelope">

<table>
<colgroup>
<col>
<col width="20%" style="align:center">
<col width="20%" style="align:center">
<col width="20%" style="align:center">
</colgroup>
<thead>
<tr class="header">
<th>Property</th>
<th>DirectShape</th>
<th>DirectShape Types</th>
<th>Component Family</th>
</tr>
</thead>
<tbody>
<tr>
<td markdown="span">Can be Hosted</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Work Plane Based</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Always Placed in Relation to 0,0,0</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
</tr>
<tr>
<td markdown="span">Nested Families</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Can be Placed in Assembly</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Material Parameter</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-grey.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-grey.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Material Assigned as Paint by Default</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
</tr>
<tr>
<td markdown="span">Align Material</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Assign Subcategory</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">View Based Representation</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-grey.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-grey.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Name Appears in Schedules</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-grey.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Multiple Type Instances for Schedules</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-grey.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Instance Parameters</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Parameter Driven Geometry</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Edit Geometry in Revit</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Appears in Browser</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
</tbody>
</table>

上面的一些选项是灰色的，因为要添加材质，需要使用可以计划的方式来创建额外的共享参数，这些参数都没有内置至默认的 Directshapes 中。

{% include youtube_player.html id="qPDKA6yN-2c" %}

## DirectShape 类别

使用 {% include ltr/comp.html uuid='7bafe137' %}  运算器来获取 DirectShape 类别,

![]({{ "/static/images/guides/revit-directshape-categories.png" | prepend: site.baseurl }})

## 增加 Point DirectShape

可以使用 {% include ltr/comp.html uuid='7a889b89' %} 运算器增加 Point Directshape至项目中，

![]({{ "/static/images/guides/revit-directshape-addpoint.png" | prepend: site.baseurl }})

## 增加 Mesh DirectShape

使用 {% include ltr/comp.html uuid='5542506a' %} 运算器来增加 Mesh DirectShape至项目,

![]({{ "/static/images/guides/revit-directshape-addmesh.png" | prepend: site.baseurl }})

## 增加 DirectShape

使用 {% include ltr/comp.html uuid='a811efa4' %} 运算器在项目中增加 DirectShape,

![]({{ "/static/images/guides/revit-directshape-adddirectshape.png" | prepend: site.baseurl }})

## 增加 Geometry DirectShape

使用 {% include ltr/comp.html uuid='0bfbda45' %} 在项目中增加 Geometry DirectShape,

![]({{ "/static/images/guides/revit-directshape-addgeometry.png" | prepend: site.baseurl }})

## 增加 Curve DirectShape

使用 {% include ltr/comp.html uuid='77f4fbdd' %} 运算器在项目中增加 Curve DirectShape,

![]({{ "/static/images/guides/revit-directshape-addcurve.png" | prepend: site.baseurl }})

## 增加 Brep DirectShape

使用 {% include ltr/comp.html uuid='5ade9ae3' %}  运算器增加 Brep DirectShape 至项目

![]({{ "/static/images/guides/revit-directshape-addbrep.png" | prepend: site.baseurl }})

## 增加 DirectShape 类型

使用 {% include ltr/comp.html uuid='25dcfe8e' %} 运算器增加 DirectShape 类型至项目

![]({{ "/static/images/guides/revit-directshape-addtype.png" | prepend: site.baseurl }})
