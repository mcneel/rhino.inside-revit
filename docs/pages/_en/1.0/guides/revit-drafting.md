---
title: Drafting
order: 62
thumbnail: /static/images/guides/revit-drafting.png
subtitle: Workflows for Drafting in Revit
group: Documentation
---

## Creating Detail Lines

To create a Detail Line, use the {% include ltr/comp.html uuid='5a94ea62' %} component. Use the View parameter to select the view where you want to add the element, and pass the open curve as input as well:

![]({{ "/static/images/guides/revit-drafting02.png" | prepend: site.baseurl }})

## Creating Add Regions

You can create a Region by using the {% include ltr/comp.html uuid='ad88cf11' %} component. Select the view where you will create the filled region through the View parameter,and pass the profile as input. Note that the profile must be a closed loop, planar, and **horizontal**.

Take into account we can not create Masking Regions through the Revit API. However, by creating a region completely inscribed in another it will be a mask for the outer one.

![]({{ "/static/images/guides/revit-anno-addFillRegion.png" | prepend: site.baseurl }})

## Creating Texts

Texts can be added thanks to the {% include ltr/comp.html uuid='49acc84c' %} component. Use the View parameter to select the view where you want to add the element, and pass the content as input as well:

![]({{ "/static/images/guides/revit-drafting03.png" | prepend: site.baseurl }})

## Add Detail Item

Place 2D Detail Item Families with the {% include ltr/comp.html uuid='fe258116' %} component. The component requires a View, Plane or Point and a Detail Family Type.

![]({{ "/static/images/guides/revit-anno-addDetail-item.png" | prepend: site.baseurl }})

## Add Symbol

Revit Symbols can be placed with the {% include ltr/comp.html uuid='2beb60ba' %} component. The component requires a View, Plane or Point and a Symbol Family Type.

![]({{ "/static/images/guides/revit-anno-addSymbol.png" | prepend: site.baseurl }})

## Dimensions

{% capture api_note %}
In Revit API, Dimensions of all types are represented by the {% include api_type.html type='Autodesk.Revit.DB.Dimension' title='DB.Dimension' %}. The {% include ltr/comp.html uuid='bc546b0c' %} primitive in {{ site.terms.rir }} represents a Dimension.
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### Creating Linear Dimensions

Use the {% include ltr/comp.html uuid='df47c980' %} component to create a new Linear Dimension on the given references. To place the dimension select a Line as input as well:

![]({{ "/static/images/guides/revit-anno-dim-linear.png" | prepend: site.baseurl }})

### Creating Angular Dimensions

To create an Angular Dimension, use the {% include ltr/comp.html uuid='0dbe67e7' %} component based on the given references. Select an arc to place the dimension as well:

![]({{ "/static/images/guides/revit-anno-dim-angular.png" | prepend: site.baseurl }})

### Adding Spot Coordinate

To create an Angular Dimension, use the {% include ltr/comp.html uuid='449b853b' %} component based on the given references. Select an arc to place the dimension as well:

![]({{ "/static/images/guides/revit-anno-dim-spotCoordinate.png" | prepend: site.baseurl }})

### Adding Spot Elevation

Spot Elevations can be created using the {% include ltr/comp.html uuid='00c729f1' %} component. To place a valid view (not drafting or unlocked 3D), Revit reference and location are required. Right click the Revit Point component to set a Valid reference.

![]({{ "/static/images/guides/revit-anno-dim-spotElevation.png" | prepend: site.baseurl }})

## Tags

### Add Area Tag

In an Area Plan use the {% include ltr/comp.html uuid='ff951e5d' %} component to add your Tag. The minimum required for this component is a Revit Area. 

![]({{ "/static/images/guides/revit-anno-tag-area.png" | prepend: site.baseurl }})

### Add Material Tag

To create an Material Tag, give the {% include ltr/comp.html uuid='11424062' %} an Element with a Material and valid View to tag in.

![]({{ "/static/images/guides/revit-anno-tag-material.png" | prepend: site.baseurl }})

### Add Multi-Category Tag

To create an Multi-Category Tag, give the {% include ltr/comp.html uuid='e6e4a2ee' %} an Element and valid View to tag in.

![]({{ "/static/images/guides/revit-anno-tag-material.png" | prepend: site.baseurl }})

## Revisions

### Query Revisions

Get all the Documents Revisions with the {% include ltr/comp.html uuid='8ead987d' %} component.

![]({{ "/static/images/guides/revit-revisions-query.png" | prepend: site.baseurl }})

### Sheet Revisions

Get all the Sheets Revisions with the {% include ltr/comp.html uuid='2120c0fb' %} component.

![]({{ "/static/images/guides/revit-revisions-sheets.png" | prepend: site.baseurl }})

### Add Revision Cloud

Get all the Sheets Revisions with the {% include ltr/comp.html uuid='8ff70eef' %} component.

![]({{ "/static/images/guides/revit-revisions-addCloud.png" | prepend: site.baseurl }})

## Images

### Add Image

Add an image resource using the {% include ltr/comp.html uuid='506d5c19' %} component.

![]({{ "/static/images/guides/revit-image-add.png" | prepend: site.baseurl }})

### Add Image Type

Create an Image Type from you Image Resource with the {% include ltr/comp.html uuid='09bd0aa8' %} component.

![]({{ "/static/images/guides/revit-image-add-type.png" | prepend: site.baseurl }})