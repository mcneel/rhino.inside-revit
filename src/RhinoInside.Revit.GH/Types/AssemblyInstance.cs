using System;
using System.Linq;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Assembly")]
  public class AssemblyInstance : GraphicalElement
  {
    protected override Type ValueType => typeof(DB.AssemblyInstance);
    public static explicit operator DB.AssemblyInstance(AssemblyInstance value) => value?.Value;
    public new DB.AssemblyInstance Value => base.Value as DB.AssemblyInstance;

    public AssemblyInstance() { }
    public AssemblyInstance(DB.AssemblyInstance assembly) : base(assembly) { }

    public override Level Level
    {
      get
      {
        if (Value is DB.AssemblyInstance assembly)
          return Types.Level.FromElementId(assembly.Document, assembly.LevelId) as Types.Level;
        return default;
      }
    }
  }
}
