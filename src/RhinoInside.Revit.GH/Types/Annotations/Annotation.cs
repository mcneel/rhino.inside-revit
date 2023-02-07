using Rhino.Geometry;

namespace RhinoInside.Revit.GH.Types
{
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
}
