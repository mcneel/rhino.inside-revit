using System;
using System.Collections.Generic;
using System.Linq;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class CADReadOptions : ValueObject, ICloneable
  {
    public CADReadOptions() : base(null, new Options()) { }
    private CADReadOptions(Options options) : base(null, options) { }

    public override string DisplayName => "Read Options";

    new Options Value => (Options) base.Value;

    class Options : ICloneable
    {
      public bool? OrientToView;
      public bool? AutocorrectOffAxisLines;
      public bool? VisibleLayersOnly;
      public ICollection<string> LayerSelection;

      public ARDB.ImportColorMode? ColorMode;
      public ARDB.ImportUnit? Unit;
      public double? CustomScale;

      object ICloneable.Clone() => Clone();
      public Options Clone() => MemberwiseClone() as Options;
    }

    #region ICloneable
    object ICloneable.Clone() => Clone();
    public CADReadOptions Clone() => new CADReadOptions(Value.Clone());
    #endregion

    public bool? OrientToView { get => Value.OrientToView; set => Value.OrientToView = value; }
    public bool? AutocorrectOffAxisLines { get => Value.AutocorrectOffAxisLines; set => Value.AutocorrectOffAxisLines = value; }
    public bool? VisibleLayersOnly { get => Value.VisibleLayersOnly; set => Value.VisibleLayersOnly = value; }
    public ICollection<string> LayerSelection { get => Value.LayerSelection; set => Value.LayerSelection = value is object ? new HashSet<string>(value) : null; }

    public ARDB.ImportColorMode? ColorMode { get => Value.ColorMode; set => Value.ColorMode = value; }
    public ARDB.ImportUnit? Unit { get => Value.Unit; set => Value.Unit = value; }
    public double? CustomScale { get => Value.CustomScale; set => Value.CustomScale = value; }

    public override bool ConvertTo<Q>(out Q target)
    {
      if (typeof(ARDB.BaseImportOptions).IsAssignableFrom(typeof(Q)))
      {
        var options = Activator.CreateInstance<Q>() as ARDB.BaseImportOptions;
        options.OrientToView = OrientToView ?? false;
        options.AutoCorrectAlmostVHLines = AutocorrectOffAxisLines ?? false;
        options.VisibleLayersOnly = VisibleLayersOnly ?? false;
        if(LayerSelection is object)
          options.SetLayerSelection(LayerSelection);
        options.ColorMode = ColorMode ?? ARDB.ImportColorMode.Preserved;
        options.Unit = Unit ?? ARDB.ImportUnit.Default;
        if (options.Unit == ARDB.ImportUnit.Custom)
          options.CustomScale = CustomScale ?? 1.0;

        target = (Q) (object) options;
        return true;
      }

      return base.ConvertTo(out target);
    }

    public ARDB.BaseImportOptions ToImportOptions(string path)
    {
      AssertValidPath(path);

      if (string.IsNullOrEmpty(path)) return null;
      if (path.EndsWith(".dwg", StringComparison.InvariantCultureIgnoreCase)) { ConvertTo(out ARDB.DWGImportOptions options); return options; }
      if (path.EndsWith(".dxf", StringComparison.InvariantCultureIgnoreCase)) { ConvertTo(out ARDB.DWGImportOptions options); return options; }
      if (path.EndsWith(".dgn", StringComparison.InvariantCultureIgnoreCase)) { ConvertTo(out ARDB.DGNImportOptions options); return options; }
      if (path.EndsWith(".sat", StringComparison.InvariantCultureIgnoreCase)) { ConvertTo(out ARDB.SATImportOptions options); return options; }
      if (path.EndsWith(".skp", StringComparison.InvariantCultureIgnoreCase)) { ConvertTo(out ARDB.SKPImportOptions options); return options; }
#if REVIT_2022
      if (path.EndsWith(".3dm", StringComparison.InvariantCultureIgnoreCase)) { ConvertTo(out ARDB.ImportOptions3DM options); return options; }
#endif
#if REVIT_2023
      if (path.EndsWith(".obj", StringComparison.InvariantCultureIgnoreCase)) { ConvertTo(out ARDB.OBJImportOptions options); return options; }
      if (path.EndsWith(".stl", StringComparison.InvariantCultureIgnoreCase)) { ConvertTo(out ARDB.STLImportOptions options); return options; }
#endif
#if REVIT_2024
      if (path.EndsWith(".stp", StringComparison.InvariantCultureIgnoreCase)) { ConvertTo(out ARDB.STEPImportOptions options); return options; }
      if (path.EndsWith(".step", StringComparison.InvariantCultureIgnoreCase)) { ConvertTo(out ARDB.STEPImportOptions options); return options; }
      if (path.EndsWith(".stpz", StringComparison.InvariantCultureIgnoreCase)) { ConvertTo(out ARDB.STEPImportOptions options); return options; }
#endif

      return null;
    }

    #region Extensions
    public static string FileFilter => $"All Supported Files ({string.Join(", ", SupportedExtensions.Select(x => $"*{x}"))})|{string.Join(";", SupportedExtensions)}";
    public static ISet<string> SupportedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      ".dwg", ".dxf", ".dgn", ".sat", ".skp",
#if REVIT_2022
      ".3dm",
#endif
#if REVIT_2023
      ".obj", ".stl",
#endif
#if REVIT_2024
      ".stp", ".step", ".stpz",
#endif
    };

    static void AssertValidPath(string path)
    {
      if (!System.IO.File.Exists(path))
        throw new Exceptions.RuntimeArgumentException("Path", $"File does not exist: {path}");

      var extension = System.IO.Path.GetExtension(path);
      if (extension is null || !SupportedExtensions.Contains(extension))
      {
        var supportedList = string.Join(", ", SupportedExtensions);
        throw new Exceptions.RuntimeArgumentException(nameof(path), $"File must have one of these extensions: {supportedList}.");
      }
    }
    #endregion
  }
}
