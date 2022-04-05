---
title: Assemblies
order: 72
thumbnail: /static/images/guides/rir-ghpython.png

group: Containers
---

## Querying Assemblies

Use the {% include ltr/comp.html uuid='fd5b45c3' %} component to find all the existing Assemblies in the project.

![]({{ "/static/images/guides/revit-worksets01.png" | prepend: site.baseurl }})

To find in another project, zoom into the {% include ltr/comp.html uuid='fd5b45c3' %} component and select the plus symbol to expose the document input.

![]({{ "/static/images/guides/revit-worksets01.png" | prepend: site.baseurl }})

{% capture api_note %}
In Revit API, Worksets are represented by the {% include api_type.html type='Autodesk.Revit.DB.AssemblyInstance' title='DB.AssemblyInstance' %}.
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

## Finding Specific Assembly

To find a Assembly by name {% include ltr/comp.html uuid='fd5b45c3' %} component shared here.

![]({{ "/static/images/guides/revit-worksets02.png" | prepend: site.baseurl }})

## Reading Assembly Elements

Use the *Workset Properties* component shared here to extract important properties of a workset.

![]({{ "/static/images/guides/revit-worksets04.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Workset Properties.ghuser' name='Workset Properties' %}

## Adding Elements to Assembly

To find the active workset in active document, use the *Active Workset* component shared here.

![]({{ "/static/images/guides/revit-worksets05.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Active Workset.ghuser' name='Active Workset' %}

## Setting Active Workset

To set the active workset in active document, use the *Set Active Workset* component shared here.

![]({{ "/static/images/guides/revit-worksets05a.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Set Active Workset.ghuser' name='Set Active Workset' %}

## Creating Worksets

To create a new workset in active document, use the *Create Workset* component shared here.

![]({{ "/static/images/guides/revit-worksets08.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Create Workset.ghuser' name='Create Workset' %}

## Getting Element Workset

To find workset of an element, use the *Get Workset* component shared here.

![]({{ "/static/images/guides/revit-worksets06.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Get Workset.ghuser' name='Get Workset' %}

## Setting Element Workset

To set workset of an element, use the *Set Workset* component shared here.

![]({{ "/static/images/guides/revit-worksets07.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Set Workset.ghuser' name='Set Workset' %}
