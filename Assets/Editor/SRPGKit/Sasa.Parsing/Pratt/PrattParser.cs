using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// A semantic token encapsulating all the information needed to
/// parse a lexed input.
/// </summary>
/// <typeparam name="T">The type of value parsed by the token.</typeparam>
public sealed class Token<T>
{
  /// <summary>
  /// Create a semantic token.
  /// </summary>
  /// <param name="id">The token identifier; sometimes this is the symbol itself.</param>
  /// <param name="leftBindingPower">The left binding power, aka precedence.</param>
  /// <param name="line">The line number where the token occurred.</param>
  /// <param name="column">The column where the token occurred.</param>
  public Token(string id, int leftBindingPower, int line, int column)
  {
      this.Id = id;
      this.LeftBindingPower = leftBindingPower;
      this.Line = line;
      this.Column = column;
  }
  /// <summary>
  /// The token identifier.
  /// </summary>
  public string Id { get; internal set; }
  /// <summary>
  /// "Stickiness" to the left.
  /// </summary>
  public int LeftBindingPower { get; set; }
  /// <summary>
  /// Node has nothing to its left, ie. prefix. The null denotation
  /// is transparently memoized by <see cref="Lazy&lt;T&gt;"/>.
  /// </summary>
  public Func<PrattParser<T>, T> NullDenotation { get; set; }
  /// <summary>
  /// The line number of the input where the error occurred.
  /// </summary>
  public int Line { get; private set; }
  /// <summary>
  /// The column where the error occurred.
  /// </summary>
  public int Column { get; private set; }

  /// <summary>
  /// Execute the null denotation function of this token.
  /// </summary>
  /// <returns>The value for the null denotation of this token.</returns>
  public T Nud(PrattParser<T> parser)
  {
    if (NullDenotation == null) throw new ParseException(Line, Column, "Syntax error: '" + Id + "'.");
    return NullDenotation(parser);
  }

  /// <summary>
  /// Node has something to the left, ie. postfix or infix.
  /// </summary>
  public Func<PrattParser<T>, T, T> LeftDenotation { get; set; }

  /// <summary>
  /// Execute the left denotation function of this token.
  /// </summary>
  /// <param name="parser">The parser state.</param>
  /// <param name="left">The value to the left of this token.</param>
  /// <returns>The left denotation of this token.</returns>
  public T Led(PrattParser<T> parser, T left)
  {
    if (LeftDenotation == null) throw new ParseException(Line, Column, "Unknown symbol: '" + Id + "'.");
    return LeftDenotation(parser, left);
  }
  /// <summary>
  /// Returns a string representation of this token.
  /// </summary>
  /// <returns>A string representation of this token.</returns>
  public override string ToString()
  {
    return "<" + Id + ">";
    //return "<" + Id + ">:" + (NullDenotation != null ? Nud().ToString() : Id);
  }
}
/// <summary>
/// A function that scans <paramref name="input"/> starting from <paramref name="start"/>
/// for a specific pattern. The return value is the new parser position.
/// </summary>
/// <param name="input">The parser input string.</param>
/// <param name="start">The index to start searching.</param>
/// <returns>The index the match completed.</returns>
public delegate int Scanner(string input, int start);

/// <summary>
/// Inherit from this class to implement a typed grammar.
/// </summary>
/// <typeparam name="T">The type of values parsed from the input.</typeparam>
/// <remarks>
/// Implements a simple single-state Pratt-style parser with a longest
/// match precedence-based lexer. Clients need only inherit from this class,
/// specify the type of elements being parsed, and in the constructor function
/// specifying the set of parsable operators with the associated semantic action.
/// 
/// Pratt-parsers are effectively Turing complete, so they can parse any grammar imaginable,
/// although the predefined combinators encourage context-free grammars.
/// 
/// References:
/// <ul>
/// <li><a href="http://effbot.org/zone/simple-top-down-parsing.htm">http://effbot.org/zone/simple-top-down-parsing.htm</a></li>
/// <li><a href="http://javascript.crockford.com/tdop/tdop.html">http://javascript.crockford.com/tdop/tdop.html</a></li>
/// </ul>
/// </remarks>
/// <example>
/// Here is a simple calculator as an example:
/// <code>
/// class Calculator : Grammar&lt;int&gt;
/// {
///     public Calculator()
///     {
///         Infix("+", 10, Add);   Infix("-", 10, Sub);
///         Infix("*", 20, Mul);   Infix("/", 20, Div);
///         InfixR("^", 30, Pow);  Postfix("!", 30, Fact);
///         Prefix("-", 100, Neg); Prefix("+", 100, Pos);
///         Group("(", ")", int.MaxValue);
///         Match("(digit)", char.IsDigit, 1, Int);
///         SkipWhile(char.IsWhiteSpace);
///     }
/// 
///     int Int(string lit) { return int.Parse(lit); }
///     int Add(int lhs, int rhs) { return lhs + rhs; }
///     int Sub(int lhs, int rhs) { return lhs - rhs; }
///     int Mul(int lhs, int rhs) { return lhs * rhs; }
///     int Div(int lhs, int rhs) { return lhs / rhs; }
///     int Pow(int lhs, int rhs) { return (int)Math.Pow(lhs, rhs); }
///     int Neg(int arg) { return -arg; }
///     int Pos(int arg) { return arg; }
///     int Fact(int arg)
///     {
///         return arg == 0 || arg == 1 ? 1 : arg * Fact(arg - 1);
///     }
/// }
/// </code>
/// </example>
/// <exception cref="ParseException">Thrown if the parser encounters any errors, such as unknown symbols.</exception>
public abstract class Grammar<T>
{
  Dictionary<string, Symbol<T>> symbols = new Dictionary<string, Symbol<T>>();

  /// <summary>
  /// Generates a symbol.
  /// </summary>
  /// <param name="sym">The symbol identifier.</param>
  /// <returns>A symbol.</returns>
  protected Symbol<T> Symbol(string sym)
  {
      return Symbol(sym, 0);
  }
  /// <summary>
  /// Generates a symbol with the given left binding power.
  /// </summary>
  /// <param name="sym">The symbol identifier.</param>
  /// <param name="lbp">The left binding power.</param>
  /// <returns>A symbol.</returns>
  protected Symbol<T> Symbol(string sym, int lbp)
  {
      Symbol<T> r = symbols.TryGetValue(sym, out r)
                  ? r : symbols[sym] = new Symbol<T>(sym);
      r.LeftBindingPower = Math.Max(r.LeftBindingPower, lbp);
      return r;
  }
  /// <summary>
  /// A symbol that matches a character predicate.
  /// </summary>
  /// <param name="id">The identifier for this symbol.</param>
  /// <param name="pred">A predicate over characters identifying legitimate members.</param>
  /// <param name="bindingPower">Operator's binding power.</param>
  /// <param name="selector">Parsing function taking a string to a <typeparamref name="T"/>.</param>
  /// <returns>Literal symbol.</returns>
  protected Symbol<T> Match(string id, Predicate<char> pred, int bindingPower, Func<string, T> selector)
  {
      var lex = Symbol(id, bindingPower);
          lex.Scanner = While(pred);
          lex.Parse = selector;
      return lex;
  }
  /// <summary>
  /// Generate a prefix symbol.
  /// </summary>
  /// <param name="op">Prefix operator symbol.</param>
  /// <param name="bindingPower">Operator's binding power.</param>
  /// <param name="selector">Mapping function.</param>
  /// <returns>Prefix operator symbol.</returns>
  protected Symbol<T> Prefix(string op, int bindingPower, Func<T, T> selector)
  {
      // do not set binding power since no left denotation
      var lex = Symbol(op);
          lex.Nud = parser => selector(parser.Parse(bindingPower));
      return lex;
  }
  /// <summary>
  /// Generate a postfix operator symbol.
  /// </summary>
  /// <param name="op">The operator symbol.</param>
  /// <param name="bindingPower">The binding power of the operator.</param>
  /// <param name="selector">The function transforming the postfix token.</param>
  /// <returns>A postfix operator symbol.</returns>
  protected Symbol<T> Postfix(string op, int bindingPower, Func<T, T> selector)
  {
      var lex = Symbol(op, bindingPower);
          lex.Led = (parser, left) => selector(left);
      return lex;
  }
  /// <summary>
  /// Left-associative infix symbol.
  /// </summary>
  /// <param name="op">The operator symbol.</param>
  /// <param name="bindingPower">The binding power of the operator.</param>
  /// <param name="selector">The function transforming the infix token.</param>
  /// <returns>A right-associative operator symbol.</returns>
  protected Symbol<T> Infix(string op, int bindingPower, Func<T, T, T> selector)
  {
      var lex = Symbol(op, bindingPower);
          lex.Led = (parser, left) => selector(left, parser.Parse(bindingPower));
      return lex;
  }
  /// <summary>
  /// Right-associative infix symbol.
  /// </summary>
  /// <param name="op">The operator symbol.</param>
  /// <param name="bindingPower">The binding power of the operator.</param>
  /// <param name="selector">The function transforming the infix token.</param>
  /// <returns>A right-associative operator symbol.</returns>
  protected Symbol<T> InfixR(string op, int bindingPower, Func<T, T, T> selector)
  {
      var lex = Symbol(op, bindingPower);
          lex.Led = (parser, left) => selector(left, parser.Parse(bindingPower - 1));
      return lex;
  }
  /// <summary>
  /// Ternary operators, like "e ? e : e".
  /// </summary>
  /// <param name="infix0">The first infix symbol of the ternary operator.</param>
  /// <param name="infix1">The second infix symbol of the ternary operator.</param>
  /// <param name="bindingPower">The binding power of the operator.</param>
  /// <param name="parse">The parsing function for each branch.</param>
  /// <returns>A ternary operator symbol.</returns>
  protected Symbol<T> TernaryInfix(string infix0, string infix1, int bindingPower, Func<T, T, T, T> parse)
  {
      var tern = Symbol(infix0, bindingPower);
          tern.Led = (parser, left) =>
          {
              var first = parser.Parse(0);
                          parser.Advance(infix1);
              var second = parser.Parse(0);
              return parse(left, first, second);
          };
      Symbol(infix1);
      return tern;
  }
  /// <summary>
  /// Ternary operators, like "if e then e else e".
  /// </summary>
  /// <param name="bindingPower">The binding power of the operator.</param>
  /// <param name="prefix">The symbol starting the ternary operator.</param>
  /// <param name="infix0">The first infix symbol in the operator.</param>
  /// <param name="infix1">The second infix symbol in the operator.</param>
  /// <param name="parse">The parsing function.</param>
  /// <returns>A ternary operator symbol.</returns>
  protected Symbol<T> TernaryPrefix(string prefix, string infix0, string infix1, int bindingPower, Func<T, T, T, T> parse)
  {
      // even though this is a prefix op, it requires a binding power
      var tern = Symbol(prefix, bindingPower);
          tern.Nud = parser =>
          {
              var first =  parser.Parse(bindingPower);
                           parser.Advance(infix0);
              var second = parser.Parse(0);
                           parser.Advance(infix1);
              var third =  parser.Parse(0);
              return parse(first, second, third);
          };
      Symbol(infix0);
      Symbol(infix1);
      return tern;
  }
  /// <summary>
  /// A grouping operator, typically parenthesis of some sort.
  /// </summary>
  /// <param name="bindingPower">The binding power of the operator.</param>
  /// <param name="leftGrouping">The left grouping symbol.</param>
  /// <param name="rightGrouping">The right grouping symbol.</param>
  /// <returns>A grouping operator.</returns>
  protected Symbol<T> Group(string leftGrouping, string rightGrouping, int bindingPower)
  {
      var left = Symbol(leftGrouping, bindingPower);
          left.Nud = parser =>
          {
              var e = parser.Parse(0);
                      parser.Advance(rightGrouping);
              return e;
          };
      Symbol(rightGrouping);
      return left;
  }
  /// <summary>
  /// An operator that is delimited by opening and closing operators.
  /// </summary>
  /// <param name="bindingPower">The binding power of the operator.</param>
  /// <param name="open">The opening delimiter.</param>
  /// <param name="close">The closing delimiter.</param>
  /// <param name="parse">The parsing function.</param>
  /// <returns>A delimited operator.</returns>
  protected Symbol<T> Delimited(string open, string close, int bindingPower, Func<T, T> parse)
  {
      var left = Symbol(open, bindingPower);
          left.Nud = parser =>
          {
              var e = parser.Parse(0);
                      parser.Advance(close);
              return parse(e);
          };
      Symbol(close);
      return left;
  }
  /// <summary>
  /// An operator that is delimited by opening and closing operators.
  /// </summary>
  /// <param name="bindingPower">The binding power of the operator.</param>
  /// <param name="open">The opening delimiter.</param>
  /// <param name="close">The closing delimiter.</param>
  /// <param name="body">A delegated parsing function.</param>
  /// <returns>A delimited operator.</returns>
  protected Symbol<T> Delimited(string open, string close, int bindingPower, Func<PrattParser<T>, T> body)
  {
      var left = Symbol(open, bindingPower);
          left.Nud = parser =>
          {
              var x = body(parser);
              parser.Advance(close);
              return x;
          };
      Symbol(close);
      return left;
  }
  /// <summary>
  /// Process a list of tokens and return a single value.
  /// </summary>
  /// <param name="memberOf">A membership test to determine whether list processing should continue.</param>
  /// <param name="parse">A function that aggregates the list into single value.</param>
  /// <returns>Aggregates a list of values into a single value.</returns>
  protected Func<PrattParser<T>, T> List(Func<Token<T>, bool> memberOf, Func<IEnumerable<T>, T> parse)
  {
      return parser =>
      {
          var list = new List<T>();
          var tok = parser.Token;
          while (memberOf(tok))
          {
              list.Add(parser.Parse(0));
              tok = parser.Token;
          }
          return parse(list);
      };
  }
  /// <summary>
  /// Creates a symbol for characters that are to be skipped, whitespace for example.
  /// </summary>
  /// <param name="pred">The predicate determing the characters to skip.</param>
  /// <returns>A symbol for skipped characters.</returns>
  protected Symbol<T> SkipWhile(Predicate<char> pred)
  {
      return Skip(While(pred));
  }
  /// <summary>
  /// Creates a symbol for characters that are to be skipped, whitespace for example.
  /// </summary>
  /// <param name="scanner">The scanner identifying the characters to skip.</param>
  /// <returns>A symbol for skipped characters.</returns>
  protected Symbol<T> Skip(Scanner scanner)
  {
      var lex = Symbol("(skip)");
          lex.Scanner = scanner;
          lex.Skip = true;
      return lex;
  }
  /// <summary>
  /// Parse the given text and return the corresponding value, or throw a parse error.
  /// </summary>
  /// <param name="text">The text to parse.</param>
  /// <returns>The value parsed from the text.</returns>
  /// <exception cref="ParseException">Thrown when the text has an invalid structure.</exception>
  public T Parse(string text)
  {
      var p = new PrattParser<T>(text, symbols);
      try
      {
          var x = p.Parse(0);
          p.EnsureInputConsumed();
          return x;
      }
      catch (ParseException)
      {
          throw;
      }
      catch (Exception e)
      {
          p.Fail(text, e.Message);
          return default(T); // unreachable
      }
  }
  /// <summary>
  /// A scanner that matches any in a sequence of tokens.
  /// </summary>
  /// <param name="tokens">The list of tokens to match.</param>
  /// <returns>A Scanner that matches the given tokens.</returns>
  protected static Scanner Any(params string[] tokens)
  {
      return (input, start) =>
      {
          // try to match any of the given tokens
          foreach (var tok in tokens)
          {
              if (input.SliceEquals(start, tok))
              {
                  return start + tok.Length;
              }
          }
          return start;
      };
  }
  /// <summary>
  /// A scanner that matches a literal value.
  /// </summary>
  /// <param name="lit"></param>
  /// <returns></returns>
  protected static Scanner ScanLiteral(string lit)
  {
      return (input, start) =>
      {
          return input.SliceEquals(start, lit) ? start + lit.Length:
                                                 start;
      };
  }
  /// <summary>
  /// Returns a scanner forwards the stream while a predicate is satisfied.
  /// </summary>
  /// <param name="pred">Predicate on characters.</param>
  /// <returns>Scanner that uses the predicate to determine the end of a match.</returns>
  protected static Scanner While(Predicate<char> pred)
  {
      return (input, start) =>
      {
          int i = start;
          while (i < input.Length && pred(input[i])) ++i;
          return i;
      };
  }
  /// <summary>
  /// A scanner for whitespace.
  /// </summary>
  /// <param name="input">The input string to scan.</param>
  /// <param name="start">The index to start scanning.</param>
  /// <returns>The end of the scan.</returns>
  protected static int WhiteSpace(string input, int start)
  {
      int i = start;
      while (i < input.Length && char.IsWhiteSpace(input[i])) ++i;
      return i;
  }
  /// <summary>
  /// A scanner for digits.
  /// </summary>
  /// <param name="input">The input string to scan.</param>
  /// <param name="start">The index to start scanning.</param>
  /// <returns>The end of the scan.</returns>
  protected static int Digits(string input, int start)
  {
      int i = start;
      while (i < input.Length && char.IsDigit(input[i])) ++i;
      return i;
  }
  /// <summary>
  /// A scanner for letters.
  /// </summary>
  /// <param name="input">The input string to scan.</param>
  /// <param name="start">The index to start scanning.</param>
  /// <returns>The end of the scan.</returns>
  protected static int Letters(string input, int start)
  {
      int i = start;
      while (i < input.Length && char.IsLetter(input[i])) ++i;
      return i;
  }
  /// <summary>
  /// A scanner for letters or digits.
  /// </summary>
  /// <param name="input">The input string to scan.</param>
  /// <param name="start">The index to start scanning.</param>
  /// <returns>The end of the scan.</returns>
  protected static int LettersOrDigits(string input, int start)
  {
      int i = start;
      while (i < input.Length && char.IsLetterOrDigit(input[i])) ++i;
      return i;
  }
  /// <summary>
  /// Scanner for a particular character.
  /// </summary>
  /// <param name="c">The character to scan for.</param>
  /// <returns>The end of the scan.</returns>
  protected static Scanner Char(char c)
  {
      return (input, start) =>
      {
          int i = start;
          while (i < input.Length && c == input[i]) ++i;
          return i;
      };
  }
  /// <summary>
  /// Scan using a regular expression.
  /// </summary>
  /// <param name="regex">The regex pattern to match against.</param>
  /// <param name="options">The options to the regex.</param>
  /// <returns>A scanner using a regular expression for matching.</returns>
  protected static Scanner Regex(string regex, RegexOptions options = RegexOptions.None)
  {
      var r = new Regex(regex, options);
      return (input, start) =>
      {
          // use \G to ensure regex matches at exactly 'start' looking forward
          var match = r.Match(@"\G" + input, start);
          return match.Success ? start + match.Length : start;
      };
  }
}
/// <summary>
/// A parser for a given input text.
/// </summary>
/// <typeparam name="T">The type to be parsed from the text.</typeparam>
public sealed class PrattParser<T>
{
  /// <summary>
  /// The current token in the stream.
  /// </summary>
  public Token<T> Token { get; private set; }
  IEnumerator<Token<T>> stream;
  int line;
  int pos;
  Dictionary<string, Symbol<T>> symbols = new Dictionary<string, Symbol<T>>();

  internal PrattParser(string text, Dictionary<string, Symbol<T>> symbols)
  {
      this.symbols = symbols;
      this.stream = Tokenize(text);
      this.Token = Next();
  }

  Token<T> Next()
  {
      return stream != null && stream.MoveNext() ? stream.Current : End();
  }
  /// <summary>
  /// Parse the next token sequence given the right binding power.
  /// </summary>
  /// <param name="rbp">The right binding power to use when parsing.</param>
  /// <returns>The next parsed value.</returns>
  public T Parse(int rbp)
  {
      //FIXME: to make parser fully incremental with backtracking, the implicit
      //stack used when calling Nud and Led must be made explicit, and must
      //track precisely which state created the activation frame (NUD/LED).
      //The IEnumerator must also become a purely functional stream type.
      //Or make it return IEnumerator<IEnumerator<Token<T>>>
      //Then Save()/Restore() primitives can be added to capture a parser
      //continuation, consisting of: (stack, line, pos, Token, stream).

      //FIXME: use breadth-first parsing, ala parallel parsing processes, and
      //schedule them ordered by the number of errors they've generated so far.
      //This requires explicit parse error nodes, ie. Result<T> = Value<T> | Error<T>
      //http://lambda-the-ultimate.org/node/4109#comment-62411
      //This naturally handles ambiguous parses and unparseable programs, returning
      //a parse error describing the closest syntactically correct program. It
      //should be simple to make this incremental as well, for use in IDE's, etc.
      var t = Token;
      Token = Next();
/*			Debug.Log("calling nud on "+t);*/
      var left = t.Nud(this);
/*			Debug.Log("begin loop");*/
      while (rbp < Token.LeftBindingPower)
      {
          t = Token;
          Token = Next();
          left = t.Led(this, left);
      }
/*			Debug.Log("end loop");*/
      return left;
  }
  internal void EnsureInputConsumed()
  {
      if (stream.MoveNext()) throw new ParseException(Token.Line, Token.Column, "Invalid input.");
  }
  /// <summary>
  /// Checks that the current token matches the expected token specified by <paramref name="id"/>.
  /// </summary>
  /// <param name="id">The expected token identifier.</param>
  public void Advance(string id)
  {
      if (!id.IsNullOrEmpty() && Token.Id != id) throw new ParseException("Expected '" + id + "'");
      Token = Next();
  }
  /// <summary>
  /// The token delimiting the end of the token stream.
  /// </summary>
  /// <returns>The end token.</returns>
  Token<T> End()
  {
      return new Token<T>("(end)", int.MinValue, line, pos);
  }
  // count the number of instances of a character
  int count(string input, char c, int start, int end)
  {
      int lines;
      for (lines = 0; start < end; ++start)
      {
          if (input[start] == c) ++lines;
      }
      return lines;
  }
  /// <summary>
  /// Throws a formatted syntax failure exception.
  /// </summary>
  /// <param name="input">The input being parsed.</param>
  /// <param name="reason">The reason for the failure.</param>
  internal void Fail(string input, string reason)
  {
      throw new ParseException(line, Column(input, pos), reason);
  }
  /// <summary>
  /// Returns the column given the input and the current state of the parser.
  /// </summary>
  /// <param name="input">The input being parsed.</param>
  /// <param name="pos">The absolute position in the input used calculate the column.</param>
  /// <returns>The current column in the input.</returns>
  internal int Column(string input, int pos)
  {
      var lineStart = input.LastIndexOf('\n', Math.Min(pos, input.Length - 1));
      return lineStart < 0 ? pos + 1 : pos - lineStart;
  }

  // scan the input and generate a stream of matching tokens
  IEnumerator<Token<T>> Tokenize(string input)
  {
      //FIXME: parser is not thread-safe
      pos = 0; line = 1;
      while (pos < input.Length)
      {
          var k = pos;
          Symbol<T> match = null;
          foreach (var r in symbols.Values)
          {
              // check whether the given symbol matches at the current position
              int j = r.Scan(input, k);
              // save the longest match with the greatest binding power
              if (j > pos || match != null && j == pos && r.LeftBindingPower > match.LeftBindingPower)
              {
                  match = r;
                  pos = j;
              }
          }
          if (k == pos) Fail(input, "invalid symbol '" + input[pos] + "'.");
          else if (match != null)
          {
              if (!match.Skip)
              {
                  // return a token for the longest match
                  var tok = match.Matched(line, Column(input, k), input.Substring(k, pos - k));
                  yield return tok;
              }
              line += count(input, '\n', k, pos);
          }
      }
      yield return End();
  }
}
/// <summary>
/// A symbol definition.
/// </summary>
/// <typeparam name="T">The type of parser values.</typeparam>
public sealed class Symbol<T>
{
  /// <summary>
  /// Construct a new Symbol.
  /// </summary>
  /// <param name="id">The symbol identifier.</param>
  internal Symbol(string id) { Id = id; }
  /// <summary>
  /// The symbol identifier.
  /// </summary>
  public string Id { get; private set; }
  /// <summary>
  /// The scanner for this type of symbol.
  /// </summary>
  public Scanner Scanner { get; set; }
  /// <summary>
  /// The left binding power of the tokens generated from this symbol.
  /// </summary>
  public int LeftBindingPower { get; set; }
  /// <summary>
  /// The null denotation of the tokens generated from this symbol.
  /// </summary>
  public Func<PrattParser<T>, T> Nud { get; set; }
  /// <summary>
  /// The left denotation of the tokens generated from this symbol.
  /// </summary>
  public Func<PrattParser<T>, T, T> Led { get; set; }
  /// <summary>
  /// The function used to parse literals or identifiers, if applicable.
  /// </summary>
  public Func<string, T> Parse { get; set; }
  /// <summary>
  /// A flag indicating whether symbols of this type should be ignored
  /// when generating tokens.
  /// </summary>
  public bool Skip { get; set; }

  /// <summary>
  /// Check whether the current symbol matches the input string.
  /// </summary>
  /// <param name="input">The input string to scan.</param>
  /// <param name="pos">The position to start scanning.</param>
  /// <returns>The index at which the scan completed.</returns>
  public int Scan(string input, int pos)
  {
      return Scanner != null            ? Scanner(input, pos):
             input.SliceEquals(pos, Id) ? pos + Id.Length:
                                          pos;
  }

  /// <summary>
  /// Returns the token for this symbol that matched the given value.
  /// </summary>
  /// <param name="line">The line of the token.</param>
  /// <param name="column">The column of the token.</param>
  /// <param name="value">The value matched to this symbol.</param>
  /// <returns>The token value derived from this symbol.</returns>
  public Token<T> Matched(int line, int column, string value)
  {
      if (Skip) return null;
      return new Token<T>(value, LeftBindingPower, line, column)
      {
          NullDenotation = Nud ?? (Parse == null ? null : new Func<PrattParser<T>, T>(parser => Parse(value))),
          LeftDenotation = Led,
      };
  }
}


public static class Strings
{
 /// <summary>
 /// Returns true if string is null or empty.
 /// </summary>
 /// <param name="s">The string to test.</param>
 /// <returns>True if the string is null or of length 0.</returns>
 public static bool IsNullOrEmpty(this string s)
 {
   return string.IsNullOrEmpty(s);
 }
 /// <summary>
 /// Checks the value of a substring.
 /// </summary>
 /// <param name="first">The string to inspect.</param>
 /// <param name="start">The index at which to check for the substring.</param>
 /// <param name="sub">The string to use for comparison.</param>
 /// <returns>True if string <paramref name="sub"/> is found at <paramref name="first"/>[<paramref name="start"/>].</returns>
 public static bool SliceEquals(this string first, int start, string sub)
 {
   if (sub.Length > first.Length - start) return false;
   for (int i = start, j = 0; j < sub.Length; ++i, ++j)
   {
     if (first[i] != sub[j]) return false;
   }
   return true;
 }
}
