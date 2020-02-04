---
title: Views
order: 62
---

## Querying Views

{% capture api_note %}
In Revit API, Views of all types are represented by the {% include api_type.html type='Autodesk.Revit.DB.View' title='DB.View' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

You can use the combination of *Element.ClassFilter*, and *Document.ElementTypes* components to collect views:

![]({{ "/static/images/guides/revit-views01.png" | prepend: site.baseurl }})

Notice that the *Element.ClassFilter* requires the full name of the API class as string input e.g. `Autodesk.Revit.DB.View`

## Querying Views by Type

{% capture api_note %}
In Revit API, View Types are represented by the {% include api_type.html type='Autodesk.Revit.DB.ViewType' title='DB.ViewType' %} enumeration
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

## Finding Specific Views

To find a view by name or by id in the active document, use the *Find View* component shared here.

![]({{ "/static/images/guides/revit-views02.png" | prepend: site.baseurl }})

As shown above, the *Find View* component, can search for a view by its name (N) or Title on Sheet (TOS).

{% include ltr/download_comp.html archive='/static/ghnodes/Find View.ghuser' name='Find View' %}

## Reading View Properties

Use the *Element.Decompose* component to inspect the properties of a view object.

![]({{ "/static/images/guides/revit-views03.png" | prepend: site.baseurl }})

## View Range

## Collecting Displayed Elements

To collect all the elements owned by a view, use the *Element.OwnerViewFilter* component, passed to the *Document.Elements* as shown below. Keep in mind that the 3D geometry that is usually shown in model views are not "Owned" by that view.

![]({{ "/static/images/guides/revit-views04.png" | prepend: site.baseurl }})

You can use the *Element.SelectableInViewFilter* component to only list the selectable elements on a view.

![]({{ "/static/images/guides/revit-views05.png" | prepend: site.baseurl }})

## Extracting Displayed Geometry

## Getting Visibility/Graphics Overrides

## Setting Visibility/Graphics Overrides

![]({{ "/static/images/guides" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes' name='' %}

## Creating New Views

### Floor Plans

### Reflected Ceiling Plans

### Elevations

### Sections

### Area Plans

### Legends

### Detail Views
