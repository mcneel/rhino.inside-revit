---
title: Grids
order: 45
thumbnail: /static/images/guides/revit-wip.png
group: Modeling
---

## Querying Grids

{% capture api_note %}
In Revit API, Grids of all types are represented by the {% include api_type.html type='Autodesk.Revit.DB.Grid' title='DB.Grid' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

The Component {% include ltr/comp.html uuid="218fdacd-" %} provides a list of all the Grids in the project. These can be refined using the Name, Elevation or Filter inputs.

![]({{ "/static/images/guides/revit-grid-query.png" | prepend: site.baseurl }})

## Adding Grids

The Component {% include ltr/comp.html uuid="cec2b3df-" %} requires a Line or Arc, Type & Name inputs are optional. 

![]({{ "/static/images/guides/revit-grid-add-by-curve.png" | prepend: site.baseurl }})


## Grid Component

All Grid Components will Cast to a Grasshopper Curve.

![]({{ "/static/images/guides/revit-grid_to_curve.png" | prepend: site.baseurl }})

Right Clicking the {% include ltr/comp.html uuid="7d2fb886-" %} Component gives access to various Grid functions as well.

![]({{ "/static/images/guides/revit-grid_right_click.png" | prepend: site.baseurl }})

## Grid Symbol

See [Modifying Types]({{ site.baseurl }}{% link _en/beta/guides/revit-types.md %}) for getting Grid Symbol Information. 





