using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB
{
  using Extensions;

#if REVIT_2024
  using IntId = Int64;
#else
  using IntId = Int32;
#endif

  internal static class NumHexDigits
  {
    public const int IntId = sizeof(IntId) * 2;
    public const int Guid = 4 + (sizeof(Int32) * 8);

    public const int DocumentId = Guid;
    public const int EpisodeId = Guid;
    public const int ElementId = IntId;

    public const int UniqueId = EpisodeId + 1 + ElementId;
  }

  static class RuntimeId
  {
    public static string Format(IntId id) => id.ToString("D", CultureInfo.InvariantCulture);
    public static IntId Parse(string s) => IntId.Parse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
    public static bool TryParse(string s, out IntId id) => IntId.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out id);
  }

  public static class UniqueId
  {
    public static string Format(Guid episodeId, IntId id) => $"{episodeId:D}-{id.ToString($"x{NumHexDigits.ElementId}", CultureInfo.InvariantCulture)}";
    public static bool TryParse(string s, out Guid episodeId, out IntId id)
    {
      episodeId = Guid.Empty;
      id = -1;
      if (NumHexDigits.EpisodeId + 1 > s.Length || s.Length > NumHexDigits.EpisodeId + 1 + NumHexDigits.ElementId)
        return false;

      return Guid.TryParseExact(s.Substring(0, NumHexDigits.EpisodeId), "D", out episodeId) &&
             s[NumHexDigits.EpisodeId] == '-' &&
             IntId.TryParse(s.Substring(NumHexDigits.EpisodeId + 1, s.Length - (NumHexDigits.EpisodeId + 1)), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out id);
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
    public int[] Index;
    public GeometryObjectType Type;
    public IntId TypeId; // May be 0 or 1 for LINEAR or an ElementType Id for RVTLINK

    public override int GetHashCode()
    {
      int hashCode = 942107248;
      hashCode = hashCode * -1521134295 + Id.GetHashCode();
      for (int i = 0; i < (Index?.Length ?? 0); ++ i)
        hashCode = hashCode * -1521134295 + Index[i].GetHashCode();
      hashCode = hashCode * -1521134295 + Type.GetHashCode();
      hashCode = hashCode * -1521134295 + TypeId.GetHashCode();
      return hashCode;
    }

    public override bool Equals(object obj) => obj is GeometryObjectId other && Equals(other);
    public bool Equals(GeometryObjectId other) => this == other;

    public static bool operator ==(GeometryObjectId left, GeometryObjectId right)
    {
      return left.Id == right.Id && left.Index.ItemsEqual(right.Index) && left.Type == right.Type && left.TypeId == right.TypeId;
    }
    public static bool operator !=(GeometryObjectId left, GeometryObjectId right)
    {
      return left.Id != right.Id || !left.Index.ItemsEqual(right.Index) || left.Type != right.Type || left.TypeId != right.TypeId;
    }

    public GeometryObjectId(IntId id, int[] index = default, GeometryObjectType type = GeometryObjectType.ELEMENT, IntId parameter = -1)
    {
      Id = id;
      Index = index;
      Type = type;
      TypeId = parameter;
    }

    public override string ToString() => ToString(null);
    internal string ToString(ARDB.Document document)
    {
      string FormatId(IntId id)
      {
        return document is null ? RuntimeId.Format(id) :
          id < 0 ?
          UniqueId.Format(ARDB.ExportUtils.GetGBXMLDocumentId(document), id) :
          document.GetElement(new ARDB.ElementId(id)).UniqueId;
      }

      if (Id == 0) return string.Empty;

      var builder = new StringBuilder(FormatId(Id), 128);

      if (Index is object)
      {
        for (int i = 0; i < Index.Length; i++)
        {
          builder.Append(':');
          builder.Append(Index[i].ToString("D", CultureInfo.InvariantCulture));
        }
      }

      if (Type != GeometryObjectType.ELEMENT)
      {
        builder.Append(':');
        builder.Append(Type.ToString());

        if (TypeId >= 0)
        {
          builder.Append('/');
          if(Type == GeometryObjectType.RVTLINK)
            builder.Append(FormatId(TypeId));
          else
            builder.Append(RuntimeId.Format(TypeId));
        }
      }

      return builder.ToString();
    }
  }

  readonly struct ReferenceId
  {
    public readonly GeometryObjectId Record;  // Record in the root document.        (RevitLinkInstance when linked).
    public readonly GeometryObjectId Element; // Element.                            (Linked element when the Record is a RevitLinkInstance).
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
          else if(ids[1].Type != GeometryObjectType.RVTLINK)
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
      {
        var linkedDocument = (document?.GetElement(new ARDB.ElementId(Record.Id)) as ARDB.RevitLinkInstance)?.GetLinkDocument();
        return IsInstance ?
          $"{Record.ToString(document)}:{Element.ToString(linkedDocument)}:{Symbol.ToString(linkedDocument)}" :
          $"{Record.ToString(document)}:{Element.ToString(linkedDocument)}";
      }
      else return IsInstance ?
          $"{Element.ToString(document)}:{Symbol.ToString(document)}" :
             Element.ToString(document);
    }

    public string ToStableRepresentation(ARDB.Document document)
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
        var geometryId = new GeometryObjectId[3];

        var tokens = s.Split(':');
        int t, nesting;
        for (t = 0, nesting = 0; t < tokens.Length && nesting < geometryId.Length; ++nesting)
        {
          geometryId[nesting].Id = ParseId(tokens[t++]);

          // Index
          {
            var index = new List<int>();
            while (t < tokens.Length && Int32.TryParse(tokens[t], out var i))
            {
              index.Add(i);
              t++;
            }

            if (index.Count > 0)
              geometryId[nesting].Index = index.ToArray();
          }

          // Type
          if (t < tokens.Length)
          {
            var types = tokens[t++].Split('/');
            if (types.Length > 2)
              throw new FormatException($"{nameof(s)} is not in the correct format.");

            if (types[0] != string.Empty)
              geometryId[nesting].Type = (GeometryObjectType) Enum.Parse(typeof(GeometryObjectType), types[0]);

            if (types.Length == 2)
            {
              switch (geometryId[nesting].Type)
              {
                case GeometryObjectType.RVTLINK:
                  geometryId[nesting].TypeId = ParseId(types[1]);
                  document = (document.GetElement(new ARDB.ElementId(geometryId[0].Id)) as ARDB.RevitLinkInstance)?.GetLinkDocument();
                  break;

                default:
                  geometryId[nesting].TypeId = RuntimeId.Parse(types[1]);
                  break;
              }
            }
            else geometryId[nesting].TypeId = -1;
          }
        }

        if (t < tokens.Length)
          throw new FormatException($"{nameof(s)} is not in the correct format.");

        for (var n = nesting; n < geometryId.Length; ++n)
          geometryId[n] = geometryId[nesting - 1];

        if (geometryId[0].Type != GeometryObjectType.RVTLINK)
          geometryId[1] = geometryId[0];

        return new ReferenceId(geometryId);
      }
      catch (FormatException)
      {
        throw;
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
}
