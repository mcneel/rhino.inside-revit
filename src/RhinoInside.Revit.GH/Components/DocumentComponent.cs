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

    public override void ClearData()
    {
      Message = string.Empty;

      base.ClearData();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Document Document = default;
      var _Document_ = Params.IndexOfInputParam("Document");
      if (_Document_ < 0)
      {
        Document = Revit.ActiveDBDocument;
        if (Document?.IsValidObject != true)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "There is no active Revit document");
          return;
        }

        // In case the user has more than one document open we show which one this component is working on
        if (Revit.ActiveDBApplication.Documents.Size > 1)
          Message = Document.Title.TripleDot(16);
      }
      else
      {
        DA.GetData(_Document_, ref Document);
        if (Document?.IsValidObject != true)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter Document failed to collect data");
          return;
        }
      }

      TrySolveInstance(DA, Document);
    }

    protected virtual void TrySolveInstance(IGH_DataAccess DA, DB.Document doc) { }
  }
}
