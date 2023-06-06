---
title: Structural Elements
order: 49
thumbnail: /static/images/guides/revit-struct.png
subtitle: Workflows for Revit Structural Elements
group: Modeling
---

{% include youtube_player.html id="dpMFnJcvO5E" %}







{% capture link_note %}
When working with Revit or Revit API, we are mostly dealing with Revit **Types** and **Custom Families**. This guide takes you through the various Grasshopper components that help you query and create types and families. For a look at how these elements are organized within Revit, see [Revit: Types & Families]({{ site.baseurl }}{% link _en/1.0/guides/revit-revit.md %}#categories-families--types)
{% endcapture %}
{% include ltr/link_card.html note=link_note thumbnail='/static/images/guides/revit-types.png' %}

## Add Beam System

You can use the combination of a category picker components e.g. {% include ltr/comp.html uuid="af9d949f-" %}, the {% include ltr/comp.html uuid="d08f7ab1-" %} component, and {% include ltr/comp.html uuid="7b00f940-" %} component to collect types in a certain Revit category:

![]({{ "/static/images/guides/revit-struct-beamsystem.jpg" | prepend: site.baseurl }})


## Add Wall Foundation

Use the {% include ltr/comp.html uuid='7dea1ba3' %} to access information about that type. Please note that the *Family Name* parameter, returns the *System Family* name for *System Types* and the *Custom Family* name for Custom Types:

![]({{ "/static/images/guides/revit-struct-wallfoundation.jpg" | prepend: site.baseurl }})

## Add Truss

When querying the custom types that exist in a Revit model, we can find out the custom family definition that contains the logic for each of these types. We are using {% include ltr/comp.html uuid="742836d7" %} component to grab the family of each type being passed into this component. You can download this component, as a Grasshopper user object, from the link below.

![]({{ "/static/images/guides/revit-struct-truss.jpg" | prepend: site.baseurl }})
