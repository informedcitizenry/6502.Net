// Copyright (c) 2026 informedcitizenry
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Sixty502DotNet.Shared.Arch;
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse.Ast;
using System.Collections.Frozen;

namespace Sixty502DotNet.Shared.Encode;

using EncodeFn = Func<AssemblyState, TokenType, Operand, bool>;

public static partial class I86Encoder
{
    private const int D = 0b10;

    private const int W = 0b01;
    
    public static class Mode
    {
        public const int NoDispl  = 0b00_000_000;
        public const int Displ8   = 0b01_000_000;
        public const int Displ16  = 0b10_000_000;
        public const int Reg2Reg  = 0b11_000_000;
        public const int IndAddr  = 0b00_000_110;
        public const int RegField = 0b00_111_000;
        public const int RegMask  = 0b11_000_111;
        public const int RmMask   = 0b11_111_000;
    }
    
    private static readonly FrozenDictionary<(TokenType, TokenType), int> s_rms = new Dictionary<(TokenType, TokenType), int>
    {
        { (TokenType.Bx, TokenType.Si),   0b0000_0000 },
        { (TokenType.Bx, TokenType.Di),   0b0000_0001 },
        { (TokenType.Bp, TokenType.Si),   0b0000_0010 },
        { (TokenType.Bp, TokenType.Di),   0b0000_0011 },
        { (TokenType.Si, TokenType.Eof),  0b0000_0100 },
        { (TokenType.Di, TokenType.Eof),  0b0000_0101 },
        { (TokenType.Eof,TokenType.Eof),  0b0000_0110 },
        { (TokenType.Bp, TokenType.Eof),  0b0100_0110 },
        { (TokenType.Bx, TokenType.Eof),  0b0000_0111 }
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<TokenType, int> s_ea = new Dictionary<TokenType, int>
    {
        { TokenType.Dec, 0x08fe },
        { TokenType.Div, 0x30f6 },
        { TokenType.Fldenv, 0x20d9 },
        { TokenType.Fnstenv, 0x30d9 },
        { TokenType.Fstenv, 0x30d99b },
        { TokenType.Idiv, 0x38f6 },
        { TokenType.Imul, 0x28f6 },
        { TokenType.Inc, 0x00fe },
        { TokenType.Mul, 0x20f6 },
        { TokenType.Neg, 0x18f6 },
        { TokenType.Not, 0x10f6 },
        { TokenType.Rcl, 0x10d0 }, 
        { TokenType.Rcr, 0x18d0 }, 
        { TokenType.Rol, 0x00d0 }, 
        { TokenType.Ror, 0x08d0 },
        { TokenType.Sal, 0x38d0 },
        { TokenType.Sar, 0x20d0 },
        { TokenType.ShlMnem, 0x20d0 },
        { TokenType.ShrMnem, 0x28d0 }
    }.ToFrozenDictionary();
    
    private static readonly FrozenDictionary<TokenType, int> s_ea16 = new Dictionary<TokenType, int>
    {
        { TokenType.Call, 0x10ff }, 
        { TokenType.Fiadd, 0x00de },
        { TokenType.Ficom, 0x10de },
        { TokenType.Ficomp, 0x18de },
        { TokenType.Fidiv, 0x30de },
        { TokenType.Fidivr, 0x38de },
        { TokenType.Fild, 0x00df },
        { TokenType.Fimul, 0x08de },
        { TokenType.Fist, 0x10df },
        { TokenType.Fistp, 0x18df },
        { TokenType.Fisub, 0x20de },
        { TokenType.Fisubr, 0x28de },
        { TokenType.Fldcw, 0x28d9 },
        { TokenType.Fnstcw, 0x38d9 },
        { TokenType.Fnstsw, 0x38dd }, 
        { TokenType.Fstcw, 0x38d99b },
        { TokenType.Idiv, 0x38f6 },
        { TokenType.Imul, 0x28f6 },
        { TokenType.Jmp, 0x20ff },
        { TokenType.Mul, 0x20f6 },
        { TokenType.Pop, 0x008f },
        { TokenType.Push, 0x30ff }
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<TokenType, int> s_ea32 = new Dictionary<TokenType, int>
    {
        { TokenType.Call, 0x18ff },
        { TokenType.Jmp, 0x28ff },
        { TokenType.Fadd, 0x00d8 },
        { TokenType.Fcom, 0x10d8 },
        { TokenType.Fcomp, 0x18d8 },
        { TokenType.Fdiv, 0x30d8 },
        { TokenType.Fdivr, 0x38d8 },
        { TokenType.Fiadd, 0x00da },
        { TokenType.Ficom, 0x10da },
        { TokenType.Ficomp, 0x18da },
        { TokenType.Fidiv, 0x30da },
        { TokenType.Fidivr, 0x38da },
        { TokenType.Fild, 0x00db },
        { TokenType.Fimul, 0x08da },
        { TokenType.Fist, 0x10db },
        { TokenType.Fistp, 0x18db },
        { TokenType.Fisub, 0x20da },
        { TokenType.Fisubr, 0x28da },
        { TokenType.Fld, 0x00d9 },
        { TokenType.Fmul, 0x08d8 },
        { TokenType.Fnsave, 0x30dd },
        { TokenType.Frstor, 0x20dd },
        { TokenType.Fsave, 0x30dd9b },
        { TokenType.Fst, 0x10d9 },
        { TokenType.Fstp, 0x18d9 },
        { TokenType.Fstsw, 0x38dd9b },
        { TokenType.Fsub, 0x20d8 },
        { TokenType.Fsubr, 0x28d8 }
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<TokenType, int> s_ea64 = new Dictionary<TokenType, int>
    {
        { TokenType.Fadd, 0x00dc },
        { TokenType.Fcom, 0x10dc },
        { TokenType.Fcomp, 0x18dc },
        { TokenType.Fdiv, 0x30dc },
        { TokenType.Fdivr, 0x38dc },
        { TokenType.Fild, 0x28df },
        { TokenType.Fistp, 0x38df },
        { TokenType.Fld, 0x00dd },
        { TokenType.Fmul, 0x08dc },
        { TokenType.Fst, 0x10dd },
        { TokenType.Fstp, 0x18dd },
        { TokenType.Fsub, 0x20dc },
        { TokenType.Fsubr, 0x28dc },
    }.ToFrozenDictionary();
    
    private static readonly FrozenDictionary<TokenType, int> s_eaTenByte = new Dictionary<TokenType, int>
    {
        { TokenType.Fld, 0x28db },
        { TokenType.Fbld, 0x20df },
        { TokenType.Fbstp, 0x30df },
        { TokenType.Fstp, 0x38db }
    }.ToFrozenDictionary();
    
    private static readonly FrozenDictionary<TokenType, int> s_shifts = new Dictionary<TokenType, int>
    {
        { TokenType.Rcl,     0x10d0 }, 
        { TokenType.Rcr,     0x18d0 }, 
        { TokenType.Rol,     0x00d0 }, 
        { TokenType.Ror,     0x08d0 },
        { TokenType.Sal,     0x38d0 },
        { TokenType.Sar,     0x20d0 },
        { TokenType.ShlMnem, 0x20d0 },
        { TokenType.ShrMnem, 0x28d0 }
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<TokenType, int> s_regRegOrImm = new Dictionary<TokenType, int>
    {
        { TokenType.Adc, 0x10 },
        { TokenType.Add, 0x00 },
        { TokenType.And, 0x20 },
        { TokenType.Cmp, 0x38 },
        { TokenType.Fadd, 0xc0d8 },
        { TokenType.Faddp, 0xc0de },
        { TokenType.Fdiv, 0xf0d8 },
        { TokenType.Fdivp, 0xf8de },
        { TokenType.Fdivr, 0xf8d8 },
        { TokenType.Fdivrp, 0xf0de },
        { TokenType.Fmul, 0xc8d8 },
        { TokenType.Fmulp, 0xc8de },
        { TokenType.Fsub, 0xe0d8 },
        { TokenType.Fsubp, 0xe8de },
        { TokenType.Fsubr, 0xe8d8 },
        { TokenType.Fsubrp, 0xe0de },
        { TokenType.Mov, 0x88 },
        { TokenType.Or, 0x08 },
        { TokenType.Sbb, 0x18 },
        { TokenType.Sub, 0x28 },
        { TokenType.Test, 0x84 },
        { TokenType.Xor, 0x30 },
        { TokenType.Xchg, 0x90 }
    }.ToFrozenDictionary();

    private static readonly FrozenSet<TokenType> s_alus =
        [
            TokenType.Adc, 
            TokenType.Add, 
            TokenType.And, 
            TokenType.Cmp, 
            TokenType.Or, 
            TokenType.Sbb, 
            TokenType.Sub, 
            TokenType.Xor
        ];
    
    private static readonly FrozenDictionary<TokenType, int> s_aluMoveTestXchgEaReg = new Dictionary<TokenType, int>
    {
        { TokenType.Adc, 0x10 },
        { TokenType.Add, 0x00 },
        { TokenType.And, 0x20 },
        { TokenType.Cmp, 0x38 },
        { TokenType.Mov, 0x88 },
        { TokenType.Or, 0x08 },
        { TokenType.Sbb, 0x18 },
        { TokenType.Sub, 0x28 },
        { TokenType.Test, 0x84 },
        { TokenType.Xor, 0x30 },
        { TokenType.Xchg, 0x86 }
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<TokenType, int> s_leas = new Dictionary<TokenType, int>
    {
        { TokenType.Lds, 0xc5 },
        { TokenType.Lea, 0x8d },
        { TokenType.Les, 0xc4 }
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<TokenType, int> s_aluMovTestXchgEaImm = new Dictionary<TokenType, int>
    {
        { TokenType.Adc, 0x1080 },
        { TokenType.Add, 0x0080 },
        { TokenType.And, 0x2080 },
        { TokenType.Cmp, 0x3880 },
        { TokenType.Mov, 0x00c6 },
        { TokenType.Or, 0x0880 },
        { TokenType.Sub, 0x2880 },
        { TokenType.Test, 0x00f6 },
        { TokenType.Xor, 0x3080 }
    }.ToFrozenDictionary();
    
    private static readonly FrozenDictionary<TokenType, I86String> s_strings86 = new Dictionary<TokenType, I86String>
    {
        { TokenType.Cmps, new I86String(0xa6, [TokenType.Ds, TokenType.Si, TokenType.Es, TokenType.Di]) },
        { TokenType.Movs, new I86String(0xa4, [TokenType.Es, TokenType.Di, TokenType.Ds, TokenType.Si]) },
        { TokenType.Lods, new I86String(0xac, [TokenType.Al, TokenType.Ds, TokenType.Si]) },
        { TokenType.Scas, new I86String(0xae, [TokenType.Al, TokenType.Es, TokenType.Di]) },
        { TokenType.Stos, new I86String(0xaa, [TokenType.Es, TokenType.Di, TokenType.Al]) },
        { TokenType.Xlat, new I86String(0xd7, [TokenType.Ds, TokenType.Bx]) }
    }.ToFrozenDictionary();
    
    private static readonly FrozenDictionary<TokenType, int> s_implieds = new Dictionary<TokenType, int>
    {
        { TokenType.Cmpsb, 0xa6 },
        { TokenType.Cmpsw, 0xa7 },
        { TokenType.Daa, 0x27 },
        { TokenType.Das, 0x2f },
        { TokenType.Aas, 0x3f },
        { TokenType.Aaa, 0x37 },
        { TokenType.Cbw, 0x98 },
        { TokenType.Cwd, 0x99 },
        { TokenType.Lock, 0xf0 },
        { TokenType.Wait, 0x9b },
        { TokenType.Pushf, 0x9c },
        { TokenType.Popf, 0x9d },
        { TokenType.Sahf, 0x9e },
        { TokenType.Lahf, 0x9f },
        { TokenType.Ret, 0xc3 },
        { TokenType.Int3, 0xcc },
        { TokenType.Into, 0xce },
        { TokenType.Iret, 0xcf },
        { TokenType.Aam, 0x0ad4 },
        { TokenType.Aad, 0x0ad5 },
        { TokenType.Hlt, 0xf4 },
        { TokenType.Cmc, 0xf5 },
        { TokenType.Clc, 0xf8 },
        { TokenType.Stc, 0xf9 },
        { TokenType.Cli, 0xfa },
        { TokenType.Sti, 0xfb },
        { TokenType.Cld, 0xfc },
        { TokenType.Std, 0xfd },
        { TokenType.Insb, 0x6c },
        { TokenType.Insw, 0x6d },
        { TokenType.F2Xm1, 0xf0d9 },
        { TokenType.Fadd, 0xc1de },
        { TokenType.Faddp, 0xc1de },
        { TokenType.Fabs, 0xe1d9 },
        { TokenType.Fchs, 0xe0d9 },
        { TokenType.Fclex, 0xe2db9b},
        { TokenType.Fcom, 0xd1d8 },
        { TokenType.Fcomp, 0xd9d8 },
        { TokenType.Fcompp, 0xd9de },
        { TokenType.Fcos, 0xffd9 },
        { TokenType.Fdecstp, 0xf6d9 },
        { TokenType.Fdisi, 0xe1db9b },
        { TokenType.Fdiv, 0xf9de },
        { TokenType.Fdivp, 0xf9de },
        { TokenType.Feni, 0xe0db9b },
        { TokenType.Fincstp, 0xf7d9 },
        { TokenType.Finit, 0xe3db9b },
        { TokenType.Fld1, 0xe8d9 },
        { TokenType.Fldl2E, 0xead9 },
        { TokenType.Fldl2T, 0xe9d9 },
        { TokenType.Fldlg2, 0xecd9 },
        { TokenType.Fldln2, 0xedd9 },
        { TokenType.Fldpi, 0xebd9 },
        { TokenType.Fldz, 0xeed9 },
        { TokenType.Fmulp, 0xc9de },
        { TokenType.Fnclex, 0xe2db},
        { TokenType.Fndisi, 0xe1db },
        { TokenType.Fneni, 0xe0db },
        { TokenType.Fninit, 0xe3db },
        { TokenType.Fnop, 0xd0d9 },
        { TokenType.Fpatan, 0xf3d9 },
        { TokenType.Fprem, 0xf8d9 },
        { TokenType.Frndint, 0xfcd9 },
        { TokenType.Fptan, 0xf2d9 },
        { TokenType.Fscale, 0xfdd9 },
        { TokenType.Fsin, 0xfed9 },
        { TokenType.Fsqrt, 0xfad9 },
        { TokenType.Fstsw, 0xe0df9b },  
        { TokenType.Fnstsw, 0xe0df }, 
        { TokenType.Fsub, 0xe9de },
        { TokenType.Fsubp, 0xe9de },
        { TokenType.Ftst, 0xe4d9 },
        { TokenType.Fwait, 0x9b },
        { TokenType.Fxch, 0xc9d9 },
        { TokenType.Fxtract, 0xf4d9 },
        { TokenType.Fxam, 0xe5d9 },
        { TokenType.Fyl2X, 0xf1d9 },
        { TokenType.Fyl2Xp1, 0xf9d9 },
        { TokenType.Lodsb, 0xac },
        { TokenType.Lodsw, 0xad },
        { TokenType.Movsb, 0xa4 },
        { TokenType.Movsw, 0xa5 },
        { TokenType.Nop, 0x90 },
        { TokenType.Outsb, 0x6e },
        { TokenType.Outsw, 0x6f },
        { TokenType.Rep, 0xf3 },
        { TokenType.Repe, 0xf3 },
        { TokenType.Repne, 0xf2 },
        { TokenType.Repnz, 0xf2 },
        { TokenType.Repz, 0xf3 },
        { TokenType.Retf, 0xcb },
        { TokenType.Salc, 0xd6 },
        { TokenType.Scasb, 0xae },
        { TokenType.Scasw, 0xaf },
        { TokenType.Stosb, 0xaa },
        { TokenType.Stosw, 0xab },
        { TokenType.Xlat, 0xd7 },
        { TokenType.Xlatb, 0xd7 }
    }.ToFrozenDictionary();
    
    private static readonly FrozenDictionary<TokenType, (int, int)> s_singleRegOpcs = new Dictionary<TokenType, (int, int)>
    {
        { TokenType.Dec, (0xc8fe, 0x48) },   { TokenType.Div, (0xf0f6, 0xf0f7) }, { TokenType.Idiv, (0xf8f6, 0xf8f7) },  
        { TokenType.Imul, (0xe8f6, 0xe8f7) }, 
        { TokenType.Inc, (0xc0fe, 0x40) },   { TokenType.Mul, (0xe0f6, 0xe0f7) },   { TokenType.Neg, (0xd8f6, 0xd8f7) },   
        { TokenType.Not, (0xd0f6, 0xd0f7) }, { TokenType.Rcl, (0xd0d0, 0xd0d1) },   { TokenType.Rcr, (0xd8d0, 0xd8d1) },   
        { TokenType.Rol, (0xc0d0, 0xc0d1) }, { TokenType.Ror, (0xc8d0, 0xc8d1) },   { TokenType.Sal, (0xe0d0, 0xe0d1) },   
        { TokenType.Sar, (0xf8d0, 0xf8d1) }, { TokenType.ShlMnem, (0xe0d0, 0xe0d1) }, 
        { TokenType.ShrMnem, (0xe8d0, 0xe8d0) }
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<TokenType, int> s_8087RegOpcs = new Dictionary<TokenType, int>
    {
        { TokenType.Fcom, 0xd0d8 },
        { TokenType.Fcomp, 0xd8d8 },
        { TokenType.Ffree, 0xc0dd },
        { TokenType.Fld, 0xc0d9 },
        { TokenType.Fst, 0xd0dd },
        { TokenType.Fstp, 0xd8dd },
        { TokenType.Fxch, 0xc8d9 }
    }.ToFrozenDictionary();
    
    private static readonly FrozenDictionary<TokenType, int> s_singleRegOpcs16 = new Dictionary<TokenType, int>
    {
        { TokenType.Call, 0xd0ff }, 
        { TokenType.Jmp, 0xe0ff }, 
        { TokenType.Push, 0x50 }, 
        { TokenType.Pop, 0x58 },
    }.ToFrozenDictionary();
    
    private static readonly FrozenDictionary<(TokenType, TokenType), int> s_address = new Dictionary<(TokenType, TokenType), int>
    {
        { (TokenType.Int, TokenType.N8), 0xcd },
        { (TokenType.Ret, TokenType.N16), 0xc2 },
        { (TokenType.Retf, TokenType.N16), 0xca },
        { (TokenType.Aam, TokenType.N8), 0xd4 },
        { (TokenType.Aad, TokenType.N8), 0xd5 }
    }.ToFrozenDictionary();
    
    private static readonly FrozenDictionary<TokenType, (int, int)> s_immediateOps = new Dictionary<TokenType, (int, int)>
    {
        { TokenType.Adc, (0x14, 0xd080) },   { TokenType.Add, (0x04, 0xc080) },     { TokenType.And, (0x24, 0xe080) },
        { TokenType.Cmp, (0x3c, 0xf880) },   { TokenType.In, (0xe4, -1) },          { TokenType.Mov, (0xb0, 0xb0) },     
        { TokenType.Or,  (0x0c, 0xc880) },   { TokenType.Sbb, (0x1c, 0xd880) },     { TokenType.Sub, (0x2c, 0xe880) },    
        { TokenType.Test, (0xa8, 0xc0f6) },  { TokenType.Xor, (0x34, 0xf080) }
    }.ToFrozenDictionary();
    
    private static readonly FrozenDictionary<TokenType, int> s_8BitRegisters = new Dictionary<TokenType, int>
    {
        { TokenType.Al, 0b000 },
        { TokenType.Cl, 0b001 },
        { TokenType.Dl, 0b010 },
        { TokenType.Bl, 0b011 },
        { TokenType.Ah, 0b100 },
        { TokenType.Ch, 0b101 },
        { TokenType.Dh, 0b110 },
        { TokenType.Bh, 0b111 }
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<TokenType, int> s_16BitRegisters = new Dictionary<TokenType, int>
    {
        { TokenType.Ax, 0b000 },
        { TokenType.Cx, 0b001 },
        { TokenType.Dx, 0b010 },
        { TokenType.Bx, 0b011 },
        { TokenType.Sp, 0b100 },
        { TokenType.Bp, 0b101 },
        { TokenType.Si, 0b110 },
        { TokenType.Di, 0b111 }
    }.ToFrozenDictionary();

    private const int SegBase = 0x26;
    
    private static readonly FrozenDictionary<TokenType, int> s_segmentRegisters = new Dictionary<TokenType, int>
    {
        { TokenType.Es, SegBase + 0x00 },
        { TokenType.Cs, SegBase + 0x08 },
        { TokenType.Ss, SegBase + 0x10 },
        { TokenType.Ds, SegBase + 0x18 }
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<TokenType, int> s_8087StackRegisters = new Dictionary<TokenType, int>
    {
        { TokenType.St,     0b000 },
        { TokenType.St0Reg, 0b000 },
        { TokenType.St1Reg, 0b001 },
        { TokenType.St2Reg, 0b010 },
        { TokenType.St3Reg, 0b011 },
        { TokenType.St4Reg, 0b100 },
        { TokenType.St5Reg, 0b101 },
        { TokenType.St6Reg, 0b110 },
        { TokenType.St7Reg, 0b111 }
    }.ToFrozenDictionary();
    
    private static readonly FrozenSet<TokenType> s_reg16OpcodesFirst =
        [TokenType.Idiv, TokenType.Imul, TokenType.Lds, TokenType.Lea, TokenType.Les];

    
    private static readonly FrozenDictionary<TokenType, int> s_branching = new Dictionary<TokenType, int>
    {
        { TokenType.Call, 0xe8 },
        { TokenType.Jmp, 0xe9 },
        { TokenType.Jo, 0x70 },
        { TokenType.Jno, 0x71 },
        { TokenType.Jc, 0x72 },
        { TokenType.Jcxz, 0xe3 },
        { TokenType.Jb, 0x72 },
        { TokenType.Jnae, 0x72 },
        { TokenType.Jnc, 0x73 },
        { TokenType.Jnb, 0x73 },
        { TokenType.Jae, 0x73 },
        { TokenType.Je, 0x74 },
        { TokenType.Jz, 0x74 },
        { TokenType.Jne, 0x75 },
        { TokenType.Jnz, 0x75 },
        { TokenType.Jbe, 0x76 },
        { TokenType.Jna, 0x76 },
        { TokenType.Jnbe, 0x77 },
        { TokenType.Ja, 0x77 },
        { TokenType.Js, 0x78 },
        { TokenType.Jns, 0x79 },
        { TokenType.Jp, 0x7a },
        { TokenType.Jpe, 0x7a },
        { TokenType.Jnp, 0x7b },
        { TokenType.Jpo, 0x7b },
        { TokenType.Jl, 0x7c },
        { TokenType.Jnge, 0x7c },
        { TokenType.Jnl, 0x7d },
        { TokenType.Jge, 0x7d },
        { TokenType.Jle, 0x7e },
        { TokenType.Jng, 0x7e },
        { TokenType.Jnle, 0x7f },
        { TokenType.Jg, 0x7f },
        { TokenType.Loop, 0xe2 },
        { TokenType.Loope, 0xe1 },
        { TokenType.Loopz, 0xe1 },
        { TokenType.Loopne, 0xe0 },
        { TokenType.Loopnz, 0xe0 }
    }.ToFrozenDictionary();

    // ReSharper disable once InconsistentNaming
    private const int NA = -1;
    
    private const int SegIx = 0;

    private const int BasIx = 1;

    private const int IndIx = 2;

    private const int RegIx = 3;
    
    private static readonly FrozenDictionary<OperandType, int[]> s_baseIndexRegIndeces 
        = new Dictionary<OperandType, int[]>
    { 
                                                                        //   seg    base    index   reg
        { OperandType.IndirectLong,                                         [NA,    NA,     NA,     NA] },
        { OperandType.IndirectRegister86,                                   [NA,     0,     NA,     NA] },
        { OperandType.BaseDisplacement,                                     [NA,     0,     NA,     NA] },
        { OperandType.BaseIndex,                                            [NA,     0,      1,     NA] },
        { OperandType.BaseIndexDisplacement,                                [NA,     0,      1,     NA] },
        { OperandType.DirectRegister,                                       [NA,    NA,     NA,      0] },
        { OperandType.IndirectRegister86Register,                           [NA,     0,     NA,      1] },
        { OperandType.BaseDisplacementRegister,                             [NA,     0,     NA,      1] },
        { OperandType.BaseIndexRegister,                                    [NA,     0,      1,      2] },
        { OperandType.BaseIndexDisplacementRegister,                        [NA,     0,      1,      2] },
        { OperandType.DirectImm,                                            [NA,    NA,     NA,     NA] },
        { OperandType.IndirectRegister86Imm,                                [NA,     0,     NA,     NA] },
        { OperandType.BaseIndexImm,                                         [NA,     0,      1,     NA] },
        { OperandType.BaseDisplacementImm,                                  [NA,     0,     NA,     NA] },
        { OperandType.BaseIndexDisplacementImm,                             [NA,     0,      1,     NA] },
        { OperandType.RegisterDirect,                                       [NA,    NA,     NA,      0] },
        { OperandType.RegisterIndirectRegister86,                           [NA,     1,     NA,      0] },
        { OperandType.RegisterBaseDisplacement,                             [NA,     1,     NA,      0] },
        { OperandType.RegisterBaseIndex,                                    [NA,     1,      2,      0] },
        { OperandType.RegisterBaseIndexDisplacement,                        [NA,     1,      2,      0] },
        { OperandType.RegisterSegmentOverrideDirect,                        [ 1,    NA,     NA,      0] },
        { OperandType.RegisterSegmentOverrideRegister,                      [ 1,     2,     NA,      0] },
        { OperandType.RegisterSegmentOverrideBaseIndex,                     [ 1,     2,      3,      0] },
        { OperandType.RegisterSegmentOverrideBaseDisplacement,              [ 1,     2,     NA,      0] },  
        { OperandType.RegisterSegmentOverrideBaseIndexDisplacement,         [ 1,     2,      3,      0] }, 
        { OperandType.SegmentOverrideRegister,                              [ 0,     1,     NA,     NA] },
        { OperandType.SegmentOverrideDirect,                                [ 0,    NA,     NA,     NA] }, 
        { OperandType.SegmentOverrideBaseIndex,                             [ 0,     1,      2,     NA] },
        { OperandType.SegmentOverrideBaseDisplacement,                      [ 0,     1,     NA,     NA] },
        { OperandType.SegmentOverrideBaseIndexDisplacement,                 [ 0,     1,      2,     NA] }, 
        { OperandType.SegmentOverrideRegisterRegister,                      [ 0,     1,     NA,      2] },
        { OperandType.SegmentOverrideDirectRegister,                        [ 0,    NA,     NA,      1] },
        { OperandType.SegmentOverrideBaseIndexRegister,                     [ 0,     1,      2,      3] },
        { OperandType.SegmentOverrideBaseIndexDisplacementRegister,         [ 0,     1,      2,      3] },
        { OperandType.SegmentOverrideRegisterSegmentOverrideRegister,       [ 0,     1,      2,      3] },
        { OperandType.SegmentOverrideDirectImm,                             [ 0,    NA,     NA,     NA] },
        { OperandType.SegmentOverrideRegisterImm,                           [ 0,     1,     NA,     NA] },
        { OperandType.SegmentOverrideBaseIndexImm,                          [ 0,     1,      2,     NA] },
        { OperandType.SegmentOverrideBaseDisplacementImm,                   [ 0,     1,     NA,     NA] },
        { OperandType.SegmentOverrideBaseIndexDisplacementImm,              [ 0,     1,      2,     NA] }
    }.ToFrozenDictionary();
    
    private static readonly FrozenDictionary<OperandType, EncodeFn> s_encodeFns = new Dictionary<OperandType, EncodeFn>
    {
        { OperandType.Implied,                                          EncodeImplied },
        { OperandType.Address ,                                         EncodeAddress },
        { OperandType.Register,                                         EncodeRegister },
        { OperandType.RegisterRegister,                                 EncodeRegisterRegister },
        { OperandType.Immediate80,                                      EncodeImmediate },
        { OperandType.Indexed,                                          EncodeOut },
        { OperandType.IndirectRegister86,                               EncodeEffectiveAddress },
        { OperandType.IndirectLong,                                     EncodeEffectiveAddress },
        { OperandType.BaseDisplacement,                                 EncodeEffectiveAddress },
        { OperandType.BaseIndex,                                        EncodeEffectiveAddress },
        { OperandType.BaseIndexDisplacement,                            EncodeEffectiveAddress },
        { OperandType.SegmentOverrideRegister,                          EncodeEffectiveAddress },
        { OperandType.SegmentOverrideDirect,                            EncodeEffectiveAddress },
        { OperandType.SegmentOverrideBaseDisplacement,                  EncodeEffectiveAddress },
        { OperandType.SegmentOverrideBaseIndex,                         EncodeEffectiveAddress },
        { OperandType.SegmentOverrideBaseIndexDisplacement,             EncodeEffectiveAddress },
        { OperandType.IndirectRegister86Imm,                            EncodeEffectiveAddressImmediate },
        { OperandType.DirectImm,                                        EncodeEffectiveAddressImmediate },
        { OperandType.BaseDisplacementImm,                              EncodeEffectiveAddressImmediate },
        { OperandType.BaseIndexImm,                                     EncodeEffectiveAddressImmediate },
        { OperandType.BaseIndexDisplacementImm,                         EncodeEffectiveAddressImmediate },
        { OperandType.SegmentOverrideRegisterImm,                       EncodeEffectiveAddressImmediate },
        { OperandType.SegmentOverrideDirectImm,                         EncodeEffectiveAddressImmediate },
        { OperandType.SegmentOverrideBaseDisplacementImm,               EncodeEffectiveAddressImmediate },
        { OperandType.SegmentOverrideBaseIndexImm,                      EncodeEffectiveAddressImmediate },
        { OperandType.SegmentOverrideBaseIndexDisplacementImm,          EncodeEffectiveAddressImmediate },
        { OperandType.RegisterIndirectRegister86,                       EncodeEffectiveAddressRegister },
        { OperandType.RegisterDirect,                                   EncodeEffectiveAddressRegister },
        { OperandType.RegisterBaseDisplacement,                         EncodeEffectiveAddressRegister },
        { OperandType.RegisterBaseIndex,                                EncodeEffectiveAddressRegister },
        { OperandType.RegisterBaseIndexDisplacement,                    EncodeEffectiveAddressRegister },
        { OperandType.RegisterSegmentOverrideRegister,                  EncodeEffectiveAddressRegister },
        { OperandType.RegisterSegmentOverrideDirect,                    EncodeEffectiveAddressRegister },
        { OperandType.RegisterSegmentOverrideBaseDisplacement,          EncodeEffectiveAddressRegister },
        { OperandType.RegisterSegmentOverrideBaseIndex,                 EncodeEffectiveAddressRegister },
        { OperandType.RegisterSegmentOverrideBaseIndexDisplacement,     EncodeEffectiveAddressRegister },
        { OperandType.IndirectRegister86Register,                       EncodeEffectiveAddressRegister },
        { OperandType.DirectRegister,                                   EncodeEffectiveAddressRegister },
        { OperandType.BaseDisplacementRegister,                         EncodeEffectiveAddressRegister },
        { OperandType.BaseIndexRegister,                                EncodeEffectiveAddressRegister },
        { OperandType.BaseIndexDisplacementRegister,                    EncodeEffectiveAddressRegister },
        { OperandType.SegmentOverrideRegisterRegister,                  EncodeEffectiveAddressRegister },
        { OperandType.SegmentOverrideDirectRegister,                    EncodeEffectiveAddressRegister },
        { OperandType.SegmentOverrideBaseDisplacementRegister,          EncodeEffectiveAddressRegister },
        { OperandType.SegmentOverrideBaseIndexRegister,                 EncodeEffectiveAddressRegister },
        { OperandType.SegmentOverrideBaseIndexDisplacementRegister,     EncodeEffectiveAddressRegister },
        { OperandType.SegmentAbsoluteDirect,                            EncodeSegmentAbsoluteDirect },
        { OperandType.SegmentOverrideRegisterSegmentOverrideRegister,   EncodeStrings }
    }.ToFrozenDictionary();
}