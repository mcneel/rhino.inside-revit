using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using RhinoInside.Revit.GH.Parameters;

namespace RhinoInside.Revit.GH.Components.Structure
{
#if REVIT_2023
  using ARDB_AnalyticalElement = ARDB.Structure.AnalyticalElement;
#else
  using ARDB_AnalyticalMember = ARDB.Structure.AnalyticalModelStick;
#endif

  [ComponentVersion(introduced: "1.27"), ComponentRevitAPIVersion(min: "2023.0")]
  public class SetStructuralRole : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("6844CF5E-8015-457E-AC7E-0E58C6B80A82");
#if REVIT_2023
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
#else
    public override GH_Exposure Exposure => GH_Exposure.hidden;
#endif
    public SetStructuralRole() : base
    (
      name: "Set Structural Role",
      nickname: "S-Role",
      description: "Given an analytical element from the Revit document, this component sets its structural role",
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
        new Parameters.AnalyticalElement()
        {
          Name = "Analytical Element",
          NickName = "AE",
          Description = "Analytical element to set the structural role",
          Access = GH_ParamAccess.item
        }
      ),

      new ParamDefinition
      (
        new Param_Enum<Types.AnalyticalStructuralRole>
        {
          Name = "Structural Role",
          NickName = "SR",
          Description = "Structural Role to apply to the analytical member",
          Access = GH_ParamAccess.item
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
          NickName = _AnalyticalElement_.Substring(0, 1),
          Description = $"Output {_AnalyticalElement_}",
        }
      )
    };

    const string _AnalyticalElement_ = "Analytical Element";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
#if REVIT_2023
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB_AnalyticalElement>
      (
        doc.Value, _AnalyticalElement_, analyticalElement =>
        {
          // Input
          if (!Params.GetData(DA, "Analytical Element", out Types.AnalyticalElement element)) return null;
          if (!Params.GetData(DA, "Structural Role", out Types.AnalyticalStructuralRole structuralRole)) return null;

          // Compute
          if (element.Value.StructuralRole != structuralRole.Value)
          {
            element.Value.StructuralRole = structuralRole.Value;
            analyticalElement = element.Value;
          }

          DA.SetData(_AnalyticalElement_, analyticalElement);
          return analyticalElement;
        }
      );
#endif
    }
  }
}

