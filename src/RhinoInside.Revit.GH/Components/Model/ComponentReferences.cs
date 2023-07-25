using System;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Geometry
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.16")]
  public class ComponentReferences : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("86D56BEA-4064-4E79-80A4-12F6D2968DEA");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => string.Empty;

    public ComponentReferences() : base
    (
      name: "Component References",
      nickname: "C-References",
      description: "Retrieves references of given component.",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.FamilyInstance()
        {
          Name = "Component",
          NickName = "C",
          Description = "Component to query for references.",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition(new Parameters.GeometryFace() { Name = "Left",                NickName = "LT",  Access = GH_ParamAccess.item }, ParamRelevance.Primary),
      new ParamDefinition(new Parameters.GeometryFace() { Name = "Center (Left/Right)", NickName = "CLR", Access = GH_ParamAccess.item }, ParamRelevance.Primary),
      new ParamDefinition(new Parameters.GeometryFace() { Name = "Right",               NickName = "RT",  Access = GH_ParamAccess.item }, ParamRelevance.Primary),
      new ParamDefinition(new Parameters.GeometryFace() { Name = "Front",               NickName = "FT",  Access = GH_ParamAccess.item }, ParamRelevance.Primary),
      new ParamDefinition(new Parameters.GeometryFace() { Name = "Center (Front/Back)", NickName = "CFB", Access = GH_ParamAccess.item }, ParamRelevance.Primary),
      new ParamDefinition(new Parameters.GeometryFace() { Name = "Back",                NickName = "BK",  Access = GH_ParamAccess.item }, ParamRelevance.Primary),
      new ParamDefinition(new Parameters.GeometryFace() { Name = "Bottom",              NickName = "BT",  Access = GH_ParamAccess.item }, ParamRelevance.Primary),
      new ParamDefinition(new Parameters.GeometryFace() { Name = "Center (Elevation)",  NickName = "CE",  Access = GH_ParamAccess.item }, ParamRelevance.Primary),
      new ParamDefinition(new Parameters.GeometryFace() { Name = "Top",                 NickName = "TP",  Access = GH_ParamAccess.item }, ParamRelevance.Primary),

      new ParamDefinition(new Parameters.GeometryObject() { Name = "Strong Reference",    NickName = "SR",  Access = GH_ParamAccess.list }, ParamRelevance.Secondary),
      new ParamDefinition(new Parameters.GeometryObject() { Name = "Weak Reference",      NickName = "WR",  Access = GH_ParamAccess.list }, ParamRelevance.Secondary),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Component", out Types.FamilyInstance component)) return;

      Params.TrySetData(DA, "Left", () =>
        component.GetReferences(ARDB.FamilyInstanceReferenceType.Left).Select(component.GetGeometryObjectFromReference<Types.GeometryFace>).FirstOrDefault());

      Params.TrySetData(DA, "Center (Left/Right)", () =>
        component.GetReferences(ARDB.FamilyInstanceReferenceType.CenterLeftRight).Select(component.GetGeometryObjectFromReference<Types.GeometryFace>).FirstOrDefault());

      Params.TrySetData(DA, "Right", () =>
        component.GetReferences(ARDB.FamilyInstanceReferenceType.Right).Select(component.GetGeometryObjectFromReference<Types.GeometryFace>).FirstOrDefault());

      Params.TrySetData(DA, "Front", () =>
        component.GetReferences(ARDB.FamilyInstanceReferenceType.Front).Select(component.GetGeometryObjectFromReference<Types.GeometryFace>).FirstOrDefault());

      Params.TrySetData(DA, "Center (Front/Back)", () =>
        component.GetReferences(ARDB.FamilyInstanceReferenceType.CenterFrontBack).Select(component.GetGeometryObjectFromReference<Types.GeometryFace>).FirstOrDefault());

      Params.TrySetData(DA, "Back", () =>
        component.GetReferences(ARDB.FamilyInstanceReferenceType.Back).Select(component.GetGeometryObjectFromReference<Types.GeometryFace>).FirstOrDefault());

      Params.TrySetData(DA, "Bottom", () =>
        component.GetReferences(ARDB.FamilyInstanceReferenceType.Bottom).Select(component.GetGeometryObjectFromReference<Types.GeometryFace>).FirstOrDefault());

      Params.TrySetData(DA, "Center (Elevation)", () =>
        component.GetReferences(ARDB.FamilyInstanceReferenceType.CenterElevation).Select(component.GetGeometryObjectFromReference<Types.GeometryFace>).FirstOrDefault());

      Params.TrySetData(DA, "Top", () =>
        component.GetReferences(ARDB.FamilyInstanceReferenceType.Top).Select(component.GetGeometryObjectFromReference<Types.GeometryFace>).FirstOrDefault());

      Params.TrySetDataList(DA, "Strong Reference", () =>
        component.GetReferences(ARDB.FamilyInstanceReferenceType.StrongReference).Select(component.GetGeometryObjectFromReference<Types.GeometryObject>));

      Params.TrySetDataList(DA, "Weak Reference", () =>
        component.GetReferences(ARDB.FamilyInstanceReferenceType.WeakReference).Select(component.GetGeometryObjectFromReference<Types.GeometryObject>));
    }
  }
}
