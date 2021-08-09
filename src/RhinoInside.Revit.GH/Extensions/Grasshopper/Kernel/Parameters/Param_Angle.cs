using System;

namespace Grasshopper.Kernel.Parameters
{
  public class Param_Angle : Param_Number
  {
    public override Guid ComponentGuid => new Guid("A909509E-6F5C-43E3-A26D-ABC86DB797B8");
    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public Param_Angle()
    {
      Name = "Angle";
      NickName = "Angle";
      Description = "Contains a collection of angles values.";
      Category = "Params";
      SubCategory = "Primitive";
      AngleParameter = true;
    }
  }
}
