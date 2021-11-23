using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ElementTypes
{
  using External.DB.Extensions;
  using Kernel.Attributes;

  public class ElementTypeDuplicate : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("5ED7E612-E5C6-4F0E-AA69-814CF2478F7E");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "D";

    public ElementTypeDuplicate() : base
    (
      name: "Duplicate Type",
      nickname: "TypeDup",
      description: "Given a Name, it duplicates an ElementType into the active Revit document",
      category: "Revit",
      subCategory: "Type"
    )
    { }

    void ReconstructElementTypeDuplicate
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [Name("Type"), NickName("T"), Description("New Type")]
      ref ARDB.ElementType elementType,

      ARDB.ElementType type,
      string name
    )
    {
      if
      (
        elementType is ARDB.ElementType &&
        elementType.GetType() == type.GetType() &&
        elementType.FamilyName == type.FamilyName &&
        elementType.Category?.Id == type.Category?.Id
      )
      {
        if (elementType.Name != name)
          elementType.Name = name;

        if (elementType is ARDB.HostObjAttributes hostElementType && type is ARDB.HostObjAttributes hostType)
          hostElementType.SetCompoundStructure(hostType.GetCompoundStructure());

        elementType.CopyParametersFrom(type);
      }
      else
      {
        elementType = type.Duplicate(name);
      }
    }
  }
}
