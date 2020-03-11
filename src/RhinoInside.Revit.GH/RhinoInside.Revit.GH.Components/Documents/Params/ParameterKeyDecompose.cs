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
  public class ParameterKeyDecompose : Component
  {
    public override Guid ComponentGuid => new Guid("A80F4919-2387-4C78-BE2B-2F35B2E60298");

    public ParameterKeyDecompose()
    : base("ParameterKey.Decompose", "ParameterKey.Decompose", "Decompose a parameter definition", "Revit", "Parameter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Documents.Params.ParameterKey(), "ParameterKey", "K", "Parameter key to decompose", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddTextParameter("Name", "N", "Parameter name", GH_ParamAccess.item);
      manager.AddParameter(new Param_Enum<Types.Documents.Params.StorageType>(), "StorageType", "S", "Parameter value type", GH_ParamAccess.item);
      manager.AddBooleanParameter("Visible", "V", "Parameter is visible in UI", GH_ParamAccess.item);
      manager.AddParameter(new Param_Guid(), "Guid", "ID", "Shared Parameter global identifier", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Types.Documents.Params.ParameterKey parameterKey = null;
      if (!DA.GetData("ParameterKey", ref parameterKey))
        return;

      if (parameterKey.Value.TryGetBuiltInParameter(out var builtInParameter))
      {
        DA.SetData("Name", DB.LabelUtils.GetLabelFor(builtInParameter));
        DA.SetData("StorageType", parameterKey.Document?.get_TypeOfStorage(builtInParameter));
        DA.SetData("Visible", true);
        DA.SetData("Guid", null);
      }
      else if (parameterKey.Document?.GetElement(parameterKey.Value) is DB.ParameterElement parameterElement)
      {
        var definition = parameterElement.GetDefinition();
        DA.SetData("Name", definition?.Name);
        DA.SetData("StorageType", definition?.ParameterType.ToStorageType());
        DA.SetData("Visible", definition?.Visible);
        DA.SetData("Guid", (parameterElement as DB.SharedParameterElement)?.GuidValue ?? null);
      }
    }
  }
}
