using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public abstract class DocumentComponent : Component, IGH_VariableParameterComponent
  {
    protected DocumentComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    public override bool NeedsToBeExpired(DB.Events.DocumentChangedEventArgs e)
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

    protected override void RegisterInputParams(GH_InputParamManager manager) { }

    protected override sealed void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Document Document = default;
      var _Document_ = Params.IndexOfInputParam("Document");
      if (_Document_ < 0)
      {
        Document = Revit.ActiveDBDocument;
        if (Document?.IsValidObject != true)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "There is no active Revit document");
          return;
        }

        Message = $"Doc : {Document.Title}";
      }
      else
      {
        DA.GetData(_Document_, ref Document);
        if (Document?.IsValidObject != true)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter Document failed to collect data");
          return;
        }
      }

      TrySolveInstance(DA, Document);
    }

    protected abstract void TrySolveInstance(IGH_DataAccess DA, DB.Document doc);

    bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) =>
      side == GH_ParameterSide.Input && index == 0 && (Params.Input.Count == 0 || Params.Input[0].Name != "Document");

    bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) =>
      side == GH_ParameterSide.Input && index == 0 && (Params.Input.Count > 0 && Params.Input[0].Name == "Document");

    IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index)
    {
      var Document = new Parameters.Document();
      Document.Name = "Document";
      Document.NickName = "Document";
      Document.Description = "Document to query elements";
      Document.Access = GH_ParamAccess.item;
      Message = string.Empty;

      return Document;
    }
    bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Input && index == 0;
    void IGH_VariableParameterComponent.VariableParameterMaintenance() { }
  }
}
