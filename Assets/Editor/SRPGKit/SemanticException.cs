using System;

public class SemanticException : Exception
{
  public SemanticException(string error) : base(error) { }
  public override string ToString()
  {
    return "Semantic error: " + Message;
  }
}