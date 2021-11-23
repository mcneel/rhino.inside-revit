using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.LinePatternElements
{
  using ElementTracking;
  using External.DB.Extensions;

  public class LinePatternByName : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("5C99445A-E908-4598-B6F4-F3DB4FB84CC1");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public LinePatternByName() : base
    (
      name: "Add Line Pattern",
      nickname: "Line Pattern",
      description: "Create a Revit line pattern by name",
      category: "Revit",
      subCategory: "Model"
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
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Line pattern Name",
          Optional = true
        }
      ),
      new ParamDefinition
      (
        new Parameters.LinePatternElement()
        {
          Name = "Template",
          NickName = "T",
          Description = "Template Line pattern",
          Optional = true
        },
        ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.LinePatternElement()
        {
          Name = _LinePattern_,
          NickName = _LinePattern_.Substring(0, 1),
          Description = $"Output {_LinePattern_}",
        }
      ),
    };

    const string _LinePattern_ = "Line Pattern";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties = { };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return;
      Params.TryGetData(DA, "Template", out ARDB.LinePatternElement template);

      // Previous Output
      Params.ReadTrackedElement(_LinePattern_, doc.Value, out ARDB.LinePatternElement pattern);

      StartTransaction(doc.Value);
      {
        pattern = Reconstruct(pattern, doc.Value, name, template);

        Params.WriteTrackedElement(_LinePattern_, doc.Value, pattern);
        DA.SetData(_LinePattern_, pattern);
      }
    }

    bool Reuse(ARDB.LinePatternElement pattern, string name, ARDB.LinePatternElement template)
    {
      if (pattern is null) return false;
      if (name is object) pattern.Name = name;
      if (template is object)
      {
        using (var oldDashes = pattern.GetLinePattern())
        using (var newDashes = template.GetLinePattern())
        {
          if (oldDashes.UpdateSegments(newDashes.GetSegments()))
            pattern.SetLinePattern(oldDashes);
        }
      }

      pattern.CopyParametersFrom(template, ExcludeUniqueProperties);
      return true;
    }

    ARDB.LinePatternElement Create(ARDB.Document doc, string name, ARDB.LinePatternElement template)
    {
      var pattern = default(ARDB.LinePatternElement);

      // Make sure the name is unique
      {
        if (name is null)
          name = template?.Name ?? _LinePattern_;

        name = doc.GetNamesakeElements
        (
          typeof(ARDB.LinePatternElement), name, categoryId: ARDB.BuiltInCategory.INVALID
        ).
        Select(x => x.Name).
        WhereNamePrefixedWith(name).
        NextNameOrDefault() ?? name;
      }

      // Try to duplicate template
      if (template is object)
      {
        {
          var ids = ARDB.ElementTransformUtils.CopyElements
          (
            template.Document,
            new ARDB.ElementId[] { template.Id },
            doc,
            default,
            default
          );

          pattern = ids.Select(x => doc.GetElement(x)).OfType<ARDB.LinePatternElement>().FirstOrDefault();
          pattern.Name = name;
        }
      }

      if (pattern is null)
      {
        using (var dashes = new ARDB.LinePattern(name))
        {
          dashes.SetSegments
          (
            new ARDB.LinePatternSegment[]
            {
              new ARDB.LinePatternSegment(ARDB.LinePatternSegmentType.Dash,  1.0 / 12.0 /* 1 inch */),
              new ARDB.LinePatternSegment(ARDB.LinePatternSegmentType.Space, 1.0 / 12.0 /* 1 inch */),
            }
          );

          pattern = ARDB.LinePatternElement.Create(doc, dashes);
        }
      }

      return pattern;
    }

    ARDB.LinePatternElement Reconstruct(ARDB.LinePatternElement pattern, ARDB.Document doc, string name, ARDB.LinePatternElement template)
    {
      if (!Reuse(pattern, name, template))
      {
        pattern = pattern.ReplaceElement
        (
          Create(doc, name, template),
          ExcludeUniqueProperties
        );
      }

      return pattern;
    }
  }
}
