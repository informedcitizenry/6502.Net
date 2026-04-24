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

using Sixty502DotNet.Shared.Eval;

namespace Sixty502DotNet.Shared.Arch;

public sealed class DecodedInstruction
{
    public DecodedInstruction
    (
        string disassemblyFormat, 
        int opcode = 0x00, 
        int size = 1, 
        bool isRelative = false, 
        bool is16BitRelative = false
    )
    {
        DisassemblyFormat = disassemblyFormat;
        Opcode = opcode;
        Size = size;
        Operands = [Size - opcode.Size()];
        IsRelative = isRelative;
        Is16BitRelative = IsRelative && is16BitRelative;
    }
    
    public DecodedInstruction
    (
        string disassemblyFormat, 
        int opcode, 
        params int[] operands
    )
        : this(disassemblyFormat, opcode, false, operands)
    {
    }
    
    public DecodedInstruction
    (
        string disassemblyFormat, 
        int opcode, 
        bool isRelative, 
        params int[] operands
    )
    {
        DisassemblyFormat = disassemblyFormat;
        Opcode = opcode;
        Operands = operands;
        Size = opcode.Size() + operands.Sum();
        IsRelative = isRelative;
        Is16BitRelative = false;
    }
    
    public int Opcode { get; }

    public int Size { get; }
    
    public int[] Operands { get; }

    public string DisassemblyFormat { get; }

    public bool IsRelative { get; }

    public bool Is16BitRelative { get; }
}

public class Decoded86
{
    public Decoded86(string mnemonic)
    {
        Mnemonic = mnemonic;
        Op1 = Operand86.None;
        Op2 = Operand86.None;
        Group = Opcode86Group.None;
    }

    public Decoded86(string mnemonic, Operand86 op)
    {
        Mnemonic = mnemonic;
        Op1 = op;
        Op2 = Operand86.None;
        Group = Opcode86Group.None;
    }

    public Decoded86(string mnemonic, Operand86 op1, Operand86 op2)
    {
        Mnemonic = mnemonic;
        Op1 = op1;
        Op2 = op2;
        Group = Opcode86Group.None;
    }

    public Decoded86(Opcode86Group group)
    {
        Mnemonic = string.Empty;
        Group = group;
        Op1 = Operand86.None;
        Op2 = Operand86.None;
    }
    
    public Decoded86(Opcode86Group group, Operand86 op)
    {
        Mnemonic = string.Empty;
        Group = group;
        Op1 = op;
        Op2 = Operand86.None;
    }

    public Decoded86(Opcode86Group group, Operand86 op1, Operand86 op2)
    {
        Mnemonic = string.Empty;
        Group = group;
        Op1 = op1;
        Op2 = op2;
    }
    
    public string Mnemonic { get; }
    
    public Opcode86Group Group { get; }
    
    public Operand86 Op1 { get; }
    
    public Operand86 Op2 { get; }
}

public enum Operand86
{
    None,_1,Ap,Eb,Ev,Ew,Ed,Eq,Et,Gb,Gv,I0,Ib,Iv,Iw,Jv,Jb,Mp,M,
    Ob,Ov,Sw,Es,Ss,Ds,Cs,Al,Cl,Dl,Bl,Ah,Ch,Dh,Bh,Ax,Bx,Cx,Dx,
    Sp,Bp,Si,Di,St,St0,Rep
}

public enum Opcode86Group
{
    None,
    Group1,
    Group2,
    Group3A,
    Group3B,
    Group4,
    Group5,
    Group8087,
    Group8087Ext
}