using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Line Pattern")]
  public class LinePatternElement : Element, Bake.IGH_BakeAwareElement
  {
    #region IGH_Goo
    public override bool IsValid => (Id?.TryGetBuiltInLinePattern(out var _) == true) || base.IsValid;

    protected override Type ValueType => typeof(ARDB.LinePatternElement);

    public sealed override bool CastFrom(object source)
    {
      if (base.CastFrom(source))
        return true;

      var document = Revit.ActiveDBDocument;
      var patternId = ARDB.ElementId.InvalidElementId;

      if (source is ValueTuple<ARDB.Document, ARDB.ElementId> tuple)
      {
        (document, patternId) = tuple;
      }
      else if (source is IGH_Goo goo)
      {
        if (source is IGH_Element element)
        {
          document = element.Document;
          source = element.Id;
        }
        else source = goo.ScriptVariable();
      }

      switch (source)
      {
        case int integer: patternId = new ARDB.ElementId(integer); break;
        case ARDB.ElementId id: patternId = id; break;
      }

      if (patternId.TryGetBuiltInLinePattern(out var _))
      {
        SetValue(document, patternId);
        return true;
      }

      return false;
    }
    #endregion

    public LinePatternElement() { }
    public LinePatternElement(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public LinePatternElement(ARDB.LinePatternElement value) : base(value) { }

    public static new LinePatternElement FromElementId(ARDB.Document doc, ARDB.ElementId id)
    {
      if (id.IsLinePatternId(doc))
        return new LinePatternElement(doc, id);

      return null;
    }

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<ARDB.ElementId, Guid>(), true, doc, att, out guid);

    public bool BakeElement
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      if (Id == ARDB.LinePatternElement.GetSolidPatternId())
      {
        idMap.Add(Id, guid = new Guid("{3999bed5-78ee-4d73-a059-032224c6fd55}"));
        return true;
      }
      else if (Value is ARDB.LinePatternElement linePattern)
      {
        // 2. Check if already exist
        var index = doc.Linetypes.Find(linePattern.Name);
        var linetype = index < 0 ?
          new Linetype() { Name = linePattern.Name } :
          doc.Linetypes[index];

        // 3. Update if necessary
        if (index < 0 || overwrite)
        {
          var feet = Convert.Geometry.UnitConverter.ConvertFromInternalUnits(1.0, Rhino.UnitSystem.Millimeters);

          using (var pattern = linePattern.GetLinePattern())
          {
            linetype.SetSegments
            (
              pattern.GetSegments().Select
              (
                x =>
                {
                  switch (x.Type)
                  {
                    case ARDB.LinePatternSegmentType.Dash: return x.Length * +feet;
                    case ARDB.LinePatternSegmentType.Space: return x.Length * -feet;
                    case ARDB.LinePatternSegmentType.Dot: return 0.0;
                    default: throw new ArgumentOutOfRangeException();
                  }
                }
              )
            );
          }

          if (index < 0) { index = doc.Linetypes.Add(linetype); linetype = doc.Linetypes[index]; }
          else if (overwrite) doc.Linetypes.Modify(linetype, index, true);
        }

        idMap.Add(Id, guid = linetype.Id);
        return true;
      }

      return false;
    }
    #endregion

    #region Properties
    public override string Name
    {
      get
      {
        if (Id is object && Id.TryGetBuiltInLinePattern(out var builtInLinePattern))
          return $"{builtInLinePattern}";

        return base.Name;
      }
    }

    public IList<double> Dashes
    {
      get
      {
        if (Value is ARDB.LinePatternElement linePattern)
        {
          var factor = GeometryDecoder.ModelScaleFactor;

          using (var pattern = linePattern.GetLinePattern())
          {
            return pattern.GetSegments().Select
            (
              x =>
              {
                switch (x.Type)
                {
                  case ARDB.LinePatternSegmentType.Dash:  return x.Length * +factor;
                  case ARDB.LinePatternSegmentType.Space: return x.Length * -factor;
                  case ARDB.LinePatternSegmentType.Dot:   return 0.0;
                  default: throw new ArgumentOutOfRangeException();
                }
              }
            ).ToArray();
          }
        }

        return default;
      }
      set
      {
        if (Value is ARDB.LinePatternElement linePattern)
        {
          var factor = GeometryEncoder.ModelScaleFactor;

          using (var pattern = linePattern.GetLinePattern())
          {
            if
            (
              pattern.UpdateSegments
              (
                value.Select
                (
                  x =>
                  {
                    if (x  < 0.0) return new ARDB.LinePatternSegment(ARDB.LinePatternSegmentType.Space, -x * factor);
                    if (x  > 0.0) return new ARDB.LinePatternSegment(ARDB.LinePatternSegmentType.Dash,  +x * factor);
                    if (x == 0.0) return new ARDB.LinePatternSegment(ARDB.LinePatternSegmentType.Dot,   0.0);
                    throw new ArgumentOutOfRangeException();
                  }
                ).ToArray()
              )
            )
              linePattern.SetLinePattern(pattern);
          }
        }
      }
    }
    #endregion
  }
}
