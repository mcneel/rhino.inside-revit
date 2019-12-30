---
title: Creating Families in Revit with Grasshopper
description: This guide covers the basic transfer of Rhino geometry into Revit.
language: en
authors: ['scott_davidson']
languages: ['Python']
platforms: ['Windows']
categories: ['general']
order: 8
keywords: ['python', 'commands', 'grasshopper']
layout: toc-guide-page
---

# Creating Revit&reg; Families with Grasshopper&reg;
This sample shows how to create a new Revit family in with Rhino geometry. In this case, a column is created and placed along a curve.

There are a lot of ways to bring in the Rhino geometry, but in this case we will be bringing them in as Family Type instances. This has a big advantage over other methods.  If the Freeform geometry is wrapped within family then it has 3 advantages:

1. The Graphic properties can be edited including the hatching and section line.  This is a big advantage over directshapes.
2. The geometry can be edited directly in the Family editor, within reason.
3. The instances will schedule properly.

![Rhino to Revit as Directshape](/images/rhino-to-revit.jpg)

## Bringing Breps into a Revit category
Rhino surfaces and brep solids can be imported into Revit as a categorized DirectShape.

Open Sample files:
1. Open the [Rhino to Revit.rvt](/rhino_to_revit.rvt) in Revit.
1. Start Rhino inside Revit and open the [Rhino office.3dm](/rhino_office.3dm) file.
1. Start Grasshopper within Rhino.

The component necessary:
1. DirectShape Category picker
1. Directshape from Geometry
1. Brep Param component in Grasshopper

![Rhino Brep to Revit as Directshape](/images/rhino-to-revit-brep.jpg)
Once you select the Breps, those will feed into the Direct component.


