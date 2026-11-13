using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;
#if RHINO_8
using Grasshopper.Rhinoceros.Params;
#endif
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;
using OS = System.Environment;

namespace RhinoInside.Revit.GH.Components.Site
{
  using Convert.Geometry;
  using Convert.System.Collections.Generic;
  using External.DB.Extensions;
  using GH.Exceptions;

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
      new ParamDefinition(Parameters.Document.AddTarget(), ParamRelevance.Occasional),
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
        }, ParamRelevance.Secondary
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

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.Toposolid>
      (
        doc.Value, _Toposolid_, toposolid =>
        {
          // Input
          if (!Params.GetData(DA, "Shape", out IGH_GeometricGoo shape)) return null;
          if (!Params.TryGetData(DA, "Settings", out IGH_Goo parameters)) return null;

          var mesh = Types.SlabEditScope.GetShapeMesh(shape, parameters, out var shell);
          if (mesh is null)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to mesh input Shape");
            return null;
          }

          mesh.Faces.ConvertQuadsToTriangles();
          mesh.FaceNormals.ComputeFaceNormals();

          var orientation = mesh.SolidOrientation();
          if (!Types.SlabEditScope.TryGetBoundaries(mesh, shell, out var boundary2d, out var boundary3d, orientation)) return null;
          if (!Types.SlabEditScope.TryGetMeshPoints(mesh, out var points, orientation)) return null;
          if (!Types.SlabEditScope.TryGetMeshCreases(mesh, out var creases, orientation)) return null;

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
        host.SetSlabShape(boundary2d, boundary3d, points ?? Array.Empty<Point3d>(), creases ?? Array.Empty<Line>(), out var skipedPoints, out var skipedCreases, AbortToken);

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
