---
title: Directshapes
order: 59
thumbnail: /static/images/guides/revit-directshapes.png
subtitle: Workflows for Revit DirectShapes
group: Modeling
---

The DirectShape element store geometric shapes in a Revit document. The geometry can include closed points, lines, solids or meshes. DirectShape is primarily intended for placing elements quickly in a model without the need for a pre-configured family. Directshape contains some but not all the organizational information of Loadable Family/Types. See the chart below to see if directshapes might work for you.

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
<th>DirectShape Type</th>
<th>Component Family</th>
</tr>
</thead>
<tbody>
<tr>
<td markdown="span">Nested Families</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Parameter Driven Shape</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Material Parameter</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-grey.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-grey.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Align Material</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Placed in Assembly</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Name Appears in Schedules</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-grey.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Multiple Instances for Schedules</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-grey.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">View Based Representation</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-grey.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-grey.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Instance Parameters</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
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
<tr>
<td markdown="span">Assign Subcategory</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Can be Hosted</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Workplane Based</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
<tr>
<td markdown="span">Place by Origin</td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-blank.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
<td markdown="span"><img style="width:16px;height:16px;" src="/rhino.inside-revit/assets/img/checkbox-checked.svg"></td>
</tr>
</tbody>
</table>

Some of the options above are grey because to add materials, use the name in a way that can be scheduled additional shared parameters need to be created as these parameters are not built into the default Directshapes.

{% include youtube_player.html id="qPDKA6yN-2c" %}

## DirectShape Categories

Use the {% include ltr/comp.html uuid='7bafe137' %} component to get the DirectShape Categories.

![]({{ "/static/images/guides/revit-directshape-categories.png" | prepend: site.baseurl }})

## Add Point DirectShape

Use the {% include ltr/comp.html uuid='7a889b89' %} component to Add Point DirectShape in the project.

![]({{ "/static/images/guides/revit-directshape-addpoint.png" | prepend: site.baseurl }})

## Add Mesh DirectShape

Use the {% include ltr/comp.html uuid='5542506a' %} component to Add Mesh DirectShape in the project.

![]({{ "/static/images/guides/revit-directshape-addmesh.png" | prepend: site.baseurl }})

## Add DirectShape

Use the {% include ltr/comp.html uuid='a811efa4' %} component to Add DirectShape in the project.

![]({{ "/static/images/guides/revit-directshape-adddirectshape.png" | prepend: site.baseurl }})

## Add Geometry DirectShape

Use the {% include ltr/comp.html uuid='0bfbda45' %} component to Add Geometry DirectShape in the project.

![]({{ "/static/images/guides/revit-directshape-addgeometry.png" | prepend: site.baseurl }})

## Add Curve DirectShape

Use the {% include ltr/comp.html uuid='77f4fbdd' %} component to Add Curve DirectShape in the project.

![]({{ "/static/images/guides/revit-directshape-addcurve.png" | prepend: site.baseurl }})

## Add Brep DirectShape

Use the {% include ltr/comp.html uuid='5ade9ae3' %} component to Add Brep DirectShape in the project.

![]({{ "/static/images/guides/revit-directshape-addbrep.png" | prepend: site.baseurl }})

## Add DirectShape Type

Use the {% include ltr/comp.html uuid='25dcfe8e' %} component to Add DirectShape Type in the project.

![]({{ "/static/images/guides/revit-directshape-addtype.png" | prepend: site.baseurl }})



