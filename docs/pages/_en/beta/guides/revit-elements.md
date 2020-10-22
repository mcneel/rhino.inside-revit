---
title: "Data Model: Elements"
order: 30
group: Revit Basics
---

Elements are the basic building blocks in Revit data model. Elements are organized into Categories. The list of categories is built into each Revit version and can not be changed. Elements have [Parameters]({{ site.baseurl }}{% link _en/beta/guides/revit-params.md %}) that hold data associated with the Element. Depending on their category, Elements will get a series of built-in parameters and can also accept custom parameters defined by user. Elements might have geometry e.g. Walls (3D) or Detail Components (2D), or might not include any geometry at all e.g. *Project Information* (Yes even that is an Element in Revit data model, although it is not selectable since Revit views are designed around geometric elements, therefore Revit provides a custom window to edit the project information). Elements have [Types]({{ site.baseurl }}{% link _en/beta/guides/revit-types.md %}) that define how the element behaves in the Revit model.

{% capture api_note %}
In Revit API, Elements are represented by the {% include api_type.html type='Autodesk.Revit.DB.Element' title='DB.Element' %} and each element parameter is represented by {% include api_type.html type='Autodesk.Revit.DB.Parameter' title='DB.Parameter' %}. The {% include api_type.html type='Autodesk.Revit.DB.Element' title='DB.Element' %} has multiple methods to provide access to its collection of properties

&nbsp;

Each element has an Id (`DB.Element.Id`) that is an integer value. However this Id is not stable across upgrades and workset operations such as *Save To Central*, and might change. It is generally safer to access elements by their Unique Id (`DB.Element.UniqueId`) especially if you intend to save a reference to an element outside the Revit model e.g. an external database. Note that although the `DB.Element.UniqueId` looks like a UUID number, it is not. Keep that in mind if you are sending this information to your external databases.
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

## Referencing Elements

There are more elaborate ways to collect various elements in Revit. The [Instances]({{ site.baseurl }}{% link _en/beta/guides/revit-instances.md %}) article discusses the generic ways to collect elements, and the rest of the articles, provide more specific ways to collect special types of elements in a Revit document. The sections below show how you can manually reference a specific element and bring that into your Grasshopper definition.

### By Selection

Use the context menu on the {% include ltr/comp.html uuid='ef607c2a' %} parameter to reference geometrical Revit elements in your definition:

![]({{ "/static/images/guides/revit-elements-select.gif" | prepend: site.baseurl }})


### By Element Id

You can use the {% include ltr/comp.html uuid='f3ea4a9c' %} parameter and add the element Ids into the *Manage Revit Element Collection* on the component context menu:

![]({{ "/static/images/guides/revit-elements01.png" | prepend: site.baseurl }})