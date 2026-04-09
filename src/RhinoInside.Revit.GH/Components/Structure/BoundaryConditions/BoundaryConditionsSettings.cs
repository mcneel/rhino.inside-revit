using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.Structure
{
  [ComponentVersion(introduced: "1.27")]
  public class BoundaryConditionsSettings : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("B5144C5D-F374-4786-99A7-6A579ED2FD59");
    public override GH_Exposure Exposure => GH_Exposure.senary | GH_Exposure.obscure;

    public BoundaryConditionsSettings() : base
    (
      name: "Boundary Conditions Settings",
      nickname: "BCS",
      description: "Boundary conditions settings associated with a Revit document.",
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
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = _Fixed_,
          NickName = _Fixed_.Substring(0,1),
          Description = "The FamilySymbol to represent a fixed boundary condition.",
          Optional = true,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = _Pinned_,
          NickName = _Pinned_.Substring(0,1),
          Description = "The FamilySymbol to represent a pinned boundary condition.",
          Optional = true,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = _Roller_,
          NickName = _Roller_.Substring(0,1),
          Description = "The FamilySymbol to represent a roller boundary condition.",
          Optional = true,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = _User_,
          NickName = _User_.Substring(0,1),
          Description = "The FamilySymbol to represent a user defined boundary condition.",
          Optional = true,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Number
        {
          Name = _Spacing_,
          NickName = _Spacing_.Substring(0,1),
          Description = "Symbol spacing for boundary conditions.",
          Optional = true,
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Element
        {
          Name = _StructuralSettings_,
          NickName = "SS",
          Description = "Structural Settings element.",
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = _Fixed_,
          NickName = _Fixed_.Substring(0,1),
          Description = "The FamilySymbol to represent a fixed boundary condition.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = _Pinned_,
          NickName = _Pinned_.Substring(0,1),
          Description = "The FamilySymbol to represent a pinned boundary condition.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = _Roller_,
          NickName = _Roller_.Substring(0,1),
          Description = "The FamilySymbol to represent a roller boundary condition.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = _User_,
          NickName = _User_.Substring(0,1),
          Description = "The FamilySymbol to represent a user defined boundary condition.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Number
        {
          Name = _Spacing_,
          NickName = _Spacing_.Substring(0,1),
          Description = "Symbol spacing for boundary conditions.",
        }, ParamRelevance.Primary
      ),
    };

    const string _StructuralSettings_ = "Structural Settings";
    const string _Fixed_ = "Fixed";
    const string _Pinned_ = "Pinned";
    const string _Roller_ = "Roller";
    const string _User_ = "User";
    const string _Spacing_ = "Spacing";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc)) return;
      if (!Parameters.BoundaryConditions.TryGetStructuralSettings(doc, out var settings)) return;
      else Params.TrySetData(DA, _StructuralSettings_, () => settings);

      if (!Params.TryGetData(DA, _Fixed_, out Types.FamilySymbol fixedSymbol)) return;
      if (!Params.TryGetData(DA, _Pinned_, out Types.FamilySymbol pinnedSymbol)) return;
      if (!Params.TryGetData(DA, _Roller_, out Types.FamilySymbol rollerSymbol)) return;
      if (!Params.TryGetData(DA, _User_, out Types.FamilySymbol userSymbol)) return;
      if (!Params.TryGetData(DA, _Spacing_, out double? spacing)) return;

      if (fixedSymbol is object && settings.BoundaryConditionFamilySymbolFixed != fixedSymbol.Id)
      {
        StartTransaction(doc.Value);
        settings.BoundaryConditionFamilySymbolFixed = fixedSymbol.Id;
      }
      Params.TrySetData(DA, _Fixed_, () => doc.GetElement<Types.FamilySymbol>(settings.BoundaryConditionFamilySymbolFixed));

      if (pinnedSymbol is object && settings.BoundaryConditionFamilySymbolPinned != pinnedSymbol.Id)
      {
        StartTransaction(doc.Value);
        settings.BoundaryConditionFamilySymbolPinned = pinnedSymbol.Id;
      }
      Params.TrySetData(DA, _Pinned_, () => doc.GetElement<Types.FamilySymbol>(settings.BoundaryConditionFamilySymbolPinned));

      if (rollerSymbol is object && settings.BoundaryConditionFamilySymbolRoller != rollerSymbol.Id)
      {
        StartTransaction(doc.Value);
        settings.BoundaryConditionFamilySymbolRoller = rollerSymbol.Id;
      }
      Params.TrySetData(DA, _Roller_, () => doc.GetElement<Types.FamilySymbol>(settings.BoundaryConditionFamilySymbolRoller));

      if (userSymbol is object && settings.BoundaryConditionFamilySymbolUserDefined != userSymbol.Id)
      {
        StartTransaction(doc.Value);
        settings.BoundaryConditionFamilySymbolUserDefined = userSymbol.Id;
      }
      Params.TrySetData(DA, _User_, () => doc.GetElement<Types.FamilySymbol>(settings.BoundaryConditionFamilySymbolUserDefined));

      if (spacing.HasValue && settings.BoundaryConditionAreaAndLineSymbolSpacing != spacing.Value / Revit.ModelUnits)
      {
        StartTransaction(doc.Value);
        settings.BoundaryConditionAreaAndLineSymbolSpacing = spacing.Value / Revit.ModelUnits;
      }
      Params.TrySetData(DA, _Spacing_, () => settings.BoundaryConditionAreaAndLineSymbolSpacing * Revit.ModelUnits);
    }
  }
}
