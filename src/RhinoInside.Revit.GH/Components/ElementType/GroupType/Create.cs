using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ModelElements
{
  using Kernel.Attributes;

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

    void ReconstructGroupTypeCreate
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [Description("New Group Type"), NickName("GT")]
      ref ARDB.GroupType groupType,

      IList<ARDB.Element> elements,
      Optional<string> name
    )
    {
      var elementIds = ARDB.ElementTransformUtils.CopyElements(document, elements.Where(x => x.Document.Equals(document)).Select(x => x.Id).ToList(), ARDB.XYZ.Zero);

      if (groupType is ARDB.GroupType oldGroupType)
      {
        // To avoid name conflicts we rename the old GroupType that will be deleted
        oldGroupType.Name = Guid.NewGuid().ToString();

        var newGroup = document.IsFamilyDocument ?
                        document.FamilyCreate.NewGroup(elementIds):
                        document.Create.NewGroup(elementIds);
        groupType = newGroup.GroupType;
        document.Delete(newGroup.Id);

        // Update other occurrences of oldGroupType
        foreach (var twinGroup in oldGroupType.Groups.Cast<ARDB.Group>())
          twinGroup.GroupType = groupType;
      }
      else
      {
        var newGroup = document.IsFamilyDocument ?
                        document.FamilyCreate.NewGroup(elementIds) :
                        document.Create.NewGroup(elementIds);
        groupType = newGroup.GroupType;
        document.Delete(newGroup.Id);
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
