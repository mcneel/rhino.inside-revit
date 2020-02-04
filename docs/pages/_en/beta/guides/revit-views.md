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

To collect views of a certain type in a model, use a combination of *View Types* and *Views by Type* components shared here.

![]({{ "/static/images/guides/revit-views01a.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/View Types.ghuser' name='View Types' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Views By Type.ghuser' name='Views By Type' %}

## Finding Specific Views

To find a view by name or by id in the active document, use the *Find View* component shared here.

![]({{ "/static/images/guides/revit-views02.png" | prepend: site.baseurl }})

As shown above, the *Find View* component, can search for a view by its name (N) or Title on Sheet (TOS).

{% include ltr/download_comp.html archive='/static/ghnodes/Find View.ghuser' name='Find View' %}

## Reading View Properties

Use the *Element.Decompose* component to inspect the properties of a view object.

![]({{ "/static/images/guides/revit-views03.png" | prepend: site.baseurl }})

## View Range

{% capture api_note %}
In Revit API, View Ranges are represented by the {% include api_type.html type='Autodesk.Revit.DB.PlanViewRange' title='DB.PlanViewRange' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

To read the view range property of a view, use the *Get View Range* component shared here.

![]({{ "/static/images/guides/revit-views03a.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Get View Range.ghuser' name='Get View Range' %}


## Collecting Displayed Elements

To collect all the elements owned by a view, use the *Element.OwnerViewFilter* component, passed to the *Document.Elements* as shown below. Keep in mind that the 3D geometry that is usually shown in model views are not "Owned" by that view. All 2d elements e.g. Detail items, detail lines, ... are owned by the view they have created on.

![]({{ "/static/images/guides/revit-views04.png" | prepend: site.baseurl }})

You can use the *Element.SelectableInViewFilter* component to only list the selectable elements on a view.

![]({{ "/static/images/guides/revit-views05.png" | prepend: site.baseurl }})


## Getting V/G Overrides

To get the Visibility/Graphics overrides for an element on a specific view, use the shared *Get VG* component.

![]({{ "/static/images/guides/revit-views06.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Get Override VG.ghuser' name='Get Override VG' %}
{% include ltr/download_comp.html archive='/static/ghnodes/VG (Destruct).ghuser' name='VG (Destruct)' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Line VG Settings (Destruct).ghuser' name='Line VG Settings (Destruct)' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Surface VG Settings (Destruct).ghuser' name='Surface VG Settings (Destruct)' %}

## Setting V/G Overrides

To set the Visibility/Graphics overrides for an element on a specific view, use the shared *Set VG* component.

![]({{ "/static/images/guides/revit-views07.png" | prepend: site.baseurl }})

See [Styles and Patterns]({{ site.baseurl }}{% link _en/beta/guides/revit-styles.md %}) on how to use the *Find Line Pattern* and *Find Fill Pattern* custom components.

{% include ltr/download_comp.html archive='/static/ghnodes/Set Override VG.ghuser' name='Set Override VG' %}
{% include ltr/download_comp.html archive='/static/ghnodes/VG (Construct).ghuser' name='VG (Construct)' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Line VG Settings (Construct).ghuser' name='Line VG Settings (Construct)' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Surface VG Settings (Construct).ghuser' name='Surface VG Settings (Construct)' %}

## Creating New Views

### Floor Plans

### Reflected Ceiling Plans

### Elevations

### Sections

### Area Plans

### Legends

### Detail Views
