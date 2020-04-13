using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DirectShapeByCurve : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("77F4FBDD-8A05-44A3-AC54-E52A79CF3E5A");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public DirectShapeByCurve() : base
    (
      "Add Curve DirectShape", "CrvDShape",
      "Given a Curve, it adds a Curve shape to the active Revit document",
      "Revit", "DirectShape"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GraphicalElement(), "Curve", "C", "New CurveShape", GH_ParamAccess.item);
    }

    void ReconstructDirectShapeByCurve
    (
      DB.Document doc,
      ref DB.Element element,

      Rhino.Geometry.Curve curve
    )
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;

      ThrowIfNotValid(nameof(curve), curve);

      if (element is DB.DirectShape ds) { }
      else ds = DB.DirectShape.CreateElement(doc, new DB.ElementId(DB.BuiltInCategory.OST_GenericModel));

      var shape = curve.
                  ToHostMultiple(scaleFactor).
                  SelectMany(x => x.ToDirectShapeGeometry());

      ds.SetShape(shape.ToList());

      ReplaceElement(ref element, ds);
    }
  }
}
