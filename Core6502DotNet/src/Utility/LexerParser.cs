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
        const char EOF = char.MinValue;
        const char SingleQuote = '\'';
        const char NewLine = '\n';

        static readonly HashSet<string> _operators = new HashSet<string>
        {
            "|", "||", "&", "&&", "<<", ">>", "<", ">", ">=", "<=", "==", "!=", "(", ")",
            "[",  "]", "%",  "^", "^^", "`", "~", "*", "-", "+", "/", ",", ":", "$"
        };

        static bool IsNotOperand(char c) => !char.IsLetterOrDigit(c) &&
                                            c != '.' &&
                                            c != '_' &&
                                            c != SingleQuote &&
                                            c != '"';

        static string ScanTo(char previousChar,
                             RandomAccessIterator<char> iterator,
                             Func<char, char, char, bool> terminal,
                             bool ignoreWhitespace)
        {
            var tokenNameBuilder = new StringBuilder();
            char c = iterator.Current;
            tokenNameBuilder.Append(c);
            if (!terminal(previousChar, c, iterator.PeekNext()))
            {
                previousChar = c;
                c = iterator.GetNext();
                while (!terminal(previousChar, c, iterator.PeekNext()))
                {
                    tokenNameBuilder.Append(c);
                    if (!ignoreWhitespace && char.IsWhiteSpace(c))
                        break;
                    previousChar = c;
                    c = iterator.GetNext();
                }
            }

            return tokenNameBuilder.ToString();
        }

        static bool FirstNonHex(char prev, char current, char next)
        {
            if (char.IsDigit(current) ||
                (current >= 'a' && current <= 'f') ||
                (current >= 'A' && current <= 'F'))
            {
                return false;
            }
            return true;
        }

        static bool FirstNonNumeric(char prev, char current, char next)
        {
            if (!char.IsDigit(current))
            {
                if (current == '.') // 0.11
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
                         (next == '+' || next == '-'))
                        return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        static bool FirstNonSymbol(char prev, char current, char next) =>
            !char.IsLetterOrDigit(current) && current != '_' && current != '.' && current != SingleQuote;

        static bool FirstNonLetterOrDigit(char prev, char current, char next)
            => !char.IsLetterOrDigit(current);

        static bool FirstNonPlusMinus(char prev, char current, char next)
        {
            return prev != current && next != current;
        }

        static bool FirstNonMatchingOperator(char prev, char current, char next)
        {
            if (_operators.Any(s => s.Contains($"{prev}{current}{next}")))
                return false;
            if (_operators.Any(s => s.Contains($"{prev}{current}")))
                return false;
            return !_operators.Any(s => s.Contains($"{current}{next}"));
        }

        static bool NonNewLineWhiteSpace(char c) => c == ' ' || c == '\t';

        static Token ParseToken(char previousChar, Token previousToken, RandomAccessIterator<char> iterator)
        {
            char c = iterator.Current;
            while (char.IsWhiteSpace(c))
            {
                if (c == NewLine || c == EOF)
                {
                    iterator.Rewind(iterator.Index - 1);
                    return null;
                }
                c = iterator.GetNext();
            }
            var token = new Token();

            //first case, simplest
            var nextChar = iterator.PeekNext();
            if (char.IsDigit(c) ||
                char.IsLetter(c) ||
                c == '_' ||
                c == '?' ||
                (c == '.' && char.IsLetterOrDigit(nextChar)) ||
                (c == '\\' && char.IsLetterOrDigit(nextChar)))
            {
                token.Type = TokenType.Operand;
                if (char.IsDigit(c) || (c == '.' && char.IsDigit(nextChar)))
                {
                    if (char.IsDigit(c) && previousChar == '$')
                        token.Name = ScanTo(previousChar, iterator, FirstNonHex, false);
                    else
                        token.Name = ScanTo(previousChar, iterator, FirstNonNumeric, false);
                }
                else if (c == '\\')
                {
                    iterator.MoveNext();
                    token.Name = c + ScanTo(previousChar, iterator, FirstNonLetterOrDigit, false);
                }
                else if (c == '?')
                {
                    token.UnparsedName =
                    token.Name = "?";
                    return token;
                }
                else
                {
                    token.UnparsedName =
                    token.Name = ScanTo(previousChar, iterator, FirstNonSymbol, false);
                    if (!Assembler.Options.CaseSensitive)
                        token.Name = token.Name.ToLower();

                    if (Assembler.InstructionLookupRules.Any(rule => rule(token.Name)))
                    {
                        token.Type = TokenType.Instruction;
                    }
                    else if (iterator.Current == '(' ||
                        (iterator.Current != NewLine && char.IsWhiteSpace(iterator.Current) &&
                        iterator.PeekNextSkipping(NonNewLineWhiteSpace) == '('))
                    {
                        token.Type = TokenType.Operator;
                        token.OperatorType = OperatorType.Function;
                    }
                    else
                    {
                        token.Type = TokenType.Operand;
                    }
                }
            }
            else if (c == '"' || c == SingleQuote)
            {
                var open = c;
                var quoteBuilder = new StringBuilder(c.ToString());
                while ((c = iterator.GetNext()) != open && c != char.MinValue)
                {
                    quoteBuilder.Append(c);
                    if (c == '\\')
                        c = iterator.GetNext();
                }
                if (c == char.MinValue)
                    throw new ExpressionException(token.Position, $"Quote string not enclosed.");
                quoteBuilder.Append(c);
                var unescaped = Regex.Unescape(quoteBuilder.ToString());
                if (c == '\'' && unescaped.Length > 3)
                    throw new ExpressionException(token.Parent, "Too many characters in character literal.");
                token.Name = unescaped;
                token.Type = TokenType.Operand;
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
                           two separate '+'. What kind of thing is the second '+'? We worry about that later.
                        c. OPERAND+++3 / ...)+++(... => again, the first '+' is a binary operator. We need to split
                           it off from the rest of the string of '+' characters, and we worry about later.
                     2. The unary operator:
                        a. +3 / +(... => single '+' immediately preceding an operand/grouping.
                        b. ++3 / ++(... => parser doesn't accept C-style prefix operators, so one of these is an
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
                      4. An anonymous label:
                         a. + => Simplest scenario.
                         b. ++, +++, ++++, etc. => Treat as one.
                     */
                    // Get the full string
                    token.Name = ScanTo(previousChar, iterator, FirstNonPlusMinus, false);
                    if (previousToken != null && (previousToken.Type == TokenType.Operand || previousToken.Name.Equals(")")))
                    {
                        // looking backward at the previous token, if it's an operand or grouping then we 
                        // know this is a binary
                        token.Type = TokenType.Operator;
                        token.OperatorType = OperatorType.Binary;
                        if (token.Name.Length > 1) // we need to split off the rest of the string so we have a single char '+'
                            iterator.Rewind(token.Position);
                    }
                    else if (!IsNotOperand(nextChar) || nextChar == '(')
                    {
                        // looking at the very next character in the input stream, if it's an operand or grouping 
                        // then we know this is a unary
                        if (token.Name.Length > 1)
                        {
                            // If the string is greater than one character,
                            // then it's not a unary, it's an operand AND a unary. So we split off the 
                            // rest of the string.
                            iterator.Rewind(token.Position);
                            token.Type = TokenType.Operand;
                        }
                        else
                        {
                            token.Type = TokenType.Operator;
                            token.OperatorType = OperatorType.Unary;
                        }
                    }
                    else
                    {
                        token.Type = TokenType.Operand;
                        //if (token.Name.Length > 1)
                        //  iterator.Rewind(iterator.Index - 1);
                    }
                }
                else if (c == '*')
                {
                    // Same as +/- scenario above, if the previous token is an operand or grouping,
                    // we need to treat the splat as a binary operator.
                    if (previousToken != null && (previousToken.Type == TokenType.Operand || previousToken.Name.Equals(")")))
                    {
                        token.Type = TokenType.Operator;
                        token.OperatorType = OperatorType.Binary;
                    }
                    else
                    {
                        // but since there is no unary version of this we will treat as an operand, and let the evaluator
                        // deal with any problems like *OPERAND /*(
                        token.Type = TokenType.Operand;
                    }
                    token.Name = c.ToString();
                }
                else
                {
                    // not a number, symbol, string, or special (+, -, *) character. So we just treat as an operator
                    token.Type = TokenType.Operator;
                    if (c.IsSeparator() || c.IsOpenOperator() || c.IsClosedOperator())
                    {
                        token.Name = c.ToString();
                        if (c.IsSeparator())
                            token.OperatorType = OperatorType.Separator;
                        else if (c.IsOpenOperator())
                            token.OperatorType = OperatorType.Open;
                        else
                            token.OperatorType = OperatorType.Closed;
                    }
                    else
                    {
                        token.Name = ScanTo(previousChar, iterator, FirstNonMatchingOperator, false);
                        token.UnparsedName = token.Name;
                        /* The general strategy to determine whether an operator is unary or binary:
                            1. Is it actually one of the defined unary types?
                            2. Peek at the next character. Is it a group or operand, or not?
                            3. Look behind at the previous token. Is it also a group or operand, or not?
                            4. If the token does NOT follow an operand or group, AND it precedes a group character,
                               or operand character, then it is a unary.
                            5. All other cases, binary.
                         * 
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
                             )
                            ) &&
                             (previousToken == null ||
                              (previousToken.Type != TokenType.Operand &&
                               !previousToken.Name.Equals(")")
                              )
                             )
                            )
                            token.OperatorType = OperatorType.Unary;
                        else
                            token.OperatorType = OperatorType.Binary;
                    }
                }
            }
            if (string.IsNullOrEmpty(token.UnparsedName))
                token.UnparsedName = token.Name;
            if (iterator.Current != token.Name[^1])
                iterator.Rewind(iterator.Index - 1);
            return token;
        }

        /// <summary>
        /// Parses the source string into a tokenized <see cref="SourceLine"/> collection.
        /// </summary>
        /// <param name="fileName">The source file's path/name.</param>
        /// <param name="source">The source string.</param>
        /// <returns>A collection of <see cref="SourceLine"/>s whose components are
        /// properly tokenized for further evaluation and assembly.</returns>
        /// <exception cref="ExpressionException"/>
        public static IEnumerable<SourceLine> Parse(string fileName, string source)
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
                if (iterator.Current != NewLine && iterator.Current != ':')
                {
                    token = ParseToken(previousChar, token, iterator);
                    if (token != null)
                    {
                        previousChar = iterator.Current;
                        token.Parent = currentParent;
                        token.Position = iterator.Index - lineIndex - token.Name.Length + 1;
                        if (token.OperatorType == OperatorType.Open || token.OperatorType == OperatorType.Closed || token.OperatorType == OperatorType.Separator)
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
                                while (currentOpen != null && currentOpen.OperatorType != OperatorType.Open)
                                    currentOpen = currentOpen.Parent;

                                if (currentOpen == null)
                                    throw new ExpressionException(token, $"Missing opening for closure \"{token.Name}\"");

                                // check if matching ( to )
                                if (!Evaluator.Groups[currentOpen.Name].Equals(token.Name))
                                    throw new ExpressionException(token, $"Mismatch between \"{currentOpen.Name}\" in column {currentOpen.Position} and \"{token.Name}\"");

                                // go up the ladder
                                currentOpen = currentParent = token.Parent = currentOpen.Parent;
                                opens--;
                            }
                            else
                            {
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
                        }
                    }
                    if (iterator.PeekNext() == NewLine)
                        iterator.MoveNext();
                }
                if (iterator.Current == NewLine || iterator.Current == ':' || iterator.Current == EOF)
                {
                    previousChar = iterator.Current;
                    /* A new source line is when:
                       1. A line termination character (New Line, colon, EOF) is encountered
                       2. And either there are no more characters left or the most recent token created
                       3. Is not a binary operator nor it is a comma separator.
                     */
                    var newLine = iterator.Current == EOF ||
                                    (opens == 0 &&
                                     (token == null ||
                                      (token.OperatorType != OperatorType.Binary &&
                                       token.OperatorType != OperatorType.Open &&
                                       !token.Name.Equals(",")
                                      )
                                     )
                                    );
                    if (iterator.Current == NewLine)
                        currentLine++;
                    if (newLine)
                    {
                        var newSourceLine = new SourceLine(fileName, lineNumber, GetSourceLineSource(), rootParent.Children[0], true);
                        lines.Add(newSourceLine);
                        if (Assembler.Options.WarnLeft && newSourceLine.Label.Position != 1)
                            Assembler.Log.LogEntry(newSourceLine, newSourceLine.Label, "Label is not at the beginning of the line.", false);
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
                throw new ExpressionException(currentOpen.LastChild.Position, $"End of source reached without finding closing \"{Evaluator.Groups[currentOpen.Name]}\".");

            if (token != null)
                lines.Add(new SourceLine(fileName, lineNumber, GetSourceLineSource(), rootParent.Children[0], true));

            return lines;

            void AddBlankSeparator()
            {
                var sepToken = new Token()
                {
                    Type = TokenType.Operator,
                    OperatorType = OperatorType.Separator,
                    Name = string.Empty,
                    Position = token == null ? 1 : token.Position,
                    Children = new List<Token>()
                };
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
                currentParent.Children = new List<Token>();
                AddBlankSeparator();
                AddBlankSeparator();
                token = null;
            }
        }
    }
}
