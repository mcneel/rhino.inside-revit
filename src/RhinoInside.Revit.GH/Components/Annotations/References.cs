using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Annotations
{
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
      subCategory: "Annotation"
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

      Params.TrySetDataList(DA, "References", () => annotation.References);
    }
  }
}
