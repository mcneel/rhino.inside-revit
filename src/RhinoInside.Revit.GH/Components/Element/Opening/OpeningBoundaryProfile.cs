using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Opening
{
  [ComponentVersion(introduced: "1.6")]
  public class OpeningBoundaryProfile : Component
  {
    public override Guid ComponentGuid => new Guid("E76B0F6B-4EE1-413D-825D-4A8EDD86D55F");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public OpeningBoundaryProfile() : base
    (
      name: "Opening Boundary Profile",
      nickname: "OpeningBoundProf",
      description: "Get the boundary profile of the given opening",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter
      (
        param: new Parameters.GraphicalElement(),
        name: "Opening",
        nickname: "O",
        description: "Opening object to query for its boundary profile",
        access: GH_ParamAccess.item
      );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddPlaneParameter
      (
        name: "Plane",
        nickname: "P",
        description: "Plane of a given opening element",
        access: GH_ParamAccess.item
      );

      manager.AddCurveParameter
      (
        name: "Profile",
        nickname: "PC",
        description: "Profile curves of a given opening element",
        access: GH_ParamAccess.list
      );
    }

    public bool TryGetRectBoundary(ARDB.Opening opening, out Plane plane, out Curve profile)
    {
      if (opening.IsRectBoundary)
      {
        var p0 = opening.BoundaryRect[0].ToPoint3d();
        var p1 = opening.BoundaryRect[1].ToPoint3d();
        var p2 = new Point3d(p0.X, p0.Y, p1.Z);
        var p3 = new Point3d(p1.X, p1.Y, p0.Z);

        var line = new Line(p0, p1);
        var center = line.PointAt(0.5);
        var xAxis = p3 - p0;
        var yAxis = p2 - p0;

        plane = new Plane(center, xAxis, yAxis);
        profile = new PolylineCurve(new Point3d[] { p0, p2, p1, p3, p0 });
        return true;
      }
      else
      {
        plane = Plane.Unset;
        profile = default;
        return false;
      }
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      ARDB.Opening opening = null;
      if (!DA.GetData("Opening", ref opening))
        return;

      if (TryGetRectBoundary(opening, out var plane, out var profile))
      {
        DA.SetData("Plane", plane);
        DA.SetData("Profile", profile);
      }
      else if (opening.GetSketch() is ARDB.Sketch sketch)
      {
        DA.SetData("Plane", sketch.SketchPlane.GetPlane().ToPlane());
        DA.SetDataList("Profile", sketch.Profile.ToCurveMany());
      }
    }
  }
}
