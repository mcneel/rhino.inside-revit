using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using ARDB_Viewer = ARDB.Element;

  [Kernel.Attributes.Name("Viewer")]
  public class Viewer : GraphicalElement, ISketchAccess
  {
    protected override Type ValueType => typeof(ARDB_Viewer);
    public new ARDB_Viewer Value => base.Value as ARDB_Viewer;

    protected override bool SetValue(ARDB_Viewer element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB_Viewer element)
    {
      return element.GetType() == typeof(ARDB_Viewer) &&
             element.Category?.Id.ToBuiltInCategory() == ARDB.BuiltInCategory.OST_Viewers;
    }

    public Viewer() { }
    public Viewer(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public Viewer(ARDB_Viewer box) : base(box)
    {
      if (!IsValidElement(box))
        throw new ArgumentException("Invalid Element", nameof(box));
    }

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args) => (Sketch as IGH_PreviewData)?.DrawViewportWires(args);
    protected override void DrawViewportMeshes(GH_PreviewMeshArgs args) => (Sketch as IGH_PreviewData)?.DrawViewportMeshes(args);
    #endregion

    #region GraphicalElement
    protected override void SubInvalidateGraphics()
    {
      _Sketch = default;

      base.SubInvalidateGraphics();
    }
    public override BoundingBox GetBoundingBox(Transform xform) => Sketch?.GetBoundingBox(xform) ?? NaN.BoundingBox;

    public override Plane Location => Sketch?.Location ?? NaN.Plane;
    #endregion

    #region ISketchAccess
    Sketch _Sketch;
    public Sketch Sketch => _Sketch ?? (Value is ARDB_Viewer viewer ? (_Sketch = new Sketch(viewer.GetSketch())) : default);
    #endregion
  }
}
