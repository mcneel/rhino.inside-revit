using System;
using DB = Autodesk.Revit.DB;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Elements.Level
{
  public class LevelByElevation : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("C6DEC111-EAC6-4047-8618-28EE144D55C5");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public LevelByElevation() : base
    (
      "AddLevel.ByElevation", "ByElevation",
      "Given its Elevation, it adds a Level to the active Revit document",
      "Revit", "Datum"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Elements.Level.Level(), "Level", "L", "New Level", GH_ParamAccess.item);
    }

    void ReconstructLevelByElevation
    (
      DB.Document doc,
      ref Autodesk.Revit.DB.Element element,

      double elevation,
      Optional<Autodesk.Revit.DB.LevelType> type,
      Optional<string> name
    )
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;
      elevation *= scaleFactor;

      SolveOptionalType(ref type, doc, DB.ElementTypeGroup.LevelType, nameof(type));

      if (element is DB.Level level)
      {
        if(level.Elevation != elevation)
          level.Elevation = elevation;
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
