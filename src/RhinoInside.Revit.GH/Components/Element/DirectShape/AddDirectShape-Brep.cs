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

  public class AddDirectShapeBrep : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("5ADE9AE3-588C-4285-ABC5-09DEB92C6660");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public AddDirectShapeBrep() : base
    (
      name: "Add DirectShape (Brep)",
      nickname: "B-Shape",
      description: "Given a Brep, it adds a Brep shape to the active Revit document",
      category: "Revit",
      subCategory: "DirectShape"
    )
    { }

    void ReconstructAddDirectShapeBrep
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [ParamType(typeof(Parameters.GraphicalElement)), Name("Brep"), NickName("B"), Description("New Brep Shape")]
      ref ARDB.DirectShape element,

      Brep brep
    )
    {
      if (!ThrowIfNotValid(nameof(brep), brep))
        return;

      var bbox = brep.GetBoundingBox(accurate: false);

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
            brep.Transform(inverse);
            element.Pinned = false;
            element.Location.Move(-bbox.Center.ToXYZ());
            element.SetShape(brep.ToShape());
            element.Location.Move(bbox.Center.ToXYZ());
          }
          catch (ConversionException e)
          {
            ThrowArgumentException(nameof(brep), e.Message, bbox);
          }
          catch (Autodesk.Revit.Exceptions.ArgumentException e)
          {
            if (e.GetType() == typeof(Autodesk.Revit.Exceptions.ArgumentException))
              ThrowArgumentException(nameof(brep), "Input geometry does not satisfy DirectShape validation criteria.", bbox);

            throw;
          }
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"DirectShape geometry is empty. {{{element.Id.ToString("D")}}}");
      }
    }
  }
}
