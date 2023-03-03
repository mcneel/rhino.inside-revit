using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.14")]
  class AddStructuralWallFoundation : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("9FF1C32F-4855-4F32-95CA-ACCB4AA564DE");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public AddStructuralWallFoundation() : base
    (
      name: "Add Structural Wall Foundation",
      nickname: "S-Wall Foundation",
      description: "Given its host element, it adds a structural wall foundation element to the active Revit document",
      category: "Revit",
      subCategory: "Build"
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
        new Parameters.ElementType
        {
          Name = "Type",
          NickName = "T",
          Description = "Structural Wall Foundation type.",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_StructuralFoundation
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Host",
          NickName = "H",
          Description = "Host element.",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.HostObject()
        {
          Name = _Foundation_,
          NickName = _Foundation_.Substring(0, 1),
          Description = $"Output {_Foundation_}",
        }
      )
    };

    const string _Foundation_ = "Wall Foundation";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.INSTANCE_MOVES_WITH_GRID_PARAM,
      ARDB.BuiltInParameter.FAMILY_LEVEL_PARAM,
      ARDB.BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.HostObject>
      (
        doc.Value, _Foundation_, foundation =>
        {
          var tol = GeometryTolerance.Model;

          // Input
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out Types.ElementType type, doc, ARDB.ElementTypeGroup.WallFoundationType)) return null;
          if (!Params.GetData(DA, "Host", out Types.GraphicalElement host)) return null;

          // Compute
          foundation = Reconstruct
          (
            foundation,
            doc.Value,
            type.Id,
            host.Value
          );

          DA.SetData(_Foundation_, foundation);
          return foundation;
        }
      );
    }

    bool Reuse
    (
      ARDB.HostObject foundation,
      ARDB.ElementId typeId,
      ARDB.Element host
    )
    {
      if (foundation is null) return false;

      var foundationMask = foundation as ARDB.WallFoundation;
      if (foundationMask.WallId != host.Id) return false;

      if (foundation.GetTypeId() != typeId)
      {
        if (ARDB.Element.IsValidType(foundation.Document, new ARDB.ElementId[] { foundation.Id }, typeId))
        {
          if (foundation.ChangeTypeId(typeId) is ARDB.ElementId id &&
              id != ARDB.ElementId.InvalidElementId)
            foundation = foundation.Document.GetElement(id) as ARDB.WallFoundation;
        }
        else
          return false;
      }

      return true;
    }

    ARDB.WallFoundation Create(ARDB.Document doc, ARDB.ElementId typeId, ARDB.Element host)
    {
      var instance = ARDB.WallFoundation.Create(doc, typeId, host.Id);

      // We turn analytical model off by default
      instance.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL)?.Update(false);
      return instance;
    }

    ARDB.HostObject Reconstruct
    (
      ARDB.HostObject foundation,
      ARDB.Document doc,
      ARDB.ElementId typeId,
      ARDB.Element host
    )
    {
      if (!Reuse(foundation, typeId, host))
      {
        foundation = foundation.ReplaceElement
        (
          Create(doc, typeId, host),
          ExcludeUniqueProperties
        );
      }

      foundation.get_Parameter(ARDB.BuiltInParameter.INSTANCE_MOVES_WITH_GRID_PARAM)?.Update(false);     
      foundation.Document.Regenerate();
      foundation.Pinned = false;     

      return foundation;
    }
  }
}
