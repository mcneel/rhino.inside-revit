---
title: "Data Model: Documents"
order: 34
group: Revit Basics
---

In simplest terms, Revit *Documents* are collections of Revit *Elements*. When using Revit, we call them *Revit Projects* and *Revit Families*.

## Querying Open Documents

Use the *All Documents* component to query the documents that are open in Revit:

![]({{ "/static/images/guides/revit-docs01.png" | prepend: site.baseurl }})

The *Active Document* component always refers to the currently active document. Be aware that when you switch documents in Revit interface, the output of the component will be updated to reflect the newly activated document:

![]({{ "/static/images/guides/revit-docs02.png" | prepend: site.baseurl }})

{% include ltr/bubble_note.html note='Note that the document components show the target document on the label at the bottom of the component' %}

Use the *Document Identity* to grab the identify information from the active document:

![]({{ "/static/images/guides/revit-docs03.png" | prepend: site.baseurl }})

## Document-Aware Components

Document-Aware components, can work on active or given documents. They have a hidden **Document** input parameter that can be added by zooming in into the component:

![]({{ "/static/images/guides/revit-docs03a.gif" | prepend: site.baseurl }})

Once this input parameter is added, any Revit document can be passed into this input:

![]({{ "/static/images/guides/revit-docs04.png" | prepend: site.baseurl }})

Here is another example of collecting wall instances from multiple source documents:

![]({{ "/static/images/guides/revit-docs06.png" | prepend: site.baseurl }})


## Document Properties

Use the *Document File* component to inspect the file properties of the given document:

![]({{ "/static/images/guides/revit-docs05.png" | prepend: site.baseurl }})

Use the *Document WorkSharing* to inspect the work-sharing properties of the given document:

![]({{ "/static/images/guides/revit-docs07.png" | prepend: site.baseurl }})


## Saving Documents

Use the Save Document component, to save the given documents into an output file. The output file is passed to the component as a path string. Make sure that the path is ending with the appropriate file extension for the given document:

![]({{ "/static/images/guides/revit-docs08.png" | prepend: site.baseurl }})
