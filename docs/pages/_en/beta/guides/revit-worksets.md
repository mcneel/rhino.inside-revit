---
title: Working with Worksets
order: 71
---

<!-- https://github.com/mcneel/rhino.inside-revit/issues/92 -->

## Querying Worksets

Use the *Document Worksets* component shared here to get all the available worksets in the active document.

{% capture api_note %}
In Revit API, Worksets are represented by the {% include api_type.html type='Autodesk.Revit.DB.Workset' title='DB.Workset' %}
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

{% include ltr/bubble_note.html note='Revit has lots of built-in worksets. It is always better to list the **User Worksets** only.' %}

![]({{ "/static/images/guides/revit-worksets01.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Document Worksets.ghuser' name='Document Worksets' %}

## Finding Specific Worksets

To find a workset by name or by id in the active document, use the *Find Workset* component shared here.

![]({{ "/static/images/guides/revit-worksets02.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-worksets03.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Find Workset.ghuser' name='Find Workset' %}

## Reading Workset Properties

Use the *Workset Properties* component shared here to extract important properties of a workset.

![]({{ "/static/images/guides/revit-worksets04.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Workset Properties.ghuser' name='Workset Properties' %}

## Active Workset

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
