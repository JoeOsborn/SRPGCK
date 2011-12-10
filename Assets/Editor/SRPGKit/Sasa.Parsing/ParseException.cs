using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Describes a parse error.
/// </summary>
public sealed class ParseException : Exception
{
  /// <summary>
  /// Constructs a new parse error.
  /// </summary>
  /// <param name="error">A description of the error.</param>
  public ParseException(string error) : base(error) { }
  /// <summary>
  /// Constructs a new error at the given line number and position of the input.
  /// </summary>
  /// <param name="line">The line number of the error.</param>
  /// <param name="column">The column position of the error.</param>
  /// <param name="error">A description of the error.</param>
  public ParseException(int line, int column, string error)
      : this(error)
  {
    Line = line;
    Column = column;
  }
  /// <summary>
  /// The line number of the error.
  /// </summary>
  public int Line { get; private set; }
  /// <summary>
  /// The position of the error on the line.
  /// </summary>
  public int Column { get; private set; }
  /// <summary>
  /// Returns a string representation describing the parse error.
  /// </summary>
  /// <returns>A string representation describing the parse error.</returns>
  public override string ToString()
  {
    return "Parse error: line " + Line + ": column " + Column + ": " + Message;
  }
}