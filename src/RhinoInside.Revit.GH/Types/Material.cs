using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class Material : Element
  {
    public override string TypeName => "Revit Material";
    public override string TypeDescription => "Represents a Revit material";
    protected override Type ScriptVariableType => typeof(DB.Material);
    public static explicit operator DB.Material(Material value) =>
      value.IsValid ? value.Document?.GetElement(value) as DB.Material : default;

    public Material() { }
    public Material(DB.Material material) : base(material) { }
  }
}
