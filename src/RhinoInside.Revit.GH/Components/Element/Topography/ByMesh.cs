using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.Geometry.Raw;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
#if REVIT_2019
  public class TopographyByMesh : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("E6EA0A85-E118-4BFD-B01E-86BA22155938");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public TopographyByMesh() : base
    (
      "Add Topography (Mesh)", "Topography",
      "Given a Mesh, it adds a Topography surface to the active Revit document",
      "Revit", "Site"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GraphicalElement(), "Topography", "T", "New Topography", GH_ParamAccess.item);
    }

    void ReconstructTopographyByMesh
    (
      DB.Document doc,
      ref DB.Architecture.TopographySurface element,

      Mesh mesh,
      [Optional] IList<Curve> regions
    )
    {
      mesh = mesh.InHostUnits();
      while (mesh.CollapseFacesByEdgeLength(false, Revit.VertexTolerance) > 0) ;
      mesh.Vertices.CombineIdentical(true, true);
      mesh.Vertices.CullUnused();

      var xyz = mesh.Vertices.ConvertAll(RawEncoder.AsXYZ);
      var facets = new List<DB.PolymeshFacet>(mesh.Faces.Count);

      var faceCount = mesh.Faces.Count;
      for (int f = 0; f < faceCount; ++f)
      {
        var face = mesh.Faces[f];

        facets.Add(new DB.PolymeshFacet(face.A, face.B, face.C));
        if (face.IsQuad)
          facets.Add(new DB.PolymeshFacet(face.C, face.D, face.A));
      }

      //if (element is DB.Architecture.TopographySurface topography)
      //{
      //  using (var editScope = new DB.Architecture.TopographyEditScope(doc, GetType().Name))
      //  {
      //    editScope.Start(element.Id);
      //    topography.DeletePoints(topography.GetPoints());
      //    topography.AddPoints(xyz);

      //    foreach (var subRegionId in topography.GetHostedSubRegionIds())
      //      doc.Delete(subRegionId);

      //    editScope.Commit(this);
      //  }
      //}
      //else
      {
        if (!DB.Architecture.TopographySurface.ArePointsDistinct(xyz))
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"At least two vertices have coincident XY values");
        else if (!DB.Architecture.TopographySurface.IsValidFaceSet(facets, xyz))
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"At least one face is not valid for {typeof(DB.Architecture.TopographySurface).Name}");
        else
          ReplaceElement(ref element, DB.Architecture.TopographySurface.Create(doc, xyz, facets));
      }

      if (element is object && regions?.Count > 0)
      {
        var curveLoops = regions.Select(region => region.ToCurveLoop());
        DB.Architecture.SiteSubRegion.Create(doc, curveLoops.ToList(), element.Id);
      }
    }
  }
#endif
}
