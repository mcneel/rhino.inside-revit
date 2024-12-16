using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace RhinoInside.Revit.GH.Components.Structure
{
  [ComponentVersion(introduced: "1.27")]
  public class AddLineBoundaryConditions : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("647AC069-ABF4-454F-BFC5-8559F2980877");

    public override GH_Exposure Exposure => GH_Exposure.senary;

    public AddLineBoundaryConditions() : base
    (
      name: "Add Boundary Conditions (Line)",
      nickname: "BC-Line",
      description: "Given a referenced curve, this component adds line boundary conditions to the analytical model.",
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
        new Parameters.GeometryCurve()
        {
          Name = "Curve",
          NickName = "C",
          Description = "Curve to reference the boundary condition",
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.BoundaryConditions()
        {
          Name = _BoundaryConditions_,
          NickName = _BoundaryConditions_.Substring(0, 1),
          Description = $"Output {_BoundaryConditions_}",
        }
      )
    };

    const string _BoundaryConditions_ = "Boundary Conditions";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {

    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.Structure.BoundaryConditions>
      (
        doc.Value, _BoundaryConditions_, boundaryConditions =>
        {
          if (!Params.GetData(DA, "Curve", out Types.GeometryCurve curve)) return null;

          boundaryConditions = Reconstruct
          (
            boundaryConditions,
            doc.Value,
            curve
          );

          DA.SetData(_BoundaryConditions_, boundaryConditions);
          return boundaryConditions;
        }
      );
    }

    bool Reuse(ARDB.Structure.BoundaryConditions bConditions, Types.GeometryCurve curve)
    {
      if (bConditions is null) return false;
      if (!(bConditions.GetCurve().IsSameKindAs(curve.Value) && bConditions.GetCurve().AlmostEquals(curve.Value)))
        return false;
      return true;
    }

    ARDB.Structure.BoundaryConditions Create(ARDB.Document doc, Types.GeometryCurve curve)
    {
      var bConditions = doc.Create.NewLineBoundaryConditions(curve.GetReference(),
                                                             TranslationRotationValue.Fixed, 0.0,
                                                             TranslationRotationValue.Fixed, 0.0,
                                                             TranslationRotationValue.Fixed, 0.0,
                                                             TranslationRotationValue.Fixed, 0.0);

      return bConditions;
    }

    ARDB.Structure.BoundaryConditions Reconstruct
    (
      ARDB.Structure.BoundaryConditions bConditions,
      ARDB.Document doc,
      Types.GeometryCurve curve
    )
    {
      if (!Reuse(bConditions, curve))
      {
        bConditions = bConditions.ReplaceElement
        (
          Create(doc, curve),
          ExcludeUniqueProperties
        );

        bConditions.Document.Regenerate();
      }
      return bConditions;
    }
  }
}
