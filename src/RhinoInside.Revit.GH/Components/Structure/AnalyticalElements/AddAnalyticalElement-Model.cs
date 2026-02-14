using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Structure
{
#if REVIT_2023
  using ARDB_AnalyticalElement = ARDB.Structure.AnalyticalElement;
  using ARDB_AnalyticalMember = ARDB.Structure.AnalyticalMember;
  using ARDB_AnalyticalPanel = ARDB.Structure.AnalyticalPanel;
  using ARDB_AnalyticalOpening = ARDB.Structure.AnalyticalOpening;
#else
  using ARDB_AnalyticalElement = ARDB.Structure.AnalyticalModel;
  using ARDB_AnalyticalMember = ARDB.Structure.AnalyticalModelStick;
  using ARDB_AnalyticalPanel = ARDB.Structure.AnalyticalModelSurface;
  using ARDB_AnalyticalOpening = ARDB.Structure.AnalyticalModelSurface;
#endif

  [ComponentVersion(introduced: "1.27"), ComponentRevitAPIVersion(min: "2023.0")]
  public class AddAnalyticalElementByModel : AddAnalyticalElement
  {
    public override Guid ComponentGuid => new Guid("AC26C810-2043-4666-B16E-8484D9DCF7DE");
#if REVIT_2023
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
#else
    public override GH_Exposure Exposure => GH_Exposure.hidden;
#endif
    public AddAnalyticalElementByModel() : base
    (
      name: "Add Analytical Element",
      nickname: "ME-Analytical",
      description: "Given a model element, it adds an analytical element representation to the active Revit document",
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
          Name = "Model Element",
          NickName = "ME",
          Description = "Model element",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.AnalyticalElement()
          {
            Name = _AnalyticalElement_,
            NickName = "AE",
            Description = $"Output {_AnalyticalElement_}",
            Access = GH_ParamAccess.list
          }
      )
    };

    const string _AnalyticalElement_ = "Analytical Element";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
#if REVIT_2023
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;
      if (!Params.GetData(DA, "Model Element", out Types.GraphicalElement element)) return;
      if (!element.Structural)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input model element is not structural.");
        return;
      }

      // Checking input
      var tol = GeometryTolerance.Model;
      Curve curve = null;
      double height = 0.0;
      Brep boundary = null;
      var thickness = 0.0;
      ARDB.ElementId typeId = default;
      ARDB.ElementId materialId = default;
      var structuralRole = ARDB.Structure.AnalyticalStructuralRole.Unset;
      switch (element)
      {
        case Types.StructuralBeam beam:
          structuralRole = ARDB.Structure.AnalyticalStructuralRole.StructuralRoleBeam;
          typeId = beam.Value.GetTypeId();
          materialId = beam.Value.StructuralMaterialId;
          break;

        case Types.StructuralBrace brace:
          structuralRole = ARDB.Structure.AnalyticalStructuralRole.StructuralRoleGirder;
          typeId = brace.Value.GetTypeId();
          materialId = brace.Value.StructuralMaterialId;
          break;

        case Types.StructuralColumn column:
          structuralRole = ARDB.Structure.AnalyticalStructuralRole.StructuralRoleColumn;
          typeId = column.Value.GetTypeId();
          materialId = column.Value.StructuralMaterialId;
          break;

        case Types.StructuralFraming framing:
          structuralRole = ARDB.Structure.AnalyticalStructuralRole.StructuralRoleMember;
          typeId = framing.Value.GetTypeId();
          materialId = framing.Value.StructuralMaterialId;
          break;

        case Types.Wall wall:
          if (wall.Sketch is Types.Sketch sketch && sketch.IsValid)
          {
            boundary = sketch.TrimmedSurface;
          }
          else if (wall.Curve.IsLinear())
          {
            boundary = wall.TrimmedSurface;
          }
          else if (wall.SlantAngle == 0.0)
          {
            curve = wall.Curve;
            curve.Translate(0.0, 0.0, wall.Value.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_OFFSET).AsDouble() * Revit.ModelUnits);
            height = wall.Value.get_Parameter(ARDB.BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble();
          }
          else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Slanted curved walls are not supported.");

          structuralRole = ARDB.Structure.AnalyticalStructuralRole.StructuralRoleWall;
          using (var structure = wall.Value.WallType.GetCompoundStructure())
          {
            var index = structure.StructuralMaterialIndex;
            if (index > 0)
            {
              thickness = structure.GetLayerWidth(index);
              materialId = structure.GetMaterialId(index);
            }
          }
          break;

        case Types.Floor floor:
          boundary = floor.Sketch.TrimmedSurface;
          structuralRole = ARDB.Structure.AnalyticalStructuralRole.StructuralRoleFloor;
          using (var structure = floor.Value.FloorType.GetCompoundStructure())
          {
            var index = structure.StructuralMaterialIndex;
            if (index > 0)
            {
              thickness = structure.GetLayerWidth(index);
              materialId = structure.GetMaterialId(index);
            }
          }
          break;
      }

      var analyticalElements = new List<ARDB_AnalyticalElement>();

      // Compute
      switch (structuralRole)
      {
        case ARDB.Structure.AnalyticalStructuralRole.Unset:
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Model element is not supported for creating an analytical element. {{{element.Id}}}");
          return;

        case ARDB.Structure.AnalyticalStructuralRole.StructuralRoleBeam:
          var beam = ReconstructElement<ARDB_AnalyticalMember>
          (
            doc.Value, _AnalyticalElement_, analyticalMember =>
            {
              analyticalMember = Reconstruct(analyticalMember, doc.Value, element.Curve.ToCurve());
              analyticalMember.StructuralRole = structuralRole;
              analyticalMember.SectionTypeId = typeId;
              analyticalMember.MaterialId = materialId;
              return analyticalMember;
            }
          );
          analyticalElements.Add(beam);
          break;

        case ARDB.Structure.AnalyticalStructuralRole.StructuralRoleColumn:
          var column = ReconstructElement<ARDB_AnalyticalMember>
          (
            doc.Value, _AnalyticalElement_, analyticalMember =>
            {
              analyticalMember = Reconstruct(analyticalMember, doc.Value, element.Curve.ToCurve());
              analyticalMember.StructuralRole = structuralRole;
              analyticalMember.SectionTypeId = typeId;
              analyticalMember.MaterialId = materialId;
              return analyticalMember;
            }
          );
          analyticalElements.Add(column);
          break;

        case ARDB.Structure.AnalyticalStructuralRole.StructuralRoleGirder:
          var girder = ReconstructElement<ARDB_AnalyticalMember>
          (
            doc.Value, _AnalyticalElement_, analyticalMember =>
            {
              analyticalMember = Reconstruct(analyticalMember, doc.Value, element.Curve.ToCurve());
              analyticalMember.StructuralRole = structuralRole;
              analyticalMember.SectionTypeId = typeId;
              analyticalMember.MaterialId = materialId;
              return analyticalMember;
            }
          );
          analyticalElements.Add(girder);
          break;

        case ARDB.Structure.AnalyticalStructuralRole.StructuralRoleMember:
          var member = ReconstructElement<ARDB_AnalyticalMember>
          (
            doc.Value, _AnalyticalElement_, analyticalMember =>
            {
              analyticalMember = Reconstruct(analyticalMember, doc.Value, element.Curve.ToCurve());
              analyticalMember.StructuralRole = structuralRole;
              analyticalMember.SectionTypeId = typeId;
              analyticalMember.MaterialId = materialId;
              return analyticalMember;
            }
          );
          analyticalElements.Add(member);
          break;

        case ARDB.Structure.AnalyticalStructuralRole.StructuralRoleFloor:
          foreach (var face in boundary.Faces)
          {
            var panel = ReconstructElement<ARDB_AnalyticalPanel>
            (
              doc.Value, _AnalyticalElement_, analyticalPanel =>
              {
                analyticalPanel = Reconstruct(analyticalPanel, doc.Value, face);
                analyticalPanel.StructuralRole = structuralRole;
                analyticalPanel.Thickness = thickness;
                return analyticalPanel;
              }
            );

            analyticalElements.Add(panel);
          }
          break;

        case ARDB.Structure.AnalyticalStructuralRole.StructuralRoleWall:
          if (curve is object)
          {
            var panel = ReconstructElement<ARDB_AnalyticalPanel>
            (
              doc.Value, _AnalyticalElement_, analyticalPanel =>
              {
                analyticalPanel = Reconstruct(analyticalPanel, doc.Value, curve.ToCurve(), (element as Types.Wall).SketchPlane.YAxis.ToXYZ() * height);
                analyticalPanel.StructuralRole = structuralRole;
                analyticalPanel.Thickness = thickness;
                return analyticalPanel;
              }
            );

            analyticalElements.Add(panel);
          }
          else if (boundary is object)
          {
            foreach (var face in boundary.Faces)
            {
              var panel = ReconstructElement<ARDB_AnalyticalPanel>
              (
                doc.Value, _AnalyticalElement_, analyticalPanel =>
                {
                  analyticalPanel = Reconstruct(analyticalPanel, doc.Value, face);
                  analyticalPanel.StructuralRole = structuralRole;
                  analyticalPanel.Thickness = thickness;
                  return analyticalPanel;
                }
              );

              analyticalElements.Add(panel);
            }
          }
          break;
      }

      DA.SetDataList(_AnalyticalElement_, analyticalElements);
#endif
    }
  }
}
