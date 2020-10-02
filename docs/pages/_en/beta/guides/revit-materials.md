---
title: Materials
order: 49
group: Modeling
---

Materials are one of the more complicated data types in Revit. They are regularly used to (a) assign graphical properties to Revit elements for drafting (e.g. tile pattern on a bathroom wall), (b) embed architectural finish information in the building model for the purpose of scheduling and takeouts, (c) assign appearance properties to surfaces for architectural visualizations, and (d) assign physical and (e) thermal properties to elements for mathematical analysis of all kinds.

Therefore a single Material in Revit has 5 main aspects:

- **Identity**
- **Graphics**
- **Appearance Properties**
- **Physical Properties**
- **Thermal Properties**

Each one of these aspects is represented by a tab in the Revit material editor window:

![]({{ "/static/images/guides/revit-materials-editortabs.png" | prepend: site.baseurl }})

In the sections below, we will discuss how to deal with all of these 5 aspects using {{ site.terms.rir }}

## Querying Materials

{% capture api_note %}
In Revit API, Materials are represented by the {% include api_type.html type='Autodesk.Revit.DB.Material' title='DB.Material' %}. This type, handles the *Identity* and *Graphics* of a material and provides methods to query and modify the *Appearance*, *Physical*, and *Thermal* properties.
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The first challenge is to be able to query available materials in a Revit model or find a specific one that we want to work with. For this we use the {% include ltr/comp.html uuid='94af13c1-' %} component. The component outputs all the materials in a Revit model by default, and also has optional inputs to filter the existing materials by class or name, and also accepts customs filters as well:

![]({{ "/static/images/guides/revit-materials01.png" | prepend: site.baseurl }})

{% include ltr/filter_note.html note='The Class and Name inputs accept Grasshopper string filtering patterns' %}

![]({{ "/static/images/guides/revit-materials02.png" | prepend: site.baseurl }})

### Extracting Materials from Geometry

To extract the set of materials assigned to faces of a geometry, use the *Geometry Materials* component shared here. In this example, a custom component is used to extract the geometry objects from Revit API ({% include api_type.html type='Autodesk.Revit.DB.Solid' title='DB.Solid' %} - See [Extracting Type Geometry by Category]({{ site.baseurl }}{% link _en/beta/guides/revit-types.md %}#extracting-type-geometry-by-category)). These objects are then passed to the *Geometry Materials* component to extract materials. Finally the *Element.Decompose* component is used to extract the material name.

![]({{ "/static/images/guides/revit-materials-extract.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Geometry Materials.ghuser' name='Geometry Materials' %}

## Material Identity

Use the {% include ltr/comp.html uuid='222b42df-' %} component to access the material identity:

![](https://via.placeholder.com/800x300.png?text=Material+Id)

### Modifying Material Identity

{% include ltr/en/wip_note.html %}

## Material Graphics

Use the {% include ltr/comp.html uuid='8c5cd6fb-' %} component to access the material identity:

![](https://via.placeholder.com/800x300.png?text=Material+Graphics)

### Customizing Material Graphics

{% include ltr/en/wip_note.html %}

## Creating Materials

Use the {% include ltr/comp.html uuid='3aedba3c-' %} component to create a new material in the Revit model. You must assign a unique name to the new material:

![](https://via.placeholder.com/800x300.png?text=Create+Material)

## Material Assets

So far, we have learned how to analyze material identify and graphics, and to create simple materials. To be able to take full advantage of the materials in Revit, we need to be familiar with the underlying concepts behind the other three aspects of a material: *Appearance*, *Physical*, and *Thermal* properties.

### Assets

Assets are the underlying concept behind the *Appearance*, *Physical*, and *Thermal* aspects of a material in Revit. {{ site.terms.rir }} provides a series of components to Create, Modify, and Analyze these assets in a Grasshopper-friendly manner. It also provides components to extract and replace these assets on a Revit material.

Remember that Assets and Materials are different data types. Each Revit Material had identity and graphics properties, and also can be assigned Assets to apply *Appearance*, *Physical*, and *Thermal* properties to the Material. Having *Physical*, and *Thermal* assets is completely optional.

{% capture api_note %}
Revit API support for assets is very limited. This note section, attempts to describe the inner-workings of Revit Visual API

#### Appearance Assets

All *Appearance* assets are of type {% include api_type.html type='Autodesk.Revit.DB.Visual.Asset' title='DB.Visual.Asset' %} and are basically a collection of visual properties that have a name e.g. `generic_diffuse`, a type, and a value. The {% include api_type.html type='Autodesk.Revit.DB.Visual.Asset' title='DB.Visual.Asset' %} has lookup methods to find and return these properties. These properties are wrapped by the type {% include api_type.html type='Autodesk.Revit.DB.Visual.AssetProperty' title='DB.Visual.AssetProperty' %} in Revit API. This type provides getters to extract the value from the property.

&nbsp;

There are many different *Appearance* assets in Revit e.g. **Generic**, **Ceramic**, **Metal**, **Layered**, **Glazing** to name a few. Each asset has a different set of properties. To work with these *Appearance* assets, we need a way to know the name of the properties that are available for each of the asset types. Revit API provides static classes with static readonly string properties that provide an easy(?) way to get the name of these properties. For example the `GenericDiffuse` property of {% include api_type.html type='Autodesk.Revit.DB.Visual.Generic' title='DB.Visual.Generic' %}, returns the name `generic_diffuse` which is the name of the diffuse property for a **Generic** Appearance asset.

&nbsp;

*Appearance* assets are then wrapped by {% include api_type.html type='Autodesk.Revit.DB.AppearanceAssetElement' title='DB.AppearanceAssetElement' %} so they can be assigned to a Revit Material ({% include api_type.html type='Autodesk.Revit.DB.Material' title='DB.Material' %})

#### Physical and Thermal Assets

*Physical*, and *Thermal* assets are completely different although operating very similarly to *Appearance* assets. They are still a collection of properties, however, the properties are modeled as Revit parameters ({% include api_type.html type='Autodesk.Revit.DB.Parameter' title='DB.Parameter' %}) and are collected by an instance of {% include api_type.html type='Autodesk.Revit.DB.PropertySetElement' title='DB.PropertySetElement' %}. Instead of having static classes as accessors for the names, they must be accessed by looking up the parameter based on a built-in Revit parameter e.g. `THERMAL_MATERIAL_PARAM_REFLECTIVITY` of {% include api_type.html type='Autodesk.Revit.DB.BuiltInParameter' title='DB.BuiltInParameter' %}

&nbsp;

Revit API provides {% include api_type.html type='Autodesk.Revit.DB.StructuralAsset' title='DB.StructuralAsset' %} and {% include api_type.html type='Autodesk.Revit.DB.ThermalAsset' title='DB.ThermalAsset' %} types to provide easy access to the *Physical*, and *Thermal* properties, however, not all the properties are included in these types and the property values are not checked for validity either.

#### Grasshopper as Playground

The Grasshopper definition provided here, has custom python components that help you interrogate the properties of these assets:

&nbsp;

![]({{ "/static/images/guides/revit-materials-assetpg.png" | prepend: site.baseurl }})

&nbsp;

{% include ltr/download_def.html archive='/static/ghdefs/AssetsPlayground.ghx' name='Assets Playground' %}

{% endcapture %}
{% include ltr/api_note.html note=api_note %}

Use the {% include ltr/comp.html uuid='1f644064-' %} to extract assets of a material:

![]({{ "/static/images/guides/revit-materials05.png" | prepend: site.baseurl }})

To replace assets of a material with a different asset, use the {% include ltr/comp.html uuid='2f1ec561-' %} component:

![]({{ "/static/images/guides/revit-materials06.png" | prepend: site.baseurl }})

## Appearance Assets

There are many *Appearance* assets in Revit API. As an example, you can use {% include ltr/comp.html uuid='0f251f87-' %} to create a *Generic* appearance asset and assign that to a Revit material using the {% include ltr/comp.html uuid='2f1ec561-' %} component:

![](https://via.placeholder.com/800x300.png?text=Create+Appearance)

The {% include ltr/comp.html uuid='5b18389b-' %} and {% include ltr/comp.html uuid='73b2376b-' %} components can be used to easily manipulate an existing asset, or analyze and extract the known property values:

![](https://via.placeholder.com/800x300.png?text=Modify+Analyze+Appearance)

## Texture Assets

Appearance assets have a series of properties that can accept a nested asset (called *Texture* assets in this guide). For example, the diffuse property of a **Generic** appearance asset can either have a color value, or be connected to another asset of type **Bitmap** (or other *Texture* assets).

{{ site.terms.rir }} provides component to construct and destruct these asset types. The *Appearance* asset component also accepts a *Texture* asset where applicable. For example, use {% include ltr/comp.html uuid='37b63660-' %} and {% include ltr/comp.html uuid='77b391db-' %} to construct and destruct **Bitmap** texture assets:

![](https://via.placeholder.com/800x300.png?text=Construct+Apply+Texture)

{% capture param_note %}
The {% include ltr/param.html uuid='49a94c44-' title='Glossiness' %} and {% include ltr/param.html uuid='c2fc2e60-' title='Bump' %} parameters of **Generic** appearance components accept both a double or color value, or a texture map respectively. Note the parameter icons show a double or color value and a checker map in background
{% endcapture %}
{% include ltr/bubble_note.html note=param_note %}

{% capture construct_note %}
Note that *Construct* an *Deconstruct* texture components only pass around a data structure containing the configuration of the texture asset. They do not create anything inside the Revit model by themselves. It is the *Create Appearance Asset* component that actually creates the texture asset (when connected to an input parameter) and connects it to properties of the appearance asset it is creating. This behavior mirrors the inner-workings of 'connected' (nested) assets in Revit API.
{% endcapture %}
{% include ltr/api_note.html note=construct_note %}


## Physical Assets

Use {% include ltr/comp.html uuid='af2678c8-' %} to create a *Physical* asset and assign to a material using {% include ltr/comp.html uuid='2f1ec561-' %} component. Use {% include ltr/comp.html uuid='6f5d09c7-' %} and {% include ltr/comp.html uuid='c907b51e-' %} as inputs, to set the type and behavior of the *Physical* asset, respectively:

![](https://via.placeholder.com/800x300.png?text=Create+Asset)

Use {% include ltr/comp.html uuid='ec93f8e0-' %} and {% include ltr/comp.html uuid='67a74d31-' %} to modify or analyze existing *Physical* assets:

![](https://via.placeholder.com/800x300.png?text=Modify+Analyze+Asset)

## Thermal Assets

Use {% include ltr/comp.html uuid='bd9164c4-' %} to create a *Thermal* asset and assign to a material using {% include ltr/comp.html uuid='2f1ec561-' %} component. Use {% include ltr/comp.html uuid='9d9d0211-' %} and {% include ltr/comp.html uuid='c907b51e-' %} as inputs, to set the type and behavior of the *Thermal* asset, respectively:

![](https://via.placeholder.com/800x300.png?text=Create+Asset)

Use {% include ltr/comp.html uuid='c3be363d-' %} and {% include ltr/comp.html uuid='2c8f541a-' %} to modify or analyze existing *Thermal* assets:

![](https://via.placeholder.com/800x300.png?text=Modify+Analyze+Asset)
