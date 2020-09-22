using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public abstract class DocumentComponent : TransactionalComponent
  {
    protected DocumentComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected static readonly string DocumentParamName = "Document";
    public static IGH_Param CreateDocumentParam() => new Parameters.Document()
    {
      Name = DocumentParamName,
      NickName = "DOC",
      Description = "Document",
      Access = GH_ParamAccess.item
    };

    protected int DocumentParamIndex => Params.IndexOfInputParam(DocumentParamName);
    protected IGH_Param DocumentParam => DocumentParamIndex < 0 ? default : Params.Input[DocumentParamIndex];

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var Document))
        return;

      TrySolveInstance(DA, Document);
    }

    protected virtual void TrySolveInstance(IGH_DataAccess DA, DB.Document doc) { }
  }
}
