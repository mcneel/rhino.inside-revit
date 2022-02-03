using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ModelElements
{
  using ElementTracking;
  using External.DB.Extensions;
  using Grasshopper.Kernel.Parameters;

  [ComponentVersion(introduced: "1.0", updated: "1.5")]
  public class GroupTypeCreate : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("39E42448-1C1C-4140-BC37-7399ABF82117");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public GroupTypeCreate() : base
    (
      name: "Create Group Type",
      nickname: "GroupType",
      description: "Given a collection of elements, it adds a Group type to the active Revit document",
      category: "Revit",
      subCategory: "Model"
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
      // Input
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return;
      if (!Params.GetDataList(DA, "Elements", out IList<Types.Element> elementIds)) return;

      // Previous Output
      Params.ReadTrackedElement(_GroupType_, doc.Value, out ARDB.GroupType type);

      StartTransaction(doc.Value);
      {
        var untracked = Existing(_GroupType_, doc.Value, ref type, name, categoryId: ARDB.BuiltInCategory.OST_IOSModelGroups);
        type = Reconstruct(type, doc.Value, name, elementIds.Select(x => x.Id).ToList());

        Params.WriteTrackedElement(_GroupType_, doc.Value, untracked ? default : type);
        DA.SetData(_GroupType_, type);
      }
    }

    bool Reuse(ARDB.GroupType groupType, IList<ARDB.ElementId> elementIds)
    {
      return false;
    }

    ARDB.GroupType Create(ARDB.Document doc, IList<ARDB.ElementId> elementIds)
    {
      var elementIdsCopy = ARDB.ElementTransformUtils.CopyElements(doc, elementIds, ARDB.XYZ.Zero);
      var newGroup = doc.IsFamilyDocument ?
                     doc.FamilyCreate.NewGroup(elementIdsCopy) :
                     doc.Create.NewGroup(elementIdsCopy);
      var groupType = newGroup.GroupType;
      doc.Delete(newGroup.Id);

      return groupType;
    }

    ARDB.GroupType Reconstruct(ARDB.GroupType groupType, ARDB.Document doc, string name, IList<ARDB.ElementId> elementIds)
    {
      if (!Reuse(groupType, elementIds))
      {
        var newGroupType = Create(doc, elementIds);
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
