using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.14")]
  public class BeamSystem : GraphicalElement<Types.BeamSystem, ARDB.BeamSystem>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("45CD3655-4658-4C35-99FB-9975A1128DDF");

    public BeamSystem() : base("Beam System", "Beam System", "Contains a collection of Revit Beam System elements", "Params", "Revit Elements") { }

  }
}
