using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Structure
{
#if REVIT_2023
  using ARDB_AnalyticalPanel = ARDB.Structure.AnalyticalPanel;
#else
  using ARDB_AnalyticalPanel = ARDB.Structure.AnalyticalModelSurface;
#endif

  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.27"), ComponentRevitAPIVersion(min: "2023.0")]
#if DEBUG
  public
#endif
  class AddAnalyticalPanelByExtrusion : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("872CCB2C-E374-4C3F-B7A7-24686AD3911C");
#if REVIT_2023
    public override GH_Exposure Exposure => GH_Exposure.secondary;
#else
    public override GH_Exposure Exposure => GH_Exposure.hidden;
#endif
    public AddAnalyticalPanelByExtrusion() : base
    (
      name: "Add Analytical Panel By Extrusion",
      nickname: "A-ExtrusionPanel",
      description: "Given a curve, it adds an analytical panel perpendicular to the provided work plane to the active Revit document",
      category: "Revit",
      subCategory: "Structure"
    )
    { }
    
    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
#if REVIT_2023
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
        new Param_Curve()
        {
          Name = "Curve",
          NickName = "C",
          Description = "Analytical panel curve",
        }
      ),
      new ParamDefinition
      (
        new Param_Number
        {
          Name = "Height",
          NickName = "H",
          Description = "Analytical panel extrusion height",
          Optional = true,
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.AnalyticalStructuralRole>
        {
          Name = "Structural Role",
          NickName = "R",
          Description = "Analytical element structural role",
          Optional = true,
        }.SetDefaultVale(ARDB.Structure.AnalyticalStructuralRole.StructuralRolePanel)
      )
#endif
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.AnalyticalPanel()
        {
          Name = _AnalyticalPanel_,
          NickName = _AnalyticalPanel_.Substring(0, 1),
          Description = $"Output {_AnalyticalPanel_}",
        }
      )
    };

    const string _AnalyticalPanel_ = "Analytical Panel";

    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
#if REVIT_2023
      ARDB.BuiltInParameter.STRUCTURAL_ANALYZES_AS,
      ARDB.BuiltInParameter.ANALYTICAL_ELEMENT_STRUCTURAL_ROLE,
      ARDB.BuiltInParameter.ANALYTICAL_PANEL_THICKNESS
#endif
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
#if REVIT_2023
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB_AnalyticalPanel>
      (
        doc.Value, _AnalyticalPanel_, analyticalPanel =>
        {

          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve, x => x.IsValid)) return null;
          if (!Params.TryGetData(DA, "Height", out double? height)) return null;
          if (!Params.GetData(DA, "Structural Role", out Types.AnalyticalStructuralRole structuralRole)) return null;

          var tol = GeometryTolerance.Model;

          if (curve.IsShort(tol.ShortCurveTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is too short.\nMin length is {tol.ShortCurveTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

          if (curve is NurbsCurve && curve.IsClosed(tol.ShortCurveTolerance * 1.01) && !curve.IsEllipse(tol.VertexTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is closed or end points are under tolerance.\nTolerance is {tol.ShortCurveTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

          if (!curve.TryGetPlane(out var plane))
            throw new Exceptions.RuntimeArgumentException("Curve", "Curve should be planar", curve);

          // Compute
          analyticalPanel = Reconstruct
          (
            analyticalPanel,
            doc.Value,
            curve,
            plane,
            structuralRole.Value,
            height ?? 3.0
          );

          DA.SetData(_AnalyticalPanel_, analyticalPanel);
          return analyticalPanel;
        }
      );
#endif
    }

#if REVIT_2023
    bool Reuse
    (
      ARDB_AnalyticalPanel analyticalPanel,
      Curve curve,
      Plane plane,
      ARDB.Structure.AnalyticalStructuralRole structuralRole,
      double height
    )
    {
      var tol = GeometryTolerance.Model;
      if (analyticalPanel is null) return false;

      // Curve
      var curveLoop = analyticalPanel.GetOuterContour().ToPolyCurve();
      var currentCurve = Curve.ProjectToPlane(curveLoop, plane).DuplicateSegments();

      if (currentCurve is null)
        return false;

      if (!currentCurve[0].ToCurve().IsSameKindAs(curve.ToCurve()))
        return false;

      Curve reversedCurve = currentCurve[0].DuplicateCurve();
      reversedCurve.Reverse();
      if (!currentCurve[0].ToCurve().AlmostEquals(curve.ToCurve(), analyticalPanel.Document.Application.VertexTolerance) &&
        !reversedCurve.ToCurve().AlmostEquals(curve.ToCurve(), analyticalPanel.Document.Application.VertexTolerance)
        )
        return false;

      // Height
      if (!curveLoop.TryGetPolyline(out var profile))
        return false;

      foreach (var edge in profile.GetSegments())
      {
        if (edge.Direction.IsParallelTo(plane.Normal, tol.DefaultTolerance) > 0 &&
            Math.Abs(Math.Abs(height) - Math.Abs(edge.Length)) > tol.DefaultTolerance)
        {
          return false;
        }
      }

      // Structural Role
      if (analyticalPanel.StructuralRole != structuralRole)
        analyticalPanel.StructuralRole = structuralRole;

      // Plane
      if (!curveLoop.TryGetPlane(out var currentPlane, tol.VertexTolerance))
        return false;

      if (!(Math.Abs(currentPlane.Normal * plane.Normal) < tol.DefaultTolerance))
        return false;

      var mainCurve = profile.GetSegments()
                             .ToList()
                             .Select(x => x.ToNurbsCurve().IsInPlane(plane, tol.DefaultTolerance))
                             .Count();

      if (profile.GetSegments()
                 .ToList()
                 .Select(x => x.ToNurbsCurve().IsInPlane(plane, tol.DefaultTolerance))
                 .Count() != 1)
      {
        return false;
      }

      return true;
    }

    ARDB_AnalyticalPanel Create(ARDB.Document doc, Curve curve, Plane plane, ARDB.Structure.AnalyticalStructuralRole structuralRole, double height)
    {
      ARDB_AnalyticalPanel analyticalPanel = ARDB_AnalyticalPanel.Create(
        doc,
        curve.ToCurve(),
        plane.Normal.ToXYZ() * (1 / Revit.ModelUnits) * height * -1 );
      analyticalPanel.StructuralRole = structuralRole;

      return analyticalPanel;
    }

    ARDB_AnalyticalPanel Reconstruct
    (
      ARDB_AnalyticalPanel analyticalPanel,
      ARDB.Document doc,
      Curve curve,
      Plane plane,
      ARDB.Structure.AnalyticalStructuralRole structuralRole,
      double height
    )
    {
      if (!Reuse(analyticalPanel, curve, plane, structuralRole, height))
      {
        analyticalPanel = analyticalPanel.ReplaceElement
        (
          Create(doc, curve, plane, structuralRole, height),
          ExcludeUniqueProperties
        );
        analyticalPanel.Document.Regenerate();
      }

      return analyticalPanel;
    }
#endif
  }
}

