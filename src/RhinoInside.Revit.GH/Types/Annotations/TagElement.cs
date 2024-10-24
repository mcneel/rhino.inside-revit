using System;
using System.Linq;
using Rhino.Display;
using Rhino.Geometry;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Numerical;
  using Convert.Geometry;
  using External.DB;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Tag")]
  public abstract class TagElement : GraphicalElement, IGH_Annotation,
    IAnnotationReferencesAccess,
    IAnnotationLeadersAccess
  {
    protected TagElement() { }
    protected TagElement(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    protected TagElement(ARDB.Element element) : base(element) { }

    #region IAnnotationReferencesAcces
    public abstract GeometryObject[] References { get; }
    #endregion

    #region IAnnotationLeadersAcces
    public abstract bool? HasLeader { get; set; }
    public abstract AnnotationLeader[] Leaders { get; }
    #endregion
  }

  [Kernel.Attributes.Name("Element Tag")]
  public class IndependentTag : TagElement
  {
    protected override Type ValueType => typeof(ARDB.IndependentTag);
    public new ARDB.IndependentTag Value => base.Value as ARDB.IndependentTag;

    public IndependentTag() { }
    public IndependentTag(ARDB.IndependentTag element) : base(element) { }

    #region Location
    public override Plane Location
    {
      get
      {
        if (Value is ARDB.IndependentTag tag)
        {
          var view = tag.Document.GetElement(tag.OwnerViewId) as ARDB.View;
          var plane = new Plane(tag.TagHeadPosition.ToPoint3d(), view.RightDirection.ToVector3d(), view.UpDirection.ToVector3d());
          return plane;
        }

        return NaN.Plane;
      }
    }
    #endregion

    #region IAnnotationReferencesAcces
    public override GeometryObject[] References =>
      Value?.GetTaggedReferences().
      Cast<ARDB.Reference>().
      Select(GetGeometryObjectFromReference<GeometryElement>).
      ToArray();
    #endregion

    #region IAnnotationLeadersAcces
    public override bool? HasLeader
    {
      get => Value?.HasLeader;
      set
      {
        if (Value is ARDB.IndependentTag tag && value is object && value.Value != tag.HasLeader)
        {
          tag.HasLeader = value.Value;
          InvalidateGraphics();
        }
      }
    }

    public override AnnotationLeader[] Leaders
    {
      get
      {
        if (Value is object)
        {
          var references = References;
          var leaders = new AnnotationLeader[references.Length];
          for (int r = 0; r < leaders.Length; ++r)
            leaders[r] = new MultiLeader(this, references[r]);

          return leaders;
        }

        return null;
      }
    }

    class MultiLeader : AnnotationLeader
    {
      readonly IndependentTag element;
      readonly ARDB.Reference target;
      public MultiLeader(IndependentTag e, GeometryObject t) { element = e; target = t.GetReference(); }

      public override Point3d HeadPosition => element.Value.TagHeadPosition.ToPoint3d();

      public override bool Visible
      {
        get => element.Value.IsLeaderVisible(target);
        set => element.Value.SetIsLeaderVisible(target, value);
      }

      public override bool HasElbow => Visible;
      public override Point3d ElbowPosition
      {
        get => element.Value.HasLeaderElbow(target) ?
               element.Value.GetLeaderElbow(target).ToPoint3d() :
               (element.Value.TagHeadPosition.ToPoint3d() + EndPosition) * 0.5;
        set { if (Visible) element.Value.SetLeaderElbow(target, value.ToXYZ()); }
      }

      public override Point3d EndPosition
      {
        get
        {
          if (!Visible) return NaN.Point3d;

          if (element.Value.LeaderEndCondition == ARDB.LeaderEndCondition.Attached)
          {
            return Rhinoceros.InvokeInHostContext
            (
              () =>
              {
                using (element.Document.RollBackScope())
                using (element.Document.RollBackScope())
                {
                  element.Value.LeaderEndCondition = ARDB.LeaderEndCondition.Free;
                  return element.Value.GetLeaderEnd(target).ToPoint3d();
                }
              }
            );
          }

          return element.Value.GetLeaderEnd(target).ToPoint3d();
        }
        set { if (Visible) element.Value.SetLeaderEnd(target, value.ToXYZ()); }
      }

      public override bool IsTextPositionAdjustable => true;
      public override Point3d TextPosition
      {
        get => element.Value.TagHeadPosition.ToPoint3d();
        set => element.Value.TagHeadPosition = value.ToXYZ();
      }
    }
    #endregion

    protected override void SubInvalidateGraphics()
    {
      _LeaderCurves = null;

      base.SubInvalidateGraphics();
    }

    Curve[] _LeaderCurves;

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value?.TagHeadPosition is ARDB.XYZ headPosition)
      {
        var hasArrow = (Type.Value.get_Parameter(ARDB.BuiltInParameter.LEADER_ARROWHEAD)?.AsElementId()).IsValid();
        var dpi = args.Pipeline.DpiScale;
        var tagSize = 0.5; // feet
        var dotPixels = 10.0 * args.Pipeline.DpiScale;
        var arrowSize = (int) Math.Round(2.0 * Grasshopper.CentralSettings.PreviewPointRadius * dpi);

        if (_LeaderCurves is null)
          _LeaderCurves = Leaders.Where(x => x.Visible).Select(x => x.LeaderCurve).ToArray();

        foreach (var leaderCurve in _LeaderCurves)
        {
          args.Pipeline.DrawCurve(leaderCurve, args.Color, args.Thickness);

          if (!hasArrow) continue;
          args.Pipeline.DrawArrowHead(leaderCurve.PointAtEnd, leaderCurve.TangentAtEnd, args.Color, arrowSize, 0.0);
        }

        var head = headPosition.ToPoint3d();
        var pixelSize = ((1.0 / args.Pipeline.Viewport.PixelsPerUnit(head).X) / Revit.ModelUnits) / dpi;
        if (dotPixels * pixelSize > tagSize)
        {
          var color = System.Drawing.Color.White;
          if(Value.IsMaterialTag)
            color = System.Drawing.Color.FromArgb(254, 251, 219);
          else if(Value.IsMulticategoryTag)
            color = System.Drawing.Color.FromArgb(216, 238, 247);
          else
            color = System.Drawing.Color.FromArgb(216, 255, 216);

          var rotation = (float) -(Constant.Tau / 8.0);
          args.Pipeline.DrawPoint
          (
            head, PointStyle.Square,
            args.Color,
            color,
            (float) (tagSize / pixelSize),
            1.0f, 0.0f, rotation,
            diameterIsInPixels: true,
            autoScaleForDpi: false
          );
        }
        else
        {
          args.Pipeline.DrawDot
          (
            head,
            string.IsNullOrEmpty(Value.TagText) ? "?" : Value.TagText,
            args.Color, System.Drawing.Color.White
          );
        }
      }
    }
  }
}
