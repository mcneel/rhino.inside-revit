using System;

namespace RhinoInside
{
  /// The exception that is thrown when a non-fatal application error occurs.
  public class ApplicationException : Exception
  {
    public ApplicationException() { }
    public ApplicationException(string message) : base(message) { }
    public ApplicationException(string message, Exception inner) : base(message, inner)
    {
    }
  }
}
