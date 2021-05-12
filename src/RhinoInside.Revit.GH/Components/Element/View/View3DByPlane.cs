using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Obsolete
{
  public class View3DByPlane : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("F7B775C9-05E0-40F7-85E9-5CC2EF79731E");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public View3DByPlane() : base
    (
      "Add View3D",
      "View3D",
      "Given a plane, it adds a 3D View to the active Revit document",
      "Revit",
      "View"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.View(), "View3D", "V", "New 3D View", GH_ParamAccess.item);
    }

    void ReconstructView3DByPlane
    (
      DB.Document doc,
      ref DB.View3D view,

      Rhino.Geometry.Plane plane,
      Optional<DB.ElementType> type,
      Optional<string> name,
      Optional<bool> perspective
    )
    {
      SolveOptionalType(doc, ref type, DB.ElementTypeGroup.ViewType3D, nameof(type));

      var orientation = new DB.ViewOrientation3D
      (
        plane.Origin.ToXYZ(),
        plane.YAxis.ToXYZ(),
        -plane.ZAxis.ToXYZ()
      );

      if (view is null)
      {
        var newView = perspective.IsNullOrMissing ?
        DB.View3D.CreatePerspective
        (
          doc,
          type.Value.Id
        ) :
        DB.View3D.CreateIsometric
        (
          doc,
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
        ReplaceElement(ref view, newView, parametersMask);
      }
      else
      {
        view.SetOrientation(orientation);

        if (perspective.HasValue)
          view.get_Parameter(DB.BuiltInParameter.VIEWER_PERSPECTIVE).Set(perspective.Value ? 1 : 0);

        ChangeElementTypeId(ref view, type.Value.Id);
      }

      if (name.HasValue && view is object)
      {
        try { view.Name = name.Value; }
        catch (Autodesk.Revit.Exceptions.ArgumentException e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{e.Message.Replace($".{Environment.NewLine}", ". ")}");
        }
      }
    }
  }
}
