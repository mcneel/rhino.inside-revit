using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Input
{
  public class ElementClasses : Grasshopper.Special.ValueSet<GH_String>
  {
    public override Guid ComponentGuid => new Guid("F432D672-FA9D-48B1-BABB-CFF8BEF38787");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;

    protected override System.Drawing.Bitmap Icon =>
      ((System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
      base.Icon;

    public ElementClasses() : base
    (
      name: "Element Classes",
      nickname: "Element Classes",
      description: "Provides a picker for Revit element classes",
      category: "Revit",
      subcategory: "Input"
    )
    {
      IconDisplayMode = GH_IconDisplayMode.name;
    }

    static readonly HashSet<Type> NonFilterableClasses = new HashSet<Type>
    {
      typeof(DB.AreaTagType),
      typeof(DB.AnnotationSymbol),
      typeof(DB.AnnotationSymbolType),
      typeof(DB.CombinableElement),
      typeof(DB.CurveByPoints),
      typeof(DB.DetailCurve),
      typeof(DB.DetailArc),
      typeof(DB.DetailEllipse),
      typeof(DB.DetailLine),
      typeof(DB.DetailNurbSpline),
      typeof(DB.ModelCurve),
      typeof(DB.ModelArc),
      typeof(DB.ModelEllipse),
      typeof(DB.ModelLine),
      typeof(DB.ModelNurbSpline),
      typeof(DB.ModelHermiteSpline),
      typeof(DB.SymbolicCurve),
      typeof(DB.DetailCurve),
      typeof(DB.Mullion),
      typeof(DB.Panel),
      typeof(DB.SlabEdge),

      typeof(DB.Architecture.RoomTagType),
      typeof(DB.Architecture.Fascia),
      typeof(DB.Architecture.Gutter),

      typeof(DB.Mechanical.SpaceTagType),

      typeof(DB.Structure.TrussType),
      typeof(DB.Structure.AreaReinforcementCurve),
    };

    static readonly HashSet<Type> SpecialClasses = new HashSet<Type>
    {
      typeof(DB.Area),
      typeof(DB.AreaTag),

      typeof(DB.Architecture.Room),
      typeof(DB.Architecture.RoomTag),

      typeof(DB.Mechanical.Space),
      typeof(DB.Mechanical.SpaceTag),
    };

    static readonly Type[] FilterableClasses = typeof(DB.Element).
      Assembly.ExportedTypes.
      Where
      (
        x =>
        {
          if (NonFilterableClasses.Contains(x)) return false;
          if (SpecialClasses.Contains(x)) return true;
          if (x.IsSubclassOf(typeof(DB.Element)))
          {
            try { using (new DB.ElementClassFilter(x)) return true; }
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
