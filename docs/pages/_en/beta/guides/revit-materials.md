---
title: Materials
order: 49
group: Modeling
---

Materials are one of the more complicated data types in Revit. They are regularly used to (a) assign graphical properties to Revit elements for drafting (e.g. tile pattern on a bathroom wall), (b) embed architectural finish information in the building model for the purpose of scheduling and takeouts, (c) assign rendering properties to surfaces for architectural visualizations, and (d) assign physical and (e) thermal properties to elements for mathematical analysis of all kinds.

Therefore a single Material in Revit has 5 main aspects:

- **Identity**
- **Graphics**
- **Rendering Appearance**
- **Physical Properties**
- **Thermal Properties**

Each one of these aspects is represented by a tab in the Revit material editor window:

![](https://via.placeholder.com/800x100.png?text=Material+Aspects+Tabs)

In the sections below, we will discuss how to deal with all of these 5 aspects using {{ site.terms.rir }}

## Querying Materials

{% capture api_note %}
In Revit API, Materials are represented by the {% include api_type.html type='Autodesk.Revit.DB.Material' title='DB.Material' %}. The {% include api_type.html type='Autodesk.Revit.DB.Material' title='DB.Material' %} type in Revit API, handles the *Identity* and *Graphics* of a material and provides methods to query and modify the *Rendering*, *Physical*, and *Thermal* properties.
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The first challenge is to be able to query available materials in a Revit model or find a specific we want to work with. For this we use the {% include ltr/comp.html uuid='94af13c1-' %} component. The component outputs all the materials in a Revit model by default, and also has optional inputs to filter the existing materials by class or name, and also accepts customs filters as well:

![](https://via.placeholder.com/800x300.png?text=Query+Materials)

{% capture tip_note %}
The Class and Name inputs accept Grasshopper string filtering patterns. See ************
{% endcapture %}
{% include ltr/bubble_note.html note=tip_note %}

### Extracting Materials from Geometry

To extract the set of materials assigned to faces of a geometry, use the *Geometry Materials* component shared here. In this example, a custom component is used to extract the geometry objects from Revit API ({% include api_type.html type='Autodesk.Revit.DB.Solid' title='DB.Solid' %} - See [Extracting Type Geometry by Category]({{ site.baseurl }}{% link _en/beta/guides/revit-types.md %}#extracting-type-geometry-by-category)). These objects are then passed to the *Geometry Materials* component to extract materials. Finally the *Element.Decompose* component is used to extract the material name.

![]({{ "/static/images/guides/revit-materials01.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Geometry Materials.ghuser' name='Geometry Materials' %}

## Material Identity and Graphics

{% include ltr/en/wip_note.html %}

Use the {% include ltr/comp.html uuid='06e0cf55-' %} component to access the material identity and graphics:

![](https://via.placeholder.com/800x300.png?text=Material+Id)

![](https://via.placeholder.com/800x300.png?text=Material+Graphics)

### Modifying Material Identity

### Customizing Material Graphics

## Creating Materials

To quickly create a material using a single color input use the {% include ltr/comp.html uuid='273ff43d-' %} component. This component has been created to help with quickly color coding Revit elements. Avoid using this component on final BIM models since the material is named by the color that is used to create it. {% include ltr/comp_doc.html uuid='273ff43d-' %}

![](https://via.placeholder.com/800x300.png?text=Add+Color+Material)

A better way to create materials is to use the {% include ltr/comp.html uuid='0d9f07e2-' %} component. This ways you can assign an appropriate name to the component:

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

uhm {% include ltr/comp.html uuid='af2678c8-' %} explain creating assets and assigning to a material using {% include ltr/comp.html uuid='2f1ec561-' %}. Show that the {% include ltr/comp.html uuid='6f5d09c7-' %} and {% include ltr/comp.html uuid='c907b51e-' %} could be used as inputs

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
