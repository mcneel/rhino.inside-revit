using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Curve Element")]
  public class CurveElement : GraphicalElement, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(DB.CurveElement);
    public static explicit operator DB.CurveElement(CurveElement value) => value?.Value;
    public new DB.CurveElement Value => base.Value as DB.CurveElement;

    public CurveElement() { }
    public CurveElement(DB.CurveElement value) : base(value) { }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Curve is Curve curve)
        return curve.GetBoundingBox(xform);

      return base.GetBoundingBox(xform);
    }

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Curve is Curve curve)
        args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
    }
    #endregion

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<DB.ElementId, Guid>(), true, doc, att, out guid);

    public bool BakeElement
    (
      IDictionary<DB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      // 3. Update if necessary
      if (Value is DB.CurveElement curve)
      {
        att = att.Duplicate();
        att.Name = DisplayName;
        if (Category.BakeElement(idMap, false, doc, att, out var layerGuid))
          att.LayerIndex = doc.Layers.FindId(layerGuid).Index;

        guid = doc.Objects.Add(curve.GeometryCurve.ToCurve(), att);

        if (guid != Guid.Empty)
        {
          idMap.Add(Id, guid);
          return true;
        }
      }

      return false;
    }
    #endregion

    #region Properties
    public override Curve Curve => Value?.GeometryCurve.ToCurve();
    #endregion
  }
}
