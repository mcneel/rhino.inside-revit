using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Levels
{
  using External.DB.Extensions;
  using ElementTracking;

  public class LevelByElevation : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("C6DEC111-EAC6-4047-8618-28EE144D55C5");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public LevelByElevation() : base
    (
      name: "Add Level",
      nickname: "Level",
      description: "Given its elevation, it adds a Level to the current Revit document",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document() { Optional = true }, ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Parameters.Elevation()
        {
          Name = "Elevation",
          NickName = "E",
          Description = "Level Elevation",
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Level Name",
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Level Type",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_Levels
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Template",
          NickName = "T",
          Description = "Template Level",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = _Level_,
          NickName = _Level_.Substring(0, 1),
          Description = $"Output {_Level_}",
        }
      ),
    };

    const string _Level_ = "Level";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.DATUM_TEXT,
      ARDB.BuiltInParameter.LEVEL_ELEV,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.Level>
      (
        doc.Value, _Level_, (level) =>
        {
          // Input
          if (!Parameters.Elevation.GetData(this, DA, "Elevation", out var height, doc)) return null;
          if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return null;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out ARDB.LevelType type, doc, ARDB.ElementTypeGroup.LevelType)) return null;
          Params.TryGetData(DA, "Template", out ARDB.Level template);

          // Compute
          StartTransaction(doc.Value);
          if (CanReconstruct(_Level_, out var untracked, ref level, doc.Value, name, categoryId: ARDB.BuiltInCategory.OST_Levels))
            level = Reconstruct(level, doc.Value, height / Revit.ModelUnits, type, name, template);

          DA.SetData(_Level_, level);
          return untracked ? null : level;
        }
      );
    }

    bool Reuse(ARDB.Level level, double height, ARDB.LevelType type, ARDB.Level template)
    {
      if (level is null) return false;
      if (level.GetElevation() != height) level.SetElevation(height);
      if (type is object && level.GetTypeId() != type.Id) level.ChangeTypeId(type.Id);
      level.CopyParametersFrom(template, ExcludeUniqueProperties);
      return true;
    }

    ARDB.Level Create(ARDB.Document doc, double height, ARDB.LevelType type, ARDB.Level template)
    {
      var level = default(ARDB.Level);

      // Try to duplicate template
      if (template is object)
      {
        level = template.CloneElement(doc);
        level?.SetElevation(height);
      }

      // Else create a brand new
      if (level is null)
      {
        level = ARDB.Level.Create(doc, height);
        level.CopyParametersFrom(template, ExcludeUniqueProperties);
      }

      if (type is object) level.ChangeTypeId(type.Id);

      return level;
    }

    ARDB.Level Reconstruct(ARDB.Level level, ARDB.Document doc, double height, ARDB.LevelType type, string name, ARDB.Level template)
    {
      if (!Reuse(level, height, type, template))
      {
        level = level.ReplaceElement
        (
          Create(doc, height, type, template),
          ExcludeUniqueProperties
        );
      }

      if (name is object && level.Name != name)
        level.Name = name;

      return level;
    }
  }
}
