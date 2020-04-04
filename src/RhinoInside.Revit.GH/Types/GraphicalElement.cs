using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  /// <summary>
  /// Represents all elements that have a Graphical representation in Revit
  /// </summary>
  public class GraphicalElement :
    Element,
    IGH_GeometricGoo,
    IGH_PreviewData
  {
    public GraphicalElement() { }
    public GraphicalElement(DB.Element element) : base(element) { }

    protected override bool SetValue(DB.Element element) => IsValidElement(element) ? base.SetValue(element) : false;
    public static bool IsValidElement(DB.Element element)
    {
      return
      (
        element is DB.DirectShape ||
        element is DB.CurveElement ||
        element is DB.CombinableElement ||
        element is DB.Architecture.TopographySurface ||
        (element.Category is object && element.CanHaveTypeAssigned())
      );
    }

    #region IGH_GeometricGoo
    public BoundingBox Boundingbox => ClippingBox;
    Guid IGH_GeometricGoo.ReferenceID
    {
      get => Guid.Empty;
      set { if (value != Guid.Empty) throw new InvalidOperationException(); }
    }
    bool IGH_GeometricGoo.IsReferencedGeometry => IsReferencedElement;
    bool IGH_GeometricGoo.IsGeometryLoaded => IsElementLoaded;

    void IGH_GeometricGoo.ClearCaches() => UnloadElement();
    IGH_GeometricGoo IGH_GeometricGoo.DuplicateGeometry() => (IGH_GeometricGoo) MemberwiseClone();
    public BoundingBox GetBoundingBox(Transform xform) => ClippingBox;
    bool IGH_GeometricGoo.LoadGeometry() => IsElementLoaded || LoadElement();
    bool IGH_GeometricGoo.LoadGeometry(Rhino.RhinoDoc doc) => IsElementLoaded || LoadElement();
    IGH_GeometricGoo IGH_GeometricGoo.Transform(Transform xform) => null;
    IGH_GeometricGoo IGH_GeometricGoo.Morph(SpaceMorph xmorph) => null;
    #endregion

    #region IGH_PreviewData
    public virtual void DrawViewportWires(GH_PreviewWireArgs args) { }
    public virtual void DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    #endregion
  }
}
