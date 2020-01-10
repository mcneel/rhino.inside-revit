---
title: Revit Parameters
order: 10
---

In this guide we will take a look at how to read the parameters from a Revit element using Grasshopper. But first let's take a look at various parameter types that we encounter when working with Revit elements.

### Built-in Parameters

These are the most obvious set of parameters that are built into Revit based on the element type. For example, a Wall, or Room element has a parameter called *Volume*. This parameter does not make sense for a 2D Filled Region element and thus is not associated with that element type.

Revit shows the list of built-in parameters in the *Element Properties* panel.

![](/static/images/guides/revit-params01.png)

{% capture api_note %}
In Revit API, all the built-in parameters are represented by the {% include api_type.html type='Autodesk.Revit.DB.BuiltInParameter' title='DB.BuiltInParameter' %} enumeration.
{% endcapture %}
{% include ltr/en/api_note.html note=api_note %}

### Project/Shared Parameters

Revit allows a user to create a series of custom parameters and apply them globally to selected categories. The *Element Properties* panel displays the project parameters attached to the selected element as well.


## Inspecting Parameters

Let's bring a single element into a new Grasshopper definition. We can use the *Element.Decompose* component to inspect the element properties.

![](/static/images/guides/revit-params02.png)

Now hold SHIFT and double-click on the *Element.Decompose* component to see a list of all parameters associated with given element

![](/static/images/guides/revit-params03.png)

You can connect any of these properties, then CTRL and double-click on the *Element.Decompose* component to collapse it to normal size. The component is smart to keep the connected parameters shown in collapsed mode.

![](/static/images/guides/revit-params04.png)

Another way of reading parameter values is by specifying the parameter name to the *Element.ParameterGet* component to get the parameter value.

![](/static/images/guides/revit-params05.png)

{% include ltr/en/locale_note.html note='Since we are specifying the name of parameter in a specific language, the definition will break if opened on a Revit with a different language. A better way (but a lot less intuitive) is to specify the API integer value of the built-in parameter as input value. You can get this value by converting the DB.BuiltInParameter value to an int in python.' image='/static/images/guides/revit-params06.png' %}

When working with Project and Shared parameters, you can also pass the parameter GUID to the component

![](/static/images/guides/revit-params07.png)

## Updating Parameters

Use the *Element.ParameterSet* component to set a parameter value on a Revit element. The component is similar to *Element.ParameterGet* except that is also takes a value to be applied to the parameter. Keep in mind that some parameters are Read-only and their value can not be overridden.


![](/static/images/guides/revit-params08.png)

Notice that the *Geometry Element* component is only holding a reference to the Revit element. So when the parameter value is updated by the *Element.ParameterGet* component, it is updated for all the components that is referencing that same element. This is different from what you might be used to when working with Grasshopper outside of Revit context.

![](/static/images/guides/revit-params09.png)

