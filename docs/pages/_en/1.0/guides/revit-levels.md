---
title: Levels
order: 46
thumbnail: /static/images/guides/revit-wip.png
group: Modeling
---

## Querying Levels

{% capture api_note %}
In Revit API, Levels of all types are represented by the {% include api_type.html type='Autodesk.Revit.DB.Level' title='DB.Level' %}. The {% include ltr/comp.html uuid='3238f8bc' %} primitive in {{ site.terms.rir }} represents a Revel.
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### Pick Existing Level

The {% include ltr/comp.html uuid="bd6a74f3-" %} component provides a searchable list of all project Levels.

![]({{ "/static/images/guides/revit-level-picker.png" | prepend: site.baseurl }})

You can also use the Level Parameter to choose an existing level. Right-click the {% include ltr/comp.html uuid="3238f8bc-" %} primitive gives access to various Level functions.

![]({{ "/static/images/guides/revit-level-component-rc.png" | prepend: site.baseurl }})

### Query Levels by Criteria

The {% include ltr/comp.html uuid="87715caf-" %} component allows you to filter a Level by its System properties.

![]({{ "/static/images/guides/revit-level-query.png" | prepend: site.baseurl }})

## Query Level Types

See [Modifying Types]({{ site.baseurl }}{% link _en/1.0/guides/revit-types.md %}) for getting Level Type Information. 

## Analyzing Levels

### Level Identity

Provided a Level, the {% include ltr/comp.html uuid="e996b34d-" %} component inspects the standard level properties.

![]({{ "/static/images/guides/revit-level-identity.png" | prepend: site.baseurl }})

### Getting Level Plane

Passing a Level to a Plane component, will extract the XY plane at the levels' elevation.

![]({{ "/static/images/guides/revit-level-component.png" | prepend: site.baseurl }})

## Creating Levels

Given an Elevation the {% include ltr/comp.html uuid="c6dec111-" %} will add a Level to the Project.

![]({{ "/static/images/guides/revit-level-add.png" | prepend: site.baseurl }})

