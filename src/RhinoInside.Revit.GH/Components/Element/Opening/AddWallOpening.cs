using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Openings
{
  [ComponentVersion(introduced: "1.6")]
  public class AddWallOpening : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("C86ED84C-2431-4E4F-A890-E5EFFED43BE2");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddWallOpening() : base
    (
      name: "Add Wall Opening",
      nickname: "WallOpen",
      description: "Given a host wall, it adds an opening to the active Revit document",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Document",
          Optional = true
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.Wall()
        {
          Name = "Wall",
          NickName = "W",
          Description = "Wall to add the opening",
        }
      ),
      new ParamDefinition
       (
        new Param_Point
        {
          Name = "Point A",
          NickName = "A",
          Description = "First point to define the wall opening",
        }
      ),
      new ParamDefinition
       (
        new Param_Point
        {
          Name = "Point B",
          NickName = "B",
          Description = "Second point to define the wall opening",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Opening()
        {
          Name = _Opening_,
          NickName = _Opening_.Substring(0, 1),
          Description = $"Output {_Opening_}",
        }
      )
    };

    const string _Opening_ = "Opening";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.Opening>
      (
        doc.Value, _Opening_, opening =>
        {
          // Input
          if (!Params.GetData(DA, "Wall", out ARDB.Wall wall)) return null;
          if (!Params.GetData(DA, "Point A", out Point3d? pointA)) return null;
          if (!Params.GetData(DA, "Point B", out Point3d? pointB)) return null;

          // Compute
          opening = Reconstruct(opening, doc.Value, wall, pointA.Value, pointB.Value);

          DA.SetData(_Opening_, opening);
          return opening;
        }
      );
    }

    bool Reuse(ARDB.Opening opening, ARDB.Wall wall, Point3d pointA, Point3d pointB)
    {
      if (opening is null) return false;

      if (!opening.Host.IsEquivalent(wall)) return false;

      if (opening.IsRectBoundary)
      {
        var minPt = pointA.Z < pointB.Z ? pointA : pointB;
        var maxPt = pointA.Z > pointB.Z ? pointA : pointB;

        var tol = GeometryTolerance.Model;
        if (Math.Abs((minPt.Z / Revit.ModelUnits) - opening.BoundaryRect[0].Z) > tol.VertexTolerance  ||
            Math.Abs((maxPt.Z / Revit.ModelUnits) - opening.BoundaryRect[1].Z) > tol.VertexTolerance)
          return false;
        
        var locationCurve = (wall.Location as ARDB.LocationCurve).Curve.ToCurve();
        var scA = locationCurve.ClosestPoint(minPt, out var tA);
        var scB = locationCurve.ClosestPoint(maxPt, out var tB);
        if (!scA || !scB)
          return false;

        var minT = Math.Min(tA, tB);
        var maxT = Math.Max(tA, tB);
        var ptA = locationCurve.PointAt(minT).ToXYZ();
        var ptB = locationCurve.PointAt(maxT).ToXYZ();

        var projectedOpeningPoints = opening.BoundaryRect.Select(p => new ARDB.XYZ(p.X, p.Y, ptA.Z));

        var isPointCoincident = true;
        foreach(var pt in projectedOpeningPoints)
          isPointCoincident &= pt.IsAlmostEqualTo(ptA) || pt.IsAlmostEqualTo(ptB);

        //var recoveredPtA = new ARDB.XYZ(ptA.X, ptA.Y, minPt.Z / Revit.ModelUnits);
        //var recoveredPtB = new ARDB.XYZ(ptB.X, ptB.Y, maxPt.Z / Revit.ModelUnits);

        //foreach (var pt in opening.BoundaryRect)
        //  isPointCoincident &= pt.IsAlmostEqualTo(recoveredPtA) || pt.IsAlmostEqualTo(recoveredPtB);

        if (isPointCoincident)
          return true;
      }

      return false;
    }

    ARDB.Opening Create(ARDB.Document doc, ARDB.Wall wall, ARDB.XYZ pointA, ARDB.XYZ pointB)
    {
      return doc.Create.NewOpening(wall, pointA, pointB);
    }

    ARDB.Opening Reconstruct(ARDB.Opening opening, ARDB.Document doc, ARDB.Wall wall, Point3d pointA, Point3d pointB)
    {
      if (!Reuse(opening, wall, pointA, pointB))
        opening = Create(doc, wall, pointA.ToXYZ(), pointB.ToXYZ());

      return opening;
    }
  }
}
