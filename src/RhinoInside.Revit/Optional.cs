using System;
using System.Diagnostics;
using System.Reflection;

namespace RhinoInside
{
  public enum Optional { Missing };

  [DebuggerDisplay("{HasValue ? (object) Value : (object) \"{}\"}")]
  public struct Optional<T>
  {
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly T value;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public readonly bool IsMissing;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool IsNullOrMissing => IsMissing || !(value is object);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool HasValue => !IsMissing && (value is object);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public T Value
    {
      get
      {
        if (IsMissing)
          throw new InvalidOperationException("Optional object must have a value.");

        return value;
      }
    }

    public Optional(Optional<T> value)
    {
      this.value = value.value;
      IsMissing = value.IsMissing;
    }

    public Optional(T value)
    {
      this.value = value;
      IsMissing = false;
    }

    public Optional(Missing _)
    {
      this.value = default;
      IsMissing = true;
    }

    public Optional(Optional _)
    {
      this.value = default;
      IsMissing = true;
    }

    public Optional(object value)
    {
      if (value is Optional || value is Missing)
      {
        this.value = default;
        IsMissing = true;
      }
      else
      {
        this.value = (T)value;
        IsMissing = false;
      }
    }

    public T GetValueOrDefault() => value;
    public T GetValueOrDefault(T defaultValue) => IsMissing ? defaultValue : value;

    public override bool Equals(object other)
    {
      return (other is Optional<T> optional) ? this == optional :
             (other is Missing) ? IsMissing :
             false;
    }

    public override int GetHashCode() => IsMissing ? Missing.Value.GetHashCode() : value?.GetHashCode() ?? 0;
    public override string ToString() => IsMissing ? Missing.Value.ToString() : value?.ToString() ?? string.Empty;

    public static explicit operator T(Optional<T> value) => value.Value;
    public static bool operator ==(Optional<T> value, Optional<T> other) => value.IsMissing == other.IsMissing && Equals(value.value, other.value);
    public static bool operator !=(Optional<T> value, Optional<T> other) => value.IsMissing != other.IsMissing || !Equals(value.value, other.value);

    public static implicit operator Optional<T>(T value) => new Optional<T>(value);
    public static bool operator ==(Optional<T> value, T other) => value.IsMissing ? false : value == other;
    public static bool operator !=(Optional<T> value, T other) => value.IsMissing ? true : value != other;

    public static explicit operator Optional<T>(Missing _) => new Optional<T>();
    public static bool operator ==(Optional<T> value, Missing _) => value.IsMissing;
    public static bool operator !=(Optional<T> value, Missing _) => !value.IsMissing;

    public static implicit operator Optional<T>(Optional _) => new Optional<T>();
    public static bool operator ==(Optional<T> value, Optional _) => value.IsMissing;
    public static bool operator !=(Optional<T> value, Optional _) => !value.IsMissing;
  }
}
