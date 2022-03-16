using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class BasePoint : GraphicalElementT<Types.IGH_BasePoint, ARDB.Element>
  {
    public override GH_Exposure Exposure => GH_Exposure.senary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("16F8DAF7-B63C-4A8B-A2E1-ACA0A08CDCB8");
    protected override string IconTag => "⌖";

    public BasePoint() : base
    (
      name: "Base Point",
      nickname: "Base Point",
      description: "Contains a collection of Revit base point elements",
      category: "Params",
      subcategory: "Revit"
    )
    { }

    #region ISelectionFilter
    public override bool AllowElement(ARDB.Element elem) => Types.Element.FromElement(elem) is Types.IGH_BasePoint;
    #endregion
  }
}
