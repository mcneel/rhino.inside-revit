using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.DirectShapes
{
  using Convert;
  using Convert.Geometry;
  using Kernel.Attributes;

  public class DirectShapeByCurve : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("77F4FBDD-8A05-44A3-AC54-E52A79CF3E5A");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public DirectShapeByCurve() : base
    (
      name: "Add Curve DirectShape",
      nickname: "CrvDShape",
      description: "Given a Curve, it adds a Curve shape to the active Revit document",
      category: "Revit",
      subCategory: "DirectShape"
    )
    { }

    void ReconstructDirectShapeByCurve
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [ParamType(typeof(Parameters.GraphicalElement)), Name("Curve"), NickName("C"), Description("New Curve Shape")]
      ref ARDB.DirectShape element,

      Curve curve
    )
    {
      if (!ThrowIfNotValid(nameof(curve), curve))
        return;

      var genericModel = new ARDB.ElementId(ARDB.BuiltInCategory.OST_GenericModel);
      if (element is object && element.Category.Id == genericModel) { }
      else ReplaceElement(ref element, ARDB.DirectShape.CreateElement(document, genericModel));

      using (var ctx = GeometryEncoder.Context.Push(element))
      {
        var bbox = curve.GetBoundingBox(accurate: false);
        var transform = Transform.Translation(Point3d.Origin - bbox.Center);
        var inverse = Transform.Translation(bbox.Center / Revit.ModelUnits - Point3d.Origin);

        ctx.RuntimeMessage = (severity, message, invalidGeometry) =>
        {
          invalidGeometry?.Transform(inverse);
          AddGeometryConversionError((GH_RuntimeMessageLevel) severity, message, invalidGeometry);
        };

        try
        {
          curve.Transform(transform);
          element.SetShape(curve.ToShape());
          element.Pinned = false;
          element.Location.Move(bbox.Center.ToXYZ());
        }
        catch (ConversionException e)
        {
          ThrowArgumentException(nameof(curve), e.Message, curve);
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException e)
        {
          if (e.GetType() == typeof(Autodesk.Revit.Exceptions.ArgumentException))
            ThrowArgumentException(nameof(curve), "Input geometry does not satisfy DirectShape validation criteria.", curve);

          throw e;
        }
      }
    }
  }
}
