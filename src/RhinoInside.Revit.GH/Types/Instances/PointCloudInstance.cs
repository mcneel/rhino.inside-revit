using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Point Cloud")]
  public class PointCloudInstance : Instance
  {
    protected override Type ValueType => typeof(ARDB.PointCloudInstance);
    public new ARDB.PointCloudInstance Value => base.Value as ARDB.PointCloudInstance;

    public PointCloudInstance() { }
    public PointCloudInstance(ARDB.PointCloudInstance instance) : base(instance) { }
  }
}
