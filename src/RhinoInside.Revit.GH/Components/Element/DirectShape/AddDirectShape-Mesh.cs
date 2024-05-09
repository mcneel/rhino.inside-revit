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

  public class AddDirectShapeMesh : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("5542506A-A09E-4EC9-92B4-F2B52417511C");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public AddDirectShapeMesh() : base
    (
      name: "Add DirectShape (Mesh)",
      nickname: "M-Shape",
      description: "Given a Mesh, it adds a Mesh shape to the active Revit document",
      category: "Revit",
      subCategory: "DirectShape"
    )
    { }

    void ReconstructAddDirectShapeMesh
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [ParamType(typeof(Parameters.GraphicalElement)), Name("Mesh"), NickName("M"), Description("New Mesh Shape")]
      ref ARDB.DirectShape element,

      Mesh mesh
    )
    {
      if (!ThrowIfNotValid(nameof(mesh), mesh))
        return;

      var bbox = mesh.GetBoundingBox(accurate: false);

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

        if (bbox.IsValid)
        {
          try
          {
            mesh.Transform(inverse);
            element.Pinned = false;
            element.Location.Move(-bbox.Center.ToXYZ());
            element.SetShape(mesh.ToShape());
            element.Location.Move(bbox.Center.ToXYZ());
          }
          catch (ConversionException e)
          {
            ThrowArgumentException(nameof(mesh), e.Message, bbox);
          }
          catch (Autodesk.Revit.Exceptions.ArgumentException e)
          {
            if (e.GetType() == typeof(Autodesk.Revit.Exceptions.ArgumentException))
              ThrowArgumentException(nameof(mesh), "Input geometry does not satisfy DirectShape validation criteria.", bbox);

            throw;
          }
        }
        else
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"DirectShape geometry is empty. {{{element.Id.ToString("D")}}}");
          element.SetShape(ReconstructDirectShapeComponent.ShapeEmpty);
        }
      }
    }
  }
}
