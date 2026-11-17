using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Table View")]
  public abstract class TableView : View
  {
    protected override Type ValueType => typeof(ARDB.TableView);
    public new ARDB.TableView Value => base.Value as ARDB.TableView;

    public TableView() { }
    public TableView(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public TableView(ARDB.TableView view) : base(view) { }
  }

  [Kernel.Attributes.Name("Schedule")]
  public sealed class ViewSchedule : TableView
  {
    protected sealed override Type ValueType => typeof(ARDB.ViewSchedule);
    public new ARDB.ViewSchedule Value => base.Value as ARDB.ViewSchedule;

    public ViewSchedule() { }
    public ViewSchedule(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public ViewSchedule(ARDB.ViewSchedule view) : base(view) { }
  }

  [Kernel.Attributes.Name("Panel Schedule")]
  public sealed class PanelScheduleView : TableView
  {
    protected sealed override Type ValueType => typeof(ARDB.Electrical.PanelScheduleView);
    public new ARDB.Electrical.PanelScheduleView Value => base.Value as ARDB.Electrical.PanelScheduleView;

    public PanelScheduleView() { }
    public PanelScheduleView(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public PanelScheduleView(ARDB.Electrical.PanelScheduleView view) : base(view) { }
  }
}
