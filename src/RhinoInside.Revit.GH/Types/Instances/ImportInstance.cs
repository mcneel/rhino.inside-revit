using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Import Symbol")]
  public class ImportInstance : Instance
  {
    protected override Type ValueType => typeof(ARDB.ImportInstance);
    public new ARDB.ImportInstance Value => base.Value as ARDB.ImportInstance;

    public ImportInstance() { }
    public ImportInstance(ARDB.ImportInstance instance) : base(instance) { }
  }
}
