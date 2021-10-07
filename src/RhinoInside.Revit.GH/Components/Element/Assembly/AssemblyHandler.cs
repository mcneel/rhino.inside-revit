using System;
using System.Linq;
using System.Collections.Generic;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Assembly
{
  class AssemblyHandler
  {
    public DB.ElementId CategoryId { get; }
    public ICollection<DB.ElementId> Members { get; }
    public string Name { get; set; }
    public DB.AssemblyInstance Template { get; set; }

    public AssemblyHandler(DB.ElementId categoryId, List<DB.ElementId> memberIds)
    {
      CategoryId = categoryId;
      Members = memberIds;
    }

    public AssemblyHandler(DB.Category category, List<DB.Element> members)
    {
      CategoryId = category.Id;
      Members = members.Select(e => e.Id).ToList();
    }

    public DB.AssemblyInstance CreateAssembly(DB.Document doc)
    {
      var assembly = DB.AssemblyInstance.Create(doc, Members, CategoryId);
      SetName(assembly, Name);
      return assembly;
    }

    public void UpdateAssembly(DB.AssemblyInstance assembly)
    {
      UpdateAssemblyMembers(assembly);

      // set the new naming category (this must be set after setting the new member list)
      if (DB.AssemblyInstance.IsValidNamingCategory(assembly.Document, CategoryId, assembly.GetMemberIds()))
        assembly.NamingCategoryId = CategoryId;
      else
        throw new Exception("Naming Category is not valid. At least one member must be of given category.");

      SetName(assembly, Name);
    }

    public void UpdateAssemblyMembers(DB.AssemblyInstance assembly)
    {
      // set the assembly members to a new list. previous is cleared
      if (DB.AssemblyInstance.AreElementsValidForAssembly(assembly.Document, Members, assembly.Id))
        assembly.SetMemberIds(Members);
      else
        throw new Exception("At least one element is not valid to be a memeber of this assembly.");
    }

    public void ExpireAssembly(DB.AssemblyInstance assembly)
    {
      // clear all existing members so they can be member of a new assembly
      assembly.SetMemberIds(new List<DB.ElementId>());
      SetName(assembly, assembly.UniqueId);
    }

    void SetName(DB.AssemblyInstance assembly, string name)
    {
      // assembly instances do not support name assignment,
      // they are named by their type
      if (Name is string
        && assembly.Document.GetElement(assembly.GetTypeId()) is DB.Element type)
        type.Name = name;
    }
  }
}
