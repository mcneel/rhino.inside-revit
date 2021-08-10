using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.ElementTracking;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Level
{
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
        new Parameters.ElementType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Level Type",
          Optional = true,
          SelectedBuiltInCategory = DB.BuiltInCategory.OST_Levels
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Level Name",
          Optional = true,
        }
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
    static readonly DB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      DB.BuiltInParameter.ELEM_FAMILY_PARAM,
      DB.BuiltInParameter.ELEM_TYPE_PARAM,
      DB.BuiltInParameter.DATUM_TEXT,
      DB.BuiltInParameter.LEVEL_ELEV,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;
      if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out DB.LevelType type, doc, DB.ElementTypeGroup.LevelType)) return;
      if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return;
      if (!Parameters.Elevation.GetData(this, DA, "Elevation", out var height, doc)) return;
      Params.TryGetData(DA, "Template", out DB.Level template);

      // Previous Output
      Params.ReadTrackedElement(_Level_, doc.Value, out DB.Level level);

      StartTransaction(doc.Value);
      {
        level = Reconstruct(level, doc.Value, height / Revit.ModelUnits, type, name, template);

        Params.WriteTrackedElement(_Level_, doc.Value, level);
        DA.SetData(_Level_, level);
      }
    }

    bool Reuse(DB.Level level, double height, DB.LevelType type, DB.Level template)
    {
      if (level is null) return false;
      if (level.GetHeight() != height) level.SetHeight(height);
      if (type is object && level.GetTypeId() != type.Id) level.ChangeTypeId(type.Id);
      level.CopyParametersFrom(template, ExcludeUniqueProperties);
      return true;
    }

    DB.Level Create(DB.Document doc, double height, DB.LevelType type, DB.Level template)
    {
      var level = default(DB.Level);

      // Try to duplicate template
      if (template is object)
      {
        var ids = DB.ElementTransformUtils.CopyElements
        (
          template.Document,
          new DB.ElementId[] { template.Id },
          doc,
          default,
          default
        );

        level = ids.Select(x => doc.GetElement(x)).OfType<DB.Level>().FirstOrDefault();
      }

      if (level is null)
        level = DB.Level.Create(doc, height);
      else
        level.SetHeight(height);

      if (type is object && type.Id != level.GetTypeId())
        level.ChangeTypeId(type.Id);

      return level;
    }

    DB.Level Reconstruct(DB.Level level, DB.Document doc, double height, DB.LevelType type, string name, DB.Level template)
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
