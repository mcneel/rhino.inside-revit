---
title: "Data Model: Parameters"
order: 31
group: Revit Basics
ghdef: revit-params.ghx
---

In this guide we will take a look at how to read the parameters from a Revit element using Grasshopper. But first let's take a look at various parameter types that we encounter when working with Revit elements.

### Built-in Parameters

These are the most obvious set of parameters that are built into Revit based on the element type. For example, a Wall, or Room element has a parameter called *Volume*. This parameter does not make sense for a 2D Filled Region element and thus is not associated with that element type.

Revit shows the list of built-in parameters in the *Element Properties* panel.

![]({{ "/static/images/guides/revit-params-parampanel.png" | prepend: site.baseurl }})

{% capture api_note %}
In Revit API, all the built-in parameters are represented by the {% include api_type.html type='Autodesk.Revit.DB.BuiltInParameter' title='DB.BuiltInParameter' %} enumeration
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

### Project/Shared Parameters

Revit allows a user to create a series of custom parameters and apply them globally to selected categories. The *Element Properties* panel displays the project parameters attached to the selected element as well.

![]({{ "/static/images/guides/revit-params-projshared.png" | prepend: site.baseurl }})

### Global Parameters

Global parameters are category-agnostic parameters that could be applied to a range of instance or type parameters across many different Revit categories.

![]({{ "/static/images/guides/revit-params-global.png" | prepend: site.baseurl }})

{% include ltr/en/wip_note.html %}


## Inspecting Parameters

Let's bring a single element into a new Grasshopper definition. We can use the {% include ltr/comp.html uuid='fad33c4b' %} component to inspect the element properties.

![]({{ "/static/images/guides/revit-params-inspect.png" | prepend: site.baseurl }})

Now hold {% include ltr/kb_key.html key='Shift' %} and double-click on the {% include ltr/comp.html uuid='fad33c4b' %} component to see a list of all parameters associated with given element

![]({{ "/static/images/guides/revit-params-inspect-expanded.png" | prepend: site.baseurl }})

You can connect any of these properties, then {% include ltr/kb_key.html key='Ctrl' %} and double-click on the {% include ltr/comp.html uuid='fad33c4b' %} component to collapse it to normal size. The component is smart to keep the connected parameters shown in collapsed mode.

![]({{ "/static/images/guides/revit-params-inspect-collapsed.png" | prepend: site.baseurl }})

### Finding the BuiltInParameter

To find a built-in parameter associated with an element parameter, pass the element and parameter name to the *Find BuiltInParameter* shared here.

![]({{ "/static/images/guides/revit-params-getbuiltin.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Find BuiltInParameter.ghuser' name='Find BuiltInParameter' %}

## Reading Parameter Values

A language-safe way to query the values for specific parameter is to use the {% include ltr/comp.html uuid='a550f532' %} parameter from the Revit Parameters panel

![]({{ "/static/images/guides/revit-params-paramkeycomp.png" | prepend: site.baseurl }})

After adding this component to the canvas, you can Right-Click on the component and select the desired parameter

![]({{ "/static/images/guides/revit-params-paramkey.png" | prepend: site.baseurl }})

The output of this component can be passed to the {% include ltr/comp.html uuid='d86050f2' %} to query the value

![]({{ "/static/images/guides/revit-params-getfromkey.png" | prepend: site.baseurl }})

Another way of reading parameter values is by specifying the parameter name to the {% include ltr/comp.html uuid='d86050f2' %} component to get the parameter value.

![]({{ "/static/images/guides/revit-params-getfromname.png" | prepend: site.baseurl }})

{% include ltr/locale_note.html note='Since we are specifying the name of parameter in a specific language, the definition will break if opened on a Revit with a different language' %}

When working with Shared parameters, you can also pass the parameter UUID to the component

![]({{ "/static/images/guides/revit-params-getfromuuid.png" | prepend: site.baseurl }})

## Updating Parameters

Use the {% include ltr/comp.html uuid='8f1ee110' %} component to set a parameter value on a Revit element. The component is similar to {% include ltr/comp.html uuid='d86050f2' %} except that is also takes a value to be applied to the parameter. Keep in mind that some parameters are Read-only and their value can not be overridden.

![]({{ "/static/images/guides/revit-params-setfromname.png" | prepend: site.baseurl }})

Notice that the {% include ltr/comp.html uuid='ef607c2a' %} component is only holding a reference to the Revit element. So when the parameter value is updated by the {% include ltr/comp.html uuid='d86050f2' %} component, it is updated for all the components that is referencing that same element. This is different from what you might be used to when working with Grasshopper outside of Revit context.

![]({{ "/static/images/guides/revit-params-setverify.png" | prepend: site.baseurl }})


## Creating Shared Parameters

The components under the *Parameter* panel in Grasshopper, allow you to create new Shared Parameters in Revit.

{% include ltr/warning_note.html note='Currently Revit API does not support creating project parameters' %}

{% include ltr/warning_note.html note='The current implementation always creates Parameters of type **Text** and places them under the **Data** category in the Revit parameters panel. The parameter will be attached to all the categories in Revit' %}

![]({{ "/static/images/guides/revit-params-definekeycomp.png" | prepend: site.baseurl }})

Create a new parameter by connecting the parameter name to the {% include ltr/comp.html uuid='84ab6f3c' %} component on the canvas. You can inspect the created parameter using the {% include ltr/comp.html uuid='3bde5890' %} component.

![]({{ "/static/images/guides/revit-params-createshared.png" | prepend: site.baseurl }})

Here is how the parameter configuration in Shared Parameters:

![]({{ "/static/images/guides/revit-params-sharedwindow.png" | prepend: site.baseurl }})

The value of this parameter can later be read by passing the parameter name to the {% include ltr/comp.html uuid='d86050f2' %} component. You can inspect the parameter value using the {% include ltr/comp.html uuid='3bde5890' %} component.

![]({{ "/static/images/guides/revit-params-valueinfo.png" | prepend: site.baseurl }})
