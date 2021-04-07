using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Units;
using DB = Autodesk.Revit.DB;
using DBX = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DeconstructParameterValue : Component
  {
    public override Guid ComponentGuid => new Guid("3BDE5890-FB80-4AF2-B9AC-373661756BDA");

    public DeconstructParameterValue()
    : base("Deconstruct ParameterValue", "Deconstruct", "Decompose a parameter value", "Revit", "Parameter")
    { }

    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.ParameterElement));
    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ParameterValue(), "ParameterValue", "V", "Parameter value to decompose", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Param_Enum<Types.BuiltInParameterGroup>(), "Group", "G", "Parameter group", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Param_Enum<Types.ParameterType>(), "Type", "T", "Parameter type", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Param_Enum<Types.ParameterBinding>(), "Binding", "B", "Parameter binding", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Param_Enum<Types.UnitType>(), "Unit", "U", "Unit type", GH_ParamAccess.item);
      manager.AddBooleanParameter("Is Read Only", "R", "Parameter is Read Only", GH_ParamAccess.item);
      manager.AddBooleanParameter("User Modifiable", "U", "Parameter is UserModifiable ", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Parameter parameter = null;
      if (!DA.GetData("ParameterValue", ref parameter))
        return;

      DA.SetData("Group", parameter?.Definition.ParameterGroup);
      DA.SetData("Type", parameter?.Definition.ParameterType);
      if (parameter?.Element is DB.ElementType) DA.SetData("Binding", DBX.ParameterBinding.Type);
      else if (parameter?.Element is DB.Element) DA.SetData("Binding", DBX.ParameterBinding.Instance);
      else DA.SetData("Binding", null);
      DA.SetData("Unit", parameter?.Definition.GetUnitType());
      DA.SetData("Is Read Only", parameter?.IsReadOnly);
      DA.SetData("User Modifiable", parameter?.UserModifiable);
    }
  }
}
