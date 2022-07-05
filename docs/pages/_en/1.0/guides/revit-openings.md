---
title: Openings
order: 48
thumbnail: /static/images/guides/revit-opening.png
subtitle: Workflows for Revit Openings
group: Modeling
---

## Querying Openings

{% capture api_note %}
In Revit API, Openings of all types are represented by the {% include api_type.html type='Autodesk.Revit.DB.Opening' title='DB.Opening' %}. The {% include ltr/comp.html uuid='18d46e90' %} primitive in {{ site.terms.rir }} represents Openings.
{% endcapture %}
{% include ltr/api_note.html note=api_note %}

To collect ALL openings of a certain type, you can use {% include ltr/comp.html uuid='0f7da57e' %} component with a {% include ltr/comp.html uuid='d08f7ab1' %} and select **Rectangular Straight Wall Opening** or **Rectangular Arc Wall Opening** (there are two categories since the geometry is calculated differerntly for straight vs arc walls) and **Shaft Openings** in {% include ltr/comp.html uuid='af9d949f' %}:

![]({{ "/static/images/guides/revit-openings-queyrwall.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-openings-queryshaft.png" | prepend: site.baseurl }})

But more often than not, we would like to find the openings that are created on certain Revit elements we are interested in. We can use {% include ltr/comp.html uuid='70ccf7a6' %} component to find the openings that are *Hosted* by a Revit element:

![]({{ "/static/images/guides/revit-openings-queryhostwall.png" | prepend: site.baseurl }})
![]({{ "/static/images/guides/revit-openings-queryhostroof.png" | prepend: site.baseurl }})

Note that *Shaft Openings* are not specifically placed on a Revit host. They are vertical elements that are bounded by two Levels and cut throught any Floor, Ceiling, or Roof elements in between. Using the {% include ltr/comp.html uuid='70ccf7a6' %} component does not return any *Shaft Openings* for this reason:

![]({{ "/static/images/guides/revit-openings-queryhostfloor.png" | prepend: site.baseurl }})

## Analyzing Openings

You can use the {% include ltr/comp.html uuid='fad33c4b' %} component to inspect class-level properties of an opening. This examples shows the properties on a **Rectangular Wall Opening** instance:

![]({{ "/static/images/guides/revit-openings-analyzeprops.png" | prepend: site.baseurl }})

You can also use the {% include ltr/comp.html uuid='e76b0f6b' %} component to extract the boundary profile for a given opening:

![]({{ "/static/images/guides/revit-openings-analyzeprofile.png" | prepend: site.baseurl }})

Use the {% include ltr/comp.html uuid='6723beb1' %} component to determine the element that is hosting the specific opening instance (e.g. Wall that has a sepcific opening)

![]({{ "/static/images/guides/revit-openings-queryarcwall.png" | prepend: site.baseurl }})

{% capture api_note %}
Currently dormers are not very well covered in Revit API. As you can see there are no properties other than Phases when inspecting a dormer:

![]({{ "/static/images/guides/revit-openings-analyzedormerprops.png" | prepend: site.baseurl }})

The dormer opening profile however can still be extracted by the {% include ltr/comp.html uuid='e76b0f6b' %} component:

![]({{ "/static/images/guides/revit-openings-analyzedormerprofile.png" | prepend: site.baseurl }})
{% endcapture %}
{% include ltr/api_note.html note=api_note %}


### Elements Cut by Shaft Opening

As mentioned before, **Shaft Openings** are not hosted on any specific Revit element. To determine elements (Floors in this example) that are cut by a shaft opening, we can use the {% include ltr/comp.html uuid='f5a32842' %} and filter for elements that are colliding with the shaft bounding box. The {% include ltr/comp.html uuid='18d46e90' %} contains the shaft opening we are interested in:

![]({{ "/static/images/guides/revit-openings-filtershaftfloors.png" | prepend: site.baseurl }})

### Element Cut Profile

To extract the boundary profile of a host element (Floors in this example), use the {% include ltr/comp.html uuid='032ad3f7' %} component and extract the Brep of the top or bottom face. Deconstructing this brep will also give you the curves:

![]({{ "/static/images/guides/revit-openings-floorprofile.png" | prepend: site.baseurl }})

![]({{ "/static/images/guides/revit-openings-floorprofile-sc.png" | prepend: site.baseurl }})


## Creating Wall Openings

To create a wall opening, just like when creating a wall opening in Revit UI, you would need two points on the wall surface. These points represent the min and max points of the rectangular cut. {% include ltr/comp.html uuid='c86ed84c' %} component takes the input wall and points and creates an opening element as a result:

![]({{ "/static/images/guides/revit-openings-createwall.png" | prepend: site.baseurl }})

## Creating Shafts

To create a shaft, use the {% include ltr/comp.html uuid='657811b7' %} component. Use the {% include ltr/comp.html uuid='bd6a74f3' %} to select the top and bottom bounding levels, and pass the shaft boundary curve as input as well:

![]({{ "/static/images/guides/revit-openings-createshaft.png" | prepend: site.baseurl }})

You can also use {% include ltr/comp.html uuid='01c853d8' %} component to construct a more specific level constraint with offset values as well and pass to the **Base** and **Top** inputs:

![]({{ "/static/images/guides/revit-openings-createshaftoffset.png" | prepend: site.baseurl }})

## Creating Vertical Openings

To create a vertical opening on a Roof, Ceiling, or Floor (Currently only non-sloped floors are supported by Revit API) use the {% include ltr/comp.html uuid='c9c0f4d2' %} component and pass the host element and boundary to it:

![]({{ "/static/images/guides/revit-openings-vertical.png" | prepend: site.baseurl }})

Notice that only non-sloped floors are currently supported by Revit API so no openings are created if the host floor has any slopes:

![]({{ "/static/images/guides/revit-openings-vertical-nofloor.png" | prepend: site.baseurl }})

