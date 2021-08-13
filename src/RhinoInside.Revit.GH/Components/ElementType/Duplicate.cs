using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
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
      DB.Document document,

      [Name("Type"), NickName("T"), Description("New Type")]
      ref DB.ElementType elementType,

      DB.ElementType type,
      string name
    )
    {
      if
      (
        elementType is DB.ElementType &&
        elementType.Category.Id == type.Category.Id &&
        elementType.FamilyName == type.FamilyName &&
        elementType.GetType() == type.GetType()
      )
      {
        if (elementType.Name != name)
          elementType.Name = name;

        if (elementType is DB.HostObjAttributes hostElementType && type is DB.HostObjAttributes hostType)
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
