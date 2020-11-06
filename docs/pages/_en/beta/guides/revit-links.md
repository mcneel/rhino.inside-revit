---
title: Links
order: 74
group: Geometry Containers
ghdef: revit-links.ghx
---

## Querying Linked Documents

{% capture api_note %}
When Revit loads a model, it also loads all the linked models into the memory as well. Each Revit model is represented by an instance of {% include api_type.html type='Autodesk.Revit.DB.Document' title='DB.Document' %}. The `DB.Document.IsLinked` shows whether the document has been loaded as a link for another document. Revit can not open two instances of the same model in a Revit session. This is the primary reason that you can not edit a linked model, without unloading it from the host model first
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

Use the {% include ltr/comp.html uuid='ebccfdd8' %} component shown here to get all the documents that are linked into the active (or given) document

![]({{ "/static/images/guides/revit-links-doclinks.png" | prepend: site.baseurl }})

## Accessing Linked Document Elements

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
