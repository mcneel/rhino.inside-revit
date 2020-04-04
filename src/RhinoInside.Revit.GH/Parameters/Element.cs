using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters
{
  public class Element : ElementIdWithoutPreviewParam<Types.Element, object>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary;
    public override Guid ComponentGuid => new Guid("F3EA4A9C-B24F-4587-A358-6A7E6D8C028B");

    public Element() : base("Element", "Element", "Represents a Revit document element.", "Params", "Revit") { }

    protected override Types.Element PreferredCast(object data) => Types.Element.FromValue(data);
  }
}
