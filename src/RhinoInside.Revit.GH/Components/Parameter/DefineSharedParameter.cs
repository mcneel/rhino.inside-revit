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
  using RhinoInside.Revit.External.DB.Extensions;

  public class DefineSharedParameter : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("84AB6F3C-BB4B-48E4-9175-B7F40791BB7F");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public DefineSharedParameter() : base
    (
      name: "Define Shared Parameter",
      nickname: "SharedPara",
      description: "Given its Name, it creates a Shared Parameter definition to the active Revit document",
      category: "Revit",
      subCategory: "Parameter"
    )
    { }

    void ReconstructDefineSharedParameter
    (
      [Optional, NickName("DOC")]
      DB.Document document,

      [Description("New Parameter definition"), NickName("K")]
      ref DB.SharedParameterElement parameterKey,

      [Description("Parameter Name")]
      string name,

      [Description("Overwrite Parameter definition if found"), Optional, DefaultValue(false)]
      bool overwrite
    )
    {
      var parameterGUID = default(Guid?);
      var parameterType = External.DB.Schemas.SpecType.String.Text;
      var parameterGroup = External.DB.Schemas.ParameterGroup.Data;
      bool instance = true;
      bool visible = true;

      using (var bindings = document.ParameterBindings.ReverseIterator())
      {
        while (bindings.MoveNext())
        {
          if (bindings.Key is DB.InternalDefinition def)
          {
            if
            (
              def.Name == name &&
              def.Visible == visible &&
              def.GetDataType() == parameterType &&
              def.GetGroupType() == parameterGroup &&
              (instance ? bindings.Current is DB.InstanceBinding : bindings.Current is DB.TypeBinding)
            )
            {
              if (document.GetElement(def.Id) is DB.SharedParameterElement parameterElement)
              {
                if (!overwrite)
                {
                  ReplaceElement(ref parameterKey, parameterElement);
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
          if (definitionFile?.Groups.Create(parameterGroup.Label).Definitions.Create(defOptions) is DB.ExternalDefinition definition)
          {
            // TODO : Ask for categories
            using (var categorySet = new DB.CategorySet())
            {
              foreach (var category in document.Settings.Categories.Cast<DB.Category>().Where(category => category.AllowsBoundParameters))
                categorySet.Insert(category);

              var binding = instance ? (DB.ElementBinding) new DB.InstanceBinding(categorySet) : (DB.ElementBinding) new DB.TypeBinding(categorySet);

              if (!document.ParameterBindings.Insert(definition, binding, parameterGroup))
              {
                if (!overwrite || !document.ParameterBindings.ReInsert(definition, binding, parameterGroup))
                  throw new InvalidOperationException("Failed while creating the parameter binding.");
              }
            }

            parameterGUID = definition.GUID;
          }
        }
      }

      ReplaceElement(ref parameterKey, DB.SharedParameterElement.Lookup(document, parameterGUID.Value));
    }
  }
}
