using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Geometry
{
  using Convert.Geometry;
  using Convert.System.Collections.Generic;
  using External.DB;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.0", updated: "1.4")]
  public abstract class ElementGeometryComponent : ZuiComponent
  {
    protected ElementGeometryComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected bool TryGetCommonDocument(IEnumerable<Types.Element> elements, out ARDB.Document document)
    {
      document = default;
      foreach (var element in elements)
      {
        if (element is null) continue;
        if (document is null) document = element.Document;
        else if (!document.Equals(element.Document))
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input elements should be from the same document");
          return false;
        }
      }

      return true;
    }

    internal static IGH_GeometricGoo ToGeometricGoo(GeometryBase geometry)
    {
      switch (geometry)
      {
        case Point point: return new GH_Point(point.Location);
        case Curve curve: return new GH_Curve(curve);
        case Brep brep: return new GH_Brep(brep);
        case SubD subD: return new GH_SubD(subD);
        case Mesh mesh: return new GH_Mesh(mesh);
      }

      return default;
    }

    static readonly ARDB.ElementFilter ElementHasGeometryFilter = CompoundElementFilter.Intersect
    (
      // Not 100% sure but looks like only elements with category have geometry.
      CompoundElementFilter.ElementHasCategoryFilter,
      CompoundElementFilter.ElementHasBoundingBoxFilter,
      // Types below return no geometry.
      new ARDB.ElementMulticlassFilter
      (
        new Type[]
        {
          typeof(ARDB.CurveElement),
          typeof(ARDB.DatumPlane)
        },
        inverted: true
      )
    );

    protected void SolveGeometry
    (
      ARDB.Document doc,
      IList<Types.Element> include,
      IList<Types.Element> exclude,
      ARDB.Options options,
      GH_Path elementsPath, out GH_Structure<Types.Element> elements,
      GH_Path geometriesPath, out GH_Structure<IGH_GeometricGoo> geometries
    )
    {
      elements = elementsPath is object ? new GH_Structure<Types.Element>() : default;
      geometries = new GH_Structure<IGH_GeometricGoo>();

      // Fill data tree
      using (var scope = exclude?.Count > 0 ? doc.RollBackScope() : default)
      {
        if (scope is object)
        {
          if (doc.Delete(exclude.ConvertAll(x => x?.Id ?? ARDB.ElementId.InvalidElementId)).Count > 0)
            doc.Regenerate();
        }

        using (var visibleInViewFilter = options.View is object ? new ARDB.VisibleInViewFilter(options.View.Document, options.View.Id) : default)
        {
          SolveGeometry
          (
            include.Select(x => x?.Value),
            visibleInViewFilter,
            options,
            elementsPath, elements,
            geometriesPath, geometries
          );
        }
      }
    }

    void SolveGeometry
    (
      IEnumerable<ARDB.Element> include,
      ARDB.VisibleInViewFilter visibleInViewFilter,
      ARDB.Options options,
      GH_Path elementsPath, GH_Structure<Types.Element> elements,
      GH_Path geometriesPath, GH_Structure<IGH_GeometricGoo> geometries,
      int level = 0
    )
    {
      var index = level;
      foreach (var element in include)
      {
        if (level == 0 && GH_Document.IsEscapeKeyDown())
        {
          OnPingDocument()?.RequestAbortSolution();
          return;
        }

        var elePath = elements is object ? elementsPath.AppendElement(index) : default;
        if (level == 0 && ExpandDependents) elePath = elePath?.AppendElement(0);
        elements?.EnsurePath(elePath);

        var geoPath = geometriesPath.AppendElement(index++);
        if (level == 0 && ExpandDependents) geoPath = geoPath.AppendElement(0);
        geometries.EnsurePath(geoPath);

        if
        (
          element is object &&
          (
            level > 0 ||
            (
              // 'Element Geometry' works with types.
              (options.View is null || !(element is ARDB.ElementType)) &&
              ElementHasGeometryFilter.PassesFilter(element) &&
              visibleInViewFilter?.PassesFilter(element) != false
            )
          )
        )
        {
          // Extract the geometry
          var geometryBase = new List<GeometryBase>();
          if (false != ExtractGeometry(element, options, geometryBase))
          {
            geometries.AppendRange(geometryBase.Select(ToGeometricGoo), geoPath);

            // Extract dependent geometry
            if (level == 0 && ExpandDependents)
            {
              var dependents = element.GetDependentElements
              (
                CompoundElementFilter.Intersect
                (
                  // 'Element Geometry' works with types, but not when expanding dependents.
                  CompoundElementFilter.ElementIsElementTypeFilter(inverted: true),
                  ElementHasGeometryFilter,
                  new ARDB.ExclusionFilter(new ARDB.ElementId[] { element.Id })
                )
              );

              SolveGeometry
              (
                dependents.Select(x => element.Document.GetElement(x)).
                Where
                (
                  x =>
                  (x as ARDB.FamilyInstance)?.Invisible != true &&
                  (visibleInViewFilter?.PassesFilter(x) != false)
                ),
                visibleInViewFilter, options,
                elePath?.CullElement(), elements,
                geoPath.CullElement(), geometries,
                level: 1
              );
            }
          }
        }

        elements?.Append(Types.Element.FromElement(element), elePath);
      }
    }

    static bool? ExtractGeometry
    (
      ARDB.Element element, ARDB.Options options,
      List<GeometryBase> list
    )
    {
      using (var geometry = element.GetGeometry(options))
      {
        if (geometry is null)
          return default;

        using (var context = GeometryDecoder.Context.Push())
        {
          context.Element = element;
          if (element.Category is ARDB.Category category)
          {
            context.Category = category;
            context.Material = category.Material;
          }

          list.AddRange
          (
            geometry.
            ToGeometryBaseMany
            (
              x =>
              options.View is null ||
              !options.View.GetCategoryHidden(GeometryDecoder.Context.Peek.Category.Id)
            ).
            Where(x => !x.IsNullOrEmpty())
          );
        }

        return true;
      }
    }

    protected void SolveAttributes
    (
      ARDB.Document doc,
      GH_Structure<IGH_GeometricGoo> geometries,
      GH_Structure<Types.Category> categories,
      GH_Structure<Types.Material> materials
    )
    {
      if (categories is object || materials is object)
      {
        foreach (var path in geometries.Paths)
        {
          var branch = geometries.get_Branch(path);

          var categoriesList = categories?.EnsurePath(path);
          var materialsList = materials?.EnsurePath(path);

          foreach (var geometry in branch.Cast<IGH_GeometricGoo>())
          {
            var geometryBase = geometry?.ScriptVariable() as GeometryBase;
            {
              if (categories is object)
              {
                if (geometryBase is null) categoriesList.Add(null);
                else
                {
                  geometryBase.TryGetUserString(ARDB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY.ToString(), out ARDB.ElementId categoryId);
                  categoriesList.Add(new Types.Category(doc, categoryId));
                }
              }

              if (materials is object)
              {
                if (geometryBase is null) materialsList.Add(null);
                else
                {
                  geometryBase.TryGetUserString(ARDB.BuiltInParameter.MATERIAL_ID_PARAM.ToString(), out ARDB.ElementId materialId);
                  materialsList.Add(new Types.Material(doc, materialId));
                }
              }
            }
          }
        }
      }
    }

    #region IO
    bool _expandDependents = false;
    protected bool ExpandDependents
    {
      get => _expandDependents;
      set
      {
        _expandDependents = value;
        Message = value ? "Dependents" : string.Empty;
      }
    }

    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      bool includeDependents = false;
      reader.TryGetBoolean("ExpandDependents", ref includeDependents);
      ExpandDependents = includeDependents;

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (ExpandDependents)
        writer.SetBoolean("ExpandDependents", ExpandDependents);

      return true;
    }
    #endregion

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      Menu_AppendItem
      (
        menu, "Expand Dependents",
        (sender, arg) =>
        {
          RecordUndoEvent("Set: Expand Dependents");
          ExpandDependents = !ExpandDependents;
          ExpireSolution(true);
        },
        enabled: true,
        ExpandDependents
      );
    }
    #endregion
  }

  public class ElementGeometry : ElementGeometryComponent
  {
    public override Guid ComponentGuid => new Guid("B3BCBF5B-2034-414F-B9FB-97626FF37CBE");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public ElementGeometry() : base
    (
      name: "Element Geometry",
      nickname: "Geometry",
      description: "Get the geometry of the specified Element",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Elements",
          NickName = "E",
          Description = "Elements to extract geometry",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Exclude",
          NickName = "EX",
          Description = "Elements to exclude while extracting the geometry",
          Access = GH_ParamAccess.list,
          DataMapping = GH_DataMapping.Flatten,
          Optional = true
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.ViewDetailLevel>()
        {
          Name = "Detail Level",
          NickName = "DL",
          Description = "View detail level used to extract geometry",
          Optional = true
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Elements",
          NickName = "E",
          Description = "Elements geometry is extracted from",
          Access = GH_ParamAccess.tree
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_Geometry()
        {
          Name = "Geometry",
          NickName = "G",
          Description = "Element geometry",
          Access = GH_ParamAccess.tree
        }
      ),
      new ParamDefinition
      (
        new Parameters.Category()
        {
          Name = "Categories",
          NickName = "C",
          Description = "Geometry category",
          Access = GH_ParamAccess.tree
        },
        ParamRelevance.Occasional
      ),
      //new ParamDefinition
      //(
      //  new Parameters.Material()
      //  {
      //    Name = "Materials",
      //    NickName = "M",
      //    Description = "Geometry material",
      //    Access = GH_ParamAccess.tree
      //  },
      //  ParamVisibility.Voluntary
      //),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetDataList(DA, "Elements", out IList<Types.Element> elements)) return;
      if (!Params.TryGetDataList(DA, "Exclude", out IList<Types.Element> exclude)) return;
      if (!Params.TryGetData(DA, "Detail Level", out ARDB.ViewDetailLevel? detailLevel)) return;

      if (!TryGetCommonDocument(elements.Concat(exclude ?? Enumerable.Empty<Types.Element>()), out var doc))
        return;

      var scope = default(IDisposable);
      if (!detailLevel.HasValue)
      {
        detailLevel = ARDB.ViewDetailLevel.Coarse;
      }
      else if (elements.Any(x => x.Value is ARDB.FamilySymbol symbol && !symbol.IsActive))
      {
        scope = doc.RollBackScope();
        try
        {
          foreach (var symbol in elements.Select(x => x.Value).OfType<ARDB.FamilySymbol>())
            symbol.Activate();

          doc.Regenerate();
        }
        catch { scope.Dispose(); }
      }

      using (scope)
      using (var options = new ARDB.Options() { DetailLevel = detailLevel.Value })
      {
        var _Elements_ = Params.IndexOfOutputParam("Elements");
        var _Geometry_ = Params.IndexOfOutputParam("Geometry");
        SolveGeometry
        (
          doc,
          elements,
          exclude,
          options,
          _Elements_ < 0 ? default : DA.ParameterTargetPath(_Elements_), out var Elements,
          DA.ParameterTargetPath(_Geometry_), out var Geometry
        );

        if (Elements is object)
          DA.SetDataTree(_Elements_, Elements);

        DA.SetDataTree(_Geometry_, Geometry);

        var _Categories_ = Params.IndexOfOutputParam("Categories");
        var Categories = _Categories_ >= 0 ? new GH_Structure<Types.Category>() : default;

        var _Materials_ = Params.IndexOfOutputParam("Materials");
        var Materials = _Materials_ >= 0 ? new GH_Structure<Types.Material>() : default;

        SolveAttributes(doc, Geometry, Categories, Materials);

        if (Categories is object)
          DA.SetDataTree(_Categories_, Categories);

        if (Materials is object)
          DA.SetDataTree(_Materials_, Materials);
      }
    }
  }

  public class ElementViewGeometry : ElementGeometryComponent
  {
    public override Guid ComponentGuid => new Guid("8B85B1FB-A3DF-4924-BC84-58D2B919E664");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public ElementViewGeometry() : base
    (
      name: "Element View Geometry",
      nickname: "ViewGeom",
      description: "Get the geometry of the given Element on a view",
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
          Name = "Elements",
          NickName = "E",
          Description = "Elements to extract geometry",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Exclude",
          NickName = "EX",
          Description = "Elements to exclude while extracting the geometry",
          Access = GH_ParamAccess.list,
          DataMapping = GH_DataMapping.Flatten,
          Optional = true
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View used to extract geometry",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Elements",
          NickName = "E",
          Description = "Elements geometry is extracted from",
          Access = GH_ParamAccess.tree
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_Geometry()
        {
          Name = "Geometry",
          NickName = "G",
          Description = "Element geometry",
          Access = GH_ParamAccess.tree
        }
      ),
      new ParamDefinition
      (
        new Parameters.Category()
        {
          Name = "Categories",
          NickName = "C",
          Description = "Geometry category",
          Access = GH_ParamAccess.tree
        },
        ParamRelevance.Primary
      ),
      //new ParamDefinition
      //(
      //  new Parameters.Material()
      //  {
      //    Name = "Materials",
      //    NickName = "M",
      //    Description = "Geometry material",
      //    Access = GH_ParamAccess.tree
      //  },
      //  ParamVisibility.Default
      //),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetDataList(DA, "Elements", out IList<Types.Element> elements)) return;
      if (!Params.TryGetDataList(DA, "Exclude", out IList<Types.Element> exclude)) return;
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;

      if (!TryGetCommonDocument(elements.Concat(exclude ?? Enumerable.Empty<Types.Element>()).Concat(Enumerable.Repeat(view, 1)), out var doc))
        return;

      using (var options = new ARDB.Options() { View = view.Value })
      {
        var _Elements_ = Params.IndexOfOutputParam("Elements");
        var _Geometry_ = Params.IndexOfOutputParam("Geometry");
        SolveGeometry
        (
          doc,
          elements,
          exclude,
          options,
          _Elements_ < 0 ? default : DA.ParameterTargetPath(_Elements_), out var Elements,
          DA.ParameterTargetPath(_Geometry_), out var Geometry
        );

        if (Elements is object)
          DA.SetDataTree(_Elements_, Elements);

        DA.SetDataTree(_Geometry_, Geometry);

        var _Categories_ = Params.IndexOfOutputParam("Categories");
        var Categories = _Categories_ >= 0 ? new GH_Structure<Types.Category>() : default;

        var _Materials_ = Params.IndexOfOutputParam("Materials");
        var Materials = _Materials_ >= 0 ? new GH_Structure<Types.Material>() : default;

        SolveAttributes(doc, Geometry, Categories, Materials);

        if (Categories is object)
          DA.SetDataTree(_Categories_, Categories);

        if (Materials is object)
          DA.SetDataTree(_Materials_, Materials);
      }
    }
  }
}
