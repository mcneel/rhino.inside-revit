using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ElementTypes
{
  using External.DB.Extensions;
  using ElementTracking;

  public class ElementTypeDuplicate : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("5ED7E612-E5C6-4F0E-AA69-814CF2478F7E");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "D";

    public ElementTypeDuplicate() : base
    (
      name: "Duplicate Type",
      nickname: "TypeDup",
      description: "Create a Revit type by name",
      category: "Revit",
      subCategory: "Type"
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
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Type Name",
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Template type"
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = _Type_,
          NickName = _Type_.Substring(0, 1),
          Description = $"Output {_Type_}",
        }
      ),
    };

    const string _Type_ = "Type";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ALL_MODEL_FAMILY_NAME,
      ARDB.BuiltInParameter.ALL_MODEL_TYPE_NAME,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return;
      if (!Params.GetData(DA, "Type", out Types.ElementType template, x => x.IsValid)) return;

      // Previous Output
      Params.ReadTrackedElement(_Type_, doc.Value, out ARDB.ElementType type);

      StartTransaction(doc.Value);
      {
        var untracked = Existing(_Type_, doc.Value, ref type, name, template.FamilyName, (ARDB.BuiltInCategory) template.Category.Id.IntegerValue);
        type = Reconstruct(type, doc.Value, name, template.Value);

        Params.WriteTrackedElement(_Type_, doc.Value, untracked ? default : type);
        DA.SetData(_Type_, type);
      }
    }

    bool Reuse(ARDB.ElementType type, string name, ARDB.ElementType template)
    {
      if (type is null) return false;
      if (type.FamilyName != template.FamilyName) return false;
      if (name is object) { if (type.Name != name) type.Name = name; }
      else type.SetIncrementalName(template?.Name ?? _Type_);

      if (type is ARDB.HostObjAttributes hostElementType && type is ARDB.HostObjAttributes hostType)
        hostElementType.SetCompoundStructure(hostType.GetCompoundStructure());

      type.CopyParametersFrom(template, ExcludeUniqueProperties);
      return true;
    }

    ARDB.ElementType Create(ARDB.Document doc, string name, ARDB.ElementType template)
    {
      var type = default(ARDB.ElementType);

      // Make sure the name is unique
      {
        name = doc.NextIncrementalName
        (
          name ?? template?.Name ?? _Type_,
          template.GetType(),
          template.FamilyName,
          (ARDB.BuiltInCategory) template.Category.Id.IntegerValue
        );
      }

      // Try to duplicate template
      if (template is object)
      {
        if (doc.Equals(template.Document))
        {
          type = template.Duplicate(name);
        }
        else
        {
          var ids = ARDB.ElementTransformUtils.CopyElements
          (
            template.Document,
            new ARDB.ElementId[] { template.Id },
            doc, default, default
          );

          type = ids.Select(x => doc.GetElement(x)).OfType<ARDB.ElementType>().FirstOrDefault();
          type.Name = name;
        }
      }

      return type;
    }

    ARDB.ElementType Reconstruct(ARDB.ElementType type, ARDB.Document doc, string name, ARDB.ElementType template)
    {
      if (!Reuse(type, name, template))
      {
        type = type.ReplaceElement
        (
          Create(doc, name, template),
          ExcludeUniqueProperties
        );
      }

      return type;
    }
  }
}
