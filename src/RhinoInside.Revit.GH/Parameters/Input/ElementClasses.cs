using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Input
{
  public class ElementClasses : Grasshopper.Special.ValueSet<GH_String>
  {
    public override Guid ComponentGuid => new Guid("F432D672-FA9D-48B1-BABB-CFF8BEF38787");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;

    protected override System.Drawing.Bitmap Icon =>
      ((System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
      base.Icon;

    public ElementClasses() : base
    (
      name: "Element Classes",
      nickname: "Element Classes",
      description: "Provides a picker for Revit element classes",
      category: "Revit",
      subcategory: "Filter"
    )
    {
      IconDisplayMode = GH_IconDisplayMode.name;
    }

    static readonly HashSet<Type> NonFilterableClasses = new HashSet<Type>
    {
      typeof(ARDB.CombinableElement),

      // ARDB.CurveElement
      typeof(ARDB.CurveByPoints),
      typeof(ARDB.DetailCurve),
      typeof(ARDB.DetailArc),
      typeof(ARDB.DetailEllipse),
      typeof(ARDB.DetailLine),
      typeof(ARDB.DetailNurbSpline),
      typeof(ARDB.ModelCurve),
      typeof(ARDB.ModelArc),
      typeof(ARDB.ModelEllipse),
      typeof(ARDB.ModelLine),
      typeof(ARDB.ModelNurbSpline),
      typeof(ARDB.ModelHermiteSpline),
      typeof(ARDB.SymbolicCurve),
      typeof(ARDB.Structure.AreaReinforcementCurve),

      // ARDB.FamilyInstance
      typeof(ARDB.Mullion),
      typeof(ARDB.Panel),
      typeof(ARDB.AnnotationSymbol),

      // ARDB.FamilySymbol
      typeof(ARDB.AreaTagType),
      typeof(ARDB.AnnotationSymbolType),
      typeof(ARDB.Architecture.RoomTagType),
      typeof(ARDB.Structure.TrussType),
      typeof(ARDB.Mechanical.SpaceTagType),

      // ARDB.HostedSweep
      typeof(ARDB.SlabEdge),
      typeof(ARDB.Architecture.Fascia),
      typeof(ARDB.Architecture.Gutter),
    };

    static readonly HashSet<Type> SpecialClasses = new HashSet<Type>
    {
      typeof(ARDB.Area),
      typeof(ARDB.AreaTag),

      typeof(ARDB.Architecture.Room),
      typeof(ARDB.Architecture.RoomTag),

      typeof(ARDB.Mechanical.Space),
      typeof(ARDB.Mechanical.SpaceTag),
    };

    static readonly Type[] FilterableClasses = typeof(ARDB.Element).
      Assembly.ExportedTypes.
      Where
      (
        x =>
        {
          if (NonFilterableClasses.Contains(x)) return false;
          if (SpecialClasses.Contains(x)) return true;
          if (x.IsSubclassOf(typeof(ARDB.Element)))
          {
            try { using (new ARDB.ElementClassFilter(x)) return true; }
            catch { }
          }
          return false;
        }
      ).
      OrderBy(x => x.FullName.Count(c => c == '.')).
      ThenBy(x => x.FullName).
      ToArray();

    protected override void LoadVolatileData()
    {
      if (SourceCount == 0)
      {
        m_data.Clear();
        m_data.AppendRange(FilterableClasses.Select(x => new GH_String(x.FullName)));
      }

      base.LoadVolatileData();
    }

    protected override void SortItems()
    {
      // FilterableClasses is already sorted alphabetically and
      // taking into account Namespace nesting level.
    }
  }

}
