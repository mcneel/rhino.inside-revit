using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using System.Collections.Generic;
using RhinoInside.Revit.GH.Exceptions;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using System.Linq;
using RhinoInside.Revit.GH.Parameters;

namespace RhinoInside.Revit.GH.Components.Structure
{
  #if REVIT_2023
    using ARDB_Structure_AnalyticalPanel = ARDB.Structure.AnalyticalPanel;
#else
      using ARDB_Structure_AnalyticalPanel = ARDB.Structure.AnalyticalModelSurface;
#endif

  [ComponentVersion(introduced: "1.27")]
  public class AddAnalyticalPanelByBoundary : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("BA2D1733-0A7A-463C-BDDC-4262405F4FE6");
#if REVIT_2023
    public override GH_Exposure Exposure => GH_Exposure.secondary;
#else
    public override GH_Exposure Exposure => GH_Exposure.hidden;
#endif
    public AddAnalyticalPanelByBoundary() : base
    (
      name: "Add Analytical Panel By Boundary",
      nickname: "A-BoundaryPanel",
      description: "Given its boundary, it adds an analytical panel to the active Revit document",
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
          Name = "Boundary",
          NickName = "B",
          Description = "Analytical panel boundary profile",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Param_Enum<Types.AnalyticalStructuralRole>
        {
          Name = "Structural Role",
          NickName = "R",
          Description = "Analytical element structural role",
          Access = GH_ParamAccess.item,
          Optional = true,
        }.SetDefaultVale(ARDB.Structure.AnalyticalStructuralRole.StructuralRolePanel)
      )
#endif
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
#if REVIT_2023
      new ParamDefinition
      (
        new Parameters.AnalyticalPanel()
        {
          Name = _AnalyticalPanel_,
          NickName = _AnalyticalPanel_.Substring(0, 1),
          Description = $"Output {_AnalyticalPanel_}",
        }
      )
#endif
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

      ReconstructElement<ARDB_Structure_AnalyticalPanel>
      (
        doc.Value, _AnalyticalPanel_, analyticalPanel =>
        {

          // Input
          if (!Params.GetDataList(DA, "Boundary", out IList<Curve> boundary)) return null;
          if (!Params.GetData(DA, "Structural Role", out Types.AnalyticalStructuralRole structuralRole)) return null;

          var tol = GeometryTolerance.Model;
          for (int index = 0; index < boundary.Count; ++index)
          {
            var loop = boundary[index];
            if (loop is null) return null;
            var plane = default(Plane);
            if
            (
              loop.IsShort(tol.ShortCurveTolerance) ||
              !loop.IsClosed ||
              !loop.TryGetPlane(out plane, tol.VertexTolerance)
            )
              throw new RuntimeArgumentException(nameof(boundary), "Boundary loop curves should be a set of valid coplanar and closed curves.", boundary);

            boundary[index] = loop.Simplify(CurveSimplifyOptions.All & ~CurveSimplifyOptions.Merge, tol.VertexTolerance, tol.AngleTolerance) ?? loop;
          }

          // Compute
          analyticalPanel = Reconstruct
          (
            analyticalPanel,
            doc.Value,
            boundary
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
      ARDB_Structure_AnalyticalPanel analyticalPanel,
      IList<Curve> boundary
    )
    {
      if (analyticalPanel is null) return false;

      if (analyticalPanel.GetOuterContour() is null) return false;

      var curveLoop = boundary.ConvertAll(x => x.ToCurveLoop()).FirstOrDefault();
      analyticalPanel.SetOuterContour(curveLoop);
      return true;
    }

    ARDB_Structure_AnalyticalPanel Create(ARDB.Document doc, IList<Curve> boundary)
    {
      var curveLoop = boundary.ConvertAll(x => x.ToCurveLoop()).FirstOrDefault();

      if (curveLoop is null)
        throw new ArgumentException("Failed to convert boundary curves to CurveLoop.", nameof(boundary));

      ARDB_Structure_AnalyticalPanel analyticalPanel = ARDB_Structure_AnalyticalPanel.Create(doc, curveLoop);

      return analyticalPanel;
    }


    ARDB_Structure_AnalyticalPanel Reconstruct
    (
      ARDB_Structure_AnalyticalPanel analyticalPanel,
      ARDB.Document doc,
      IList<Curve> boundary
    )
    {
      if (!Reuse(analyticalPanel, boundary))
      {
        analyticalPanel = analyticalPanel.ReplaceElement
        (
          Create(doc, boundary),
          ExcludeUniqueProperties
        );
        analyticalPanel.Document.Regenerate();
      }

      return analyticalPanel;
    }
#endif
  }
}
