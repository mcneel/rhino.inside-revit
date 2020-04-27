---
title: Spatial Elements
order: 42
---

{% capture api_note %}
In Revit API, Spatial Elements are represented by the {% include api_type.html type='Autodesk.Revit.DB.SpatialElement' title='DB.SpatialElement' %}. This type is then used to create custom spatial types for *Rooms* ({% include api_type.html type='Autodesk.Revit.DB.Architecture.Room' title='DB.Architecture.Room' %}), *Spaces* ({% include api_type.html type='Autodesk.Revit.DB.Mechanical.Space' title='DB.Mechanical.Space' %}), and *Areas* ({% include api_type.html type='Autodesk.Revit.DB.Area' title='DB.Area' %}). `DB.Space` elements can be grouped by *HVAC Zones* ({% include api_type.html type='Autodesk.Revit.DB.Mechanical.Zone' title='DB.Mechanical.Zone' %})
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

## Querying Spatial Elements

Use a combination of category component, connected {% include ltr/comp.html uuid="d08f7ab1-" %} to collect specific *Spatial Elements*:

![]({{ "/static/images/guides/revit-spatial01.png" | prepend: site.baseurl }})

## Querying Separation Lines

To find the separation (or boundary) lines associated with a category of spatial elements (e.g. Rooms, Areas, Spaces) use the *Separation Lines* dropdown component shared here. This component helps filtering down the list of categories to the separation lines. The output can be used with {% include ltr/comp.html uuid="d08f7ab1-" %} and {% include ltr/comp.html uuid="0f7da57e-" %} components to grab the desired separation lines from the model.

![]({{ "/static/images/guides/revit-spatial02.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Separation Lines.ghuser' name='Separation Lines' %}

## Querying Area Schemes

*Area Schemes* are container for Area element. Each *Area Schemes* can contain many Area element, host on various levels. *Area* plans are directly associated with a Level (just like any other plan view) and also a specific *Area Scheme*. This is why you would see the *Area Scheme* name in parentheses in front of the *Area* plan name e.g. **My Area Plan (Gross Building)**.

Use the **Get Area Schemes** component shared here to query the available *Area Schemes*. Note that *Area Schemes* are Elements and you can use the *Element.Identity* component to grab their Name:

![]({{ "/static/images/guides/revit-spatial03.png" | prepend: site.baseurl }})

Grabbing the name as shown above, can help filtering *Area Schemes* by their Name:

![]({{ "/static/images/guides/revit-spatial04.png" | prepend: site.baseurl }})

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Get Area Schemes.ghuser' name='Get Area Schemes' %}

{% include ltr/issue_note.html issue_id='181' note='Category picker is missing OST_AreaSchemes builtin category' %}

## Querying HVAC Zones

*HVAZ Zones* can be collected just like any other element type:

![]({{ "/static/images/guides/revit-spatial04a.png" | prepend: site.baseurl }})

## Analyzing Spatial Elements

Use the **Analyse Spatial Element** component shared here to get the common properties of *Spatial Elements*:

![]({{ "/static/images/guides/revit-spatial05.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Spatial Element.ghuser' name='Analyse Spatial Element' %}

### Filtering Spatial Elements by Level

The component shared above can be used to filter the *Spatial Elements* by level:

![]({{ "/static/images/guides/revit-spatial06.png" | prepend: site.baseurl }})

## Getting Spatial Element Geometry

To grab the most accurate geometry of a spatial element, use the custom *Analyse Spatial Element* (shared above) and *Boundary Location* components shared here.

![]({{ "/static/images/guides/revit-spatial07.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Boundary Location.ghuser' name='Boundary Location' %}

{% include ltr/warning_note.html note='Currently Revit API does not return geometry for `CoreBoundary` and `CoreCenter` boundary locations' %}

Make sure that the *Area and Volume* is checked under *Area and Volume Computations* configuration in your Revit model. Otherwise room geometry is not going to be correctly bound at the top and bottom.

![]({{ "/static/images/guides/revit-spatial08.png" | prepend: site.baseurl }})

Here is an example of using this workflow to get geometry of rooms:

![]({{ "/static/images/guides/revit-spatial09.gif" | prepend: site.baseurl }})

### Spatial Elements as Containers

Sometimes it is necessary to use the spatial elements as spatial containers, meaning that you would want a single surface separating two containers from each other and not two overlapping surfaces that you would usually get when extracting spatial element geometries.

This is an example of a geometry extracted from spatial elements. Each space has its own closed geometry. See the orange and light gray boxes representing two independent spatial elements:

![]({{ "/static/images/guides/revit-spatial10.png" | prepend: site.baseurl }})

Using the *NonManifold Merge* component shared here, you can merge the geometries shown above into a single Brep with single faces separating the containers. See the single red surface in the image below, separating the orange and light gray containers:

![]({{ "/static/images/guides/revit-spatial11.png" | prepend: site.baseurl }})

Make sure to grab the *Center* location lines when working with spatial containers. The extracted geometry is then passed to *NonManifold Merge* component to be merged:

![]({{ "/static/images/guides/revit-spatial12.png" | prepend: site.baseurl }})

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/NonManifold Merge.ghuser' name='NonManifold Merge' %}

## Analyzing Area Schemes

Use the **Analyse Area Scheme** component shared here to analyze the *Area Scheme* elements:

![]({{ "/static/images/guides/revit-spatial13.png" | prepend: site.baseurl }})

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Area Scheme.ghuser' name='Analyse Area Scheme' %}

## Analyzing Areas

Use the **Analyse Area** component shared here to analyze the *Area* elements:

![]({{ "/static/images/guides/revit-spatial15.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Area.ghuser' name='Analyse Area' %}

### Filtering Areas by Area Scheme

Use the **Analyse Area** component shared above to filter the *Area* elements by their associated *Area Scheme*:

![]({{ "/static/images/guides/revit-spatial16.png" | prepend: site.baseurl }})

## Analyzing HVAC Zones

{% include ltr/en/wip_note.html %}

## Modifying Spatial Elements

{% include ltr/en/wip_note.html %}

## Modifying Separation Lines

{% include ltr/en/wip_note.html %}

## Modifying HVAC Zones

{% include ltr/en/wip_note.html %}

## Creating Separation Lines

### Area Boundary Lines

To create area boundary lines, use the *Create Area Boundaries* component shared here. The component needs an Area Plan and curves as input.

![]({{ "/static/images/guides/revit-spatial14.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Create Area Boundaries.ghuser' name='Create Area Boundaries' %}

## Creating Spatial Elements

{% include ltr/en/wip_note.html %}

### Creating Rooms

{% include ltr/en/wip_note.html %}

### Creating Areas

{% include ltr/en/wip_note.html %}

### Creating Spaces

{% include ltr/en/wip_note.html %}

## Creating Area Schemes

{% include ltr/en/wip_note.html %}

## Creating HVAC Zones

{% include ltr/en/wip_note.html %}
