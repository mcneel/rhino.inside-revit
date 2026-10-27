using System;
using System.IO;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;
using OS = System.Environment;

namespace RhinoInside.Revit.GH.Components.Insert
{
  using Convert.Geometry;
  using External.DB.Extensions;

  public abstract class LinkCADFile : ElementTrackerComponent
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    protected LinkCADFile(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory)
    { }

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _Output_,
          NickName = _Output_.Substring(0, 1),
          Description = $"Output {_Output_}"
        }
      )
    };

    protected const string _Output_ = "Link";

    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.IMPORT_BASE_LEVEL,
      ARDB.BuiltInParameter.IMPORT_BASE_LEVEL_OFFSET,
    };

    bool Reuse
    (
      ref ARDB.ImportInstance instance,
      ARDB.View view,
      string path,
      ARDB.BaseImportOptions options,
      bool move
    )
    {
      if (instance is null) return false;

      var ownerViewId = options.ThisViewOnly ? view.Id : ElementIdExtension.Invalid;
      if (instance.OwnerViewId != ownerViewId) return false;

      var type = view.Document.GetCADLinkType(path);
      if (type is null) return false;
      if (instance.GetTypeId() != type.Id) return false;

      switch (options.Placement)
      {
        case ARDB.ImportPlacement.Site:
          if (instance.GetSharedSite() is object) return false;
          if (move) instance.SetLocation(BasePointExtension.GetProjectBasePoint(view.Document).GetPosition(), ERDB.UnitXYZ.BasisX, ERDB.UnitXYZ.BasisY);
          break;
        case ARDB.ImportPlacement.Origin:
          if (instance.GetSharedSite() is object) return false;
          if (move) instance.SetLocation(XYZExtension.Zero, ERDB.UnitXYZ.BasisX, ERDB.UnitXYZ.BasisY);
          break;
        case ARDB.ImportPlacement.Centered:
          if (instance.GetSharedSite() is object) return false;
          //if (move) instance.SetLocation(XYZExtension.Zero, ERDB.UnitXYZ.BasisX, ERDB.UnitXYZ.BasisY);
          //break;
          return false;
        case ARDB.ImportPlacement.Shared:
          if (type.GetSharedSites().Length > 0 && instance.GetSharedSite() is null) return false;
          break;
        default:
          return false;
      }

      type.SetImportUnits(options.Unit, UnitConverter.Model.ConvertToInternalUnits(options.CustomScale));
      return true;
    }

    ARDB.ImportInstance Create
    (
      ARDB.View view,
      string path,
      ARDB.BaseImportOptions options
    )
    {
      if (!Types.CADReadOptions.SupportedExtensions.Contains(Path.GetExtension(path)))
        throw new Exceptions.RuntimeException($"Unsupported file extension: {Path.GetExtension(path)}");

      var instance = ImportInstanceExtension.Link(view, path, options, out var result);
      if (instance is null)
        throw new Exceptions.RuntimeException($"Failed to link file '{path}'" + OS.NewLine + $"{result?.LoadResult ?? ARDB.LinkLoadResultType.LinkNotOpenable}");

      ARDB.XYZ origin = default;
      if (options.Placement == ARDB.ImportPlacement.Site)
        origin = BasePointExtension.GetProjectBasePoint(view.Document).GetPosition();
      else if (options.Placement == ARDB.ImportPlacement.Origin)
        origin = XYZExtension.Zero;

      if (origin is object)
        instance.SetLocation(origin, ERDB.UnitXYZ.BasisX, ERDB.UnitXYZ.BasisY);

      return instance;
    }

    protected ARDB.ImportInstance Reconstruct
    (
      ARDB.ImportInstance instance,
      ARDB.View view,
      string path,
      ARDB.BaseImportOptions options,
      bool move
    )
    {
      if (!Reuse(ref instance, view, path, options, move))
      {
        if (instance is null) move = false;

        var (origin, basisX, basisY) = move ? instance.GetLocation() : (default, default, default);
        instance = instance.ReplaceElement
        (
          Create(view, path, options),
          ExcludeUniqueProperties
        );

        if (!move && origin is object && !basisX.IsNaN && !basisY.IsNaN)
          instance.SetLocation(origin, basisX, basisY);
      }

      return instance;
    }
  }

  [ComponentVersion(introduced: "1.36")]
  public sealed class LinkModelCADFile : LinkCADFile
  {
    public override Guid ComponentGuid => new Guid("E2B7D9F5-3F8D-6A6E-F2C5-7E9D1B3C4E5F");

    public LinkModelCADFile() : base
    (
      name: "Link Model CAD File",
      nickname: "LinkCAD",
      description: "Link a CAD file to the Revit model",
      category: "Revit",
      subCategory: "Insert"
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
          Description = "Reference view.",
        }
      ),
      new ParamDefinition
      (
        new Param_FilePath
        {
          Name = "Path",
          NickName = "P",
          Description = "Absolute path to the CAD file",
          FileFilter = Types.CADReadOptions.FileFilter
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.ImportPlacement>
        {
          Name = "Placement",
          NickName = "P",
          Description = "Where to place the import",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.LevelConstraint
        {
          Name = "Elevation",
          NickName = "E",
          Description = $"Elevation the Import.{OS.NewLine}This input accepts a 'Level Constraint', an 'Elevation' or a 'Number' as an offset from the 'Placement'.",
          Optional = true,
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_GenericObject
        {
          Name = "Options",
          NickName = "O",
          Description = "CAD import options",
          Optional = true
        }, ParamRelevance.Primary
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view)) return;

      ReconstructElement<ARDB.ImportInstance>
      (
        view.Document, _Output_, instance =>
        {
          if (!Params.GetData(DA, "Path", out string path)) return null;
          if (!Params.TryGetData(DA, "Placement", out ARDB.ImportPlacement? placement)) return null;
          if (!Params.TryGetData(DA, "Elevation", out ERDB.ElevationElementReference? baseElevation)) return null;
          if (!Params.TryGetData(DA, "Options", out Types.CADReadOptions readOptions)) return null;
          readOptions ??= new Types.CADReadOptions();

          if (!view.Value.IsModelView())
            throw new Exceptions.RuntimeArgumentException
            (
              nameof(view),
              $"View '{view.DisplayName}' does not support model CAD files." + OS.NewLine +
              "Consider using 'Link Drafing CAD File' component instead on this view."
            );

          using (var options = readOptions.ToImportOptions(path))
          {
            options.Placement = placement ?? ARDB.ImportPlacement.Origin;
            options.ThisViewOnly = false;

            instance = Reconstruct(instance, view.Value, path, options, placement is object);
          }

          if (baseElevation is object)
          {
            var (origin, basisX, basisY) = instance.GetLocation();
            ERDB.ElevationElementReference.SolveBase
            (
              view.Document, origin.Z,
              0.0, ref baseElevation
            );

            if (baseElevation.Value.IsLevelConstraint(out var level, out var levelOffset))
            {
              instance.get_Parameter(ARDB.BuiltInParameter.IMPORT_BASE_LEVEL).Set(level);
              instance.get_Parameter(ARDB.BuiltInParameter.IMPORT_BASE_LEVEL_OFFSET).Set(levelOffset ?? 0.0);
            }
            else if (baseElevation.Value.IsOffset(out var offset))
            {
              origin = new ARDB.XYZ(origin.X, origin.Y, origin.Z + offset);
              instance.SetLocation(origin, basisX, basisY);
            }
          }

          DA.SetData(_Output_, instance);
          return instance;
        }
      );
    }
  }

  [ComponentVersion(introduced: "1.36")]
  public sealed class LinkDraftingCADFile : LinkCADFile
  {
    public override Guid ComponentGuid => new Guid("DC029DB6-CAB4-4E71-8579-0CA0EC36DE65");

    public LinkDraftingCADFile() : base
    (
      name: "Link Drafing CAD File",
      nickname: "LinkCAD",
      description: "Link a CAD file to a Revit view",
      category: "Revit",
      subCategory: "Insert"
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
          Description = "Target view.\r\nImported drawing will be visible only into the provided View.",
        }
      ),
      new ParamDefinition
      (
        new Param_FilePath
        {
          Name = "Path",
          NickName = "P",
          Description = "Absolute path to the CAD file",
          FileFilter = Types.CADReadOptions.FileFilter
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.ImportPlacement>
        {
          Name = "Placement",
          NickName = "P",
          Description = "Where to place the import",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.DrawLayer>
        {
          Name = "Draw Layer",
          NickName = "Dl",
          Description = "Import draw layer",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_GenericObject
        {
          Name = "Options",
          NickName = "O",
          Description = "CAD import options",
          Optional = true
        }, ParamRelevance.Primary
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view)) return;

      ReconstructElement<ARDB.ImportInstance>
      (
        view.Document, _Output_, instance =>
        {
          if (!Params.GetData(DA, "Path", out string path)) return null;
          if (!Params.TryGetData(DA, "Placement", out ARDB.ImportPlacement? placement)) return null;
          if (!Params.TryGetData(DA, "Draw Layer", out ARDB.DrawLayer? drawLayer)) return null;
          if (!Params.TryGetData(DA, "Options", out Types.CADReadOptions readOptions)) return null;
          readOptions ??= new Types.CADReadOptions();

          if (!view.Value.IsAnnotationView() || view.Value is ARDB.View3D)
            throw new Exceptions.RuntimeArgumentException
            (
              nameof(view),
              $"View '{view.DisplayName}' does not support drafting CAD files." + OS.NewLine +
              "Consider using 'Link Model CAD File' component instead on this view."
            );
          using (var options = readOptions.ToImportOptions(path))
          {
            options.Placement = placement ?? ARDB.ImportPlacement.Origin;
            options.ThisViewOnly = true;

            instance = Reconstruct(instance, view.Value, path, options, placement is object);
          }

          instance.get_Parameter(ARDB.BuiltInParameter.IMPORT_BACKGROUND).Set(drawLayer ?? ARDB.DrawLayer.Background);

          DA.SetData(_Output_, instance);
          return instance;
        }
      );
    }
  }
}
