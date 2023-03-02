using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  using External.DB.Extensions;
  using Rhino.Geometry;
  using RhinoInside.Revit.Convert.Geometry;

  [ComponentVersion(introduced: "1.13")]
  public class AddElevationMarker : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("2101FFF6-0618-418C-AE1E-A3311E942535");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddElevationMarker() : base
    (
      name: "Add Elevation Marker",
      nickname: "ElevMark",
      description: "Given a name, it adds a section view to the active Revit document",
      category: "Revit",
      subCategory: "View"
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
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_Plane
        {
          Name = "Location",
          NickName = "L",
          Description = "Elevation Mark location",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.ViewFamilyType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Section view type",
          Optional = true,
        }, ParamRelevance.Occasional
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _ElevationMarker_,
          NickName = "EM",
          Description = $"Output {_ElevationMarker_}",
        }
      )
    };

    protected const string _ElevationMarker_ = "Elevation Marker";

    public static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.ElevationMarker>
      (
        doc.Value, _ElevationMarker_, mark =>
        {
          // Input
          if (!Params.TryGetData(DA, "Location", out Plane? location, x => x.IsValid)) return null;
          if (!Parameters.ViewFamilyType.GetDataOrDefault(this, DA, "Type", out Types.ViewFamilyType type, doc, ARDB.ElementTypeGroup.ViewTypeElevation)) return null;

          // Compute
          StartTransaction(doc.Value);
          if (CanReconstruct(_ElevationMarker_, out var untracked, ref mark, doc.Value, string.Empty, ARDB.ViewType.DraftingView.ToString()))
            mark = Reconstruct(mark, location.Value.ToFrame(), type.Value);

          DA.SetData(_ElevationMarker_, mark);
          return untracked ? null : mark;
        }
      );
    }


    bool Reuse(ARDB.ElevationMarker mark, ARDB.Frame location, ARDB.ViewFamilyType type)
    {
      if (mark is null) return false;
      if (type.Id != mark.GetTypeId()) mark.ChangeTypeId(type.Id);

      return true;
    }

    ARDB.ElevationMarker Create(ARDB.Frame location, ARDB.ViewFamilyType type)
    {
      return ARDB.ElevationMarker.CreateElevationMarker(type.Document, type.Id, location.Origin, 100);
    }

    ARDB.ElevationMarker Reconstruct(ARDB.ElevationMarker mark, ARDB.Frame location, ARDB.ViewFamilyType type)
    {
      if (!Reuse(mark, location, type))
        mark = Create(location, type);

      mark.SetLocation(location.Origin, (ERDB.UnitXYZ) location.BasisX, (ERDB.UnitXYZ) location.BasisY);
      return mark;
    }
  }
}
