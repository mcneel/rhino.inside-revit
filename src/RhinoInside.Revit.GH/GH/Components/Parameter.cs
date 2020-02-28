using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Autodesk.Revit.DB;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components
{
  public class ParameterKeyDecompose : Component
  {
    public override Guid ComponentGuid => new Guid("A80F4919-2387-4C78-BE2B-2F35B2E60298");

    public ParameterKeyDecompose()
    : base("ParameterKey.Decompose", "ParameterKey.Decompose", "Decompose a parameter definition", "Revit", "Parameter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ParameterKey(), "ParameterKey", "K", "Parameter key to decompose", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddTextParameter("Name", "N", "Parameter name", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Param_Enum<Types.StorageType>(), "StorageType", "S", "Parameter value type", GH_ParamAccess.item);
      manager.AddBooleanParameter("Visible", "V", "Parameter is visible in UI", GH_ParamAccess.item);
      manager.AddParameter(new Param_Guid(), "Guid", "ID", "Shared Parameter global identifier", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Types.ParameterKey parameterKey = null;
      if (!DA.GetData("ParameterKey", ref parameterKey))
        return;

      if (parameterKey.Value.TryGetBuiltInParameter(out var builtInParameter))
      {
        DA.SetData("Name", LabelUtils.GetLabelFor(builtInParameter));
        DA.SetData("StorageType", parameterKey.Document?.get_TypeOfStorage(builtInParameter));
        DA.SetData("Visible", true);
        DA.SetData("Guid", null);
      }
      else if (parameterKey.Document?.GetElement(parameterKey.Value) is ParameterElement parameterElement)
      {
        var definition = parameterElement.GetDefinition();
        DA.SetData("Name", definition?.Name);
        DA.SetData("StorageType", definition?.ParameterType.ToStorageType());
        DA.SetData("Visible", definition?.Visible);
        DA.SetData("Guid", (parameterElement as SharedParameterElement)?.GuidValue ?? null);
      }
    }
  }

  public class ParameterValueDecompose : Component
  {
    public override Guid ComponentGuid => new Guid("3BDE5890-FB80-4AF2-B9AC-373661756BDA");

    public ParameterValueDecompose()
    : base("ParameterValue.Decompose", "ParameterValue.Decompose", "Decompose a parameter value", "Revit", "Parameter")
    { }

    protected override ElementFilter ElementFilter => new Autodesk.Revit.DB.ElementClassFilter(typeof(ParameterElement));
    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ParameterValue(), "ParameterValue", "V", "Parameter value to decompose", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Param_Enum<Types.BuiltInParameterGroup>(), "Group", "G", "Parameter group", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Param_Enum<Types.ParameterType>(),"Type", "T", "Parameter type", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Param_Enum<Types.UnitType>(), "Unit", "U", "Unit type", GH_ParamAccess.item);
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

  public class ParameterByName : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("84AB6F3C-BB4B-48E4-9175-B7F40791BB7F");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public ParameterByName() : base
    (
      "AddParameterKey.ByName", "ByName",
      "Given its Name, it adds a Parameter definition to the active Revit document",
      "Revit", "Parameter"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ParameterKey(), "ParameterKey", "K", "New Parameter definition", GH_ParamAccess.item);
    }

    void ReconstructParameterByName
    (
      Document doc,
      ref SharedParameterElement element,

      [Description("Parameter Name")] string name,
      [Description("Overwrite Parameter definition if found"), Optional, DefaultValue(false)] bool overwrite
    )
    {
      var parameterGUID = default(Guid?);
      var parameterType = ParameterType.Text;
      var parameterGroup = BuiltInParameterGroup.PG_DATA;
      bool instance = true;
      bool visible = true;

      using (var bindings = doc.ParameterBindings.ReverseIterator())
      {
        while (bindings.MoveNext())
        {
          if (bindings.Key is InternalDefinition def)
          {
            if
            (
              def.Name == name &&
              def.Visible == visible &&
              def.ParameterType == parameterType &&
              def.ParameterGroup == parameterGroup &&
              (instance ? bindings.Current is InstanceBinding : bindings.Current is TypeBinding)
            )
            {
              if (doc.GetElement(def.Id) is SharedParameterElement parameterElement)
              {
                if (!overwrite)
                {
                  ReplaceElement(ref element, parameterElement);
                  throw new WarningException($"A parameter called \"{name}\" is already in the document");
                }
                parameterGUID = parameterElement.GuidValue;
              }
            }
          }
        }
      }

      using (var defOptions = new ExternalDefinitionCreationOptions(name, parameterType) { Visible = visible })
      {
        if (parameterGUID.HasValue)
          defOptions.GUID = parameterGUID.Value;

        using (var definitionFile = Revit.ActiveUIApplication.Application.CreateSharedParameterFile())
        {
          if (definitionFile?.Groups.Create(LabelUtils.GetLabelFor(parameterGroup)).Definitions.Create(defOptions) is ExternalDefinition definition)
          {
            // TODO : Ask for categories
            using (var categorySet = new CategorySet())
            {
              foreach (var category in doc.Settings.Categories.Cast<Category>().Where(category => category.AllowsBoundParameters))
                categorySet.Insert(category);

              var binding = instance ? (ElementBinding) new InstanceBinding(categorySet) : (ElementBinding) new TypeBinding(categorySet);

              if (!doc.ParameterBindings.Insert(definition, binding, parameterGroup))
              {
                if (!overwrite || !doc.ParameterBindings.ReInsert(definition, binding, parameterGroup))
                  throw new InvalidOperationException("Failed while creating the parameter binding.");
              }
            }

            parameterGUID = definition.GUID;
          }
        }
      }

      ReplaceElement(ref element, SharedParameterElement.Lookup(doc, parameterGUID.Value));
    }
  }
}
