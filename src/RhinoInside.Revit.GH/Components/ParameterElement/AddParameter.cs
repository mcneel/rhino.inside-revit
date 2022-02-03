using System;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.ParameterElements
{
  using External.DB.Extensions;
  using ElementTracking;

  [ComponentVersion(introduced: "1.0", updated: "1.4")]
  public class AddParameter : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("84AB6F3C-BB4B-48E4-9175-B7F40791BB7F");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public AddParameter() : base
    (
      name: "Add Parameter",
      nickname: "Add Param",
      description: "Given its Definition, it adds a new Parameter into the Revit document",
      category: "Revit",
      subCategory: "Parameter"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Document",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.ParameterKey()
        {
          Name = "Definition",
          NickName = "D",
          Description = "Parameter definition",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.ParameterScope>()
        {
          Name = "Scope",
          NickName = "S",
          Description = "Parameter scope",
        }.
        SetDefaultVale(ERDB.ParameterScope.Instance)
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.ParameterKey()
        {
          Name = _Parameter_,
          NickName = "K",
          Description = $"Output {_Parameter_}",
        }
      ),
    };

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Input<IGH_Param>("Binding") is IGH_Param binding)
        binding.Name = "Scope";

      base.AddedToDocument(document);
    }

    const string _Parameter_ = "Parameter";
    string UserSharedParametersFilename;
    ARDB.DefinitionFile DefinitionFile;

    protected override void BeforeSolveInstance()
    {
      base.BeforeSolveInstance();

      // Create Temp Shared Parameters File
      if (Core.Host.Services.Value is Autodesk.Revit.ApplicationServices.Application app)
      {
        UserSharedParametersFilename = app.SharedParametersFilename;
        app.SharedParametersFilename = System.IO.Path.GetTempFileName() + ".txt";
        using (System.IO.File.CreateText(app.SharedParametersFilename)) { }
        DefinitionFile = app.OpenSharedParameterFile();
      }
    }

    protected override void AfterSolveInstance()
    {
      // Restore User Shared Parameters File
      if (Core.Host.Services.Value is Autodesk.Revit.ApplicationServices.Application app)
      {
        using (DefinitionFile) DefinitionFile = default;

        var tempSharedParametersFilename = app.SharedParametersFilename;
        app.SharedParametersFilename = UserSharedParametersFilename;
        UserSharedParametersFilename = default;

        try { System.IO.File.Delete(tempSharedParametersFilename); }
        finally { }
      }

      base.AfterSolveInstance();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      if (!Params.GetData(DA, "Definition", out Types.ParameterKey key, x => x.IsValid)) return;
      if (!Params.GetData(DA, "Scope", out Types.ParameterScope scope, x => x.IsValid && x.Value != ERDB.ParameterScope.Unknown)) return;

      if (key.DataType is null)
        throw new Exceptions.RuntimeErrorException($"Unknown data-type for parameter '{key.Name}'");

      if (key.Id.TryGetBuiltInParameter(out var _))
        throw new Exceptions.RuntimeWarningException($"Parameter '{key.Name}' is a BuiltIn parameter");

      // Previous Output
      Params.ReadTrackedElement(_Parameter_, doc.Value, out ARDB.ParameterElement parameter);

      StartTransaction(doc.Value);
      {
        parameter = Reconstruct(parameter, doc.Value, key, scope.Value);

        Params.WriteTrackedElement(_Parameter_, doc.Value, parameter);
        DA.SetData(_Parameter_, parameter);
      }
    }

    bool ReuseGlobalParameter
    (
      ARDB.ParameterElement parameter,
      Types.ParameterKey key
    )
    {
      if (!(parameter is ARDB.GlobalParameter)) return false;

      if (parameter.Name != key.Name)
        parameter.Name = key.Name;

      if (parameter.GetDefinition() is ARDB.InternalDefinition definition)
      {
        if ((ERDB.Schemas.DataType) definition.GetDataType() != key.DataType) return false;

        if (definition.GetGroupType() != key.Group)
          definition.SetGroupType(key.Group);

        return true;
      }

      return false;
    }

    bool ReuseProjectParameter
    (
      ARDB.ParameterElement parameter,
      Types.ParameterKey key,
      ERDB.ParameterScope parameterScope
    )
    {
      if (parameter is null) return false;
      if (parameter is ARDB.GlobalParameter) return false;

      if (parameter is ARDB.SharedParameterElement shared)
      {
        if (key.GUID.HasValue && shared.GuidValue != key.GUID.Value)
          return false;
      }

      if (parameter.Name != key.Name)
      {
        if (parameter is ARDB.SharedParameterElement) return false;
        if (parameter.Document.IsFamilyDocument)
        {
          var familyParameter = parameter.Document.FamilyManager.get_Parameter(parameter.GetDefinition());
          parameter.Document.FamilyManager.RenameParameter(familyParameter, key.Name);
        }
        else parameter.Name = key.Name;
      }

      if (parameter.GetDefinition() is ARDB.InternalDefinition definition)
      {
        if ((ERDB.Schemas.DataType) definition.GetDataType() != key.DataType) return false;
        if (definition.GetParameterScope(parameter.Document) != parameterScope) return false;

        if (definition.GetGroupType() != key.Group)
          definition.SetGroupType(key.Group);

        return true;
      }

      return false;
    }

    bool ReuseFamilyParameter
    (
      ARDB.ParameterElement parameter,
      Types.ParameterKey key,
      ERDB.ParameterScope parameterScope
    )
    {
      if (parameter is null) return false;
      if (parameter is ARDB.GlobalParameter) return false;

      if (parameter is ARDB.SharedParameterElement shared)
      {
        if (key.GUID.HasValue && shared.GuidValue != key.GUID.Value)
          return false;
      }

      if (parameter.GetDefinition() is ARDB.InternalDefinition definition)
      {
        if ((ERDB.Schemas.DataType) definition.GetDataType() != key.DataType) return false;

        var manager = parameter.Document.FamilyManager;
        if (manager.get_Parameter(definition) is ARDB.FamilyParameter familyParameter)
        {
          if (parameter.Name != key.Name)
          {
            if (familyParameter.IsShared) return false;
            manager.RenameParameter(familyParameter, key.Name);
          }

          if (parameterScope == ERDB.ParameterScope.Instance && !familyParameter.IsInstance)
            manager.MakeInstance(familyParameter);

          if (parameterScope == ERDB.ParameterScope.Type && familyParameter.IsInstance)
            manager.MakeType(familyParameter);

          if (definition.GetGroupType() != key.Group)
            definition.SetGroupType(key.Group);

          return true;
        }
      }

      return false;
    }


    ARDB.ParameterElement CreateGlobalParameter
    (
      ARDB.Document doc,
      Types.ParameterKey key
    )
    {
      if(!ARDB.GlobalParametersManager.AreGlobalParametersAllowed(doc))
        throw new InvalidOperationException("Global parameters are only allowed on project documents.");

      if (!ARDB.GlobalParametersManager.IsUniqueName(doc, key.Name))
        throw new InvalidOperationException($"A global parameter named '{key.Name}' already exists in the document.");

      var parameter = ARDB.GlobalParameter.Create(doc, key.Name, key.DataType ?? ERDB.Schemas.SpecType.String.Text);
      parameter.GetDefinition().SetGroupType(key.Group);
      return parameter;
    }

    ARDB.ParameterElement CreateProjectParameter
    (
      ARDB.Document doc,
      Types.ParameterKey key,
      ERDB.ParameterScope parameterScope
    )
    {
      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        if
        (
          collector.OfClass(typeof(ARDB.ParameterElement)).
          WhereParameterEqualsTo(ARDB.BuiltInParameter.ELEM_DELETABLE_IN_FAMILY, 1).
          Any(x => !(x is ARDB.GlobalParameter) && x.Name == key.Name)
        )
          throw new InvalidOperationException($"A project parameter with the name '{key.Name}' is already defined on document '{doc.GetTitle()}'.");
      }

      if (key.CastTo(out ARDB.ExternalDefinitionCreationOptions options))
      {
        using (options)
        {
          if (options.Type == ARDB.ParameterType.Invalid)
            options.Type = ARDB.ParameterType.Text;

          var groupName = key.Group?.LocalizedLabel ?? "Other";
          var group = DefinitionFile.Groups.get_Item(groupName) ??
                      DefinitionFile.Groups.Create(groupName);
          if
          (
            !(group.Definitions.get_Item(key.Name) is ARDB.ExternalDefinition definition) ||
            definition.GUID != key.GUID
          )
            definition = group.Definitions.Create(options) as ARDB.ExternalDefinition;

          using (var categorySet = new ARDB.CategorySet())
          {
            var categories = doc.GetBuiltInCategoriesWithParameters().Select(x => doc.GetCategory(x)).ToList();

            var binding = default(ARDB.ElementBinding);
            switch (parameterScope)
            {
              case ERDB.ParameterScope.Instance:
                binding = new ARDB.InstanceBinding(categorySet);
                break;

              case ERDB.ParameterScope.Type:
                binding = new ARDB.TypeBinding(categorySet);
                break;
            }

            foreach (var category in categories)
              binding.Categories.Insert(category);

            if (!doc.ParameterBindings.Insert(definition, binding, key.Group))
            {
              if (!doc.ParameterBindings.ReInsert(definition, binding, key.Group))
                throw new InvalidOperationException("Failed while creating the parameter binding.");
            }
          }

          return ARDB.SharedParameterElement.Lookup(doc, definition.GUID);
        }
      }

      return default;
    }

    ARDB.ParameterElement CreateFamilyParameter
    (
      ARDB.Document doc,
      Types.ParameterKey key,
      ERDB.ParameterScope parameterScope
    )
    {
      if (key.GUID.HasValue)
      {
        using (var collector = new ARDB.FilteredElementCollector(doc))
        {
          if
          (
            collector.OfClass(typeof(ARDB.ParameterElement)).
            WhereParameterEqualsTo(ARDB.BuiltInParameter.ELEM_DELETABLE_IN_FAMILY, 0).
            Any(x => !(x is ARDB.GlobalParameter) && x.Name == key.Name)
          )
            throw new InvalidOperationException($"A family parameter with the name '{key.Name}' is already defined on document '{doc.GetTitle()}'.");
        }

        if (key.CastTo(out ARDB.ExternalDefinitionCreationOptions options))
        {
          using (options)
          {
            var groupName = key.Group?.LocalizedLabel ?? "Group";
            var group = DefinitionFile.Groups.get_Item(groupName) ??
                        DefinitionFile.Groups.Create(groupName);
            if
            (
              !(group.Definitions.get_Item(key.Name) is ARDB.ExternalDefinition definition) ||
              definition.GUID != key.GUID
            )
              definition = group.Definitions.Create(options) as ARDB.ExternalDefinition;

            try
            {
              var familyParam = key.Group is object ?
                doc.FamilyManager.AddParameter(definition, key.Group, parameterScope == ERDB.ParameterScope.Instance) :
                doc.FamilyManager.AddParameter(definition, ARDB.BuiltInParameterGroup.INVALID, parameterScope == ERDB.ParameterScope.Instance);
              return doc.GetElement(familyParam.Id) as ARDB.ParameterElement;
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException e)
            {
              throw new InvalidOperationException($"{e.Message}{Environment.NewLine}Parameter: {key.Name}", e);
            }
          }
        }
      }
      else
      {
        var familyParam = ERDB.Schemas.CategoryId.IsCategoryId(key.DataType, out var categoryId) ?
        doc.FamilyManager.AddParameter
        (
          key.Name,
          key.Group,
          ARDB.Category.GetCategory(doc, categoryId),
          parameterScope == ERDB.ParameterScope.Instance
        ) :
        doc.FamilyManager.AddParameter
        (
          key.Name,
          key.Group,
          key.DataType,
          parameterScope == ERDB.ParameterScope.Instance
        );

        doc.FamilyManager.SetDescription(familyParam, key.Description ?? string.Empty);

        return doc.GetElement(familyParam.Id) as ARDB.ParameterElement;
      }

      return default;
    }

    ARDB.ParameterElement Reconstruct
    (
      ARDB.ParameterElement parameter,
      ARDB.Document doc,
      Types.ParameterKey key,
      ERDB.ParameterScope parameterScope
    )
    {
      if (parameterScope == ERDB.ParameterScope.Global)
      {
        if (!ReuseGlobalParameter(parameter, key))
        {
          parameter?.Document.Delete(parameter.Id);
          parameter = CreateGlobalParameter(doc, key);
        }
      }
      else if (doc.IsFamilyDocument)
      {
        if (!ReuseFamilyParameter(parameter, key, parameterScope))
        {
          parameter?.Document.Delete(parameter.Id);
          parameter = CreateFamilyParameter(doc, key, parameterScope);
        }
      }
      else
      {
        if (!ReuseProjectParameter(parameter, key, parameterScope))
        {
          parameter?.Document.Delete(parameter.Id);
          parameter = CreateProjectParameter(doc, key, parameterScope);
        }
      }

      return parameter;
    }
  }
}
