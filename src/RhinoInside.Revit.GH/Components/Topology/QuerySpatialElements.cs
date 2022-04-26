using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Topology
{
  public abstract class QuerySpatialElements : ElementCollectorComponent
  {
    protected internal static readonly ARDB.ElementFilter elementFilter =
      new ARDB.ElementClassFilter(typeof(ARDB.SpatialElement));
    protected override ARDB.ElementFilter ElementFilter => elementFilter;

    protected QuerySpatialElements(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Document",
          Optional = true
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Point",
          NickName = "P",
          Description = "Point to query.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Number",
          NickName = "Number",
          Description = "Element Number.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "Name",
          Description = "Element Name.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Level",
          NickName = "L",
          Description = "Level to query on.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Phase()
        {
          Name = "Phase",
          NickName = "P",
          Description = "Project phase to query on.",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.ElementFilter()
        {
          Name = "Filter",
          NickName = "F",
          Description = "Additional Filter.",
          Optional = true
        }, ParamRelevance.Occasional
      ),
    };
  }

  [ComponentVersion(introduced: "1.7")]
  public class QueryAreas : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("D1940EB3-B81B-4E57-8F5A-94D045BFB509");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    static readonly ARDB.ElementFilter elementFilter =
      QuerySpatialElements.elementFilter.Intersect(CompoundElementFilter.ElementClassFilter(typeof(ARDB.Area)));
    protected override ARDB.ElementFilter ElementFilter => elementFilter;
      
    public QueryAreas() : base
    (
      name: "Query Areas",
      nickname: "Areas",
      description: "Get document area elements list",
      category: "Revit",
      subCategory: "Topology"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Document",
          Optional = true
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Point",
          NickName = "P",
          Description = "Point to query.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Number",
          NickName = "Number",
          Description = "Element Number.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "Name",
          Description = "Element Name.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Level",
          NickName = "L",
          Description = "Level to query on.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.ElementFilter()
        {
          Name = "Filter",
          NickName = "F",
          Description = "Additional Filter.",
          Optional = true
        }, ParamRelevance.Occasional
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.AreaElement()
        {
          Name = "Areas",
          NickName = "A",
          Description = "Areas list",
          Access = GH_ParamAccess.list
        }
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      if (!Params.TryGetData(DA, "Point", out Point3d? point)) return;
      if (!Params.TryGetData(DA, "Number", out string number)) return;
      if (!Params.TryGetData(DA, "Name", out string name)) return;
      if (!Parameters.Level.TryGetDataOrDefault(this, DA, "Level", out Types.Level level, doc, point.HasValue ? point.Value.Z : double.NaN)) return;
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter)) return;

      var tol = GeometryObjectTolerance.Model;
      using (var collector = new ARDB.FilteredElementCollector(doc.Value))
      {
        var elementsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          elementsCollector = elementsCollector.WherePasses(filter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.ROOM_NUMBER, ref number, out var numberFilter))
          elementsCollector = elementsCollector.WherePasses(numberFilter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.ROOM_NAME, ref name, out var nameFilter))
          elementsCollector = elementsCollector.WherePasses(nameFilter);

        if (level is object)
          elementsCollector = elementsCollector.WhereParameterEqualsTo(ARDB.BuiltInParameter.ROOM_LEVEL_ID, level.Id);

        var areas = collector.Cast<ARDB.Area>().Where(x => x.Area > 0.0).Select(x => new Types.AreaElement(x));

        if (!string.IsNullOrEmpty(number))
          areas = areas.Where(x => x.Number.IsSymbolNameLike(number));

        if (!string.IsNullOrEmpty(name))
          areas = areas.Where(x => x.Name.IsSymbolNameLike(name));

        if (point.HasValue)
        {
          var xyz = point.Value.ToXYZ();
          areas = areas.Where
          (
            area =>
            {
              if (level is object)
              {
                var plane = area.Location;
                foreach (var boundary in area.Boundaries)
                {
                  var containment = boundary.Contains(point.Value, plane, NumericTolerance.DefaultTolerance);
                  if (containment == PointContainment.Inside)
                    return true;
                }
              }

              return false;
            }
          );
        }

        DA.SetDataList
        (
          "Areas",
          areas.
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }

  [ComponentVersion(introduced: "1.7")]
  public class QueryRooms : QuerySpatialElements
  {
    public override Guid ComponentGuid => new Guid("5DDCB816-61A3-480F-AC45-67F66BEB2E78");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    static new readonly ARDB.ElementFilter elementFilter =
      QuerySpatialElements.elementFilter.Intersect(CompoundElementFilter.ElementClassFilter(typeof(ARDB.Architecture.Room)));
    protected override ARDB.ElementFilter ElementFilter => elementFilter;

    public QueryRooms() : base
    (
      name: "Query Rooms",
      nickname: "Rooms",
      description: "Get document room elements list",
      category: "Revit",
      subCategory: "Topology"
    )
    { }

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.RoomElement()
        {
          Name = "Rooms",
          NickName = "R",
          Description = "Rooms list",
          Access = GH_ParamAccess.list
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;

      if (!Params.TryGetData(DA, "Point", out Point3d? point)) return;
      var xyz = point.HasValue ? point.Value.ToXYZ() : default;
      if (!Params.TryGetData(DA, "Number", out string number)) return;
      if (!Params.TryGetData(DA, "Name", out string name)) return;
      if (!Params.TryGetData(DA, "Level", out Types.Level level)) return;
      if (!Params.TryGetData(DA, "Phase", out Types.Phase phase)) return;
      if (phase is null && Params.IndexOfInputParam("Phase") < 0)
        phase = new Types.Phase(doc.Value.Phases.Cast<ARDB.Phase>().LastOrDefault());
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter)) return;

      var tol = GeometryObjectTolerance.Model;
      using (var collector = new ARDB.FilteredElementCollector(doc.Value))
      {
        var elementsCollector = collector.WherePasses(ElementFilter);

        if (xyz is object)
          elementsCollector = elementsCollector.WherePasses(new ARDB.BoundingBoxContainsPointFilter(xyz));

        if (filter is object)
          elementsCollector = elementsCollector.WherePasses(filter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.ROOM_NUMBER, ref number, out var numberFilter))
          elementsCollector = elementsCollector.WherePasses(numberFilter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.ROOM_NAME, ref name, out var nameFilter))
          elementsCollector = elementsCollector.WherePasses(nameFilter);

        if (level is object)
          elementsCollector = elementsCollector.WhereParameterEqualsTo(ARDB.BuiltInParameter.ROOM_LEVEL_ID, level.Id);

        if (phase is object)
          elementsCollector = elementsCollector.WhereParameterEqualsTo(ARDB.BuiltInParameter.ROOM_PHASE, phase.Id);

        var rooms = collector.Cast<ARDB.Architecture.Room>().Where(x => x.Area > 0.0);

        if (!string.IsNullOrEmpty(number))
          rooms = rooms.Where(x => x.Number.IsSymbolNameLike(number));

        if (!string.IsNullOrEmpty(name))
          rooms = rooms.Where(x => x.Name.IsSymbolNameLike(name));

        if (xyz is object)
          rooms = rooms.Where(room => room.IsPointInRoom(xyz));

        DA.SetDataList
        (
          "Rooms",
          rooms.
          Select(x => new Types.RoomElement(x)).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }

  [ComponentVersion(introduced: "1.7")]
  public class QuerySpaces : QuerySpatialElements
  {
    public override Guid ComponentGuid => new Guid("A1CCF034-AA1F-4731-9863-3C22E0644E2B");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    static new readonly ARDB.ElementFilter elementFilter =
      QuerySpatialElements.elementFilter.Intersect(CompoundElementFilter.ElementClassFilter(typeof(ARDB.Mechanical.Space)));
    protected override ARDB.ElementFilter ElementFilter => elementFilter;

    public QuerySpaces() : base
    (
      name: "Query Spaces",
      nickname: "Spaces",
      description: "Get document space elements list",
      category: "Revit",
      subCategory: "Topology"
    )
    { }

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.SpaceElement()
        {
          Name = "Spaces",
          NickName = "S",
          Description = "Spaces list",
          Access = GH_ParamAccess.list
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;

      if (!Params.TryGetData(DA, "Point", out Point3d? point)) return;
      var xyz = point.HasValue ? point.Value.ToXYZ() : default;
      if (!Params.TryGetData(DA, "Number", out string number)) return;
      if (!Params.TryGetData(DA, "Name", out string name)) return;
      if (!Params.TryGetData(DA, "Level", out Types.Level level)) return;
      if (!Params.TryGetData(DA, "Phase", out Types.Phase phase)) return;
      if (phase is null && Params.IndexOfInputParam("Phase") < 0)
        phase = new Types.Phase(doc.Value.Phases.Cast<ARDB.Phase>().LastOrDefault());
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter)) return;

      var tol = GeometryObjectTolerance.Model;
      using (var collector = new ARDB.FilteredElementCollector(doc.Value))
      {
        var elementsCollector = collector.WherePasses(ElementFilter);

        if (xyz is object)
          elementsCollector = elementsCollector.WherePasses(new ARDB.BoundingBoxContainsPointFilter(xyz));

        if (filter is object)
          elementsCollector = elementsCollector.WherePasses(filter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.ROOM_NUMBER, ref number, out var numberFilter))
          elementsCollector = elementsCollector.WherePasses(numberFilter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.ROOM_NAME, ref name, out var nameFilter))
          elementsCollector = elementsCollector.WherePasses(nameFilter);

        if (level is object)
          elementsCollector = elementsCollector.WhereParameterEqualsTo(ARDB.BuiltInParameter.ROOM_LEVEL_ID, level.Id);

        if (phase is object)
          elementsCollector = elementsCollector.WhereParameterEqualsTo(ARDB.BuiltInParameter.ROOM_PHASE, phase.Id);

        var spaces = collector.Cast<ARDB.Mechanical.Space>().Where(x => x.Area > 0.0);

        if (!string.IsNullOrEmpty(number))
          spaces = spaces.Where(x => x.Number.IsSymbolNameLike(number));

        if (!string.IsNullOrEmpty(name))
          spaces = spaces.Where(x => x.Name.IsSymbolNameLike(name));

        if (xyz is object)
          spaces = spaces.Where(room => room.IsPointInSpace(xyz));

        DA.SetDataList
        (
          "Spaces",
          spaces.
          Select(x => new Types.SpaceElement(x)).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
