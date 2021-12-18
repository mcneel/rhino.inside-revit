using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Site
{
  using Convert.Geometry;
  using Convert.System.Collections.Generic;
  using Kernel.Attributes;

  public class TopographyByPoints : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("E8D8D05A-8703-4F75-B106-12B40EC9DF7B");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public TopographyByPoints() : base
    (
      name: "Add Topography (Points)",
      nickname: "Topography",
      description: "Given a set of Points, it adds a Topography surface to the active Revit document",
      category: "Revit",
      subCategory: "Site"
    )
    { }

    //class FailuresPreprocessor : DB.IFailuresPreprocessor
    //{
    //  public DB.FailureProcessingResult PreprocessFailures(DB.FailuresAccessor failuresAccessor) => DB.FailureProcessingResult.Continue;
    //}

    void ReconstructTopographyByPoints
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [ParamType(typeof(Parameters.GraphicalElement)), Description("New Topography")]
      ref ARDB.Architecture.TopographySurface topography,

      IList<Point3d> points,
      [Optional] IList<Curve> regions
    )
    {
      var xyz = points.ConvertAll(GeometryEncoder.ToXYZ);

      //if (element is DB.Architecture.TopographySurface topography)
      //{
      //  var tol = GeometryObjectTolerance.Model;
      //  using (var scope = new DB.Architecture.TopographyEditScope(topography.Document, NickName))
      //  {
      //    scope.Start(topography.Id);

      //    var boundaryPoints = topography.GetBoundaryPoints();
      //    var bbox = new BoundingBox(boundaryPoints.Convert(GeometryDecoder.ToPoint3d));
      //    bbox.Inflate(tol.VertexTolerance * 10.0);
      //    var bboxCorners = bbox.GetCorners().Take(4).Convert(GeometryEncoder.ToXYZ).ToArray();

      //    using (var tx = new DB.Transaction(topography.Document, NickName))
      //    {
      //      tx.Start();

      //      topography.AddPoints(bboxCorners);
      //      topography.DeletePoints(topography.GetPoints());

      //      tx.Commit();
      //    }

      //    using (var tx = new DB.Transaction(topography.Document, NickName))
      //    {
      //      tx.Start();

      //      topography.AddPoints(xyz);
      //      topography.DeletePoints(bboxCorners);

      //      tx.Commit();
      //    }

      //    scope.Commit(new FailuresPreprocessor());
      //  }
      //}
      //else
      {
        ReplaceElement(ref topography, ARDB.Architecture.TopographySurface.Create(document, xyz));
      }

      if (topography is object && regions?.Count > 0)
      {
        var curveLoops = regions.Select(region => region.ToCurveLoop());
        ARDB.Architecture.SiteSubRegion.Create(document, curveLoops.ToList(), topography.Id);
      }
    }
  }
}
