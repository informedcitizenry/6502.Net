//-----------------------------------------------------------------------------
// Copyright (c) 2017 Nate Burnett <informedcitizenry@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to 
// deal in the Software without restriction, including without limitation the 
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;

namespace Asm6502.Net
{
    /// <summary>
    /// A class that turns SourceLine listing to a formatted html file.
    /// </summary>
    public class HtmlOutput : AssemblerBase
    {
        #region Constructors

        public HtmlOutput() :
            base()
        {
            Reserved.Types.Add("Mnemonics", new HashSet<string>(new string[]
                {
                    "adc","and","asl","bcc","bcs","beq","bit","bmi","bne","bpl","brk","bvc","bvs","clc","cli",
                    "clv","cmp","cpx","cpy","dec","dex","dey","eor","inc","inx","iny","jmp","jsr","lda","ldx",
                    "ldy","lsr","nop","ora","pha","php","pla","plp","rol","ror","rti","rts","sbc","sbc","sec",
                    "sed","sei","sta","stx","sty","tax","tay","tsx","txa","txs","tya"
                }));

            Reserved.Types.Add("Blocks", new HashSet<string>(new string[]
                {
                    ".binclude", ".include",
                    ".block",    ".endblock",
                    ".macro",    ".endmacro",
                    ".segment",  ".endsegment" 
                }));

            Reserved.Types.Add("Comments", new HashSet<string>(new string[]
                {
                    ".comment",  ".endcomment"
                }));

            Reserved.Types.Add("PseudoOps", new HashSet<string>(new string[]
                {
                    ".addr", ".byte", ".char", ".dint", ".dword", ".enc", ".fill", ".lint", ".long", ".cstring", 
                    ".pstring", ".nstring", ".string", ".word", ".binary", 
                    ".align", 
                    ".dstruct", ".dunion", 
                    ".repeat", ".rta", ".sint",
                    ".lsstring"
                }));

            Reserved.Types.Add("Functions", new HashSet<string>(new string[]
                {
                     "abs", "acos", "asin", "atan", "cbrt", "ceil", "cos", "cosh", "deg", 
                     "exp", "floor", "frac", "hypot", "ln", "log10", "pow", "rad", "random", 
                     "round", "sgn", "sin", "sinh", "sqrt", "str", "tan", "tanh", "trunc"
                }));

            Reserved.Types.Add("Directives", new HashSet<string>(new string[]
                {
                    ".proff", ".pron", ".eor", ".error", ".end", ".cerror", 
                    ".cwarn", ".relocate", ".pseudopc", ".realpc", ".endrelocate", ".warn", 
                    ".cpu"
                }));

            Reserved.Types.Add("Operators", new HashSet<string>(new string[]
                {
                    "%", "#", "<", ">", "^", "!", "=" , "~", "&", "|", "+", "-", "/", "*",
                    "(", ")", "?"
                }));
        }
        #endregion

        #region Methods

        /// <summary>
        /// Renders a SourceLine as HTML, looking at its original source string,
        /// parsed tokens such as labels and instructions, and assembled bytes.
        /// </summary>
        /// <param name="writer">An HtmlTextWriter to render to.</param>
        /// <param name="line">The SourceLine.</param>
        private void RenderLine(HtmlTextWriter writer, SourceLine line)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "Address");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            if (string.IsNullOrEmpty(line.Instruction) == false)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "Operators");
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                string address = string.Empty;
                if (Reserved.IsOneOf("Mnemonics", line.Instruction))
                    writer.Write(".");
                else if (line.Instruction.Equals(".equ") || line.Instruction.Equals("="))
                    writer.Write("=");
                else
                    writer.Write(">");
                writer.RenderEndTag();

                writer.AddAttribute(HtmlTextWriterAttribute.Class, "Numbers");
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                address += string.Format("{0:x4}", line.PC);
                writer.Write(address);
                writer.RenderEndTag();
            }
            writer.RenderEndTag();
            writer.WriteLine();

            writer.AddAttribute(HtmlTextWriterAttribute.Class, "Assembly");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            if (line.Assembly.Count > 0)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "Numbers");
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                string bytes = string.Empty;
                line.Assembly.ForEach(b => bytes += string.Format(" {0:x2}", b));
                writer.Write(bytes);
                writer.RenderEndTag();
            }
            writer.RenderEndTag();
            writer.WriteLine();

            writer.AddAttribute(HtmlTextWriterAttribute.Class, "Disassembly");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            if (string.IsNullOrEmpty(line.Disassembly) == false)
            {
                string[] components = line.Disassembly.Split(' ');

                writer.AddAttribute(HtmlTextWriterAttribute.Class, "Mnemonics");
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.Write(components.First());
                writer.RenderEndTag();
               
                writer.WriteEncodedText(" ");
               
                if (components.Count() > 1)
                {
                    if (components.Last().StartsWith("#"))
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "Operators");
                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                        writer.Write("#");
                        writer.RenderEndTag();

                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "Numbers");
                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                        writer.Write(components.Last().Substring(1));
                        writer.RenderEndTag();
                    }
                    else
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "Numbers");
                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                        writer.Write(components.Last());
                        writer.RenderEndTag();
                    }
                }
            }
            writer.RenderEndTag();
            writer.WriteLine();

            writer.AddAttribute(HtmlTextWriterAttribute.Class, "Source");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            string source = line.SourceString;

            if (string.IsNullOrWhiteSpace(source))
            {
                writer.RenderEndTag();
                return;
            }
            for (int i = 0; i < source.Length; i++ )
            {
                if (char.IsWhiteSpace(source[i]))
                {
                    writer.Write("&nbsp;");
                    continue;
                }
                if (string.IsNullOrEmpty(line.Label) == false)
                {
                    writer.Write(source.Substring(0, line.Label.Length));
                    i += line.Label.Length;
                    if (i < source.Length - 1)
                    {
                        if (char.IsWhiteSpace(source[i]))
                        {
                            writer.WriteEncodedText(" ");
                            i++;
                        }
                    }
                }
                if (char.IsWhiteSpace(source[i]))
                {
                    writer.Write("&nbsp;");
                    continue;
                }
                if (string.IsNullOrEmpty(line.Instruction) == false)
                {
                    string cssClass = Reserved.GetType(line.Instruction);
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, cssClass);
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.Write(source.Substring(i, line.Instruction.Length));
                    writer.RenderEndTag();

                    i += line.Instruction.Length;
                    if (i < source.Length - 1)
                    {
                        if (char.IsWhiteSpace(source[i]))
                        {
                            writer.Write("&nbsp;");
                            i++;
                        }
                    }
                }
                if (string.IsNullOrEmpty(line.Operand) == false)
                {
                    bool inquotes = false;
                    bool comment = Reserved.IsOneOf("Comments", line.Instruction);
                    string token = string.Empty;

                    if (comment)
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "Comments");
                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    }

                    for(int j = i; j < source.Length; j++)
                    {
                        if (comment)
                        {
                            if (char.IsWhiteSpace(source[j]))
                                writer.Write("&nbsp;");
                            else
                                writer.Write(source[j].ToString());
                            continue;
                        }
                        if (inquotes)
                        {
                            if (char.IsWhiteSpace(source[j]))
                                writer.Write("&nbsp;");
                            else
                                writer.Write(source[j].ToString());

                            if (source[j] == '"' || source[j] == '\'')
                            {
                                inquotes = !inquotes;
                                writer.RenderEndTag();
                            }
                            continue;
                        }
                        if (char.IsWhiteSpace(source[j]) || IsOperator(source[j]))
                        {
                            if (!string.IsNullOrEmpty(token))
                            {
                                if (IsNumber(token))
                                {
                                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "Numbers");
                                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                    writer.Write(token);
                                    writer.RenderEndTag();
                                }
                                else
                                {
                                    writer.Write(token);
                                }
                                token = string.Empty;
                            }
                            if (char.IsWhiteSpace(source[j]))
                            {
                                writer.Write("&nbsp;");
                                continue;
                            }
                            else 
                            {
                                writer.AddAttribute(HtmlTextWriterAttribute.Class, "Operators");
                                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                writer.WriteEncodedText(source[j].ToString());
                                writer.RenderEndTag();
                            }
                        }
                        else
                        {
                            if (source[j] == '"' || source[j] == '\'')
                            {
                                inquotes = true; 
                                writer.AddAttribute(HtmlTextWriterAttribute.Class, "Quote");
                                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                writer.WriteEncodedText(source[j].ToString());
                            }
                            else if (source[j] == ';')
                            {
                                comment = true;
                                writer.AddAttribute(HtmlTextWriterAttribute.Class, "Comments");
                                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                writer.Write(source[j].ToString());
                            }
                            else
                            {
                                token += source[j].ToString();
                            }
                        }   
                    }
                    if (IsNumber(token) && !inquotes && !comment)
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "Numbers");
                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                        writer.Write(token);
                        writer.RenderEndTag();
                    }
                    else
                    {
                        writer.Write(token);
                    }
                    if (comment)
                        writer.RenderEndTag();
                    break;
                }
            }
            writer.RenderEndTag();
        }

        /// <summary>
        /// Determines whether the character is an operator.
        /// </summary>
        /// <param name="p">The char to test.</param>
        /// <returns>True if the char is an operator, otherwise false.</returns>
        private bool IsOperator(char p)
        {
            return Regex.IsMatch(p.ToString(), @"[\|\?\^\(\)\+\*-/%~#<>]");
        }

        /// <summary>
        /// Determines whether the string token is a number of any of the three
        /// accepted numerical formats: decimal, hex, and binary.
        /// </summary>
        /// <param name="token">The token to test.</param>
        /// <returns>True, if the token represents a number, otherwise false.</returns>
        private bool IsNumber(string token)
        {
            return Regex.IsMatch(token, @"\$[a-fA-F0-9]+") ||
                   Regex.IsMatch(token, @"%[0-1]+") ||
                   Regex.IsMatch(token, @"\d+");
        }

        /// <summary>
        /// Gets the HTML markup of the source listing.
        /// </summary>
        /// <param name="lines">The SourceLine listing.</param>
        /// <returns>A string containing the HTML.</returns>
        public string GetHtml(IEnumerable<SourceLine> lines)
        {
            StringWriter stringWriter = new StringWriter();
            using (HtmlTextWriter htmlWriter = new HtmlTextWriter(stringWriter))
            {
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Html);
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Head);
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Style);
                htmlWriter.WriteLine("body { font-family: Consolas, Courier, monospace; }");
                htmlWriter.WriteLine(" table { width: 800px; }");
                htmlWriter.WriteLine(" td { vertical-align: top; }");
                htmlWriter.WriteLine(".Address { width: 10%; }");
                htmlWriter.WriteLine(".Assembly { width: 25%; }");
                htmlWriter.WriteLine(".Disassembly { width: 15%; }");
                htmlWriter.WriteLine(".Source { width: 50%; }");
                htmlWriter.WriteLine(".Operators { color: blue; }");
                htmlWriter.WriteLine(".Numbers { color: red; }");
                htmlWriter.WriteLine(".Mnemonics { color: #000080; font-weight: bold; }");
                htmlWriter.WriteLine(".PseudoOps { color: #ff8000; font-weight: bold; }");
                htmlWriter.WriteLine(".Directives { color: #0000f; font-weight: bold; }");
                htmlWriter.WriteLine(".Comments { color: #800000; font-style: italic; }");
                htmlWriter.WriteLine(".Quote { color: #ff8000; }");
                htmlWriter.Write(".Functions { color: #000080; }");

                htmlWriter.RenderEndTag();
                htmlWriter.RenderEndTag();
                htmlWriter.WriteLine();

                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Body);
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Table);
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Tbody);
                foreach (var line in lines)
                {
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Tr);
                    RenderLine(htmlWriter, line);
                    htmlWriter.RenderEndTag();
                    htmlWriter.WriteLine();
                }
                htmlWriter.RenderEndTag(); // </tbody>
                htmlWriter.RenderEndTag(); // </table>
                htmlWriter.RenderEndTag(); // </body>
                htmlWriter.RenderEndTag(); // </html>
            }
            return stringWriter.ToString();
        }

        #endregion
    }
}
