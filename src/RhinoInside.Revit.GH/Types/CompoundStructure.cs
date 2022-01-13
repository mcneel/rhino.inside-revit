using System;
using System.Collections.Generic;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.System.Collections.Generic;

  [Kernel.Attributes.Name("Compound Structure")]
  public class CompoundStructure : ValueObject
  {
    #region DocumentObject
    public override string DisplayName
    {
      get
      {
        if (Value is ARDB.CompoundStructure structure)
          return $"{structure.GetWidth() * Revit.ModelUnits} {Grasshopper.Kernel.GH_Format.RhinoUnitSymbol()} compound structure : {structure.LayerCount} layers";

        return "<None>";
      }
    }
    #endregion

    public new ARDB.CompoundStructure Value => base.Value as ARDB.CompoundStructure;

    public CompoundStructure() : base() { }

    public CompoundStructure(ARDB.Document doc, ARDB.CompoundStructure value) : base(doc, value) { }
    public CompoundStructure(ARDB.Document doc) : base
    (
      doc,
      ARDB.CompoundStructure.CreateSingleLayerCompoundStructure(ARDB.MaterialFunctionAssignment.None, 1.0, ARDB.ElementId.InvalidElementId)
    )
    {
      Value.OpeningWrapping = ARDB.OpeningWrappingCondition.None;
      Value.EndCap = ARDB.EndCapCondition.NoEndCap;
    }

    public CompoundStructure(ARDB.Document doc, double sampleHeight) : base
    (
      doc,
      ARDB.CompoundStructure.CreateSingleLayerCompoundStructure(sampleHeight / Revit.ModelUnits, ARDB.MaterialFunctionAssignment.None, 1.0, ARDB.ElementId.InvalidElementId)
    )
    {
      Value.OpeningWrapping = ARDB.OpeningWrappingCondition.None;
      Value.EndCap = ARDB.EndCapCondition.NoEndCap;
    }

    public CompoundStructure(ARDB.Document doc, IList<CompoundStructureLayer> core) : base
    (
      doc,
      ARDB.CompoundStructure.CreateSimpleCompoundStructure(core.ConvertAll(x => x.Value))
    )
    { }

    public bool Audit(out IDictionary<int, ARDB.CompoundStructureError> errors)
    {
      errors = default;
      var valid = false;
      if (Value is ARDB.CompoundStructure structure)
      {
        valid = structure.IsValid(Document, out errors, out var m);
      }

      return valid;
    }

    static readonly Dictionary<ARDB.CompoundStructureError, string> CompoundStructureErrorMessages = new Dictionary<ARDB.CompoundStructureError, string>
    {
      { ARDB.CompoundStructureError.BadShellOrder, "Layer Function Priorities ascend from the Core Boundary to the Finish Face." },
      { ARDB.CompoundStructureError.CoreTooThin, "Core contain a membrane layer or thickness of core is zero." },
      { ARDB.CompoundStructureError.MembraneTooThick, "Thickness of membrane layer is more than zero." },
      { ARDB.CompoundStructureError.NonmembraneTooThin, "Thickness of non-membrane layer is too thin." },
      { ARDB.CompoundStructureError.BadShellsStructure, "The number of shell layers is larger than the total number of layers." },
      { ARDB.CompoundStructureError.ThinOuterLayer, "Thickness of face layer is too thin." },
      { ARDB.CompoundStructureError.VerticalUnusedLayer, "A layer is not membrane layer and the Thickness of layer is zero." },
      { ARDB.CompoundStructureError.VerticalWrongOrderLayer, "Layers assigned to the same Row are not on the same side of the Core Boundary." },
      { ARDB.CompoundStructureError.VerticalWrongOrderCoreExterior, "Exterior core boundary have more than one face at any height." },
      { ARDB.CompoundStructureError.VerticalWrongOrderCoreInterior, "Interior core boundary have more than one face at any height." },
      { ARDB.CompoundStructureError.VerticalWrongOrderMembrane, "Membrane Layer have more than one face at any height." },
      { ARDB.CompoundStructureError.DeckCantBoundAbove, "There is no layer above Structural deck or it is too thin." },
      { ARDB.CompoundStructureError.DeckCantBoundBelow, "There is no layer below Structural deck or it is too thin." },
      { ARDB.CompoundStructureError.VarThickLayerCantBeZero, "Variable thickness layer have zero thickness." },
      { ARDB.CompoundStructureError.InvalidMaterialId, "Element id used as material id does not correspond to an actual MaterialElem." },
      { ARDB.CompoundStructureError.ExtensibleRegionsNotContiguousAlongTop, "Extension Layers at the top of the wall must be adjacent." },
      { ARDB.CompoundStructureError.ExtensibleRegionsNotContiguousAlongBottom, "Extension Layers at the bottom of the wall must be adjacent." },
      { ARDB.CompoundStructureError.InvalidProfileId, "Element id used as profile id does not correspond to a valid deck profile." },
    };

    public static string ToString(ARDB.CompoundStructureError error)
    {
      return CompoundStructureErrorMessages.TryGetValue(error, out var message) ?
        message :
        "Unknown compound structure error";
    }

    #region Properties
    public IList<CompoundStructureLayer> Layers
    {
      get
      {
        if (Value is ARDB.CompoundStructure structure)
        {
          var layers = structure.GetLayers();

          var value = new CompoundStructureLayer[layers.Count];
          for (int l = 0; l < value.Length; ++l)
            value[l] = new CompoundStructureLayer(Document, layers[l]);

          return value;
        }

        return default;
      }
      set
      {
        if (Value is ARDB.CompoundStructure structure)
        {
          var layers = new ARDB.CompoundStructureLayer[value.Count];
          for (int l = 0; l < value.Count; ++l)
            layers[l] = value[l].Value;

          structure.SetLayers(layers);
        }
      }
    }

    public void GetLayers(out IList<CompoundStructureLayer> exterior, out IList<CompoundStructureLayer> core, out IList<CompoundStructureLayer> interior)
    {
      if (Value is ARDB.CompoundStructure structure)
      {
        var layers = structure.GetLayers();
        exterior  = new CompoundStructureLayer[structure.GetNumberOfShellLayers(ARDB.ShellLayerType.Exterior)];
        interior  = new CompoundStructureLayer[structure.GetNumberOfShellLayers(ARDB.ShellLayerType.Interior)];
        core      = new CompoundStructureLayer[-exterior.Count + layers.Count - interior.Count];

        var material = structure.StructuralMaterialIndex;
        var variable = structure.VariableLayerIndex;

        int l = 0;
        for (int e = 0; e < exterior.Count; ++e, ++l)
        {
          exterior[e] = new CompoundStructureLayer(Document, layers[l]);
          if (l == material) exterior[e].StructuralMaterial = true;
          if (l == variable) exterior[e].VariableWidth = true;
        }

        for (int c = 0; c < core.Count; ++c, ++l)
        {
          core[c] = new CompoundStructureLayer(Document, layers[l]);
          if (l == material) core[c].StructuralMaterial = true;
          if (l == variable) core[c].VariableWidth = true;
        }

        for (int i = 0; i < interior.Count; ++i, ++l)
        {
          interior[i] = new CompoundStructureLayer(Document, layers[l]);
          if (l == material) interior[i].StructuralMaterial = true;
          if (l == variable) interior[i].VariableWidth= true;
        }
      }
      else exterior = core = interior = default;
    }

    public void SetLayers(IList<CompoundStructureLayer> exterior, IList<CompoundStructureLayer> core, IList<CompoundStructureLayer> interior)
    {
      if (Value is ARDB.CompoundStructure structure)
      {
        var layers = new ARDB.CompoundStructureLayer[(exterior?.Count ?? 0) + (core?.Count ?? 0) + (interior?.Count ?? 0)];
        {
          int l = 0;
          for (int e = 0; e < exterior?.Count; ++e, ++l)
            layers[l] = exterior[e].Value;

          for (int c = 0; c < core?.Count; ++c, ++l)
            layers[l] = core[c].Value;

          for (int i = 0; i < interior?.Count; ++i, ++l)
            layers[l] = interior[i].Value;
        }

        structure.SetLayers(layers);
        structure.SetNumberOfShellLayers(ARDB.ShellLayerType.Exterior, exterior?.Count ?? 0);
        structure.SetNumberOfShellLayers(ARDB.ShellLayerType.Interior, interior?.Count ?? 0);

        {
          int l = 0;
          for (int e = 0; e < exterior?.Count; ++e, ++l)
          {
            structure.SetParticipatesInWrapping(l, exterior[e].LayerCapFlag.Value);
            if (exterior[e].VariableWidth.Value) structure.VariableLayerIndex = l;
          }

          for (int c = 0; c < core?.Count; ++c, ++l)
          {
            if (core[c].StructuralMaterial.Value) structure.StructuralMaterialIndex = l;
            if (core[c].VariableWidth.Value) structure.VariableLayerIndex = l;
          }

          for (int i = 0; i < interior?.Count; ++i, ++l)
          {
            structure.SetParticipatesInWrapping(l, interior[i].LayerCapFlag.Value);
            if (interior[i].VariableWidth.Value) structure.VariableLayerIndex = l;
          }
        }
      }
    }

    public double GetWidth()
    {
      return Value is ARDB.CompoundStructure structure ? structure.GetWidth() * Revit.ModelUnits : 0.0;
    }

    public void SetWidth(double width)
    {
      width /= Revit.ModelUnits;
      if (Value is ARDB.CompoundStructure structure)
      {
        var total = structure.GetWidth();
        var count = structure.LayerCount;
        for (int l = 0; l < count; ++l)
        {
          var w = structure.GetLayerWidth(l);
          if (w == 0.0) continue;

          var function = structure.GetLayerFunction(l);
          if(function != ARDB.MaterialFunctionAssignment.StructuralDeck)
            structure.SetLayerWidth(l, w / total * width);
        }
      }
    }

    public OpeningWrappingCondition OpeningWrapping
    {
      get => Value is ARDB.CompoundStructure structure ? new OpeningWrappingCondition(structure.OpeningWrapping) : default;
      set
      {
        if (value is object && Value is ARDB.CompoundStructure structure)
          structure.OpeningWrapping = value.Value;
      }
    }

    public EndCapCondition EndCap
    {
      get => Value is ARDB.CompoundStructure structure ? new EndCapCondition(structure.EndCap) : default;
      set
      {
        if (value is object && Value is ARDB.CompoundStructure structure)
          structure.EndCap = value.Value;
      }
    }

    public double? SampleHeight
    {
      get => Value is ARDB.CompoundStructure structure ? structure.SampleHeight * Revit.ModelUnits : default;
      set
      {
        if (value is object && Value is ARDB.CompoundStructure structure)
          structure.SampleHeight = value.Value / Revit.ModelUnits;
      }
    }

    public double? CutoffHeight
    {
      get => Value is ARDB.CompoundStructure structure ? structure.CutoffHeight * Revit.ModelUnits : default;
      set
      {
        if (value is object && Value is ARDB.CompoundStructure structure)
          structure.CutoffHeight = value.Value / Revit.ModelUnits;
      }
    }
    #endregion
  }

  [Kernel.Attributes.Name("Compound Structure Layer")]
  public class CompoundStructureLayer : ValueObject, ICloneable
  {
    #region DocumentObject
    public override string DisplayName
    {
      get
      {
        if (Value is ARDB.CompoundStructureLayer layer)
          return $"{layer.Function} : {layer.Width * Revit.ModelUnits} {Grasshopper.Kernel.GH_Format.RhinoUnitSymbol()}";

        return "<None>";
      }
    }
    #endregion

    public new ARDB.CompoundStructureLayer Value => base.Value as ARDB.CompoundStructureLayer;

    object ICloneable.Clone()
    {
      return new CompoundStructureLayer
      (
        Document,
        Value is ARDB.CompoundStructureLayer layer ? new ARDB.CompoundStructureLayer(layer) : default
      );
    }

    public CompoundStructureLayer() : base() { }

    public CompoundStructureLayer(CompoundStructureLayer value) :
      base(value?.Document, value is null ? null : new ARDB.CompoundStructureLayer(value.Value))
    { }

    public CompoundStructureLayer(ARDB.Document doc, ARDB.CompoundStructureLayer value) :
      base(doc, value) { }

    public CompoundStructureLayer(ARDB.Document doc) :
      base(doc, new ARDB.CompoundStructureLayer()) { }

    #region Properties
    public LayerFunction Function
    {
      get => Value is ARDB.CompoundStructureLayer layer ? new LayerFunction(layer.Function) : default;
      set
      {
        if (value is object && Value is ARDB.CompoundStructureLayer layer)
          layer.Function = value.Value;
      }
    }

    public Material Material
    {
      get => Value is ARDB.CompoundStructureLayer layer ? new Material(Document, layer.MaterialId) : default;
      set
      {
        if (value is object && Value is ARDB.CompoundStructureLayer layer)
        {
          AssertValidDocument(value, nameof(Material));
          layer.MaterialId = value.Id;
        }
      }
    }

    bool structuralMaterial = false;
    public bool? StructuralMaterial
    {
      get => Value is ARDB.CompoundStructureLayer ? structuralMaterial : default;
      set
      {
        if (value is object && Value is ARDB.CompoundStructureLayer layer)
          structuralMaterial = value.Value;
      }
    }

    public static double MinWidth => ARDB.CompoundStructure.GetMinimumLayerThickness() * Revit.ModelUnits;

    public double? Width
    {
      get => Value is ARDB.CompoundStructureLayer layer ? layer.Width * Revit.ModelUnits : default;
      set
      {
        if (value is object && Value is ARDB.CompoundStructureLayer layer)
          layer.Width = value.Value / Revit.ModelUnits;
      }
    }

    bool variableWidth = false;
    public bool? VariableWidth
    {
      get => Value is ARDB.CompoundStructureLayer ? variableWidth : default;
      set
      {
        if (value is object && Value is ARDB.CompoundStructureLayer layer)
          variableWidth = value.Value;
      }
    }

    public bool? LayerCapFlag
    {
      get => Value is ARDB.CompoundStructureLayer layer ? layer.LayerCapFlag : default;
      set
      {
        if (value is object && Value is ARDB.CompoundStructureLayer layer)
          layer.LayerCapFlag = value.Value;
      }
    }

    public FamilySymbol DeckProfile
    {
      get => Value is ARDB.CompoundStructureLayer layer ? Element.FromElementId(Document, layer.DeckProfileId) as FamilySymbol : default;
      set
      {
        if (value is object && Value is ARDB.CompoundStructureLayer layer)
        {
          AssertValidDocument(value, nameof(DeckProfile));
          layer.DeckProfileId = value.Id;
        }
      }
    }

    public DeckEmbeddingType DeckEmbeddingType
    {
      get => Value is ARDB.CompoundStructureLayer layer ? new DeckEmbeddingType(layer.DeckEmbeddingType) : default;
      set
      {
        if (value is object && Value is ARDB.CompoundStructureLayer layer)
          layer.DeckEmbeddingType = value.Value;
      }
    }
    #endregion
  }
}
