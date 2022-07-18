---
title: Drafting
order: 62
thumbnail: /static/images/guides/revit-drafting.png
subtitle: Workflows for Drafting in Revit
group: Documentation
---

## Creating Detail Lines

To create a Detail Line, use the {% include ltr/comp.html uuid='5a94ea62' %} component. Use the View parameter to select the view where you want to add the element, and pass the open curve as input as well:

![]({{ "/static/images/guides/revit-drafting01.png" | prepend: site.baseurl }})

## Creating Add Regions

You can create a Region by using the {% include ltr/comp.html uuid='ad88cf11' %} component. Select the view where you will create the filled region through the View parameter, and pass the profile as input. Note that the profile must be a closed loop, planar, and **horizontal**.

Take into account we can not create Masking Regions through the Revit API. However, by creating a region completely inscribed in another it will be a mask for the outer one.

![]({{ "/static/images/guides/revit-drafting02.png" | prepend: site.baseurl }})

## Creating Texts

Texts can be added thanks to the {% include ltr/comp.html uuid='49acc84c' %} component. Use the View parameter to select the view where you want to add the element, and pass the content as input as well:

![]({{ "/static/images/guides/revit-drafting03.png" | prepend: site.baseurl }})

## Dimensions

{% capture api_note %}
In Revit API, Dimensions of all types are represented by the {% include api_type.html type='Autodesk.Revit.DB.Dimension' title='DB.Dimension' %}. The {% include ltr/comp.html uuid='bc546b0c' %} primitive in {{ site.terms.rir }} represents a Dimension.
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### Creating Linear Dimensions

Use the {% include ltr/comp.html uuid='df47c980' %} component to create a new Linear Dimension on the given references. To place the dimension select a Line as input as well:

![]({{ "/static/images/guides/revit-drafting04.png" | prepend: site.baseurl }})

### Creating Angular Dimensions

To create an Angular Dimension, use the {% include ltr/comp.html uuid='0dbe67e7' %} component based on the given references. Select an arc to place the dimension as well:

![]({{ "/static/images/guides/revit-drafting05.png" | prepend: site.baseurl }})
