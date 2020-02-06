---
title: Spatial Elements
order: 42
---

{% include ltr/en/wip_note.html %}


## Getting Spatial Element Geometry

To grab the most accurate geometry of a spatial element, use the custom *Analyse Spatial Element* and *Boundary Location* components shared here.

![]({{ "/static/images/guides/revit-spatial01.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Boundary Location.ghuser' name='Boundary Location' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Analyse Spatial Element.ghuser' name='Analyse Spatial Element' %}

Make sure that the *Area and Volume* is checked under *Area and Volume Computations* configuration in your Revit model. Otherwise room geometry is not going to be correctly bound at the top and bottom.

![]({{ "/static/images/guides/revit-spatial03.png" | prepend: site.baseurl }})

Here is an example of using this workflow to get geometry of rooms:

![]({{ "/static/images/guides/revit-spatial02.gif" | prepend: site.baseurl }})
