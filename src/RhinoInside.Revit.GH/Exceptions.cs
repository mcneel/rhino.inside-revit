using System;

namespace RhinoInside.Revit.GH.Exceptions
{
  /// <summary>
  /// The exception that is thrown when a non-fatal runtime issue occurs.
  /// The current operation is canceled and is up to the catcher to rollback the entire transaction.
  /// </remarks>
  public class RuntimeException : Exception
  {
    public RuntimeException() : base(string.Empty) { }
    public RuntimeException(string message) : base(message) { }
    public RuntimeException(string message, Exception inner) : base(message, inner) { }
  }

  class RuntimeArgumentException : RuntimeException
  {
    public string ParamName { get; }
    public object Value { get; }

    public RuntimeArgumentException() : this(string.Empty, string.Empty) { }
    public RuntimeArgumentException(string paramName) : this(paramName, paramName) { }
    public RuntimeArgumentException(string paramName, string message) : base(message)
    {
      ParamName = paramName;
    }
    public RuntimeArgumentException(string paramName, string message, object value) : base(message)
    {
      ParamName = paramName;
      Value = value;
    }
  }

  class RuntimeArgumentNullException : RuntimeArgumentException
  {
    public RuntimeArgumentNullException() : base(string.Empty, string.Empty) { }
    public RuntimeArgumentNullException(string paramName) : base(paramName, string.Empty) { }
    public RuntimeArgumentNullException(string paramName, string message) : base(paramName, message) { }
  }

  /// <summary>
  /// The exception that is thrown when a non-fatal warning occurs.
  /// The current operation is canceled but what is already committed remains valid.
  /// </summary>
  /// <remarks>
  /// If it is catched inside a loop is safe to continue looping over the rest of elements.
  /// </remarks>
  public class RuntimeWarningException : Exception
  {
    public RuntimeWarningException() : base(string.Empty) { }
    public RuntimeWarningException(string message) : base(message) { }
    public RuntimeWarningException(string message, Exception inner) : base(message, inner) { }
  }

  /// <summary>
  /// The exception that is thrown when a fatal error occurs.
  /// The current operation is canceled and what is already committed is rolledback.
  /// </summary>
  /// <remarks>
  /// If it is catched inside a loop is NOT safe to continue looping over the rest of elements.
  /// </remarks>
  public class RuntimeErrorException : Exception
  {
    public RuntimeErrorException() : base(string.Empty) { }
    public RuntimeErrorException(string message) : base(message) { }
    public RuntimeErrorException(string message, Exception inner) : base(message, inner) { }
  }
}

