using System;
using Autodesk.Revit.DB.Structure;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Types;
using ARDB = Autodesk.Revit.DB;

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
      ARDB.BuiltInParameter.BOUNDARY_DIRECTION_X,
      ARDB.BuiltInParameter.BOUNDARY_DIRECTION_Y,
      ARDB.BuiltInParameter.BOUNDARY_DIRECTION_Z,
      ARDB.BuiltInParameter.BOUNDARY_DIRECTION_ROT_X,
      ARDB.BuiltInParameter.BOUNDARY_DIRECTION_ROT_Y,
      ARDB.BuiltInParameter.BOUNDARY_DIRECTION_ROT_Z,
      ARDB.BuiltInParameter.LOAD_USE_LOCAL_COORDINATE_SYSTEM
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;
      if (!Parameters.Document.TryGetStructuralSettings(doc, out var settings) || !settings.BoundaryConditionFamilySymbolFixed.IsValid())
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Document does not have symbols for Boundary Conditions representation.");
        return;
      }

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

          DA.SetData(_BoundaryConditions_, new LineBoundaryConditions(boundaryConditions));
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
      }
      return bConditions;
    }
  }
}
