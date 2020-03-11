using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

using RhinoInside.Revit.Exceptions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Documents.Params
{
  public class ParameterValueDecompose : Component
  {
    public override Guid ComponentGuid => new Guid("3BDE5890-FB80-4AF2-B9AC-373661756BDA");

    public ParameterValueDecompose()
    : base("ParameterValue.Decompose", "ParameterValue.Decompose", "Decompose a parameter value", "Revit", "Parameter")
    { }

    protected override DB.ElementFilter ElementFilter => new Autodesk.Revit.DB.ElementClassFilter(typeof(DB.ParameterElement));
    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Documents.Params.ParameterValue(), "ParameterValue", "V", "Parameter value to decompose", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Param_Enum<Types.Documents.Params.BuiltInParameterGroup>(), "Group", "G", "Parameter group", GH_ParamAccess.item);
      manager.AddParameter(new Param_Enum<Types.Documents.Params.ParameterType>(), "Type", "T", "Parameter type", GH_ParamAccess.item);
      manager.AddParameter(new Param_Enum<Types.Documents.Units.UnitType>(), "Unit", "U", "Unit type", GH_ParamAccess.item);
      manager.AddBooleanParameter("IsReadOnly", "R", "Parameter is Read Only", GH_ParamAccess.item);
      manager.AddBooleanParameter("UserModifiable", "U", "Parameter is UserModifiable ", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Autodesk.Revit.DB.Parameter parameter = null;
      if (!DA.GetData("ParameterValue", ref parameter))
        return;

      DA.SetData("Group", parameter?.Definition.ParameterGroup);
      DA.SetData("Type", parameter?.Definition.ParameterType);
      DA.SetData("Unit", parameter?.Definition.UnitType);
      DA.SetData("IsReadOnly", parameter?.IsReadOnly);
      DA.SetData("UserModifiable", parameter?.UserModifiable);
    }
  }
}
