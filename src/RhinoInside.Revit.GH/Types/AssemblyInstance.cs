using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Assembly")]
  public class AssemblyInstance : GraphicalElement
  {
    protected override Type ValueType => typeof(DB.AssemblyInstance);
    public new DB.AssemblyInstance Value => base.Value as DB.AssemblyInstance;

    public AssemblyInstance() { }
    public AssemblyInstance(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public AssemblyInstance(DB.AssemblyInstance assembly) : base(assembly) { }
  }
}
