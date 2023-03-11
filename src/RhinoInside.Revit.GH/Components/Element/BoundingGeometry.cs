using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Geometry
{
  using Convert.Geometry;
  using External.DB.Extensions;

  public class ElementBoundingGeometry : Component
  {
    public override Guid ComponentGuid => new Guid("3396DBC4-0E8F-4402-969A-EF5A0E30E093");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "BG";

    public ElementBoundingGeometry() : base
    (
      name: "Element Bounding Geometry",
      nickname: "EBG",
      description: "Bounding geometry of given element",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter
      (
        param: new Parameters.GraphicalElement(),
        name: "Element",
        nickname: "E",
        description: "Element with complex geometry",
        access: GH_ParamAccess.item
      );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddBrepParameter
      (
        name: "Bounding Geometry",
        nickname: "BG",
        description: "Element Bounding geometry",
        access: GH_ParamAccess.item
      );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // grab input wall type
      ARDB.Element element = default;
      if (!DA.GetData("Element", ref element))
        return;

      switch (element)
      {
        case ARDB.Wall wall:
          // extract the bounding geometry of the wall and set on output
          DA.SetData("Bounding Geometry", ComputeWallBoundingGeometry(wall));
          break;

        // TODO: implement other elements that might have interesting bounding geometries e.g. floors, roofs, ...
      }
    }

    /// <summary>
    /// Return bounding geometry of the wall
    /// Bounding geometry is the geometry wrapping Stacked or Curtain walls (different from Bounding Box).
    /// For Basic Walls the bounding geometry is identical to the default wall geometry
    /// </summary>
    /// <returns>Bounding geometry of a wall</returns>
    static Brep ComputeWallBoundingGeometry(ARDB.Wall wall)
    {
      // TODO: brep creation might be crude and could use performance improvements

      // extract global properties
      // e.g. base height, thickness, ...
      var height = wall.get_Parameter(ARDB.BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble() * Revit.ModelUnits;
      var thickness = wall.GetWidth() * Revit.ModelUnits;
      // construct a base offset plane that is used later to offset base curves
      var offsetPlane = Plane.WorldXY;
      var baseElevation = wall.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_OFFSET).AsDouble();

      // calculate slant
      double topOffset = double.NaN;
#if REVIT_2021
      // if wall slant is supported grab the slant angle
      var slantParam = wall.get_Parameter(ARDB.BuiltInParameter.WALL_SINGLE_SLANT_ANGLE_FROM_VERTICAL);
      if (slantParam is ARDB.Parameter)
      {
        // and calculate the cuvre offset at the top based on the curve slant angle
        //     O = top offset distance
        // ---------
        //  \      |
        //   \  S = slant angle
        //    \    |
        //     \   | H = wall height
        //      \  |
        //       \ |
        //        \|
        var slantAngle = slantParam.AsDouble();
        if (slantAngle > 0)
          topOffset = height * (Math.Sin(slantAngle) / Math.Abs(Math.Cos(slantAngle)));
      }
#endif

      // get the base curve of wall (center curve), and wall thickness
      // this will be used to create a bottom-profile of the wall
      var baseCurve = ((ARDB.LocationCurve) wall.Location).Curve.ToCurve();
      // transform to where the wall base is
      baseCurve.Translate(0, 0, baseElevation);

      // create the base curves on boths sides
      var side1BottomCurve = baseCurve.Offset(offsetPlane, thickness / 2.0, 0.1, CurveOffsetCornerStyle.None)[0];
      var side2BottomCurve = baseCurve.Offset(offsetPlane, thickness / -2.0, 0.1, CurveOffsetCornerStyle.None)[0];

      // create top curves, by moving a duplicate of base curves to top
      var fromPoint = side1BottomCurve.PointAtStart;
      var side1TopCurve = side1BottomCurve.DuplicateCurve();
      side1TopCurve.Translate(0, 0, fromPoint.Z + height);
      var side2TopCurve = side2BottomCurve.DuplicateCurve();
      side2TopCurve.Translate(0, 0, fromPoint.Z + height);

      // offset the top curves to get the slanted wall top curves, based on the previously calculated offset distance
      if (topOffset > 0)
      {
        side1TopCurve = side1TopCurve.Offset(offsetPlane, topOffset, 0.1, CurveOffsetCornerStyle.None)[0];
        side2TopCurve = side2TopCurve.Offset(offsetPlane, topOffset, 0.1, CurveOffsetCornerStyle.None)[0];
      }

      // build a list of curve-pairs for the 6 sides
      var sideCurvePairs = new List<Tuple<Curve, Curve>>()
      {
        // side 1
        new Tuple<Curve, Curve>(side1BottomCurve, side1TopCurve),
        // side 2
        new Tuple<Curve, Curve>(side2BottomCurve, side2TopCurve),
        // bottom
        new Tuple<Curve, Curve>(side1BottomCurve, side2BottomCurve),
        // start side
        new Tuple<Curve, Curve>(
          new Line(side1BottomCurve.PointAtStart, side1TopCurve.PointAtStart).ToNurbsCurve(),
          new Line(side2BottomCurve.PointAtStart, side2TopCurve.PointAtStart).ToNurbsCurve()
        ),
        // top
        new Tuple<Curve, Curve>(side1TopCurve, side2TopCurve),
        // end side
        new Tuple<Curve, Curve>(
          new Line(side1TopCurve.PointAtEnd, side1BottomCurve.PointAtEnd).ToNurbsCurve(),
          new Line(side2TopCurve.PointAtEnd, side2BottomCurve.PointAtEnd).ToNurbsCurve()
        )
      };

      // build breps for each side and add to list
      var finalBreps = new List<Brep>();
      foreach (var curvePair in sideCurvePairs)
      {
        var loft = Brep.CreateFromLoft(
            curves: new List<Curve>() { curvePair.Item1, curvePair.Item2 },
            start: Point3d.Unset,
            end: Point3d.Unset,
            loftType: LoftType.Normal,
            closed: false
            ).First(); // grab the first loft
        finalBreps.Add(loft);
      }
      // join all the breps into one
      return Brep.JoinBreps(finalBreps, 0.1).First();
    }
  }

  [ComponentVersion(introduced: "1.13")]
#if DEBUG
  public
#endif
  class ElementGeometryReferences : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("BBD8187B-829A-4604-B6BC-DE896A9FF62B");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.hidden;
    protected override string IconTag => "GR";

    public ElementGeometryReferences() : base
    (
      name: "Element Geometry References",
      nickname: "G-References",
      description: "Retrieves geometry references of given element.",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to deconstruct",
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Invisble",
          NickName = "I",
          Description = "Include non visible geometry",
          Optional = true
        }.SetDefaultVale(false), ParamRelevance.Secondary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Deconstructed element",
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.GeometryFace()
        {
          Name = "Faces",
          NickName = "F",
          Description = "List of element face references",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GeometryCurve()
        {
          Name = "Curves",
          NickName = "C",
          Description = "List of element curve references",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GeometryPoint()
        {
          Name = "Points",
          NickName = "P",
          Description = "List of element point references",
          Access = GH_ParamAccess.list
        },ParamRelevance.Secondary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.GraphicalElement element)) return;
      else Params.TrySetData(DA, "Element", () => element);

      if (!Params.TryGetData(DA, "Invisble", out bool? invisibles)) return;

      using (var options = new ARDB.Options() { ComputeReferences = true, IncludeNonVisibleObjects = invisibles ?? false })
      {
        if (element.Value.GetGeometry(options) is ARDB.GeometryElement geometry)
        {
          Params.TrySetDataList(DA, "Faces", () =>
            geometry.GetFaceReferences(element.Value).Select(element.GetGeometryObjectFromReference<Types.GeometryFace>));

          Params.TrySetDataList(DA, "Curves", () =>
            geometry.GetEdgeReferences(element.Value).Select(element.GetGeometryObjectFromReference<Types.GeometryCurve>));

          Params.TrySetDataList(DA, "Points", () =>
            geometry.GetEdgeEndPointReferences(element.Value).Select(element.GetGeometryObjectFromReference<Types.GeometryPoint>));
        }
      }
    }
  }
}
