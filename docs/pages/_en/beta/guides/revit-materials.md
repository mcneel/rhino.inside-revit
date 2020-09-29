---
title: Materials
order: 49
group: Modeling
---

## Querying Materials

{% capture api_note %}
In Revit API, Materials are represented by the {% include api_type.html type='Autodesk.Revit.DB.Material' title='DB.Material' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

- explain {% include ltr/comp.html uuid='94af13c1-' %} component
- name can filter for a specific material and accepts text filters e.g. ";Glass"
- class to filter by class, same as name
- accepts more complex filters

![](https://via.placeholder.com/800x300.png?text=Query+Materials)

### Extracting Materials from Geometry

To extract the set of materials assigned to faces of a geometry, use the *Geometry Materials* component shared here. In this example, a custom component is used to extract the geometry objects from Revit API ({% include api_type.html type='Autodesk.Revit.DB.Solid' title='DB.Solid' %} - See [Extracting Type Geometry by Category]({{ site.baseurl }}{% link _en/beta/guides/revit-types.md %}#extracting-type-geometry-by-category)). These objects are then passed to the *Geometry Materials* component to extract materials. Finally the *Element.Decompose* component is used to extract the material name.

![]({{ "/static/images/guides/revit-materials01.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Geometry Materials.ghuser' name='Geometry Materials' %}

## Material Identity and Graphics

{% include ltr/en/wip_note.html %}

- explain using {% include ltr/comp.html uuid='06e0cf55-' %} to get material identity and graphics

![](https://via.placeholder.com/800x300.png?text=Material+Id)

![](https://via.placeholder.com/800x300.png?text=Material+Graphics)

## Creating Materials

- explain {% include ltr/comp.html uuid='273ff43d-' %} for quick materials. explain the pitfalls

![](https://via.placeholder.com/800x300.png?text=Add+Color+Material)

- explain {% include ltr/comp.html uuid='0d9f07e2-' %}

![](https://via.placeholder.com/800x300.png?text=Add+Material)

## Advanced Materials with Assets

{% capture api_note %}
explain assets
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

- explain assets
- explain asset property

![](https://via.placeholder.com/800x300.png?text=Asset+Props+Screenshot)

- show {% include ltr/comp.html uuid='1f644064-' %} to extract material assets

![](https://via.placeholder.com/800x300.png?text=Extract+Assets)

- show {% include ltr/comp.html uuid='2f1ec561-' %} to replace the existing assets

![](https://via.placeholder.com/800x300.png?text=Replace+Assets)

## Shader (Appearance) Assets

{% capture api_note %}
explain shader assets
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

{% include ltr/comp.html uuid='0f251f87-' %} explain creating shader and assigning to a material using {% include ltr/comp.html uuid='2f1ec561-' %}.

![](https://via.placeholder.com/800x300.png?text=Create+Shader)

show {% include ltr/comp.html uuid='5b18389b-' %} and {% include ltr/comp.html uuid='73b2376b-' %} with example

![](https://via.placeholder.com/800x300.png?text=Modify+Analyze+Shader)

## Texture Assets

{% capture api_note %}
explain texture assets
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

{% include ltr/comp.html uuid='37b63660-' %} explain creating shader and then show and example of this being used in the {% include ltr/comp.html uuid='0f251f87-' %}

![](https://via.placeholder.com/800x300.png?text=Construct+Apply+Texture)

show {% include ltr/comp.html uuid='77b391db-' %} to extract texture info from a map applied to a material

![](https://via.placeholder.com/800x300.png?text=Deconstruct+Texture)


## Physical (Structural) Assets

{% capture api_note %}
explain struct assets
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

{% include ltr/comp.html uuid='af2678c8-' %} explain creating assets and assigning to a material using {% include ltr/comp.html uuid='2f1ec561-' %}. Show that the {% include ltr/comp.html uuid='6f5d09c7-' %} and {% include ltr/comp.html uuid='c907b51e-' %} could be used as inputs

![](https://via.placeholder.com/800x300.png?text=Create+Asset)

show {% include ltr/comp.html uuid='ec93f8e0-' %} and {% include ltr/comp.html uuid='67a74d31-' %} with example

![](https://via.placeholder.com/800x300.png?text=Modify+Analyze+Asset)

## Thermal Assets

{% capture api_note %}
explain thermal assets
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

{% include ltr/comp.html uuid='bd9164c4-' %} explain creating assets and assigning to a material using {% include ltr/comp.html uuid='2f1ec561-' %}. Show that the {% include ltr/comp.html uuid='9d9d0211-' %} and {% include ltr/comp.html uuid='c907b51e-' %} could be used as inputs

![](https://via.placeholder.com/800x300.png?text=Create+Asset)

show {% include ltr/comp.html uuid='c3be363d-' %} and {% include ltr/comp.html uuid='2c8f541a-' %} with example

![](https://via.placeholder.com/800x300.png?text=Modify+Analyze+Asset)
