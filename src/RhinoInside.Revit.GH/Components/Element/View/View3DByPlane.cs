using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  public class View3DByPlane : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("F7B775C9-05E0-40F7-85E9-5CC2EF79731E");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public View3DByPlane() : base
    (
      name: "Add View3D",
      nickname: "View3D",
      description: "Given a plane, it adds a 3D View to the active Revit document",
      category: "Revit",
      subCategory: "View"
    )
    { }

    void ReconstructView3DByPlane
    (
      [Optional, NickName("DOC")]
      DB.Document document,

      [Description("New 3D View")]
      ref DB.View3D view3D,

      Rhino.Geometry.Plane plane,
      Optional<DB.ElementType> type,
      Optional<string> name,
      Optional<bool> perspective
    )
    {
      SolveOptionalType(document, ref type, DB.ElementTypeGroup.ViewType3D, nameof(type));

      var orientation = new DB.ViewOrientation3D
      (
        plane.Origin.ToXYZ(),
        plane.YAxis.ToXYZ(),
        -plane.ZAxis.ToXYZ()
      );

      if (view3D is null)
      {
        var newView = perspective.IsNullOrMissing ?
        DB.View3D.CreatePerspective
        (
          document,
          type.Value.Id
        ) :
        DB.View3D.CreateIsometric
        (
          document,
          type.Value.Id
        );

        var parametersMask = new DB.BuiltInParameter[]
        {
          DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          DB.BuiltInParameter.ELEM_FAMILY_PARAM,
          DB.BuiltInParameter.ELEM_TYPE_PARAM
        };

        newView.SetOrientation(orientation);
        newView.get_Parameter(DB.BuiltInParameter.VIEWER_CROP_REGION).Set(0);
        ReplaceElement(ref view3D, newView, parametersMask);
      }
      else
      {
        if (view3D.IsLocked)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "View is locked and cannot be reoriented.");
        }
        else
        {
          view3D.SetOrientation(orientation);

          if (perspective.HasValue)
            view3D.get_Parameter(DB.BuiltInParameter.VIEWER_PERSPECTIVE).Set(perspective.Value ? 1 : 0);
        }

        ChangeElementTypeId(ref view3D, type.Value.Id);
      }

      if (name.HasValue && view3D is object)
      {
        try { view3D.Name = name.Value; }
        catch (Autodesk.Revit.Exceptions.ArgumentException e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{e.Message.Replace($".{Environment.NewLine}", ". ")}");
        }
      }
    }
  }
}
