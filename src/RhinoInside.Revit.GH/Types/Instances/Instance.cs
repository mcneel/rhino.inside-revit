using System;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Linked Element")]
  public class Instance : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.Instance);
    public new ARDB.Instance Value => base.Value as ARDB.Instance;

    public Instance() { }
    public Instance(ARDB.Instance instance) : base(instance) { }

    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      return element is ARDB.Instance && !(element is ARDB.FamilyInstance);
    }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.Instance instance)
        {
          instance.GetLocation(out var origin, out var basisX, out var basisY);
          return new Plane(origin.ToPoint3d(), basisX.Direction.ToVector3d(), basisY.Direction.ToVector3d());
        }

        return base.Location;
      }
    }
  }
}
