using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  [ComponentVersion(introduced: "1.8")]
  public class AddAngularDimension : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("0DBE67E7-7D8E-41F9-85B0-139C0B7F1745");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => string.Empty;

    public AddAngularDimension() : base
    (
      name: "Add Angular Dimension",
      nickname: "AngleDim",
      description: "Given an arc, it adds an angular dimension to the given View",
      category: "Revit",
      subCategory: "Annotation"
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
          Description = "View to add a specific dimension"
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "References",
          NickName = "R",
          Description = "References to create a specific dimension",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Param_Arc
        {
          Name = "Arc",
          NickName = "A",
          Description = "Arc to place a specific dimension",
        }
      ),
      new ParamDefinition
      (
        new Parameters.DimensionType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Element type of the given dimension",
          Optional = true
        }, ParamRelevance.Occasional
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Dimension()
        {
          Name = _Output_,
          NickName = _Output_.Substring(0, 1),
          Description = $"Output {_Output_}",
          Access = GH_ParamAccess.item
        }
      )
    };

    const string _Output_ = "Dimension";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out ARDB.View view)) return;

      ReconstructElement<ARDB.Dimension>
      (
        view.Document, _Output_, dimension =>
        {
          // Input
          if (!Params.GetDataList(DA, "References", out IList<ARDB.Element> elements)) return null;
          if (!Params.GetData(DA, "Arc", out Arc? arc)) return null;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out ARDB.DimensionType type, Types.Document.FromValue(view.Document), ARDB.ElementTypeGroup.AngularDimensionType)) return null;

          if
          (
            view.ViewType is ARDB.ViewType.Schedule ||
            view.ViewType is ARDB.ViewType.ColumnSchedule ||
            view.ViewType is ARDB.ViewType.PanelSchedule
          )
            throw new Exceptions.RuntimeArgumentException("View", "This view does not support detail items creation", view);

          // Compute
          dimension = Reconstruct(dimension, view, arc.Value.ToArc(), elements, type);

          DA.SetData(_Output_, dimension);
          return dimension;
        }
      );
    }

    static bool Contains(ARDB.ReferenceArray references, ARDB.ElementId value)
    {
      foreach (var reference in references.Cast<ARDB.Reference>())
        if (reference.ElementId == value)
          return true;

      return false;
    }

    bool Reuse(ARDB.Dimension dimension, ARDB.View view, ARDB.Arc arc, IList<ARDB.Element> elements, ARDB.DimensionType type)
    {
      if (dimension is null) return false;
      if (dimension.OwnerViewId != view.Id) return false;
      if (type != default && type.Id != dimension.GetTypeId()) return false;

      // Line
      if (!dimension.Curve.AlmostEquals(arc, GeometryTolerance.Internal.VertexTolerance)) return false;

      // Elements
      var currentReference = dimension.References;
      if (currentReference.Size != elements.Count) return false;

      foreach (var element in elements)
      {
        if (!Contains(currentReference, element.Id))
          return false;
      }

      return true;
    }

    ARDB.Dimension Create(ARDB.View view, ARDB.Arc arc, IList<ARDB.Element> elements, ARDB.DimensionType type)
    {
      var references = GetReferences(elements);
      return ARDB.AngularDimension.Create(view.Document, view, arc, references, type);
    }

    static IList<ARDB.Reference> GetReferences(IList<ARDB.Element> elements)
    {
      var referenceArray = new List<ARDB.Reference>(elements.Count);

      foreach (var element in elements)
      {
        if (element.GetDefaultReference() is ARDB.Reference reference)
          referenceArray.Append(reference);
      }

      return referenceArray;
    }

    ARDB.Dimension Reconstruct(ARDB.Dimension dimension, ARDB.View view, ARDB.Arc arc, IList<ARDB.Element> elements, ARDB.DimensionType type)
    {
      if (!Reuse(dimension, view, arc, elements, type))
        dimension = Create(view, arc, elements, type);

      return dimension;
    }
  }
}

