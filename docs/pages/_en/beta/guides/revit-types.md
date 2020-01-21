---
title: Working with Types
order: 50
---

## Element Types in Revit

Revit database separates the element types into two major groups:

### System Types

Sometimes referred to as System Families; are built-in element types that can exist in any Revit model. These types can not be stored in separate family files.

### Custom Types

Referred to as Custom Families; are custom element types defined by a Revit user. These types are stored inside a Revit Family file

{% include ltr/bubble_note.html note='Generally Types are predefined and built-in Revit types and Families are custom types that can be saved to family files and shared with others.' %}

## Querying Types

You can use the combination of *Model.CategoriesPicker*, *Element.CategoryFilter*, and *Document.ElementTypes* components to collect element types of a certain Revit category:

![]({{ "/static/images/guides/revit-families01.png" | prepend: site.baseurl }})

The *Document.ElementTypes* component can further filter the list of types:

![]({{ "/static/images/guides/revit-families02.png" | prepend: site.baseurl }})


## Accessing Family of a Type

When querying the custom types that exist in a Revit model, we can find out the family file that contains each of these types. We are using a custom Grasshopper Python component to grab the Family of each type being passed into this component. You can download this component as a cluster from the link below.

![]({{ "/static/images/guides/revit-families03.png" | prepend: site.baseurl }})

{% include ltr/download_pkg.html archive='/static/clusters/Type Family.ghcluster' title='**Type Family** Cluster' %}

Notice that in case of **Duct Systems** the element types are system types and therefore have no associated family file

![]({{ "/static/images/guides/revit-families04.png" | prepend: site.baseurl }})


## Picking Types

## Extracting Type Geometry

## Loading New Families

## Saving Existing Families

## Editing Existing Families

## Creating New Families

## Samples

Samples listed below create various types of families in Revit using {{ site.terms.rir }}

- [Column Family Along Curve]({{ site.baseurl }}{% link _en/beta/samples/column-family-along-curve.md %})
