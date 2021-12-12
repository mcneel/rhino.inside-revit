using Rhino;
using Rhino.Display;
using Rhino.Render;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Display
{
  using Convert.Render;
  using Convert.System.Drawing;

  /// <summary>
  /// Represents a converter for converting <see cref="DisplayMaterial"/> values
  /// back and forth Revit and Rhino.
  /// </summary>
  static class DisplayMaterialConverter
  {
    public static DisplayMaterial ToDisplayMaterial(this ARDB.Material material)
    {
      if(RhinoDoc.ActiveDoc is RhinoDoc rhinoDoc)
      {
        using (var renderMaterial = material.ToRenderMaterial(rhinoDoc))
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
