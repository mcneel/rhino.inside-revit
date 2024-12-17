using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.27")]
  public class StructuralInstance : GraphicalElement<Types.IGH_StructuralInstance, ARDB.FamilyInstance>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("54C68928-F012-42B0-94DC-DC9FC00FAE31");
    protected override string IconTag => "SC";

    public StructuralInstance() : base("Structural Component", "Structural Component", "Contains a collection of Revit Structural Component elements", "Params", "Revit Elements") { }

    public override bool AllowElement(ARDB.Element elem) => base.AllowElement(elem) && Types.StructuralInstance.IsValidElement(elem);
  }
}
