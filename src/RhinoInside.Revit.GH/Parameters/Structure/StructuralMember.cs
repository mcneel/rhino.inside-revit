using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.27")]
  public class StructuralMember : GraphicalElement<Types.IGH_StructuralMember, ARDB.FamilyInstance>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("54C68928-F012-42B0-94DC-DC9FC00FAE31");
    protected override string IconTag => "SM";

    public StructuralMember() : base("Structural Member", "Structural Member", "Contains a collection of Revit Structural Framing elements", "Params", "Revit Elements") { }

    public override bool AllowElement(ARDB.Element elem) => base.AllowElement(elem) && Types.StructuralMember.IsValidElement(elem);
  }
}
