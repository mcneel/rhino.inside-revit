using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using DB = Autodesk.Revit.DB;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace RhinoInside.Revit.GH.Components.Elements.DirectShape
{
  public class DirectShapeByLocation : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("A811EFA4-8DE2-46F3-9F88-3D4F13FE40BE");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public DirectShapeByLocation() : base
    (
      "AddDirectShape.ByLocation", "ByLocation",
      "Given its location, it reconstructs a DirectShape into the active Revit document",
      "Revit", "Build"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GeometricElement(), "DirectShape", "DS", "New DirectShape", GH_ParamAccess.item);
    }

    void ReconstructDirectShapeByLocation
    (
      DB.Document doc,
      ref Autodesk.Revit.DB.Element element,

      [Description("Location where to place the element. Point or plane is accepted.")]
      Rhino.Geometry.Plane location,
      Autodesk.Revit.DB.DirectShapeType type
    )
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;

      if (element is DB.DirectShape ds && ds.Category.Id == type.Category.Id) { }
      else ds = DB.DirectShape.CreateElement(doc, type.Category.Id);

      if (ds.TypeId != type.Id)
        ds.SetTypeId(type.Id);

      var library = DB.DirectShapeLibrary.GetDirectShapeLibrary(doc);
      if (!library.ContainsType(type.UniqueId))
        library.AddDefinitionType(type.UniqueId, type.Id);

      var transform = Rhino.Geometry.Transform.PlaneToPlane(Rhino.Geometry.Plane.WorldXY, location.ChangeUnits(scaleFactor)).ToHost();
      ds.SetShape(DB.DirectShape.CreateGeometryInstance(doc, type.UniqueId, transform));

      var parametersMask = new DB.BuiltInParameter[]
      {
        DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
        DB.BuiltInParameter.ELEM_FAMILY_PARAM,
        DB.BuiltInParameter.ELEM_TYPE_PARAM
      };

      ReplaceElement(ref element, ds, parametersMask);
    }
  }
}
