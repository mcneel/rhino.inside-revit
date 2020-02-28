---
title: Spatial Elements
order: 42
---

{% include ltr/en/wip_note.html %}

{% capture api_note %}
In Revit API, Spatial Elements are represented by the {% include api_type.html type='Autodesk.Revit.DB.SpatialElement' title='DB.SpatialElement' %}. This type is then used to create custom spatial types for Rooms ({% include api_type.html type='Autodesk.Revit.DB.Architecture.Room' title='DB.Architecture.Room' %}), Spaces ({% include api_type.html type='Autodesk.Revit.DB.Mechanical.Space' title='DB.Mechanical.Space' %}), and Areas ({% include api_type.html type='Autodesk.Revit.DB.Area' title='DB.Area' %})
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

## Querying Spatial Elements

## Querying Separation Lines

To find the separation (or boundary) lines associated with a category of spatial elements (e.g. Rooms, Areas, Spaces) use the *Separation Lines* dropdown component shared here. This component helps filtering down the list of categories to the separation lines. The output can be used with *Element.CategoryFilter* and *Document.Elements* components to grab the desired separation lines from the model.

![]({{ "/static/images/guides/revit-spatial01.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Separation Lines.ghuser' name='Separation Lines' %}

## Creating Separation Lines

### Area Boundary Lines

To create area boundary lines, use the *Create Area Boundaries* component shared here. The component needs an Area Plan and curves as input.

![]({{ "/static/images/guides/revit-spatial02.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Create Area Boundaries.ghuser' name='Create Area Boundaries' %}

## Getting Spatial Element Geometry

To grab the most accurate geometry of a spatial element, use the custom *Analyse Spatial Element* and *Boundary Location* components shared here.

![]({{ "/static/images/guides/revit-spatial03.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Boundary Location.ghuser' name='Boundary Location' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Spatial Element.ghuser' name='Analyse Spatial Element' %}

{% include ltr/warning_note.html note='Currently Revit API does not return geometry for `CoreBoundary` and `CoreCenter` boundary locations' %}

Make sure that the *Area and Volume* is checked under *Area and Volume Computations* configuration in your Revit model. Otherwise room geometry is not going to be correctly bound at the top and bottom.

![]({{ "/static/images/guides/revit-spatial04.png" | prepend: site.baseurl }})

Here is an example of using this workflow to get geometry of rooms:

![]({{ "/static/images/guides/revit-spatial05.gif" | prepend: site.baseurl }})

### Spatial Elements as Containers

Sometimes it is necessary to use the spatial elements as spatial containers, meaning that you would want a single surface separating two containers from each other and not two overlapping surfaces that you would usually get when extracting spatial element geometries.

This is an example of a geometry extracted from spatial elements. Each space has its own closed geometry. See the orange and light gray boxes representing two independent spatial elements:

![]({{ "/static/images/guides/revit-spatial06a.png" | prepend: site.baseurl }})

Using the *NonManifold Merge* component shared here, you can merge the geometries shown above into a single Brep with single faces separating the containers. See the single red surface in the image below, separating the orange and light gray containers:

![]({{ "/static/images/guides/revit-spatial06b.png" | prepend: site.baseurl }})

Make sure to grab the *Center* location lines when working with spatial containers. The extracted geometry is then passed to *NonManifold Merge* component to be merged:

![]({{ "/static/images/guides/revit-spatial07.png" | prepend: site.baseurl }})

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/NonManifold Merge.ghuser' name='NonManifold Merge' %}