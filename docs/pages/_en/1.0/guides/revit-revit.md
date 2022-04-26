---
title: "Overview"
subtitle: Understanding Revit's data model
order: 10
thumbnail: /static/images/guides/revit-revit.png
group: Essentials
---

Let's look at how Revit generates and stores building information. Having a firm understanding of Revit's data model is very important when working with the data that is generated and managed by Revit. In this guide we will take a look at Revit's data model and discuss it in detail. Other chapters will guide you in working with this data model using the Revit-aware Grasshopper components.

## The Element DNA

The graphic below, shows the DNA of a single {% include ltr/misc.html uuid='11f05ff5' title="Revit Element" %}. It works like a small machine that takes inputs, processes them, and generates geometry and data as outputs. Keep in mind that not every element has geometry. Some of these elements might only carry information.

![]({{ "/static/images/guides/revit-revit-element-dna.svg" | prepend: site.baseurl }})

So practically, we feed type and instance information into the family *function*, to generate the element metadata (including calculated properties) and geometry. Always keep in mind that the data we provide in Revit *Family* definition, and *Type* or *Instance* parameters, are used alongside the family logic to generate BIM data structure.

![]({{ "/static/images/guides/revit-revit-element-func.svg" | prepend: site.baseurl }})

The generated elements are then stored in a Revit **Document**. They are also organized by a series of 
**Containers**, each with a specific purpose.

![]({{ "/static/images/guides/revit-revit-containers.svg" | prepend: site.baseurl }})

This is also a good place to mention **Subcategories**. They sound like an organization level right under the **Category**, but in practice, it is easier to think of them as a property of geometry rather than an organization level. When a Family function generates geometry, it can group them into subcategories of the main category. Their main purpose is to allow finer control over the graphical representation of each part of the geometry.


## Elements & Instances

An often-asked question is **What is an Element?** Elements are the basic building blocks in Revit data model. Elements are organized into Categories. The list of categories is built into each Revit version and can not be changed. Elements have [Parameters]({{ site.baseurl }}{% link _en/1.0/guides/revit-params.md %}) that hold data associated with the Element. Depending on their category, Elements will get a series of built-in parameters and can also accept custom parameters defined by user. Elements might have geometry e.g. Walls (3D) or Detail Components (2D), or might not include any geometry at all e.g. *Project Information* (Yes even that is an Element in Revit data model, although it is not selectable since Revit views are designed around geometric elements, therefore Revit provides a custom window to edit the project information). Elements have [Types]({{ site.baseurl }}{% link _en/1.0/guides/revit-types.md %}) that define how the element behaves in the Revit model.

{% capture api_note %}
In Revit API, Elements are represented by the {% include api_type.html type='Autodesk.Revit.DB.Element' title='DB.Element' %} and each element parameter is represented by {% include api_type.html type='Autodesk.Revit.DB.Parameter' title='DB.Parameter' %}. The {% include api_type.html type='Autodesk.Revit.DB.Element' title='DB.Element' %} has multiple methods to provide access to its collection of properties

&nbsp;

Each element has an Id (`DB.Element.Id`) that is an integer value. However this Id is not stable across upgrades and workset operations such as *Save To Central*, and might change. It is generally safer to access elements by their Unique Id (`DB.Element.UniqueId`) especially if you intend to save a reference to an element outside the Revit model e.g. an external database. Note that although the `DB.Element.UniqueId` looks like a UUID number, it is not. Keep that in mind if you are sending this information to your external databases.
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

{% capture link_note %}
See [Revit: Elements & Instances]({{ site.baseurl }}{% link _en/1.0/guides/revit-elements.md %}) guide on how to work with Revit Elements and Instances using Grasshopper in Revit.
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-elements.png' %}

## Parameters

Parameter are attached to elements so they can carry metadata. For example, height is a property of a *Wall* element and its value is carried by the *Height Parameter*.

Parameters come in many types:

1. Built-in Parameters
1. Project/Shared Parameters for Instances or Types
1. Global Parameters

Let's take a look at various parameter types that we encounter when working with Revit elements.

### Built-in Parameters

These are the most obvious set of parameters that are built into Revit based on the categories and element types. For example, a Wall, or Room element has a parameter called *Volume*. This parameter does not make sense for a 2D Filled Region element and thus is not associated with that element type.

Revit shows the list of built-in parameters in the *Element Properties* panel.

![]({{ "/static/images/guides/revit-params-parampanel.png" | prepend: site.baseurl }})

Built-in Parameters are commonly defined in every Revit project by default. There is not way to change teh definition of them, only to read and write values to them.

{% capture api_note %}
In Revit API, all the built-in parameters are represented by the {% include api_type.html type='Autodesk.Revit.DB.BuiltInParameter' title='DB.BuiltInParameter' %} enumeration
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### Project/Shared Parameters

Revit allows a user to create a series of custom parameters and apply them globally to selected categories. The *Element Properties* panel displays the project parameters attached to the selected element as well.

Project Parameters may be attached to the Element Type or the Element Instance.

To create a project parameter one must first define a parameter definition.  This is a template that outlines a parameter's Name, DataType, Group and optionally an ID (Guid). The definition is used  then a Parameter may be added to the projects.  Once added to the project, a Parameter instance will be attached to all the element instances, types or Globally to the project. Each Parameter instance can then store a unique value of a specific datatype.

Shared Parameters are simply Project Parameters whos Parameter definitions can be transferred from one project to another thru a from a shared parameter file. Multiple projects can contain common parameter definitions. It is important to note that it is only the definitions that are shared, not the values within the parameters themselves. Shared parameters have a unique ID (Guid) that project parameters do not.

![]({{ "/static/images/guides/revit-params-projshared.png" | prepend: site.baseurl }})

### Global Parameters

Global parameters are category-agnostic parameters that could be applied to a range of instance or type parameters across many different Revit categories. Many times these are used for project level data.

![]({{ "/static/images/guides/revit-params-global.png" | prepend: site.baseurl }})

&nbsp;

{% capture link_note %}
See [Revit: Parameters]({{ site.baseurl }}{% link _en/1.0/guides/revit-params.md %}) guide on how to inspect, read, and write Revit element instance and type parameters using Grasshopper in Revit.
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-params.png' %}


## Categories, Families, & Types

As the above graphic shows, Revit organizes the building components into *Categories*, *Families*, and *Types*. Let's discuss each in more detail.

### Categories

![]({{ "/static/images/guides/revit-types-categories.svg" | prepend: site.baseurl }})

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

![]({{ "/static/images/guides/revit-types-types.svg" | prepend: site.baseurl }})

Before discussing *Families*, we need to discuss *Types* in Revit. There can be multiple types of elements under each of the Revit categories discussed above. For example a 3ft x 7ft single-panel door, is a door *Type* under the *Doors* category, or a 2x4 wood post is a column *Type* under the *Structural Columns* category.

Each type, can have a series of **Type Parameters** that modify the behavior or other aspect of that specific Type. When working with Revit, we tend to define or modify various *Types* and place instances of these types into the model. For example we can define a 3ft x 7ft single-panel door Type and place many instances of this type in the model. All these instances will follow the logic that is enforced by that specific type. However, Type definitions can also allow certain **Instance Parameters** to be modified to change the behavior or graphics of a specific instance.

### Families

![]({{ "/static/images/guides/revit-types-families.svg" | prepend: site.baseurl }})

Now that we know what Types are we can discuss Families. There is big challenge with the Category and Type structure that we discussed above. There can be many many various types in a Revit model and they can be radically different from each other. For example we can have hundreds of door types with various designs and sizes. A garage door is vastly different from a single-panel interior door. So we need a way to organize these types into related groups:

- *System Families* are named groups e.g. *Duct System* or *Basic Wall*

- *Custom Families* (or [Loadable Families](http://help.autodesk.com/view/RVT/2020/ENU/?guid=GUID-7AEC5D66-C2E0-40E2-9504-3CC13781B87A)) are far more complex. They are a way to create custom types with custom design, and behavior. For example you can create a new table family that looks like a spaceship, is hovering over the floor, and can show 6 to 12 chairs depending on the desired configuration. Revit *Family Editor* can be used to define new custom families based on a family template file (`*.rft`). Custom families can be stored in external family files (`*.rfa`) and be shared with other Revit users. *In-Place Families* are a simplified variation of the custom families, specifically created for geometry that has limited use in a model.

{% include ltr/warning_note.html note='The name, *System Families*, has led to a lot of confusion among Revit users. Remember, **System Families** are just a name given to a related group of types. They are vastly different from **Custom Families** and can NOT be stored in external family files. As Revit users or Revit programmers we generally do not deal with *System Families* and Revit API does not support creating or modifying the *System Families* as of yet either. Hence when discussing Revit, it is quite common to refer to *Custom Families* simply as *Families*' %}

{% capture api_note %}
In Revit API, **Custom Families** are represented by the {% include api_type.html type='Autodesk.Revit.DB.Family' title='DB.Family' %}, their various types are represented by {% include api_type.html type='Autodesk.Revit.DB.FamilySymbol' title='DB.FamilySymbol' %}, and each instance is represented by a {% include api_type.html type='Autodesk.Revit.DB.FamilyInstance' title='DB.FamilyInstance' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

Defining new custom families is not a trivial task especially if they need to be smart and flexible to adapt to multiple model conditions, and is arguably one of the most important automation topics in Revit. Most companies create their own set of custom families for various components that are often used in their models. There are also third-party companies that create custom families, or create family organization solutions.

To get you started, Revit installation provides a default set of these custom families based on the measurement system e.g. Imperial vs Metric and also provides many templates to help with creating new custom families from scratch.

{% capture link_note %}
See [Revit: Types & Families]({{ site.baseurl }}{% link _en/1.0/guides/revit-types.md %}) guide on how to work with Revit Types, and Families using Grasshopper in Revit.
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-types.png' %}

## Containers

Revit Containers (e.g. *Worksets*, *Design Options*, etc.) are a mechanism to logically group a series of elements, and they each have a very specific usage. For example Worksets allow loading parts of the buildings only so collaboration and conflict resolution becomes easier. When loading a specific workset, the elements that are not part of that workset are not loaded.

## Documents & Links

In simplest terms, Revit *Documents* are collections of Revit *Elements*. A Revit Document can represent a building model (*Revit Projects*) or can represent an *Custom Family* definition (*Revit Families*).

{% capture link_note %}
See [Revit: Documents]({{ site.baseurl }}{% link _en/1.0/guides/revit-docs.md %}) guide on how to work with Revit documents, linked documents, and querying elements inside them using Grasshopper in Revit.
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-docs.png' %}
