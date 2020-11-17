using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class ProjectInformation : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("2F3AFCC9-C8A4-4423-B1BF-2A04E8FD2734");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "i";

    public ProjectInformation()
    : base
    (
      "Project Information",
      "Information",
      "Project information.",
      "Revit",
      "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Document>("Project", "P", relevance: ParamVisibility.Voluntary),

      ParamDefinition.Create<Param_String>("Organization Name", "ON", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Organization Description", "OD", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Building Name", "BN", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Author", "A", optional: true, relevance: ParamVisibility.Default),

      ParamDefinition.Create<Param_String>("Client Name", "CN", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Project Issue Date", "PID", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Project Status", "PS", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Project Address", "PA", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Project Name", "PN", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Project Number", "PNUM", optional: true, relevance: ParamVisibility.Default),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Element>("Project Information", "PI", relevance: ParamVisibility.Voluntary),

      ParamDefinition.Create<Param_String>("Organization Name", "ON", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Organization Description", "OD", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Building Name", "BN", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Author", "A", relevance: ParamVisibility.Default),

      ParamDefinition.Create<Param_String>("Client Name", "CN", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Project Issue Date", "PID", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Project Status", "PS", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Project Address", "PA", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Project Name", "PN", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Project Number", "PNUM", relevance: ParamVisibility.Default),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var info = default(DB.ProjectInfo);

      if (!Parameters.Document.GetDataOrDefault(this, DA, "Project", out var doc)) return;
      if (doc.IsFamilyDocument || (info = doc.ProjectInformation) == null)
      {
        info = doc.ProjectInformation;
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "'Project' is not a valid Project document");
        return;
      }

      bool update = false;
      update |= Params.GetData(DA, "Organization Name", out string organizationName);
      update |= Params.GetData(DA, "Organization Description", out string organizationDescription);
      update |= Params.GetData(DA, "Building Name", out string buildingName);
      update |= Params.GetData(DA, "Author", out string author);

      update |= Params.GetData(DA, "Client Name", out string clientName);
      update |= Params.GetData(DA, "Project Issue Date", out string issueDate);
      update |= Params.GetData(DA, "Project Status", out string status);
      update |= Params.GetData(DA, "Project Address", out string address);
      update |= Params.GetData(DA, "Project Name", out string name);
      update |= Params.GetData(DA, "Project Number", out string number);

      if (update)
      {
        StartTransaction(doc);
        if (organizationName != null) info.OrganizationName = organizationName;
        if (organizationDescription != null) info.OrganizationDescription = organizationDescription;
        if (buildingName != null) info.BuildingName = buildingName;
        if (author != null) info.Author = author;

        if (clientName != null) info.ClientName = clientName;
        if (issueDate != null) info.IssueDate = issueDate;
        if (status != null) info.Status = status;
        if (address != null) info.Address = address;
        if (name != null) info.Name = name;
        if (number != null) info.Number = number;
      }

      Params.TrySetData(DA, "Project Information", () => info);

      Params.TrySetData(DA, "Organization Name", () => info.OrganizationName);
      Params.TrySetData(DA, "Organization Description", () => info.OrganizationDescription);
      Params.TrySetData(DA, "Building Name", () => info.BuildingName);
      Params.TrySetData(DA, "Author", () => info.Author);

      Params.TrySetData(DA, "Client Name", () => info.ClientName);
      Params.TrySetData(DA, "Project Issue Date", () => info.IssueDate);
      Params.TrySetData(DA, "Project Status", () => info.Status);
      Params.TrySetData(DA, "Project Address", () => info.Address);
      Params.TrySetData(DA, "Project Name", () => info.Name);
      Params.TrySetData(DA, "Project Number", () => info.Number);
    }
  }
}
