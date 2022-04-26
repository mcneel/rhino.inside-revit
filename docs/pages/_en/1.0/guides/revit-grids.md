---
title: Grids
order: 45
thumbnail: /static/images/guides/revit-wip.png
group: Modeling
---

## Querying Grids

{% capture api_note %}
In Revit API, Grids of all types are represented by the {% include api_type.html type='Autodesk.Revit.DB.Grid' title='DB.Grid' %}. The {% include ltr/comp.html uuid='7d2fb886' %} primitive in {{ site.terms.rir }} represents a Grid.
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### Pick Existing Grid

The {% include ltr/comp.html uuid="218fdacd-" %} component provides a list of all the Grids in the project. These can be refined using the Name, Elevation or Filter inputs.

![]({{ "/static/images/guides/revit-grid-query.png" | prepend: site.baseurl }})

You can also use the Grid Parameter to choose an existing grid. Right-click the {% include ltr/comp.html uuid="7d2fb886-" %} primitive gives access to various Grid functions.

![]({{ "/static/images/guides/revit-grid_right_click.png" | prepend: site.baseurl }})

## Querying Grid Types

See [Modifying Types]({{ site.baseurl }}{% link _en/1.0/guides/revit-types.md %}) for getting Grid Type Information. 

## Analyzing Grids

### Extract Grid Curve

Passing a Grid, to a Grasshopper Curve component, will extract the curve of that grid:

![]({{ "/static/images/guides/revit-grid_to_curve.png" | prepend: site.baseurl }})

## Creating Grids

The Component {% include ltr/comp.html uuid="cec2b3df-" %} requires a Line or Arc, Type & Name inputs are optional. 

![]({{ "/static/images/guides/revit-grid-add-by-curve.png" | prepend: site.baseurl }})





