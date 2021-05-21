using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;
using DBX = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class ParameterKeyIdentity : Component
  {
    public override Guid ComponentGuid => new Guid("A80F4919-2387-4C78-BE2B-2F35B2E60298");

    public ParameterKeyIdentity()
    : base("ParameterKey Identity", "Identity", "Decompose a parameter definition", "Revit", "Parameter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ParameterKey(), "ParameterKey", "K", "Parameter key to decompose", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddTextParameter("Name", "N", "Parameter name", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Param_Enum<Types.StorageType>(), "StorageType", "S", "Parameter value type", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Param_Enum<Types.ParameterClass>(), "Class", "C", "Identifies where the parameter is defined", GH_ParamAccess.item);
      manager.AddParameter(new Param_Guid(), "Guid", "ID", "Shared Parameter global identifier", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Types.ParameterKey parameterKey = null;
      if (!DA.GetData("ParameterKey", ref parameterKey))
        return;

      if (parameterKey.Id.TryGetBuiltInParameter(out var builtInParameter))
      {
        DA.SetData("Name", DB.LabelUtils.GetLabelFor(builtInParameter));
        DA.SetData("StorageType", parameterKey.Document?.get_TypeOfStorage(builtInParameter));
        DA.SetData("Class", DBX.ParameterClass.BuiltIn);
        DA.SetData("Guid", null);
      }
      else if (parameterKey.Document?.GetElement(parameterKey.Id) is DB.ParameterElement parameterElement)
      {
        var definition = parameterElement.GetDefinition();

        DA.SetData("Name", definition?.Name);
        DA.SetData("StorageType", definition?.GetDataType().ToStorageType());

        if (parameterElement is DB.SharedParameterElement shared)
        {
          DA.SetData("Class", DBX.ParameterClass.Shared);
          DA.SetData("Guid", shared.GuidValue);
        }
        else
        {
          DA.SetData("Guid", null);

          if (parameterElement is DB.GlobalParameter)
          {
            DA.SetData("Class", DBX.ParameterClass.Global);
          }
          else
          {
            switch (parameterElement.get_Parameter(DB.BuiltInParameter.ELEM_DELETABLE_IN_FAMILY).AsInteger())
            {
              case 0: DA.SetData("Class", DBX.ParameterClass.Family); break;
              case 1: DA.SetData("Class", DBX.ParameterClass.Project); break;
            }
          }
        }
      }
    }
  }
}
