//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.IO;

namespace Sixty502DotNet
{
    /// <summary>
    /// The base class lexer responsible for tokenizing assembly source code.
    /// This class is inherited by the generated lexer.
    /// </summary>
    public abstract class LexerBase : Lexer
    {
        private readonly LinkedList<IToken> _cachedTokens;
        private int _groups;
        private IToken? _previousToken;
        private IDictionary<string, int>? _reservedWords;

        /// <summary>
        /// Construct a new instance of the <see cref="LexerBase"/> class.
        /// </summary>
        /// <param name="input">The input character stream.</param>
        protected LexerBase(ICharStream input)
            : this(input, Console.Out, Console.Error)
        {
        }

        /// <summary>
        /// Construct a new instance of the <see cref="LexerBase"/> class.
        /// </summary>
        /// <param name="input">The input character stream.</param>
        /// <param name="output">The output to send output info.</param>
        /// <param name="errorOutput">The output to report error info.</param>
        protected LexerBase(ICharStream input, TextWriter output, TextWriter errorOutput)
            : base(input, output, errorOutput)
        {
            _groups = 0;
            _previousToken = null;
            _cachedTokens = new LinkedList<IToken>();
            Sources = new Stack<string>();
        }

        /// <summary>
        /// Checks if the macro indicated in the current text state is defined.
        /// </summary>
        /// <param name="text">The text of the potential macro definition.
        /// </param>
        /// <returns><c>true</c> if the macro is already defined, <c>false</c>
        /// otherwise.</returns>
        protected bool MacroDefined(string text)
        {
            if (IsAMacro(Preprocessor.GetMacroName(text)))
            {
                Type = Sixty502DotNetLexer.BadMacro;
                Channel = Sixty502DotNetLexer.ERROR;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the token is a language keyword/directive.
        /// </summary>
        /// <returns><c>true</c> if the currently parsed token is a
        /// language keyword, <c>false</c> otherwise.</returns>
        protected bool IsDirective()
        {
            var directive = -1;
            if ((_previousToken == null || IsInvocation()) &&
                _reservedWords?.TryGetValue(Text, out directive) == true)
            {
                Type = directive;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the token represents a macro invocation.
        /// </summary>
        /// <returns><c>true</c> if the currently parsed token is a macro
        /// invocation, <c>false</c> otherwise.</returns>
        protected bool IsInvocation() => _previousToken != null &&
                        (_previousToken.Channel == Hidden ||
                        char.IsWhiteSpace(_previousToken.Text[^1]));

        /// <summary>
        /// Checks if the currently parsing token is a <c>af'</c> token.
        /// </summary>
        /// <returns><c>true</c> if the token represents a Z80 shadow <c>af</c>
        /// token, <c>false</c> otherwise.</returns>
        protected bool IsShadowAf() =>
            _reservedWords?.ContainsKey(Text) == true;

        /// <summary>
        /// Check if the currently parsing token is a reserved word.
        /// </summary>
        protected void CheckReserved()
        {
            int keyword = -1;
            if (_reservedWords?.TryGetValue(Text, out keyword) == true)
            {
                Type = keyword;
            }
        }

        /// <summary>
        /// Check if the currently parsing token ends a macro definition.
        /// </summary>
        /// <returns><c>true</c> if the currently parsing token is an
        /// <c>.endmacro</c> token, <c>false</c> otherwise.</returns>
        protected bool IsEndMacro()
        {
            var next = InputStream.LA(1);
            return next == TokenConstants.EOF || char.IsWhiteSpace((char)next);
        }

        /// <summary>
        /// Activate the current mnemonic set of the instruction set.
        /// </summary>
        /// <param name="cpuType">The CPU type name.</param>
        public void SetMnemonics(string cpuType)
            => _reservedWords = InstructionSet?.SetMnemonics(cpuType);

        /// <summary>
        /// Set the CPU according to the current token in the stream.
        /// </summary>
        public void SetCPU()
        {
            Type = Sixty502DotNetLexer.Cpu;
            Emit(); // send token to output
            var cpuToken = Token;
            var name = base.NextToken();
            while (name.Type != Sixty502DotNetLexer.StringLiteral)
            {
                if (name.Type == TokenConstants.EOF ||
                    name.Channel != Hidden)
                {
                    break;
                }
                name = base.NextToken();
            }
            Preprocessor?.SetCpu(this, cpuToken, name);
        }

        /// <summary>
        /// Expand the <c>.include</c> directive into a stream of parsed tokens
        /// and cache for the token stream.
        /// </summary>
        protected void ExpandInclude()
        {
            Channel = Sixty502DotNetLexer.INCLUDE_CHANNEL;
            PushMode(Sixty502DotNetLexer.includeArgMode);
            var include = Text;
            var t = NextToken();
            var args = new List<IToken>();
            while (CurrentMode == Sixty502DotNetLexer.includeArgMode)
            {
                if (t.Channel == Sixty502DotNetLexer.INCLUDE_CHANNEL)
                {
                    args.Add(t);
                    // even though they are in the include channel
                    // do not send these tokens to output in case
                    // the -E option is selected.
                    Consume();
                }
                t = base.NextToken();
            }
            Channel = DefaultTokenChannel;
            IList<IToken> tokens = Preprocessor!.Include(include, args[0]);
            foreach (var proc in tokens)
            {
                Emit(proc);
            }
        }

        /// <summary>
        /// Expand the defined macro into a stream of parsed tokens and cache
        /// for the token stream.
        /// </summary>
        protected void ExpandMacro()
        {
            if (IsAMacro(Text))
            {
                Channel = Sixty502DotNetLexer.PREPROCESSOR;
                PushMode(Sixty502DotNetLexer.macroInvokeArgMode);
                Type = Sixty502DotNetLexer.MacroInvocation;
                var invokeText = Text;
                var invokeTokens = new List<IToken>();
                var token = base.NextToken();
                // ignore leading white space
                if (!(token.Type == Sixty502DotNetLexer.MacroInvokeText && string.IsNullOrWhiteSpace(token.Text)))
                {
                    invokeTokens.Add(token);
                }
                while (true)
                {
                    Consume(); // do not send these to the output stream!
                    if (CurrentMode == DEFAULT_MODE || token.Type == TokenConstants.EOF)
                    {
                        break;
                    }
                    token = base.NextToken();
                    invokeTokens.Add(token);
                }
                var expanded = Preprocessor?.ExpandMacro(Line, invokeText, invokeTokens);
                if (expanded != null)
                {
                    Channel = DefaultTokenChannel;
                    foreach (var proc in expanded)
                        Emit(proc);
                }
            }
            else
            {
                Type = Sixty502DotNetLexer.BadMacro;
            }
        }

        private void Consume()
            => _cachedTokens.RemoveLast();

        /// <summary>
        /// Emit a token to the output stream.
        /// </summary>
        /// <param name="token">The <see cref="IToken"/> object.</param>
        public override void Emit(IToken token)
        {
            base.Token = token;
            _cachedTokens.AddLast(token);
            if (FirstSeen == null && token.Channel != Hidden && token.Type != Sixty502DotNetLexer.Newline)
            {
                FirstSeen = Token;
            }
        }

        /// <summary>
        /// Parse and return a new token for the output stream.
        /// </summary>
        /// <returns>A <see cref="IToken"/> to be placed into the output
        /// stream.</returns>
        public override IToken NextToken()
        {
            var next = base.NextToken();
            _previousToken = next;
            if (_cachedTokens.Count > 0)
            {
                next = _cachedTokens.First!.Value;
                _cachedTokens.RemoveFirst();
            }
            if (next.Type == TokenConstants.EOF && Sources.Count > 0)
            {
                var nextSource = Sources.Pop();
                var charStream = CharStreams.fromPath(nextSource);
                SetInputStream(charStream);
                var factory = (TokenFactory)TokenFactory;
                factory.Filenames.Pop();
                factory.Filenames.Push(nextSource);
                // separate source files with a newline token for the parser
                return new Token(Sixty502DotNetParser.Newline, "\n");
            }
            return next;
        }

        /// <summary>
        /// Define a macro.
        /// </summary>
        protected void DefineMacro()
        {
            Channel = Sixty502DotNetLexer.PREPROCESSOR;
            Mode(Sixty502DotNetLexer.macroArgMode);
            var name = Preprocessor.GetMacroName(Text);
            if (string.IsNullOrEmpty(name))
            {
                Type = Sixty502DotNetLexer.BadMacro;
            }
            else
            {
                Type = Sixty502DotNetLexer.MacroDef;
                var macroToken = new Token(Tuple.Create((ITokenSource)this, (ICharStream)InputStream),
                    Type,
                    Channel,
                    TokenStartCharIndex,
                    CharIndex - 1);
                var defTokens = new List<IToken>();
                while (CurrentMode != DEFAULT_MODE)
                {
                    defTokens.Add(base.NextToken());
                    Consume(); // do not send to output
                }
                _ = base.NextToken(); // pop the last remaining token
                Preprocessor?.DefineMacro(macroToken, name, defTokens);
            }
        }

        /// <summary>
        /// Check if the text of the currently parsing token is a macro.
        /// </summary>
        /// <param name="text">The text of the token.</param>
        /// <returns><c>true</c> if the text represents a macro,
        /// <c>false</c> otherwise.</returns>
        protected bool IsAMacro(string text)
            => Preprocessor?.IsAMacro(text) == true;

        /// <summary>
        /// Signal to the lexer that a grouping token is encountered.
        /// </summary>
        protected void Group() => ++_groups;

        /// <summary>
        /// Signal to the lexer that an ungrouping token is encountered.
        /// </summary>
        protected void Ungroup() => --_groups;

        /// <summary>
        /// Process the literal newline character whether it should be a token.
        /// </summary>
        protected void IsNewline() { if (_groups > 0) Skip(); }

        /// <summary>
        /// Checks if the <c>,</c> character denotes the terminus of a macro
        /// default argument expression.
        /// </summary>
        protected void IsMacroDefaultArgEnd()
        {
            if (_groups > 0)
            {
                Type = Sixty502DotNetLexer.MacroDefaultText;
            }
            else
            {
                Mode(Sixty502DotNetLexer.macroArgMode);
            }
            Channel = Sixty502DotNetLexer.PREPROCESSOR;
        }

        /// <summary>
        /// Checks if the <c>,</c> character denotes the terminus of a macro
        /// invocation argument.
        /// </summary>
        protected void IsMacroInvokeArgEnd()
        {
            if (_groups > 0)
            {
                Type = Sixty502DotNetLexer.MacroInvokeText;
            }
            Channel = Sixty502DotNetLexer.PREPROCESSOR;
        }

        /// <summary>
        /// Get the lexer's sources processed.
        /// </summary>
        public Stack<string> Sources { get; init; }

        /// <summary>
        /// Get the first seen token properly processed.
        /// </summary>
        public IToken? FirstSeen { get; private set; }

        /// <summary>
        /// Get or set the lexer's <see cref="Preprocessor"/> responsible for
        /// all text preprocessing.
        /// </summary>
        public Preprocessor? Preprocessor { get; set; }

        /// <summary>
        /// Get or set the lexer's current instruction set to differentiate
        /// keyword tokens such as mnemonics and directives from identifier
        /// tokens.
        /// </summary>
        public InstructionSet? InstructionSet { get; set; }
    }
}
