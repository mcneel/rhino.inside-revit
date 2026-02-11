using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.DesignOptions
{
  using External.UI.Extensions;

  public class ActiveDesignOption : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("B6349DDA-4486-44EB-9AF7-3D13404A3F3E");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "A";
    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementClassFilter(typeof(ARDB.DesignOption));

    public ActiveDesignOption() : base
    (
      name: "Active Design Option",
      nickname: "A-DOption",
      description: "Gets the active Design Option",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.ElementSource(), ParamRelevance.Occasional),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Element>("Active Design Option", "O", "Active design option")
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.ElementSource.GetElementSourceOrCurrent(this, DA, out var source))
        return;

      var option = new Types.DesignOption(source.SourceDocument.Value, ARDB.DesignOption.GetActiveDesignOptionId(source.SourceDocument.Value));
      DA.SetData("Active Design Option", option.FromSource(source));
    }

    #region UI
    private class ButtonAttributes : ExpireButtonAttributes
    {
      public ButtonAttributes(ZuiComponent owner) : base(owner) { }
      protected override string DisplayText => "Query";
      protected override bool Visible => Owner.Params.Input.Count == 0;
    }
    public override void CreateAttributes() => m_attributes = new ButtonAttributes(this);

    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      {
        var activeApp = Revit.ActiveUIApplication;
        var postable = activeApp.TryGetRevitCommandId(Autodesk.Revit.UI.PostableCommand.DesignOptions, out var commandId);
        Menu_AppendItem
        (
          menu, $"Open Design Options…",
          async (sender, arg) =>
          {
            using (var scope = new External.UI.EditScope(activeApp))
            {
              var activeDoc = activeApp.ActiveUIDocument.Document;
              var activeDesignOption = ARDB.DesignOption.GetActiveDesignOptionId(activeDoc);
              var changes = await scope.ExecuteCommandAsync(commandId);
              if (changes.GetSummary(activeDoc, out var _, out var _, out var _) > 0)
              {
                if (activeDesignOption != ARDB.DesignOption.GetActiveDesignOptionId(activeDoc))
                  ExpireSolution(true);
              }
            }
          },
          postable, false
        );
      }
    }
    #endregion
  }
}
