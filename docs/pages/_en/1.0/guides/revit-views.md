---
title: Views
order: 63
thumbnail: /static/images/guides/rir-ghpython.png

group: Documentation
---

## Querying Views

{% capture api_note %}
In Revit API, Views of all types are represented by the {% include api_type.html type='Autodesk.Revit.DB.View' title='DB.View' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

You can use the combination of *Element.ClassFilter*, and {% include ltr/comp.html uuid="7b00f940-" %} components to collect views:

![]({{ "/static/images/guides/revit-views01.png" | prepend: site.baseurl }})

Notice that the *Element.ClassFilter* requires the full name of the API class as string input e.g. `Autodesk.Revit.DB.View`

{% capture issue_note %}
Add Views to category pickers so an {% include ltr/comp.html uuid="d08f7ab1-" %} can be used to list views
{% endcapture %}
{% include ltr/issue_note.html issue_id='142' note=issue_note %}

## Querying Views by System Family

{% capture api_note %}
In Revit API, View System Families are represented by the {% include api_type.html type='Autodesk.Revit.DB.ViewFamily' title='DB.ViewFamily' %} enumeration. However, there is a `ViewType` property on the `DB.View` instances that also provides the System Family of the view instance. The enumeration for this property is {% include api_type.html type='Autodesk.Revit.DB.ViewType' title='DB.ViewType' %}. `DB.ViewFamily` and `DB.ViewType` seems to have been created with the same goal in mind. The components shared here use the `DB.ViewFamily` to list the views by system family, however, the same results might be achieved using `DB.ViewType`
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

To collect views of a certain system family in a model, use a combination of *View System Families* and *Views By System Family* components shared here.

![]({{ "/static/images/guides/revit-views01a.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/View System Families.ghuser' name='View System Families' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Views By System Family.ghuser' name='Views By System Family' %}

## Querying View Types

{% capture api_note %}
In Revit API, View Types are represented by the {% include api_type.html type='Autodesk.Revit.DB.ViewFamilyType' title='DB.ViewFamilyType' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

To collect a list of view types in a model associated with a view system family, use the *View Types* component shared here.

![]({{ "/static/images/guides/revit-views01b.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/View Types.ghuser' name='View Types' %}

## Find Specific View Type

To find a specific view type in a model, use the *Find View Type* component shared here.

![]({{ "/static/images/guides/revit-views01c.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Find View Type.ghuser' name='Find View Type' %}

## Querying Views by Type

To collect views of a certain type, use a combination of *Find View Type* and *Views By Type* components shared here.

![]({{ "/static/images/guides/revit-views01d.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Views By Type.ghuser' name='Views By Type' %}

## Finding Specific Views

To find a view by name or by id in the active document, use the *Find View* component shared here.

![]({{ "/static/images/guides/revit-views02.png" | prepend: site.baseurl }})

As shown above, the *Find View* component, can search for a view by its name (N) or Title on Sheet (TOS).

{% include ltr/download_comp.html archive='/static/ghnodes/Find View.ghuser' name='Find View' %}

## Accessing Active View

{% capture api_note %}
In Revit API, the active view can be accessed from the {% include api_type.html type='Autodesk.Revit.UI.UIDocument' title='UI.UIDocument' %} object. The `UI.UIDocument` is responsible for handling the GUI operations of a view e.g. view window, zooming and panning, etc. Note that there is a legacy `UI.UIDocument.ActiveView` property that might return non-geometric views e.g. Project Browser (yes that is a *View* into Revit data). Always access the active view through `UI.UIDocument.ActiveGraphicalView` to avoid errors
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

Use the *Active View* component shared here to get the active view of the current of given document:

![]({{ "/static/images/guides/revit-views02a.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Active View.ghuser' name='Active View' %}

## Analyzing Views

Use the component shared here to get general properties of view elements:

![]({{ "/static/images/guides/revit-views02b.png" | prepend: site.baseurl }})

Outputs parameters are:

- **VD:** View Direction

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Analyse View.ghuser' name='Analyse View' %}

### Reading View Properties

Use the *Element.Decompose* component to inspect the properties of a view object.

![]({{ "/static/images/guides/revit-views03.png" | prepend: site.baseurl }})

### View Range

{% capture api_note %}
In Revit API, View Ranges are represented by the {% include api_type.html type='Autodesk.Revit.DB.PlanViewRange' title='DB.PlanViewRange' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

To read the view range property of a view, use the *Get View Range* component shared here.

![]({{ "/static/images/guides/revit-views03a.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Get View Range.ghuser' name='Get View Range' %}

### Collecting Displayed Elements

To collect all the elements owned by a view, use the *Element.OwnerViewFilter* component, passed to the {% include ltr/comp.html uuid="0f7da57e-" %} as shown below. Keep in mind that the 3D geometry that is usually shown in model views are not "Owned" by that view. All 2d elements e.g. Detail items, detail lines, ... are owned by the view they have created on.

![]({{ "/static/images/guides/revit-views04.png" | prepend: site.baseurl }})

You can use the *Element.SelectableInViewFilter* component to only list the selectable elements on a view.

![]({{ "/static/images/guides/revit-views05.png" | prepend: site.baseurl }})

### Getting V/G Overrides

To get the Visibility/Graphics overrides for an element on a specific view, use the shared *Get VG* component.

![]({{ "/static/images/guides/revit-views06.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Get Override VG.ghuser' name='Get Override VG' %}
{% include ltr/download_comp.html archive='/static/ghnodes/VG (Destruct).ghuser' name='VG (Destruct)' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Line VG Settings (Destruct).ghuser' name='Line VG Settings (Destruct)' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Surface VG Settings (Destruct).ghuser' name='Surface VG Settings (Destruct)' %}

### Setting V/G Overrides

To set the Visibility/Graphics overrides for an element on a specific view, use the shared *Set VG* component.

![]({{ "/static/images/guides/revit-views07.png" | prepend: site.baseurl }})

See [Styles and Patterns]({{ site.baseurl }}{% link _en/1.0/guides/revit-styles.md %}) on how to use the *Find Line Pattern* and *Find Fill Pattern* custom components. Here is an example of running the example above on a series of walls in a 3D view:

![]({{ "/static/images/guides/revit-views08.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Set Override VG.ghuser' name='Set Override VG' %}
{% include ltr/download_comp.html archive='/static/ghnodes/VG (Construct).ghuser' name='VG (Construct)' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Line VG Settings (Construct).ghuser' name='Line VG Settings (Construct)' %}
{% include ltr/download_comp.html archive='/static/ghnodes/Surface VG Settings (Construct).ghuser' name='Surface VG Settings (Construct)' %}
