using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class GroupTypeCreate : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("39E42448-1C1C-4140-BC37-7399ABF82117");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public GroupTypeCreate() : base
    (
      name: "Create GroupType",
      nickname: "GroupType",
      description: "Given a collection of elements, it adds a GroupType to the active Revit document",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "GroupType", "G", "New GroupType", GH_ParamAccess.item);
    }

    void ReconstructGroupTypeCreate
    (
      DB.Document doc,
      ref DB.GroupType groupType,

      IList<DB.Element> elements,
      Optional<string> name
    )
    {
      var elementIds = DB.ElementTransformUtils.CopyElements(doc, elements.Where(x => x.Document.Equals(doc)).Select(x => x.Id).ToList(), DB.XYZ.Zero);

      if (groupType is DB.GroupType oldGroupType)
      {
        // To avoid name conflicts we rename the old GroupType that will be deleted
        oldGroupType.Name = Guid.NewGuid().ToString();

        var newGroup = doc.IsFamilyDocument ?
                        doc.FamilyCreate.NewGroup(elementIds):
                        doc.Create.NewGroup(elementIds);
        groupType = newGroup.GroupType;
        doc.Delete(newGroup.Id);

        // Update other occurrences of oldGroupType
        foreach (var twinGroup in oldGroupType.Groups.Cast<DB.Group>())
          twinGroup.GroupType = groupType;
      }
      else
      {
        var newGroup = doc.IsFamilyDocument ?
                        doc.FamilyCreate.NewGroup(elementIds) :
                        doc.Create.NewGroup(elementIds);
        groupType = newGroup.GroupType;
        doc.Delete(newGroup.Id);
      }

      if (groupType is object && name.HasValue)
      {
        try { groupType.Name = name.Value; }
        catch (Autodesk.Revit.Exceptions.ArgumentException e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{e.Message.Replace($".{Environment.NewLine}", ". ")}");
        }
      }
    }
  }
}
