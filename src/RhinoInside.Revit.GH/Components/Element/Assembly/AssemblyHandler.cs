using System;
using System.Linq;
using System.Collections.Generic;

using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Assemblies
{
  class AssemblyHandler
  {
    public ARDB.ElementId CategoryId { get; }
    public ICollection<ARDB.ElementId> Members { get; }
    public string Name { get; set; }
    public ARDB.AssemblyInstance Template { get; set; }

    public AssemblyHandler(List<ARDB.ElementId> memberIds, ARDB.ElementId categoryId)
    {
      Members = memberIds;
      CategoryId = categoryId;
    }

    public AssemblyHandler(List<ARDB.Element> members, ARDB.Category category = null)
      : this(
          members.Select(m => m.Id).ToList(),
          category?.Id ?? ARDB.ElementId.InvalidElementId
          )
    {
      if (CategoryId.Equals(ARDB.ElementId.InvalidElementId)
          && members.Any()
          && members.First().Category is ARDB.Category firstMemberCategory
          )
        CategoryId = firstMemberCategory.Id;
    }

    public ARDB.AssemblyInstance CreateAssembly(ARDB.Document doc)
    {
      var assembly = ARDB.AssemblyInstance.Create(doc, Members, CategoryId);
      SetName(assembly, Name);
      return assembly;
    }

    public void UpdateAssembly(ARDB.AssemblyInstance assembly)
    {
      UpdateAssemblyMembers(assembly);

      // set the new naming category (this must be set after setting the new member list)
      if (ARDB.AssemblyInstance.IsValidNamingCategory(assembly.Document, CategoryId, assembly.GetMemberIds()))
        assembly.NamingCategoryId = CategoryId;
      else
        throw new Exception("Naming Category is not valid. At least one member must be of given category.");

      SetName(assembly, Name);
    }

    public void UpdateAssemblyMembers(ARDB.AssemblyInstance assembly)
    {
      // set the assembly members to a new list. previous is cleared
      if (ARDB.AssemblyInstance.AreElementsValidForAssembly(assembly.Document, Members, assembly.Id))
        assembly.SetMemberIds(Members);
      else
        throw new Exception("At least one element is not valid to be a memeber of this assembly.");
    }

    public void ExpireAssembly(ARDB.AssemblyInstance assembly)
    {
      // clear all existing members so they can be member of a new assembly
      assembly.SetMemberIds(new List<ARDB.ElementId>());
      SetName(assembly, assembly.UniqueId);
    }

    void SetName(ARDB.AssemblyInstance assembly, string name)
    {
      // assembly instances do not support name assignment,
      // they are named by their type
      if (Name is string
        && assembly.Document.GetElement(assembly.GetTypeId()) is ARDB.ElementType type)
        type.Name = name;
    }
  }
}
