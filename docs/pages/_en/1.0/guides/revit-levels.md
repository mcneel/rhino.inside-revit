---
title: Levels
order: 46
thumbnail: /static/images/guides/revit-wip.png
group: Modeling
---


{% capture api_note %}
In Revit API, Levels of all types are represented by the {% include api_type.html type='Autodesk.Revit.DB.Level' title='DB.Level' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

## Levels Picker

The {% include ltr/comp.html uuid="bd6a74f3-" %} Component provides a searchable list of all project Levels.

![]({{ "/static/images/guides/revit-level-picker.png" | prepend: site.baseurl }})

## Add Level

Given an Elevation the {% include ltr/comp.html uuid="c6dec111-" %} will add a Level to the Project.

![]({{ "/static/images/guides/revit-level-add.png" | prepend: site.baseurl }})

## Level Identity
Provided a Level the {% include ltr/comp.html uuid="e996b34d-" %} Component returns the System properties.

![]({{ "/static/images/guides/revit-level-identity.png" | prepend: site.baseurl }})

## Query Levels

The {% include ltr/comp.html uuid="87715caf-" %} Component allows you to filter a Level by its System properties.

![]({{ "/static/images/guides/revit-level-query.png" | prepend: site.baseurl }})

## Level Component
The {% include ltr/comp.html uuid="3238f8bc-" %} Component will Cast to an XY Plane at the Levels Elevation.

![]({{ "/static/images/guides/revit-level-component.png" | prepend: site.baseurl }})

Right clicking the {% include ltr/comp.html uuid="3238f8bc-" %} Component gives access to various Level functions. 
![]({{ "/static/images/guides/revit-level-component-rc.png" | prepend: site.baseurl }})

## Level Filters
The {% include ltr/comp.html uuid="b534489b-" %} allows you to Filter Project Elements by Level.

![]({{ "/static/images/guides/revit-level-filters.png" | prepend: site.baseurl }})

## Level Symbol

See [Modifying Types]({{ site.baseurl }}{% link _en/beta/guides/revit-types.md %}) for getting Level Symbol Information. 