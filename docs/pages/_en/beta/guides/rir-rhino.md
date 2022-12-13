---
title: Rhino in Revit
subtitle: How to use Rhino inside Revit
order: 12
thumbnail: /static/images/guides/rir-rhino.png
group: Essentials
---

## Rhino On the Side

When working with Rhino, having Rhino open on the side to see the geometry and previews is really valuable. But most of the times the complete Rhino's interface is not necessary. In these cases, you can tear off a Rhino view and keep that on the side instead:

![]({{ "/static/images/guides/rir-rhino-tornoffview.gif" | prepend: site.baseurl }})

## Import Rhino Files into Revit

The **Import 3DM File** is a quick method to import Rhino elements into a project or create a Family file from a Rhino model.  This also hmay be a good way to read in other filre formats into Rhino and then into Revit to help translate models into Revit.

There are two methods of import.

1. Import into a exiting Project file
1. Import into an exiting Family.

Data in the Rhino file will be translated into Revit based on each of these methods.

### Importing Rhino files as Directshapes into Project

When importing a 3DM file directly into a project the result will be a single Directshape.  THis is a quick way to bring in geomtery but some with all the limitations of Directshapes. 

On insert there are a number of options:

![]({{ "/static/images/guides/import-3dm-project.jpg" | prepend: site.baseurl }})

There are two methods:

Use the Layers control to import only the objects tat are on Visible Layers, or import all the geometry in the 3DM file.

Select the category that the DirectShapes should be created in using the Category dropdown.

The Workset control will allow Rhino to import the model directly into any existing workset that is not locked by another Rhino session.  This control will be blank if no worksets exist in the project.

DirectShapes have both a Family Name and a Type name that can be used. These two fields will default to the file name, but can be overridden by selecting an existing or typing a new name into the controls. If the same Family Name is used, then the import will add an additional Type into the existing Family.

Materials are only painted material in Revit.  This means that it can control the surface.

### Creating new Families from Rhino files

Importing Rhino models directly into Family files allows for a bit more flexibility. There is a bit more data that can be translated out of the Rhino files.  

![]({{ "/static/images/guides/import-3dm-family.jpg" | prepend: site.baseurl }})

One limitation of a family is that the NURBS geometry. If Revit rejects that NURBS geometry, it will not show up in Revit. most of the time this geometry can be fixed with Rhino with come work.

When importing into Revit into a family:

1. Layers will be translated into Subcategories.  This will allow the graphics to be adjusted in Revit based on the Layers of the Rhino file.
1. Materials in Rhino will be translated to Revit Materials by name.  If the name matches a Revit material then it will use the existing Revit.