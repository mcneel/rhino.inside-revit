using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using System.Linq;
  using Convert.Geometry;
  using Convert.System.Drawing;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Filled Region")]
  public class FilledRegion : GeometricElement, ISketchAccess, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(ARDB.FilledRegion);
    public new ARDB.FilledRegion Value => base.Value as ARDB.FilledRegion;

    public FilledRegion() { }
    public FilledRegion(ARDB.FilledRegion element) : base(element) { }

    #region ISketchAccess
    public Sketch Sketch => Value is ARDB.FilledRegion region ?
      new Sketch(region.GetSketch()) : default;
    #endregion

    #region Location
    public override Plane Location
    {
      get
      {
        if (Sketch is Sketch sketch)
        {
          var plane = sketch.ProfilesPlane;
          plane.Origin = plane.ClosestPoint(BoundingBox.Center);

          return plane;
        }

        return base.Location;
      }
    }
    #endregion

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<ARDB.ElementId, Guid>(), true, doc, att, out guid);

    protected internal static bool BakeGeometryElement
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      Transform transform,
      ARDB.FilledRegion element,
      out int index
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(element.Id, out var guid))
      {
        index = doc.InstanceDefinitions.FindId(guid).Index;
        return true;
      }

      // Get a Unique Instance Definition name.
      var idef_name = GetBakeInstanceDefinitionName(element, out var idef_description);

      // 2. Check if already exist
      index = doc.InstanceDefinitions.Find(idef_name)?.Index ?? -1;

      // 3. Update if necessary
      if (index < 0 || overwrite)
      {
        bool identity = transform.IsIdentity;

        var geometry = new List<GeometryBase>();
        var attributes = new List<ObjectAttributes>();

        var type = element.Document.GetElement(element.GetTypeId()) as ARDB.FilledRegionType;
        var pattern = Types.Element.FromElementId(type.Document, type.ForegroundPatternId) as Types.FillPatternElement;

        var patternIndex = -1;
        if (pattern.BakeElement(idMap, false, doc, att, out var patternId))
          patternIndex = doc.HatchPatterns.FindId(patternId).Index;

        var tol = GeometryTolerance.Model;
        var curves = element.GetBoundaries().Select(GeometryDecoder.ToPolyCurve).ToArray();

        // Lets do an aproximation using just lines.
        var fillPattern = pattern.Value.GetFillPattern();
        var angle = fillPattern.GetFillGrid(0).Angle;
        var offset = fillPattern.GetFillGrid(0).Offset * Revit.ModelUnits;
        if (fillPattern.Target != ARDB.FillPatternTarget.Drafting && doc.ModelSpaceHatchScalingEnabled)
        {
          var view = element.Document.GetElement(element.OwnerViewId) as ARDB.View;
          angle -= element.GetSketch().SketchPlane.GetPlane().XVec.AngleOnPlaneTo(view.RightDirection, view.ViewDirection);
          offset /= doc.ModelSpaceHatchScale;
        }

        // 'Hatch1' is spaced 1/8. We inverse this here as if it was 1.0.
        if (fillPattern.GridCount == 2)
        {
          var gridPatternSpaccing = 1.0 / 4.0;
          offset /= gridPatternSpaccing;
        }
        else
        {
          var hatch1PatternSpaccing = 1.0 / 8.0;
          offset /= hatch1PatternSpaccing;
        }

        // Curves are in the reverse orientation Hatch.Create needs.
        foreach (var curve in curves) curve.Reverse();
        var hatches = Hatch.Create(curves, patternIndex, angle, offset, tol.VertexTolerance);

        // Boundary
        {
          var byLayerAttributes = new ObjectAttributes()
          {
            LayerIndex = att.LayerIndex,
            LinetypeSource = ObjectLinetypeSource.LinetypeFromLayer,
            ColorSource = ObjectColorSource.ColorFromLayer,
            PlotColorSource = ObjectPlotColorSource.PlotColorFromLayer,
            PlotWeightSource = ObjectPlotWeightSource.PlotWeightFromLayer,
            MaterialSource = ObjectMaterialSource.MaterialFromLayer,
          };

          var byParentAttributes = new ObjectAttributes()
          {
            LayerIndex = att.LayerIndex,
            LinetypeSource = ObjectLinetypeSource.LinetypeFromParent,
            ColorSource = ObjectColorSource.ColorFromParent,
            PlotColorSource = ObjectPlotColorSource.PlotColorFromParent,
            PlotWeightSource = ObjectPlotWeightSource.PlotWeightFromParent,
            MaterialSource = ObjectMaterialSource.MaterialFromParent,
          };

          foreach (var loop in element.GetSketch().GetProfileCurveElements())
          {
            foreach (var curve in loop)
            {
              var category = Category.FromCategory((curve.LineStyle as ARDB.GraphicsStyle).GraphicsStyleCategory);
              if (category.BakeElement(idMap, false, doc, att, out var layerGuid))
              {
                var linestyle = byLayerAttributes.Duplicate();
                linestyle.LayerIndex = doc.Layers.FindId(layerGuid).Index;
                attributes.Add(linestyle);
              }
              else attributes.Add(byParentAttributes);

              geometry.Add(curve.GeometryCurve.ToCurve());
            }
          }
        }

        // Hatch
        {
          var byTypeAttributes = new ObjectAttributes()
          {
            LayerIndex = att.LayerIndex,
            LinetypeSource = ObjectLinetypeSource.LinetypeFromObject,
            ColorSource = ObjectColorSource.ColorFromObject,
            ObjectColor = type.ForegroundPatternColor.ToColor(),
            PlotColorSource = ObjectPlotColorSource.PlotColorFromDisplay,
            PlotWeightSource = ObjectPlotWeightSource.PlotWeightFromParent,
            MaterialSource = ObjectMaterialSource.MaterialFromParent,
          };

          foreach (var hatch in hatches)
          {
            if (fillPattern.Target == ARDB.FillPatternTarget.Drafting)
            {
              geometry.Add(hatch);
              attributes.Add(byTypeAttributes);
            }
            else
            {
              var lines = hatch.Explode();
              geometry.AddRange(lines);
              attributes.AddRange(Enumerable.Repeat(byTypeAttributes, lines.Length));
            }
          }
        }

        if (!identity)
        {
          foreach (var geo in geometry)
            geo.Transform(transform);
        }

        if (index < 0) index = doc.InstanceDefinitions.Add(idef_name, idef_description, Point3d.Origin, geometry, attributes);
        else if (!doc.InstanceDefinitions.ModifyGeometry(index, geometry, attributes)) index = -1;
      }

      return index >= 0;
    }

    public override bool BakeElement
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
      if (Value is ARDB.FilledRegion filledRegion)
      {
        att = att?.Duplicate() ?? doc.CreateDefaultAttributes();
        att.Name = filledRegion.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty;
        att.Url = filledRegion.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_URL)?.AsString() ?? string.Empty;

        if (Category.BakeElement(idMap, false, doc, att, out var layerGuid))
          att.LayerIndex = doc.Layers.FindId(layerGuid).Index;

        var location = Location;
        var worldToElement = Transform.PlaneToPlane(location, Plane.WorldXY);
        if (BakeGeometryElement(idMap, overwrite, doc, att, worldToElement, filledRegion, out var idefIndex))
          guid = doc.Objects.AddInstanceObject(idefIndex, Transform.PlaneToPlane(Plane.WorldXY, location), att);

        if (guid != Guid.Empty)
        {
          idMap.Add(Id, guid);
          return true;
        }
      }

      return false;
    }
    #endregion
  }
}
