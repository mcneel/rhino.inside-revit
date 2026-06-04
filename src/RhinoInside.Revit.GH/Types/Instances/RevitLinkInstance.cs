using System;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Linked Model")]
  public sealed class RevitLinkInstance : Instance, IGH_ElementSource
  {
    protected override Type ValueType => typeof(ARDB.RevitLinkInstance);
    public new ARDB.RevitLinkInstance Value => base.Value as ARDB.RevitLinkInstance;

    #region IGH_ElementSource
    RevitLinkInstance IGH_ElementSource.SourceInstance => this;
    public Document SourceDocument => Types.Document.FromValue(Value.GetLinkDocument());
    #endregion

    public RevitLinkInstance() { }
    public RevitLinkInstance(ARDB.RevitLinkInstance instance) : base(instance) { }

    public override string DisplayName
    {
      get
      {
        if (Value is ARDB.RevitLinkInstance instance)
          return instance.Name;

        return base.DisplayName;
      }
    }

    public override string Nomen
    {
      get
      {
        if (Value is ARDB.RevitLinkInstance instance)
          return instance.get_Parameter(ARDB.BuiltInParameter.RVT_LINK_INSTANCE_NAME).AsString();

        return base.Nomen;
      }
    }
  }
}
