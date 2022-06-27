---
title: "Revit: Categories"
subtitle: All working with Categories
order: 23
group: Essentials
home: true
thumbnail: /static/images/guides/revit-wip.png
ghdef: 
---

![]({{ "/static/images/guides/revit-types-categories.svg" | prepend: site.baseurl }})

Categories are the highest-level groups. These categories are built into Revit and loosely organize the components by their function. Categories cannot be removed or added-to in Revit. There are many Categories within Revit organized in various ways.

There are multiple category types in a Revit model:
  - *Model* categories e.g. *Walls*, *Doors*, *Floors*, *Roofs*, etc.
  - *Analytical* categories e.g. *Analytical Surfaces*, *Structural Loads*, etc.
  - *Annotation* categories e.g. *Tags*, *Dimensions*, etc.
  - *Internal* categories various Tags etc.

Model Categories vary in the elements they can contain. Developing and understanding of how commonly used categories will allow is important to using Revit. Categories may or may not contain:
  - *Directshapes*
  - *Loadable Families* e.g. *Furniture*, *Generic Model*, *Site* etc.
  - *System* Families/Types e.g. *Walls*, *Doors*, *Floors*, *Roofs*, etc.

 See [Rhino To Revit]({{ site.baseurl }}{% link _en/1.0/guides/rhino-to-revit.md%}) guide to understand how to add any these element types using Rhino and Grasshopper.

## Query Categories

There are two main ways to find categories in a document. 

Use the {% include ltr/comp.html uuid="d150e40e" %} component to query the document for the main categories and sub-categories that exist in a project. For query can be modified by using the {% include ltr/comp.html uuid="5ffb1339" %} component to filter for only certain categories types or use a Boolean toggle to select main categories or sub-categories.

![]({{ "/static/images/guides/gh-query-category.png" | prepend: site.baseurl }})

The second is to use the {% include ltr/comp.html uuid="af9d949f" %}. This selector will list both main categories and built-in sub-categories that exist in every Revit file.  Here this selected category is then run through the {% include ltr/comp.html uuid="d794361e" %} component to output the parts of the category identity

![]({{ "/static/images/guides/gh-built-in-category.png" | prepend: site.baseurl }})

{% include ltr/bubble_note.html note='The Category selector supports searching. Double-click on the component title and use the name input box to enter an exact name, alternatively you can enter a name pattern. If a pattern is used, the list will be filled up with all the items that match it. Several kind of patterns are supported, the method used depends on the first pattern character see Microsoft.VisualBasic Like Operator;Regular expression, see here as reference' %}

## Accessing Categories

 {% include ltr/comp.html uuid="d794361e" %} component can be establish the Parameters Type (Model, Annotation, Analysis) Name and whether it is a Main or Sub-category.

![]({{ "/static/images/guides/gh-built-in-category.png" | prepend: site.baseurl }})

A list of Parameters which belong to a Category can be created to assist in making a list of values that can be used to create schedules or set them in a convenient list.  The example below uses the  {% include ltr/comp.html uuid="af9d949f" %} component select the Door Category then to find the Doors in the project in addition to all the Parameters in the Door category with the  {% include ltr/comp.html uuid="189f0a94" %} component.

![]({{ "/static/images/guides/gh-category-parameter.png" | prepend: site.baseurl }})

Get and set the Graphics styles for a Category thru the {% include ltr/comp.html uuid="ca3c1cf9" %} component.

![]({{ "/static/images/guides/gh-category-graphic-style.png" | prepend: site.baseurl }})

## Extending Categories
Main Categories are built-in and cannot be edited. Although subcategories can be added within most Categories for further organization and refined control of Elements 

## Sub-Categories
Use the {% include ltr/comp.html uuid="8de336fb" %} component to add a subcategory.  If the sub-category already exist the component will simply return the existing sub-category.

![]({{ "/static/images/guides/gh-sub-category.png" | prepend: site.baseurl }})

Use the {% include ltr/comp.html uuid="4915ab87" %} component to create a list of sub-categories within a specific category.

![]({{ "/static/images/guides/gh-category-subcategory.png" | prepend: site.baseurl }})
