using System;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
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

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.AssemblyInstance instance)
        {
          using (var transform = instance.GetTransform())
            return new Plane(transform.Origin.ToPoint3d(), transform.BasisX.ToVector3d(), transform.BasisY.ToVector3d());
        }

        return NaN.Plane;
      }
      set
      {
        if (Value is ARDB.AssemblyInstance instance && value.IsValid && value != Location)
        {
          using(var transform = Transform.PlaneToPlane(Plane.WorldXY, value).ToTransform())
            instance.SetTransform(transform);

          InvalidateGraphics();
        }
      }
    }
  }
}
