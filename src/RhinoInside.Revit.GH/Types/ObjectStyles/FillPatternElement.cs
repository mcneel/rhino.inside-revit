using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using ARDB = Autodesk.Revit.DB;
#if RHINO_8
using Grasshopper.Rhinoceros.Drafting;
using Grasshopper.Rhinoceros;
#endif

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Units;

  [Kernel.Attributes.Name("Fill Pattern")]
  public class FillPatternElement : Element, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(ARDB.FillPatternElement);
    public new ARDB.FillPatternElement Value => base.Value as ARDB.FillPatternElement;

    public FillPatternElement() { }
    public FillPatternElement(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public FillPatternElement(ARDB.FillPatternElement value) : base(value) { }

    public static new FillPatternElement FromElementId(ARDB.Document doc, ARDB.ElementId id)
    {
      return Element.FromElementId(doc, id) as FillPatternElement;
    }

    #region IGH_Goo
    public override bool CastTo<Q>(out Q target)
    {
#if RHINO_8
      if (typeof(Q).IsAssignableFrom(typeof(ModelHatchPattern)))
      {
        target = (Q) (object) ToModelContent(new Dictionary<ARDB.ElementId, ModelContent>());
        return true;
      }
#endif

      return base.CastTo(out target);
    }
    #endregion

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

      if (Value is ARDB.FillPatternElement fillPattern)
      {
        using (var pattern = fillPattern.GetFillPattern())
        {
          // 2. Check if already exist
          var hatchPattern = doc.HatchPatterns.FindName(fillPattern.Name) ??
                             new HatchPattern() { Name = fillPattern.Name };

          var index = hatchPattern.Index;

          // 3. Update if necessary
          if (index < 0 || overwrite)
          {
            if (pattern.IsSolidFill)
            {
              hatchPattern.FillType = HatchPatternFillType.Solid;
              hatchPattern.Name = "<Solid fill>";
            }
            else
            {
              hatchPattern.FillType = HatchPatternFillType.Lines;

#if RHINO_8
              var grids = pattern.GetFillGrids();
              for (int g = 0; g < grids.Count; ++g)
              {
                var grid = grids[g];
                var line = new HatchLine()
                {
                  BasePoint = new Point2d(UnitScale.Convert(grid.Origin.U, UnitScale.Internal, UnitScale.Millimeters), UnitScale.Convert(grid.Origin.V, UnitScale.Internal, UnitScale.Millimeters)),
                  Offset = new Vector2d(UnitScale.Convert(grid.Shift, UnitScale.Internal, UnitScale.Millimeters), UnitScale.Convert(grid.Offset, UnitScale.Internal, UnitScale.Millimeters)),
                  Angle = grid.Angle,
                };
                line.SetDashes(grid.GetSegments().Select((x, i) => UnitScale.Convert(i % 2 == 0 ? +x : -x, UnitScale.Internal, UnitScale.Millimeters)));

                hatchPattern.AddHatchLine(line);
              }
#else
              hatchPattern = pattern.GridCount == 2 ?
                HatchPattern.Defaults.Grid :
                HatchPattern.Defaults.Hatch1;

              hatchPattern.Index = -1;
              hatchPattern.Id = Guid.Empty;
              hatchPattern.Name = fillPattern.Name;
#endif
            }
          }

#if RHINO_8
          if (index < 0) { index = doc.HatchPatterns.Add(hatchPattern); hatchPattern = doc.HatchPatterns[index]; }
          else if (overwrite) { doc.HatchPatterns.Modify(hatchPattern, index, true); }
#else
          if (index < 0) { index = doc.HatchPatterns.Add(hatchPattern); hatchPattern = doc.HatchPatterns[index]; }
          else if (overwrite) { /*doc.HatchPatterns.Modify(hatchPattern, index, true);*/ }
#endif

          idMap.Add(Id, guid = hatchPattern.Id);
          return true;
        }
      }

      return false;
    }
    #endregion

    #region ModelContent
#if RHINO_8
    internal ModelContent ToModelContent(IDictionary<ARDB.ElementId, ModelContent> idMap)
    {
      if (idMap.TryGetValue(Id, out var modelContent))
        return modelContent;

      if (Value is ARDB.FillPatternElement fillPatter)
      {
        var attributes = new ModelHatchPattern.Attributes() { Path = fillPatter.Name };
        using (var pattern = fillPatter.GetFillPattern())
        {
          if (pattern.IsSolidFill)
          {
            attributes.Name = "<Solid fill>";
          }
          else
          {
            var grids = pattern.GetFillGrids();
            var lines = new ModelHatchLine[grids.Count];
            for(int g = 0; g < grids.Count; ++g)
            {
              var grid = grids[g];
              lines[g] = new ModelHatchLine.Attributes
              {
                Base = new Point2d(UnitScale.Convert(grid.Origin.U, UnitScale.Internal, UnitScale.Millimeters), UnitScale.Convert(grid.Origin.V, UnitScale.Internal, UnitScale.Millimeters)),
                Offset = new Vector2d(UnitScale.Convert(grid.Shift, UnitScale.Internal, UnitScale.Millimeters), UnitScale.Convert(grid.Offset, UnitScale.Internal, UnitScale.Millimeters)),
                Angle = grid.Angle,
                Segments = grid.GetSegments().Select((x, i) => UnitScale.Convert(i % 2 == 0 ? +x : -x, UnitScale.Internal, UnitScale.Millimeters)).ToArray()
              };
            }
            attributes.Lines = lines;
          }
        }

        idMap.Add(Id, modelContent = attributes.ToModelData() as ModelContent);
        return modelContent;
      }

      return null;
    }
#endif
    #endregion
  }
}
