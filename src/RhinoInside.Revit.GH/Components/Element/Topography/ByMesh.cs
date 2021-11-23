using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Site
{
#if REVIT_2019
  using Convert.Geometry;
  using Convert.Geometry.Raw;
  using Convert.System.Collections.Generic;
  using Kernel.Attributes;

  public class TopographyByMesh : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("E6EA0A85-E118-4BFD-B01E-86BA22155938");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public TopographyByMesh() : base
    (
      name: "Add Topography (Mesh)",
      nickname: "Topography",
      description: "Given a Mesh, it adds a Topography surface to the active Revit document",
      category: "Revit",
      subCategory: "Site"
    )
    { }

    void ReconstructTopographyByMesh
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [ParamType(typeof(Parameters.GraphicalElement)), Description("New Topography")]
      ref ARDB.Architecture.TopographySurface topography,

      Mesh mesh,
      [Optional] IList<Curve> regions
    )
    {
      mesh = mesh.InHostUnits();
      while (mesh.CollapseFacesByEdgeLength(false, Revit.VertexTolerance) > 0) ;
      mesh.Vertices.CombineIdentical(true, true);
      mesh.Vertices.CullUnused();

      var xyz = mesh.Vertices.ConvertAll(RawEncoder.AsXYZ);
      var facets = new List<ARDB.PolymeshFacet>(mesh.Faces.Count);

      var faceCount = mesh.Faces.Count;
      for (int f = 0; f < faceCount; ++f)
      {
        var face = mesh.Faces[f];

        facets.Add(new ARDB.PolymeshFacet(face.A, face.B, face.C));
        if (face.IsQuad)
          facets.Add(new ARDB.PolymeshFacet(face.C, face.D, face.A));
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
        if (!ARDB.Architecture.TopographySurface.ArePointsDistinct(xyz))
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"At least two vertices have coincident XY values");
        else if (!ARDB.Architecture.TopographySurface.IsValidFaceSet(facets, xyz))
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"At least one face is not valid for {typeof(ARDB.Architecture.TopographySurface).Name}");
        else
          ReplaceElement(ref topography, ARDB.Architecture.TopographySurface.Create(document, xyz, facets));
      }

      if (topography is object && regions?.Count > 0)
      {
        var curveLoops = regions.Select(region => region.ToCurveLoop());
        ARDB.Architecture.SiteSubRegion.Create(document, curveLoops.ToList(), topography.Id);
      }
    }
  }
#endif
}
