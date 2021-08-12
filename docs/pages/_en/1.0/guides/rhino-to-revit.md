---
title: "Rhino to Revit"
subtitle: How to move geometry and data from Rhino into Revit
order: 31
thumbnail: /static/images/guides/rhino-to-revit.png
group: Essentials
---

In this guide we will take a look at how to send Rhino geometry to {{ site.terms.revit }} using Grasshopper. {{ site.terms.rir }} allows Rhino shapes and forms to be encoded into, and categorized as Revit elements. It is important to note that the easiest and quickest way of moving geometry into Revit may not be the best method. Determining which the final goal of the forms in Revit can improve the quality of the final Revit data structure and increase project efficiency.

Revit data model is based on a categorization system. Determining the best categories and subcategories to use will allow the elements to be drawn and scheduled properly. The challenge is that not every Revit category is available for each method discussed here.

There are 3 main ways to classify and move Rhino geometry to Revit. Each successive strategy increases the integration within a BIM model, but each strategy also takes a bit more planning. The 3 ways are:

1. Using [DirectShapes](#rhino-objects-as-directshapes) can be quite fast and takes the least amount of organizing. The limited organization and speed make DirectShapes best for temporary drawing sets such as competitions and early design presentations. DirectShapes may not the best for late project phases.
2. Developing [Loadable Families with Subcategories](#rhino-objects-as-loadable-families) works well for standalone elements in a model or elements that might be ordered or built by an independant fabricator. Being part of a Family, these objects could have their own set of drawings in addition to being part of the larger project drawings.
3. Use [Rhino geometry to generate Native Revit elements](#using-revit-built-in-system-families) is the best way to generate final Revit elements. While it is not always possible to create everything with native elements, native elements normally integrate best with the rest of the Revit team. These objects can potentially be edited without any dependency on {{ site.terms.rir }}. While the creating elements in this way can be limited, the resulting elements are native Revit elements.

Here is a Rhino model and quick Revit drawings for a competition model using DirectShapes:

![Competition model in Rhino]({{ "/static/images/guides/rhino-office-display.jpg" | prepend: site.baseurl }})

Through a simple Grasshopper script, objects can be categorized for elevations:

![A Quick Elevation in Revit]({{ "/static/images/guides/revit-office-elevation.jpg" | prepend: site.baseurl }})

And plan views using the categories to control graphics:

![A Quick Plan in Revit]({{ "/static/images/guides/revit-office-plan.jpg" | prepend: site.baseurl }})

### Rhino objects as DirectShapes

DirectShapes are the most obvious and many times the easiest way to get Geometry from Rhino into Revit. DirectShapes are generic Revit elements that can contain and categorize arbitrary non-parametric geometry inside the Revit model. However, since the geometry is not parametric, Revit does not know how they are created and can not resolved interactions between DirectShapes and other native elements. An example is that native Revit walls can not be extended to reach a DirectShape roof geometry.

Good reasons for using DirectShapes include:
1. Temporary models used in a competition or early design study submission for quick drawings.
1. Placeholders for part of the building that is still changing during design development. For instance, while the floor plates might be finished, the façade might be in flux in Grasshopper. Using a DirectShape as a placeholder for elevations and other design development drawings may work well.
1. A completely bespoke component or assembly that cannot be modeled using Revit native Families.

{% include youtube_player.html id="HAMPkiA5_Ug" %}

DirectShapes can be placed in any top level Category enabling graphic and material control thru Object Styles:

![Create a DirectShapes]({{ "/static/images/guides/rhino-to-revit-directshape.png" | prepend: site.baseurl }})

For additional graphic controls between elements within a category, [Rule-based View Filters](https://knowledge.autodesk.com/support/revit-products/learn-explore/caas/CloudHelp/cloudhelp/2019/ENU/Revit-DocumentPresent/files/GUID-145815E2-5699-40FE-A358-FFC739DB7C46-htm.html) can be applied with custom parameter values. DirectShapes cannot be placed in subcategories but the source geometry can be imported into Loadable Families and be assigned a cubcategory (discussed later in this guide):

![Add a Shared Parameter for a filter]({{ "/static/images/guides/directshape-filter-gh.png" | prepend: site.baseurl }})

In addition to pushing Rhino geometry into Revit as DirectShapes, it is also possible to create DirectShape types that can be inserted multiple times for repetitive elements:

![Insert multiple DirectShape instances]({{ "/static/images/guides/rhino-to-revit-directshape-instance.png" | prepend: site.baseurl }})

{% capture api_warning_note %}
DirectShapes created from smooth NURBS surfaces in Rhino may be imported as smooth solid or converted to a mesh by Revit. If the NURBS is converted to a mesh, that is a symptom that the NURBS geometry was rejected by Revit. There are many reasons for this, but very often this problem can be fixed in Rhino.
{% endcapture %}
{% include ltr/warning_note.html note=api_warning_note %}

### Rhino objects as Loadable Families

Rhino objects imported as forms inside a Revit family allow for inserting multiple instances of an object and also assigning [subcategories](https://knowledge.autodesk.com/support/revit-products/learn-explore/caas/CloudHelp/cloudhelp/2018/ENU/Revit-Customize/files/GUID-8C1F9882-E4AB-4E03-A735-8C44F19E194B-htm.html). You can use subcategories to control the visibility and graphics of portions of a family within a top level category.

Wrapping Rhino geometry inside Loadable Families have many advantages:
* Repeated objects can be inserted multiple times allowing forms to be scheduled and counted correctly
* Forms in loadable families can be edited by Revit if needed.
* Forms placed inside Family/Types can be placed in subcategories for further graphics control and scheduling.

As an example, here is an exterior walkway canopy in Rhino. It is a structure that will be built by a specialty fabricator. The small footings will be poured on-site and the rest of the walkway assembled above. Therefore, the footings are part of one family and the rest of the structure part of another family.

![An Exterior Walkway in Rhino]({{ "/static/images/guides/canopy-rhino.png" | prepend: site.baseurl }})

By mapping Rhino layers to subcategories in Revit an automated translation can be used. Graphics and materials can be controlled in Revit per subcategory and view:

![Plan view with Sub-categories]({{ "/static/images/guides/canopy-plan.png" | prepend: site.baseurl }})

![Elevation view with Sub-categories]({{ "/static/images/guides/canopy-elevation.png" | prepend: site.baseurl }})

The process of creating subcategories is covered in this video:

{% include youtube_player.html id="z57Ic0-4r2I" %}

Use the subcategory component to assign a subcategory to objects before sending them to the family creator component:

![Creating subcategory]({{ "/static/images/guides/subcategory-rhino-revit-gh.png" | prepend: site.baseurl }})

The subcategory component will create a new subcategory if it does not already exist.

Subcategory properties can be edited in the *Object Styles* dialog:

![An Exterior Walkway in Rhino]({{ "/static/images/guides/revit-objectstyles.jpg" | prepend: site.baseurl }})

Subcategories can also be used with [Rule-based View Filters](https://knowledge.autodesk.com/support/revit-products/learn-explore/caas/CloudHelp/cloudhelp/2019/ENU/Revit-DocumentPresent/files/GUID-145815E2-5699-40FE-A358-FFC739DB7C46-htm.html) for additional graphic control.

### Using Revit built-in System Families

Using built-in Revit *System Families* such as Walls, Floors, Ceilings, and Roofs can take the most amount of thought, however, the extra effort can be worth it. Advantages of native elements include:

1. Great integration in the project BIM schema including maximum graphic control, dynamic built-in parameter values and all access to all common project standard BIM parameters as any native elements would have
2. Elements can be edited even when {{ site.terms.rir }} is not available. The elements may have dimensions attached to them. The elements may be used to host other elements
3. Many Revit users downstream may not realize these elements were created with {{ site.terms.rir }}

Here is video on creating native Levels, Floors, Columns and Façade panels using {{ site.terms.rir }}:

{% include youtube_player.html id="cc3WLvGkWcc" %}

For more information on working with each category of Revit elements, see the [Modeling section in Guides]({{ site.baseurl }}{% link _en/1.0/guides/index.md %}#modeling-in-revit)
