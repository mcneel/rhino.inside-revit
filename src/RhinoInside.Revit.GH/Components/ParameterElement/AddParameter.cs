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
      // Create Temp Shared Parameters File
      if (Core.Host.Services.Value is Autodesk.Revit.ApplicationServices.Application app)
      {
        UserSharedParametersFilename = app.SharedParametersFilename;
        app.SharedParametersFilename = System.IO.Path.GetTempFileName() + ".txt";
        using (System.IO.File.CreateText(app.SharedParametersFilename)) { }
        DefinitionFile = app.OpenSharedParameterFile();
      }

      base.BeforeSolveInstance();
    }

    protected override void AfterSolveInstance()
    {
      base.AfterSolveInstance();

      // Restore User Shared Parameters File
      if (Core.Host.Services.Value is Autodesk.Revit.ApplicationServices.Application app)
      {
        var tempSharedParametersFilename = app.SharedParametersFilename;
        app.SharedParametersFilename = UserSharedParametersFilename;
        UserSharedParametersFilename = default;

        using (DefinitionFile) DefinitionFile = default;

        try { System.IO.File.Delete(tempSharedParametersFilename); }
        finally { }
      }
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.ParameterElement>
      (
        doc.Value, _Parameter_, (parameter) =>
        {
          // Input
          if (!Params.GetData(DA, "Definition", out Types.ParameterKey key, x => x.IsValid)) return null;
          if (!Params.GetData(DA, "Scope", out Types.ParameterScope scope, x => x.IsValid && x.Value != ERDB.ParameterScope.Unknown)) return null;

          if (key.DataType is null)
            throw new Exceptions.RuntimeException($"Unknown data-type for parameter '{key.Nomen}'");

          if (key.Id.TryGetBuiltInParameter(out var _))
            throw new Exceptions.RuntimeWarningException($"Parameter '{key.Nomen}' is a BuiltIn parameter");

          // We can not reuse a parameter on a different scope.
          if (scope.Value != parameter?.GetDefinition()?.GetParameterScope(doc.Value))
            parameter = null;

          // Compute
          StartTransaction(doc.Value);
          if
          (
            CanReconstruct
            (
              _Parameter_, out var untracked, ref parameter,
              doc.Value, key.Nomen,
              (d, n) =>
              {
                return d.TryGetParameter(out var existing, n, scope.Value) &&
                (ERDB.Schemas.DataType) existing.GetDefinition()?.GetDataType() == key.DataType ?
                existing : null;
              }
            )
          )
          {
            parameter = Reconstruct(parameter, doc.Value, key, scope.Value);
          }

          DA.SetData(_Parameter_, parameter);
          return untracked ? null : parameter;
        }
      );
    }

    bool ReuseGlobalParameter
    (
      ARDB.ParameterElement parameter,
      Types.ParameterKey key
    )
    {
      if (!(parameter is ARDB.GlobalParameter)) return false;

      if (parameter.Name != key.Nomen)
        parameter.Name = key.Nomen;

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

      if (parameter.Name != key.Nomen)
      {
        if (parameter is ARDB.SharedParameterElement) return false;
        if (parameter.Document.IsFamilyDocument)
        {
          var familyParameter = parameter.Document.FamilyManager.get_Parameter(parameter.GetDefinition());
          parameter.Document.FamilyManager.RenameParameter(familyParameter, key.Nomen);
        }
        else parameter.Name = key.Nomen;
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
          if (parameter.Name != key.Nomen)
          {
            if (familyParameter.IsShared) return false;
            manager.RenameParameter(familyParameter, key.Nomen);
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
      var parameter = ARDB.GlobalParameter.Create(doc, key.Nomen, key.DataType ?? ERDB.Schemas.SpecType.String.Text);
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
      if (key.CastTo(out ARDB.ExternalDefinitionCreationOptions options))
      {
        using (options)
        {
          if ((ERDB.Schemas.DataType) options.GetDataType() == ERDB.Schemas.DataType.Empty)
            options.SetDataType(ERDB.Schemas.SpecType.String.Text);

          var groupName = key.Group?.LocalizedLabel ?? "Other";
          var group = DefinitionFile.Groups.get_Item(groupName) ??
                      DefinitionFile.Groups.Create(groupName);
          if
          (
            !(group.Definitions.get_Item(key.Nomen) is ARDB.ExternalDefinition definition) ||
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
              if
              (
                FailureProcessingMode != ARDB.FailureProcessingResult.ProceedWithCommit ||
                !doc.ParameterBindings.ReInsert(definition, binding, key.Group)
              )
                throw new Exceptions.RuntimeException
                (
                  $"The shared parameter '{key.GUID}' cannot be added with name '{key.Nomen}' and type '{key.DataType.Label}' " +
                  "because it conflicts with an existing shared parameter."
                );
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
        if (key.CastTo(out ARDB.ExternalDefinitionCreationOptions options))
        {
          using (options)
          {
            var groupName = key.Group?.LocalizedLabel ?? "Group";
            var group = DefinitionFile.Groups.get_Item(groupName) ??
                        DefinitionFile.Groups.Create(groupName);
            if
            (
              !(group.Definitions.get_Item(key.Nomen) is ARDB.ExternalDefinition definition) ||
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
              throw new InvalidOperationException($"{e.Message}{Environment.NewLine}Parameter: {key.Nomen}", e);
            }
          }
        }
      }
      else
      {
        var familyParam = ERDB.Schemas.CategoryId.IsCategoryId(key.DataType, out var categoryId) ?
        doc.FamilyManager.AddParameter
        (
          key.Nomen,
          key.Group,
          ARDB.Category.GetCategory(doc, categoryId),
          parameterScope == ERDB.ParameterScope.Instance
        ) :
        doc.FamilyManager.AddParameter
        (
          key.Nomen,
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
        if (!ARDB.GlobalParametersManager.AreGlobalParametersAllowed(doc))
          throw new Exceptions.RuntimeException("Global parameters are only allowed on project documents.");

        if (!ReuseGlobalParameter(parameter, key))
        {
          parameter?.Document.Delete(parameter.Id);
          parameter = CreateGlobalParameter(doc, key);
        }
      }
      else
      {
        if (doc.IsFamilyDocument)
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
      }

      return parameter;
    }
  }
}
