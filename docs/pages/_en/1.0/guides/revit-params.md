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

Now hold {% include ltr/kb_key.html key='Shift' %} and double-click on the {% include ltr/comp.html uuid='fad33c4b' %} component to see a list of all parameters associated with given element.  This will include built-in, project and shared parameters associated with the element.

![]({{ "/static/images/guides/revit-params-inspect-expanded.png" | prepend: site.baseurl }})

You can connect any of these properties, then {% include ltr/kb_key.html key='Ctrl' %} and double-click on the {% include ltr/comp.html uuid='fad33c4b' %} component to collapse it to normal size. The component is smart to keep the connected parameters shown in collapsed mode.

![]({{ "/static/images/guides/revit-params-inspect-collapsed.png" | prepend: site.baseurl }})

To inspect an existing parameter's definition use the {% include ltr/comp.html uuid='3bde5890' %} component:

![]({{ "/static/images/guides/param-identity.png" | prepend: site.baseurl }})

### Finding an Elements Parameter

To find a parameter associated with an element, pass the element and parameter name to the  {% include ltr/comp.html uuid='44515a6b' %} component:

![]({{ "/static/images/guides/param-find-builtin.png" | prepend: site.baseurl }})

### Parameter Scope

Parameters can be attached to the Element Type or singular Instance. Input the {% include ltr/comp.html uuid='ef607c2a' %} directly into the {% include ltr/comp.html uuid='fad33c4b' %} component to inspect both the Instance Parameters.  Get the  {% include ltr/comp.html uuid='fe427d04' %} component into the {% include ltr/comp.html uuid='fad33c4b' %} component to inspect the Type Parameters.  You can see that the available Parameters are different.

![]({{ "/static/images/guides/revit-params-instance-type.png" | prepend: site.baseurl }})

### Finding the Built-In Parameters

Use the {% include ltr/comp.html uuid='c1d96f56' %} component to reference parameters that are built into Revit.  Double-click on the title to search for part of the name of the parameter.

![]({{ "/static/images/guides/revit-params-querybuiltin.png" | prepend: site.baseurl }})

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

## Setting Parameter Values

Use the same {% include ltr/comp.html uuid='f568d3e7' %} component to set a parameter value on a Revit element. Keep in mind that some parameters are Read-only and their value can not be overridden.

![]({{ "/static/images/guides/revit-params-setfromname.png" | prepend: site.baseurl }})

Notice that the {% include ltr/comp.html uuid='f568d3e7' %} component is only holding a reference to the Revit element. So when the parameter value is updated by the component, it is updated for all the components that is referencing that same element. This is different from what you might be used to when working with Grasshopper outside of Revit context.

![]({{ "/static/images/guides/revit-params-setverify.png" | prepend: site.baseurl }})

## Creating Parameters

The components under the *Parameter* panel in Grasshopper, allow you to create new Parameters in Revit.

The process in creating a few steps process. 

1. A Parameter definition must first be made.
2. The Parameter then is added to the document
3. For Project Parameters, additional Category and Group must be set. (Optional)
4. Then the value of the Parameter may be set on the Element, Type or Global.

Parameter definitions can be created through the {% include ltr/comp.html uuid='134b7171' %} component or be read directly out of a [Shared Parameter File](#Shared_Parameters_File).

Once the definition is created, then the Parameter can be added to the project using the {% include ltr/comp.html uuid='84ab6f3c' %} component. If the scope of the [parameter is Global](#Global_Parameters), then the parameter value can now be set the {% include ltr/comp.html uuid='32e77d86' %} component. [Project Parameters](#Adding_a_Project_parameter) require additional properties to be set before used.

![]({{ "/static/images/guides/param-global-create.png" | prepend: site.baseurl }})

### Adding a Project parameter

Project parameters have addition properties beyond a basic Parameter Definition. Project parameters belong to certain Categories and the values can may vary across elements.

![]({{ "/static/images/guides/revit-params-definekeycomp.png" | prepend: site.baseurl }})

Create a new parameter by connecting the parameter name to the {% include ltr/comp.html uuid='134b7171' %} component on the canvas. Then use the {% include ltr/comp.html uuid='8ab856c6' %} and {% include ltr/comp.html uuid='5d331b12' %} to set the parameter type and group inputs. The {% include ltr/comp.html uuid='134b7171' %}  component will create a new parameter definition. This definition can then be passed to {% include ltr/comp.html uuid='84ab6f3c' %} component to actually create the parameter in the Revit project. You can inspect the created parameter using the {% include ltr/comp.html uuid='3bde5890' %} component.

![]({{ "/static/images/guides/revit-params-createshared.png" | prepend: site.baseurl }})

{% include ltr/warning_note.html note='Currently Revit API does not support creating project parameters directly.  So project parameters created by Grasshopper are Shared Project Parameters' %}

Here is the parameter configuration in Shared Parameters:

![]({{ "/static/images/guides/revit-params-sharedwindow.png" | prepend: site.baseurl }})

The value of this parameter can later be read or set by passing the parameter name to the {% include ltr/comp.html uuid='f568d3e7' %} component. You can inspect the parameter value using the {% include ltr/comp.html uuid='fad33c4b' %} component, and passing the parameter into {% include ltr/comp.html uuid='3bde5890' %} component:

![]({{ "/static/images/guides/revit-params-valueinfo.png" | prepend: site.baseurl }})

## Shared Parameters File

Using the {% include ltr/comp.html uuid='7844b410' %} component to read the shared parameter file.  This will return the current file path, the groups and the parameter definitions. The file only contains definitions, Parameters must be added to the current project before values can be set. The {% include ltr/comp.html uuid='84ab6f3c' %} component adds the Parameter and {% include ltr/comp.html uuid='f568d3e7' %} component sets the value in the parameter on an element.

![]({{ "/static/images/guides/param-shared-file.png" | prepend: site.baseurl }})

## Global Parameters

Global parameters must be queried from the current project by using the Global scope into the {% include ltr/comp.html uuid='d82d9fc3' %} component. Then get or set the parameter value using the {% include ltr/comp.html uuid='32e77d86' %} component:

![]({{ "/static/images/guides/param-global.png" | prepend: site.baseurl }})

Add Global parameters to a project thru the stndard [Add Pararameter Process](#creating-parameters).

## Parameter Formula

Use formulas in dimensions and parameters to drive and control parametric content in a model. The {% include ltr/comp.html uuid='21f9f9c6' %} component can be used to create Parameter Formulas using the syntax as covered in the [Valid Formula Syntax and Abbreviations](https://help.autodesk.com/view/RVT/2022/ENU/?guid=GUID-B37EA687-2BDF-4712-9951-2088B2A8E523)