//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// The base class for a 6502.Net lexer. This class must be inherited.
/// </summary>
public abstract class LexerBase : Lexer
{
    protected int groups;
    private readonly LinkedList<IToken> _cachedTokens;

    private readonly Stack<int> _savedGroups;
    private IToken? _previousToken;

    protected LexerBase(ICharStream input)
        : this(input, TextWriter.Null, TextWriter.Null)
    {
    }

    protected LexerBase(ICharStream input, TextWriter output, TextWriter errorOutput)
        : base(input, output, errorOutput)
    {
        groups = 0;
        _savedGroups = new();
        _cachedTokens = new();
        ReservedWords = new Dictionary<string, int>();
    }

    protected void SaveGroups()
    {
        _savedGroups.Push(groups);
        PushMode(DEFAULT_MODE);
        groups = 0;
    }

    protected void UnsaveGroups()
    {
        _savedGroups.TryPop(out groups);
        if (ModeStack.Count > 0)
        {
            PopMode();
        }
    }

    public override void Emit(IToken token)
    {
        base.Token = token;
        _cachedTokens.AddLast(token);
    }

    public override IToken NextToken()
    {
        IToken next = base.NextToken();
        _previousToken = next;
        if (_cachedTokens.Count > 0)
        {
            next = _cachedTokens.First!.Value;
            _cachedTokens.RemoveFirst();
        }
        return next;
    }

    private bool CanBeDirective()
    {
        if (Text.Length > 0 && Text[0] == '.' && _previousToken != null)
        {
            if (_previousToken.Type.IsOneOf(SyntaxParser.NL, SyntaxParser.Colon))
            {
                return true;
            }
            int prevStop = _previousToken.StopIndex + 1;
            int startIndex = TokenStartCharIndex;
            return startIndex - prevStop > 0;
        }
        return true;
    }

    protected bool IsReservedWord()
    {
        if (CanBeDirective() && ReservedWords.TryGetValue(Text, out int type))
        {
            Type = type;
            if (type == SyntaxLexer.Cpu)
            {
                Emit();
                IToken cpuName = base.NextToken();
                if (cpuName.Type.IsOneOf(SyntaxLexer.StringLiteral, SyntaxLexer.UnicodeStringLiteral))
                {
                    try
                    {
                        string cpuid = cpuName.Text;
                        int qix = cpuid.IndexOf('"');
                        ReservedWords = new Dictionary<string, int>(
                            InstructionSets.GetByCpuid(cpuid[qix..].TrimOnce('"')),
                            IsCaseSensitive.ToStringComparer());
                    }
                    catch
                    {

                    }
                }
            }
            return true;
        }
        return false;
    }

    protected bool IsDotIdentifier()
    {
        return CanBeDirective();
    }

    protected bool IsShadowAF()
    {
        return ReservedWords.TryGetValue(Text, out int type) &&
            type == SyntaxLexer.ShadowAF;
    }

    protected void SkipNewline()
    {
        if (groups > 0)
        {
            Skip();
        }
    }

    protected bool PreviousIsExpr()
    {
        return _previousToken != null &&
               _previousToken.Type.IsOneOf(SyntaxLexer.BinLiteral,
                                           SyntaxLexer.BinFloatLiteral,
                                           SyntaxLexer.CharLiteral,
                                           SyntaxLexer.DecLiteral,
                                           SyntaxParser.False,
                                           SyntaxLexer.DoubleQuote,
                                           SyntaxLexer.DoublePlus,
                                           SyntaxLexer.DoubleHyphen,
                                           SyntaxLexer.HexLiteral,
                                           SyntaxLexer.Identifier,
                                           SyntaxLexer.MultiPlus,
                                           SyntaxLexer.MultiHyphen,
                                           SyntaxParser.NaN,
                                           SyntaxLexer.RightParen,
                                           SyntaxLexer.RightSquare,
                                           SyntaxLexer.RightCurly,
                                           SyntaxLexer.StringLiteral,
                                           SyntaxParser.True,
                                           SyntaxLexer.UnicodeStringLiteral);
    }

    public bool IsCaseSensitive { get; set; }

    public IDictionary<string, int> ReservedWords { get; set; }
}

