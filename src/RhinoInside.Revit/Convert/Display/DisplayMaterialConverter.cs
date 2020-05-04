using System.Drawing;
using Rhino.Display;
using RhinoInside.Revit.Convert.System.Drawing;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Display
{
  public static class DisplayMaterialConverter
  {
    static readonly DisplayMaterial DefaultMaterial = new DisplayMaterial(Color.WhiteSmoke);
    public static DisplayMaterial ToDisplayMaterial(this DB.Material material, DisplayMaterial parentMaterial)
    {
      return (material is null) ?
        parentMaterial ?? DefaultMaterial :
        new DisplayMaterial()
        {
          Diffuse = material.Color.ToColor(),
          Transparency = material.Transparency / 100.0,
          Shine = material.Shininess / 128.0
        };
    }
  }
}
