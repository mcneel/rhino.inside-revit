using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ElementTypes
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.0", updated: "1.5")]
  public class ElementTypeDuplicate : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("5ED7E612-E5C6-4F0E-AA69-814CF2478F7E");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "D";

    public ElementTypeDuplicate() : base
    (
      name: "Duplicate Type",
      nickname: "Duplicate",
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
          Name = "Type Name",
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
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.ElementType>
      (
        doc.Value, _Type_, (type) =>
        {
          // Input
          if (!Params.TryGetData(DA, "Type Name", out string name, x => !string.IsNullOrEmpty(x))) return null;
          if (!Params.GetData(DA, "Type", out Types.ElementType template, x => x.IsValid)) return null;

          // Compute
          StartTransaction(doc.Value);
          if (CanReconstruct(_Type_, out var untracked, ref type, doc.Value, name, template.FamilyName, (ARDB.BuiltInCategory) template.Category.Id.IntegerValue))
            type = Reconstruct(type, doc.Value, name, template.Value);

          DA.SetData(_Type_, type);
          return untracked ? null : type;
        }
      );
    }

    bool Reuse(ARDB.ElementType type, string name, ARDB.ElementType template)
    {
      if (type is null) return false;
      if (type.FamilyName != template.FamilyName) return false;
      if (name is object) { if (type.Name != name) type.Name = name; }
      else type.SetIncrementalNomen(template?.Name ?? _Type_);

      if (type is ARDB.HostObjAttributes hostElementType && type is ARDB.HostObjAttributes hostType)
        hostElementType.SetCompoundStructure(hostType.GetCompoundStructure());

      type.CopyParametersFrom(template, ExcludeUniqueProperties);
      return true;
    }

    ARDB.ElementType Create(ARDB.Document doc, string name, ARDB.ElementType template)
    {
      var type = default(ARDB.ElementType);

      // Make sure the name is unique
      if (name is null)
      {
        name = doc.NextIncrementalNomen
        (
          template?.Name ?? _Type_,
          template.GetType(),
          template.FamilyName,
          template.Category is ARDB.Category category ?
          (ARDB.BuiltInCategory) category.Id.IntegerValue :
          ARDB.BuiltInCategory.INVALID
        );
      }

      // Try to duplicate template
      if (template is object)
      {
        // `View.Duplicate` fails with ARDB.ViewFamilyType
        //if (doc.Equals(template.Document))
        //{
        //  type = template.Duplicate(name);
        //}
        //else
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
