using System;
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
      var _Filter_ = Params.IndexOfInputParam("Filter");
      var filters = _Filter_ < 0 ?
                    Enumerable.Empty<DB.ElementFilter>() :
                    Params.Input[_Filter_].VolatileData.AllData(true).OfType<Types.ElementFilter>().Select(x => new DB.LogicalAndFilter(x.Value, elementFilter));

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
          foreach (var param in Params.Output.OfType<Kernel.IGH_ElementIdParam>())
          {
            if (param.NeedsToBeExpired(document, empty, deleted, empty))
              return true;
          }
        }
      }

      return false;
    }

    protected static bool TryGetFilterIntegerParam(DB.BuiltInParameter paramId, int pattern, out DB.ElementFilter filter)
    {
      var rule = new DB.FilterIntegerRule
      (
        new DB.ParameterValueProvider(new DB.ElementId(paramId)),
        new DB.FilterNumericEquals(),
        pattern
      );

      filter = new DB.ElementParameterFilter(rule, false);
      return true;
    }

    protected static bool TryGetFilterDoubleParam(DB.BuiltInParameter paramId, double pattern, out DB.ElementFilter filter)
    {
      var rule = new DB.FilterDoubleRule
      (
        new DB.ParameterValueProvider(new DB.ElementId(paramId)),
        new DB.FilterNumericEquals(),
        pattern,
        1e-6
      );

      filter = new DB.ElementParameterFilter(rule, false);
      return true;
    }

    protected static bool TryGetFilterDoubleParam(DB.BuiltInParameter paramId, double pattern, double tolerance, out DB.ElementFilter filter)
    {
      var rule = new DB.FilterDoubleRule
      (
        new DB.ParameterValueProvider(new DB.ElementId(paramId)),
        new DB.FilterNumericEquals(),
        pattern,
        tolerance
      );

      filter = new DB.ElementParameterFilter(rule, false);
      return true;
    }

    protected static bool TryGetFilterLengthParam(DB.BuiltInParameter paramId, double pattern, out DB.ElementFilter filter)
    {
      var rule = new DB.FilterDoubleRule
      (
        new DB.ParameterValueProvider(new DB.ElementId(paramId)),
        new DB.FilterNumericEquals(),
        pattern,
        Revit.VertexTolerance
      );

      filter = new DB.ElementParameterFilter(rule, false);
      return true;
    }

    protected static bool TryGetFilterStringParam(DB.BuiltInParameter paramId, ref string pattern, out DB.ElementFilter filter)
    {
      if (pattern is string subPattern)
      {
        var inverted = false;
        var method = Operator.CompareMethodFromPattern(ref subPattern, ref inverted);
        if (Operator.CompareMethod.Nothing < method && method < Operator.CompareMethod.Wildcard)
        {
          var evaluator = default(DB.FilterStringRuleEvaluator);
          switch (method)
          {
            case Operator.CompareMethod.Equals: evaluator = new DB.FilterStringEquals(); break;
            case Operator.CompareMethod.StartsWith: evaluator = new DB.FilterStringBeginsWith(); break;
            case Operator.CompareMethod.EndsWith: evaluator = new DB.FilterStringEndsWith(); break;
            case Operator.CompareMethod.Contains: evaluator = new DB.FilterStringContains(); break;
          }

          var rule = new DB.FilterStringRule
          (
            new DB.ParameterValueProvider(new DB.ElementId(paramId)),
            evaluator,
            subPattern,
            true
          );

          filter = new DB.ElementParameterFilter(rule, inverted);
          pattern = default;
          return true;
        }
      }

      filter = default;
      return false;
    }

    protected static bool TryGetFilterElementIdParam(DB.BuiltInParameter paramId, DB.ElementId pattern, out DB.ElementFilter filter)
    {
      var rule = new DB.FilterElementIdRule
      (
        new DB.ParameterValueProvider(new DB.ElementId(paramId)),
        new DB.FilterNumericEquals(),
        pattern
      );

      filter = new DB.ElementParameterFilter(rule, false);
      pattern = default;
      return true;
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

        // In case the user has more than one document open we show which one this component is working on
        if (Revit.ActiveDBApplication.Documents.Size > 1)
          Message = Document.Title.TripleDot(16);
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

    #region IGH_VariableParameterComponent
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
    #endregion
  }
}
