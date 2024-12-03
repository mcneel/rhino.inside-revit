using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using RhinoInside.Revit.GH.Parameters;
using Autodesk.Revit.DB.Structure;
using System.Collections.Generic;
using RhinoInside.Revit.GH.Types;
using System.Linq;
using RhinoInside.Revit.Convert.System.Collections.Generic;

namespace RhinoInside.Revit.GH.Components.Structure
{
#if REVIT_2023
  using ARDB_AnalyticalPanel = ARDB.Structure.AnalyticalPanel;
  using ARDB_AnalyticalOpening = ARDB.Structure.AnalyticalOpening;
#else
  using ARDB_AnalyticalPanel = ARDB.Structure.AnalyticalModelSurface;
  using ARDB_AnalyticalOpening = ARDB.Structure.AnalyticalModelSurface;
#endif

  [ComponentVersion(introduced: "1.27"), ComponentRevitAPIVersion(min: "2023.0")]
  public class AddAnalyticalPanelByElement : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("F2228146-1A4B-42BB-AA90-5EBE35F70160");
#if REVIT_2023
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
#else
    public override GH_Exposure Exposure => GH_Exposure.hidden;
#endif
    public AddAnalyticalPanelByElement() : base
    (
      name: "Add Analytical Panel (Element)",
      nickname: "AP-Element",
      description: "Given an element, it extract its analytical panel to the active Revit document",
      category: "Revit",
      subCategory: "Structure"
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
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Graphical element",
          Access = GH_ParamAccess.item
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.AnalyticalPanel()
        {
          Name = _AnalyticalPanel_,
          NickName = _AnalyticalPanel_.Substring(0, 1),
          Description = $"Output {_AnalyticalPanel_}",
        }
      )
    };

    const string _AnalyticalPanel_ = "Analytical Panel";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
#if REVIT_2023
      ARDB.BuiltInParameter.STRUCTURAL_ANALYZES_AS,
      ARDB.BuiltInParameter.ANALYTICAL_ELEMENT_STRUCTURAL_ROLE,
      ARDB.BuiltInParameter.ANALYTICAL_PANEL_THICKNESS
#endif
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
#if REVIT_2023
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB_AnalyticalPanel>
      (
        doc.Value, _AnalyticalPanel_, analyticalPanel =>
        {
          var tol = GeometryTolerance.Model;

          // Input
          if (!Params.GetData(DA, "Element", out Types.GraphicalElement element)) return null;

          //Compute
          bool isAnalyticalPanel = false;
          IList<Curve> boundary = null;
          switch (element)
          {
            case Types.FamilyInstance familyInstance:

              switch (familyInstance.Value.StructuralType)
              {
                case ARDB.Structure.StructuralType.Footing:
                  isAnalyticalPanel = true;
                  //boundary = familyInstance.

                  var g = familyInstance.Value;
                  break;

                case ARDB.Structure.StructuralType.UnknownFraming:
                  this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"This element has an unknown framing type: {element.Id}");
                  break;

                case ARDB.Structure.StructuralType.NonStructural:
                default:
                  this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"This element is non structural: {element.Id}");
                  break;
              }
              break;

            case ISketchAccess sketchAccess when element is Types.Wall:
              isAnalyticalPanel = true;
              //boundary = element.Surface.ToBrep().Loops;
              break;

            case ISketchAccess sketchAccess:
              isAnalyticalPanel = true;
              boundary = sketchAccess.Sketch.Profiles.ToList();
              break;

            default:
              this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"The element is not valid to create an analytical panel: {element.Id}";
              break;
          }

          if (isAnalyticalPanel)
            analyticalPanel = Create(doc.Value, boundary);

          DA.SetData(_AnalyticalPanel_, analyticalPanel);
          return analyticalPanel;
        }
      );
#endif
    }

#if REVIT_2023
    private ARDB_AnalyticalPanel Create(ARDB.Document doc, IList<Curve> boundary)
    {
      if (boundary.Count < 1) return null;

      var curveLoop = boundary.ConvertAll(x => x.ToBoundedCurveLoop());

      if (curveLoop is null)
        throw new ArgumentException("Failed to convert boundary curves to CurveLoop.", nameof(boundary));

      var panel = ARDB_AnalyticalPanel.Create(doc, curveLoop[0]);

      for (int b = 1; b < boundary.Count; ++b)
        ARDB_AnalyticalOpening.Create(doc, curveLoop[b], panel.Id);

      return panel;
    }
#endif
  }
}
