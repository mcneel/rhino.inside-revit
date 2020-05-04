import clr

clr.AddReference('RevitAPI')
clr.AddReference('RhinoCommon')
clr.AddReference('RhinoInside.Revit')

from Autodesk.Revit.DB import Transaction, ElementId, BuiltInCategory, DirectShape
from Rhino.Geometry import Point3d, Vector3d, Mesh, MeshingParameters, Sphere
from RhinoInside.Revit import Revit

import RhinoInside.Revit
clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)

doc = Revit.ActiveDBDocument

with Transaction(doc, "Sample7") as trans:
    trans.Start()
    
    sphere = Sphere(Point3d.Origin, 12 * Revit.ModelUnits)
    brep = sphere.ToBrep()
    meshes = Mesh.CreateFromBrep(brep, MeshingParameters.Default)

    category = ElementId(BuiltInCategory.OST_GenericModel)
    ds = DirectShape.CreateElement(doc, category)

    for mesh in meshes :
        ds.AppendShape(mesh.ToShape())

    trans.Commit()
