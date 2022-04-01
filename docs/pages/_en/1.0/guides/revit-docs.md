---
title: "Revit: Documents & Links"
subtitle: Where all the elements are stored and shared
order: 25
group: Essentials
home: true
thumbnail: /static/images/guides/revit-docs.png
ghdef: revit-docs.ghx
---

{% capture link_note %}
In this guide we will take a look at how to work with Revit Documents and Links using Grasshopper inside Revit.
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-docs.png' %}

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


## Saving Documents

Use the Save Document component, to save the given documents into an output file. The output file is passed to the component as a path string. Make sure that the path is ending with the appropriate file extension for the given document:

![]({{ "/static/images/guides/revit-docs-save.png" | prepend: site.baseurl }})

## Querying Linked Documents

{% capture api_note %}
When Revit loads a model, it also loads all the linked models into the memory as well. Each Revit model is represented by an instance of {% include api_type.html type='Autodesk.Revit.DB.Document' title='DB.Document' %}. The `DB.Document.IsLinked` shows whether the document has been loaded as a link for another document. Revit can not open two instances of the same model in a Revit session. This is the primary reason that you can not edit a linked model, without unloading it from the host model first
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

{% include youtube_player.html id="UkIW-0U0-Yk" %}

Use the {% include ltr/comp.html uuid='ebccfdd8' %} component shown here to get all the documents that are linked into the active (or given) document.

![]({{ "/static/images/guides/revit-links-doclinks-1.4.png" | prepend: site.baseurl }})

The links output contains the document name, Location, Shared Location Name and the unique instance ID for that link.

The documents output contains the name of the linked document and can be used in the document input of the query components.

## Querying Linked Elements

Use the {% include ltr/comp.html uuid='0f7da57e' %} component shown here to access elements if a given document. The input document can be a linked document as well.

![]({{ "/static/images/guides/revit-links-querywalls1.4.png" | prepend: site.baseurl }})

When the {% include ltr/comp.html uuid='0f7da57e' %} component is used to find elements in linked models, will import in their base orientation as they sit in the linked model. Linked elements then must be oriented to the location of the link instance in the host project. See the [Linked Geometry Orientation](#linked-geometry-orientation) below.


## Linked Geometry Orientation

The graphic elements will come into the file as they are oriented in their base project. It is necessary to orient the geometry into the location of the link instance.  Use the Orient Component for this:

![]({{ "/static/images/guides/revit-links-doclinks-orient.png" | prepend: site.baseurl }})
