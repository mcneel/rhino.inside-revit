using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
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
    }

    public Rhino.Geometry.Curve GetRhinoBoundaryFromRectBoundary(IList<ARDB.XYZ> pts)
    {
      ARDB.PolyLine pline = ARDB.PolyLine.Create(pts);
      var polyline = GeometryDecoder.ToPolylineCurve(pline);
      return polyline.ToNurbsCurve();
    }

    public Rhino.Geometry.Curve GetRhinoBoundaryFromWallOpening(ARDB.Opening opening)
    {
      var p0 = opening.BoundaryRect[0].ToPoint3d();
      var p1 = opening.BoundaryRect[1].ToPoint3d();
      var host = opening.Host as ARDB.Wall;
      var plane = new Rhino.Geometry.Plane(p0, host.GetOrientationVector().ToVector3d());
      return new Rhino.Geometry.Rectangle3d(plane, p0, p1).ToNurbsCurve();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      ARDB.Opening opening = null;
      if (!DA.GetData("Opening", ref opening))
        return;

      if ((ARDB.BuiltInCategory)opening.Category.Id.IntegerValue == ARDB.BuiltInCategory.OST_SWallRectOpening)
      {
        if (opening.BoundaryRect != null)
          DA.SetData("Profile Curve", GetRhinoBoundaryFromWallOpening(opening));
      }
      else
      {
        if (opening.IsRectBoundary)
          DA.SetData("Profile Curve", GetRhinoBoundaryFromRectBoundary(opening.BoundaryRect));
        else
          DA.SetData("Profile Curve", GeometryDecoder.ToCurve(opening.BoundaryCurves).ToNurbsCurve());
      } 
    }
  }
}
