---
title: Spatial Elements
order: 43
thumbnail: /static/images/guides/revit-spatial.png
subtitle: Workflows for Revit Spatial Elements (Rooms, Areas, and Spaces)
group: Modeling
---

{% capture api_note %}
In Revit API, Spatial Elements are represented by the {% include api_type.html type='Autodesk.Revit.DB.SpatialElement' title='DB.SpatialElement' %}. This type is then used to create custom spatial types for *Rooms* ({% include api_type.html type='Autodesk.Revit.DB.Architecture.Room' title='DB.Architecture.Room' %}), *Spaces* ({% include api_type.html type='Autodesk.Revit.DB.Mechanical.Space' title='DB.Mechanical.Space' %}), and *Areas* ({% include api_type.html type='Autodesk.Revit.DB.Area' title='DB.Area' %}). `DB.Space` elements can be grouped by *HVAC Zones* ({% include api_type.html type='Autodesk.Revit.DB.Mechanical.Zone' title='DB.Mechanical.Zone' %})
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

## Querying Rooms

Use the {% include ltr/comp.html uuid="5ddcb816-" %} component to gather all Rooms or filter by 
criteria. 
![]({{ "/static/images/guides/Revit-Spatial-Query-Rooms.png" | prepend: site.baseurl }})

## Add Room Separation Lines

The {% include ltr/comp.html uuid="34186815-" %} component will add individual Separation Lines to a view.
![]({{ "/static/images/guides/Revit-Spatial-Add-Room-Seperation.png" | prepend: site.baseurl }})

## Add Rooms

The {% include ltr/comp.html uuid="de5e832b-" %} component will add Rooms to a view given a location.
![]({{ "/static/images/guides/Revit-Spatial-Add-Room.png" | prepend: site.baseurl }})



&nbsp;


## Querying Spaces

Use the {% include ltr/comp.html uuid="a1ccf034-" %} to gather all Spaces or filter by 
criteria. 

![]({{ "/static/images/guides/Revit-Spatial-Query-Spaces.png" | prepend: site.baseurl }})

## Add Space Separation Lines

The {% include ltr/comp.html uuid="dea31165-" %}  component will add individual Space Separation Lines to a view.

![]({{ "/static/images/guides/Revit-Spatial-Add-Space-Seperation.png" | prepend: site.baseurl }})

## Add Spaces

The {% include ltr/comp.html uuid="07711559-" %} component will add Spaces to a view given a location.

![]({{ "/static/images/guides/Revit-Spatial-Add-Space.png" | prepend: site.baseurl }})

&nbsp;

## Querying Area Schemes

Use the {% include ltr/comp.html uuid="3e2a753b-" %} to get all Area Schemes or filter by Name.
![]({{ "/static/images/guides/Revit-Spatial-Query-Area-Schemes.png" | prepend: site.baseurl }})

## Querying Areas

Use the {% include ltr/comp.html uuid="d1940eb3-" %} to gather all Areas or filter by 
criteria. 
![]({{ "/static/images/guides/Revit-Spatial-Query-Areas.png" | prepend: site.baseurl }})

## Add Area Boundary Lines

Use the {% include ltr/comp.html uuid="34d68cdc-" %} to add individual Area Boundary Lines

![]({{ "/static/images/guides/Revit-Spatial-Add-Area-Boundary.png" | prepend: site.baseurl }})

## Add Areas

Use the {% include ltr/comp.html uuid="2ee360f3-" %} to add Areas to Views.

![]({{ "/static/images/guides/Revit-Spatial-Add-Area.png" | prepend: site.baseurl }})

&nbsp;

## Analyze Instance Space

When the Revit option to Room Calculation Point is enabled in a Family (such as doors) the {% include ltr/comp.html uuid="6ac37380-" %} component returns the associated Spatial information. 

![]({{ "/static/images/guides/Revit-Spatial-Analyze-Instance-space.png" | prepend: site.baseurl }})


## Spatial Element Identity

Get Spatial Element Properties with the {% include ltr/comp.html uuid="e3d32938-" %} component.

![]({{ "/static/images/guides/Revit-Spatial-Spatial-Element-Identity.png" | prepend: site.baseurl }})

## Spatial Element Boundary

Get Spatial Element Boundary based on its Location Property with the {% include ltr/comp.html uuid="419062df-" %} component.

![]({{ "/static/images/guides/Revit-Spatial-Spatial-Element-Boundary.png" | prepend: site.baseurl }})

## Spatial Element Geometry

Get Spatial Element Geometry with the {% include ltr/comp.html uuid="a1878f3d-" %} component.

![]({{ "/static/images/guides/Revit-Spatial-Spatial-Element-Geometry.png" | prepend: site.baseurl }})