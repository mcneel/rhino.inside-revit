using System;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Linked Element")]
  public class Instance : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.Instance);
    public new ARDB.Instance Value => base.Value as ARDB.Instance;

    public Instance() { }
    public Instance(ARDB.Instance instance) : base(instance) { }

    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      return element is ARDB.Instance && !(element is ARDB.FamilyInstance);
    }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.Instance instance)
        {
          instance.GetLocation(out var origin, out var basisX, out var basisY);
          return new Plane(origin.ToPoint3d(), basisX.ToVector3d(), basisY.ToVector3d());
        }

        return base.Location;
      }
    }
  }

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

  [Kernel.Attributes.Name("Import Symbol")]
  public class ImportInstance : Instance
  {
    protected override Type ValueType => typeof(ARDB.ImportInstance);
    public new ARDB.ImportInstance Value => base.Value as ARDB.ImportInstance;

    public ImportInstance() { }
    public ImportInstance(ARDB.ImportInstance instance) : base(instance) { }
  }

  [Kernel.Attributes.Name("Point Cloud")]
  public class PointCloudInstance : Instance
  {
    protected override Type ValueType => typeof(ARDB.PointCloudInstance);
    public new ARDB.PointCloudInstance Value => base.Value as ARDB.PointCloudInstance;

    public PointCloudInstance() { }
    public PointCloudInstance(ARDB.PointCloudInstance instance) : base(instance) { }
  }
}
