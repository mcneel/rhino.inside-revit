---
title: Worksets
order: 71
thumbnail: /static/images/guides/revit-worksets.png
subtitle: Workflows for Revit Worksets
group: Containers
---

<!-- https://github.com/mcneel/rhino.inside-revit/issues/92 -->

## Query Worksets

To get the Worksets in a document us the {% include ltr/comp.html uuid='311316ba' %} component. Right click on the Kind (K) to Expose Picker.

![]({{ "/static/images/guides/Revit-Worksets-Query.png" | prepend: site.baseurl }})

## Active Workset

To get the Active Workset use the {% include ltr/comp.html uuid='aa467c94' %} component. You can also Set the Active Workset when the input is added via the Zoom UI. 

![]({{ "/static/images/guides/Revit-Worksets-Active.png" | prepend: site.baseurl }})

## Ensure Workset

To make sure that a particular user created Workset is in the document use the {% include ltr/comp.html uuid='a406c6a0' %} component. This is also the way to create a new Workset in the document.

![]({{ "/static/images/guides/revit-workset-ensure.png" | prepend: site.baseurl }})


## Delete Workset

To delete a Workset in the REvit document use the {% include ltr/comp.html uuid='bf1b9be9' %} component.

![]({{ "/static/images/guides/revit-workset-delete.png" | prepend: site.baseurl }})


## Element Workset

To Get or Set a Elements Workset use the {% include ltr/comp.html uuid='b441ba8c' %} component.

![]({{ "/static/images/guides/Revit-Worksets-Element.png" | prepend: site.baseurl }})


## Workset Global Visibility

To Get or Set the Global Visibility of a Workset with the {% include ltr/comp.html uuid='2922af4a' %} component.

![]({{ "/static/images/guides/Revit-Worksets-vis-global.png" | prepend: site.baseurl }})

## Workset View Override

Get-Set workset visibility overrides on the specified View with {% include ltr/comp.html uuid='b062c96e' %} component. To select the particular override option Right Click on Visibility to Expose Picker. 

![]({{ "/static/images/guides/Revit-Worksets-vis-overrides.png" | prepend: site.baseurl }})


## Workset Identity

Workset properties Get-Set access component to workset information with the {% include ltr/comp.html uuid='c33cd128' %} component. To rename a Workset use the ZUI to expose the Name property.

![]({{ "/static/images/guides/Revit-Worksets-Identity.png" | prepend: site.baseurl }})

## Element Ownership Information

Use the {% include ltr/comp.html uuid='f68f96ec' %} component to get Element Ownership properties.

![]({{ "/static/images/guides/Revit-Worksets-Ownership.png" | prepend: site.baseurl }})

## Document Worksharing Information

Use the {% include ltr/comp.html uuid='f7d56db0' %} component to Get Document Worksharing properties.

![]({{ "/static/images/guides/Revit-Worksets-Document.png" | prepend: site.baseurl }})

## Document Server Information

Us the {% include ltr/comp.html uuid='2577a55b' %} component to get Document Server properties.

![]({{ "/static/images/guides/Revit-Worksets-Server.png" | prepend: site.baseurl }})