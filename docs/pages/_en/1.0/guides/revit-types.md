---
title: "Revit: Types & Families"
subtitle: Revit's categorization system, Categories, Types, ...
order: 23
group: Essentials
home: true
thumbnail: /static/images/guides/revit-types.png
---

{% capture link_note %}
When working with Revit or Revit API, we are mostly dealing with Revit **Types** and **Custom Families**. This guide takes you through the various Grasshopper components that help you query and create types and families. For an look at how these elements are organized within Revit, see [Revit: Types & Families]({{ site.baseurl }}{% link _en/1.0/guides/revit-revit.md %}#categories-families--types)
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-types.png' %}

## Querying Types

You can use the combination of a category picker components e.g. {% include ltr/comp.html uuid="af9d949f-" %}, the {% include ltr/comp.html uuid="d08f7ab1-" %} component, and {% include ltr/comp.html uuid="7b00f940-" %} component to collect types in a certain Revit category:

![]({{ "/static/images/guides/revit-families01.png" | prepend: site.baseurl }})

The {% include ltr/comp.html uuid="7b00f940-" %} component can further filter the list of types:

![]({{ "/static/images/guides/revit-families02.png" | prepend: site.baseurl }})

### Querying Type Info

Use the {% include ltr/comp.html uuid='7dea1ba3' %} to access information about that type. Please note that the *Family Name* parameter, returns the *System Family* name for *System Types* and the *Custom Family* name for Custom Types:

![]({{ "/static/images/guides/revit-families02a.png" | prepend: site.baseurl }})

## Accessing Family of a Type

When querying the custom types that exist in a Revit model, we can find out the custom family definition that contains the logic for each of these types. We are using a custom Grasshopper Python component (*Type Family*) to grab the family of each type being passed into this component. You can download this component, as a Grasshopper user object, from the link below.

![]({{ "/static/images/guides/revit-families03.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Type Family.ghuser' name='Type Family' %}

Notice that **Duct Systems** for example, is a system type and therefore have no associated custom family definition. Therefore the *Type Family* component is returning `null`.

![]({{ "/static/images/guides/revit-families04.png" | prepend: site.baseurl }})

## Choosing A Specific Type

{% include ltr/comp.html uuid="af9d949f-" %} component allows selecting a specific model category e.g. Walls

You can pass the any of the categories above to the {% include ltr/comp.html uuid="d3fb53d3-9" %} component to select a specific type from that category:

![]({{ "/static/images/guides/revit-families05.png" | prepend: site.baseurl }})


## Determining Default Types

When a build tool is launched (e.g. *Place Door*), Revit will automatically select the last-used type for that specific category (e.g. *Doors* for *Place Door* tool). This is called the **Default Type** for that category. This information is helpful when creating elements using the API. Use the component shared below to inspect the default types for the provided category:

![]({{ "/static/images/guides/revit-families05a.png" | prepend: site.baseurl }})

In case of custom types, the component will return the default `DB.FamilySymbol`:

![]({{ "/static/images/guides/revit-families05b.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Get Default Type.ghuser' name='Get Default Type' %}

## Modifying Types

Once you have filtered out the desired type, you can query its parameters and apply new values. See [Document Model: Parameters]({{ site.baseurl }}{% link _en/1.0/guides/revit-params.md %}) to learn how to edit parameters of an element. The element parameter components work on element types as well.

![]({{ "/static/images/guides/revit-families06.png" | prepend: site.baseurl }})

## Extracting Type Geometry

Once you have filtered out the desired type, you can extract the geometry for that element type using the {% include ltr/comp.html uuid="b3bcbf5b-" %} component. The {% include ltr/comp.html uuid="b078e48a-" %} value list component makes it easy to provide correct values for LOD input parameter.

![]({{ "/static/images/guides/revit-families07.png" | prepend: site.baseurl }})

The {% include ltr/comp.html uuid="b3bcbf5b-" %} component automatically previews the geometry in Rhino window.

![]({{ "/static/images/guides/revit-families08.png" | prepend: site.baseurl }})

## Extracting Type Geometry by Category

<!-- https://github.com/mcneel/rhino.inside-revit/issues/93 -->

The *Element Geometry By SubCategory* component shared here helps you extract geometry of a family instance alongside information about its subcategory definition inside the family. The example here extracts the geometry from a series of window instances

![]({{ "/static/images/guides/revit-families08a.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-families08b.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Element Geometry By SubCategory.ghuser' name='Element Geometry By SubCategory' %}

## Creating New Types

To create new types, you would need to find an existing type, use the {% include ltr/comp.html uuid="5ed7e612-" %} component to duplicate that type with a new name, and adjust the desired properties.

![]({{ "/static/images/guides/revit-families09.png" | prepend: site.baseurl }})

Revit *Project Browser* now displays the new type under *Families*

## Removing Types

You can use the {% include ltr/comp.html uuid="3ffc2cb2-" %} component to delete types. Remember that deleting types will delete all instances of that type as well. If you don't want this, find the instances and change their types before deleting a type from model.

![]({{ "/static/images/guides/revit-families09d.png" | prepend: site.baseurl }})

## Loading Families

Use the {% include ltr/comp.html uuid="0e244846-" %} component to load a new family file into your model.

![]({{ "/static/images/guides/revit-families10.png" | prepend: site.baseurl }})

Revit *Project Browser* now lists the new family under *Families*

## Saving Families

Use the {% include ltr/comp.html uuid="c2b9b045-" %} component to save a family into an external file.

![]({{ "/static/images/guides/revit-families11.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Type Family.ghuser' name='Type Family' %}

## Creating New Families

Under current {{ site.terms.rir }} implementation, you can use the {% include ltr/comp.html uuid='82523911' %} component to generate new Revit families and insert a new geometry into the family. Make sure to assign the correct template file to the component for best results.

![]({{ "/static/images/guides/revit-families12.png" | prepend: site.baseurl }})

Revit *Project Browser* now lists the new family under *Families*

You can also pass the **Generic Model** template to the {% include ltr/comp.html uuid='82523911' %} component and set the category manually using the {% include ltr/comp.html uuid="af9d949f-" %} component.

![]({{ "/static/images/guides/revit-families13.png" | prepend: site.baseurl }})

There are a series of components under the *Revit > Family* panel that will help you generate geometry for Revit families:

- {% include ltr/comp.html uuid='f0887ad5' %}
- {% include ltr/comp.html uuid='6fbb9200' %}
- {% include ltr/comp.html uuid='72fdc627' %}
- {% include ltr/comp.html uuid='f0887ad5' %}

![]({{ "/static/images/guides/revit-families14.png" | prepend: site.baseurl }})

As shown in the example above, you can use the {% include ltr/comp.html uuid='10ea29d4' %} component to create visibility options for the generated geometry. This components provides all the options available in the native Revit *Visibility/Graphics* editor for family geometries.

![]({{ "/static/images/guides/revit-families15.png" | prepend: site.baseurl }})

## Editing Families

You can use the {% include ltr/comp.html uuid='82523911' %} component to edit existing families as well. Just pass the appropriate template and family name, the new geometry, and the {% include ltr/comp.html uuid='82523911' %} component automatically finds the existing family, replaces the content and reloads the family into the Revit model. Make sure the *OverrideFamily* is set to `True` and *OverrideParameters* is set appropriately to override the family parameters if needed.

![]({{ "/static/images/guides/revit-families16.png" | prepend: site.baseurl }})
