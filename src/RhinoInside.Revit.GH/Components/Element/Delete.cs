using System;
using System.Linq;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components
{
  public class ElementDelete : ElementPurge
  {
    public override Guid ComponentGuid => new Guid("3FFC2CB2-48FF-4151-B5CB-511C964B487D");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;
    protected override string IconTag => "X";

    public ElementDelete() : base
    (
      name: "Delete Element",
      nickname: "Delete",
      description: "Deletes elements from Revit document",
      category: "Revit",
      subCategory: "Element"
    )
    {}

    protected override ComponentCommand Command => ComponentCommand.Delete;

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Elements",
          NickName = "E",
          Description = "Elements to Delete",
          Access = GH_ParamAccess.list
        },
        ParamVisibility.Binding
      ),
    };
  }
}

namespace RhinoInside.Revit.GH.Components.Obsolete
{
  [Obsolete("Obsolete since 2020-05-21")]
  public class ElementDelete : TransactionsComponent
  {
    public override Guid ComponentGuid => new Guid("213C1F14-A827-40E2-957E-BA079ECCE700");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.hidden;
    protected override string IconTag => "X";

    public ElementDelete()
    : base("Delete Element", "Delete", "Deletes elements from Revit document", "Revit", "Element")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Elements", "E", "Elements to delete", GH_ParamAccess.tree);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager) { }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!DA.GetDataTree<Types.Element>("Elements", out var elementsTree))
        return;

      var elementsToDelete = Parameters.Element.
                             ToElementIds(elementsTree).
                             GroupBy(x => x.Document).
                             ToArray();

      foreach (var group in elementsToDelete)
      {
        StartTransaction(group.Key);

        try
        {
          var deletedElements = group.Key.Delete(group.Select(x => x.Id).ToArray());

          if (deletedElements.Count == 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No elements were deleted");
          else
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{elementsToDelete.Length} elements and {deletedElements.Count - elementsToDelete.Length} dependant elements were deleted.");
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "One or more of the elements cannot be deleted.");
        }
      }
    }
  }
}
