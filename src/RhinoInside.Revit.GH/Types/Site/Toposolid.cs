using System;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Display;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
#if REVIT_2024
  [Kernel.Attributes.Name("Toposolid")]
  public class Toposolid : HostObject,
    ISketchAccess
  {
    protected override Type ValueType => typeof(ARDB.Toposolid);
    public new ARDB.Toposolid Value => base.Value as ARDB.Toposolid;

    public Toposolid() { }
    public Toposolid(ARDB.Toposolid element) : base(element) { }

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

    #region IHostElementAccess
    public override GraphicalElement HostElement =>
      Value is ARDB.Toposolid solid ?
          solid.HostTopoId.IsValid() ?
          GetElement<GraphicalElement>(solid.HostTopoId) :
          new GraphicalElement():
        default;
    #endregion

    #region ISketchAccess
    public Sketch Sketch => Value is ARDB.Toposolid solid ?
      new Sketch(solid.GetSketch()) : default;
    #endregion
  }
#endif
}
