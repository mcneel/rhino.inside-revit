---
title: "Data Model: Instances"
order: 33
---

*Instances* are individual elements places in a Revit model e.g. a single Wall, or a single Door, or any other single element. Instances have *Categories* and might also have *Type* e.g. each Door has a Door type. Instances inherit a series of *Parameters* from their *Category* and *Type* and might have instance parameters as well that only belongs to that single instance.

## Querying Instance of a Type

Use a combination of category component, connected to *ElementType.ByName* and *Element.TypeFilter*, to query all the instances of a specific type. The example below is collecting all the instance of the **My Basic Wall** type:

![]({{ "/static/images/guides/revit-instances01.png" | prepend: site.baseurl }})

## Filtering Instances by Property

{% include ltr/en/wip_note.html %}

## Extracting Instance Geometry

{% include ltr/en/wip_note.html %}

{% include ltr/download_comp.html archive='/static/ghnodes/Level Of Detail.ghuser' name='Level Of Detail' %}

## Modifying Instances

Once you have filtered out the desired instance, you can query its parameters and apply new values. See [Document Model: Parameters]({{ site.baseurl }}{% link _en/beta/guides/revit-params.md %}) to learn how to edit parameters of an element.

## Changing Instance Type

{% include ltr/en/wip_note.html %}