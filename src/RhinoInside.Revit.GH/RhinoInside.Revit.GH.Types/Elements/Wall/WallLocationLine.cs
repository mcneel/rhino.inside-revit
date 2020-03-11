using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types.Elements.Wall
{
  public class WallLocationLine : GH_Enum<DB.WallLocationLine>
  {
    public override string ToString()
    {
      switch (Value)
      {
        case DB.WallLocationLine.WallCenterline: return "Wall Centerline";
        case DB.WallLocationLine.CoreCenterline: return "Core Centerline";
        case DB.WallLocationLine.FinishFaceExterior: return "Finish Face: Exterior";
        case DB.WallLocationLine.FinishFaceInterior: return "Finish Face: Interior";
        case DB.WallLocationLine.CoreExterior: return "Core Face: Exterior";
        case DB.WallLocationLine.CoreInterior: return "Core Face: Interior";
      }

      return base.ToString();
    }
  }
}
