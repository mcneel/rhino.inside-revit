---
title: Links
order: 74
---

{% include ltr/en/wip_note.html %}

## Querying Linked Documents

{% capture api_note %}
When Revit loads a model, it also loads all the linked models into the memory as well. Each Revit model is represented by an instance of {% include api_type.html type='Autodesk.Revit.DB.Document' title='DB.Document' %}. The `DB.Document.IsLinked` shows whether the document has been loaded as a link for another document. Revit can not open two instances of the same model in a Revit session. This is the primary reason that you can not edit a linked model, without unloading it from the host model first
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

Use the *Get Linked Docs* component shared here to get all the documents that are linked into the active (or given) document

![]({{ "/static/images/guides/revit-links01.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Get Linked Docs.ghuser' name='Get Linked Docs' %}

## Accessing Linked Document Elements

The build in *Document.Elements* component does not accept an input document and only works on the active document. Use the *Get Doc Elements* component shared here to access elements if a given document. The input document can be a linked document as well.

![]({{ "/static/images/guides/revit-links02.png" | prepend: site.baseurl }})

Use the **T** toggle to list element types as well

![]({{ "/static/images/guides/revit-links02a.png" | prepend: site.baseurl }})

{% include ltr/download_comp.html archive='/static/ghnodes/Get Doc Elements.ghuser' name='Get Doc Elements' %}

## Unloading/Reloading Links

## Replacing Links

## Finding Link Instances
