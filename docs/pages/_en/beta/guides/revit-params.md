---
title: Revit Parameters
order: 31
---

In this guide we will take a look at how to read the parameters from a Revit element using Grasshopper. But first let's take a look at various parameter types that we encounter when working with Revit elements.

### Built-in Parameters

These are the most obvious set of parameters that are built into Revit based on the element type. For example, a Wall, or Room element has a parameter called *Volume*. This parameter does not make sense for a 2D Filled Region element and thus is not associated with that element type.

Revit shows the list of built-in parameters in the *Element Properties* panel.

![]({{ "/static/images/guides/revit-params01.png" | prepend: site.baseurl }})

{% capture api_note %}
In Revit API, all the built-in parameters are represented by the {% include api_type.html type='Autodesk.Revit.DB.BuiltInParameter' title='DB.BuiltInParameter' %} enumeration
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### Project/Shared Parameters

Revit allows a user to create a series of custom parameters and apply them globally to selected categories. The *Element Properties* panel displays the project parameters attached to the selected element as well.


## Inspecting Parameters

Let's bring a single element into a new Grasshopper definition. We can use the *Element.Decompose* component to inspect the element properties.

![]({{ "/static/images/guides/revit-params02.png" | prepend: site.baseurl }})

Now hold {% include ltr/kb_key.html key='Shift' %} and double-click on the *Element.Decompose* component to see a list of all parameters associated with given element

![]({{ "/static/images/guides/revit-params03.png" | prepend: site.baseurl }})

You can connect any of these properties, then {% include ltr/kb_key.html key='Ctrl' %} and double-click on the *Element.Decompose* component to collapse it to normal size. The component is smart to keep the connected parameters shown in collapsed mode.

![]({{ "/static/images/guides/revit-params04.png" | prepend: site.baseurl }})

### Finding the BuiltInParameter

To find a built-in parameter associated with an element parameter, pass the element and parameter name to the *Find BuiltInParameter* shared here.

![]({{ "/static/images/guides/revit-params04a.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Find BuiltInParameter.ghuser' name='Find BuiltInParameter' %}

## Reading Parameter Values

A language-safe way to query the values for specific parameter is to use the *Parameter Key* component from the Revit Parameters panel

![]({{ "/static/images/guides/revit-params07a.png" | prepend: site.baseurl }})

After adding this component to the canvas, you can Right-Click on the component and select the desired parameter

![]({{ "/static/images/guides/revit-params07b.png" | prepend: site.baseurl }})

The output of this component can be passed to the *Element.ParameterGet* to query the value

![]({{ "/static/images/guides/revit-params07c.png" | prepend: site.baseurl }})

Another way of reading parameter values is by specifying the parameter name to the *Element.ParameterGet* component to get the parameter value.

![]({{ "/static/images/guides/revit-params05.png" | prepend: site.baseurl }})

{% include ltr/locale_note.html note='Since we are specifying the name of parameter in a specific language, the definition will break if opened on a Revit with a different language' %}

When working with Shared parameters, you can also pass the parameter GUID to the component

![]({{ "/static/images/guides/revit-params07.png" | prepend: site.baseurl }})

## Updating Parameters

Use the *Element.ParameterSet* component to set a parameter value on a Revit element. The component is similar to *Element.ParameterGet* except that is also takes a value to be applied to the parameter. Keep in mind that some parameters are Read-only and their value can not be overridden.


![]({{ "/static/images/guides/revit-params08.png" | prepend: site.baseurl }})

Notice that the *Geometry Element* component is only holding a reference to the Revit element. So when the parameter value is updated by the *Element.ParameterGet* component, it is updated for all the components that is referencing that same element. This is different from what you might be used to when working with Grasshopper outside of Revit context.

![]({{ "/static/images/guides/revit-params09.png" | prepend: site.baseurl }})


## Creating Shared Parameters

The components under the *Parameter* panel in Grasshopper, allow you to create new Shared Parameters in Revit.

{% include ltr/api_note.html note='Currently Revit API does not support creating project parameters' %}

{% include ltr/warning_note.html note='The current implementation always creates Parameters of type **Text** and places them under the **Data** category in the Revit parameters panel. The parameter will be attached to all the categories in Revit' %}

![]({{ "/static/images/guides/revit-params10.png" | prepend: site.baseurl }})

Create a new parameter by connecting the parameter name to the *AddParameterKey.ByName* component on the canvas. You can inspect the created parameter using the *ParameterKey.Decompose* component.

![]({{ "/static/images/guides/revit-params11.png" | prepend: site.baseurl }})

Here is how the parameter configuration in Shared Parameters:

![]({{ "/static/images/guides/revit-params12.png" | prepend: site.baseurl }})

The value of this parameter can later be read by passing the parameter name to the *Element.ParameterGet* component. You can inspect the parameter value using the *ParameterValue.Decompose* component.

![]({{ "/static/images/guides/revit-params13.png" | prepend: site.baseurl }})
