using System;
using Autodesk.Revit.DB;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components
{
  public class LevelByElevation : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("C6DEC111-EAC6-4047-8618-28EE144D55C5");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public LevelByElevation() : base
    (
      name: "Add Level",
      nickname: "Level",
      description: "Given its Elevation, it adds a Level to the active Revit document",
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
      Document doc,
      ref Autodesk.Revit.DB.Element element,

      double elevation,
      Optional<Autodesk.Revit.DB.LevelType> type,
      Optional<string> name
    )
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;
      elevation *= scaleFactor;

      SolveOptionalType(ref type, doc, ElementTypeGroup.LevelType, nameof(type));

      if (element is Level level)
      {
        if(level.Elevation != elevation)
          level.Elevation = elevation;
      }
      else
      {
        var newLevel = Level.Create
        (
          doc,
          elevation
        );

        var parametersMask = name.IsMissing ?
          new BuiltInParameter[]
          {
            BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
            BuiltInParameter.ELEM_FAMILY_PARAM,
            BuiltInParameter.ELEM_TYPE_PARAM
          } :
          new BuiltInParameter[]
          {
            BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
            BuiltInParameter.ELEM_FAMILY_PARAM,
            BuiltInParameter.ELEM_TYPE_PARAM,
            BuiltInParameter.DATUM_TEXT
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
