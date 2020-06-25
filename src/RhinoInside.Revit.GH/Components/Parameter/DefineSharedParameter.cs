using System;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using RhinoInside.Revit.Exceptions;
using RhinoInside.Revit.External.ApplicationServices.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Kernel.Attributes;

  public class DefineSharedParameter : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("84AB6F3C-BB4B-48E4-9175-B7F40791BB7F");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public DefineSharedParameter() : base
    (
      "Define Shared Parameter", "SharedPara" +
      "",
      "Given its Name, it creates a Shared Parameter definition to the active Revit document",
      "Revit", "Parameter"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ParameterKey(), "ParameterKey", "K", "New Parameter definition", GH_ParamAccess.item);
    }

    void ReconstructDefineSharedParameter
    (
      DB.Document doc,
      ref DB.SharedParameterElement element,

      [Description("Parameter Name")] string name,
      [Description("Overwrite Parameter definition if found"), Optional, DefaultValue(false)] bool overwrite
    )
    {
      var parameterGUID = default(Guid?);
      var parameterType = DB.ParameterType.Text;
      var parameterGroup = DB.BuiltInParameterGroup.PG_DATA;
      bool instance = true;
      bool visible = true;

      using (var bindings = doc.ParameterBindings.ReverseIterator())
      {
        while (bindings.MoveNext())
        {
          if (bindings.Key is DB.InternalDefinition def)
          {
            if
            (
              def.Name == name &&
              def.Visible == visible &&
              def.ParameterType == parameterType &&
              def.ParameterGroup == parameterGroup &&
              (instance ? bindings.Current is DB.InstanceBinding : bindings.Current is DB.TypeBinding)
            )
            {
              if (doc.GetElement(def.Id) is DB.SharedParameterElement parameterElement)
              {
                if (!overwrite)
                {
                  ReplaceElement(ref element, parameterElement);
                  throw new CancelException($"A parameter called \"{name}\" is already in the document");
                }
                parameterGUID = parameterElement.GuidValue;
              }
            }
          }
        }
      }

      using (var defOptions = new DB.ExternalDefinitionCreationOptions(name, parameterType) { Visible = visible })
      {
        if (parameterGUID.HasValue)
          defOptions.GUID = parameterGUID.Value;

        using (var definitionFile = Revit.ActiveUIApplication.Application.CreateSharedParameterFile())
        {
          if (definitionFile?.Groups.Create(DB.LabelUtils.GetLabelFor(parameterGroup)).Definitions.Create(defOptions) is DB.ExternalDefinition definition)
          {
            // TODO : Ask for categories
            using (var categorySet = new DB.CategorySet())
            {
              foreach (var category in doc.Settings.Categories.Cast<DB.Category>().Where(category => category.AllowsBoundParameters))
                categorySet.Insert(category);

              var binding = instance ? (DB.ElementBinding) new DB.InstanceBinding(categorySet) : (DB.ElementBinding) new DB.TypeBinding(categorySet);

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

      ReplaceElement(ref element, DB.SharedParameterElement.Lookup(doc, parameterGUID.Value));
    }
  }
}
