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
  public class ViewSchedule : TableView
  {
    protected override Type ValueType => typeof(ARDB.ViewSchedule);

    public ViewSchedule() { }
    public ViewSchedule(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public ViewSchedule(ARDB.ViewSchedule view) : base(view) { }
  }

  [Kernel.Attributes.Name("Panel Schedule")]
  public class PanelScheduleView : TableView
  {
    protected override Type ValueType => typeof(ARDB.Electrical.PanelScheduleView);

    public PanelScheduleView() { }
    public PanelScheduleView(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public PanelScheduleView(ARDB.Electrical.PanelScheduleView view) : base(view) { }
  }
}
