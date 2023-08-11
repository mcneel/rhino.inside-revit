using System;
using System.Linq;
using System.Collections.Generic;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.UI.Selection;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.GH.Components.Input
{
  [ComponentVersion(introduced: "1.17")]
  public class PickElements : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("F795D14C-E7AF-438B-9C57-332D0D8C4402");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override string IconTag => string.Empty;

    public PickElements() : base
    (
      name: "Pick Elements",
      nickname: "E-Pick",
      description: string.Empty,
      category: "Revit",
      subCategory: "Input"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.ElementFilter()
        {
          Name = "Filter",
          NickName = "F",
          Optional = true
        }, ParamRelevance.Primary
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GeometryObject()
        {
          Name = "Elements",
          NickName = "E",
          Description = "Picked elements",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Points",
          NickName = "P",
          Description = "Picked points",
          Hidden = true,
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
    };

    class SelectionFilter : ARUI.Selection.ISelectionFilter
    {
      readonly ARDB.ElementFilter Filter;
      public SelectionFilter( ARDB.ElementFilter filter ) => Filter = filter;

      public bool AllowElement(ARDB.Element elem) => Filter?.PassesFilter( elem ) ?? true;

      public bool AllowReference(ARDB.Reference reference, ARDB.XYZ position) => reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_NONE;
    }

    readonly GH_Structure<Types.IGH_GeometryObject> Elements = new GH_Structure<Types.IGH_GeometryObject>();
    readonly GH_Structure<GH_Point> Points = new GH_Structure<GH_Point>();

    protected override void BeforeSolveInstance()
    {
      base.BeforeSolveInstance();

      if ((Attributes as ButtonAttributes).Pressed)
      {
        Elements.Clear();
        Points.Clear();
      }
      else
      {
        foreach (var element in Elements.OfType<Types.IGH_ReferenceData>())
          element.LoadReferencedData();
      }
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var path = DA.ParameterTargetPath(0);
      Params.TryGetData(DA, "Filter", out Types.ElementFilter filter);

      if ((Attributes as ButtonAttributes).Pressed)
      {
        if (Revit.ActiveUIDocument is ARUI.UIDocument uiDocument)
        {
          var selectionFilter = new SelectionFilter(filter?.Value);
          if (uiDocument.PickObjects(out var references, selectionFilter) == ARUI.Result.Succeeded)
          {
            Elements.AppendRange
            (
              references.Select(x => Types.GeometryElement.FromReference(uiDocument.Document, x)),
              path
            );

            Points.AppendRange
            (
              references.Select(x => new GH_Point(x.GlobalPoint.ToPoint3d())),
              path
            );
          }
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "There is no active Revit document");
      }

      var pass = (Elements.get_Branch(path)?.OfType<Types.GeometryElement>() ?? Array.Empty<Types.GeometryElement>()).
        Select(x => filter?.Value.PassesFilter(x.Document, x.Id) ?? true).ToList();

      Params.TrySetDataList
      (
        DA, "Elements",
        () =>
        (Elements.get_Branch(path)?.OfType<Types.GeometryElement>() ?? Array.Empty<Types.GeometryElement>()).
        Zip(pass, (Value, Pass) => (Value, Pass)).
        Where(x => x.Pass).Select(x => x.Value)
      );

      Params.TrySetDataList
      (
        DA, "Points",
        () =>
        (Points.get_Branch(path)?.OfType<GH_Point>() ?? Array.Empty<GH_Point>()).
        Zip(pass, (Value, Pass) => (Value, Pass)).
        Where(x => x.Pass).Select(x => x.Value)
      );
    }

    #region Display
    public override BoundingBox ClippingBox => base.ClippingBox;
    public override void DrawViewportWires(IGH_PreviewArgs args)
    {
      base.DrawViewportWires(args);

      if (Params.Output<Param_Point>("Points") is Param_Point points)
      {
        if (Attributes.Selected)
        {
          var xform = args.Display.Viewport.GetTransform(Rhino.DocObjects.CoordinateSystem.World, Rhino.DocObjects.CoordinateSystem.Screen);
          var data = points.VolatileData;
          foreach (var path in data.Paths)
          {
            var pts = new List<Point3d>();
            var branch = data.get_Branch(path);
            for (int i = 0; i < branch.Count; i++)
            {
              if (branch[i] is GH_Point point)
              {
                pts.Add(point.Value);
                var pt = xform * point.Value;
                args.Display.Draw2dText($"{path} ({i})", args.WireColour_Selected, new Point2d(pt.X, pt.Y - 24), true, 24);
              }
            }

            args.Display.DrawPatternedPolyline(pts, args.WireColour_Selected, 0x00003333, args.DefaultCurveThickness, false);
          }
        }

        points.DrawViewportWires(args);
      }
    }
    #endregion

    #region UI
    private class ButtonAttributes : ExpireButtonAttributes
    {
      public ButtonAttributes(ZuiComponent owner) : base(owner) { }

      protected override string DisplayText => "Pick Elements";

      protected override bool Visible => true;
    }

    public override void CreateAttributes() => m_attributes = new ButtonAttributes(this);
    #endregion

    #region IO
    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader)) return false;

      Elements.Clear();
      if (reader.FindChunk(nameof(Elements)) is GH_IReader chunk)
        Elements.Read(chunk);

      Points.Clear();
      if (reader.FindChunk(nameof(Points)) is GH_IReader points)
        Points.Read(points);

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer)) return false;

      if (Elements.Branches.Count > 0)
        Elements.Write(writer.CreateChunk(nameof(Elements)));

      if (Points.Branches.Count > 0)
        Points.Write(writer.CreateChunk(nameof(Points)));

      return true;
    }
    #endregion
  }
}
