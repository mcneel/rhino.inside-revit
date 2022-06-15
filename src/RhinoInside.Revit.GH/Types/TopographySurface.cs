using System;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Display;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Topography")]
  public class TopographySurface : GeometricElement
  {
    protected override Type ValueType => typeof(ARDB.Architecture.TopographySurface);
    public new ARDB.Architecture.TopographySurface Value => base.Value as ARDB.Architecture.TopographySurface;

    public TopographySurface() { }
    public TopographySurface(ARDB.Architecture.TopographySurface element) : base(element) { }

    #region Location
    public override Mesh Mesh
    {
      get
      {
        if (Value is object)
        {
          using (var options = new ARDB.Options() { DetailLevel = ARDB.ViewDetailLevel.Fine })
          using (var geometry = Value.get_Geometry(options))
          {
            if (geometry is object)
            {
              var mesh = new Mesh();
              mesh.Append(geometry.GetPreviewMeshes(Document, null));
              mesh.Normals.ComputeNormals();

              if (mesh.Faces.Count > 0)
                return mesh;
            }
          }
        }

        return null;
      }
    }
    #endregion
  }
}
