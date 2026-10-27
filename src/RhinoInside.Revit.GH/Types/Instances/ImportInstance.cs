using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Import Symbol")]
  public class ImportInstance : Instance
  {
    protected override Type ValueType => typeof(ARDB.ImportInstance);
    public new ARDB.ImportInstance Value => base.Value as ARDB.ImportInstance;

    public ImportInstance() { }
    public ImportInstance(ARDB.ImportInstance instance) : base(instance) { }

    public override string Nomen
    {
      get
      {
        if (Value is ARDB.ImportInstance instance)
          return instance.get_Parameter(ARDB.BuiltInParameter.IMPORT_SYMBOL_NAME).AsString();

        return base.Nomen;
      }
    }
  }

  [Kernel.Attributes.Name("Symbol Type")]
  public class CADLinkType : ElementType
  {
    protected override Type ValueType => typeof(ARDB.CADLinkType);
    public new ARDB.CADLinkType Value => base.Value as ARDB.CADLinkType;

    public CADLinkType() { }
    public CADLinkType(ARDB.CADLinkType type) : base(type) { }
  }
}
