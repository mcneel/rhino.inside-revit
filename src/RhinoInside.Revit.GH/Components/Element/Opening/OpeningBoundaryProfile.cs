using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Opening
{
  public class OpeningBoundaryProfile : Component
  {
    public override Guid ComponentGuid => new Guid("E76B0F6B-4EE1-413D-825D-4A8EDD86D55F");
    protected override string IconTag => String.Empty;

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
      manager.AddParameter(
        param: new Parameters.GraphicalElement(),
        name: "Opening",
        nickname: "O",
        description: "Opening object to query for its boundary profile",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddCurveParameter(
        name: "Profile Curve",
        nickname: "PC",
        description: "Profile curve of a given opening element",
        access: GH_ParamAccess.item
        );
      manager.AddPlaneParameter(
        name: "Plane",
        nickname: "P",
        description: "Plane of a given opening element",
        access: GH_ParamAccess.item
        );
    }

    public Plane GetOpeningPlaneFromRectBoundary(ARDB.Opening opening)
    {
      var p0 = opening.BoundaryRect[0].ToPoint3d();
      var p1 = opening.BoundaryRect[1].ToPoint3d();
      var p2 = new Point3d(p0.X, p0.Y, p1.Z);
      var p3 = new Point3d(p1.X, p1.Y, p0.Z);

      var line = new Line(p0, p1);
      var center = line.PointAt(0.5);
      var xAxis = p3 - p0;
      var yAxis = p2 - p0;
      return new Plane(center, xAxis, yAxis);
    }

    public Curve GetRhinoBoundaryFromRectBoundary(ARDB.Opening opening)
    {
      var p0 = opening.BoundaryRect[0].ToPoint3d();
      var p1 = opening.BoundaryRect[1].ToPoint3d();
      var p2 = new Point3d(p0.X, p0.Y, p1.Z);
      var p3 = new Point3d(p1.X, p1.Y, p0.Z);
      var pline = new Polyline(new List<Point3d>() { p0, p2, p1, p3, p0 });
      
      return pline.ToNurbsCurve();
    }

    public Plane GetOpeningPlaneFromCurvBoundary(Curve curve)
    {
      Plane plane = default;
      if (curve.TryGetPlane(out plane))
      {
        var amp = AreaMassProperties.Compute(curve);
        plane.Origin = amp.Centroid;
      }
      return plane;
    }

    public Curve GetRhinoBoundaryFromCurvBoundary(ARDB.Opening opening)
    {
      var curves = GeometryDecoder.ToCurveMany(opening.BoundaryCurves);
      return Curve.JoinCurves(curves)[0];
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      ARDB.Opening opening = null;
      if (!DA.GetData("Opening", ref opening))
        return;

      if (opening.IsRectBoundary)
      {
        DA.SetData("Profile Curve", GetRhinoBoundaryFromRectBoundary(opening));
        DA.SetData("Plane", GetOpeningPlaneFromRectBoundary(opening));
      }

      else
      {
        var curve = GetRhinoBoundaryFromCurvBoundary(opening);
        DA.SetData("Profile Curve", curve);
        DA.SetData("Plane", GetOpeningPlaneFromCurvBoundary(curve));
      }
        
    }
  }
}
