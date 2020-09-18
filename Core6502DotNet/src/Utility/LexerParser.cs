//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Core6502DotNet
{
    /// <summary>
    /// A class that tokenizes string text.
    /// </summary>
    public static class LexerParser
    {
        #region constants

        const char EOF = char.MinValue;
        const char SingleQuote = '\'';
        const char NewLine = '\n';

        #endregion

        #region Members

        static public readonly Dictionary<string, string> Groups = new Dictionary<string, string>
        {
            ["("] = ")",
            ["["] = "]",
            ["{"] = "}"
        };

        static readonly HashSet<string> s_operators = new HashSet<string>
        {
            "|", "||", "&", "&&", "<<", ">>", "<", ">", ">=", "<=", "==", "!=", "(", ")",
            "[",  "]", "%",  "^", "^^", "`", "~", "!", "*", "-", "+", "/", ",", ":", "$"
        };

        static readonly HashSet<string> s_compoundOperators = new HashSet<string>
        {
            "||", "&&", "<<", ">>", ">=", "<=", "==", "!=", "^^",
        };

        static readonly HashSet<char> s_nonCompoundOperators = new HashSet<char>
        {
            '(', ')', '[', ']', '{', '}', '%', '`', '~', '*', '-', '+', '/', ',', ':', '$'
        };

        #endregion

        #region Methods

        static bool IsHex(char c)
            => char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

        static bool IsNotOperand(char c) => !char.IsLetterOrDigit(c) &&
                                            c != '.' &&
                                            c != '_' &&
                                            c != SingleQuote &&
                                            c != '"';

        static string ScanTo(char previousChar,
                             RandomAccessIterator<char> iterator,
                             Func<char, char, char, bool> terminal)
        {
            var tokenNameBuilder = new StringBuilder();
            var c = iterator.Current;
            tokenNameBuilder.Append(c);
            if (!terminal(previousChar, c, iterator.PeekNext()))
            {
                previousChar = c;
                c = iterator.GetNext();
                while (!terminal(previousChar, c, iterator.PeekNext()))
                {
                    tokenNameBuilder.Append(c);
                    if (char.IsWhiteSpace(c))
                        break;
                    previousChar = c;
                    c = iterator.GetNext();
                }
            }
            return tokenNameBuilder.ToString();
        }

        static bool FirstNonHex(char prev, char current, char next)
            => !IsHex(current);

        static bool FirstNonNonBase10(char prev, char current, char next)
        {
            if (char.IsDigit(current))
                return false;
            if (prev == '0' &&
                (current == 'b' || current == 'B' ||
                 current == 'o' || current == 'O') && char.IsDigit(next))
                return false;
            if (prev == '0' &&
                (current == 'x' || current == 'X') && IsHex(next))
                return false;
            if ((prev == 'x' || prev == 'X' || IsHex(prev)) && IsHex(current))
                return false;

            return true;
        }

        static bool FirstNonNumeric(char prev, char current, char next)
        {
            if (!char.IsDigit(current))
            {
                if (current == '.')
                {
                    if (char.IsDigit(prev) || char.IsDigit(next))
                        return false;
                }
                else if (current == '+' || current == '-')
                {
                    if ((prev == 'E' || prev == 'e') && char.IsDigit(next))
                        return false;
                }
                else if (current == 'E' || current == 'e')
                {
                    if (char.IsDigit(prev) &&
                         (next == '+' || next == '-' || char.IsDigit(next)))
                        return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        static bool FirstNonAltBin(char prev, char current, char next)
            => !(current == '.' || current == '#');

        static bool FirstNonSymbol(char prev, char current, char next) =>
            !char.IsLetterOrDigit(current) && current != '_' && current != '.' && current != SingleQuote;

        static bool FirstNonLetterOrDigit(char prev, char current, char next)
            => !char.IsLetterOrDigit(current);

        static bool FirstNonPlusMinus(char prev, char current, char next) 
            => (current != '-' && current != '+') || (prev != current && next != current);

        static bool FirstNonMatchingOperator(char prev, char current, char next)
        {
            if (!current.IsOperator() || s_nonCompoundOperators.Contains(current))
                return true;

            if (s_compoundOperators.Any(co => co[0] == current && co[1] == next))
                return false;

            return !s_compoundOperators.Any(co => co[0] == prev && co[1] == current);
        }

        static bool NonNewLineWhiteSpace(char c) => c == ' ' || c == '\t';

        static Token ParseToken(char previousChar, 
                                Token previousToken, 
                                RandomAccessIterator<char> iterator,
                                AssemblyServices services)
        {
            var unparsedSb = new StringBuilder();
            char c = iterator.Current;
            while (char.IsWhiteSpace(c))
            {
                if (c == NewLine)
                {
                    iterator.Rewind(iterator.Index - 1);
                    return null;
                }
                unparsedSb.Append(c);
                c = iterator.GetNext();
            }
            if (c == ';' || c == EOF)
                return null;

            var tokenType = TokenType.None;
            var operatorType = OperatorType.None;
            var source = string.Empty;
            var unparsedSource = string.Empty;

            //first case, simplest
            var nextChar = iterator.PeekNext();
            if (char.IsDigit(c) ||
                char.IsLetter(c) ||
                c == '_' ||
                c == '?' ||
                (c == '.' && char.IsLetterOrDigit(nextChar)) ||
                (c == '\\' && char.IsLetterOrDigit(nextChar)))
            {
                tokenType = TokenType.Operand;
                if (char.IsDigit(c) || (c == '.' && char.IsDigit(nextChar)))
                {
                    if (char.IsDigit(c) && previousChar == '$')
                    {
                        source = ScanTo(previousChar, iterator, FirstNonHex);
                    }
                    else if (c == '0' && (nextChar == 'b' ||
                                          nextChar == 'B' ||
                                          nextChar == 'o' ||
                                          nextChar == 'O' ||
                                          nextChar == 'x' ||
                                          nextChar == 'X'))
                    {
                        source = ScanTo(previousChar, iterator, FirstNonNonBase10);
                    }
                    else
                    {
                        source = ScanTo(previousChar, iterator, FirstNonNumeric);
                    }
                }
                else if (c == '\\') // for macro expansions
                {
                    iterator.MoveNext();
                    source = c + ScanTo(previousChar, iterator, FirstNonLetterOrDigit);
                }
                else if (c == '?') // for uninitialized operator
                {
                    source = "?";
                    unparsedSb.Append('?');
                    return new Token(source, unparsedSb.ToString(), TokenType.Operand);
                }
                else
                {
                    unparsedSource =
                    source = ScanTo(previousChar, iterator, FirstNonSymbol);
                    if (!services.Options.CaseSensitive)
                        source = source.ToLower();
                    if (services.InstructionLookupRules.Any(rule => rule(source)))
                    {
                        tokenType = TokenType.Instruction;
                    }
                    else if (iterator.Current == '(' ||
                        (iterator.Current != NewLine && char.IsWhiteSpace(iterator.Current) &&
                         iterator.PeekNextSkipping(NonNewLineWhiteSpace) == '('))
                    {
                        tokenType = TokenType.Operator;
                        operatorType = OperatorType.Function;
                    }
                    else
                    {
                        tokenType = TokenType.Operand;
                    }
                }
            }
            else if (previousToken != null &&
                     previousToken.Name.Equals("%") &&
                     previousToken.OperatorType == OperatorType.Unary &&
                     (c == '.' || c == '#'))
            {
                // alternative binary string parsing
                tokenType = TokenType.Operand;
                source = ScanTo(previousChar, iterator, FirstNonAltBin).Replace('.', '0')
                                                                       .Replace('#', '1');
            }
            else if (c == '"' || c == SingleQuote)
            {
                var open = c;
                var quoteBuilder = new StringBuilder(c.ToString());
                var escaped = false;
                while ((c = iterator.GetNext()) != open && c != char.MinValue)
                {
                    quoteBuilder.Append(c);
                    if (c == '\\')
                    {
                        escaped = true;
                        quoteBuilder.Append(iterator.GetNext());
                    }
                    else if (c == '\n')
                        throw new Exception("Newline reached before quote string is enclosed.");
                }
                if (c == char.MinValue)
                    throw new Exception("Quote string not enclosed.");
                quoteBuilder.Append(c);
                var unescaped = escaped ? Regex.Unescape(quoteBuilder.ToString()) : quoteBuilder.ToString();
                if (c == '\'' && unescaped.Length > 3)
                    throw new Exception("Too many characters in character literal.");
                source = unescaped;
                tokenType = TokenType.Operand;
            }
            else
            {
                if (c == '+' || c == '-')
                {
                    /*
                     Scenarios for parsing '+' or '-', since they can function as different things
                     in an expression.
                     1. The binary operator:
                        a. OPERAND+3 / ...)+(... => single '+' sandwiched between two operands/groupings
                        b. OPERAND++3 / ...)++(... => the first '+' is a binary operator since it is to the
                           right of an operand/grouping. We need to split off the single '++' to two 
                           separate '+' tokens. What kind of token is the second '+'? We worry about that later.
                        c. OPERAND+++3 / ...)+++(... => again, the first '+' is a binary operator. We need to split
                           it off from the rest of the string of '+' characters, and we worry about later.
                     2. The unary operator:
                        a. +3 / +(... => single '+' immediately preceding an operand/grouping.
                        b. ++3 / ++(... => parser doesn't accept C-style prefix (or postfix) operators, so one of these is an
                           anonymous label. Which one? Easy, the first. Split the '+' string.
                     3. A full expression mixing both:
                        a. OPERAND+++3 / ...)+++(... => From scenario 1.c, we know the first '+' is a binary operator,
                           which leaves us with => '++3' left, which from scenario 2.b. we know the first '+'
                           has to be an operand. So we split the string again, so that the next scan leaves us with
                           '+3', so the third and final plus is a unary operator.
                           * OPERAND => operand
                           * +       => binary operator
                           * +       => operand
                           * +       => unary operator
                           * 3/(     => operand/grouping
                      4. A line reference:
                         a. + => Simplest scenario.
                         b. ++, +++, ++++, etc. => Treat as one.
                     */
                    // Get the full string
                    source = ScanTo(previousChar, iterator, FirstNonPlusMinus);
                    if (source.Length > 1)
                        nextChar = iterator.Current;
                    if (previousToken != null && (previousToken.Type == TokenType.Operand || previousToken.Name.Equals(")")))
                    {
                        // looking backward at the previous token, if it's an operand or grouping then we 
                        // know this is a binary
                        tokenType = TokenType.Operator;
                        operatorType = OperatorType.Binary;
                        if (source.Length > 1)
                        {
                            iterator.Rewind(iterator.Index - source.Length);          
                            source = c.ToString();
                        }
                    }
                    else if (!IsNotOperand(nextChar) || 
                             nextChar == '(' || 
                             (nextChar.IsRadixOperator() && TokenAfterNextIsNonBase10()))
                    {
                        // looking at the very next character in the input stream, if it's an operand or grouping,
                        // or if it's a radix operator, then we know this is a unary
                        if (source.Length > 1)
                        {
                            // If the string is greater than one character,
                            // then it's not a unary, it's an operand AND a unary. So we split off the 
                            // rest of the string.
                            iterator.Rewind(iterator.Index - source.Length);
                            source = c.ToString();
                            tokenType = TokenType.Operand;
                        }
                        else
                        {
                            // we have a unary operator because the next item
                            // will be an operand/grouping
                            tokenType = TokenType.Operator;
                            operatorType = OperatorType.Unary;
                        }
                    }
                    else
                    {
                        tokenType = TokenType.Operand;
                    }
                }
                else if (c == '*')
                {
                    // Same as +/- scenario above, if the previous token is an operand or grouping,
                    // we need to treat the splat as a binary operator.
                    if (previousToken != null && (previousToken.Type == TokenType.Operand || previousToken.Name.Equals(")")))
                    {
                        tokenType = TokenType.Operator;
                        operatorType = OperatorType.Binary;
                    }
                    else
                    {
                        // but since there is no unary version of this we will treat as an operand, and let the evaluator
                        // deal with any problems like *OPERAND /*(
                        tokenType = TokenType.Operand;
                    }
                    source = c.ToString();
                }
                else
                {
                    // not a number, symbol, string, or special (+, -, *) character. So we just treat as an operator
                    tokenType = TokenType.Operator;
                    if (c.IsSeparator() || c.IsOpenOperator() || c.IsClosedOperator())
                    {
                        source = c.ToString();
                        if (c.IsSeparator()) 
                            operatorType = OperatorType.Separator;
                        else if (c.IsOpenOperator()) 
                            operatorType = OperatorType.Open;
                        else
                            operatorType = OperatorType.Closed;
                    }
                    else
                    {
                        unparsedSource =
                        source = ScanTo(previousChar, iterator, FirstNonMatchingOperator);
                        if (source.Length > 1)
                            nextChar = iterator.Current;
                        /* The general strategy to determine whether an operator is unary or binary:
                            1. Is it actually one of the defined unary types?
                            2. Peek at the next character. Is it a group or operand, or not?
                            3. Look behind at the previous token. Is it also a group or operand, or not?
                            4. If the token does NOT follow an operand or group, AND it precedes a group character,
                               or operand character, then it is a unary.
                            5. All other cases, binary.
                         */
                        if (
                            (
                             (
                              c.IsUnaryOperator() &&
                              (
                               !IsNotOperand(nextChar) ||
                               nextChar == '(' ||
                               nextChar.IsRadixOperator() ||
                               nextChar.IsUnaryOperator()
                              )
                             ) ||
                             (
                              c.IsRadixOperator() && char.IsLetterOrDigit(nextChar)
                             ) ||
                             (
                              c == '%' && (nextChar == '.' || nextChar == '#')
                             )
                            ) &&
                             (previousToken == null ||
                              (previousToken.Type != TokenType.Operand &&
                               !previousToken.Name.Equals(")")
                              )
                             )
                            )
                            operatorType = OperatorType.Unary;
                        else 
                            operatorType = OperatorType.Binary;
                    }
                }
            }
            if (!string.IsNullOrEmpty(source))
            {
                if (string.IsNullOrEmpty(unparsedSource))
                    unparsedSource = source;
                if (iterator.Current != source[^1])
                    iterator.Rewind(iterator.Index - 1);
            }
            unparsedSb.Append(source);
            return new Token(source, unparsedSb.ToString(), tokenType, operatorType);

            bool TokenAfterNextIsNonBase10()
            {
                if (nextChar == '$')
                    return true;
                var iterCopy = new RandomAccessIterator<char>(iterator, false);
                iterCopy.SetIndex(iterCopy.Index + 2);
                var nextTokName = string.Empty;
                var binRegex = "^[0-1]+$";
                if (iterCopy.Current == '.' || iterCopy.Current == '#')
                {
                    nextTokName = ScanTo(nextChar, iterCopy, FirstNonAltBin);
                    binRegex = "^[#.]+$";
                }
                else
                {
                    nextTokName = ScanTo(nextChar, iterCopy, FirstNonNumeric);
                }
                return !string.IsNullOrEmpty(nextTokName) && Regex.IsMatch(nextTokName, binRegex);
            }
        }

        /// <summary>
        /// Parses the source string into a tokenized <see cref="SourceLine"/> collection.
        /// </summary>
        /// <param name="fileName">The source file's path/name.</param>
        /// <param name="source">The source string.</param>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="parseOneLine">Stop after first line is parsed.</param>
        /// <returns>A collection of <see cref="SourceLine"/>s whose components are
        /// properly tokenized for further evaluation and assembly.</returns>
        /// <exception cref="ExpressionException"/>
        public static IEnumerable<SourceLine> Parse(string fileName, 
                                                    string source, 
                                                    AssemblyServices services,
                                                    bool parseOneLine = false)
        {
            var iterator = new RandomAccessIterator<char>(source.ToCharArray());
            Token rootParent, currentParent;
            Token token = null;

            Reset();

            Token currentOpen = null;
            int currentLine = 1, lineNumber = currentLine;

            // lineIndex is the iterator index at the start of each line for purposes of calculating token
            // positions. sourceLindeIndex is the iterator index at the start of each new line
            // of source. Usually lineIndex and sourceLindeIndex are the same, but for those source lines
            // whose source code span multiple lines, they will be different.
            int lineIndex = -1, opens = 0, sourceLineIndex = lineIndex;

            var lines = new List<SourceLine>();
            char previousChar = iterator.Current;

            while (iterator.GetNext() != EOF)
            {
                if (iterator.Current != NewLine && iterator.Current != ':' && iterator.Current != ';')
                {
                    try
                    {
                        token = ParseToken(previousChar, token, iterator, services);
                        if (token != null && !token.Name.Equals(":"))
                        {
                            previousChar = iterator.Current;
                            token.Position = iterator.Index - lineIndex - token.Name.Length + 1;
                            if (token.OperatorType == OperatorType.Open || 
                                token.OperatorType == OperatorType.Closed || 
                                token.OperatorType == OperatorType.Separator)
                            {
                                if (token.OperatorType == OperatorType.Open) 
                                {
                                    opens++;
                                    currentParent.AddChild(token);
                                    currentOpen =
                                    currentParent = token;
                                    AddBlankSeparator();
                                }
                                else if (token.OperatorType == OperatorType.Closed)
                                {    
                                    if (currentOpen == null)
                                        throw new SyntaxException(token.Position, $"Missing opening for closure \"{token.Name}\"");

                                    // check if matching ( to )
                                    if (!Groups[currentOpen.Name].Equals(token.Name))
                                        throw new SyntaxException(token.Position, $"Mismatch between \"{currentOpen.Name}\" in column {currentOpen.Position} and \"{token.Name}\"");

                                    // go up the ladder
                                    currentOpen = currentParent = currentOpen.Parent;

                                    while (currentOpen != null && currentOpen.OperatorType != OperatorType.Open)
                                        currentOpen = currentOpen.Parent;
                                    opens--;
                                }
                                else
                                {
                                    if (currentParent.Name.IsByteExtractor())
                                        currentParent = currentParent.Parent;
                                    currentParent = currentParent.Parent;
                                    currentParent.AddChild(token);
                                    currentParent = token;
                                }
                            }
                            else if (token.Type == TokenType.Instruction)
                            {
                                while (currentParent.Parent != rootParent)
                                    currentParent = currentParent.Parent;
                                currentParent.AddChild(token);
                                AddBlankSeparator();
                                AddBlankSeparator();
                            }
                            else
                            {
                                currentParent.AddChild(token);

                                if (token.OperatorType == OperatorType.Unary && token.Name.IsByteExtractor())
                                    currentParent = token;
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        if (e is ExpressionException ex)
                            services.Log.LogEntry(fileName, lineNumber, ex.Position, ex.Message);
                        else
                            services.Log.LogEntry(fileName, lineNumber, iterator.Index - sourceLineIndex, e.Message);
                    }
                    if (iterator.PeekNext() == NewLine)
                        iterator.MoveNext();
                }
                if (iterator.Current == ';')
                    _ = iterator.FirstOrDefault(c => c == NewLine || 
                                                     c == EOF     || 
                                                     (c == ':' && !services.Options.IgnoreColons));

                if (iterator.Current == NewLine || 
                    iterator.Current == ':'     || 
                    iterator.Current == EOF)
                {
                    previousChar = iterator.Current;
                    /* A new source line is when:
                       1. A line termination character (New Line, colon, EOF) is encountered
                       2. And either there are no more characters left or the most recent token created
                       3. The most recent token obeys the currently defined rules whether it can
                          terminate a line.
                     */
                    var newLine = iterator.Current == EOF ||
                                     (opens == 0 && TerminatesLine(token));
                    if (iterator.Current == NewLine)
                        currentLine++;
                    if (newLine)
                    {
                        var newSourceLine = new SourceLine(fileName, 
                                                           lineNumber,
                                                           sourceLineIndex + 1,
                                                           GetSourceLineSource(), 
                                                           rootParent.Children[0],
                                                           services,
                                                           parseOneLine);
                        lines.Add(newSourceLine);
                        if (parseOneLine && (newSourceLine.Label != null || newSourceLine.Instruction != null))
                            return lines;

                        if (services.Options.WarnLeft && 
                            newSourceLine.Label != null && 
                            newSourceLine.Label.Position != 1)
                            services.Log.LogEntry(newSourceLine, 
                                                  newSourceLine.Label, 
                                                  "Label is not at the beginning of the line.", 
                                                  false);
                        Reset();
                        lineNumber = currentLine;
                     }
                    else
                    {
                        token = null;
                    }
                    lineIndex = iterator.Index;
                    if (newLine)
                        sourceLineIndex = iterator.Index;
                }
            }
            if (currentOpen != null && currentOpen.OperatorType == OperatorType.Open)
             services.Log.LogEntry(fileName, 1, currentOpen.LastChild.Position, 
                 $"End of source reached without finding closing \"{Groups[currentOpen.Name]}\".");

            if (token != null)
                lines.Add(new SourceLine(fileName, 
                                         lineNumber, 
                                         sourceLineIndex + 1,
                                         GetSourceLineSource(), 
                                         rootParent.Children[0], 
                                         services, 
                                         parseOneLine));

            return lines;

            void AddBlankSeparator()
            {
                var sepToken = Token.SeparatorToken();
                sepToken.Position = token == null ? 1 : token.Position;
                currentParent.AddChild(sepToken);
                currentParent = sepToken;
            }

            string GetSourceLineSource()
            {
                if (iterator.Index > sourceLineIndex + 1)
                    return source.Substring(sourceLineIndex + 1, iterator.Index - sourceLineIndex - 1);
                return string.Empty;
            }

            void Reset()
            {
                currentParent =
                rootParent = new Token();
                AddBlankSeparator();
                AddBlankSeparator();
                token = null;
            }
        }

        static bool TerminatesLine(Token token) =>
            token == null ||
            (LineTerminationFunc != null && LineTerminationFunc(token)) ||
            (token.OperatorType != OperatorType.Binary &&
               token.OperatorType != OperatorType.Open &&
               !token.Name.Equals(","));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the function for the rule that determines whether
        /// a given token can terminate a line when the parser is at a new line 
        /// character.
        /// </summary>
        public static Func<Token, bool> LineTerminationFunc { get; set; }

        #endregion
    }
}