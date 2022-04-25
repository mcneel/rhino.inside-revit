using System;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  [Kernel.Attributes.Name("Spatial Element Tag")]
  public class SpatialElementTag : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.SpatialElementTag);
    public new ARDB.SpatialElementTag Value => base.Value as ARDB.SpatialElementTag;

    public SpatialElementTag() { }
    public SpatialElementTag(ARDB.SpatialElementTag element) : base(element) { }

    public override string DisplayName => Value is ARDB.SpatialElementTag tag && tag.IsOrphaned ?
      base.DisplayName + " (Orphaned)" : base.DisplayName;

    #region Location
    public override Plane Location
    {
      get
      {
        if (Value is ARDB.SpatialElementTag tag && tag.Location is ARDB.LocationPoint point)
        {
          var plane = new Plane(point.Point.ToPoint3d(), Vector3d.XAxis, Vector3d.YAxis);
          plane.Rotate(tag.RotationAngle, -tag.View.ViewDirection.ToVector3d());
          return plane;
        }

        return NaN.Plane;
      }
    }

    public override Level Level => Level.FromElement(Value?.View.GenLevel) as Level;
    #endregion
  }

  [Kernel.Attributes.Name("Area Tag")]
  public class AreaElementTag : SpatialElementTag
  {
    protected override Type ValueType => typeof(ARDB.AreaTag);
    public new ARDB.AreaTag Value => base.Value as ARDB.AreaTag;

    public AreaElementTag() { }
    public AreaElementTag(ARDB.AreaTag element) : base(element) { }
  }

  [Kernel.Attributes.Name("Room Tag")]
  public class RoomElementTag : SpatialElementTag
  {
    protected override Type ValueType => typeof(ARDB.Architecture.RoomTag);
    public new ARDB.Architecture.RoomTag Value => base.Value as ARDB.Architecture.RoomTag;

    public RoomElementTag() { }
    public RoomElementTag(ARDB.Architecture.RoomTag element) : base(element) { }
  }

  [Kernel.Attributes.Name("Space Tag")]
  public class SpaceElementTag : SpatialElementTag
  {
    protected override Type ValueType => typeof(ARDB.Mechanical.SpaceTag);
    public new ARDB.Mechanical.SpaceTag Value => base.Value as ARDB.Mechanical.SpaceTag;

    public SpaceElementTag() { }
    public SpaceElementTag(ARDB.Mechanical.SpaceTag element) : base(element) { }
  }
}
