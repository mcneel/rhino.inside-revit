---
title: Materials
order: 45
---

{% include ltr/en/wip_note.html %}


## Querying Materials

{% capture api_note %}
In Revit API, Materials are represented by the {% include api_type.html type='Autodesk.Revit.DB.Material' title='DB.Material' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

## Reading Material Properties

## Extracting Materials from Geometry

To extract the set of materials assigned to faces of a geometry, use the *Geometry Materials* component shared here. In this example, a custom component is used to extract the geometry objects from Revit API ({% include api_type.html type='Autodesk.Revit.DB.Solid' title='DB.Solid' %} - See [Extracting Type Geometry by Category]({{ site.baseurl }}{% link _en/beta/guides/revit-types.md %}#extracting-type-geometry-by-category)). These objects are then passed to the *Geometry Materials* component to extract materials. Finally the *Element.Decompose* component is used to extract the material name.

![]({{ "/static/images/guides/revit-materials01.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Geometry Materials.ghuser' name='Geometry Materials' %}

## Modifying Materials

## Creating Materials

