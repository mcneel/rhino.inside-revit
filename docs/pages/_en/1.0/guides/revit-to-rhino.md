---
title: "Revit to Rhino"
subtitle: How to extract geometry and data from Revit into Rhino
order: 30
thumbnail: /static/images/guides/revit-to-rhino.png
group: Essentials
---

This guide covers at sending Revit Elements to Rhino.

There are 3 stages to each of the examples below:

1. Select Revit elements to import.
1. Extract geometry, categories names or any other needed information about each of the Revit elements.
1. Baking the geometry and information into Rhino.  In this each case a simple Python bake component is used to create geometry in Rhino on a layer names for the category the elements came from in Revit.

![]({{ "/static/images/guides/revit-view-to-rhino.png" | prepend: site.baseurl }})

The Element Geometry component has a hidden Category output that is useful.  Zoom in on the component and click on the "+" symbol under the Geometry Output to expose the Category output.

For baking many plugins can be used to attach information to Rhino objects such as Layer, Object Name, Color, Material, or UserData (Key/Value) information. Popular ones include [Human](https://www.food4rhino.com/en/app/human), [LunchBox](https://www.food4rhino.com/en/app/lunchbox) and [Elefront](https://www.food4rhino.com/en/app/elefront).  Or, download the simple download the [Python Bake Component]().

{% include youtube_player.html id="DVzsSyxTQS0" %}

## Selecting what to import

Grasshopper can select objects in Revit in various ways.  Three of the most common are:

1. Use a View filter. This can be the the easiest way to bring in what is visible.
2. Use Category filters, which can be useful in many situations. Although, Category selection can get complicated because Categories are quite broad.
3. Selecting the elements directly in Revit is simple a straight forward.

### Visible in View (Recommended)

One of the best ways to select Revit objects to import into Rhino is by what is visible in a specific view. Saved in a view is the needed categories, worksets and phase.  Creating a specific view just for importing to Rhino is a good way to store the configuration.  This way the current state of any working view is not a concern.

![]({{ "/static/images/guides/revit-to-rhino-select.png" | prepend: site.baseurl }})

Note in the illustration above:

1. The view name "To Rhino" is a 3D view saved in the current project.
1. {% include ltr/comp.html uuid="df691659-" %} returns the view element, that is used as input into the {% include ltr/comp.html uuid="ac546f16-" %} .
1. The Filter is used to Query the elements.  The "Limit" input has been hidden by zooming into the component and hitting the "-" button on the component.
1. Once the Elements are selected, the rest of the definition is the same as the other examples here.

### Select by Filter

A popular way of selecting object is by {% include ltr/comp.html uuid="d08f7ab1-" %}. This can be a quick way to get what is needed, but be aware that categories can be quite broad. Often using {% include ltr/comp.html uuid="d08f7ab1-" %} along with additional filters works to filter out the extra elements. For example {% include ltr/comp.html uuid="6804582b-" %} or {% include ltr/comp.html uuid="805c21ee-" %} may be need to be combined with a filter to limit what is selected.

Also, when trying to get a lot of different categories the list of categories needed might be extensive.  The list can become quite overwhelming quite quickly.

![]({{ "/static/images/guides/categories-rir-to-rhino.png" | prepend: site.baseurl }})

### Select Graphic Element

The simplest way to get Revit Elements is to use a {% include ltr/comp.html uuid="ef607c2a-" %} to input into the {% include ltr/comp.html uuid="b3bcbf5b-" %}. It will return only the object selected.

![]({{ "/static/images/guides/gh-revit-select-to-rhino.png" | prepend: site.baseurl }})

## Spatial Elements

Revit Spatial elements such as rooms and areas can be baked in Rhino. See the [Getting Spatial Element Geometry Guide](https://www.rhino3d.com/inside/revit/1.0/guides/revit-spatial#getting-spatial-element-geometry)

## Analytical Elements

Many Elements in Revit can generate Analytical Elements that can be imported into Rhino. See the [Working with Analytical Models Guide](https://www.rhino3d.com/inside/revit/1.0/guides/revit-struct) for more details.
