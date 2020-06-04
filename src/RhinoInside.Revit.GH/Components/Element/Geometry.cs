using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Geometry.Extensions;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public abstract class ElementGeometryComponent : TransactionalComponent
  {
    protected ElementGeometryComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected override System.Drawing.Bitmap Icon => ((System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject("ElementGeometry")) ??
                                      ImageBuilder.BuildIcon(IconTag);

    protected bool TryGetCommonDocument(IEnumerable<DB.Element> elements, out DB.Document document)
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

    static IGH_GeometricGoo ToGoo(GeometryBase geometry)
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

    protected delegate DB.Options OptionsDelegate();

    protected void SolveGeometry
    (
      int iteration,
      DB.Document doc,
      List<DB.Element> elements,
      List<DB.Element> exclude,
      OptionsDelegate optionsDelegate,
      out GH_Structure<IGH_GeometricGoo> geometries
    )
    {
      geometries = new GH_Structure<IGH_GeometricGoo>();

      // Build an empty data tree
      {
        int index = 0;
        foreach (var element in elements)
          geometries.EnsurePath(iteration, index++);
      }

      // Fill data tree
      if (doc is object)
      {
        using (var transaction = exclude.Count > 0 ? NewTransaction(doc) : default)
        {
          if (transaction is object)
          {
            transaction.Start();

            if (doc.Delete(exclude.ConvertAll(x => x?.Id ?? DB.ElementId.InvalidElementId)).Count > 0)
              doc.Regenerate();
          }

          int index = 0;
          foreach (var element in elements)
          {
            if (GH_Document.IsEscapeKeyDown())
            {
              OnPingDocument()?.RequestAbortSolution();
              return;
            }

            if (!element.IsValidObject)
              continue;

            if (element.get_BoundingBox(null) is null)
              continue;

            // Extract the geometry
            using(var options = optionsDelegate())
            {
              using (var geometry = element.GetGeometry(options))
              {
                if (geometry is object)
                {
                  using (var context = GeometryDecoder.Context.Push())
                  {
                    context.Element = element;
                    context.GraphicsStyleId = element.Category.GetGraphicsStyle(DB.GraphicsStyleType.Projection)?.Id ?? DB.ElementId.InvalidElementId;
                    context.MaterialId = element.Category.Material?.Id ?? DB.ElementId.InvalidElementId;

                    var list = geometry?.
                        ToGeometryBaseMany().
                        OfType<GeometryBase>().
                        Where(x => !IsEmpty(x)).
                        ToList();

                    if (list?.Count == 0)
                    {
                      foreach (var dependent in element.GetDependentElements(null).Select(x => element.Document.GetElement(x)))
                      {
                        if (dependent.get_BoundingBox(null) is DB.BoundingBoxXYZ)
                        {
                          using (var dependentOptions = optionsDelegate())
                          {
                            using (var dependentGeometry = dependent?.GetGeometry(dependentOptions))
                            {
                              if (dependentGeometry is object)
                                list.AddRange(dependentGeometry.ToGeometryBaseMany().OfType<GeometryBase>());
                            }
                          }
                        }
                      }
                    }

                    var valid = list.Where(x => !IsEmpty(x)).Select(x => ToGoo(x));
                    geometries.AppendRange(valid, new GH_Path(iteration, index++));
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
            if (geometry.ScriptVariable() is GeometryBase geometryBase)
            {
              if (categories is object)
              {
                geometryBase.GetUserElementId(DB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY.ToString(), out var categoryId);
                categoriesList.Add(new Types.Category(doc, categoryId));
              }

              if (materials is object)
              {
                geometryBase.GetUserElementId(DB.BuiltInParameter.MATERIAL_ID_PARAM.ToString(), out var materialId);
                materialsList.Add(Types.Material.FromElementId(doc, materialId) as Types.Material);
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
    protected override string IconTag => "G";

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
          Access = GH_ParamAccess.item,
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
        ParamVisibility.Voluntary
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Param_Geometry()
        {
          Name = "Geometry",
          NickName = "G",
          Description = "Element geometry",
          Access = GH_ParamAccess.tree
        },
        ParamVisibility.Binding
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
        ParamVisibility.Voluntary
      ),
      new ParamDefinition
      (
        new Parameters.Material()
        {
          Name = "Materials",
          NickName = "M",
          Description = "Geometry material",
          Access = GH_ParamAccess.tree
        },
        ParamVisibility.Voluntary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var elements = new List<DB.Element>();
      if (!DA.GetDataList("Elements", elements) || elements.Count == 0)
        return;

      var exclude = new List<DB.Element>();
      var _Exclude_ = Params.IndexOfInputParam("Exclude");
      if(_Exclude_ >= 0)
        DA.GetDataList(_Exclude_, exclude);

      if (!TryGetCommonDocument(elements.Concat(exclude), out var doc))
        return;

      var detailLevel = DB.ViewDetailLevel.Undefined;
      var _DetailLevel_ = Params.IndexOfInputParam("Detail Level");
      if (_DetailLevel_ >= 0)
      {
        DA.GetData(_DetailLevel_, ref detailLevel);
        if (detailLevel == DB.ViewDetailLevel.Undefined)
          detailLevel = DB.ViewDetailLevel.Coarse;
      }

      SolveGeometry
      (
        DA.Iteration,
        doc,
        elements,
        exclude,
        () => new DB.Options() { DetailLevel = detailLevel },
        out var Geometry
      );

      var _Geometry_ = Params.IndexOfOutputParam("Geometry");
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

  public class GraphicalElementGeometry : ElementGeometryComponent
  {
    public override Guid ComponentGuid => new Guid("8B85B1FB-A3DF-4924-BC84-58D2B919E664");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public GraphicalElementGeometry() : base
    (
      name: "Graphical Element Geometry",
      nickname: "Geometry",
      description: "Get the geometry of the specified Graphical Element on a view",
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
          Access = GH_ParamAccess.item
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
        ParamVisibility.Default
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Param_Geometry()
        {
          Name = "Geometry",
          NickName = "G",
          Description = "Element geometry",
          Access = GH_ParamAccess.tree
        },
        ParamVisibility.Binding
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
        ParamVisibility.Default
      ),
      new ParamDefinition
      (
        new Parameters.Material()
        {
          Name = "Materials",
          NickName = "M",
          Description = "Geometry material",
          Access = GH_ParamAccess.tree
        },
        ParamVisibility.Default
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var elements = new List<DB.Element>();
      if (!DA.GetDataList("Elements", elements) || elements.Count == 0)
        return;

      var exclude = new List<DB.Element>();
      var _Exclude_ = Params.IndexOfInputParam("Exclude");
      if(_Exclude_ >= 0)
        DA.GetDataList(_Exclude_, exclude);

      if (!TryGetCommonDocument(elements.Concat(exclude), out var doc))
        return;

      var view = default(DB.View);
      var _View_ = Params.IndexOfInputParam("View");
      if (!DA.GetData(_View_, ref view))
        return;

      SolveGeometry
      (
        DA.Iteration,
        doc,
        elements,
        exclude,
        () => new DB.Options() { View = view },
        out var Geometry
      );

      var _Geometry_ = Params.IndexOfOutputParam("Geometry");
      DA.SetDataTree(_Geometry_, Geometry);

      var _Categories_ = Params.IndexOfOutputParam("Categories");
      var Categories = _Categories_ >= 0 ? new GH_Structure<Types.Category>() : default;

      var _Materials_  = Params.IndexOfOutputParam("Materials");
      var Materials  = _Materials_  >= 0 ? new GH_Structure<Types.Material>() : default;

      SolveAttributes(doc, Geometry, Categories, Materials);

      if(Categories is object)
        DA.SetDataTree(_Categories_, Categories);

      if(Materials is object)
        DA.SetDataTree(_Materials_, Materials);
    }
  }
}

namespace RhinoInside.Revit.GH.Components.Obsolete
{
  [Obsolete("Obsolete since 2020-05-25")]
  public class ElementGeometry : Component
  {
    public override Guid ComponentGuid => new Guid("B7E6A82F-684F-4045-A634-A4AA9F7427A8");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.hidden;

    public ElementGeometry()
    : base("Element Geometry", "Geometry", "Get the geometry of the specified Element", "Revit", "Element")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Element", "E", "Element to query", GH_ParamAccess.item);
      manager[manager.AddParameter(new Parameters.Param_Enum<Types.ViewDetailLevel>(), "DetailLevel", "LOD", "Geometry Level of detail LOD [1, 3]", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddGeometryParameter("Geometry", "G", "Element geometry", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var element = default(DB.Element);
      if (!DA.GetData("Element", ref element))
        return;

      var detailLevel = DB.ViewDetailLevel.Undefined;
      DA.GetData("DetailLevel", ref detailLevel);
      if (detailLevel == DB.ViewDetailLevel.Undefined)
        detailLevel = DB.ViewDetailLevel.Coarse;

      if (element.get_BoundingBox(null) is DB.BoundingBoxXYZ)
      {
        // Extract the geometry
        {
          using (var options = new DB.Options() { DetailLevel = detailLevel })
          using (var geometry = element?.GetGeometry(options))
          {
            var list = geometry?.
              ToGeometryBaseMany().
              OfType<GeometryBase>().
              Where(x => !ElementGeometryComponent.IsEmpty(x)).
              ToList();

            if (list?.Count == 0)
            {
              foreach (var dependent in element.GetDependentElements(null).Select(x => element.Document.GetElement(x)))
              {
                if (dependent.get_BoundingBox(null) is DB.BoundingBoxXYZ)
                {
                  using (var dependentOptions = new DB.Options() { DetailLevel = detailLevel })
                  using (var dependentGeometry = dependent?.GetGeometry(dependentOptions))
                  {
                    if (dependentGeometry is object)
                      list.AddRange(dependentGeometry.ToGeometryBaseMany().OfType<GeometryBase>());
                  }
                }
              }
            }

            var valid = list.Where(x => !ElementGeometryComponent.IsEmpty(x));
            DA.SetDataList("Geometry", valid);
          }
        }
      }
    }
  }
}
