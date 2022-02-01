using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using ARDBSectionBox = Autodesk.Revit.DB.Element;

  [Kernel.Attributes.Name("Section Box")]
  class SectionBox : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDBSectionBox);
    public new ARDBSectionBox Value => base.Value as ARDBSectionBox;

    protected override bool SetValue(ARDBSectionBox element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDBSectionBox element)
    {
      return element.GetType() == typeof(ARDBSectionBox) &&
             element.Category?.Id.IntegerValue == (int) ARDB.BuiltInCategory.OST_SectionBox;
    }

    public SectionBox() { }
    public SectionBox(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public SectionBox(ARDBSectionBox box) : base(box)
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
        if (Value is ARDBSectionBox box)
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
        if (Value is ARDBSectionBox box)
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
