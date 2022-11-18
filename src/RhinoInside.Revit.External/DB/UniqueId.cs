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
    public static string Format(Guid documentId, string stableId) => $"{documentId:D}:{stableId}";
    public static bool TryParse(string s, out Guid documentId, out string stableId)
    {
      documentId = Guid.Empty;
      stableId = string.Empty;
      if (s.Length < NumHexDigits.DocumentId + 1 + NumHexDigits.EpisodeId + 1 + NumHexDigits.ElementId)
        return false;

      if (Guid.TryParseExact(s.Substring(0, NumHexDigits.DocumentId), "D", out documentId) && s[NumHexDigits.DocumentId] == ':')
      {
        stableId = s.Substring(NumHexDigits.DocumentId + 1);
        return true;
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

    public override string ToString() => ToString(null);
    internal string ToString(ARDB.Document document)
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
    public readonly GeometryObjectId Record;  // Record in the root document.        (RevitLinkInstance when linked).
    public readonly GeometryObjectId Element; // Element.                            (Linked element when Reference is a RevitLinkInstance).
    public readonly GeometryObjectId Symbol;  // Element that contains the geometry. (Symbol when Element is an Instance)

    public bool IsLinked => Record.Type == GeometryObjectType.RVTLINK;
    public bool IsInstance => Element.Type == GeometryObjectType.INSTANCE;

    public bool IsElement => Element.Type == GeometryObjectType.ELEMENT && Element.Id != 0;
    public bool IsGeometry => Element.Type != GeometryObjectType.ELEMENT && Element.Id != 0;

    public ReferenceId(params GeometryObjectId[] ids)
    {
      switch (ids.Length)
      {
        case 0:
          {
            Record = Element = Symbol = default;
          }
          break;

        case 1:
          if (ids[0].Type != GeometryObjectType.RVTLINK)
          {
            Record = Element = Symbol = ids[0];
          }
          else throw new ArgumentException(nameof(ids));
          break;

        case 2:
          if (ids[0].Type == GeometryObjectType.RVTLINK)
          {
            Record = ids[0];
            Element = Symbol = ids[1];
          }
          else if(ids[0].Type == GeometryObjectType.INSTANCE)
          {
            Record = Element = ids[0];
            Symbol = ids[1];
          }
          else throw new ArgumentException(nameof(ids));
          break;

        case 3:
          if
          (
            (ids[0].Type != GeometryObjectType.RVTLINK  && ids[0] == ids[1] && ids[1] == ids[2]) ||
            (ids[0].Type == GeometryObjectType.INSTANCE && ids[0] == ids[1] && ids[1] != ids[2]) ||
            (ids[0].Type == GeometryObjectType.RVTLINK  && ids[1] == ids[2]) ||
            (ids[0].Type == GeometryObjectType.RVTLINK  && ids[1].Type == GeometryObjectType.INSTANCE && ids[1] != ids[2])
          )
          {
            Record  = ids[0];
            Element = ids[1];
            Symbol  = ids[2];
          }
          else throw new ArgumentException(nameof(ids));
          break;

        default:
          throw new ArgumentException(nameof(ids));
      }
    }

    public override string ToString() => ToString(null);
    public string ToString(ARDB.Document document)
    {
      if (IsLinked)
        return IsInstance ? $"{Record.ToString(document)}:{Element}:{Symbol}" : $"{Record.ToString(document)}:{Element}";
      else
        return IsInstance ? $"{Element.ToString(document)}:{Symbol.ToString(document)}" : Element.ToString(document);
    }

    #region IParsable
    public static ReferenceId Parse(string s, ARDB.Document document)
    {
      IntId ParseId(string uniqueId)
      {
        if (document is object && UniqueId.TryParse(uniqueId, out var episode, out var id))
        {
          if (id > 0)
            return ARDB.Reference.ParseFromStableRepresentation(document, uniqueId).ElementId.ToValue();

          if (episode != ARDB.ExportUtils.GetGBXMLDocumentId(document))
            throw new FormatException($"{nameof(s)} is not in the correct format.");

          return id;
        }

        return RuntimeId.Parse(uniqueId);
      }

      try
      {
        var tokens = s.Split(':');
        if (0 > tokens.Length || tokens.Length > 9)
          throw new FormatException($"{nameof(s)} is not in the correct format.");

        int nesting = 0;
        var geometryId = new GeometryObjectId[3];
        for (int i = 0; i < tokens.Length && nesting < geometryId.Length; ++i)
        {
          geometryId[nesting] = default;
          geometryId[nesting].Id = ParseId(tokens[i]);

          if (++i < tokens.Length)
          {
            if (Int32.TryParse(tokens[i], out geometryId[nesting].Index))
              i++;

            if (i < tokens.Length)
            {
              var types = tokens[i].Split('/');
              geometryId[nesting].Type = (GeometryObjectType) Enum.Parse(typeof(GeometryObjectType), types[0]);

              if (types.Length > 1)
                geometryId[nesting].Parameters = types.Skip(1).Select(ParseId).ToArray();
            }
          }

          if (geometryId[nesting].Type == GeometryObjectType.RVTLINK)
            document = default;

          for (var n = nesting; n < geometryId.Length; ++n)
            geometryId[n] = geometryId[nesting];
        
          nesting++;
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
    #endregion
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
