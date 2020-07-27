using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components
{
  public class ElementPropertyName : TransactionalComponent
  {
    public override Guid ComponentGuid => new Guid("01934AD1-F31B-43E5-ADD9-C196F4A2467E");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "N";

    public ElementPropertyName()
    : base
    (
      "Element Name",
      "ElemName",
      "Element Name Property. Get-Set accessor to Element Name property.",
      "Revit",
      "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access Name",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Element Name",
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
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access Name",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Element Name",
          Access = GH_ParamAccess.item
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var element = default(Types.Element);
      if (!DA.GetData("Element", ref element))
        return;

      var _Name_ = Params.IndexOfInputParam("Name");
      if (_Name_ < 0 || Params.Input[_Name_].DataType == GH_ParamData.@void)
      {
        DA.SetData("Element", element);
        DA.SetData("Name", element.Name);
      }
      else
      {
        var name = default(string);
        if (!DA.GetData(_Name_, ref name))
          return;

        var doc = element.Document;
        using (var transaction = NewTransaction(doc))
        {
          transaction.Start();
          element.Name = name;
          transaction.Commit();

          DA.SetData("Element", element);
          DA.SetData("Name", element.Name);
        }
      }
    }
  }

  public class ElementPropertyCategory : TransactionalComponent
  {
    public override Guid ComponentGuid => new Guid("5AC48DE6-F706-4E88-A4AD-7A4439F1DAB5");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "C";

    public ElementPropertyCategory()
    : base
    (
      "Element Category",
      "ElemCat",
      "Element Category Property. Get-Set accessor to Element Category property.",
      "Revit",
      "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access Type",
          Access = GH_ParamAccess.item
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access Type",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Parameters.Category()
        {
          Name = "Category",
          NickName = "C",
          Description = "Element Category",
          Access = GH_ParamAccess.item
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var element = default(Types.Element);
      if (!DA.GetData("Element", ref element))
        return;

      DA.SetData("Element", element);
      DA.SetData("Category", element.Category);
    }
  }

  public class ElementPropertyType : TransactionalComponent
  {
    public override Guid ComponentGuid => new Guid("FE427D04-1D8F-48BE-BFBA-EB28AD23FC03");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "T";

    public ElementPropertyType()
    : base
    (
      "Element Type",
      "ElemType",
      "Element Type Property. Get-Set accessor to Element Type property.",
      "Revit",
      "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access Type",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Element Type",
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
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access Type",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Element Type",
          Access = GH_ParamAccess.item
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var element = default(Types.Element);
      if (!DA.GetData("Element", ref element))
        return;

      var _Type_ = Params.IndexOfInputParam("Type");
      if (_Type_ < 0 || Params.Input[_Type_].DataType == GH_ParamData.@void)
      {
        DA.SetData("Element", element);
        DA.SetData("Type", element.Type);
      }
      else
      {
        var type = default(Types.ElementType);
        if (!DA.GetData(_Type_, ref type))
          return;

        var doc = element.Document;
        using (var transaction = NewTransaction(doc))
        {
          transaction.Start();
          element.Type = type;
          transaction.Commit();

          DA.SetData("Element", element);
          DA.SetData("Type", element.Type);
        }
      }
    }
  }
}
