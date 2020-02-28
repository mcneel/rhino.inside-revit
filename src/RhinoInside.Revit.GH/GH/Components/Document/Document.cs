using System.Linq;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public abstract class DocumentComponent : Component
  {
    protected DocumentComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    public override bool NeedsToBeExpired(Autodesk.Revit.DB.Events.DocumentChangedEventArgs e)
    {
      var elementFilter = ElementFilter;
      var filters = Params.Input.Count > 0 ?
                    Params.Input[0].VolatileData.AllData(true).OfType<Types.ElementFilter>().Select(x => new DB.LogicalAndFilter(x.Value, elementFilter)) :
                    Enumerable.Empty<DB.ElementFilter>();

      foreach (var filter in filters.Any() ? filters : Enumerable.Repeat(elementFilter, 1))
      {
        var added = filter is null ? e.GetAddedElementIds() : e.GetAddedElementIds(filter);
        if (added.Count > 0)
          return true;

        var modified = filter is null ? e.GetModifiedElementIds() : e.GetModifiedElementIds(filter);
        if (modified.Count > 0)
          return true;

        var deleted = e.GetDeletedElementIds();
        if (deleted.Count > 0)
        {
          var document = e.GetDocument();
          var empty = new DB.ElementId[0];
          foreach (var param in Params.Output.OfType<Parameters.IGH_ElementIdParam>())
          {
            if (param.NeedsToBeExpired(document, empty, deleted, empty))
              return true;
          }
        }
      }

      return false;
    }
  }
}
