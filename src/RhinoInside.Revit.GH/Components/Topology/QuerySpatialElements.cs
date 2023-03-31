using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Topology
{
  using Convert.Geometry;
  using External.DB;
  using External.DB.Extensions;

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
        new Param_Boolean()
        {
          Name = "Placed",
          NickName = "PD",
          Description = "Wheter element is placed or not.",
          Optional = true
        }.SetDefaultVale(true), ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Number",
          NickName = "NUM",
          Description = "Element Number.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Element Name.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Level",
          NickName = "LV",
          Description = "Level to query on.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Phase()
        {
          Name = "Phase",
          NickName = "PH",
          Description = "Project phase to query on.",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Enclosed",
          NickName = "ED",
          Description = "Wheter element is on a properly enclosed region or not.",
          Optional = true
        }.SetDefaultVale(true), ParamRelevance.Secondary
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

  [ComponentVersion(introduced: "1.7", updated: "1.12")]
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
        new Param_Boolean()
        {
          Name = "Placed",
          NickName = "PD",
          Description = "Wheter element is placed or not.",
          Optional = true
        }.SetDefaultVale(true), ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Number",
          NickName = "NUM",
          Description = "Element Number.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Element Name.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Level",
          NickName = "LV",
          Description = "Level to query on.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Area Scheme",
          NickName = "AS",
          Description = "Only areas on a specific Area Scheme.",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Enclosed",
          NickName = "ED",
          Description = "Wheter area defines a properly enclosed region or not.",
          Optional = true
        }.SetDefaultVale(true), ParamRelevance.Secondary
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
      if (!Params.TryGetData(DA, "Placed", out bool? placed)) return;
      if (!Params.TryGetData(DA, "Number", out string number)) return;
      if (!Params.TryGetData(DA, "Name", out string name)) return;
      if (!Params.TryGetData(DA, "Level", out Types.Level level)) return;
      if (!Params.TryGetData(DA, "Area Scheme", out ARDB.AreaScheme scheme)) return;
      if (!Params.TryGetData(DA, "Enclosed", out bool? enclosed)) return;
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter)) return;

      if (point.HasValue)
      {
        placed = true;
        enclosed = true;
      }

      var tol = GeometryTolerance.Model;
      using (var collector = new ARDB.FilteredElementCollector(doc.Value))
      {
        var elementsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          elementsCollector = elementsCollector.WherePasses(filter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.ROOM_NUMBER, ref number, out var numberFilter))
          elementsCollector = elementsCollector.WherePasses(numberFilter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.ROOM_NAME, ref name, out var nameFilter))
          elementsCollector = elementsCollector.WherePasses(nameFilter);

        if (enclosed.HasValue)
        {
          var rule = new ARDB.FilterDoubleRule
          (
            new ARDB.ParameterValueProvider(new ARDB.ElementId(ARDB.BuiltInParameter.ROOM_PERIMETER)),
            new ARDB.FilterNumericGreater(),
            0.0, 0.0
          );

          elementsCollector = elementsCollector.WherePasses(new ARDB.ElementParameterFilter(rule, !enclosed.Value));
        }

        if (level is object)
          elementsCollector = elementsCollector.WhereParameterEqualsTo(ARDB.BuiltInParameter.ROOM_LEVEL_ID, level.Id);

        var areas = collector.Select(x => new Types.AreaElement(x as ARDB.Area));

        if (placed.HasValue)
          areas = areas.Where(x => x.IsPlaced == placed.Value);

        if (!string.IsNullOrEmpty(number))
          areas = areas.Where(x => x.Number.IsSymbolNameLike(number));

        if (!string.IsNullOrEmpty(name))
          areas = areas.Where(x => x.Name.IsSymbolNameLike(name));

        if (scheme is object)
          areas = areas.Where(x => x.Value.AreaScheme.IsEquivalent(scheme));

        if (point.HasValue)
        {
          var xyz = point.Value.ToXYZ();
          areas = areas.Where
          (
            area =>
            {
              var plane = area.Location;
              var count = 0;
              foreach (var boundary in area.Boundaries)
              {
                var containment = boundary.Contains(point.Value, plane, GeometryTolerance.Model.VertexTolerance);
                if (containment == PointContainment.Inside)
                  count++;
              }

              return count % 2 != 0;
            }
          );
        }

        DA.SetDataList("Areas", areas.TakeWhileIsNotEscapeKeyDown(this));
      }
    }
  }

  [ComponentVersion(introduced: "1.12")]
  public class QueryAreaSchemes : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("3E2A753B-6321-4C4C-B697-022D1F04F213");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    static readonly ARDB.ElementFilter elementFilter = CompoundElementFilter.ElementClassFilter(typeof(ARDB.AreaScheme));
    protected override ARDB.ElementFilter ElementFilter => CompoundElementFilter.ElementClassFilter(typeof(ARDB.AreaScheme));

    public QueryAreaSchemes() : base
    (
      name: "Query Area Schemes",
      nickname: "AreaSchemes",
      description: "Get document area shemes list",
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
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Element Name.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Gross Building",
          NickName = "GB",
          Description = "Only Gross Building Area schemes.",
          Optional = true
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Area Schemes",
          NickName = "AS",
          Description = "Area Schemes list",
        }
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      if (!Params.TryGetData(DA, "Name", out string name)) return;
      if (!Params.TryGetData(DA, "Gross Building", out bool? gross)) return;

      var tol = GeometryTolerance.Model;
      using (var collector = new ARDB.FilteredElementCollector(doc.Value))
      {
        var elementsCollector = collector.WherePasses(ElementFilter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.AREA_SCHEME_NAME, ref name, out var nameFilter))
          elementsCollector = elementsCollector.WherePasses(nameFilter);

        var areas = collector.Select(x => Types.Element.FromElement(x as ARDB.AreaScheme));

        if (gross.HasValue)
          areas = areas.Where(x => (x.Value as ARDB.AreaScheme).IsGrossBuildingArea == gross);

        if (!string.IsNullOrEmpty(name))
          areas = areas.Where(x => x.Nomen.IsSymbolNameLike(name));

        DA.SetDataList("Area Schemes", areas.TakeWhileIsNotEscapeKeyDown(this));
      }
    }
  }

  [ComponentVersion(introduced: "1.7", updated: "1.9")]
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
      if (!Params.TryGetData(DA, "Placed", out bool? placed)) return;
      if (!Params.TryGetData(DA, "Number", out string number)) return;
      if (!Params.TryGetData(DA, "Name", out string name)) return;
      if (!Params.TryGetData(DA, "Level", out Types.Level level)) return;
      if (!Params.TryGetData(DA, "Phase", out Types.Phase phase)) return;
      if (phase is null && Params.IndexOfInputParam("Phase") < 0)
        phase = new Types.Phase(doc.Value.Phases.Cast<ARDB.Phase>().LastOrDefault());
      if (!Params.TryGetData(DA, "Enclosed", out bool? enclosed)) return;
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter)) return;

      if (point.HasValue)
      {
        placed = true;
        enclosed = true;
      }

      var tol = GeometryTolerance.Model;
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

        if (enclosed.HasValue)
        {
          var rule = new ARDB.FilterDoubleRule
          (
            new ARDB.ParameterValueProvider(new ARDB.ElementId(ARDB.BuiltInParameter.ROOM_PERIMETER)),
            new ARDB.FilterNumericGreater(),
            0.0, 0.0
          );

          elementsCollector = elementsCollector.WherePasses(new ARDB.ElementParameterFilter(rule, !enclosed.Value));
        }

        var rooms = collector.Select(x => new Types.RoomElement(x as ARDB.Architecture.Room));

        if (placed.HasValue)
          rooms = rooms.Where(x => x.IsPlaced == placed.Value);

        if (!string.IsNullOrEmpty(number))
          rooms = rooms.Where(x => x.Number.IsSymbolNameLike(number));

        if (!string.IsNullOrEmpty(name))
          rooms = rooms.Where(x => x.Name.IsSymbolNameLike(name));

        if (xyz is object)
          rooms = rooms.Where(room => room.Value.IsPointInRoom(xyz));

        DA.SetDataList("Rooms",rooms.TakeWhileIsNotEscapeKeyDown(this));
      }
    }
  }

  [ComponentVersion(introduced: "1.7", updated: "1.9")]
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
      if (!Params.TryGetData(DA, "Placed", out bool? placed)) return;
      if (!Params.TryGetData(DA, "Number", out string number)) return;
      if (!Params.TryGetData(DA, "Name", out string name)) return;
      if (!Params.TryGetData(DA, "Level", out Types.Level level)) return;
      if (!Params.TryGetData(DA, "Phase", out Types.Phase phase)) return;
      if (phase is null && Params.IndexOfInputParam("Phase") < 0)
        phase = new Types.Phase(doc.Value.Phases.Cast<ARDB.Phase>().LastOrDefault());
      if (!Params.TryGetData(DA, "Enclosed", out bool? enclosed)) return;
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter)) return;

      if (point.HasValue)
      {
        placed = true;
        enclosed = true;
      }

      var tol = GeometryTolerance.Model;
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

        if (enclosed.HasValue)
        {
          var rule = new ARDB.FilterDoubleRule
          (
            new ARDB.ParameterValueProvider(new ARDB.ElementId(ARDB.BuiltInParameter.ROOM_PERIMETER)),
            new ARDB.FilterNumericGreater(),
            0.0, 0.0
          );

          elementsCollector = elementsCollector.WherePasses(new ARDB.ElementParameterFilter(rule, !enclosed.Value));
        }

        var spaces = collector.Select(x => new Types.SpaceElement(x as ARDB.Mechanical.Space));

        if (placed.HasValue)
          spaces = spaces.Where(x => x.IsPlaced == placed.Value);

        if (!string.IsNullOrEmpty(number))
          spaces = spaces.Where(x => x.Number.IsSymbolNameLike(number));

        if (!string.IsNullOrEmpty(name))
          spaces = spaces.Where(x => x.Name.IsSymbolNameLike(name));

        if (xyz is object)
          spaces = spaces.Where(room => room.Value.IsPointInSpace(xyz));

        DA.SetDataList("Spaces", spaces.TakeWhileIsNotEscapeKeyDown(this));
      }
    }
  }
}
