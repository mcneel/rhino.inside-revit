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
  using External.DB.Extensions;

  public class AddDirectShapeCurve : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("77F4FBDD-8A05-44A3-AC54-E52A79CF3E5A");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public AddDirectShapeCurve() : base
    (
      name: "Add DirectShape (Curve)",
      nickname: "C-Shape",
      description: "Given a Curve, it adds a Curve shape to the active Revit document",
      category: "Revit",
      subCategory: "DirectShape"
    )
    { }

    void ReconstructAddDirectShapeCurve
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

      var bbox = curve.GetBoundingBox(accurate: false);

      var genericModel = new ARDB.ElementId(ARDB.BuiltInCategory.OST_GenericModel);
      if (element is object && element.Category.Id == genericModel) element.Pinned = false;
      else ReplaceElement(ref element, ARDB.DirectShape.CreateElement(document, genericModel));

      using (var ctx = GeometryEncoder.Context.Push(element))
      {
        var transform = Transform.Translation(bbox.Center / Revit.ModelUnits - Point3d.Origin);
        var inverse = Transform.Translation(Point3d.Origin - bbox.Center);

        ctx.RuntimeMessage = (severity, message, invalidGeometry) =>
        {
          invalidGeometry = invalidGeometry?.Duplicate();
          invalidGeometry?.Transform(transform);
          AddGeometryConversionError((GH_RuntimeMessageLevel) severity, message, invalidGeometry);
        };

        element.SetShape(ReconstructDirectShapeComponent.ShapeEmpty);
        if (bbox.IsValid)
        {
          try
          {
            curve.Transform(inverse);
            element.Pinned = false;
            element.Location.Move(-bbox.Center.ToXYZ());
            element.SetShape(curve.ToShape());
            element.Location.Move(bbox.Center.ToXYZ());
          }
          catch (ConversionException e)
          {
            ThrowArgumentException(nameof(curve), e.Message, bbox);
          }
          catch (Autodesk.Revit.Exceptions.ArgumentException e)
          {
            if (e.GetType() == typeof(Autodesk.Revit.Exceptions.ArgumentException))
              ThrowArgumentException(nameof(curve), "Input geometry does not satisfy DirectShape validation criteria.", bbox);

            throw;
          }
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"DirectShape geometry is empty. {{{element.Id.ToString("D")}}}");
      }
    }
  }
}
