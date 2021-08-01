using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.ElementTracking;
using DB = Autodesk.Revit.DB;
using DBX = RhinoInside.Revit.External.DB;
using DBXS = RhinoInside.Revit.External.DB.Schemas;

namespace RhinoInside.Revit.GH.Components.ParameterElement
{
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
        new Parameters.Param_Enum<Types.ParameterBinding>()
        {
          Name = "Binding",
          NickName = "B",
          Description = "Parameter binding",
        }.
        SetDefaultVale(DBX.ParameterBinding.Instance)
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

    const string _Parameter_ = "Parameter";
    string UserSharedParametersFilename;
    DB.DefinitionFile DefinitionFile;

    protected override void BeforeSolveInstance()
    {
      base.BeforeSolveInstance();

      // Create Temp Shared Parameters File
      if (AddIn.Host.Services.Value is Autodesk.Revit.ApplicationServices.Application app)
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
      if (AddIn.Host.Services.Value is Autodesk.Revit.ApplicationServices.Application app)
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
      if (!Params.GetData(DA, "Binding", out Types.ParameterBinding binding, x => x.IsValid && x.Value != DBX.ParameterBinding.Unknown)) return;

      // Previous Output
      Params.ReadTrackedElement(_Parameter_, doc.Value, out DB.ParameterElement parameter);

      StartTransaction(doc.Value);
      {
        parameter = Reconstruct(parameter, doc.Value, key, binding.Value);

        Params.WriteTrackedElement(_Parameter_, doc.Value, parameter);
        DA.SetData(_Parameter_, parameter);
      }
    }

    bool ReuseGlobalParameter
    (
      DB.ParameterElement parameter,
      Types.ParameterKey key
    )
    {
      if (!(parameter is DB.GlobalParameter)) return false;

      if (parameter.Name != key.Name)
        parameter.Name = key.Name;

      if (parameter.GetDefinition() is DB.InternalDefinition definition)
      {
        if ((DBXS.DataType) definition.GetDataType() != key.DataType) return false;

        if (definition.GetGroupType() != key.Group)
          definition.SetGroupType(key.Group);

        return true;
      }

      return false;
    }

    bool ReuseProjectParameter
    (
      DB.ParameterElement parameter,
      Types.ParameterKey key,
      DBX.ParameterBinding parameterBinding
    )
    {
      if (parameter is null) return false;
      if (parameter is DB.GlobalParameter) return false;

      if (parameter is DB.SharedParameterElement shared)
      {
        if (key.GUID.HasValue && shared.GuidValue != key.GUID.Value)
          return false;
      }

      if (parameter.Name != key.Name)
      {
        if (parameter is DB.SharedParameterElement) return false;
        if (parameter.Document.IsFamilyDocument)
        {
          var familyParameter = parameter.Document.FamilyManager.get_Parameter(parameter.GetDefinition());
          parameter.Document.FamilyManager.RenameParameter(familyParameter, key.Name);
        }
        else parameter.Name = key.Name;
      }

      if (parameter.GetDefinition() is DB.InternalDefinition definition)
      {
        if ((DBXS.DataType) definition.GetDataType() != key.DataType) return false;
        if (definition.GetParameterBinding(parameter.Document) != parameterBinding) return false;

        if (definition.GetGroupType() != key.Group)
          definition.SetGroupType(key.Group);

        return true;
      }

      return false;
    }

    bool ReuseFamilyParameter
    (
      DB.ParameterElement parameter,
      Types.ParameterKey key,
      DBX.ParameterBinding parameterBinding
    )
    {
      if (parameter is null) return false;
      if (parameter is DB.GlobalParameter) return false;

      if (parameter is DB.SharedParameterElement shared)
      {
        if (key.GUID.HasValue && shared.GuidValue != key.GUID.Value)
          return false;
      }

      if (parameter.GetDefinition() is DB.InternalDefinition definition)
      {
        if ((DBXS.DataType) definition.GetDataType() != key.DataType) return false;

        var manager = parameter.Document.FamilyManager;
        if (manager.get_Parameter(definition) is DB.FamilyParameter familyParameter)
        {
          if (parameter.Name != key.Name)
          {
            if (familyParameter.IsShared) return false;
            manager.RenameParameter(familyParameter, key.Name);
          }

          if (parameterBinding == DBX.ParameterBinding.Instance && !familyParameter.IsInstance)
            manager.MakeInstance(familyParameter);

          if (parameterBinding == DBX.ParameterBinding.Type && familyParameter.IsInstance)
            manager.MakeType(familyParameter);

          if (definition.GetGroupType() != key.Group)
            definition.SetGroupType(key.Group);

          return true;
        }
      }

      return false;
    }


    DB.ParameterElement CreateGlobalParameter
    (
      DB.Document doc,
      Types.ParameterKey key
    )
    {
      if(!DB.GlobalParametersManager.AreGlobalParametersAllowed(doc))
        throw new InvalidOperationException("Global parameters are only allowed on project documents.");

      if (!DB.GlobalParametersManager.IsUniqueName(doc, key.Name))
        throw new InvalidOperationException($"A global parameter named '{key.Name}' already exists in the document.");

      var parameter = DB.GlobalParameter.Create(doc, key.Name, key.DataType ?? DBXS.SpecType.String.Text);
      parameter.GetDefinition().SetGroupType(key.Group);
      return parameter;
    }

    DB.ParameterElement CreateProjectParameter
    (
      DB.Document doc,
      Types.ParameterKey key,
      DBX.ParameterBinding parameterBinding
    )
    {
      using (var collector = new DB.FilteredElementCollector(doc))
      {
        if
        (
          collector.OfClass(typeof(DB.ParameterElement)).
          WhereParameterEqualsTo(DB.BuiltInParameter.ELEM_DELETABLE_IN_FAMILY, 1).
          Any(x => !(x is DB.GlobalParameter) && x.Name == key.Name)
        )
          throw new InvalidOperationException($"A project parameter with the name '{key.Name}' is already defined on document '{doc.GetTitle()}'.");
      }

      if (key.CastTo(out DB.ExternalDefinitionCreationOptions options))
      {
        using (options)
        {
          if (options.Type == DB.ParameterType.Invalid)
            options.Type = DB.ParameterType.Text;

          var groupName = key.Group?.LocalizedLabel ?? "Other";
          var group = DefinitionFile.Groups.get_Item(groupName) ??
                      DefinitionFile.Groups.Create(groupName);
          if
          (
            !(group.Definitions.get_Item(key.Name) is DB.ExternalDefinition definition) ||
            definition.GUID != key.GUID
          )
            definition = group.Definitions.Create(options) as DB.ExternalDefinition;

          using (var categorySet = new DB.CategorySet())
          {
            var categories = doc.GetBuiltInCategoriesWithParameters().Select(x => doc.GetCategory(x)).ToList();

            var binding = default(DB.ElementBinding);
            switch (parameterBinding)
            {
              case DBX.ParameterBinding.Instance:
                binding = new DB.InstanceBinding(categorySet);
                break;

              case DBX.ParameterBinding.Type:
                binding = new DB.TypeBinding(categorySet);
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

          return DB.SharedParameterElement.Lookup(doc, definition.GUID);
        }
      }

      return default;
    }

    DB.ParameterElement CreateFamilyParameter
    (
      DB.Document doc,
      Types.ParameterKey key,
      DBX.ParameterBinding parameterBinding
    )
    {
      if (key.GUID.HasValue)
      {
        using (var collector = new DB.FilteredElementCollector(doc))
        {
          if
          (
            collector.OfClass(typeof(DB.ParameterElement)).
            WhereParameterEqualsTo(DB.BuiltInParameter.ELEM_DELETABLE_IN_FAMILY, 0).
            Any(x => !(x is DB.GlobalParameter) && x.Name == key.Name)
          )
            throw new InvalidOperationException($"A family parameter with the name '{key.Name}' is already defined on document '{doc.GetTitle()}'.");
        }

        if (key.CastTo(out DB.ExternalDefinitionCreationOptions options))
        {
          using (options)
          {
            var groupName = key.Group?.LocalizedLabel ?? "Group";
            var group = DefinitionFile.Groups.get_Item(groupName) ??
                        DefinitionFile.Groups.Create(groupName);
            if
            (
              !(group.Definitions.get_Item(key.Name) is DB.ExternalDefinition definition) ||
              definition.GUID != key.GUID
            )
              definition = group.Definitions.Create(options) as DB.ExternalDefinition;

            try
            {
              var familyParam = key.Group is object ?
                doc.FamilyManager.AddParameter(definition, key.Group, parameterBinding == DBX.ParameterBinding.Instance) :
                doc.FamilyManager.AddParameter(definition, DB.BuiltInParameterGroup.INVALID, parameterBinding == DBX.ParameterBinding.Instance);
              return doc.GetElement(familyParam.Id) as DB.ParameterElement;
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
        var familyParam = DBXS.CategoryId.IsCategoryId(key.DataType, out var categoryId) ?
        doc.FamilyManager.AddParameter
        (
          key.Name,
          key.Group,
          DB.Category.GetCategory(doc, categoryId),
          parameterBinding == DBX.ParameterBinding.Instance
        ) :
        doc.FamilyManager.AddParameter
        (
          key.Name,
          key.Group,
          key.DataType,
          parameterBinding == DBX.ParameterBinding.Instance
        );

        doc.FamilyManager.SetDescription(familyParam, key.Description ?? string.Empty);

        return doc.GetElement(familyParam.Id) as DB.ParameterElement;
      }

      return default;
    }

    DB.ParameterElement Reconstruct
    (
      DB.ParameterElement parameter,
      DB.Document doc,
      Types.ParameterKey key,
      DBX.ParameterBinding parameterBinding
    )
    {
      if (parameterBinding == DBX.ParameterBinding.Global)
      {
        if (!ReuseGlobalParameter(parameter, key))
        {
          parameter?.Document.Delete(parameter.Id);
          parameter = CreateGlobalParameter(doc, key);
        }
      }
      else if (doc.IsFamilyDocument)
      {
        if (!ReuseFamilyParameter(parameter, key, parameterBinding))
        {
          parameter?.Document.Delete(parameter.Id);
          parameter = CreateFamilyParameter(doc, key, parameterBinding);
        }
      }
      else
      {
        if (!ReuseProjectParameter(parameter, key, parameterBinding))
        {
          parameter?.Document.Delete(parameter.Id);
          parameter = CreateProjectParameter(doc, key, parameterBinding);
        }
      }

      return parameter;
    }
  }
}
