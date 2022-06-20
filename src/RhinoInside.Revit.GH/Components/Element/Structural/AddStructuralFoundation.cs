using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using Exceptions;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.9")]
  public class AddStructuralFoundation : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("C1C7CDBB-EE50-40FC-A398-E01465EC65EB");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public AddStructuralFoundation() : base
    (
      name: "Add Structural Foundation",
      nickname: "S-Foundation",
      description: "Given its Location, it adds a structural foundation element to the active Revit document",
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
        new Param_Plane()
        {
          Name = "Location",
          NickName = "L",
          Description = "Structural Foundation location.",
        }
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = "Type",
          NickName = "T",
          Description = "Structural Foundation type.",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_StructuralFoundation
        }
      ),
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Level",
          NickName = "L",
          Description = "Level.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Host",
          NickName = "H",
          Description = "Host element.",
          Optional = true
        }, ParamRelevance.Secondary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.FamilyInstance()
        {
          Name = _Foundation_,
          NickName = _Foundation_.Substring(0, 1),
          Description = $"Output {_Foundation_}",
        }
      )
    };

    const string _Foundation_ = "Foundation";
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

      ReconstructElement<ARDB.FamilyInstance>
      (
        doc.Value, _Foundation_, foundation =>
        {
          var tol = GeometryTolerance.Model;

          // Input
          if (!Params.GetData(DA, "Location", out Plane? location, x => x.IsValid)) return null;
          if (!Parameters.FamilySymbol.GetDataOrDefault(this, DA, "Type", out Types.FamilySymbol type, doc, ARDB.BuiltInCategory.OST_StructuralFoundation)) return null;
          if (!Parameters.Level.GetDataOrDefault(this, DA, "Level", out Types.Level level, doc, location.Value.Origin.Z)) return null;
          if (!Params.TryGetData(DA, "Host", out Types.GraphicalElement host)) return null;

          // Compute
          foundation = Reconstruct
          (
            foundation,
            doc.Value,
            location.Value.Origin.ToXYZ(),
            location.Value.XAxis.ToXYZ(),
            location.Value.YAxis.ToXYZ(),
            type.Value,
            level.Value,
            host?.Value
          );

          DA.SetData(_Foundation_, foundation);
          return foundation;
        }
      );
    }

    bool Reuse
    (
      ARDB.FamilyInstance foundation,
      ARDB.FamilySymbol type,
      ARDB.Level level,
      ARDB.Element host
    )
    {
      if (foundation is null) return false;

      if (!foundation.Host.IsEquivalent(host)) return false;
      if (foundation.LevelId != (level?.Id ?? ARDB.ElementId.InvalidElementId)) return false;
      if (foundation.GetTypeId() != type.Id)
      {
        if (ARDB.Element.IsValidType(foundation.Document, new ARDB.ElementId[] { foundation.Id }, type.Id))
        {
          if (foundation.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            foundation = foundation.Document.GetElement(id) as ARDB.FamilyInstance;
        }
        else return false;
      }

      return true;
    }

    ARDB.FamilyInstance Create(ARDB.Document doc, ARDB.XYZ point, ARDB.FamilySymbol type, ARDB.Level level, ARDB.Element host)
    {
      return doc.IsFamilyDocument ?
        doc.FamilyCreate.NewFamilyInstance
        (
          point,
          type,
          host,
          ARDB.Structure.StructuralType.Footing
        ) :
        doc.Create.NewFamilyInstance
        (
          point,
          type,
          host,
          level,
          ARDB.Structure.StructuralType.Footing
        );
    }

    ARDB.FamilyInstance Reconstruct
    (
      ARDB.FamilyInstance foundation,
      ARDB.Document doc,
      ARDB.XYZ origin,
      ARDB.XYZ basisX,
      ARDB.XYZ basisY,
      ARDB.FamilySymbol type,
      ARDB.Level level,
      ARDB.Element host
    )
    {
      if (!Reuse(foundation, type, level, host))
      {
        foundation = foundation.ReplaceElement
        (
          doc.Create.NewFamilyInstance
          (
            origin,
            type,
            host,
            level,
            ARDB.Structure.StructuralType.Footing
          ),
          ExcludeUniqueProperties
        );

        // We turn off analytical model off by default
        foundation.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL)?.Update(false);
      }

      foundation.get_Parameter(ARDB.BuiltInParameter.INSTANCE_MOVES_WITH_GRID_PARAM)?.Update(false);
      var levelParam = foundation.get_Parameter(ARDB.BuiltInParameter.FAMILY_LEVEL_PARAM);
      if (!levelParam.IsReadOnly) levelParam.Update(level.Id);

      {
        foundation.Document.Regenerate();
        var pinned = foundation.Pinned;
        try
        {
          foundation.Pinned = false;
          foundation.SetLocation(origin, basisX, basisY);
        }
        finally
        {
          foundation.Pinned = true;
        }
      }

      return foundation;
    }
  }
}
