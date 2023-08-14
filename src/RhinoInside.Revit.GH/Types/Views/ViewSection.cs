using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Section View")]
  public class ViewSection : View
  {
    protected override Type ValueType => typeof(ARDB.ViewSection);
    public new ARDB.ViewSection Value => base.Value as ARDB.ViewSection;

    public ViewSection() { }
    public ViewSection(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public ViewSection(ARDB.ViewSection view) : base(view) { }
  }

  [Kernel.Attributes.Name("Section")]
  public class SectionView : ViewSection
  {
    protected override Type ValueType => typeof(ARDB.ViewSection);

    public SectionView() { }
    public SectionView(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public SectionView(ARDB.ViewSection view) : base(view) { }
  }

  [Kernel.Attributes.Name("Detail")]
  public class DetailView : ViewSection
  {
    protected override Type ValueType => typeof(ARDB.ViewSection);

    public DetailView() { }
    public DetailView(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public DetailView(ARDB.ViewSection view) : base(view) { }
  }
}
