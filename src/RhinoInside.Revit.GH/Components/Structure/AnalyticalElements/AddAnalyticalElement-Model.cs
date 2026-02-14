using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.GH.Exceptions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Structure
{
#if REVIT_2023
  using ARDB_AnalyticalMember = ARDB.Structure.AnalyticalMember;
  using ARDB_AnalyticalPanel = ARDB.Structure.AnalyticalPanel;
  using ARDB_AnalyticalOpening = ARDB.Structure.AnalyticalOpening;
#else
  using ARDB_AnalyticalMember = ARDB.Structure.AnalyticalModelStick;
  using ARDB_AnalyticalPanel = ARDB.Structure.AnalyticalModelSurface;
  using ARDB_AnalyticalOpening = ARDB.Structure.AnalyticalModelSurface;
#endif

  [ComponentVersion(introduced: "1.27"), ComponentRevitAPIVersion(min: "2023.0")]
  public class AddAnalyticalElementByModel : AddAnalyticalElement
  {
    public override Guid ComponentGuid => new Guid("AC26C810-2043-4666-B16E-8484D9DCF7DE");
#if REVIT_2023
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
#else
    public override GH_Exposure Exposure => GH_Exposure.hidden;
#endif
    public AddAnalyticalElementByModel() : base
    (
      name: "Add Analytical Element",
      nickname: "ME-Analytical",
      description: "Given a model element, it adds an analytical element representation to the active Revit document",
      category: "Revit",
      subCategory: "Structure"
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
        new Parameters.GraphicalElement()
        {
          Name = "Model Element",
          NickName = "ME",
          Description = "Model element",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.AnalyticalElement()
          {
            Name = _AnalyticalElement_,
            NickName = "AE",
            Description = $"Output {_AnalyticalElement_}",
            Access = GH_ParamAccess.list
          }
      )
    };

    const string _AnalyticalElement_ = "Analytical Element";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
#if REVIT_2023
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;
      if (!Params.GetData(DA, "Model Element", out Types.GraphicalElement element)) return;

      // Checking input
      var tol = GeometryTolerance.Model;
      Brep boundary = null;
      var thickness = 0.0;
      ARDB.ElementId typeId = default;
      ARDB.ElementId materialId = default;
      var structuralRole = ARDB.Structure.AnalyticalStructuralRole.Unset;
      switch (element)
      {
        case Types.StructuralBeam beam:
          structuralRole = ARDB.Structure.AnalyticalStructuralRole.StructuralRoleBeam;
          typeId = beam.Value.GetTypeId();
          materialId = beam.Value.StructuralMaterialId;
          break;

        case Types.StructuralBrace brace:
          structuralRole = ARDB.Structure.AnalyticalStructuralRole.StructuralRoleGirder;
          typeId = brace.Value.GetTypeId();
          materialId = brace.Value.StructuralMaterialId;
          break;

        case Types.StructuralColumn column:
          structuralRole = ARDB.Structure.AnalyticalStructuralRole.StructuralRoleColumn;
          typeId = column.Value.GetTypeId();
          materialId = column.Value.StructuralMaterialId;
          break;

        case Types.StructuralFraming framing:
          structuralRole = ARDB.Structure.AnalyticalStructuralRole.StructuralRoleMember;
          typeId = framing.Value.GetTypeId();
          materialId = framing.Value.StructuralMaterialId;
          break;

        case Types.Wall wall:
          boundary = wall.TrimmedSurface;
          structuralRole = ARDB.Structure.AnalyticalStructuralRole.StructuralRoleWall;
          thickness = wall.Value.Width;
          break;

        case Types.Floor floor:
          boundary = floor.Sketch.TrimmedSurface;
          structuralRole = ARDB.Structure.AnalyticalStructuralRole.StructuralRoleFloor;
          thickness = floor.Value.get_Parameter(ARDB.BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM).AsDouble();
          break;
      }

      // Compute
      switch (structuralRole)
      {
        case ARDB.Structure.AnalyticalStructuralRole.Unset:
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Model element is not supported for creating an analytical element. {{{element.Id}}}");
          return;

        case ARDB.Structure.AnalyticalStructuralRole.StructuralRoleBeam:
          var beam = ReconstructElement<ARDB_AnalyticalMember>
          (
            doc.Value, _AnalyticalElement_, analyticalMember =>
            {
              analyticalMember = Reconstruct(analyticalMember, doc.Value, element.Curve.ToCurve());
              analyticalMember.StructuralRole = structuralRole;
              analyticalMember.SectionTypeId = typeId;
              analyticalMember.MaterialId = materialId;
              return analyticalMember;
            }
          );
          DA.SetDataList(_AnalyticalElement_, new List<ARDB_AnalyticalMember> { beam });
          break;
        case ARDB.Structure.AnalyticalStructuralRole.StructuralRoleColumn:
          var column = ReconstructElement<ARDB_AnalyticalMember>
          (
            doc.Value, _AnalyticalElement_, analyticalMember =>
            {
              analyticalMember = Reconstruct(analyticalMember, doc.Value, element.Curve.ToCurve());
              analyticalMember.StructuralRole = structuralRole;
              analyticalMember.SectionTypeId = typeId;
              analyticalMember.MaterialId = materialId;
              DA.SetData(_AnalyticalElement_, analyticalMember);
              return analyticalMember;
            }
          );
          DA.SetDataList(_AnalyticalElement_, new List<ARDB_AnalyticalMember> { column });
          break;
        case ARDB.Structure.AnalyticalStructuralRole.StructuralRoleGirder:
          var girder = ReconstructElement<ARDB_AnalyticalMember>
          (
            doc.Value, _AnalyticalElement_, analyticalMember =>
            {
              analyticalMember = Reconstruct(analyticalMember, doc.Value, element.Curve.ToCurve());
              analyticalMember.StructuralRole = structuralRole;
              analyticalMember.SectionTypeId = typeId;
              analyticalMember.MaterialId = materialId;
              return analyticalMember;
            }
          );
          DA.SetDataList(_AnalyticalElement_, new List<ARDB_AnalyticalMember> { girder });
          break;

        case ARDB.Structure.AnalyticalStructuralRole.StructuralRoleMember:
          var member = ReconstructElement<ARDB_AnalyticalMember>
          (
            doc.Value, _AnalyticalElement_, analyticalMember =>
            {
              analyticalMember = Reconstruct(analyticalMember, doc.Value, element.Curve.ToCurve());
              analyticalMember.StructuralRole = structuralRole;
              analyticalMember.SectionTypeId = typeId;
              analyticalMember.MaterialId = materialId;
              return analyticalMember;
            }
          );
          DA.SetDataList(_AnalyticalElement_, new List<ARDB_AnalyticalMember> { member });
          break;

        case ARDB.Structure.AnalyticalStructuralRole.StructuralRoleFloor:

          var analyticalPanelsFloors = new List<ARDB_AnalyticalPanel>();
          foreach (var face in boundary.Faces)
          {
            var panel = ReconstructElement<ARDB_AnalyticalPanel>
            (
              doc.Value, _AnalyticalElement_, analyticalPanel =>
              {
                if (!face.TryGetPlane(out var _, tol.VertexTolerance))
                  throw new RuntimeArgumentException("Boundary", "Boundary surface should be planar.", boundary);

                var loops = face.Loops.Where(x => x.LoopType == BrepLoopType.Outer).Select(x => x.To3dCurve()).ToArray();

                var boundaryPlane = default(Rhino.Geometry.Plane);
                var maxArea = 0.0;
                for (int index = 0; index < loops.Length; ++index)
                {
                  var loop = loops[index];
                  var plane = default(Rhino.Geometry.Plane);
                  if (loop is null || loop.IsShort(tol.ShortCurveTolerance))
                    throw new RuntimeArgumentException("Boundary", $"Loop {index} is too short.\nTolerance is {tol.ShortCurveTolerance}", loop);

                  if (!loop.IsClosed(tol.VertexTolerance) || !loop.TryGetPlane(out plane, tol.VertexTolerance))
                    throw new RuntimeArgumentException("Boundary", $"Loop {index} should be closed and planar.\nTolerance is {tol.VertexTolerance}", loop);

                  loops[index] = loop.Simplify(CurveSimplifyOptions.All & ~CurveSimplifyOptions.Merge, tol.VertexTolerance, tol.AngleTolerance) ?? loop;

                  using (var properties = AreaMassProperties.Compute(loop, tol.VertexTolerance))
                  {
                    if (properties is null)
                      throw new RuntimeArgumentException("Boundary", "Failed to compute loop Area.", loop);

                    if (properties.Area > maxArea)
                    {
                      maxArea = properties.Area;
                      var orientation = loop.ClosedCurveOrientation(plane);

                      if (orientation == CurveOrientation.CounterClockwise)
                        plane.Flip();

                      boundaryPlane = plane;
                    }
                    else if (plane.Normal.IsParallelTo(boundaryPlane.Normal) == 0 || Math.Abs(plane.DistanceTo(boundaryPlane.Origin)) > GeometryTolerance.Internal.DefaultTolerance)
                    {
                      throw new RuntimeArgumentException("Boundary", "Loops should be a list of coplanar curves.", loops);
                    }
                  }

                }

                analyticalPanel = Reconstruct
                (
                  analyticalPanel,
                  doc.Value,
                  loops
                );

                analyticalPanel.StructuralRole = structuralRole;
                analyticalPanel.Thickness = thickness;
                return analyticalPanel;
              }
             );

            analyticalPanelsFloors.Add(panel);
          }
          DA.SetDataList(_AnalyticalElement_, analyticalPanelsFloors);
          break;

        case ARDB.Structure.AnalyticalStructuralRole.StructuralRoleWall:
          var analyticalPanelsWalls = new List<ARDB_AnalyticalPanel>();
          foreach (var face in boundary.Faces)
          {
            var panel = ReconstructElement<ARDB_AnalyticalPanel>
            (
              doc.Value, _AnalyticalElement_, analyticalPanel =>
              {
                if (!boundary.Faces[0].TryGetPlane(out var _, tol.VertexTolerance))
                  throw new RuntimeArgumentException("Boundary", "Boundary surface should be planar.", boundary);

                var loops = boundary.Loops.Where(x => x.LoopType == BrepLoopType.Outer).Select(x => x.To3dCurve()).ToArray();

                var boundaryPlane = default(Rhino.Geometry.Plane);
                var maxArea = 0.0;
                for (int index = 0; index < loops.Length; ++index)
                {
                  var loop = loops[index];
                  var plane = default(Rhino.Geometry.Plane);
                  if (loop is null || loop.IsShort(tol.ShortCurveTolerance))
                    throw new RuntimeArgumentException("Boundary", $"Loop {index} is too short.\nTolerance is {tol.ShortCurveTolerance}", loop);

                  if (!loop.IsClosed(tol.VertexTolerance) || !loop.TryGetPlane(out plane, tol.VertexTolerance))
                    throw new RuntimeArgumentException("Boundary", $"Loop {index} should be closed and planar.\nTolerance is {tol.VertexTolerance}", loop);

                  loops[index] = loop.Simplify(CurveSimplifyOptions.All & ~CurveSimplifyOptions.Merge, tol.VertexTolerance, tol.AngleTolerance) ?? loop;

                  using (var properties = AreaMassProperties.Compute(loop, tol.VertexTolerance))
                  {
                    if (properties is null)
                      throw new RuntimeArgumentException("Boundary", "Failed to compute loop Area.", loop);

                    if (properties.Area > maxArea)
                    {
                      maxArea = properties.Area;
                      var orientation = loop.ClosedCurveOrientation(plane);

                      if (orientation == CurveOrientation.CounterClockwise)
                        plane.Flip();

                      boundaryPlane = plane;
                    }
                    else if (plane.Normal.IsParallelTo(boundaryPlane.Normal) == 0 || Math.Abs(plane.DistanceTo(boundaryPlane.Origin)) > GeometryTolerance.Internal.DefaultTolerance)
                    {
                      throw new RuntimeArgumentException("Boundary", "Loops should be a list of coplanar curves.", loops);
                    }
                  }
                }

                analyticalPanel = Reconstruct
                (
                  analyticalPanel,
                  doc.Value,
                  loops
                );

                analyticalPanel.StructuralRole = structuralRole;
                analyticalPanel.Thickness = thickness;
                DA.SetData(_AnalyticalElement_, analyticalPanel);
                return analyticalPanel;
              }
            );

            analyticalPanelsWalls.Add(panel);
          }
          
          DA.SetDataList(_AnalyticalElement_, analyticalPanelsWalls);
          break;
      }
#endif
    }
  }
}
