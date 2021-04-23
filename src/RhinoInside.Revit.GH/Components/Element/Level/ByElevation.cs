using System;
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

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Level(), "Level", "L", "New Level", GH_ParamAccess.item);
    }

    void ReconstructLevelByElevation
    (
      DB.Document doc,
      ref DB.Level element,

      [ParamType(typeof(Parameters.Elevation))]
      double elevation,
      Optional<DB.LevelType> type,
      Optional<string> name
    )
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;
      elevation *= scaleFactor;
      elevation += doc.GetBasePointLocation(Params.Input<Parameters.Elevation>("Elevation").ElevationBase).Z;

      SolveOptionalType(doc, ref type, DB.ElementTypeGroup.LevelType, nameof(type));

      if (element is DB.Level level)
      {
        level.SetHeight(elevation);
      }
      else
      {
        var newLevel = DB.Level.Create
        (
          doc,
          elevation
        );

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

        ReplaceElement(ref element, newLevel, parametersMask);
      }

      ChangeElementTypeId(ref element, type.Value.Id);

      if (name != Optional.Missing && element != null)
      {
        try { element.Name = name.Value; }
        catch (Autodesk.Revit.Exceptions.ArgumentException e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{e.Message.Replace($".{Environment.NewLine}", ". ")}");
        }
      }
    }
  }
}
