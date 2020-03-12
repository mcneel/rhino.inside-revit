using System;
using System.IO;
using System.Linq;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class Document : GH_Goo<DB.Document>, IEquatable<Document>
  {
    public override string TypeName => "Revit Documnent";
    public override string TypeDescription => "Represents a Revit document";
    public override bool IsValid => Value is object;
    public override sealed IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    public bool Equals(Document goo) => Value.Equals(goo.Value);
    public override bool Equals(object obj) => (obj is Document doc) ? Equals(doc) : base.Equals(obj);
    public override int GetHashCode() => Value.GetFingerprintGUID().GetHashCode();

    public static Document FromDocument(DB.Document document)
    {
      if (document is null)
        return null;

      if (document.IsFamilyDocument)
        return new FamilyDocument(document);

      return new ProjectDocument(document);
    }

    public Document() { }
    protected Document(DB.Document value) : base(value) { }

    public override bool CastFrom(object source)
    {
      if (source is DB.Document doc)
      {
        Value = doc;
        return true;
      }
      else if (source is Element element)
      {
        Value = element.Document;
        return true;
      }
      else if (source is GH_String str)
      {
        using (var Documents = Revit.ActiveDBApplication.Documents)
        {
          var docs = Documents.Cast<DB.Document>();

          var match_path = docs.Where(x => x.PathName.Equals(str.Value, StringComparison.InvariantCultureIgnoreCase)).ToArray();
          if (match_path.Length == 1)
          {
            Value = match_path[0];
            return true;
          }

          var match_file = docs.Where(x => Path.GetFileName(x.GetFilePath()).Equals(str.Value, StringComparison.InvariantCultureIgnoreCase)).ToArray();
          if (match_file.Length == 1)
          {
            Value = match_file[0];
            return true;
          }

          var match_title = docs.Where(x => x.Title.Equals(str.Value, StringComparison.InvariantCultureIgnoreCase)).ToArray();
          if (match_title.Length == 1)
          {
            Value = match_title[0];
            return true;
          }
        }

        return false;
      }

      return false;
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(DB.Document)))
      {
        target = (Q) (object) Value;
        return true;
      }

      return base.CastTo<Q>(ref target);
    }

    public override sealed string ToString()
    {
      var tip = IsValid ?
        $"{TypeName} : {DisplayName}" :
        (true/*IsReferencedElement*/ && Value is null /*!IsElementLoaded*/) ?
        $"Unresolved {TypeName} : {Value.GetFingerprintGUID()}" :
        $"Invalid {TypeName}";

      return tip;
    }

    public virtual string DisplayName => Value is null ? "<Null>" : Value.Title;
  }

  public class ProjectDocument : Document
  {
    public override string TypeName => "Revit Project Documnent";
    public override string TypeDescription => "Represents a Revit project document";

    public ProjectDocument(DB.Document value) : base(value) { }
  }

  public class FamilyDocument : Document
  {
    public override string TypeName => "Revit Family Documnent";
    public override string TypeDescription => "Represents a Revit family document";

    public FamilyDocument(DB.Document value) : base(value) { }
  }
}
