---
title: "Data Model: Types"
order: 32
---

Revit organizes the building components into *Categories*, *Families*, and *Types*. In this article, we will discuss each and guide you in dealing with this organization method when using {{ site.terms.rir }} (or Revit API).

### Categories

![]({{ "/static/images/guides/revit-categories.svg" | prepend: site.baseurl }})

Categories are the highest-level groups. These categories are built into Revit and loosely organize the components by their function. There are also multiple category types in a Revit model:
  - *Model* categories e.g. *Walls*, *Doors*, *Floors*, *Roofs*, etc.
  - *Analytical* categories e.g. *Analytical Surfaces*, *Structural Loads*, etc.
  - *Annotation* categories e.g. *Tags*, *Dimensions*, etc.

There are many categories in each category type. Some argue that the Category Type is actually a higher-level organization but in practice, following the *Categories*, *Families*, and *Types* organization system is easier to understand and remember.

{% capture api_note %}
In Revit API, all the built-in categories are represented by the {% include api_type.html type='Autodesk.Revit.DB.BuiltInCategory' title='DB.BuiltInCategory' %} enumeration and all the built-in category types are represented by the {% include api_type.html type='Autodesk.Revit.DB.CategoryType' title='DB.CategoryType' %} enumeration
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### Types

![]({{ "/static/images/guides/revit-types.svg" | prepend: site.baseurl }})

Before discussing *Families*, we need to discuss *Types* in Revit. There can be multiple types of elements under each of the Revit categories discussed above. For example a 3ft x 7ft single-panel door, is a door *Type* under the *Doors* category, or a 2x4 wood post is a column *Type* under the *Structural Columns* category.

There are two groups of *Types* in Revit:

 - **System Types** are built-in types that can exist in any Revit model e.g. types listed under *Basic Wall* or *Duct System* in *Project Browser*. The logic and behavior of these types is built into Revit and can not be changed by the user.
 - **Custom Types** are types that are defined by a Revit user e.g. *Furniture* types or *Door* types.

Each type, whether *System* or *Custom*, can have a series of **Type Parameters** that modify the behavior or other aspect of that specific Type. When working with Revit, we tend to define or modify various *System* or *Custom* Types and place instances of these types into the model. For example we can define a 3ft x 7ft single-panel door Type and place many instances of this type in the model. All these instances will follow the logic that is enforced by that specific type. However, Type definitions can also allow certain **Instance Parameters** to be modified to change the behavior or graphics of a specific instance.

### Families

![]({{ "/static/images/guides/revit-families.svg" | prepend: site.baseurl }})

Now that we know what Types are we can discuss Families. There is big challenge with the Category and Type structure that we discussed above. There can be many many various types in a Revit model and they can be radically different from each other. For example we can have hundreds of door types with various designs and sizes. A garage door is vastly different from a single-panel interior door. So we need a way to organize these types into related groups.

Revit families are a mechanism designed to organize the *System* and *Custom Types*:

- *System Families* are named groups, that attempt to organize related system types e.g. *Duct System* or *Basic Wall*.

- *Custom Families* (or [Loadable Families](http://help.autodesk.com/view/RVT/2020/ENU/?guid=GUID-7AEC5D66-C2E0-40E2-9504-3CC13781B87A)) are far more complex. They are a way to create custom types with custom design, and behavior. For example you can create a new table family that looks like a spaceship, is hovering over the floor, and can show 6 to 12 chairs depending on the desired configuration. Revit *Family Editor* can be used to define new custom families based on a family template file (`*.rft`). Custom families can be stored in external family files (`*.rfa`) and be shared with other Revit users. *In-Place Families* are a simplified variation of the custom families, specifically created for geometry that has limited use in a model.

{% include ltr/warning_note.html note='The name, *System Families*, has led to a lot of confusion among Revit users. Remember, **System Families** are just a name given to a related group of **System Types**. They are vastly different from **Custom Families** and can not be stored in external family files. As Revit users or Revit programmers we generally do not deal with *System Families* and Revit API does not support creating or modifying the *System Families* as of yet either. Hence when discussing Revit, it is quite common to refer to *Custom Families* simply as *Families*' %}

{% capture api_note %}
In Revit API, **Custom Families** are represented by the {% include api_type.html type='Autodesk.Revit.DB.Family' title='DB.Family' %}, their various types are represented by {% include api_type.html type='Autodesk.Revit.DB.FamilySymbol' title='DB.FamilySymbol' %}, and each instance is represented by a {% include api_type.html type='Autodesk.Revit.DB.FamilyInstance' title='DB.FamilyInstance' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

Defining new custom families is not a trivial task especially if they need to be smart and flexible to adapt to multiple model conditions, and is arguably one of the most important automation topics in Revit. Most companies create their own set of custom families for various components that are often used in their models. There are also third-party companies that create custom families, or create family organization solutions.

To get you started, Revit installation provides a default set of these custom families based on the measurement system e.g. Imperial vs Metric and also provides many templates to help with creating new custom families from scratch.

When working with Revit or Revit API, we are mostly dealing with Revit **Types** and **Custom Families**. This guide takes you through the various {{ site.terms.rir }} components that help you query and create types and families.

## Querying Types

You can use the combination of a category picker components e.g. *Model.CategoriesPicker*, the *Element.CategoryFilter* component, and *Document.ElementTypes* component to collect types in a certain Revit category:

![]({{ "/static/images/guides/revit-families01.png" | prepend: site.baseurl }})

The *Document.ElementTypes* component can further filter the list of types:

![]({{ "/static/images/guides/revit-families02.png" | prepend: site.baseurl }})

### Querying Type Info

Use the *ElementType.Identity* to access information about that type. Please note that the *Family Name* parameter, returns the *System Family* name for *System Types* and the *Custom Family* name for Custom Types:

![]({{ "/static/images/guides/revit-families02a.png" | prepend: site.baseurl }})

## Accessing Family of a Type

When querying the custom types that exist in a Revit model, we can find out the custom family definition that contains the logic for each of these types. We are using a custom Grasshopper Python component (*Type Family*) to grab the family of each type being passed into this component. You can download this component, as a Grasshopper user object, from the link below.

![]({{ "/static/images/guides/revit-families03.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Type Family.ghuser' name='Type Family' %}

Notice that **Duct Systems** for example, is a system type and therefore have no associated custom family definition. Therefore the *Type Family* component is returning `null`.

![]({{ "/static/images/guides/revit-families04.png" | prepend: site.baseurl }})


## Picking Types

{{ site.terms.rir }} includes a few components that can help you pick a specific element type from a Revit category:

- *Model.CategoriesPicker*: Allows selecting a specific model category e.g. Walls
- *Analytical.CategoriesPicker*: Allows selecting a specific analytical category e.g. Analytical Walls
- *Annotation.CategoriesPicker*: Allows selecting a specific annotation category e.g. Dimensions
  - *Tag.CategoriesPicker*: Allows selecting a specific tag category e.g. Room Tags

You can pass the any of the categories above to the *ElementType.ByName* component to select a specific type from that category:

![]({{ "/static/images/guides/revit-families05.png" | prepend: site.baseurl }})


## Determining Default Types

When a build tool is launched (e.g. *Place Door*), Revit will automatically select the last-used type for that specific category (e.g. *Doors* for *Place Door* tool). This is called the **Default Type** for that category. This information is helpful when creating elements using the API. Use the component shared below to inspect the default types for the provided category:

![]({{ "/static/images/guides/revit-families05a.png" | prepend: site.baseurl }})

In case of custom types, the component will return the default `DB.FamilySymbol`:

![]({{ "/static/images/guides/revit-families05b.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Get Default Type.ghuser' name='Get Default Type' %}

## Modifying Types

Once you have filtered out the desired type, you can query its parameters and apply new values. See [Document Model: Parameters]({{ site.baseurl }}{% link _en/beta/guides/revit-params.md %}) to learn how to edit parameters of an element. The element parameter components work on element types as well.

![]({{ "/static/images/guides/revit-families06.png" | prepend: site.baseurl }})

## Extracting Type Geometry

Once you have filtered out the desired type, you can extract the geometry for that element type using the *Element.Geometry* component. The *Level Of Detail* value list component makes it easy to provide correct values for LOD input parameter.

![]({{ "/static/images/guides/revit-families07.png" | prepend: site.baseurl }})

The *Element.Geometry* component automatically previews the geometry in Rhino window.

![]({{ "/static/images/guides/revit-families08.png" | prepend: site.baseurl }})

&nbsp;

{% include ltr/download_comp.html archive='/static/ghnodes/Level Of Detail.ghuser' name='Level Of Detail' %}

## Extracting Type Geometry by Category

<!-- https://github.com/mcneel/rhino.inside-revit/issues/93 -->

The *Element Geometry By SubCategory* component shared here helps you extract geometry of a family instance alongside information about its subcategory definition inside the family. The example here extracts the geometry from a series of window instances

![]({{ "/static/images/guides/revit-families08a.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-families08b.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Element Geometry By SubCategory.ghuser' name='Element Geometry By SubCategory' %}

See [Examples](#examples)

## Creating New Types

To create new types, you would need to find an existing type, use the *ElementType.Duplicate* component to duplicate that type with a new name, and adjust the desired properties.

![]({{ "/static/images/guides/revit-families09.png" | prepend: site.baseurl }})

Revit *Project Browser* now displays the new type under *Families*

## Placing Instances of Types

Use the *AddFamilyInstance.ByLocation* component (under *Revit > Build* panel) to place an instance of a type into the Revit model space.

![]({{ "/static/images/guides/revit-families09a.png" | prepend: site.baseurl }})

For types that require a host, you can pass a host element to the *AddFamilyInstance.ByLocation* component as well.

![]({{ "/static/images/guides/revit-families09b.png" | prepend: site.baseurl }})

The component, places the given type on the nearest location along the host element. In the image below, the green sphere is the actual location that is passed to the component. Notice that the door is placed on the closest point on the wall.

![]({{ "/static/images/guides/revit-families09c.png" | prepend: site.baseurl }})

## Removing Types

You can use the *Element.Delete* component to delete types. Remember that deleting types will delete all instances of that type as well. If you don't want this, find the instances and change their types before deleting a type from model.

![]({{ "/static/images/guides/revit-families09d.png" | prepend: site.baseurl }})

## Loading Families

Use the *Family.Load* component to load a new family file into your model.

![]({{ "/static/images/guides/revit-families10.png" | prepend: site.baseurl }})

Revit *Project Browser* now lists the new family under *Families*

## Saving Families

Use the *Family.Save* component to save a family into an external file.

![]({{ "/static/images/guides/revit-families11.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Type Family.ghuser' name='Type Family' %}

## Creating New Families

Under current {{ site.terms.rir }} implementation, you can use the *Family.New* component to generate new Revit families and insert a new geometry into the family. Make sure to assign the correct template file to the component for best results.

![]({{ "/static/images/guides/revit-families12.png" | prepend: site.baseurl }})

Revit *Project Browser* now lists the new family under *Families*

You can also pass the **Generic Model** template to the *Family.New* component and set the category manually using the *Model.CategoriesPicker* component.

![]({{ "/static/images/guides/revit-families13.png" | prepend: site.baseurl }})

There are a series of components under the *Revit > Family* panel that will help you generate geometry for Revit families:

- *FamilyElement.ByBrep*
- *FamilyElement.ByCurve*
- *FamilyOpening.ByCurve*
- *FamilyVoid.ByBrep*

![]({{ "/static/images/guides/revit-families14.png" | prepend: site.baseurl }})

As shown in the example above, you can use the *Visibility.Construct* component to create visibility options for the generated geometry. This components provides all the options available in the native Revit *Visibility/Graphics* editor for family geometries.

![]({{ "/static/images/guides/revit-families15.png" | prepend: site.baseurl }})

## Editing Families

You can use the *Family.New* component to edit existing families as well. Just pass the appropriate template and family name, the new geometry, and the *Family.New* component automatically finds the existing family, replaces the content and reloads the family into the Revit model. Make sure the *OverrideFamily* is set to `True` and *OverrideParameters* is set appropriately to override the family parameters if needed.

![]({{ "/static/images/guides/revit-families16.png" | prepend: site.baseurl }})

## Examples

Examples listed below create various types of families in Revit using {{ site.terms.rir }}

- [Column Family Along Curve]({{ site.baseurl }}{% link _en/beta/samples/column-family-along-curve.md %})
- [Window Geometry and Material]({{ site.baseurl }}{% link _en/beta/samples/type-geom-material.md %})
