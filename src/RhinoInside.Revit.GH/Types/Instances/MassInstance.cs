using System;
using System.Collections.Generic;
using System.Linq;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Mass Component")]
  public sealed class MassInstance : FamilyInstance
  {
    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      if (!ARDB.Analysis.MassLevelData.IsMassFamilyInstance(element.Document, element.Id))
        return false;

      if (element is ARDB.FamilyInstance instance)
      {
        var symbol = instance.Symbol;
        if (symbol.Family.IsConceptualMassFamily)
          return true;
      }

      return false;
    }

    public MassInstance() { }
    public MassInstance(ARDB.FamilyInstance value) : base(value) { }

    #region Gross mass properties
    public double? GrossVolume
    {
      get
      {
        try { return ARDB.MassInstanceUtils.GetGrossVolume(Document, Id); } catch { return default; }
      }
    }
    public double? GrossSurfaceArea
    {
      get
      {
        try { return ARDB.MassInstanceUtils.GetGrossSurfaceArea(Document, Id); } catch { return default; }
      }
    }

    public double? GrossFloorArea
    {
      get
      {
        try { return ARDB.MassInstanceUtils.GetGrossFloorArea(Document, Id); } catch { return default; }
      }
    }
    #endregion

    public Level[] MassLevels
    {
      get
      {
        try { return ARDB.MassInstanceUtils.GetMassLevelIds(Document, Id).Select(x => GetElement<Level>(x)).ToArray(); }
        catch { return null; }
      }
    }
  }
}
