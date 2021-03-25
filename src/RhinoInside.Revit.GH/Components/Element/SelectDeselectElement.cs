using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

using DBX = Autodesk.Revit.DB;
using UIX = Autodesk.Revit.UI;

namespace RhinoInside.Revit.GH.Components
{
  public class SelectDeselectElement : Component
  {
    public override Guid ComponentGuid => new Guid("d59d8b4b-a8ab-49c7-9036-209872aedd8b");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;
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
      if (Revit.ActiveUIDocument is UIX.UIDocument uidoc)
      {
        var currentSelection = uidoc.Selection.GetElementIds();

        var elementIds = new List<DBX.ElementId>();
        
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
