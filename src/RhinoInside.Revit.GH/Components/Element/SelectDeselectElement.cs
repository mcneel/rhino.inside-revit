using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class SelectDeselectElement : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("3E44D6BB-5F49-40E8-A2C4-53E5E3A63DDC");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;
    protected override string IconTag => "SDE";

    public SelectDeselectElement() : base
    (
      name: "Select Element",
      nickname: "SelElems",
      description: "Adds or remove elements from active selection",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access selection state",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Selected",
          NickName = "S",
          Description = "New state for Element Pin",
          Access = GH_ParamAccess.item,
          Optional = true
        },
        ParamVisibility.Default
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access selection state",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Selected",
          NickName = "S",
          Description = "State for Element Pin",
          Access = GH_ParamAccess.item
        },
        ParamVisibility.Default
      ),
    };

    readonly Dictionary<DB.Document, HashSet<DB.ElementId>> Selection = new Dictionary<DB.Document, HashSet<DB.ElementId>>();
    protected override void BeforeSolveInstance()
    {
      base.BeforeSolveInstance();

      Revit.ActiveDBApplication.GetOpenDocuments(out var projects, out var families);

      foreach (var doc in projects.Concat(families))
      {
        var uiDoc = new Autodesk.Revit.UI.UIDocument(doc);
        Selection.Add(uiDoc.Document, new HashSet<DB.ElementId>(uiDoc.Selection.GetElementIds()));
      }
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var element = default(Types.Element);
      if (!DA.GetData("Element", ref element))
        return;

      DA.SetData("Element", element);

      if (Params.GetData(DA, "Selected", out bool? selected))
      {
        if (Selection.TryGetValue(element.Document, out var selection))
        {
          if (selected.Value) selection.Add(element.Id);
          else selection.Remove(element.Id);
        }
      }
      else
      {
        Params.TrySetData(DA, "Selected", () =>
        {
          if (Selection.TryGetValue(element.Document, out var selection))
            selected = selection.Contains(element.Id);

          return selected;
        });
      }
    }

    protected override void AfterSolveInstance()
    {
      try
      {
        var input = Params.Input<IGH_Param>("Selected");
        if (input is object)
        {
          var output = Params.Output<IGH_Param>("Selected");
          if (output is object)
            output.VolatileData.ClearData();

          // Make Selection effective
          foreach (var selection in Selection)
          {
            Rhinoceros.InvokeInHostContext(() =>
            {
              using (var uiDocument = new Autodesk.Revit.UI.UIDocument(selection.Key))
              {
                uiDocument.Selection.SetElementIds(selection.Value);
              }
            });
          }

          // Update Selected output
          if (output is object && Params.Output<IGH_Param>("Element") is IGH_Param element)
          {
            output.AddVolatileDataTree
            (
              element.VolatileData,
              (Types.Element x) => x is object && Selection.TryGetValue(x?.Document, out var sel) ? new GH_Boolean(sel.Contains(x.Id)) : null
            );
          }
        }
      }
      finally
      {
        Selection.Clear();
      }

      base.AfterSolveInstance();
    }
  }
}

namespace RhinoInside.Revit.GH.Components.Obsolete
{
  [Obsolete("Since 2021-03-29")]
  public class SelectDeselectElement : Component
  {
    public override Guid ComponentGuid => new Guid("d59d8b4b-a8ab-49c7-9036-209872aedd8b");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override string IconTag => "SDE";

    public SelectDeselectElement() : base
    (
      name: "Select Elements",
      nickname: "SelElems",
      description: "Adds or remove elements from active selection",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager[
        pManager.AddParameter(
          param: new Parameters.Element(),
          name: "Elements",
          nickname: "ES",
          description: "Elements to Query selection state, Select, or Deselect",
          access: GH_ParamAccess.list
          )
      ].Optional = true;

      pManager[
        pManager.AddBooleanParameter(
          name: "Select",
          nickname: "S",
          description: "Select or Deselect elements when input provided. Otherwise component will query selection state of input elements",
          access: GH_ParamAccess.item
          )
      ].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(
        param: new Parameters.Element(),
        name: "Elements",
        nickname: "E",
        description: "Elements to Select or Deselect",
        access: GH_ParamAccess.list
        );

      pManager.AddBooleanParameter(
        name: "Selected",
        nickname: "S",
        description: "Whether elements are included in active selection or not",
        access: GH_ParamAccess.item
        );
    }


    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      /* Note:
       * 1) No input to "Element" parameter will return the currently selection
       * elements in output. Output "Selected" parameter will be a list of "True"
       * 2) No input to "Select" parameter, will put the component in query mode,
       * and will check whether the input elements are currently selection or not
       * 3) "True" input to "Select" parameter, will make the component replace the
       * current selection with input elements. If user needs to select multiple
       * batches of element, they can merge them into one list and pass to this
       * component at the end. Output "Selected" parameter will be a a verification
       * of whether the element was selected or not
       * 4) "False" input to "Select" parameter, will make the component remove the 
       * input elements from the current selection and query the selection on the
       * output
      */
      if (Revit.ActiveUIDocument is Autodesk.Revit.UI.UIDocument uidoc)
      {
        var currentSelection = uidoc.Selection.GetElementIds();

        var elementIds = new List<DB.ElementId>();
        
        var _Elements_ = Params.IndexOfInputParam("Elements");
        var _Select_ = Params.IndexOfInputParam("Select");

        if (_Elements_ >= 0 && Params.Input[_Elements_].DataType != GH_ParamData.@void)
        {
          List<Types.Element> elements = new List<Types.Element>();
          if (DA.GetDataList("Elements", elements))
          {
            elementIds = elements.Select(e => e.Id).ToList();

            if (_Select_ >= 0 && Params.Input[_Select_].DataType != GH_ParamData.@void)
            {
              bool select = false;
              if (DA.GetData(_Select_, ref select))
              {
                if (select)
                {
                  currentSelection = elementIds;
                  uidoc.Selection.SetElementIds(elementIds);
                }
                else
                {
                  currentSelection = currentSelection.Where(eid => !elementIds.Contains(eid)).ToList();
                  uidoc.Selection.SetElementIds(currentSelection);
                }
              }
            }  
          }
        }
        else
          elementIds = uidoc.Selection.GetElementIds().ToList();

        DA.SetDataList("Elements", elementIds.Select(eid => uidoc.Document.GetElement(eid)));
        DA.SetDataList("Selected", elementIds.Select(eid => currentSelection.Contains(eid)));
      }
    }
  }
}
