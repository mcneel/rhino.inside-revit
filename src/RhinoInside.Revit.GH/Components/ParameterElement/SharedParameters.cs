using System;
using System.IO;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ParameterElement
{
  public class SharedParameters : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("7844B410-0C2D-46E3-9CDE-229E81438E38");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.SharedParameters);
      Menu_AppendItem
      (
        menu, $"Open Shared Parameters…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }
    #endregion

    public SharedParameters() : base
    (
      name: "Shared Parameters",
      nickname: "Shared",
      description: "Explore the content of the shared parameters file",
      category: "Revit",
      subCategory: "Parameter"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Param_FilePath()
        {
          Name = "File",
          NickName = "F",
          Description = "Shared parameters file path",
          FileFilter = "Shared parameters file(*.txt)|*.txt"
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_String
        {
          Name = "Group",
          NickName = "G",
          Description = "Group name",
          Optional = true
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String
        {
          Name = "Name",
          NickName = "N",
          Description = "Parameter name",
          Optional = true
        },
        ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Param_FilePath()
        {
          Name = "File",
          NickName = "F",
          Description = "Shared parameters file path"
        }
      ),
      new ParamDefinition
      (
        new Param_String
        {
          Name = "Group",
          NickName = "G",
          Description = "Parameter group",
          Access = GH_ParamAccess.tree
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.ParameterKey()
        {
          Name = "Definition",
          NickName = "D",
          Description = "Parameter Definition",
          Access = GH_ParamAccess.tree
        },
        ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (AddIn.Host.Services.Value is Autodesk.Revit.ApplicationServices.Application app)
      {
        // Input
        if (!Params.TryGetData(DA, "File", out string file, x => File.Exists(x))) return;
        if (!Params.TryGetData(DA, "Group", out string group, x => !string.IsNullOrEmpty(x))) return;
        if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return;

        var UserSharedParametersFilename = app.SharedParametersFilename;
        try
        {
          if (file is object)
            app.SharedParametersFilename = file;

          DA.SetData("File", app.SharedParametersFilename);

          using (var parametersFile = app.OpenSharedParameterFile())
          {
            if (parametersFile is object)
            {
              var _Group_ = Params.IndexOfOutputParam("Group");
              var _Definitions_ = Params.IndexOfOutputParam("Definition");
              if (_Group_ >= 0 || _Definitions_ >= 0)
              {
                var groupPath = DA.ParameterTargetPath(_Group_);
                var definitionsPath = DA.ParameterTargetPath(_Definitions_);

                var groups = new GH_Structure<GH_String>();
                var definitions = new GH_Structure<Types.ParameterKey>();

                int index = 0;
                foreach (var parameterGroup in parametersFile.Groups)
                {
                  if (group is object && !Operator.IsSymbolNameLike(parameterGroup.Name, group))
                    continue;

                  var gPath = groupPath.AppendElement(index);
                  groups.Append(new GH_String(parameterGroup.Name), gPath);

                  var dPath = definitionsPath.AppendElement(index);
                  definitions.EnsurePath(dPath);
                  foreach (var definition in parameterGroup.Definitions)
                  {
                    if (name is object && !Operator.IsSymbolNameLike(definition.Name, name))
                      continue;

                    definitions.Append(new Types.ParameterKey(definition as DB.ExternalDefinition), dPath);
                  }
                  index++;
                }

                if (_Group_ >= 0) DA.SetDataTree(_Group_, groups);
                if (_Definitions_ >= 0) DA.SetDataTree(_Definitions_, definitions);
              }
            }
          }
        }
        finally
        {
          app.SharedParametersFilename = UserSharedParametersFilename;
        }
      }
    }
  }
}
