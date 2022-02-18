using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  [ComponentVersion(introduced: "1.6")]
  public class ElementVersion : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("3848C899-CC07-4397-855D-961E907C09D9");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override string IconTag => string.Empty;

    public ElementVersion() : base
    (
      name: "Element Version",
      nickname: "Version",
      description: "Element version information",
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
          Name = "Element",
          NickName = "E",
          Description = "Element to access version.",
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
          Name = "Element", NickName = "E",
          Description = "Element to access version."
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Editable", NickName = "EB",
          Description = "Identifies if the element is read-only or can possibly be modified"
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Guid()
        {
          Name = "Created", NickName = "C",
          Description = "Document episode when Element was created"
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Guid()
        {
          Name = "Version", NickName = "V",
          Description = "Document episode when Element was edited last time."
        },
#if REVIT_2021
        ParamRelevance.Primary
#else
        ParamRelevance.Occasional
#endif
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Edited By", NickName = "EB",
          Description = "Identifies the user last edited the element."
        }, ParamRelevance.Secondary
      ),
    };

#if !REVIT_2021
    protected override void BeforeSolveInstance()
    {
      if (Params.IndexOfOutputParam("Version") >= 0)
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "'Version' episode information is only available on Revit 2021 or above.");

      base.BeforeSolveInstance();
    }
#endif

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.Element element, x => x.IsValid)) return;
      else Params.TrySetData(DA, "Element", () => element);

      Params.TrySetData(DA, "Editable", () => element.IsEditable);

      var (created, version) = element.Version;
      Params.TrySetData(DA, "Created", () => created);
      Params.TrySetData(DA, "Version", () => version);

      Params.TrySetData
      (
        DA, "Edited By",
        () =>
        {
          var editedBy = element.Value.GetParameter(ERDB.Schemas.ParameterId.EditedBy)?.AsString();
          return string.IsNullOrEmpty(editedBy) ? null : editedBy;
        }
      );
    }
  }
}
