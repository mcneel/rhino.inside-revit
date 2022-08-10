using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Drafting View")]
  public class ViewDrafting : View
  {
    protected override Type ValueType => typeof(ARDB.ViewDrafting);
    public new ARDB.ViewDrafting Value => base.Value as ARDB.ViewDrafting;

    public ViewDrafting() { }
    public ViewDrafting(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public ViewDrafting(ARDB.ViewDrafting view) : base(view) { }
  }
}
