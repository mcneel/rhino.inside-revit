using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Fill Pattern")]
  public class FillPatternElement : Element, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(ARDB.FillPatternElement);
    public new ARDB.FillPatternElement Value => base.Value as ARDB.FillPatternElement;
    public static explicit operator ARDB.FillPatternElement(FillPatternElement value) => value?.Value;

    public FillPatternElement() { }
    public FillPatternElement(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public FillPatternElement(ARDB.FillPatternElement value) : base(value) { }

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
          if (pattern.IsSolidFill)
          {
            idMap.Add(Id, guid = HatchPattern.Defaults.Solid.Id);
            return true;
          }

          // 2. Check if already exist
          var hatchPattern = doc.HatchPatterns.FindName(fillPattern.Name) ??
                             new HatchPattern() { Name = fillPattern.Name };

          var index = hatchPattern.Index;

          // 3. Update if necessary
          if (index < 0 || overwrite)
          {
            // TODO: No API
            //hatchPattern.FillType = HatchPatternFillType.Lines;

            hatchPattern = pattern.GridCount == 2 ?
              HatchPattern.Defaults.Grid:
              HatchPattern.Defaults.Hatch1;
            hatchPattern.Name = fillPattern.Name;
          }

          if (index < 0) { index = doc.HatchPatterns.Add(hatchPattern); hatchPattern = doc.HatchPatterns[index]; }
          else if (overwrite) { /*doc.HatchPatterns.Modify(hatchPattern, index, true);*/ }

          idMap.Add(Id, guid = hatchPattern.Id);
          return true;
        }
      }

      return false;
    }
    #endregion

  }
}
