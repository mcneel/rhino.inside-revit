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

  public abstract class ImportCADFile : ElementTrackerComponent
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    protected ImportCADFile(string name, string nickname, string description, string category, string subCategory)
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

    protected const string _Output_ = "Import";

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

      var type = view.Document.GetCADLinkType(Path.GetFileName(path));
      if (type is null || type.IsLinked()) return false;
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
          if (move) instance.SetLocation(XYZExtension.Zero, ERDB.UnitXYZ.BasisX, ERDB.UnitXYZ.BasisY);
          break;
        case ARDB.ImportPlacement.Shared:
          if (type.GetSharedSites().Length > 0 && instance.GetSharedSite() is null) return false;
          if (move) instance.SetLocation(BasePointExtension.GetSurveyPoint(view.Document).GetPosition(), ERDB.UnitXYZ.BasisX, ERDB.UnitXYZ.BasisY);
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
      if (!Types.CADReadOptions.ImportExtensions.Contains(Path.GetExtension(path)))
        throw new Exceptions.RuntimeException($"Unsupported file extension: {Path.GetExtension(path)}");

      var type = view.Document.GetCADLinkType(Path.GetFileName(path));
      if (type?.IsLinked() is true)
        throw new Exceptions.RuntimeException($"Revit already has a link with the input location name, so no new import with same name can be created.");

      var instance = type is object ?
        ARDB.ImportInstance.Create(view.Document, type.Id, view):
        ImportInstanceExtension.Import(view, path, options);

      if (instance is null)
        throw new Exceptions.RuntimeException($"Failed to import file '{path}'");

      if (type is null)
      {
        type = instance.Document.GetElement(instance.GetTypeId()) as ARDB.CADLinkType;
        type.Name = Path.GetFileName(path);
      }

      ARDB.XYZ origin = default;
      if (options.Placement == ARDB.ImportPlacement.Site)
        origin = BasePointExtension.GetProjectBasePoint(view.Document).GetPosition();
      else if (options.Placement == ARDB.ImportPlacement.Origin)
        origin = XYZExtension.Zero;
      else if (options.Placement == ARDB.ImportPlacement.Centered)
        origin = XYZExtension.Zero;
      if (options.Placement == ARDB.ImportPlacement.Shared)
        origin = BasePointExtension.GetSurveyPoint(view.Document).GetPosition();

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
  public sealed class ImportModelCADFile : ImportCADFile
  {
    public override Guid ComponentGuid => new Guid("A6758D97-28DF-42F5-8C7D-10F595042256");

    public ImportModelCADFile() : base
    (
      name: "Import Model CAD File",
      nickname: "ImportCAD",
      description: "Import a CAD file to the Revit model",
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
          FileFilter = Types.CADReadOptions.ImportFileFilter
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
              "Consider using 'Import Drafing CAD File' component instead on this view."
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
  public sealed class ImportDraftingCADFile : ImportCADFile
  {
    public override Guid ComponentGuid => new Guid("46532F7C-B726-44E6-BD2F-2D3B467F891A");

    public ImportDraftingCADFile() : base
    (
      name: "Import Drafing CAD File",
      nickname: "ImportCAD",
      description: "Import a CAD file to a Revit view",
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
          FileFilter = Types.CADReadOptions.ImportFileFilter
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
              "Consider using 'Import Model CAD File' component instead on this view."
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
