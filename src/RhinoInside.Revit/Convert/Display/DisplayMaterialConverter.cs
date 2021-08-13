using Rhino;
using Rhino.Display;
using Rhino.Render;
using RhinoInside.Revit.Convert.System.Drawing;
using DB = Autodesk.Revit.DB;
using SD = System.Drawing;

namespace RhinoInside.Revit.Convert.Display
{
  public static class DisplayMaterialConverter
  {
    static readonly DisplayMaterial DefaultMaterial = new DisplayMaterial(SD.Color.WhiteSmoke);
    public static DisplayMaterial ToDisplayMaterial(this DB.Material material, DisplayMaterial parentMaterial)
    {
      if (material is null)
        return parentMaterial ?? DefaultMaterial;

      if(RhinoDoc.ActiveDoc is RhinoDoc rhinoDoc)
      {
        using (var renderMaterial = Render.RenderMaterialConverter.ToRenderMaterial(material, rhinoDoc))
        {
          if (renderMaterial?.SimulatedMaterial(RenderTexture.TextureGeneration.Allow) is Rhino.DocObjects.Material rhinoMaterial)
            return new DisplayMaterial(rhinoMaterial);
        }
      }

      return new DisplayMaterial()
      {
        Diffuse = material.Color.ToColor(),
        Transparency = material.Transparency / 100.0,
        Shine = material.Shininess / 128.0
      };
    }
  }
}
