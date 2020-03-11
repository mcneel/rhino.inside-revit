using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using DB = Autodesk.Revit.DB;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Elements.Topography
{
  public class TopographyByPoints : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("E8D8D05A-8703-4F75-B106-12B40EC9DF7B");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public TopographyByPoints() : base
    (
      "AddTopography.ByPoints", "ByPoints",
      "Given a set of Points, it adds a Topography surface to the active Revit document",
      "Revit", "Site"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GeometricElement(), "Topography", "T", "New Topography", GH_ParamAccess.item);
    }

    void ReconstructTopographyByPoints
    (
      DB.Document doc,
      ref Autodesk.Revit.DB.Element element,

      IList<Rhino.Geometry.Point3d> points,
      [Optional] IList<Rhino.Geometry.Curve> regions
    )
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;
      var xyz = points.Select(x => x.ChangeUnits(scaleFactor).ToHost()).ToArray();

      //if (element is TopographySurface topography)
      //{
      //  using (var editScope = new TopographyEditScope(doc, "TopographyByPoints"))
      //  {
      //    editScope.Start(element.Id);
      //    topography.DeletePoints(topography.GetPoints());
      //    topography.AddPoints(points.ToHost().ToList());

      //    foreach (var subRegionId in topography.GetHostedSubRegionIds())
      //      doc.Delete(subRegionId);

      //    editScope.Commit(new Revit.FailuresPreprocessor());
      //  }
      //}
      //else
      {
        ReplaceElement(ref element, DB.Architecture.TopographySurface.Create(doc, xyz));
      }

      if (element is object && regions?.Count > 0)
      {
        var curveLoops = regions.Select(region => DB.CurveLoop.Create(region.ChangeUnits(scaleFactor).ToHostMultiple().ToArray())).ToArray();
        DB.Architecture.SiteSubRegion.Create(doc, curveLoops, element.Id);
      }
    }
  }
}
