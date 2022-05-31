---
title: Assemblies
order: 72
thumbnail: /static/images/guides/rir-ghpython.png

group: Containers
---

## Querying Assemblies

Use the {% include ltr/comp.html uuid='fd5b45c3' %} component to find all the existing Assemblies in the project.

![]({{ "/static/images/guides/revit-assembly-query.png" | prepend: site.baseurl }})

{% capture api_note %}
In Revit API, Worksets are represented by the {% include api_type.html type='Autodesk.Revit.DB.AssemblyInstance' title='DB.AssemblyInstance' %}.
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

## Finding Specific Assembly

To find a Assembly by name use the {% include ltr/comp.html uuid='fd5b45c3' %} component.

![]({{ "/static/images/guides/revit-assembly-query-typeName.png" | prepend: site.baseurl }})

## Reading Assembly Elements

Get or Set Assembly Elements using the {% include ltr/comp.html uuid='33ead71b' %} component.

![]({{ "/static/images/guides/revit-assembly-members.png" | prepend: site.baseurl }})



## Creating an Assembly

Create a new Assembly Type with the {% include ltr/comp.html uuid='6915b697' %} component.

![]({{ "/static/images/guides/revit-assembly-create.png" | prepend: site.baseurl }})



## Adding Assembly to Project

Add an Assembly Type to a Project Location with the {% include ltr/comp.html uuid='26feb2e9' %} component.

![]({{ "/static/images/guides/revit-assembly-add-location.png" | prepend: site.baseurl }})



## Disassemble Assembly

Disassemble Assembly instance with the {% include ltr/comp.html uuid='ff0f49ca' %} component.

![]({{ "/static/images/guides/revit-assembly-disassemble.png" | prepend: site.baseurl }})


