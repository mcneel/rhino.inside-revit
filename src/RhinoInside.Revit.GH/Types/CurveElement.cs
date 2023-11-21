using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
#if RHINO_8
using Grasshopper.Rhinoceros;
using Grasshopper.Rhinoceros.Model;
#endif

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Curve Element")]
  public class CurveElement : GraphicalElement, Bake.IGH_BakeAwareElement,
    IHostElementAccess
  {
    protected override Type ValueType => typeof(ARDB.CurveElement);
    public static explicit operator ARDB.CurveElement(CurveElement value) => value?.Value;
    public new ARDB.CurveElement Value => base.Value as ARDB.CurveElement;

    public CurveElement() { }
    public CurveElement(ARDB.CurveElement value) : base(value) { }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Curve is Curve curve)
        return curve.GetBoundingBox(xform);

      return base.GetBoundingBox(xform);
    }

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Curve is Curve curve)
        args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
    }
    #endregion

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<ARDB.ElementId, Guid>(), true, doc, att, out guid);

    public bool BakeElement
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
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
      if (Value is ARDB.CurveElement curve)
      {
        att = att?.Duplicate() ?? doc.CreateDefaultAttributes();
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

    #region ModelContent
#if RHINO_8
    internal override ModelContent ToModelContent(IDictionary<ARDB.ElementId, ModelContent> idMap)
    {
      if (idMap.TryGetValue(Id, out var modelContent))
        return modelContent;

      if (Value is ARDB.CurveElement curve)
      {
        var attributes = ModelObject.Cast(new GH_Curve(curve.GeometryCurve.ToCurve())).ToAttributes();
        //attributes.Name = DisplayName;
        attributes.Layer = Category.ToModelContent(idMap) as ModelLayer;

        modelContent = attributes.ToModelData() as ModelContent;
        //idMap.Add(Id, modelContent);
        return modelContent;
      }

      return null;
    }
#endif
    #endregion

    #region IHostElementAccess
    public virtual GraphicalElement HostElement
    {
      get
      {
        if (Value is ARDB.CurveElement curveElement)
        {
          var host = GetElement<GraphicalElement>(curveElement.SketchPlane.GetHost(out var hostReference));
          return hostReference is object ? host?.GetElementFromReference<GraphicalElement>(hostReference) : host;
        }

        return default;
      }
    }
    #endregion

    #region Category
    public override Category Subcategory
    {
      get => Category.FromCategory(LineStyle?.Value.GraphicsStyleCategory);
      set
      {
        if (value is object && Value is ARDB.CurveElement element)
        {
          AssertValidDocument(value, nameof(Subcategory));
          var styleType = LineStyle?.Value.GraphicsStyleType ?? ARDB.GraphicsStyleType.Projection;
          var style = value.APIObject?.GetGraphicsStyle(styleType);
          element.LineStyle = style;
        }
      }
    }

    public GraphicsStyle LineStyle
    {
      get => GetElement<GraphicsStyle>(Value?.LineStyle);
      set
      {
        if (value is object && Value is ARDB.CurveElement element)
          element.LineStyle = SetElement<ARDB.GraphicsStyle>(value);
      }
    }
    #endregion

    #region Location
    public override Curve Curve => Value?.GeometryCurve.ToCurve();

    public override void SetCurve(Curve curve, bool keepJoins = false)
    {
      if (Value is ARDB.CurveElement curveElement && curve is object)
      {
        var newCurve = curve.ToCurve();
        if (!curveElement.GeometryCurve.AlmostEquals(newCurve, GeometryTolerance.Internal.VertexTolerance))
        {
          curveElement.SetGeometryCurve(newCurve, overrideJoins: !keepJoins);
          InvalidateGraphics();
        }
      }
    }

    public SketchPlane SketchPlane => GetElement<SketchPlane>(Value?.SketchPlane);
    #endregion
  }
}
