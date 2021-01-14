---
title: "Revit: Documents & Links"
subtitle: Where all the elements are stored and shared
order: 25
group: Essentials
thumbnail: /static/images/guides/revit-docs.png
ghdef: revit-docs.ghx
---

In this guide we will take a look at how to work with Revit Documents and Links using Grasshopper inside Revit.

## Querying Open Documents

Use the {% include ltr/comp.html uuid='5b935ca4' %} component to query the documents that are open in Revit:

![]({{ "/static/images/guides/revit-docs-opendocs.png" | prepend: site.baseurl }})

The {% include ltr/comp.html uuid='ee033516' %} component always refers to the currently active document. Be aware that when you switch documents in Revit interface, the output of the component will be updated to reflect the newly activated document:

![]({{ "/static/images/guides/revit-docs-activedoc.png" | prepend: site.baseurl }})

{% include ltr/bubble_note.html note='Note that the document components show the target document on the label at the bottom of the component' %}

Use the {% include ltr/comp.html uuid='94bd655c' %} to grab the identify information from the active document:

![]({{ "/static/images/guides/revit-docs-identity.png" | prepend: site.baseurl }})

## Document-Aware Components

Document-Aware components, can work on active or given documents. They have a hidden {% include ltr/misc.html uuid='f3427d5c' title='Document' %} input parameter that can be added by zooming in into the component:

![]({{ "/static/images/guides/revit-docs-docparam.gif" | prepend: site.baseurl }})

Once this input parameter is added, any Revit document can be passed into this input:

![]({{ "/static/images/guides/revit-docs-identityall.png" | prepend: site.baseurl }})

Here is another example of collecting wall instances from multiple source documents:

![]({{ "/static/images/guides/revit-docs-docelements.png" | prepend: site.baseurl }})


## Document Properties

Use the {% include ltr/comp.html uuid='c1c15806' %} component to inspect the file properties of the given document:

![]({{ "/static/images/guides/revit-docs-docfile.png" | prepend: site.baseurl }})

Use the {% include ltr/comp.html uuid='f7d56db0' %} to inspect the work-sharing properties of the given document:

![]({{ "/static/images/guides/revit-docs-wsidentity.png" | prepend: site.baseurl }})

## Opening Documents

{% include ltr/en/wip_note.html %}

## Saving Documents

Use the Save Document component, to save the given documents into an output file. The output file is passed to the component as a path string. Make sure that the path is ending with the appropriate file extension for the given document:

![]({{ "/static/images/guides/revit-docs-save.png" | prepend: site.baseurl }})

## Querying Linked Documents

{% capture api_note %}
When Revit loads a model, it also loads all the linked models into the memory as well. Each Revit model is represented by an instance of {% include api_type.html type='Autodesk.Revit.DB.Document' title='DB.Document' %}. The `DB.Document.IsLinked` shows whether the document has been loaded as a link for another document. Revit can not open two instances of the same model in a Revit session. This is the primary reason that you can not edit a linked model, without unloading it from the host model first
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

Use the {% include ltr/comp.html uuid='ebccfdd8' %} component shown here to get all the documents that are linked into the active (or given) document

![]({{ "/static/images/guides/revit-links-doclinks.png" | prepend: site.baseurl }})

## Querying Linked Elements

Use the {% include ltr/comp.html uuid='0f7da57e' %} component shown here to access elements if a given document. The input document can be a linked document as well.

![]({{ "/static/images/guides/revit-links-querywalls.png" | prepend: site.baseurl }})

You can chain the {% include ltr/comp.html uuid='5b935ca4' %} component into the {% include ltr/comp.html uuid='ebccfdd8' %} component to grab all the linked documents from all the open documents:

![]({{ "/static/images/guides/revit-links-querywalls-alldocs.png" | prepend: site.baseurl }})

## Unloading/Reloading Links

{% include ltr/en/wip_note.html %}

## Replacing Links

{% include ltr/en/wip_note.html %}

## Finding Link Instances

{% include ltr/en/wip_note.html %}
