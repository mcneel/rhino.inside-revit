using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;
using OS = System.Environment;

namespace RhinoInside.Revit.GH.Components.Site
{
  using Convert.Geometry;
  using Convert.System.Collections.Generic;
  using External.DB.Extensions;
  using GH.Exceptions;
  using Grasshopper.Kernel.Types;
  using Grasshopper.Rhinoceros.Params;

#if REVIT_2024
  [ComponentVersion(introduced: "1.37"), ComponentRevitAPIVersion(min: "2024.0")]
  public class AddToposolidShape : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("D9B0616F-6433-4628-B304-14BC6643E5C4");
    public override GH_Exposure Exposure => SDKCompliancy(GH_Exposure.tertiary);

    public AddToposolidShape() : base
    (
      name: "Add Toposolid (Shape)",
      nickname: "M-Toposolid",
      description: "Given a mesh, it adds a Toposolid element to the active Revit document",
      category: "Revit",
      subCategory: "Site"
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
        new Param_Geometry
        {
          Name = "Shape",
          NickName = "S",
          Description = "Toposolid surface shape"
        }
      ),
      new ParamDefinition
      (
#if RHINO_8
        new Param_ModelMeshingParameters
#else
        new Param_MeshParameters
#endif
        {
          Name = "Settings",
          NickName = "Ms",
          Description = "Meshing settings",
          Optional = true
        }
      ),
      new ParamDefinition
       (
        new Parameters.HostObjectType
        {
          Name = "Type",
          NickName = "T",
          Description = "Toposolid type",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_Toposolid
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
       (
        new Parameters.LevelConstraint
        {
          Name = "Base",
          NickName = "BA",
          Description = $"Base of the {_Toposolid_.ToLowerInvariant()}.{OS.NewLine}This input accepts a 'Level Constraint', an 'Elevation' or a 'Number' as an offset from the 'Boundary'.",
          Optional = true
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Toposolid()
        {
          Name = _Toposolid_,
          NickName = _Toposolid_.Substring(0, 1),
          Description = $"Output {_Toposolid_}",
        }
      )
    };

    const string _Toposolid_ = "Toposolid";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.LEVEL_PARAM,
      ARDB.BuiltInParameter.TOPOSOLID_HEIGHTABOVELEVEL_PARAM,
    };

    static bool TryGetMeshBoundaries(Mesh mesh, Brep shell, out IList<Curve> boundaries2d, out IList<Curve> boundaries3d, int orientation)
    {
      if (shell is object)
      {
        var silhouettes = Silhouette.Compute(shell, SilhouetteType.Boundary, Vector3d.ZAxis, DocumentTolerance(), DocumentAngleTolerance());
        boundaries3d = Curve.JoinCurves(silhouettes.Select(x => x.Curve));
        boundaries2d = default;

        var bbox = BoundingBox.Empty;
        foreach (var edge in boundaries3d)
          bbox.Union(edge.GetBoundingBox(accurate:true));

        var plane = new Plane(new Point3d(0.0, 0.0, bbox.Min.Z), orientation < 0 ? -Vector3d.ZAxis : Vector3d.ZAxis);
        boundaries2d = boundaries3d.Select(x => x.ProjectToPlane(plane)).ToArray();
        return true;
      }

      var count = mesh.TopologyEdges.Count;
      var edges = new List<Line>(count);
      if (count > 0)
      {
        var tol = GeometryTolerance.Model;

        // Find relevant edges
        for (int e = 0; e < count; ++e)
        {
          var faces = mesh.TopologyEdges.GetConnectedFaces(e);
          switch (faces.Length)
          {
            case 1:
            {
              var face0 = mesh.FaceNormals[faces[0]];
              if (face0.Z <= 0.0) continue;
            }
            break;
            case 2:
            {
              var face0 = mesh.FaceNormals[faces[0]];
              var face1 = mesh.FaceNormals[faces[1]];
              if (face0.Z <= 0.0 == face1.Z <= 0.0) continue;
            }
            break;
            default: continue;
          }

          edges.Add(mesh.TopologyEdges.EdgeLine(e));
        }

        var bbox = BoundingBox.Empty;
        foreach (var edge in edges)
          bbox.Union(edge.BoundingBox);

        // Compute outlines
        var plane = new Plane(new Point3d(0.0, 0.0, bbox.Min.Z), orientation < 0 ? -Vector3d.ZAxis : Vector3d.ZAxis);
        var polylines2d = mesh.GetOutlines(plane);
        {
          // Clean overlapped edges.
          var breps = Brep.CreatePlanarBreps(new Rhino.Collections.CurveList(polylines2d.Select(x => new PolylineCurve(x))), DocumentTolerance());
          if (breps?.Length > 0)
          {
            polylines2d = breps.SelectMany(x => x.Loops.Select(x => x.To3dCurve())).
              Select(x => x.ToPolyline(0.0, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity).ToPolyline()).
              ToArray();
          }
        }

        // Pull back outlines back to edges
        var polylines3d = new List<Polyline>();
        foreach (var polyline2d in polylines2d)
        {
          // Clean out outline
          polyline2d.MergeColinearSegments(tol.AngleTolerance, includeSeam: true);
          polyline2d.CollapseShortSegments(tol.VertexTolerance);

          polylines3d.Add
          (
            new Polyline
            (
              polyline2d.Select
              (
                pt => Intersection.MeshLine(mesh, new Line(new Point3d(pt.X, pt.Y, bbox.Min.Z), new Point3d(pt.X, pt.Y, bbox.Max.Z))).
                //pt => edges.Select
                //(
                //  c => Intersection.LineLine(c, new Line(new Point3d(pt.X, pt.Y, bbox.Min.Z), new Point3d(pt.X, pt.Y, bbox.Max.Z)), out var a, out var b, 0.0, true) & false ?
                //        c.PointAt(a) : pt
                //).
                OrderByDescending(x => orientation < 0 ? -x.Z : x.Z).
                FirstOr(pt)
              )
            )
          );
        }

        boundaries3d = polylines3d.Select(x => new PolylineCurve(x)).ToArray();
        boundaries2d = polylines2d.Select(x => new PolylineCurve(x)).ToArray();
        return true;
      }

      boundaries2d = default;
      boundaries3d = default;
      return false;
    }

    static bool TryGetMeshPoints(Mesh mesh, out IList<Point3d> points, int orientation)
    {
      if (mesh.Faces.Count > 0)
      {
        var bbox = mesh.GetBoundingBox(accurate: true);

        var vertices = mesh.TopologyVertices;
        var count = vertices.Count;
        var vi = new HashSet<Point3d>(count);
        for (int v = 0; v < count; ++v)
        {
          var faces = vertices.ConnectedFaces(v);
          if (faces.Length < 2) continue;

          if (faces.Any(f => mesh.FaceNormals[f].Z > 0.1))
          {
            var vertex = vertices.MeshVertexIndices(v)[0];
            var pt = mesh.Vertices.Point3dAt(vertex);

            if (IsPointOnSurface(mesh, pt, bbox, orientation))
              vi.Add(pt);
          }
        }

        points = vi.ToArray();
        return true;
      }

      points = default;
      return false;
    }

    static bool TryGetMeshCreases(Mesh mesh, out IList<Line> creases, int orientation)
    {
      var count = mesh.TopologyEdges.Count;
      if (count > 0)
      {
        var bbox = mesh.GetBoundingBox(accurate: true);

        creases = new List<Line>(count);
        for (int e = 0; e < count; ++e)
        {
          var faces = mesh.TopologyEdges.GetConnectedFaces(e);
          switch (faces.Length)
          {
            case 1:
            {
              var face0 = mesh.FaceNormals[faces[0]];
              if (face0.Z <= 0.0) continue;
            }
            break;
            case 2:
            {
              var face0 = mesh.FaceNormals[faces[0]];
              var face1 = mesh.FaceNormals[faces[1]];
              if (face0.Z <= 0.0 && face1.Z <= 0.0) continue;
              var angle = Vector3d.VectorAngle(face0, face1);
              if (angle < RhinoMath.ToRadians(1.0)) continue;
            }
            break;
            default: continue;
          }

          var edge = mesh.TopologyEdges.EdgeLine(e);
          if (IsPointOnSurface(mesh, edge.From, bbox, orientation) && IsPointOnSurface(mesh, edge.To, bbox, orientation))
            creases.Add(edge);
        }

        return true;
      }

      creases = default;
      return false;
    }

    static bool IsPointOnSurface(Mesh mesh, Point3d pt, BoundingBox bbox, int orientation)
    {
      var ray = orientation >= 0 ?
        new Line(new Point3d(pt.X, pt.Y, pt.Z), new Point3d(pt.X, pt.Y, bbox.Max.Z)) :
        new Line(new Point3d(pt.X, pt.Y, bbox.Min.Z), new Point3d(pt.X, pt.Y, pt.Z));

      return Intersection.MeshLine(mesh, ray).Length <= 1;
    }

    private static Mesh MergeFragments(params Mesh[] fragments)
    {
      switch (fragments.Length)
      {
        case 0: break;
        case 1: return fragments[0];
        default:
          var result = new Mesh();
          result.Append(fragments);
          return result;
      }

      return null;
    }


    private static Mesh ToMesh(IGH_GeometricGoo geometry, MeshingParameters meshing)
    {
      switch (geometry)
      {
        case GH_Mesh mesh: return mesh.Value;
        case GH_Surface surface: return MergeFragments(Mesh.CreateFromBrep(surface.Value, meshing));
        case GH_Brep brep: return MergeFragments(Mesh.CreateFromBrep(brep.Value, meshing));
        case GH_Extrusion extrusion: return Mesh.CreateFromExtrusion(extrusion.Value, meshing);
        case GH_SubD subD: return Mesh.CreateFromSubD(subD.Value, (int) SubDDisplayParameters.Density.MediumDensity);
      }

      return null;
    }

    private static Brep ToBrep(IGH_GeometricGoo geometry)
    {
      switch (geometry)
      {
        case GH_Brep brep: return brep.Value;
        case GH_Surface surface: return surface.Value;
        case GH_Extrusion extrusion: return extrusion.Value.ToBrep(false);
        case GH_SubD subD: return subD.Value.ToBrep(new SubDToBrepOptions(true, SubDToBrepOptions.ExtraordinaryVertexProcessOption.LocalG1xx));
      }

      return null;
    }

    private static Mesh GetShapeMesh(IGH_GeometricGoo geometry, IGH_Goo parameters, out Brep shell)
    {
      if (parameters.CastTo(out QuadRemeshParameters quads))
      {
        shell = ToBrep(geometry);
        if (shell is object)
          return Mesh.QuadRemeshBrep(shell, quads);

        if (geometry.CastTo(out Mesh mesh))
          return mesh.QuadRemesh(quads);
      }
      else
      {
        if (!parameters.CastTo(out MeshingParameters meshing))
        {
          if (parameters.CastTo(out GH_MeshingParameters mp))
            meshing = mp.Value;
        }

        if (ToMesh(geometry, meshing ?? MeshingParameters.Default) is Mesh mesh)
        {
          shell = ToBrep(geometry);
          return mesh;
        }
      }

      shell = null;
      return null;
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.Toposolid>
      (
        doc.Value, _Toposolid_, toposolid =>
        {
          // Input
          if (!Params.GetData(DA, "Shape", out IGH_GeometricGoo shape)) return null;
          if (!Params.GetData(DA, "Settings", out IGH_Goo parameters)) return null;

          var mesh = GetShapeMesh(shape, parameters, out var shell);
          if (mesh is null)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to mesh input Shape");
            return null;
          }

          mesh.Faces.ConvertQuadsToTriangles();
          mesh.FaceNormals.ComputeFaceNormals();

          var orientation = mesh.SolidOrientation();
          if (!TryGetMeshBoundaries(mesh, shell, out var boundary2d, out var boundary3d, orientation)) return null;
          if (!TryGetMeshPoints(mesh, out var points, orientation)) return null;
          if (!TryGetMeshCreases(mesh, out var creases, orientation)) return null;

          var boundaryElevation = Interval.Unset;
          {
            var tol = GeometryTolerance.Model;
            var normal = default(Vector3d);
            var maxArea = 0.0; var maxIndex = 0;
            for (int index = 0; index < boundary2d.Count; ++index)
            {
              var loop = boundary2d[index];
              if (loop is null) return null;
              if
              (
                loop.IsShort(tol.ShortCurveTolerance) ||
                !loop.IsClosed ||
                !loop.TryGetPlane(out var plane, tol.VertexTolerance) ||
                plane.ZAxis.IsParallelTo(Vector3d.ZAxis, tol.AngleTolerance) == 0
              )
                throw new RuntimeArgumentException(nameof(mesh), "Boundary loop curves should be a set of valid horizontal, coplanar and closed curves.", boundary3d[index]);

              boundaryElevation = Interval.FromUnion(boundaryElevation, new Interval(plane.OriginZ, plane.OriginZ));
              boundary2d[index] = loop.Simplify(CurveSimplifyOptions.All & ~CurveSimplifyOptions.Merge, tol.VertexTolerance, tol.AngleTolerance) ?? loop;

              using (var properties = AreaMassProperties.Compute(loop, tol.VertexTolerance))
              {
                if (properties is null) return null;
                if (properties.Area > maxArea)
                {
                  normal = plane.Normal;
                  maxArea = properties.Area;
                  maxIndex = index;

                  if (loop.ClosedCurveOrientation(Plane.WorldXY) == CurveOrientation.CounterClockwise)
                    normal.Reverse();
                }
              }
            }

            if (!boundaryElevation.IsValid) return null;
          }

          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out Types.HostObjectType type, doc, ARDB.BuiltInCategory.OST_Toposolid)) return null;

          if (!(type.Value is ARDB.ToposolidType toposolidType))
            throw new RuntimeArgumentException(nameof(type), $"Type '{type.Nomen}' is not a valid toposolid type.");

          if (!Params.TryGetData(DA, "Base", out ERDB.ElevationElementReference? baseElevation)) return null;

          // Solve optional Base
          ERDB.ElevationElementReference.SolveBase
          (
            doc.Value, GeometryEncoder.ToInternalLength(boundaryElevation.Min),
            0.0, ref baseElevation
          );

          // Compute
          var bbox = mesh.GetBoundingBox(accurate: true);
          var box = new Box(bbox);
          box.RepositionBasePlane(new Point3d(box.Center.X, box.Center.Y, bbox.Min.Z));

          toposolid = Reconstruct
          (
            toposolid,
            doc.Value,
            box,
            boundary2d,
            boundary3d,
            points,
            creases,
            toposolidType,
            baseElevation.Value
          );
          DA.SetData(_Toposolid_, toposolid);
          return toposolid;
        }
      );
    }

    bool Reuse
    (
      ref ARDB.Toposolid toposolid,
      Plane plane,
      IList<Curve> boundaries,
      ARDB.ToposolidType type,
      ARDB.Level baseLevel
    )
    {
      if (toposolid is null) return false;

      if (!(toposolid.GetSketch() is ARDB.Sketch sketch && Types.Sketch.SetProfile(sketch, boundaries, Vector3d.ZAxis)))
        return false;

      if (toposolid.GetTypeId() != type.Id)
      {
        if (ARDB.Element.IsValidType(toposolid.Document, new ARDB.ElementId[] { toposolid.Id }, type.Id))
        {
          if (toposolid.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            toposolid = toposolid.Document.GetElement(id) as ARDB.Toposolid;
        }
        else return false;
      }

      toposolid.get_Parameter(ARDB.BuiltInParameter.LEVEL_PARAM).Update(baseLevel.Id);

      return true;
    }

    ARDB.Toposolid Create
    (
      ARDB.Document document,
      Plane plane,
      IList<Curve> boundary,
      ARDB.ToposolidType type,
      ARDB.Level baseLevel
    )
    {
      // We create a Level here to obtain a toposolid with a <not associated> `SketchPlane`
      var level = ARDB.Level.Create(document, plane.OriginZ);

      var curveLoops = boundary.ConvertAll(GeometryEncoder.ToCurveLoop);
      var toposolid = ARDB.Toposolid.Create(document, curveLoops, type.Id, level.Id);

      toposolid.get_Parameter(ARDB.BuiltInParameter.LEVEL_PARAM).Update(baseLevel.Id);
      document.Delete(level.Id);

      return toposolid;
    }

    ARDB.Toposolid Reconstruct
    (
      ARDB.Toposolid toposolid,
      ARDB.Document doc,
      Box box,
      IList<Curve> boundary2d,
      IList<Curve> boundary3d,
      IList<Point3d> points,
      IList<Line> creases,
      ARDB.ToposolidType type,
      ERDB.ElevationElementReference baseElevation
    )
    {
      if (!baseElevation.IsLevelConstraint(out var baseLevel, out var baseOffset)) return default;

      if (!Reuse(ref toposolid, box.Plane, boundary2d, type, baseLevel))
      {
        toposolid = toposolid.ReplaceElement
        (
          Create(doc, box.Plane, boundary2d, type, baseLevel),
          ExcludeUniqueProperties
        );
      }

      if (toposolid is object)
      {
        toposolid.get_Parameter(ARDB.BuiltInParameter.TOPOSOLID_HEIGHTABOVELEVEL_PARAM).Update(baseOffset ?? 0.0);

        var host = new Types.Toposolid(toposolid);
        host.SetSlabShape(boundary2d, boundary3d, points ?? Array.Empty<Point3d>(), creases ?? Array.Empty<Line>(), out var skipedPoints, out var skipedCreases, this);

        foreach (var point in skipedPoints)
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Point projection is outside boundary.", new Point(point));

        foreach (var crease in skipedCreases)
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Some invalid creases were skipped.", new LineCurve(crease));
      }

      return toposolid;
    }
  }
#endif
}
