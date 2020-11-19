using System;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Linked Element")]
  public class Instance : GraphicalElement
  {
    protected override Type ScriptVariableType => typeof(DB.Instance);
    public new DB.Instance Value => base.Value as DB.Instance;

    public Instance() { }
    public Instance(DB.Instance instance) : base(instance) { }

    protected override bool SetValue(DB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(DB.Element element)
    {
      return element is DB.Instance && !(element is DB.FamilyInstance);
    }

    public override Plane Location
    {
      get
      {
        if (Value is DB.Instance instance)
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
    protected override Type ScriptVariableType => typeof(DB.RevitLinkInstance);
    public new DB.RevitLinkInstance Value => base.Value as DB.RevitLinkInstance;

    public RevitLinkInstance() { }
    public RevitLinkInstance(DB.RevitLinkInstance instance) : base(instance) { }
  }

  [Kernel.Attributes.Name("Import Symbol")]
  public class ImportInstance : Instance
  {
    protected override Type ScriptVariableType => typeof(DB.ImportInstance);
    public new DB.ImportInstance Value => base.Value as DB.ImportInstance;

    public ImportInstance() { }
    public ImportInstance(DB.ImportInstance instance) : base(instance) { }
  }

  [Kernel.Attributes.Name("Point Cloud")]
  public class PointCloudInstance : Instance
  {
    protected override Type ScriptVariableType => typeof(DB.PointCloudInstance);
    public new DB.PointCloudInstance Value => base.Value as DB.PointCloudInstance;

    public PointCloudInstance() { }
    public PointCloudInstance(DB.PointCloudInstance instance) : base(instance) { }
  }
}
