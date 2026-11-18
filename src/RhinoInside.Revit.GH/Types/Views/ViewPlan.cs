using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Plan View")]
  public class ViewPlan : View
  {
    protected sealed override Type ValueType => typeof(ARDB.ViewPlan);
    public new ARDB.ViewPlan Value => base.Value as ARDB.ViewPlan;

    internal ViewPlan() { }
    internal ViewPlan(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    internal ViewPlan(ARDB.ViewPlan view) : base(view) { }
  }

  [Kernel.Attributes.Name("Floor Plan")]
  public sealed class FloorPlan : ViewPlan
  {
    public FloorPlan() { }
    public FloorPlan(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public FloorPlan(ARDB.ViewPlan view) : base(view) { }
  }

  [Kernel.Attributes.Name("Ceiling Plan")]
  public sealed class CeilingPlan : ViewPlan
  {
    public CeilingPlan() { }
    public CeilingPlan(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public CeilingPlan(ARDB.ViewPlan view) : base(view) { }
  }

  [Kernel.Attributes.Name("Area Plan")]
  public sealed class AreaPlan : ViewPlan
  {
    public AreaPlan() { }
    public AreaPlan(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public AreaPlan(ARDB.ViewPlan view) : base(view) { }

    public override string DisplayName => Value?.AreaScheme is ARDB.AreaScheme scheme ?
      $"{base.DisplayName} ({scheme.Name})" : base.DisplayName;
  }

  [Kernel.Attributes.Name("Structural Plan")]
  public sealed class StructuralPlan : ViewPlan
  {
    public StructuralPlan() { }
    public StructuralPlan(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public StructuralPlan(ARDB.ViewPlan view) : base(view) { }
  }
}
