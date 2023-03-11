using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Linked Model")]
  public class RevitLinkInstance : Instance
  {
    protected override Type ValueType => typeof(ARDB.RevitLinkInstance);
    public new ARDB.RevitLinkInstance Value => base.Value as ARDB.RevitLinkInstance;

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
