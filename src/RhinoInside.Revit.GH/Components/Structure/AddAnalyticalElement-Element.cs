using System;
using System.Linq;
using Autodesk.Revit.DB.Structure;
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
  public class AddAnalyticalElementByElement : BaseAnalyticalComponent
  {
    public override Guid ComponentGuid => new Guid("AC26C810-2043-4666-B16E-8484D9DCF7DE");
#if REVIT_2023
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
#else
    public override GH_Exposure Exposure => GH_Exposure.hidden;
#endif
    public AddAnalyticalElementByElement() : base
    (
      name: "Add Analytical Element (Element)",
      nickname: "AE-Element",
      description: "Given an element, it extract its analytical element to the active Revit document",
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
          Name = "Element",
          NickName = "E",
          Description = "Graphical element",
          Access = GH_ParamAccess.item
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.AnalyticalMember()
        {
          Name = _AnalyticalMemberBeam_,
          NickName = "AM-Beam",
          Description = $"Output {_AnalyticalMemberBeam_}",
        }
      ),
      new ParamDefinition
      (
        new Parameters.AnalyticalMember()
        {
          Name = _AnalyticalMemberColumn_,
          NickName = "AM-Column",
          Description = $"Output {_AnalyticalMemberColumn_}",
        }
      ),
      new ParamDefinition
      (
        new Parameters.AnalyticalMember()
        {
          Name = _AnalyticalMemberBrace_,
          NickName = "AM-Brace",
          Description = $"Output {_AnalyticalMemberBrace_}",
        }
      ),
      new ParamDefinition
      (
        new Parameters.AnalyticalPanel()
        {
          Name = _AnalyticalPanelFloor_,
          NickName = "AP-Floor",
          Description = $"Output {_AnalyticalPanelFloor_}",
        }
      ),
      new ParamDefinition
      (
        new Parameters.AnalyticalPanel()
        {
          Name = _AnalyticalPanelWall_,
          NickName = "AP-Wall",
          Description = $"Output {_AnalyticalPanelWall_}",
        }
      )
    };

    const string _AnalyticalMemberBeam_ = "Analytical Member (Beam)";
    const string _AnalyticalMemberColumn_ = "Analytical Member (Column)";
    const string _AnalyticalMemberBrace_ = "Analytical Member (Brace)";
    const string _AnalyticalPanelFloor_ = "Analytical Panel (Floor)";
    const string _AnalyticalPanelWall_ = "Analytical Panel (Wall)";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
#if REVIT_2023
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;
      if (!Params.GetData(DA, "Element", out Types.GraphicalElement element)) return;

      // Checking input
      var tol = GeometryTolerance.Model;
      Brep boundary = null;
      AnalyticalStructuralRole structuralRole = AnalyticalStructuralRole.Unset;
      switch (element)
      {
        case Types.StructuralMember member:

          switch (member.Value.StructuralType)
          {
            case ARDB.Structure.StructuralType.Beam:
              structuralRole = AnalyticalStructuralRole.StructuralRoleBeam;
              break;
            case ARDB.Structure.StructuralType.Brace:
              structuralRole = AnalyticalStructuralRole.StructuralRoleGirder;
              break;
            case ARDB.Structure.StructuralType.Column:
              structuralRole = AnalyticalStructuralRole.StructuralRoleColumn;
              break;
          }

          break;

        case Types.Wall wall:
          boundary = wall.TrimmedSurface;
          structuralRole = AnalyticalStructuralRole.StructuralRoleWall;
          break;

        case Types.Floor floor:
          boundary = floor.Sketch.TrimmedSurface;
          structuralRole = AnalyticalStructuralRole.StructuralRoleFloor;
          break;
      }

      // Compute
      switch (structuralRole)
      {
        case AnalyticalStructuralRole.Unset:
          this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Element with id {element.Id} is not supported for creating an analytical element");
          return;
        case AnalyticalStructuralRole.StructuralRoleBeam:
          ReconstructElement<ARDB_AnalyticalMember>
          (
            doc.Value, _AnalyticalMemberBeam_, analyticalMember =>
            {
              analyticalMember = Reconstruct
              (
                analyticalMember,
                doc.Value,
                element.Curve.ToCurve()
              );

              analyticalMember.StructuralRole = structuralRole;
              DA.SetData(_AnalyticalMemberBeam_, analyticalMember);
              return analyticalMember;
            }
          );
          break;
        case AnalyticalStructuralRole.StructuralRoleColumn:
          ReconstructElement<ARDB_AnalyticalMember>
          (
            doc.Value, _AnalyticalMemberColumn_, analyticalMember =>
            {
              analyticalMember = Reconstruct
              (
                analyticalMember,
                doc.Value,
                element.Curve.ToCurve()
              );

              analyticalMember.StructuralRole = structuralRole;
              DA.SetData(_AnalyticalMemberColumn_, analyticalMember);
              return analyticalMember;
            }
          );
          break;
        case AnalyticalStructuralRole.StructuralRoleGirder:
          ReconstructElement<ARDB_AnalyticalMember>
          (
            doc.Value, _AnalyticalMemberBrace_, analyticalMember =>
            {
              analyticalMember = Reconstruct
              (
                analyticalMember,
                doc.Value,
                element.Curve.ToCurve()
              );

              analyticalMember.StructuralRole = structuralRole;
              DA.SetData(_AnalyticalMemberBrace_, analyticalMember);
              return analyticalMember;
            }
          );
          break;

        case AnalyticalStructuralRole.StructuralRoleFloor:
          ReconstructElement<ARDB_AnalyticalPanel>
        (
          doc.Value, _AnalyticalPanelFloor_, analyticalPanel =>
          {
            if (boundary.Faces.Count != 1)
              throw new RuntimeArgumentException("Boundary", "Boundary surface should have only one face.", boundary);

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
            DA.SetData(_AnalyticalPanelFloor_, analyticalPanel);
            return analyticalPanel;
          }
        );
          break;
        case AnalyticalStructuralRole.StructuralRoleWall:
          ReconstructElement<ARDB_AnalyticalPanel>
        (
          doc.Value, _AnalyticalPanelWall_, analyticalPanel =>
          {
            if (boundary.Faces.Count != 1)
              throw new RuntimeArgumentException("Boundary", "Boundary surface should have only one face.", boundary);

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
            DA.SetData(_AnalyticalPanelWall_, analyticalPanel);
            return analyticalPanel;
          }
        );
          break;
      }
#endif
    }
  }
}
