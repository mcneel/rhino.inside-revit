using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Assembly")]
  public class AssemblyInstance : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.AssemblyInstance);
    public new ARDB.AssemblyInstance Value => base.Value as ARDB.AssemblyInstance;

    public AssemblyInstance() { }
    public AssemblyInstance(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public AssemblyInstance(ARDB.AssemblyInstance assembly) : base(assembly) { }
  }
}
