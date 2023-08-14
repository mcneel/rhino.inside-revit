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

  public class DirectShapeByPoint : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("7A889B89-C423-4ED8-91D9-5CECE1EE803D");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public DirectShapeByPoint() : base
    (
      name: "Add Point DirectShape",
      nickname: "P-Shape",
      description: "Given a Point, it adds a Point shape to the active Revit document",
      category: "Revit",
      subCategory: "DirectShape"
    )
    { }

    void ReconstructDirectShapeByPoint
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [ParamType(typeof(Parameters.GraphicalElement)), Name("Point"), NickName("P"), Description("New Point Shape")]
      ref ARDB.DirectShape element,

      Point3d point
    )
    {
      if (!ThrowIfNotValid(nameof(point), point))
        return;

      var genericModel = new ARDB.ElementId(ARDB.BuiltInCategory.OST_GenericModel);
      if (element is object && element.Category.Id == genericModel) element.Pinned = false;
      else ReplaceElement(ref element, ARDB.DirectShape.CreateElement(document, genericModel));

      using (var ctx = GeometryEncoder.Context.Push(element))
      {
        var transform = Transform.Translation(point / Revit.ModelUnits - Point3d.Origin);

        ctx.RuntimeMessage = (severity, message, invalidGeometry) =>
        {
          invalidGeometry = invalidGeometry?.Duplicate();
          invalidGeometry?.Transform(transform);
          AddGeometryConversionError((GH_RuntimeMessageLevel) severity, message, invalidGeometry);
        };

        element.SetShape(ReconstructDirectShapeComponent.ShapeEmpty);
        if (point.IsValid)
        {
          try
          {
            var shape = new ARDB.Point[] { ARDB.Point.Create(XYZExtension.Zero) };
            element.Pinned = false;
            element.Location.Move(-point.ToXYZ());
            element.SetShape(shape);
            element.Location.Move(point.ToXYZ());
          }
          catch (ConversionException e)
          {
            ThrowArgumentException(nameof(point), e.Message, point);
          }
          catch (Autodesk.Revit.Exceptions.ArgumentException e)
          {
            if (e.GetType() == typeof(Autodesk.Revit.Exceptions.ArgumentException))
              ThrowArgumentException(nameof(point), "Input geometry does not satisfy DirectShape validation criteria.", point);

            throw e;
          }
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"DirectShape geometry is empty. {{{element.Id.ToString("D")}}}");
      }
    }
  }
}
