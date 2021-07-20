using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public abstract class ElementGeometryComponent : ZuiComponent
  {
    protected ElementGeometryComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected bool TryGetCommonDocument(IEnumerable<Types.Element> elements, out DB.Document document)
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

    internal static bool IsEmpty(GeometryBase geometry)
    {
      switch (geometry)
      {
        case Brep brep: return brep.Faces.Count == 0;
        case Mesh mesh: return mesh.Faces.Count == 0;
      }

      return false;
    }

    protected void SolveGeometry
    (
      GH_Path basePath,
      DB.Document doc,
      IList<Types.Element> elements,
      IList<Types.Element> exclude,
      DB.Options options,
      out GH_Structure<IGH_GeometricGoo> geometries
    )
    {
      geometries = new GH_Structure<IGH_GeometricGoo>();

      // Fill data tree
      if (doc is object)
      {
        using (var scope = exclude?.Count > 0 ? doc.RollBackScope() : default)
        {
          if (scope is object)
          {
            if (doc.Delete(exclude.ConvertAll(x => x?.Id ?? DB.ElementId.InvalidElementId)).Count > 0)
              doc.Regenerate();
          }

          int index = 0;
          foreach (var element in elements.Select(x => x.Value))
          {
            if (GH_Document.IsEscapeKeyDown())
            {
              OnPingDocument()?.RequestAbortSolution();
              return;
            }

            var path = basePath.AppendElement(index++);
            geometries.EnsurePath(path);

            if (element.IsValid() && element.get_BoundingBox(null) is object)
            {
              // Extract the geometry
              using (var geometry = element.GetGeometry(options))
              {
                if (geometry is object)
                {
                  using (var context = GeometryDecoder.Context.Push())
                  {
                    context.Element = element;
                    context.GraphicsStyleId = element.Category?.GetGraphicsStyle(DB.GraphicsStyleType.Projection)?.Id ?? DB.ElementId.InvalidElementId;
                    context.MaterialId = element.Category?.Material?.Id ?? DB.ElementId.InvalidElementId;

                    var list = geometry?.
                        ToGeometryBaseMany().
                        OfType<GeometryBase>().
                        Where(x => !IsEmpty(x)).
                        ToList();

                    if (list.Count == 0)
                    {
                      foreach (var dependent in element.GetDependentElements(new DB.ExclusionFilter(new DB.ElementId[] { element.Id })).Select(x => element.Document.GetElement(x)))
                      {
                        if (dependent.get_BoundingBox(null) is DB.BoundingBoxXYZ)
                        {
                          using (var dependentGeometry = dependent?.GetGeometry(options))
                          {
                            if (dependentGeometry is object)
                              list.AddRange(dependentGeometry.ToGeometryBaseMany().OfType<GeometryBase>());
                          }
                        }
                      }
                    }

                    var valid = list.Where(x => !IsEmpty(x)).Select(ToGeometricGoo);
                    geometries.AppendRange(valid, path);
                  }
                }
              }
            }
          }
        }
      }
    }

    protected void SolveAttributes
    (
      DB.Document doc,
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
                  geometryBase.TryGetUserString(DB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY.ToString(), out DB.ElementId categoryId);
                  categoriesList.Add(new Types.Category(doc, categoryId));
                }
              }

              if (materials is object)
              {
                if (geometryBase is null) materialsList.Add(null);
                else
                {
                  geometryBase.TryGetUserString(DB.BuiltInParameter.MATERIAL_ID_PARAM.ToString(), out DB.ElementId materialId);
                  materialsList.Add(Types.Material.FromElementId(doc, materialId) as Types.Material);
                }
              }
            }
          }
        }
      }
      }
    }

  public class ElementGeometry : ElementGeometryComponent
  {
    public override Guid ComponentGuid => new Guid("B3BCBF5B-2034-414F-B9FB-97626FF37CBE");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;

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
        new Parameters.Param_Enum<Types.ViewDetailLevel>()
        {
          Name = "Detail Level",
          NickName = "DL",
          Description = "View detail level used to extract geometry",
          Optional = true
        }
      ),
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
      )
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
          Access = GH_ParamAccess.list
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
      if (!Params.TryGetData(DA, "Detail Level", out DB.ViewDetailLevel? detailLevel)) return;
      if (!Params.GetDataList(DA, "Elements", out IList<Types.Element> elements)) return;
      if (!Params.TryGetDataList(DA, "Exclude", out IList<Types.Element> exclude)) return;

      if (!detailLevel.HasValue)
        detailLevel = DB.ViewDetailLevel.Coarse;

      if (!TryGetCommonDocument(elements.Concat(exclude ?? Enumerable.Empty<Types.Element>()), out var doc))
        return;

      using(var options = new DB.Options() { DetailLevel = detailLevel.Value })
      {
        Params.TrySetDataList(DA, "Elements", () => elements);

        var _Geometry_ = Params.IndexOfOutputParam("Geometry");
        SolveGeometry
        (
          DA.ParameterTargetPath(_Geometry_),
          doc,
          elements,
          exclude,
          options,
          out var Geometry
        );
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
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View used to extract geometry",
        }
      ),
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
        ParamRelevance.Primary
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
          Access = GH_ParamAccess.list
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
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      if (!Params.GetDataList(DA, "Elements", out IList<Types.Element> elements)) return;
      if (!Params.TryGetDataList(DA, "Exclude", out IList<Types.Element> exclude)) return;

      if (!TryGetCommonDocument(elements.Concat(exclude ?? Enumerable.Empty<Types.Element>()).Concat(Enumerable.Repeat(view, 1)), out var doc))
        return;

      using (var options = new DB.Options() { View = view.Value })
      {
        Params.TrySetDataList(DA, "Elements", () => elements);

        var _Geometry_ = Params.IndexOfOutputParam("Geometry");
        SolveGeometry
        (
          DA.ParameterTargetPath(_Geometry_),
          doc,
          elements,
          exclude,
          options,
          out var Geometry
        );

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
