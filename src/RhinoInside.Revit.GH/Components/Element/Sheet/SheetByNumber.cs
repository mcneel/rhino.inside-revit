using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  public class SheetByNumber : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("704d9c1b-fc56-4407-87cf-720047ae5875");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public SheetByNumber() : base
    (
      name: "Add Sheet",
      nickname: "Sheet",
      description: "Create a new sheet in Revit with given number and name",
      category: "Revit",
      subCategory: "Sheet"
    )
    { }

    void ReconstructSheetByNumber(
      [Optional, NickName("DOC")]   DB.Document document,
      [Description("New Sheet")]    ref DB.ViewSheet sheet,
                                    Optional<string> number,
                                    Optional<string> name,
      [NickName("TitleBlock Type"),
       Description("New Sheet")]    Optional<DB.ElementType> titleblockType)
    {

    }
  }
}
