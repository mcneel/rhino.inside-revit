using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.DirectShapes
{
  using System.Windows;
  using Convert;
  using Convert.Geometry;
  using Kernel.Attributes;

  public class DirectShapeByMesh : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("5542506A-A09E-4EC9-92B4-F2B52417511C");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public DirectShapeByMesh() : base
    (
      name: "Add Mesh DirectShape",
      nickname: "MshDShape",
      description: "Given a Mesh, it adds a Mesh shape to the active Revit document",
      category: "Revit",
      subCategory: "DirectShape"
    )
    { }

    void ReconstructDirectShapeByMesh
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

      var genericModel = new ARDB.ElementId(ARDB.BuiltInCategory.OST_GenericModel);
      if (element is object && element.Category.Id == genericModel) { }
      else ReplaceElement(ref element, ARDB.DirectShape.CreateElement(document, genericModel));

      using (var ctx = GeometryEncoder.Context.Push(element))
      {
        var bbox = mesh.GetBoundingBox(accurate: false);
        var transform = Transform.Translation(Point3d.Origin - bbox.Center);
        var inverse = Transform.Translation(bbox.Center / Revit.ModelUnits - Point3d.Origin);

        ctx.RuntimeMessage = (severity, message, invalidGeometry) =>
        {
          invalidGeometry?.Transform(inverse);
          AddGeometryConversionError((GH_RuntimeMessageLevel) severity, message, invalidGeometry);
        };

        try
        {
          mesh.Transform(transform);
          element.SetShape(mesh.ToShape());
          element.Pinned = false;
          element.Location.Move(bbox.Center.ToXYZ());
        }
        catch (ConversionException e)
        {
          ThrowArgumentException(nameof(mesh), e.Message, mesh);
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException e)
        {
          if (e.GetType() == typeof(Autodesk.Revit.Exceptions.ArgumentException))
            ThrowArgumentException(nameof(mesh), "Input geometry does not satisfy DirectShape validation criteria.", mesh);

          throw e;
        }
      }
    }
  }
}
