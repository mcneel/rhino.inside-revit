using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Level
{
  public class LevelByElevation : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("C6DEC111-EAC6-4047-8618-28EE144D55C5");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public LevelByElevation() : base
    (
      name: "Add Level",
      nickname: "Level",
      description: "Given its elevation, it adds a Level to the active Revit document",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    void ReconstructLevelByElevation
    (
      [Optional, NickName("DOC")]
      DB.Document document,

      [Description("New Level")]
      ref DB.Level level,

      [ParamType(typeof(Parameters.Elevation))]
      double elevation,
      Optional<DB.LevelType> type,
      Optional<string> name
    )
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;
      elevation *= scaleFactor;
      elevation += document.GetBasePointLocation(Params.Input<Parameters.Elevation>("Elevation").ElevationBase).Z;

      SolveOptionalType(document, ref type, DB.ElementTypeGroup.LevelType, nameof(type));

      if (level is object)
      {
        level.SetHeight(elevation);
      }
      else
      {
        var newLevel = DB.Level.Create(document, elevation);

        var parametersMask = name.IsMissing ?
          new DB.BuiltInParameter[]
          {
            DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
            DB.BuiltInParameter.ELEM_FAMILY_PARAM,
            DB.BuiltInParameter.ELEM_TYPE_PARAM
          } :
          new DB.BuiltInParameter[]
          {
            DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
            DB.BuiltInParameter.ELEM_FAMILY_PARAM,
            DB.BuiltInParameter.ELEM_TYPE_PARAM,
            DB.BuiltInParameter.DATUM_TEXT
          };

        ReplaceElement(ref level, newLevel, parametersMask);
      }

      ChangeElementTypeId(ref level, type.Value.Id);

      if (name != Optional.Missing && level is object)
      {
        try { level.Name = name.Value; }
        catch (Autodesk.Revit.Exceptions.ArgumentException e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{e.Message.Replace($".{Environment.NewLine}", ". ")}");
        }
      }
    }
  }
}
