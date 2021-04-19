using System;

#if !REVIT_2021
namespace Autodesk.Revit.DB
{
  internal class ForgeTypeId : IDisposable
  {
    public ForgeTypeId() { }
    public ForgeTypeId(string typeId)
    {
      if (typeId is null) throw new ArgumentNullException(nameof(typeId));

      TypeId = typeId;
    }
    ~ForgeTypeId() { }

    public bool IsValidObject => true;
    public string TypeId { get; set; } = string.Empty;

    public ForgeTypeId Clear()
    {
      TypeId = string.Empty;
      return this;
    }

    public void Dispose() { }
    public bool Empty() => TypeId == string.Empty;

    private string Name
    {
      get
      {
        var id = TypeId;

        var start = id.IndexOf(':') + 1;
        if (start == 0) return string.Empty;

        var end = id.IndexOf('-', start);
        if (end < 0) end = id.Length;

        return id.Substring(start, end - start);
      }
    }

    public override bool Equals(object other) => other is ForgeTypeId typeId && NameEquals(typeId);
    public override int GetHashCode() => Name.GetHashCode();

    public bool NameEquals(ForgeTypeId other) => Name == other.Name;
    public bool StrictlyEquals(ForgeTypeId other) => TypeId == other.TypeId;

    public static bool operator ==(ForgeTypeId lhs, ForgeTypeId rhs) =>  (ReferenceEquals(lhs, rhs) || lhs.NameEquals(rhs));
    public static bool operator !=(ForgeTypeId lhs, ForgeTypeId rhs) => !(ReferenceEquals(lhs, rhs) || lhs.NameEquals(rhs));
  }
}
#endif
