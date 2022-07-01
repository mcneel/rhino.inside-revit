using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ParameterElements
{
  using External.DB.Extensions;

  public class DefineParameter : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("134B7171-84E2-4900-B0C9-1D3E40F9B679");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public DefineParameter() : base
    (
      name: "Define Parameter",
      nickname: "Define Param",
      description: "Given its attributes, it creates a Parameter definition",
      category: "Revit",
      subCategory: "Parameter"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      //new ParamDefinition
      //(
      //  new Param_Guid()
      //  {
      //    Name = "Guid",
      //    NickName = "ID",
      //    Description = "Parameter global unique ID",
      //    Optional = true
      //  },
      //  ParamRelevance.Occasional
      //),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Parameter Name"
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.ParameterType>()
        {
          Name = "Type",
          NickName = "T",
          Description = "Parameter type",
        }.
        SetDefaultVale(External.DB.Schemas.SpecType.String.Text)
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.ParameterGroup>()
        {
          Name = "Group",
          NickName = "G",
          Description = "Parameter group",
        }.
        SetDefaultVale(External.DB.Schemas.ParameterGroup.Data)
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Description",
          NickName = "D",
          Description = "Tooltip Description",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.ParameterKey()
        {
          Name = _Definition_,
          NickName = "P",
          Description = $"Parameter {_Definition_}",
        }
      ),
    };

    const string _Definition_ = "Definition";
    const string UnrecommendedCharacters = "+-*/^='\"$%";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Params.TryGetData(DA, "Guid", out Guid? guid)) return;
      if (!Params.GetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return;
      if (!Params.GetData(DA, "Type", out Types.ParameterType type, x => x.IsValid)) return;
      if (!Params.GetData(DA, "Group", out Types.ParameterGroup group, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Description", out string description)) return;

      if (ElementNaming.IsValidName(name))
      {
#if REVIT_2023
        if (name.Any(UnrecommendedCharacters.Contains))
          AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Name '{name}' contains unrecommended characters\nUnrecomended characters are {string.Join(" ", UnrecommendedCharacters.ToCharArray())}");
#endif

        var definition = new Types.ParameterKey()
        {
          //GUID = guid,
          Nomen = name,
          DataType = type.Value,
          Group = group.Value,
          Description = description,
        };

        DA.SetData(_Definition_, definition);
      }
      else AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"'{name}' is not a valid name\nProhibited characters are {string.Join(" ", ElementNaming.InvalidCharacters.ToCharArray())}");
    }
  }
}
