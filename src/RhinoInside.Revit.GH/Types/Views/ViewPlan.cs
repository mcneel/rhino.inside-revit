using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Plan View")]
  public class ViewPlan : View
  {
    protected override Type ValueType => typeof(ARDB.ViewPlan);
    public new ARDB.ViewPlan Value => base.Value as ARDB.ViewPlan;

    public ViewPlan() { }
    public ViewPlan(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public ViewPlan(ARDB.ViewPlan view) : base(view) { }
  }

  [Kernel.Attributes.Name("Floor Plan")]
  public class FloorPlan : ViewPlan
  {
    protected override Type ValueType => typeof(ARDB.ViewPlan);
    public new ARDB.ViewPlan Value => base.Value as ARDB.ViewPlan;

    public FloorPlan() { }
    public FloorPlan(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public FloorPlan(ARDB.ViewPlan view) : base(view) { }
  }

  [Kernel.Attributes.Name("Ceiling Plan")]
  public class CeilingPlan : ViewPlan
  {
    protected override Type ValueType => typeof(ARDB.ViewPlan);
    public new ARDB.ViewPlan Value => base.Value as ARDB.ViewPlan;

    public CeilingPlan() { }
    public CeilingPlan(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public CeilingPlan(ARDB.ViewPlan view) : base(view) { }
  }

  [Kernel.Attributes.Name("Area Plan")]
  public class AreaPlan : ViewPlan
  {
    protected override Type ValueType => typeof(ARDB.ViewPlan);
    public new ARDB.ViewPlan Value => base.Value as ARDB.ViewPlan;

    public AreaPlan() { }
    public AreaPlan(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public AreaPlan(ARDB.ViewPlan view) : base(view) { }
  }

  [Kernel.Attributes.Name("Structural Plan")]
  public class StructuralPlan : ViewPlan
  {
    protected override Type ValueType => typeof(ARDB.ViewPlan);
    public new ARDB.ViewPlan Value => base.Value as ARDB.ViewPlan;

    public StructuralPlan() { }
    public StructuralPlan(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public StructuralPlan(ARDB.ViewPlan view) : base(view) { }
  }
}
