using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using Kernel.Attributes;

  [
    ComponentGuid("4615F47E-A20E-448A-A5DB-AF3473867E3D"),
    Name("Element Kind"),
    Description("Contains a collection of Revit element kind values"),
  ]
  public class ElementKind : GH_Flags<External.DB.ElementKind>
  {
    public override bool IsEmpty => Value == External.DB.ElementKind.None;
  }

  [
    ComponentGuid("83088978-8B44-4154-ABC9-A7CA53CA65E5"),
    Name("Parameter Class"),
    Description("Contains a collection of Revit Parameter class values"),
  ]
  public class ParameterClass : GH_Enum<External.DB.ParameterClass>
  {
    public override bool IsEmpty => Value == External.DB.ParameterClass.Any;
  }

  [
    ComponentGuid("2A5D36DD-CD94-4306-963B-D9312DAEB0F9"),
    Name("Parameter Scope"),
    Description("Contains a collection of Revit parameter scope type values"),
  ]
  public class ParameterScope : GH_Enum<External.DB.ParameterScope>
  {
    public override bool IsEmpty => Value == External.DB.ParameterScope.Unknown;
  }

  [
    ComponentGuid("A3621A84-190A-48C2-9B0C-F5784B78089C"),
    Name("Storage Type"),
    Description("Contains a collection of Revit storage type values"),
  ]
  public class StorageType : GH_Enum<ARDB.StorageType>
  {
    public override bool IsEmpty => Value == ARDB.StorageType.None;
  }

  [
    ComponentGuid("ABE3F6CB-CE2D-4DBE-AB81-A6CB884D7DE1"),
    Name("Unit System"),
    Description("Contains a collection of Revit unit system values"),
  ]
  public class UnitSystem : GH_Enum<ARDB.UnitSystem> { }

  [
    ComponentGuid("195B9D7E-D4B0-4335-A442-3C2FA40794A2"),
    Name("Category Type"),
    Description("Contains a collection of Revit parameter category type values"),
  ]
  public class CategoryType : GH_Enum<ARDB.CategoryType>
  {
    public CategoryType() : base(ARDB.CategoryType.Invalid) { }
    public CategoryType(ARDB.CategoryType value) : base(value) { }

    public override bool IsEmpty => Value == ARDB.CategoryType.Invalid;
    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) ARDB.CategoryType.Model,            "Model"       },
        { (int) ARDB.CategoryType.Annotation,       "Annotation"  },
        { (int) ARDB.CategoryType.Internal,         "Internal"    },
        { (int) ARDB.CategoryType.AnalyticalModel,  "Analytical"  },
      }
    );
  }

  [
    ComponentGuid("CC3DB1A4-5C24-478D-A784-00725BB1E1F6"),
    Name("Element On Phase Status"),
    Description("Represents the statuses that an element can have with respect to a given phase."),
  ]
  public class ElementOnPhaseStatus : GH_Enum<ARDB.ElementOnPhaseStatus>
  {
    public ElementOnPhaseStatus() : base() { }
    public ElementOnPhaseStatus(ARDB.ElementOnPhaseStatus value) : base(value) { }

    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) ARDB.ElementOnPhaseStatus.None,       "<None>"      },
        { (int) ARDB.ElementOnPhaseStatus.Past,       "Past"        },
        { (int) ARDB.ElementOnPhaseStatus.Existing,   "Existing"    },
        { (int) ARDB.ElementOnPhaseStatus.Demolished, "Demolished"  },
        { (int) ARDB.ElementOnPhaseStatus.New,        "New"         },
        { (int) ARDB.ElementOnPhaseStatus.Temporary,  "Temporary"   },
        { (int) ARDB.ElementOnPhaseStatus.Future,     "Future"      },
      }
    );
  }

  [
    ComponentGuid("1AF2E8BF-5FAF-41AD-9A2F-EB96A706587C"),
    Name("Graphics Style Type"),
    Description("Contains a collection of graphics style type values"),
  ]
  public class GraphicsStyleType : GH_Enum<ARDB.GraphicsStyleType>
  {
    public GraphicsStyleType() : base(ARDB.GraphicsStyleType.Projection) { }
    public GraphicsStyleType(ARDB.GraphicsStyleType value) : base(value) { }
  }

  [
    ComponentGuid("F992A251-4085-4525-A514-298F3155DF8A"),
    Name("Detail Level"),
    Description("Contains a collection of view detail level values"),
  ]
  public class ViewDetailLevel : GH_Enum<ARDB.ViewDetailLevel>
  {
    public override bool IsEmpty => Value == ARDB.ViewDetailLevel.Undefined;
  }

  [
    ComponentGuid("83380EFC-D2E2-3A9E-A1D7-939EC71852DD"),
    Name("View Discipline"),
    Description("Contains a collection of Revit view discipline values"),
  ]
  public class ViewDiscipline : GH_Enum<ARDB.ViewDiscipline>
  {
    public override bool IsEmpty => Value == default;
  }

  [
    ComponentGuid("485C3278-0D1A-445D-B3DA-75FB8CD38CF9"),
    Name("View Family"),
    Description("Contains a collection of Revit view family values"),
  ]
  public class ViewFamily : GH_Enum<ARDB.ViewFamily>
  {
    public ViewFamily() : base(ARDB.ViewFamily.Invalid) { }
    public ViewFamily(ARDB.ViewFamily value) : base(value) { }
    public override bool IsEmpty => Value == ARDB.ViewFamily.Invalid;

    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) ARDB.ViewFamily.ThreeDimensional,         "3D View"                   },
        { (int) ARDB.ViewFamily.FloorPlan,                "Floor Plan"                },
        { (int) ARDB.ViewFamily.CeilingPlan,              "Ceiling Plan"              },
        { (int) ARDB.ViewFamily.StructuralPlan,           "Structural Plan"           },
        { (int) ARDB.ViewFamily.AreaPlan,                 "Area Plan"                 },
        { (int) ARDB.ViewFamily.Elevation,                "Elevation"                 },
        { (int) ARDB.ViewFamily.Section,                  "Section"                   },
        { (int) ARDB.ViewFamily.Detail,                   "Detail View"               },
        { (int) ARDB.ViewFamily.Drafting,                 "Drafting View"             },
        { (int) ARDB.ViewFamily.ImageView,                "Rendering"                 },
        { (int) ARDB.ViewFamily.Walkthrough,              "Walkthrough"               },
        { (int) ARDB.ViewFamily.Legend,                   "Legend"                    },
        { (int) ARDB.ViewFamily.Sheet,                    "Sheet"                     },
        { (int) ARDB.ViewFamily.Schedule,                 "Schedule"                  },
        { (int) ARDB.ViewFamily.GraphicalColumnSchedule,  "Graphical Column Schedule" },
        { (int) ARDB.ViewFamily.PanelSchedule,            "Panel Schedule"            },
        { (int) ARDB.ViewFamily.CostReport,               "Cost Report"               },
        { (int) ARDB.ViewFamily.LoadsReport,              "Loads Report"              },
        { (int) ARDB.ViewFamily.PressureLossReport,       "Pressure Loss Report"      },
#if REVIT_2020
        { (int) ARDB.ViewFamily.SystemsAnalysisReport,    "Systems Analysis Report"   },
#endif
      }
    );
  }

  [
    ComponentGuid("BF051011-660D-39E7-86ED-20EEE3A68DB0"),
    Name("View Type"),
    Description("Contains a collection of Revit view type values"),
  ]
  public class ViewType : GH_Enum<ARDB.ViewType>
  {
    public override bool IsEmpty => Value == ARDB.ViewType.Undefined;
    public ViewType() { }
    public ViewType(ARDB.ViewType value) : base(value) { }
    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) ARDB.ViewType.FloorPlan,            "Floor Plan" },
        { (int) ARDB.ViewType.CeilingPlan,          "Ceiling Plan" },
        { (int) ARDB.ViewType.Elevation,            "Elevation" },
        { (int) ARDB.ViewType.ThreeD,               "3D View" },
        { (int) ARDB.ViewType.Schedule,             "Schedule" },
        { (int) ARDB.ViewType.DrawingSheet,         "Sheet" },
        { (int) ARDB.ViewType.ProjectBrowser,       "Project Browser" },
        { (int) ARDB.ViewType.Report,               "Report" },
        { (int) ARDB.ViewType.DraftingView,         "Drafting" },
        { (int) ARDB.ViewType.Legend,               "Legend" },
        { (int) ARDB.ViewType.SystemBrowser,        "System Browser" },
        { (int) ARDB.ViewType.EngineeringPlan,      "Structural Plan" },
        { (int) ARDB.ViewType.AreaPlan,             "Area Plan" },
        { (int) ARDB.ViewType.Section,              "Section" },
        { (int) ARDB.ViewType.Detail,               "Detail" },
        { (int) ARDB.ViewType.CostReport,           "Cost Report" },
        { (int) ARDB.ViewType.LoadsReport,          "Loads Report" },
        { (int) ARDB.ViewType.PresureLossReport,    "Presure Loss Report" },
        { (int) ARDB.ViewType.ColumnSchedule,       "Column Schedule" },
        { (int) ARDB.ViewType.PanelSchedule,        "Panel Schedule" },
        { (int) ARDB.ViewType.Walkthrough,          "Walkthrough" },
        { (int) ARDB.ViewType.Rendering,            "Rendering" },
#if REVIT_2020
        { (int) ARDB.ViewType.SystemsAnalysisReport,"Systems Analysis Report" },
#endif
        { (int) ARDB.ViewType.Internal,             "Internal" },
      }
    );
  }

  [
    ComponentGuid("2FDE857C-EDAB-4999-B6AE-DC531DD2AD18"),
    Name("Image Fit direction type"),
    Description("Contains a collection of Revit fit direction type values"),
  ]
  public class FitDirectionType : GH_Enum<ARDB.FitDirectionType>
  {
    public FitDirectionType() : base(ARDB.FitDirectionType.Horizontal) { }
    public FitDirectionType(ARDB.FitDirectionType value) : base(value) { }
  }

  [
    ComponentGuid("C6132D3E-1BA4-4BF5-B40C-D08F81A79AB1"),
    Name("Image Resolution"),
    Description("Contains a collection of Revit image resolution values"),
  ]
  public class ImageResolution : GH_Enum<ARDB.ImageResolution>
  {
    public ImageResolution() : base(ARDB.ImageResolution.DPI_72) { }
    public ImageResolution(ARDB.ImageResolution value) : base(value) { }

    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) ARDB.ImageResolution.DPI_72,   "72 DPI" },
        { (int) ARDB.ImageResolution.DPI_150, "150 DPI" },
        { (int) ARDB.ImageResolution.DPI_300, "300 DPI" },
        { (int) ARDB.ImageResolution.DPI_600, "600 DPI" },
      }
    );
  }

  [
    ComponentGuid("F6BABEFF-C4AD-49D0-81D6-9C3CD021DD45"),
    Name("Image FileType"),
    Description("Contains a collection of Revit image file type values"),
  ]
  public class ImageFileType : GH_Enum<ARDB.ImageFileType>
  {
    public ImageFileType() : base(ARDB.ImageFileType.BMP) { }
    public ImageFileType(ARDB.ImageFileType value) : base(value) { }

    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) ARDB.ImageFileType.BMP,           "BMP" },
        { (int) ARDB.ImageFileType.JPEGLossless,  "JPEG-Lossless" },
        { (int) ARDB.ImageFileType.JPEGMedium,    "JPEG-Medium" },
        { (int) ARDB.ImageFileType.JPEGSmallest,  "JPEG-Smallest" },
        { (int) ARDB.ImageFileType.PNG,           "PNG" },
        { (int) ARDB.ImageFileType.TARGA,         "TARGA" },
        { (int) ARDB.ImageFileType.TIFF,          "TIFF" },
      }
    );
  }

  [
    ComponentGuid("2A3E4872-EF41-442A-B886-8B7DBA73DFE2"),
    Name("Wall Location Line"),
    Description("Contains a collection of Revit wall location line values"),
  ]
  public class WallLocationLine : GH_Enum<ARDB.WallLocationLine>
  {
    public WallLocationLine() : base() { }
    public WallLocationLine(ARDB.WallLocationLine value) : base(value) { }

    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) ARDB.WallLocationLine.WallCenterline,      "Wall Centerline"       },
        { (int) ARDB.WallLocationLine.CoreCenterline,      "Core Centerline"       },
        { (int) ARDB.WallLocationLine.FinishFaceExterior,  "Finish Face: Exterior" },
        { (int) ARDB.WallLocationLine.FinishFaceInterior,  "Finish Face: Interior" },
        { (int) ARDB.WallLocationLine.CoreExterior,        "Core Face: Exterior"   },
        { (int) ARDB.WallLocationLine.CoreInterior,        "Core Face: Interior"   },
      }
    );
  }

  [
    ComponentGuid("2FEFFADD-BD29-4B19-9682-4CC5947DF11C"),
    Name("Wall System Family"),
    Description("Contains a collection of Revit wall system family"),
  ]
  public class WallSystemFamily : GH_Enum<ARDB.WallKind>
  {
    public WallSystemFamily() : base(ARDB.WallKind.Unknown) { }
    public WallSystemFamily(ARDB.WallKind value) : base(value) { }
    public override bool IsEmpty => Value == ARDB.WallKind.Unknown;

    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) ARDB.WallKind.Basic,      "Basic Wall"    },
        { (int) ARDB.WallKind.Curtain,    "Curtain Wall"  },
        { (int) ARDB.WallKind.Stacked,    "Stacked Wall"  },
      }
    );
  }

  [
    ComponentGuid("F069304B-4066-4D23-9542-7AC54CED3C92"),
    Name("Wall Function"),
    Description("Contains a collection of Revit wall function"),
  ]
  public class WallFunction : GH_Enum<ARDB.WallFunction> {
    public WallFunction() : base() { }
    public WallFunction(ARDB.WallFunction value) : base(value) { }
  }

  [
    ComponentGuid("7A71E012-6E92-493D-960C-83BE3C50ECAE"),
    Name("Wall Wrapping"),
    Description("Contains a collection of Revit wall wrapping option"),
  ]
  public class WallWrapping : GH_Enum<External.DB.WallWrapping>
  {
    public WallWrapping() : base() { }
    public WallWrapping(External.DB.WallWrapping value) : base(value) { }
  }

  [
    ComponentGuid("2F1CE55B-FD85-4EC5-8638-8DA06932DE0E"),
    Name("Structural Wall Usage"),
    Description("Contains a collection of Revit structural wall usage values"),
  ]
  public class StructuralWallUsage : GH_Enum<ARDB.Structure.StructuralWallUsage> {
    public StructuralWallUsage() : base() { }
    public StructuralWallUsage(ARDB.Structure.StructuralWallUsage value) : base(value) { }

    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) ARDB.Structure.StructuralWallUsage.NonBearing,  "Non-Bearing"         },
        { (int) ARDB.Structure.StructuralWallUsage.Bearing,     "Bearing"             },
        { (int) ARDB.Structure.StructuralWallUsage.Shear,       "Shear"               },
        { (int) ARDB.Structure.StructuralWallUsage.Combined,    "Structural combined" },
      }
    );
  }

  [
    ComponentGuid("A8122936-6A69-4D78-B1F5-13FD8F2144A5"),
    Name("End Cap Condition"),
    Description("Represents end cap condition of a compound structure"),
  ]
  public class EndCapCondition : GH_Enum<ARDB.EndCapCondition>
  {
    public EndCapCondition() : base() { }
    public EndCapCondition(ARDB.EndCapCondition value) : base(value) { }

    public override bool IsEmpty => Value == ARDB.EndCapCondition.None;
    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) ARDB.EndCapCondition.None,      "<empty>"  },
        { (int) ARDB.EndCapCondition.Exterior,  "Exterior" },
        { (int) ARDB.EndCapCondition.Interior,  "Interior" },
        { (int) ARDB.EndCapCondition.NoEndCap,  "None"     },
      }
    );
  }

  [
    ComponentGuid("68D22DE2-CDD5-4441-9745-462E28030A03"),
    Name("Deck Embedding Type"),
    Description("Represents deck embedding type of a compound structure layer"),
  ]
  public class DeckEmbeddingType : GH_Enum<ARDB.StructDeckEmbeddingType>
  {
    public DeckEmbeddingType() : base(ARDB.StructDeckEmbeddingType.Invalid) { }
    public DeckEmbeddingType(ARDB.StructDeckEmbeddingType value) : base(value) { }

    public override bool IsEmpty => Value == ARDB.StructDeckEmbeddingType.Invalid;
  }

  [
    ComponentGuid("4220F183-C273-4342-9885-3DEB13531731"),
    Name("Layer Function"),
    Description("Represents layer function of a wall compound structure layer"),
  ]
  public class LayerFunction : GH_Enum<ARDB.MaterialFunctionAssignment>
  {
    public LayerFunction() : base() { }
    public LayerFunction(ARDB.MaterialFunctionAssignment value) : base(value) { }

    public override bool IsEmpty => Value == ARDB.MaterialFunctionAssignment.None;

    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) ARDB.MaterialFunctionAssignment.None,                 "<empty>"               },
        { (int) ARDB.MaterialFunctionAssignment.Structure,            "Structure [1]"         },
        { (int) ARDB.MaterialFunctionAssignment.Substrate,            "Substrate [2]"         },
        { (int) ARDB.MaterialFunctionAssignment.Insulation,           "Thermal/Air Layer [3]" },
        { (int) ARDB.MaterialFunctionAssignment.Finish1,              "Finish 1 [4]"          },
        { (int) ARDB.MaterialFunctionAssignment.Finish2,              "Finish 2 [5]"          },
        { (int) ARDB.MaterialFunctionAssignment.Membrane,             "Membrane Layer"        },
        { (int) ARDB.MaterialFunctionAssignment.StructuralDeck,       "Structural Deck [1]"   },
      }
    );
  }

  [
    ComponentGuid("BF8B68B5-4E24-4602-8065-7EE90536B90E"),
    Name("Opening Wrapping Condition"),
    Description("Represents compound structure layers wrapping at openings setting"),
  ]
  public class OpeningWrappingCondition : GH_Enum<ARDB.OpeningWrappingCondition>
  {
    public OpeningWrappingCondition() : base() { }
    public OpeningWrappingCondition(ARDB.OpeningWrappingCondition value) : base(value) { }

    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) ARDB.OpeningWrappingCondition.None,                 "None"                },
        { (int) ARDB.OpeningWrappingCondition.Exterior,             "Exterior"            },
        { (int) ARDB.OpeningWrappingCondition.Interior,             "Interior"            },
        { (int) ARDB.OpeningWrappingCondition.ExteriorAndInterior,  "Exterior & Interior" },
      }
    );
  }

  [
    ComponentGuid("621785D8-363C-46EF-A920-B8CF0026B4CF"),
    Name("Curtain Grid Align Type"),
    Description("Represents alignment type for curtain grids at either direction"),
  ]
  public class CurtainGridAlignType : GH_Enum<ARDB.CurtainGridAlignType>
  {
    public CurtainGridAlignType() : base() { }
    public CurtainGridAlignType(ARDB.CurtainGridAlignType value) : base(value) { }
  }

  [
    ComponentGuid("A734FF65-D9E6-4C8C-A413-B5EACD6E3062"),
    Name("Curtain Grid Layout"),
    Description("Represents layout for curtain grids at either direction"),
  ]
  public class CurtainGridLayout : GH_Enum<External.DB.CurtainGridLayout>
  {
    public CurtainGridLayout() : base() { }
    public CurtainGridLayout(External.DB.CurtainGridLayout value) : base(value) { }

    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) External.DB.CurtainGridLayout.None,            "None"             },
        { (int) External.DB.CurtainGridLayout.FixedDistance,   "Fixed Distance"   },
        { (int) External.DB.CurtainGridLayout.FixedNumber,     "Fixed Number"     },
        { (int) External.DB.CurtainGridLayout.MaximumSpacing,  "Maximum Spacing"  },
        { (int) External.DB.CurtainGridLayout.MinimumSpacing,  "Minimum Spacing"  },
      }
    );
  }

  [
    ComponentGuid("371E482B-BB95-4D9D-962F-00867E01AB35"),
    Name("Curtain Grid Join Condition"),
    Description("Represents join condition for curtain grids at either direction"),
  ]
  public class CurtainGridJoinCondition : GH_Enum<External.DB.CurtainGridJoinCondition>
  {
    public CurtainGridJoinCondition() : base() { }
    public CurtainGridJoinCondition(External.DB.CurtainGridJoinCondition value) : base(value) { }
    public override bool IsEmpty => Value == External.DB.CurtainGridJoinCondition.NotDefined;

    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) External.DB.CurtainGridJoinCondition.NotDefined,                        "Not Defined" },
        { (int) External.DB.CurtainGridJoinCondition.VerticalGridContinuous,            "Vertical Grid Continuous" },
        { (int) External.DB.CurtainGridJoinCondition.HorizontalGridContinuous,          "Horizontal Grid Continuous" },
        { (int) External.DB.CurtainGridJoinCondition.BorderAndVerticalGridContinuous,   "Border & Vertical Grid Continuous" },
        { (int) External.DB.CurtainGridJoinCondition.BorderAndHorizontalGridContinuous, "Border & Horizontal Grid Continuous" },
      }
    );
  }

  [
    ComponentGuid("C61AA1B8-4CB2-44A0-9217-091E151D1D0A"),
    Name("Curtain Mullion System Family"),
    Description("Represents builtin curtain mullion system families"),
  ]
  public class CurtainMullionSystemFamily : GH_Enum<External.DB.CurtainMullionSystemFamily>
  {
    public CurtainMullionSystemFamily() : base(External.DB.CurtainMullionSystemFamily.Unknown) { }
    public CurtainMullionSystemFamily(External.DB.CurtainMullionSystemFamily value) : base(value) { }
    public override bool IsEmpty => Value == External.DB.CurtainMullionSystemFamily.Unknown;

    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) External.DB.CurtainMullionSystemFamily.Unknown,         "Unknown"         },
        { (int) External.DB.CurtainMullionSystemFamily.Rectangular,     "Rectangular"     },
        { (int) External.DB.CurtainMullionSystemFamily.Circular,        "Circular"        },
        { (int) External.DB.CurtainMullionSystemFamily.LCorner,         "L Corner"        },
        { (int) External.DB.CurtainMullionSystemFamily.TrapezoidCorner, "Trapezoid Corner"},
        { (int) External.DB.CurtainMullionSystemFamily.QuadCorner,      "Quad Corner"     },
        { (int) External.DB.CurtainMullionSystemFamily.VCorner,         "V Corner"        },
      }
    );
  }

  [
    ComponentGuid("9F9D90FC-06FF-4908-B67E-ED63B089937E"),
    Name("Curtain Panel System Family"),
    Description("Represents builtin curtain panel system families"),
  ]
  public class CurtainPanelSystemFamily : GH_Enum<External.DB.CurtainPanelSystemFamily>
  {
    public CurtainPanelSystemFamily() : base() { }
    public CurtainPanelSystemFamily(External.DB.CurtainPanelSystemFamily value) : base(value) { }
    public override bool IsEmpty => Value == External.DB.CurtainPanelSystemFamily.Unknown;
  }

  [
    ComponentGuid("CF3ACC14-D9F3-4169-985B-C207260250DA"),
    Name("Floor Function"),
    Description("Represents builtin floor function"),
  ]
  public class FloorFunction : GH_Enum<External.DB.FloorFunction>
  {
  }

  [
    ComponentGuid("07b212f2-3e72-4f1a-a178-54481fcf3df3"),
    Name("Physical Asset Class"),
    Description("Represents physical asset class"),
  ]
  public class StructuralAssetClass : GH_Enum<ARDB.StructuralAssetClass>
  {
    public override bool IsEmpty => Value == ARDB.StructuralAssetClass.Undefined;
  }

  [
    ComponentGuid("cf6a7af7-f588-486a-95e0-a398a5410e24"),
    Name("Material Behavior"),
    Description("Represents material behavior of physical or thermal assets"),
  ]
  public class StructuralBehavior : GH_Enum<ARDB.StructuralBehavior>
  {
  }

  [
    ComponentGuid("6a2b7564-9dd1-4cfc-a539-a352cb39cb7c"),
    Name("Thermal Material Class"),
    Description("Represents thermal material class"),
  ]
  public class ThermalMaterialType : GH_Enum<ARDB.ThermalMaterialType>
  {
    public override bool IsEmpty => Value == ARDB.ThermalMaterialType.Undefined;
  }

  [
    ComponentGuid("84DAF907-5D71-4766-9776-B6B86069A2B9"),
    Name("Workset Kind"),
    Description("Represents workset kind"),
  ]
  public class WorksetKind : GH_Enum<ARDB.WorksetKind>
  {
    public override bool IsEmpty => Value == ARDB.WorksetKind.OtherWorkset;

    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) ARDB.WorksetKind.OtherWorkset,    "Other"             },
        { (int) ARDB.WorksetKind.StandardWorkset, "Project Standards" },
        { (int) ARDB.WorksetKind.UserWorkset,     "User-Created"      },
        { (int) ARDB.WorksetKind.FamilyWorkset,   "Families"          },
        { (int) ARDB.WorksetKind.ViewWorkset,     "Views"             },
      }
    );
  }


  [
    ComponentVersion(introduced: "1.2"),
    ComponentGuid("CE75343A-FC1B-4246-B7AD-A0FC0DE050A4"),
    Name("Checkout Status"),
    Description("Represents checkout status"),
  ]
  public class CheckoutStatus : GH_Enum<ARDB.CheckoutStatus>
  {
    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) ARDB.CheckoutStatus.OwnedByCurrentUser, "Owned by current user" },
        { (int) ARDB.CheckoutStatus.OwnedByOtherUser,   "Owned by other user" },
        { (int) ARDB.CheckoutStatus.NotOwned,           "Not Owned" },
      }
    );
  }

  [
    ComponentVersion(introduced: "1.6"),
    ComponentGuid("DC6E20DD-37C9-4E17-A415-298614CFB00E"),
    Name("Model Updates Status"),
    Description("Represents model update status"),
  ]
  public class ModelUpdatesStatus : GH_Enum<ARDB.ModelUpdatesStatus>
  {
    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) ARDB.ModelUpdatesStatus.CurrentWithCentral, "Current With Central" },
        { (int) ARDB.ModelUpdatesStatus.NotYetInCentral,    "Not Yet In Central" },
        { (int) ARDB.ModelUpdatesStatus.DeletedInCentral,   "Deleted In Central" },
        { (int) ARDB.ModelUpdatesStatus.UpdatedInCentral,   "Updated In Central" },
      }
    );
  }
}
