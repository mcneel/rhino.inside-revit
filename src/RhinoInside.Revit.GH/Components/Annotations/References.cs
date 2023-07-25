using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  using External.DB;

  [ComponentVersion(introduced: "1.12")]
  public class AnnotationReferences : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("96D578C0-D8D4-40D7-A96C-FC4481567733");
    public override GH_Exposure Exposure => GH_Exposure.quinary;
    protected override string IconTag => string.Empty;

    public AnnotationReferences() : base
    (
      name: "Annotation References",
      nickname: "References",
      description: string.Empty,
      category: "Revit",
      subCategory: "Annotate"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Annotation()
        {
          Name = "Annotation",
          NickName = "A",
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Annotation()
        {
          Name = "Annotation",
          NickName = "A",
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.GeometryObject()
        {
          Name = "References",
          NickName = "R",
          Description = "Geometry references Annotation ",
          Access = GH_ParamAccess.list
        }
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Annotation", out Types.IGH_Annotation annotation, x => x.IsValid)) return;
      else Params.TrySetData(DA, "Annotation", () => annotation);

      Params.TrySetDataList(DA, "References", () => (annotation as Types.IAnnotationReferencesAccess)?.References);
    }
  }

  [ComponentVersion(introduced: "1.16")]
  public class ElementAnnotations : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("2AB03AAF-98E4-4EF5-A84B-918B64E5908D");
    public override GH_Exposure Exposure => GH_Exposure.quinary;
    protected override string IconTag => string.Empty;

    public ElementAnnotations() : base
    (
      name: "Element Annotations",
      nickname: "E-Annotations",
      description: string.Empty,
      category: "Revit",
      subCategory: "Annotate"
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
        }
      ),
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Optional = true
        }, ParamRelevance.Primary
      )
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
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.Dimension()
        {
          Name = "Dimensions",
          NickName = "D",
          Description = "Element dimensions",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Annotation()
        {
          Name = "Tags",
          NickName = "T",
          Description = "Element tags",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.GraphicalElement element, x => x.IsValid)) return;
      else Params.TrySetData(DA, "Element", () => element);
      if (!Params.TryGetData(DA, "View", out Types.View view, x => x.IsValid)) return;

      var _Dimensions_ = Params.IndexOfOutputParam("Dimensions");
      var _Tags_ = Params.IndexOfOutputParam("Tags");

      var typesList = new List<Type>(3);
      if (_Dimensions_ >= 0)
      {
        typesList.Add(typeof(ARDB.Dimension));
      }
      if (_Tags_ >= 0)
      {
        typesList.Add(typeof(ARDB.IndependentTag));
        typesList.Add(typeof(ARDB.SpatialElementTag));
      }

      if (typesList.Count == 0) return;
      var filter = CompoundElementFilter.ElementClassFilter(typesList);

      if (view is object)
        filter = filter.Intersect(new ARDB.ElementOwnerViewFilter(view.Id));

      var dimensions = new List<ARDB.Dimension>();
      var tags = new List<ARDB.Element>();

      foreach (var id in element.Value.GetDependentElements(filter.ThatExcludes(element.Id)))
      {
        switch (element.Document.GetElement(id))
        {
          case ARDB.IndependentTag independentTag:
            tags.Add(independentTag); break;

          case ARDB.SpatialElementTag spatialElementTag:
            tags.Add(spatialElementTag); break;

          case ARDB.Dimension dimension:
            dimensions.Add(dimension); break;
        }
      }

      Params.TrySetDataList(DA, "Dimensions", () => dimensions);
      Params.TrySetDataList(DA, "Tags", () => tags);
    }
  }
}
