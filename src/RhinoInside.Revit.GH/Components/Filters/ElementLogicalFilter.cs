using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Filters
{
  public abstract class ElementLogicalFilter : Component, IGH_VariableParameterComponent
  {
    protected ElementLogicalFilter(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory)
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementFilter(), "Filter A", "A", string.Empty, GH_ParamAccess.item);
      manager.AddParameter(new Parameters.ElementFilter(), "Filter B", "B", string.Empty, GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementFilter(), "Filter", "F", string.Empty, GH_ParamAccess.item);
    }

    static int ToIndex(char value) => value - 'A';
    static char ToChar(int value) => (char) ('A' + value);

    public bool CanInsertParameter(GH_ParameterSide side, int index)
    {
      return side == GH_ParameterSide.Input && index <= ToIndex('Z') && index == Params.Input.Count;
    }

    public bool CanRemoveParameter(GH_ParameterSide side, int index)
    {
      return side == GH_ParameterSide.Input && index > ToIndex('B') && index == Params.Input.Count - 1;
    }

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      if (side == GH_ParameterSide.Output) return default;

      var name = $"Filter {ToChar(index)}";
      var nickName = ToChar(index).ToString();
      return new Parameters.ElementFilter()
      {
        Name = name,
        NickName = Grasshopper.CentralSettings.CanvasFullNames ? name : nickName
      };
    }

    public bool DestroyParameter(GH_ParameterSide side, int index) => CanRemoveParameter(side, index);
    public void VariableParameterMaintenance() { }

    public override void AddedToDocument(GH_Document document)
    {
      Grasshopper.CentralSettings.CanvasFullNamesChanged += CentralSettings_CanvasFullNamesChanged;
      base.AddedToDocument(document);
    }

    public override void RemovedFromDocument(GH_Document document)
    {
      Grasshopper.CentralSettings.CanvasFullNamesChanged -= CentralSettings_CanvasFullNamesChanged;
      base.RemovedFromDocument(document);
    }

    private void CentralSettings_CanvasFullNamesChanged()
    {
      for (int i = 0; i < Params.Input.Count; ++i)
      {
        var param = Params.Input[i];
        var name = $"Filter {ToChar(i)}";
        var nickName = ToChar(i).ToString();

        if (Grasshopper.CentralSettings.CanvasFullNames)
        {
          if (param.NickName == nickName)
            param.NickName = name;
        }
        else
        {
          if (param.NickName == name)
            param.NickName = nickName;
        }
      }
    }
  }

  public class ElementLogicalAndFilter : ElementLogicalFilter
  {
    public override Guid ComponentGuid => new Guid("0E534AFB-7264-4AFF-99F3-7F7EA7DB9F3D");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "∧";

    public ElementLogicalAndFilter()
    : base("Logical And Filter", "AndFltr", "Filter used to combine a set of filters that pass when all pass", "Revit", "Filter")
    { }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var filters = new List<DB.ElementFilter>(Params.Input.Count);
      for (int i = 0; i < Params.Input.Count; ++i)
      {
        DB.ElementFilter filter = default;
        if (DA.GetData(i, ref filter) && filter is object)
          filters.Add(filter);
      }

      DA.SetData("Filter", CompoundElementFilter.Intersect(filters));
    }
  }

  public class ElementLogicalOrFilter : ElementLogicalFilter
  {
    public override Guid ComponentGuid => new Guid("3804757F-3F4C-469D-8788-FCA26F477A9C");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "∨";

    public ElementLogicalOrFilter()
    : base("Logical Or Filter", "OrFltr", "Filter used to combine a set of filters that pass when any pass", "Revit", "Filter")
    { }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var filters = new List<DB.ElementFilter>(Params.Input.Count);
      for (int i = 0; i < Params.Input.Count; ++i)
      {
        DB.ElementFilter filter = default;
        if (DA.GetData(i, ref filter) && filter is object)
          filters.Add(filter);
      }

      DA.SetData("Filter", CompoundElementFilter.Union(filters));
    }
  }

  public class ElementExclusionFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("396F7E91-7F08-4A3D-9B9B-B6AA91AC0A2B");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "⊄";

    public ElementExclusionFilter()
    : base("Exclusion Filter", "Exclude", "Filter used to exclude a set of elements", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Elements", "E", "Elements to exclude", GH_ParamAccess.list);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var elementIds = new List<DB.ElementId>();
      if (!DA.GetDataList("Elements", elementIds))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      var ids = elementIds.Where(x => x is object).ToList();
      DA.SetData("Filter", CompoundElementFilter.ExclusionFilter(ids, inverted));
    }
  }
}
