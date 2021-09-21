using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Assembly
{
  public class AssemblyViews : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("64594dea-057a-47b9-8e63-5e0832e13adb");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.hidden;
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.View));

    public AssemblyViews() : base
    (
      name: "Assembly Views",
      nickname: "AV",
      description: "Get all views that belong to given assembly",
      category: "Revit",
      subCategory: "Assembly"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.AssemblyInstance()
        {
          Name = "Assembly",
          NickName = "A",
          Description = "Assembly to query for views",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "Views",
          NickName = "V",
          Description = "Views belonging to given assembly",
          Access = GH_ParamAccess.list
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Assembly", out Types.AssemblyInstance assembly, x => x.IsValid))
        return;

      using (var collector = new DB.FilteredElementCollector(assembly.Document))
      {
        var viewCollector = collector.WherePasses(ElementFilter).
          WhereParameterEqualsTo(DB.BuiltInParameter.VIEW_ASSOCIATED_ASSEMBLY_INSTANCE_ID, assembly.Id);

        DA.SetDataList("Views", viewCollector);
      }
    }
  }
}
