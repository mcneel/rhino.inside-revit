using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using ARDB_SectionBox = ARDB.Element;

  [Kernel.Attributes.Name("Section Box")]
  class SectionBox : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB_SectionBox);
    public new ARDB_SectionBox Value => base.Value as ARDB_SectionBox;

    protected override bool SetValue(ARDB_SectionBox element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB_SectionBox element)
    {
      return element.GetType() == typeof(ARDB_SectionBox) &&
             element.Category?.Id.IntegerValue == (int) ARDB.BuiltInCategory.OST_SectionBox;
    }

    public SectionBox() { }
    public SectionBox(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public SectionBox(ARDB_SectionBox box) : base(box)
    {
      if (!IsValidElement(box))
        throw new ArgumentException("Invalid Element", nameof(box));
    }

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var box = Box;
      if (box.IsValid)
        args.Pipeline.DrawBox(box, args.Color, args.Thickness);
    }
    #endregion

    #region Properties
    public override BoundingBox BoundingBox
    {
      get
      {
        if (Value is ARDB_SectionBox box)
        {
          if (box.GetFirstDependent<ARDB.View>() is ARDB.View3D view)
          {
            var sectionBox = view.GetSectionBox();
            sectionBox.Enabled = true;
            return sectionBox.ToBoundingBox();
          }
        }

        return NaN.BoundingBox;
      }
    }

    public override Box Box
    {
      get
      {
        if (Value is ARDB_SectionBox box)
        {
          if (box.GetFirstDependent<ARDB.View>() is ARDB.View3D view)
          {
            var sectionBox = view.GetSectionBox();
            sectionBox.Enabled = true;
            return sectionBox.ToBox();
          }
        }

        return NaN.Box;
      }
    }

    public override Plane Location
    {
      get
      {
        var box = Box;
        return box.IsValid ? box.Plane : NaN.Plane;
      }
    }
    #endregion
  }
}
