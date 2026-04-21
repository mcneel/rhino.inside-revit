using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Documents
{
  public class ActiveDocument : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("EE033516-C1DC-4C72-8FCD-F85F38A0F267");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "A";

    public ActiveDocument() : base
    (
      "Active Document", "A-Document",
      "Gets the active document",
      "Revit", "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => Array.Empty<ParamDefinition>();

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Document>("Active Document", "D", "Active document", GH_ParamAccess.item)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (Parameters.Document.TryGetCurrentDocument(this, out var Document))
        DA.SetData("Active Document", Document);
    }

    #region UI
    private class ButtonAttributes : ExpireButtonAttributes
    {
      public ButtonAttributes(ZuiComponent owner) : base(owner) { }
      protected override string DisplayText => "Query";
      protected override bool Visible => Owner.Params.Input.Count == 0;
    }

    public override void CreateAttributes() => m_attributes = new ButtonAttributes(this);
    #endregion
  }
}
