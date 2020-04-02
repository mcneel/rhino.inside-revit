---
title: "Data Model: Elements"
order: 30
---

{% include ltr/en/wip_note.html %}

Elements are the basic building blocks in Revit data model. Elements are organized into Categories. The list of categories is built into each Revit version, Elements have [Parameters]({{ site.baseurl }}{% link _en/beta/guides/revit-params.md %}) that hold data associated with the Element. Depending on their category, Elements will get a series of built-in parameters and can also accept custom parameters defined by user. Elements might have geometry e.g. Walls (3d) or Detail Components (2d), or might not include any geometry at all e.g. Project Information (Yes even Project Information is an Element in Revit data model, although it is not selectable in the user interface since Revit views are designed around geometric elements, therefore Revit GUI provides a custom window to edit the *Project Information*). Elements have [Types]({{ site.baseurl }}{% link _en/beta/guides/revit-types.md %}) that define how the element behaves in the Revit model.

{% capture api_note %}
In Revit API, Elements are represented by the {% include api_type.html type='Autodesk.Revit.DB.Element' title='DB.Element' %} and element parameters are represented by {% include api_type.html type='Autodesk.Revit.DB.Parameter' title='DB.Parameter' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

{% capture api_warning_note %}
Each element has an Id (`DB.Element.Id`) that is an integer value. However this Id is not stable across upgrades and workset operations such as *Save To Central*, and might change. It is generally safer to access elements by their Unique Id (`DB.Element.UniqueId`) especially if you intend to save a reference to an element outside the Revit model e.g. an external database
{% endcapture %}
{% include ltr/warning_note.html note=api_warning_note %}


## Accessing Specific Elements

### By Element Id

You can use the *Elements* parameter and add the element Ids into the *Manage Revit Element Collection* on the component context menu:

![]({{ "/static/images/guides/revit-elements01.png" | prepend: site.baseurl }})