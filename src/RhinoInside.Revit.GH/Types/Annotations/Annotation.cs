using System;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Annotation")]
  public interface IGH_Annotation : IGH_GraphicalElement
  {

  }

  interface IAnnotationReferencesAccess
  {
    /// <summary>
    /// Returns an array of geometric references to which the dimension is attached.
    /// </summary>
    GeometryObject[] References { get; }
  }

  #region AnnotationLeader
  public abstract class AnnotationLeader
  {
    public virtual Curve LeaderCurve => HasElbow ?
     new PolylineCurve(new Point3d[] { HeadPosition, ElbowPosition, EndPosition }) :
     new PolylineCurve(new Point3d[] { HeadPosition, EndPosition });

    public abstract Point3d HeadPosition { get; }

    public abstract bool Visible { get; set; }
    public abstract bool HasElbow { get; }
    public abstract Point3d ElbowPosition { get; set; }

    public abstract Point3d EndPosition { get; set; }

    public abstract bool IsTextPositionAdjustable { get; }
    public abstract Point3d TextPosition { get; set; }
  }

  interface IAnnotationLeadersAccess
  {
    bool? HasLeader { get; set; }

    /// <summary>
    /// Returns an array of geometric references to which the dimension is attached.
    /// </summary>
    AnnotationLeader[] Leaders { get; }
  }
  #endregion

  [Kernel.Attributes.Name("Annotation Type")]
  public abstract class AnnotationType : ElementType
  {
    public AnnotationType() { }
    protected internal AnnotationType(ARDB.ElementType type) : base(type) { }

    #region Text Properties
    public double? TextSize => Value?.get_Parameter(ARDB.BuiltInParameter.TEXT_SIZE)?.AsDouble();
    public string TextFont => Value?.get_Parameter(ARDB.BuiltInParameter.TEXT_FONT)?.AsString();
    public bool? TextStyleBold => Value?.get_Parameter(ARDB.BuiltInParameter.TEXT_STYLE_BOLD)?.AsBoolean();
    public bool? TextStyleItalic => Value?.get_Parameter(ARDB.BuiltInParameter.TEXT_STYLE_ITALIC)?.AsBoolean();
    public bool? TextStyleUnderline => Value?.get_Parameter(ARDB.BuiltInParameter.TEXT_STYLE_UNDERLINE)?.AsBoolean();
    public double? TextWidthScale => Value?.get_Parameter(ARDB.BuiltInParameter.TEXT_WIDTH_SCALE)?.AsDouble();
    #endregion
  }

  [Kernel.Attributes.Name("Annotation Type")]
  public class LineAndTextAttrSymbol : AnnotationType
  {
    protected override Type ValueType => typeof(ARDB.LineAndTextAttrSymbol);
    public new ARDB.LineAndTextAttrSymbol Value => base.Value as ARDB.LineAndTextAttrSymbol;

    public LineAndTextAttrSymbol() { }
    protected internal LineAndTextAttrSymbol(ARDB.LineAndTextAttrSymbol type) : base(type) { }
  }
}
