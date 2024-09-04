using System;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Model Text")]
  public class ModelText : GeometricElement
  {
    protected override Type ValueType => typeof(ARDB.ModelText);
    public new ARDB.ModelText Value => base.Value as ARDB.ModelText;

    public ModelText() : base() { }
    public ModelText(ARDB.ModelText modelText) : base(modelText) { }

    #region Location
    public override Plane Location
    {
      get
      {
        if (Value is ARDB.ModelText modelText)
        {
          using (var options = new ARDB.Options { DetailLevel = ARDB.ViewDetailLevel.Undefined })
          {
            var geometry = modelText.get_Geometry(options);
            if (geometry.TryGetLocation(out var gO, out var gX, out var gY))
            {
              var xform = ARDB.Transform.Identity;
              xform.SetAlignCoordSystem
              (
                ARDB.XYZ.Zero, External.DB.UnitXYZ.BasisX, External.DB.UnitXYZ.BasisY, External.DB.UnitXYZ.BasisZ,
                gO, gX, gY, gX.CrossProduct(gY).ToUnitXYZ()
              );

              var box = geometry.GetBoundingBox(xform);

              var origin = (box.GetCenter() - box.Transform.BasisZ * 0.5 * (box.Max.Z - box.Min.Z));
              switch (modelText.HorizontalAlignment)
              {
                case ARDB.HorizontalAlign.Left: origin = box.GetCorners()[3]; break;
                case ARDB.HorizontalAlign.Right: origin = box.GetCorners()[2]; break;
                case ARDB.HorizontalAlign.Center: origin = (box.GetCorners()[3] + box.GetCorners()[2]) * 0.5; break;
              }

              return new Plane(origin.ToPoint3d(), box.Transform.BasisX.ToVector3d(), box.Transform.BasisY.ToVector3d());
            }
          }

          var bbox = BoundingBox;
          if (bbox.IsValid)
            return new Plane(BoundingBox.Center, Vector3d.XAxis, Vector3d.YAxis);
        }

        return NaN.Plane;
      }
    }
    #endregion
  }
}
