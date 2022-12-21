using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Groups
{
  using External.DB;
  using External.DB.Extensions;
  using Grasshopper.Kernel.Parameters;

  [ComponentVersion(introduced: "1.0", updated: "1.5")]
  public class GroupTypeCreate : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("39E42448-1C1C-4140-BC37-7399ABF82117");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public GroupTypeCreate() : base
    (
      name: "Create Group Type",
      nickname: "GroupType",
      description: "Given a collection of elements, it adds a Group type to the active Revit document",
      category: "Revit",
      subCategory: "Type"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document() { Optional = true }, ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Group Name",
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Elements",
          NickName = "E",
          Description = "Group elements",
          Access = GH_ParamAccess.list
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
          Name = _GroupType_,
          NickName = _GroupType_.Substring(0, 1),
          Description = $"Output {_GroupType_}",
        }
      ),
    };

    const string _GroupType_ = "Group Type";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ALL_MODEL_FAMILY_NAME,
      ARDB.BuiltInParameter.ALL_MODEL_TYPE_NAME,
    };

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Output<IGH_Param>("GroupType") is IGH_Param groupType)
        groupType.Name = "Group Type";

      base.AddedToDocument(document);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.GroupType>
      (
        doc.Value, _GroupType_, (type) =>
        {
          // Input
          if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return null;
          if (!Params.GetDataList(DA, "Elements", out IList<Types.Element> elements)) return null;


          // Compute
          StartTransaction(doc.Value);
          if (CanReconstruct(_GroupType_, out var untracked, ref type, doc.Value, name, categoryId: ARDB.BuiltInCategory.OST_IOSModelGroups))
          {
            var validElements = elements.Where(x => x?.IsValid == true).ToArray();
            var sourceDocuments = validElements.Select(x => x.Document).Distinct().ToArray();
            if (sourceDocuments.Length == 1)
            {
              var elementIds = validElements.Select(x => x.Id).ToArray();
              type = Reconstruct(type, doc.Value, name, sourceDocuments[0], elementIds);
            }
            else type = null;
          }

          DA.SetData(_GroupType_, type);
          return untracked ? null : type;
        }
      );
    }

    bool Reuse(ARDB.GroupType groupType, ARDB.Document sourceDocument, IList<ARDB.ElementId> elementIds)
    {
      if (groupType is object)
        groupType.Name = Guid.NewGuid().ToString();

      return false;
    }

    ARDB.GroupType Create(ARDB.Document doc, ARDB.Document sourceDocument, IList<ARDB.ElementId> elementIds)
    {
      using (var scope = doc.IsEquivalent(sourceDocument) ? null : sourceDocument.RollBackScope())
      {
        var elementIdsCopy = ARDB.ElementTransformUtils.CopyElements(sourceDocument, elementIds, XYZExtension.Zero);

        using (var create = sourceDocument.Create())
        {
          var newGroup = create.NewGroup(elementIdsCopy);

          var groupType = newGroup.GroupType;
          sourceDocument.Delete(newGroup.Id);

          if (scope is object)
          {
            using (var options = new ARDB.CopyPasteOptions())
            {
              options.SetDuplicateTypeNamesAction(ARDB.DuplicateTypeAction.UseDestinationTypes);

              var groupTypeId = ARDB.ElementTransformUtils.CopyElements(sourceDocument, new ARDB.ElementId[] { groupType.Id }, doc, default, options);
              return doc.GetElement(groupTypeId.First()) as ARDB.GroupType;
            }
          }

          return groupType;
        }
      }
    }

    ARDB.GroupType Reconstruct(ARDB.GroupType groupType, ARDB.Document doc, string name, ARDB.Document sourceDocument, IList<ARDB.ElementId> elementIds)
    {
      if (!Reuse(groupType, sourceDocument, elementIds))
      {
        var newGroupType = Create(doc, sourceDocument, elementIds);
        groupType.ReplaceElement(newGroupType, ExcludeUniqueProperties);

        if (groupType is object)
        {
          name = name ?? groupType.Name;
          groupType.Document.Delete(groupType.Id);
        }

        groupType = newGroupType;
      }

      if (name is object && groupType.Name != name)
        groupType.Name = name;

      return groupType;
    }
  }
}
