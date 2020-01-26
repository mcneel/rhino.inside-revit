---
title: Working with Types
order: 30
---

## Element Types in Revit

Revit separates the element types into two major groups:

 - **System Types:** Built-in element types that can exist in any Revit model e.g. *Wall* types or *Duct System* types. These types can not be stored in separate family files
 - **Custom Types:** Custom element types defined by a Revit user e.g. *Furniture* types or *Door* types. These types can be stored in a Revit Family file (*.rfa)

These element types are grouped by Revit built-in categories e.g. *Walls*, *Furniture*, *Doors*. The built-in categories are fixed in Revit by design, but you can define new sub-categories for Custom Types.

{% include ltr/bubble_note.html note='Generally system types are referred to as **System Families**, and custom types that can be saved to family files and shared with others, are referred to as **Families**' %}

## Querying Types

You can use the combination of *Model.CategoriesPicker*, *Element.CategoryFilter*, and *Document.ElementTypes* components to collect element types of a certain Revit category:

![]({{ "/static/images/guides/revit-families01.png" | prepend: site.baseurl }})

The *Document.ElementTypes* component can further filter the list of types:

![]({{ "/static/images/guides/revit-families02.png" | prepend: site.baseurl }})


## Accessing Family of a Type

When querying the custom types that exist in a Revit model, we can find out the family file that contains each of these types. We are using a custom Grasshopper Python component (*Type Family*) to grab the family of each type being passed into this component. You can download this component, as a Grasshopper cluster, from the link below.

![]({{ "/static/images/guides/revit-families03.png" | prepend: site.baseurl }})

{% include ltr/download_pkg.html archive='/static/clusters/Type Family.ghcluster' title='Download **Type Family**' %}

Notice that in case of **Duct Systems** for example, the element types are system types and therefore have no associated family file. Hence the *Type Family* component is returning `null`.

![]({{ "/static/images/guides/revit-families04.png" | prepend: site.baseurl }})


## Picking Types

{{ site.terms.rir }} includes a few components that can help you pick a specific element type from a Revit category:

- *Analytical.CategoriesPicker*: Allows selecting a specific analytical category e.g. Analytical Walls
- *Annotation.CategoriesPicker*: Allows selecting a specific annotation category e.g. Dimensions
- *Model.CategoriesPicker*: Allows selecting a specific model category e.g. Walls
- *Tag.CategoriesPicker*: Allows selecting a specific tag category e.g. Room Tags

You can pass the any of the categories above to the *ElementType.ByName* component to select a specific type from that category:

![]({{ "/static/images/guides/revit-families05.png" | prepend: site.baseurl }})


## Extracting Type Geometry

## Loading New Families

## Saving Existing Families

## Editing Existing Families

## Creating New Families

## Samples

Samples listed below create various types of families in Revit using {{ site.terms.rir }}

- [Column Family Along Curve]({{ site.baseurl }}{% link _en/beta/samples/column-family-along-curve.md %})
