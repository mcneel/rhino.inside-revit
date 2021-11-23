using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;
using DBXS = RhinoInside.Revit.External.DB.Schemas;

namespace RhinoInside.Revit.GH.Components.ParameterElements
{
  public class QueryParameters : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("D82D9FC3-FC74-4C54-AAE1-CB4D806741DB");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementClassFilter(typeof(ARDB.ParameterElement));

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var doc = activeApp.ActiveUIDocument?.Document;
      if (doc is null) return;

      var commandId = doc.IsFamilyDocument ?
        Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.FamilyTypes) :
        Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.ProjectParameters);

      var commandName = doc.IsFamilyDocument ?
        "Open Family Parameters…" :
        "Open Project Parameters…";

      Menu_AppendItem
      (
        menu, commandName,
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }
    #endregion

    public QueryParameters() : base
    (
      name: "Query Parameters",
      nickname: "Parameters",
      description: "Get document parameters list",
      category: "Revit",
      subCategory: "Parameter"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Parameters.Param_Enum<Types.ParameterBinding>>("Binding", "B", "Category type", optional: true),
      ParamDefinition.Create<Param_String>("Name", "N", "Parameter name", optional: true),
      ParamDefinition.Create<Parameters.Param_Enum<Types.ParameterType>>("Type", "T", "Parameter type", optional: true),
      ParamDefinition.Create<Parameters.Param_Enum<Types.ParameterGroup>>("Group", "G", "Parameter group", optional: true, relevance: ParamRelevance.Primary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ParameterKey>("Parameter", "K", "Parameters list", GH_ParamAccess.list)
    };

    IEnumerable<(ARDB.Definition Definition, ARDB.Binding Binding)>
    GetAllProjectParameters(ARDB.Document document)
    {
      using (var iterator = document.ParameterBindings.ForwardIterator())
      {
        while (iterator.MoveNext())
          yield return (iterator.Key, iterator.Current as ARDB.Binding);
      }
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      if (!Params.TryGetData(DA, "Binding", out Types.ParameterBinding binding, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Name", out string name, x => x is object)) return;
      if (!Params.TryGetData(DA, "Type", out Types.ParameterType type, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Group", out Types.ParameterGroup group, x => x.IsValid)) return;

      var parameters = Enumerable.Empty<ARDB.InternalDefinition>();

      // Project or Family parameters
      if (doc.IsFamilyDocument)
      {
        if (binding is object)
        {
          switch (binding.Value)
          {
            case ERDB.ParameterBinding.Instance:
              parameters = doc.FamilyManager.Parameters.Cast<ARDB.FamilyParameter>().
                Where(x => x.IsInstance == true).Select(x => x.Definition as ARDB.InternalDefinition);
              break;

            case ERDB.ParameterBinding.Type:
              parameters = doc.FamilyManager.Parameters.Cast<ARDB.FamilyParameter>().
                Where(x => x.IsInstance == false).Select(x => x.Definition as ARDB.InternalDefinition);
              break;
          }
        }
        else parameters = doc.FamilyManager.Parameters.Cast<ARDB.FamilyParameter>().
            Select(x => x.Definition as ARDB.InternalDefinition);
      }
      else
      {
        if (binding is object)
        {
          switch (binding.Value)
          {
            case ERDB.ParameterBinding.Instance:
              parameters = GetAllProjectParameters(doc).
                Where(x => x.Binding is ARDB.InstanceBinding).
                Select(x => x.Definition).
                OfType<ARDB.InternalDefinition>();
              break;

            case ERDB.ParameterBinding.Type:
              parameters = GetAllProjectParameters(doc).
                Where(x => x.Binding is ARDB.TypeBinding).
                Select(x => x.Definition).
                OfType<ARDB.InternalDefinition>();
              break;
          }
        }
        else parameters = GetAllProjectParameters(doc).
                Select(x => x.Definition).
                OfType<ARDB.InternalDefinition>();
      }

      // Global parameters
      if (ARDB.GlobalParametersManager.AreGlobalParametersAllowed(doc))
      {
        var globals = ARDB.GlobalParametersManager.GetAllGlobalParameters(doc).
          Select(x => (doc.GetElement(x) as ARDB.GlobalParameter).GetDefinition());

        if (binding is null)
          parameters = parameters.Concat(globals);
        else if (binding.Value == ERDB.ParameterBinding.Global)
          parameters = globals;
      }

      if (!string.IsNullOrEmpty(name))
        parameters = parameters.Where(x => x.Name.IsSymbolNameLike(name));

      if (type is object)
        parameters = parameters.Where(x => (DBXS.DataType) x.GetDataType() == type.Value);

      if (group is object)
        parameters = parameters.Where(x => x.GetGroupType() == group.Value);

      // As any other Query component this should return elements sorted by Id.
      parameters = parameters.OrderBy(x => x.Id.IntegerValue);

      DA.SetDataList
      (
        "Parameter",
        parameters.
        Select(x => new Types.ParameterKey(doc, x)).
        TakeWhileIsNotEscapeKeyDown(this)
      );
    }
  }
}
