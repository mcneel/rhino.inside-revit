---
title: "Revit: Parameters"
subtitle: All the different types of Parameters
order: 22
group: Essentials
home: true
thumbnail: /static/images/guides/revit-params.png
ghdef: revit-params.ghx
---

{% capture link_note %}
In this guide we will take a look at how to read the parameters from a Revit element using Grasshopper. To review how parameters are organized in Revit, see the [Revit Elements:Parameters Guide]({{ site.baseurl }}{% link _en/1.0/guides/revit-revit.md %}#parameters)
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-params.png' %}

## Inspecting Parameters

Let's bring a single element into a new Grasshopper definition. We can use the {% include ltr/comp.html uuid='fad33c4b' %} component to inspect the element properties.

![]({{ "/static/images/guides/revit-params-inspect.png" | prepend: site.baseurl }})

Now hold {% include ltr/kb_key.html key='Shift' %} and double-click on the {% include ltr/comp.html uuid='fad33c4b' %} component to see a list of all parameters associated with given element

![]({{ "/static/images/guides/revit-params-inspect-expanded.png" | prepend: site.baseurl }})

You can connect any of these properties, then {% include ltr/kb_key.html key='Ctrl' %} and double-click on the {% include ltr/comp.html uuid='fad33c4b' %} component to collapse it to normal size. The component is smart to keep the connected parameters shown in collapsed mode.

![]({{ "/static/images/guides/revit-params-inspect-collapsed.png" | prepend: site.baseurl }})

### Finding the BuiltInParameter

Use the {% include ltr/comp.html uuid='c1d96f56' %} component to reference parameters that are built into Revit:

![]({{ "/static/images/guides/revit-params-querybuiltin.png" | prepend: site.baseurl }})

To find a built-in parameter associated with an element parameter, pass the element and parameter name to the *Find BuiltInParameter* shared here.:

![]({{ "/static/images/guides/revit-params-getbuiltin.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Find BuiltInParameter.ghuser' name='Find BuiltInParameter' %}

## Reading Parameter Values

A language-safe way to query the values for specific parameter is to use the {% include ltr/comp.html uuid='a550f532' %} parameter from the Revit Parameters panel

![]({{ "/static/images/guides/revit-params-paramkeycomp.png" | prepend: site.baseurl }})

After adding this component to the canvas, you can Right-Click on the component and select the desired parameter

![]({{ "/static/images/guides/revit-params-paramkey.png" | prepend: site.baseurl }})

The output of this component can be passed to the {% include ltr/comp.html uuid='f568d3e7' %} to query the value

![]({{ "/static/images/guides/revit-params-getfromkey.png" | prepend: site.baseurl }})

Another way of reading parameter values is by specifying the parameter name to the {% include ltr/comp.html uuid='f568d3e7' %} component to get the parameter value.

![]({{ "/static/images/guides/revit-params-getfromname.png" | prepend: site.baseurl }})

{% include ltr/locale_note.html note='Since we are specifying the name of parameter in a specific language, the definition will break if opened on a Revit with a different language' %}

When working with Shared parameters, you can also pass the parameter UUID to the component

![]({{ "/static/images/guides/revit-params-getfromuuid.png" | prepend: site.baseurl }})

## Updating Parameters

Use the same {% include ltr/comp.html uuid='f568d3e7' %} component to set a parameter value on a Revit element. Keep in mind that some parameters are Read-only and their value can not be overridden.

![]({{ "/static/images/guides/revit-params-setfromname.png" | prepend: site.baseurl }})

Notice that the {% include ltr/comp.html uuid='f568d3e7' %} component is only holding a reference to the Revit element. So when the parameter value is updated by the component, it is updated for all the components that is referencing that same element. This is different from what you might be used to when working with Grasshopper outside of Revit context.

![]({{ "/static/images/guides/revit-params-setverify.png" | prepend: site.baseurl }})


## Creating Shared Parameters

The components under the *Parameter* panel in Grasshopper, allow you to create new Shared Parameters in Revit.

{% include ltr/warning_note.html note='Currently Revit API does not support creating project parameters' %}

![]({{ "/static/images/guides/revit-params-definekeycomp.png" | prepend: site.baseurl }})

Create a new parameter by connecting the parameter name to the {% include ltr/comp.html uuid='134b7171' %} component on the canvas. Then use the {% include ltr/comp.html uuid='8ab856c6' %} and {% include ltr/comp.html uuid='5d331b12' %} to set the parameter type and group inputs. The {% include ltr/comp.html uuid='134b7171' %}  component will create a new parameter definition. This definition can then be passed to {% include ltr/comp.html uuid='84ab6f3c' %} component to actually create the parameter in the Revit project. You can inspect the created parameter using the {% include ltr/comp.html uuid='3bde5890' %} component.

![]({{ "/static/images/guides/revit-params-createshared.png" | prepend: site.baseurl }})

Here is how the parameter configuration in Shared Parameters:

![]({{ "/static/images/guides/revit-params-sharedwindow.png" | prepend: site.baseurl }})

The value of this parameter can later be read or set by passing the parameter name to the {% include ltr/comp.html uuid='f568d3e7' %} component. You can inspect the parameter value using the {% include ltr/comp.html uuid='fad33c4b' %} component, and passing the parameter into {% include ltr/comp.html uuid='3bde5890' %} component:

![]({{ "/static/images/guides/revit-params-valueinfo.png" | prepend: site.baseurl }})
