using System;
using System.IO;
using System.Linq;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public interface IGH_ExternalGoo : IGH_Goo
  {
    /// <summary>
    /// SHA256 of Value content
    /// </summary>
    byte[] Identity { get; }

    /// <summary>
    /// External resource URI
    /// </summary>
    Uri ReferenceUri { get; set; }

    /// <summary>
    /// returns true if the external resource is resolved and loaded
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Resolve and load the external resource
    /// </summary>
    /// <returns></returns>
    bool Open();

    /// <summary>
    /// Discards external resource
    /// </summary>
    void Close();

    /// <summary>
    /// returns the external resolved resource
    /// </summary>
    object Target { get; }
  }

  public class Document : GH_Goo<DB.Document>, IEquatable<Document>
  {
    public override string TypeName => "Revit Documnent";
    public override string TypeDescription => "Represents a Revit document";
    public override bool IsValid => Value is object;
    public override sealed IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    public bool Equals(Document goo) => Value.Equals(goo.Value);
    public override bool Equals(object obj) => (obj is Document doc) ? Equals(doc) : base.Equals(obj);
    public override int GetHashCode() => Value.GetFingerprintGUID().GetHashCode();

    public Document() { }
    public Document(DB.Document value) : base(value) { }

    public override bool CastFrom(object source)
    {
      if (source is GH_String str)
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
      if (source is DB.Document doc)
      {
        Value = doc;
        return true;
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
}
