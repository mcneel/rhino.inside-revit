using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Insert
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.36")]
  public sealed class LinkCAD : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("E2B7D9F5-3F8D-6A6E-F2C5-7E9D1B3C4E5F");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public LinkCAD() : base
    (
      name: "Link CAD File",
      nickname: "LinkCAD",
      description: "Link a CAD file to the current Revit document",
      category: "Revit",
      subCategory: "Insert"
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
          Description = "Target document for linking the CAD file",
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_FilePath
        {
          Name = "Path",
          NickName = "P",
          Description = "Absolute path to the CAD file",
          FileFilter = FileFilter
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _Output_,
          NickName = _Output_.Substring(0, 1),
          Description = $"Output {_Output_}",
          Access = GH_ParamAccess.item
        }
      )
    };

    const string _Output_ = "Import Symbol";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.ImportInstance>
      (
        doc.Value, _Output_, instance =>
        {
          if (!Params.GetData(DA, "Path", out string path)) return null;

          AssertValidPath(path);
          instance = Reconstruct(instance, doc.Value, path);

          DA.SetData(_Output_, instance);
          return instance;
        }
      );
    }

    void AssertValidPath(string path)
    {
      if (!File.Exists(path))
        throw new Exceptions.RuntimeArgumentException("Path", $"File does not exist: {path}");

      var extension = Path.GetExtension(path);
      if (extension is null || !SupportedExtensions.Contains(extension))
      {
        var supportedList = string.Join(", ", SupportedExtensions);
        throw new Exceptions.RuntimeArgumentException(nameof(path), $"File must have one of these extensions: {supportedList}.");
      }
    }

    static ARDB.BaseImportOptions CreateImportOptions(string path)
    {
      if (string.IsNullOrEmpty(path)) return null;
      if (path.EndsWith(".dwg", StringComparison.InvariantCultureIgnoreCase)) return new ARDB.DWGImportOptions();
      if (path.EndsWith(".dxf", StringComparison.InvariantCultureIgnoreCase)) return new ARDB.DWGImportOptions();
      if (path.EndsWith(".dgn", StringComparison.InvariantCultureIgnoreCase)) return new ARDB.DGNImportOptions();
      if (path.EndsWith(".sat", StringComparison.InvariantCultureIgnoreCase)) return new ARDB.SATImportOptions();
      if (path.EndsWith(".skp", StringComparison.InvariantCultureIgnoreCase)) return new ARDB.SKPImportOptions();
#if REVIT_2022
      if (path.EndsWith(".3dm", StringComparison.InvariantCultureIgnoreCase)) return new ARDB.ImportOptions3DM();
#endif
#if REVIT_2023
      if (path.EndsWith(".obj", StringComparison.InvariantCultureIgnoreCase)) return new ARDB.OBJImportOptions();
      if (path.EndsWith(".stl", StringComparison.InvariantCultureIgnoreCase)) return new ARDB.STLImportOptions();
#endif

      return null;
    }

    ARDB.ImportInstance Create
    (
      ARDB.Document doc,
      string path,
      ARDB.BaseImportOptions options
    )
    {
      if (!SupportedExtensions.Contains(Path.GetExtension(path)))
        throw new Exceptions.RuntimeException($"Unsupported file extension: {Path.GetExtension(path)}");

      using (var collector = new ARDB.FilteredElementCollector(doc).OfClass(typeof(ARDB.View)))
      {
        if (collector.OfType<ARDB.View>().FirstOrDefault(x => x.IsModelView()) is ARDB.View view)
        {
          var instanceId = ARDB.ElementId.InvalidElementId;

          if (options is ARDB.DWGImportOptions optionsDwg)      doc.Link(path, optionsDwg, view, out instanceId);
          else if (options is ARDB.DGNImportOptions optionsDGN) doc.Link(path, optionsDGN, view, out instanceId);
          else if (options is ARDB.SATImportOptions optionsSAT) instanceId = doc.Link(path, optionsSAT, view);
          else if (options is ARDB.SATImportOptions optionsSKP) instanceId = doc.Link(path, optionsSKP, view);
#if REVIT_2022
          else if (options is ARDB.ImportOptions3DM options3DM) instanceId = doc.Link(path, options3DM, view);
#endif
#if REVIT_2023
          else if (options is ARDB.OBJImportOptions optionsOBJ) instanceId = doc.Link(path, optionsOBJ, view);
          else if (options is ARDB.STLImportOptions optionsSTL) instanceId = doc.Link(path, optionsSTL, view);
#endif

          if (instanceId == ARDB.ElementId.InvalidElementId)
            throw new Exceptions.RuntimeException($"Failed to link file '{path}'");

          // Setup always same location when the instance is new.
          var instance = doc.GetElement(instanceId) as ARDB.ImportInstance;
          if (doc.GetNearestLevel(0.0) is ARDB.Level level)
          {
            instance.get_Parameter(ARDB.BuiltInParameter.IMPORT_BASE_LEVEL).Set(level);
            instance.get_Parameter(ARDB.BuiltInParameter.IMPORT_BASE_LEVEL_OFFSET).Set(-level.GetElevation());
          }
          instance.SetLocation(XYZExtension.Zero, UnitXYZ.BasisX, UnitXYZ.BasisY);

          return instance;
        }
      }

      return null;
    }

    ARDB.ImportInstance Reconstruct
    (
      ARDB.ImportInstance instance,
      ARDB.Document doc,
      string path
    )
    {
      using (var options = CreateImportOptions(path))
      {
        if (instance is null)
        {
          instance = Create(doc, path, options);
        }
        else
        {
          using (var scope = new ARDB.SubTransaction(instance.Document))
          {
            scope.Start();
            var newInstance = Create(doc, path, options);
            if (newInstance.GetTypeId() != instance.GetTypeId())
            {
              var (origin, basisX, basisY) = instance.GetLocation();
              newInstance.SetLocation(origin, basisX, basisY);

              instance = instance.ReplaceElement(newInstance, ExcludeUniqueProperties);
              scope.Commit();
            }
          }
        }
      }

      return instance;
    }

    static HashSet<string> SupportedExtensions => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      ".dwg", ".dxf", ".dgn", ".sat", ".skp",
#if REVIT_2022
      ".3dm",
#endif
#if REVIT_2023
      ".obj", ".stl",
#endif
    };

    static string FileFilter =>
#if REVIT_2023
      "All Supported Files (*3dm, *sat, *obj, *.stl, *skp, *.dwg, *.dxf, *.dgn)|*3dm;*sat;*obj;*.stl;*skp;*.dwg;*.dxf;*.dgn";
#elif REVIT_2022
      "All Supported Files (*3dm, *sat, *skp, *.dwg, *.dxf, *.dgn)|*3dm;*sat;*skp;*.dwg;*.dxf;*.dgn";
#else
      "All Supported Files (*sat, *skp, *.dwg, *.dxf, *.dgn)|*sat;*skp;*.dwg;*.dxf;*.dgn";
#endif
  }
}
