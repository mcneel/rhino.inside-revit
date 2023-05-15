using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.14")]
  public class AddStructuralWallFoundation : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("9FF1C32F-4855-4F32-95CA-ACCB4AA564DE");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public AddStructuralWallFoundation() : base
    (
      name: "Add Wall Foundation",
      nickname: "S-Wall Foundation",
      description: "Given its host element, it adds a structural wall foundation element to the active Revit document",
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
        new Parameters.HostObjectType
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
        new Parameters.Wall()
        {
          Name = "Wall",
          NickName = "W",
          Description = "Wall element.",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.WallFoundation()
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

      ReconstructElement<ARDB.WallFoundation>
      (
        doc.Value, _Foundation_, foundation =>
        {
          var tol = GeometryTolerance.Model;

          // Input
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out Types.ElementType type, doc, ARDB.ElementTypeGroup.WallFoundationType)) return null;
          if (!Params.GetData(DA, "Wall", out Types.Wall wall)) return null;

          // Compute
          foundation = Reconstruct
          (
            foundation,
            doc.Value,
            type.Id,
            wall.Value
          );

          DA.SetData(_Foundation_, foundation);
          return foundation;
        }
      );
    }

    bool Reuse
    (
      ARDB.WallFoundation foundation,
      ARDB.ElementId typeId,
      ARDB.Wall wall
    )
    {
      if (foundation is null) return false;

      if (foundation.WallId != wall.Id) return false;

      if (foundation.GetTypeId() != typeId)
      {
        if (!ARDB.Element.IsValidType(foundation.Document, new ARDB.ElementId[] { foundation.Id }, typeId))
          return false;

        foundation.ChangeTypeId(typeId);
      }

      return true;
    }

    ARDB.WallFoundation Create(ARDB.Document doc, ARDB.ElementId typeId, ARDB.Wall wall)
    {
      var foundation = ARDB.WallFoundation.Create(doc, typeId, wall.Id);

      // We turn analytical model off by default
      foundation.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL)?.Update(false);
      return foundation;
    }

    ARDB.WallFoundation Reconstruct
    (
      ARDB.WallFoundation foundation,
      ARDB.Document doc,
      ARDB.ElementId typeId,
      ARDB.Wall wall
    )
    {
      if (!Reuse(foundation, typeId, wall))
      {
        foundation = foundation.ReplaceElement
        (
          Create(doc, typeId, wall),
          ExcludeUniqueProperties
        );
      }

      foundation.get_Parameter(ARDB.BuiltInParameter.INSTANCE_MOVES_WITH_GRID_PARAM)?.Update(false);

      return foundation;
    }
  }
}
