using System;

namespace RhinoInside.Revit.External.DB
{
  using IntId = Int32;

  public static class UniqueId
  {
    const int sizeofGuid = 4 * sizeof(Int32); // sizeof(Guid)
    const int EpisodeIdDigits = 4 + (sizeofGuid * 2); 
    const int ElementIdDigits = sizeof(IntId) * 2;

    public static string Format(Guid episodeId, IntId id) => $"{episodeId:D}-{id,ElementIdDigits:x}";
    public static bool TryParse(string s, out Guid episodeId, out IntId id)
    {
      episodeId = Guid.Empty;
      id = -1;
      if (s.Length != EpisodeIdDigits + 1 + ElementIdDigits)
        return false;

      return Guid.TryParseExact(s.Substring(0, EpisodeIdDigits), "D", out episodeId) &&
             s[EpisodeIdDigits] == '-' &&
             IntId.TryParse(s.Substring(EpisodeIdDigits + 1, ElementIdDigits), System.Globalization.NumberStyles.AllowHexSpecifier, null, out id);
    }
  }

  public static class FullUniqueId
  {
    const int sizeofGuid = 4 * sizeof(Int32); // sizeof(Guid)
    const int DocumentIdDigits = 4 + (sizeofGuid * 2);
    const int EpisodeIdDigits = 4 + (sizeofGuid * 2);
    const int ElementIdDigits = sizeof(IntId) * 2;

    public static string Format(Guid documentId, string uniqueId) => $"{documentId:D}-{uniqueId}";
    public static bool TryParse(string s, out Guid documentId, out string uniqueId)
    {
      documentId = Guid.Empty;
      uniqueId = string.Empty;
      if (s.Length < DocumentIdDigits + 1 + EpisodeIdDigits + 1 + ElementIdDigits)
        return false;

      if (Guid.TryParseExact(s.Substring(0, DocumentIdDigits), "D", out documentId) && s[DocumentIdDigits] == '-')
      {
        uniqueId = s.Substring(DocumentIdDigits + 1);
        return true;
      }

      return false;
    }
  }
}
