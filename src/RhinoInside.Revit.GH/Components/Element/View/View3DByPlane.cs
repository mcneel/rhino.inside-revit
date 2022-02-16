using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using Kernel.Attributes;

  [ComponentVersion(introduced: "1.0", updated: "1.5")]
  public class View3DByPlane : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("F7B775C9-05E0-40F7-85E9-5CC2EF79731E");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public View3DByPlane() : base
    (
      name: "Add 3D View",
      nickname: "3D View",
      description: "Given a plane, it adds a 3D View to the active Revit document",
      category: "Revit",
      subCategory: "View"
    )
    { }

    void ReconstructView3DByPlane
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [Description("New 3D View")]
      ref ARDB.View3D view3D,

      Rhino.Geometry.Plane plane,
      Optional<ARDB.ViewFamilyType> type,
      Optional<string> name,
      Optional<bool> perspective
    )
    {
      SolveOptionalType(document, ref type, ARDB.ElementTypeGroup.ViewType3D, nameof(type));

      var orientation = new ARDB.ViewOrientation3D
      (
        plane.Origin.ToXYZ(),
        plane.YAxis.ToXYZ(),
        -plane.ZAxis.ToXYZ()
      );

      if (view3D is null)
      {
        var newView = perspective.IsNullOrMissing ?
        ARDB.View3D.CreatePerspective
        (
          document,
          type.Value.Id
        ) :
        ARDB.View3D.CreateIsometric
        (
          document,
          type.Value.Id
        );

        var parametersMask = new ARDB.BuiltInParameter[]
        {
          ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
          ARDB.BuiltInParameter.ELEM_TYPE_PARAM
        };

        newView.SetOrientation(orientation);
        newView.get_Parameter(ARDB.BuiltInParameter.VIEWER_CROP_REGION).Update(0);
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
            view3D.get_Parameter(ARDB.BuiltInParameter.VIEWER_PERSPECTIVE).Update(perspective.Value ? 1 : 0);
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
