using System;
using System.Linq;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB
{
  using Extensions;
  using IntId = Int32;

  static class NumHexDigits
  {
    public const int IntId = sizeof(IntId) * 2;
    public const int Guid = 4 + (sizeof(Int32) * 8);

    public const int DocumentId = Guid;
    public const int EpisodeId = Guid;
    public const int ElementId = IntId;
  }

  static class RuntimeId
  {
    public static string Format(IntId id) => id.ToString("D", System.Globalization.CultureInfo.InvariantCulture);
    public static IntId Parse(string s) => IntId.Parse(s, System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture);
    public static bool TryParse(string s, out IntId id) => IntId.TryParse(s, System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out id);
  }

  public static class UniqueId
  {
    public static string Format(Guid episodeId, IntId id) => $"{episodeId:D}-{id,NumHexDigits.ElementId:x}";
    public static bool TryParse(string s, out Guid episodeId, out IntId id)
    {
      episodeId = Guid.Empty;
      id = -1;
      if (s.Length != NumHexDigits.EpisodeId + 1 + NumHexDigits.ElementId)
        return false;

      return Guid.TryParseExact(s.Substring(0, NumHexDigits.EpisodeId), "D", out episodeId) &&
             s[NumHexDigits.EpisodeId] == '-' &&
             IntId.TryParse(s.Substring(NumHexDigits.EpisodeId + 1, NumHexDigits.ElementId), System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.InvariantCulture, out id);
    }
  }

  public static class FullUniqueId
  {
    public static string Format(Guid documentId, string uniqueId) => $"{documentId:D}-{uniqueId}";
    public static bool TryParse(string s, out Guid documentId, out string uniqueId)
    {
      documentId = Guid.Empty;
      uniqueId = string.Empty;
      if (s.Length < NumHexDigits.DocumentId + 1 + NumHexDigits.EpisodeId + 1 + NumHexDigits.ElementId)
        return false;

      if (Guid.TryParseExact(s.Substring(0, NumHexDigits.DocumentId), "D", out documentId) && s[NumHexDigits.DocumentId] == '-')
      {
        uniqueId = s.Substring(NumHexDigits.DocumentId + 1);
        return UniqueId.TryParse(uniqueId, out var _, out var _);
      }

      return false;
    }
  }

  enum GeometryObjectType
  {
    ELEMENT = 0,
    LINEAR = 1,
    SURFACE = 2,
    RVTLINK = 3,
    INSTANCE = 4,
    CUT = 5,        // TODO : Test
    MESH = 6,       // TODO : Test
    SUBELEMENT = 7, // TODO : Test
  }

  struct GeometryObjectId : IEquatable<GeometryObjectId>
  {
    public IntId Id;
    public int Index;
    public GeometryObjectType Type;
    public IntId[] Parameters; // May be 0 or 1 for LINEAR or a type id for RVTLINK

    public override int GetHashCode()
    {
      int hashCode = 942107248;
      hashCode = hashCode * -1521134295 + Id.GetHashCode();
      hashCode = hashCode * -1521134295 + Index.GetHashCode();
      hashCode = hashCode * -1521134295 + Type.GetHashCode();
      return hashCode;
    }

    public override bool Equals(object obj) => obj is GlobalReferenceId other && Equals(other);
    public bool Equals(GeometryObjectId other) => this == other;

    public static bool operator ==(GeometryObjectId left, GeometryObjectId right)
    {
      return left.Id == right.Id && left.Index == right.Index && left.Type == right.Type;
    }
    public static bool operator !=(GeometryObjectId left, GeometryObjectId right)
    {
      return left.Id != right.Id || left.Index != right.Index || left.Type != right.Type;
    }

    public GeometryObjectId(IntId id, int index = 0, GeometryObjectType type = GeometryObjectType.ELEMENT, params IntId[] parameters)
    {
      Id = id;
      Index = index;
      Type = type;
      Parameters = parameters;
    }

    public GeometryObjectId(ARDB.ElementId id, int index = 0, GeometryObjectType type = GeometryObjectType.ELEMENT, params IntId[] parameters)
    {
      Id = id.ToValue();
      Index = index;
      Type = type;
      Parameters = parameters;
    }

    public override string ToString() => Format(null);
    internal string Format(ARDB.Document document)
    {
      string FormatId(IntId id)
      {
        return document is null ? id.ToString("D") :
          id < 0 ?
          UniqueId.Format(ARDB.ExportUtils.GetGBXMLDocumentId(document), id) :
          document.GetElement(new ARDB.ElementId(id)).UniqueId;
      }

      if (Id != 0)
      {
        var uniqueId = FormatId(Id);

        if (Type != GeometryObjectType.ELEMENT)
        {
          if (Parameters is object)
          {
            if(Type == GeometryObjectType.RVTLINK && Parameters?.Length > 0)
              return $"{uniqueId}:{Index:D}:{Type}/{string.Join("/", Parameters.Select(FormatId))}";
            else
              return $"{uniqueId}:{Index:D}:{Type}";
          }
          else
            return $"{uniqueId}:{Index:D}:{Type}";
        }

        if (Index != 0)
          return $"{uniqueId}:{Index:D}";

        return uniqueId;
      }

      return string.Empty;
    }
  }

  readonly struct ReferenceId
  {
    public readonly GeometryObjectId Root;    // Link instance when linked, equal to Element when not.
    public readonly GeometryObjectId Element; // Linked element when linked.
    public readonly GeometryObjectId Symbol;  // default when Element is not an INSTANCE

    public bool IsLinked => Root != Element;
    public bool IsInstance => Symbol != default;

    public bool IsElement => Element.Id != 0 && Element.Type == GeometryObjectType.ELEMENT;
    public bool IsGeometry => Element.Id != 0 && Element.Type != GeometryObjectType.ELEMENT;

    public ReferenceId(params GeometryObjectId[] ids)
    {
      switch (ids.Length)
      {
        case 0:
          Root = Element = Symbol = default;
          break;
        case 1:
          if (ids[0].Type == GeometryObjectType.ELEMENT)
          {
            Root = Element = ids[0];
            Symbol = default;
          }
          else throw new ArgumentException(nameof(ids));
          break;

        case 2:
          if (ids[0].Type == GeometryObjectType.RVTLINK)
          {
            Root = ids[0];
            Element = ids[1];
            Symbol = default;
          }
          else if(ids[0].Type == GeometryObjectType.INSTANCE)
          {
            Root = ids[0];
            Element = ids[0];
            Symbol = ids[1];
          }
          else throw new ArgumentException(nameof(ids));
          break;

        case 3:
          if (ids[0].Type == GeometryObjectType.RVTLINK)
          {
            Root = ids[0];
            Element = ids[1];
            Symbol = ids[2];
          }
          else if (ids[0].Type == GeometryObjectType.INSTANCE)
          {
            Root = ids[0];
            Element = ids[1];
            Symbol = ids[2];
          }
          else throw new ArgumentException(nameof(ids));
          break;

        default:
          throw new ArgumentException(nameof(ids));
      }
    }

    public override string ToString() => Format(null);
    public string Format(ARDB.Document document)
    {
      if (IsLinked)
        return IsInstance ? $"{Root.Format(document)}:{Element}:{Symbol}" : $"{Root.Format(document)}:{Element}";
      else
        return IsInstance ? $"{Element.Format(document)}:{Symbol.Format(document)}" : Element.Format(document);
    }

    public static ReferenceId Parse(string s, ARDB.Document document)
    {
      IntId ParseId(string uniqueId)
      {
        if (UniqueId.TryParse(uniqueId, out var _, out var id))
          return document is object && id > 0 ? ARDB.Reference.ParseFromStableRepresentation(document, uniqueId).ElementId.ToValue() : id;

        return RuntimeId.Parse(uniqueId);
      }

      try
      {
        var tokens = s.Split(':');
        int level = 0;
        var geometryId = new GeometryObjectId[3];
        for (int i = 0; i < tokens.Length && level < geometryId.Length; ++i)
        {
          geometryId[level].Id = ParseId(tokens[i]);

          if (++i >= tokens.Length) break;
          geometryId[level].Index = Int32.Parse(tokens[i]);

          if (++i >= tokens.Length) break;

          var types = tokens[i].Split('/');
          geometryId[level].Type = (GeometryObjectType) Enum.Parse(typeof(GeometryObjectType), types[0]);

          if (types.Length > 1)
            geometryId[level].Parameters = types.Skip(1).Select(ParseId).ToArray();

          if (level == 0)
          {
            if (geometryId[level].Type == GeometryObjectType.RVTLINK)
              document = default;
            else
              geometryId[++level] = geometryId[0];
          }

          level++;
        }

        return new ReferenceId(geometryId);
      }
      catch (Exception ex)
      {
        throw new FormatException($"{nameof(s)} is not in the correct format.", ex);
      }
    }

    public static bool TryParse(string s, out ReferenceId referenceId, ARDB.Document document)
    {
      try { referenceId = Parse(s, document); return true; }
      catch (FormatException) { }

      referenceId = default;
      return false;
    }
  }

  readonly struct GlobalReferenceId : IEquatable<GlobalReferenceId>
  {
    public readonly string Id;
    public GlobalReferenceId(string id) => Id = id;

    public override string ToString() => Id;
    public override int GetHashCode() => Id.GetHashCode();
    public override bool Equals(object obj) => obj is GlobalReferenceId other && Equals(other);
    public bool Equals(GlobalReferenceId other) => Id == other.Id;
  }
}
