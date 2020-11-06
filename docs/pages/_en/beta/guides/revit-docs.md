---
title: "Data Model: Documents"
order: 34
group: Revit Basics
ghdef: revit-docs.ghx
---

In simplest terms, Revit *Documents* are collections of Revit *Elements*. When using Revit, we call them *Revit Projects* and *Revit Families*.

## Querying Open Documents

Use the {% include ltr/comp.html uuid='5b935ca4' %} component to query the documents that are open in Revit:

![]({{ "/static/images/guides/revit-docs-opendocs.png" | prepend: site.baseurl }})

The {% include ltr/comp.html uuid='ee033516' %} component always refers to the currently active document. Be aware that when you switch documents in Revit interface, the output of the component will be updated to reflect the newly activated document:

![]({{ "/static/images/guides/revit-docs-activedoc.png" | prepend: site.baseurl }})

{% include ltr/bubble_note.html note='Note that the document components show the target document on the label at the bottom of the component' %}

Use the {% include ltr/comp.html uuid='94bd655c' %} to grab the identify information from the active document:

![]({{ "/static/images/guides/revit-docs-identity.png" | prepend: site.baseurl }})

## Document-Aware Components

Document-Aware components, can work on active or given documents. They have a hidden {% include ltr/param.html uuid='f3427d5c' title='Document' %} input parameter that can be added by zooming in into the component:

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
