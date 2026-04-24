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
using System.Text;
using System.Collections.Frozen;

namespace Sixty502DotNet.Shared.Encode;

public static partial class I86Encoder
{
    public static string Decode(ReadOnlySpan<byte> bytes, ref int programCounter)
    {
        var sb = new StringBuilder();
        var startPc = programCounter;
        var index = 0;
        string? segment = null;
        while (bytes[index] == 0x66 || bytes[index] == 0x67)
        {
            // skip 32-bit prefix stuff
            index++;
            programCounter++;
        }
        if (bytes.Length > 1 && s_segments.TryGetValue(bytes[0], out segment))
        {
            index++;
            programCounter++;
        }
        var opcPosition = index;
        var opcHex = bytes[opcPosition];
        try
        {
            var decoded = s_decoded[opcHex];
            if (decoded == null ||
                (!string.IsNullOrEmpty(segment) && decoded.Op1 is Operand86.Jb or Operand86.Jv))
            {
                programCounter++;
                return $".byte 0x{bytes[0]:x2}";
            }
            var op1 = decoded.Op1;
            var op2 = decoded.Op2;
            var decodedGroup = decoded.Group;
            switch (decoded.Group)
            {
                case Opcode86Group.None:
                    sb.Append(decoded.Mnemonic);
                    break;
                case Opcode86Group.Group8087Ext:
                {
                    if (bytes.Length < 3 || 
                        !s_decoded8087.TryGetValue(bytes[++index], out var extGroup) ||
                        !extGroup.TryGetValue(bytes[++index], out decoded))
                    {
                        sb.Append("wait");
                        break;
                    }
                    sb.Append(decoded.Mnemonic);
                    op1 = decoded.Op1;
                    op2 = decoded.Op2;
                    break;
                }
                case Opcode86Group.Group8087:
                {
                    var group8087 = s_decoded8087[bytes[index++]];
                    var selector = bytes[index];
                    if (!group8087.TryGetValue(selector, out decoded))
                    {
                        if (selector >= Mode.Reg2Reg)
                        {
                            selector &= Mode.RmMask; // mask rm
                        }
                        else
                        {
                            selector &= Mode.RegField; // mask mod
                        }
                        decoded = group8087[selector];
                    }
                    sb.Append(decoded.Mnemonic);
                    op1 = decoded.Op1;
                    op2 = decoded.Op2;
                    break;
                }
                default:
                {
                    var opcVal = (bytes[opcPosition + 1] >> 3) & 7;
                    if (opcVal == 0 && decoded.Group is Opcode86Group.Group3A or Opcode86Group.Group3B)
                    {
                        op1 = decoded.Group == Opcode86Group.Group3A ? Operand86.Eb : Operand86.Ev;
                        op2 = decoded.Group == Opcode86Group.Group3A ? Operand86.Ib : Operand86.Iv;
                    }
                    var mnemonic = s_groups[decoded.Group][opcVal];
                    if (string.IsNullOrEmpty(mnemonic))
                    {
                        programCounter++;
                        return $".byte 0x{bytes[0]:x2}";
                    }
                    sb.Append(mnemonic);
                    if (decoded.Group == Opcode86Group.Group5 && opcVal is 3 or 5)
                    {
                        op1 = Operand86.Ed; // far call and jmp through EA
                    }
                    break;
                }
            }
            var instructionSize = GetInstructionSize(bytes, opcPosition, decodedGroup, op1, op2);
            if (op1 == Operand86.None)
            {
                if (s_strings86Formats.TryGetValue(bytes[0], out var strFormat))
                {
                    sb.Append(strFormat);
                }
                programCounter += instructionSize;
                return sb.ToString();
            }
            sb.Append(' ');
            switch (op1)
            {
                case Operand86.Ah:
                case Operand86.Al:
                case Operand86.Ax:
                case Operand86.Bl:
                case Operand86.Bh:
                case Operand86.Bp:
                case Operand86.Bx:
                case Operand86.Ch:
                case Operand86.Cl:
                case Operand86.Cs:
                case Operand86.Cx:
                case Operand86.Dh:
                case Operand86.Di:
                case Operand86.Dl:
                case Operand86.Ds:
                case Operand86.Dx:
                case Operand86.Es: 
                case Operand86.Si:
                case Operand86.Sp:
                case Operand86.Ss:
                    sb.Append(op1.ToString().ToLower());
                    break;
                case Operand86.I0:
                {    
                    var imm = bytes[++opcPosition];
                    if (imm != 0xa)
                    {
                        sb.Append($"0x{imm:x}");
                    }
                    break;
                }
                case Operand86.Ib:
                {
                    var byt = bytes[++index];
                    sb.Append($"0x{byt:x}");
                    break;
                }
                case Operand86.Iw:
                {
                    var word = bytes[++index] + bytes[++index] * 256;
                    sb.Append($"0x{word:x}");
                    break;
                }
                case Operand86.Jv:
                {
                    var offs = bytes[++index] + bytes[++index] * 256;
                    if (offs > 32767) offs -= 65536;
                    var label = programCounter + index + offs + 1;
                    sb.Append($"0x{label:x}");
                    break;
                }
                case Operand86.Jb:
                {
                    var offs = (int)bytes[++index];
                    if (offs > 127) offs -= 256;
                    var label = programCounter + index + offs + 1;
                    sb.Append($"0x{label:x}");
                    break;
                }
                case Operand86.Eb:
                case Operand86.Ev:
                case Operand86.Ew:
                {
                    index++;
                    if (bytes[opcPosition + 1] < 0xc0)
                    {
                        sb.Append(op1 == Operand86.Eb ? "BYTE" : "WORD");
                        sb.Append(" PTR ");
                    }
                    index += DecodeEbEvEw(bytes, sb, segment, op1, opcPosition, 0);
                    break;
                }
                case Operand86.Ed:
                {
                    sb.Append("DWORD PTR ");
                    index += DecodeEbEvEw(bytes, sb, segment, op1, opcPosition, 0);
                    break;
                }
                case Operand86.Eq:
                    sb.Append("QWORD PTR ");
                    DecodeEbEvEw(bytes, sb, segment, op1, opcPosition, 0);
                    break;
                case Operand86.Et:
                    sb.Append("TBYTE PTR ");
                    DecodeEbEvEw(bytes, sb, segment, op1, opcPosition, 0);
                    break;
                case Operand86.Gb:
                case Operand86.Gv:
                    DecodeGbGv(bytes, sb, opcPosition, op1);
                    break;
                case Operand86.Ap:
                {
                    var address = bytes[opcPosition + 1] + 
                                  bytes[opcPosition + 2] * 0x100 +
                                  bytes[opcPosition + 3] * 0x10000 + 
                                  bytes[opcPosition + 4] * 0x1000000;
                    var segAbsolute = bytes[opcPosition + 5] + bytes[opcPosition + 6] * 256;
                    sb.Append($"0x{segAbsolute:x}:0x{address:x}");
                    break;
                }
                case Operand86.M:
                    if (!DecodeM(bytes, sb, ref opcPosition))
                    {
                        programCounter = startPc + 1;
                        return $".byte 0x{bytes[0]:x}";
                    }
                    break;
                case Operand86.Ob:
                case Operand86.Ov:
                {
                    DecodeObOv(bytes, sb, segment, op1, opcPosition);
                    break;
                }
                case Operand86.Rep:
                {
                    if (bytes.Length > 1 &&
                        s_strings86Formats.TryGetValue(bytes[opcPosition + 1], out var stringOpc))
                    {
                        sb.Append(s_decoded[bytes[opcPosition + 1]]?.Mnemonic ?? string.Empty);
                        sb.Append(stringOpc);
                    }
                    break;
                }
                case Operand86.St:
                {
                    if (op2 == Operand86.None || (bytes[opcPosition] & 0x0c) == 0xc)
                    {
                        var regByte = bytes[opcPosition + 1] & 7;
                        var reg = s_regsStack[regByte];
                        sb.Append(reg);
                    }
                    else
                    {
                        sb.Append("st");
                    }
                    break;
                }
                case Operand86.St0:
                    sb.Append("st0");
                    break;
                case Operand86.Sw:
                    DecodeSw(bytes, sb, opcPosition);
                    break;
            }
            if (op2 == Operand86.None)
            {
                programCounter += instructionSize;
                return sb.ToString();
            }
            sb.Append(',');
            switch (op2)
            {
                case Operand86._1:
                    sb.Append('1');
                    break;
                case Operand86.Ah:
                case Operand86.Al:
                case Operand86.Ax:
                case Operand86.Bh:
                case Operand86.Bl:
                case Operand86.Bx:
                case Operand86.Ch:
                case Operand86.Cl:
                case Operand86.Cs:
                case Operand86.Cx:
                case Operand86.Dh:
                case Operand86.Di:
                case Operand86.Dl:
                case Operand86.Ds:
                case Operand86.Dx:
                case Operand86.Es:
                case Operand86.Ss:
                    sb.Append(op2.ToString().ToLower());
                    break;
                case Operand86.Eb:
                case Operand86.Ev:
                case Operand86.Ew:
                    if (bytes[opcPosition + 1] < 0xc0)
                    {
                        sb.Append(op2 == Operand86.Eb ? "BYTE" : "WORD");
                        sb.Append(" PTR ");
                    }
                    DecodeEbEvEw(bytes, sb, segment, op2, opcPosition, 1);
                    break;
                case Operand86.Gb:
                case Operand86.Gv:
                    DecodeGbGv(bytes, sb, opcPosition, op2);
                    break;
                case Operand86.Ib:
                case Operand86.Iv:
                {
                    var operand = (int)bytes[++index];
                    if (op2 == Operand86.Iv)
                    {
                        operand += bytes[++index] * 256;
                    }
                    sb.Append($"0x{operand:x}");
                    break;
                }
                case Operand86.M:
                case Operand86.Mp:
                    if (Operand86.Mp == op2)
                    {
                        sb.Append(" DWORD PTR ");
                    }
                    if (!DecodeM(bytes, sb, ref opcPosition))
                    {
                        programCounter = startPc + 1;
                        return $".byte 0x{bytes[0]:x}";
                    }
                    break;
                case Operand86.Ob:
                case Operand86.Ov:
                {
                    DecodeObOv(bytes, sb, segment, op2, opcPosition);
                    break;
                }
                case Operand86.St:
                {
                    if ((bytes[opcPosition] & 0x0c) == 0x8)
                    {
                        var regByte = bytes[opcPosition + 1] & 7;
                        var reg = s_regsStack[regByte];
                        sb.Append(reg);
                    }
                    else
                    {
                        sb.Append("st");
                    }
                    break;
                }
                case Operand86.Sw:
                    DecodeSw(bytes, sb, opcPosition);
                    break;
            }
            programCounter += instructionSize;
            return sb.ToString();
        }
        catch
        {
            programCounter = startPc + 1;
            return $".byte 0x{bytes[0]:x2}";
        }
    }

    private static void DecodeSw(ReadOnlySpan<byte> bytes, StringBuilder sb, int opcPosition)
    {
        var mod = bytes[opcPosition + 1];
        var reg = mod & 0x18;
        var regStr = s_segFormats[reg];
        sb.Append(regStr);
    }
    
    private static int DecodeEbEvEw
    (
        ReadOnlySpan<byte> bytes, 
        StringBuilder sb,
        string? segment,
        Operand86 type,
        int opcPosition, 
        int position
    )
    {
        var mod = bytes[opcPosition + 1];
        if (!string.IsNullOrEmpty(segment))
        {
            sb.Append($"{segment}:");
        }
        if ((mod & 0xc0) == 0xc0)
        {
            var reg = position == 0 || type == Operand86.Ew ? mod & 7 : (mod & 0b111000) >> 3;
            var regStr = type is Operand86.Eb ? s_regs8[reg] : s_regs16[reg];
            sb.Append(regStr);
            return 0;
        }
        var modFmt = s_modRmFormats[mod & 0xc7];
        if (mod < 0x40 && (mod & 0x7) != 0x6)
        {
            sb.Append(modFmt);
            return 0;
        }
        var size = 1;
        var displ = (int)bytes[opcPosition + 2];
        if ((mod & 0b10_000_000) == 0x80 || (mod & 0b01_000_111) == 0x6)
        {
            size++;
            displ += bytes[opcPosition + 3] * 256;
        }
        if ((size == 1 && displ > sbyte.MaxValue) || (size == 2 && displ > short.MaxValue))
        {
            // represent e.g. [bp+0xfc] as [bp-0x4]
            var plInx = modFmt.LastIndexOf('+');
            if (plInx > 0)
            {
                var modFmtSb = new StringBuilder(modFmt)
                {
                    [plInx] = '-'
                };
                modFmt = modFmtSb.ToString();
                if (size == 1)
                {
                    displ = 256 - displ;
                }
                else
                {
                    displ = 65536 - displ;
                }
            }
        }
        sb.AppendFormat(modFmt, displ);
        return size;
    }

    private static void DecodeGbGv
    (
        ReadOnlySpan<byte> bytes, 
        StringBuilder sb, 
        int opcIndex, 
        Operand86 operand
    )
    {
        var reg = (bytes[opcIndex + 1] & 0b111000) >> 3;
        sb.Append(operand == Operand86.Gb ? s_regs8[reg] : s_regs16[reg]);
    }

    private static bool DecodeM
    (
        ReadOnlySpan<byte> bytes,
        StringBuilder sb,
        ref int opcIndex
    )
    {
        var mod = bytes[++opcIndex];
        if (mod >= 0xc0)
        {
            return false;
        }
        var modFmt = s_modRmFormats[mod & 0xc7];
        if (mod < 0x40 && (mod & 0x7) != 0x6)
        {
            sb.Append(modFmt);
            return true;
        }
        var displ = (int)bytes[++opcIndex];
        if ((mod & 0b10_000_000) == 0x80 || (mod & 0b01_000_111) == 0x6)
        {
            displ += bytes[++opcIndex] * 256;
            if (displ > short.MaxValue)
            {
                displ = 65536 - displ;
                modFmt = modFmt.Replace('+', '-');
            }
        }
        else if (displ > sbyte.MaxValue)
        {
            displ = 256 - displ;
            modFmt = modFmt.Replace('+', '-');
        }
        sb.AppendFormat(modFmt, displ);
        return true;
    }

    private static void DecodeObOv
    (
        ReadOnlySpan<byte> bytes, 
        StringBuilder sb, 
        string? segment,
        Operand86 op, 
        int opcPosition
    )
    {
        var addr =  bytes[opcPosition + 1] +  bytes[opcPosition + 2] * 256;
        sb.Append(op == Operand86.Ob ? "BYTE" : "WORD");
        sb.Append(" PTR ");
        if (!string.IsNullOrEmpty(segment))
        {
            sb.Append($"{segment}:");
        }
        sb.Append($"[0x{addr:x}]");
    }
    
    private static int GetInstructionSize
    (
        ReadOnlySpan<byte> bytes, 
        int opcPosition, 
        Opcode86Group group, 
        Operand86 op1, 
        Operand86 op2
    )
    {
        var size = 1;
        if (group != Opcode86Group.None)
        {
            size++;
            if (group == Opcode86Group.Group8087Ext)
            {
                size++;
            }
        }
        if (s_usesModRm.Contains(op1) || s_usesModRm.Contains(op2))
        {
            var modRm = bytes[opcPosition + 1];
            if (group == Opcode86Group.None)
            {
                size++;
            }
            else
            {
                modRm &= Mode.RegMask;
            }
            if ((modRm & Mode.Reg2Reg) != Mode.Reg2Reg && modRm is >= Mode.Displ8 or Mode.IndAddr)  
            {
                size++;
                if ((modRm & Mode.Displ16) == Mode.Displ16 || (modRm & Mode.RegMask) == Mode.IndAddr)
                {
                    size++;
                }
            }
        } 
        switch (op1)
        {
            case Operand86.Ib:
            case Operand86.Jb:
                size++;
                break;
            case Operand86.Iv:
            case Operand86.Jv:
            case Operand86.Ob:
            case Operand86.Ov:
                size += 2;
                break;
            case Operand86.Ap:
                size += 6;
                break;
            case Operand86.Rep:
                if (bytes.Length > 1 && s_strings86Formats.ContainsKey(bytes[opcPosition + 1]))
                {
                    size++;
                }
                break;
        }
        switch (op2)
        {
            case Operand86.Ib:
                size++;
                break;
            case Operand86.Iv:
            case Operand86.Ob:
            case Operand86.Ov:
                size += 2;
                break;
        }
        return size;
    }
    
    private static readonly Decoded86?[] s_decoded =
    [
        new("add", Operand86.Eb, Operand86.Gb),
        new("add", Operand86.Ev, Operand86.Gv),
        new("add", Operand86.Gb, Operand86.Eb),
        new("add", Operand86.Gv, Operand86.Ev),
        new("add", Operand86.Al, Operand86.Ib),
        new("add", Operand86.Ax, Operand86.Iv),
        new("push", Operand86.Es),
        new("pop", Operand86.Es),
        
        new("or", Operand86.Eb, Operand86.Gb),
        new("or", Operand86.Ev, Operand86.Gv),
        new("or", Operand86.Gb, Operand86.Eb),
        new("or", Operand86.Gv, Operand86.Ev),
        new("or", Operand86.Al, Operand86.Ib),
        new("or", Operand86.Ax, Operand86.Iv),
        new("push", Operand86.Cs),
        null, 
        
        new("adc", Operand86.Eb, Operand86.Gb),
        new("adc", Operand86.Ev, Operand86.Gv),
        new("adc", Operand86.Gb, Operand86.Eb),
        new("adc", Operand86.Gv, Operand86.Ev),
        new("adc", Operand86.Al, Operand86.Ib),
        new("adc", Operand86.Ax, Operand86.Iv),
        new("push", Operand86.Ss),
        new("pop", Operand86.Ss),
        
        new("sbb", Operand86.Eb, Operand86.Gb),
        new("sbb", Operand86.Ev, Operand86.Gv),
        new("sbb", Operand86.Gb, Operand86.Eb),
        new("sbb", Operand86.Gv, Operand86.Ev),
        new("sbb", Operand86.Al, Operand86.Ib),
        new("sbb", Operand86.Ax, Operand86.Iv),
        new("push", Operand86.Ds),
        new("pop", Operand86.Ds),
        
        new("and", Operand86.Eb, Operand86.Gb),
        new("and", Operand86.Ev, Operand86.Gv),
        new("and", Operand86.Gb, Operand86.Eb),
        new("and", Operand86.Gv, Operand86.Ev),
        new("and", Operand86.Al, Operand86.Ib),
        new("and", Operand86.Ax, Operand86.Iv),
        null,
        new("daa"),
        
        new("sub", Operand86.Eb, Operand86.Gb),
        new("sub", Operand86.Ev, Operand86.Gv),
        new("sub", Operand86.Gb, Operand86.Eb),
        new("sub", Operand86.Gv, Operand86.Ev),
        new("sub", Operand86.Al, Operand86.Ib),
        new("sub", Operand86.Ax, Operand86.Iv),
        null,
        new("das"),
        
        new("xor", Operand86.Eb, Operand86.Gb),
        new("xor", Operand86.Ev, Operand86.Gv),
        new("xor", Operand86.Gb, Operand86.Eb),
        new("xor", Operand86.Gv, Operand86.Ev),
        new("xor", Operand86.Al, Operand86.Ib),
        new("xor", Operand86.Ax, Operand86.Iv),
        null,
        new("aaa"),
        
        new("cmp", Operand86.Eb, Operand86.Gb),
        new("cmp", Operand86.Ev, Operand86.Gv),
        new("cmp", Operand86.Gb, Operand86.Eb),
        new("cmp", Operand86.Gv, Operand86.Ev),
        new("cmp", Operand86.Al, Operand86.Ib),
        new("cmp", Operand86.Ax, Operand86.Iv),
        null,
        new("aas"),
        
        new("inc", Operand86.Ax),
        new("inc", Operand86.Cx),
        new("inc", Operand86.Dx),
        new("inc", Operand86.Bx),
        new("inc", Operand86.Sp),
        new("inc", Operand86.Bp),
        new("inc", Operand86.Si),
        new("inc", Operand86.Di),
        
        new("dec", Operand86.Ax),
        new("dec", Operand86.Cx),
        new("dec", Operand86.Dx),
        new("dec", Operand86.Bx),
        new("dec", Operand86.Sp),
        new("dec", Operand86.Bp),
        new("dec", Operand86.Si),
        new("dec", Operand86.Di),
        
        new("push", Operand86.Ax),
        new("push", Operand86.Cx),
        new("push", Operand86.Dx),
        new("push", Operand86.Bx),
        new("push", Operand86.Sp),
        new("push", Operand86.Bp),
        new("push", Operand86.Si),
        new("push", Operand86.Di),
        
        new("pop", Operand86.Ax),
        new("pop", Operand86.Cx),
        new("pop", Operand86.Dx),
        new("pop", Operand86.Bx),
        new("pop", Operand86.Sp),
        new("pop", Operand86.Bp),
        new("pop", Operand86.Si),
        new("pop", Operand86.Di),
        
        null, null, null, null, null, null, null, null,
        null, null, null, null, null, null, null, null,
        
        new("jo", Operand86.Jb),
        new("jno", Operand86.Jb),
        new("jb", Operand86.Jb),
        new("jnb", Operand86.Jb),
        new("jz", Operand86.Jb),
        new("jnz", Operand86.Jb),
        new("jbe", Operand86.Jb),
        new("ja", Operand86.Jb),
        
        new("js", Operand86.Jb),
        new("jns", Operand86.Jb),
        new("jpe", Operand86.Jb),
        new("jpo", Operand86.Jb),
        new("jl", Operand86.Jb),
        new("jge", Operand86.Jb),
        new("jle", Operand86.Jb),
        new("jg", Operand86.Jb),
        
        new(Opcode86Group.Group1, Operand86.Eb, Operand86.Ib),
        new(Opcode86Group.Group1,  Operand86.Ev, Operand86.Iv),
        new(Opcode86Group.Group1,  Operand86.Eb, Operand86.Ib),
        new(Opcode86Group.Group1,  Operand86.Ev, Operand86.Ib),
        new("test", Operand86.Eb, Operand86.Gb),
        new("test", Operand86.Ev, Operand86.Gv),
        new("xchg", Operand86.Eb, Operand86.Gb),
        new("xchg", Operand86.Ev, Operand86.Gv), 
        
        new("mov", Operand86.Eb, Operand86.Gb),
        new("mov", Operand86.Ev, Operand86.Gv),
        new("mov", Operand86.Gb, Operand86.Eb),
        new("mov", Operand86.Gv, Operand86.Ev),
        new("mov", Operand86.Ew, Operand86.Sw),
        new("lea", Operand86.Gv, Operand86.M),
        new("mov", Operand86.Sw, Operand86.Ew),
        new("pop", Operand86.Ev),
        
        new("nop"),
        new("xchg", Operand86.Cx, Operand86.Ax),
        new("xchg", Operand86.Dx, Operand86.Ax),
        new("xchg", Operand86.Bx, Operand86.Ax),
        new("xchg", Operand86.Sp, Operand86.Ax),
        new("xchg", Operand86.Bp, Operand86.Ax),
        new("xchg", Operand86.Si, Operand86.Ax),
        new("xchg", Operand86.Di, Operand86.Ax),
        
        new("cbw"),
        new("cwd"),
        new("call", Operand86.Ap),
        new(Opcode86Group.Group8087Ext),
        new("pushf"),
        new("popf"),
        new("sahf"),
        new("lahf"),
        
        new("mov", Operand86.Al, Operand86.Ob),
        new("mov", Operand86.Ax, Operand86.Ov),
        new("mov", Operand86.Ob, Operand86.Al),
        new("mov", Operand86.Ov, Operand86.Ax),
        new("movs"),
        new("movs"),
        new("cmps"),
        new("cmps"),
        
        new("test", Operand86.Al, Operand86.Ib),
        new("test", Operand86.Ax, Operand86.Iv),
        new("stos"),
        new("stos"),
        new("lods"),
        new("lods"),
        new("scas"),
        new("scas"),
        
        new("mov", Operand86.Al, Operand86.Ib),
        new("mov", Operand86.Cl, Operand86.Ib),
        new("mov", Operand86.Dl, Operand86.Ib),
        new("mov", Operand86.Bl, Operand86.Ib),
        new("mov", Operand86.Ah, Operand86.Ib),
        new("mov", Operand86.Ch, Operand86.Ib),
        new("mov", Operand86.Dh, Operand86.Ib),
        new("mov", Operand86.Bh, Operand86.Ib),
        
        new("mov", Operand86.Ax, Operand86.Iv),
        new("mov", Operand86.Cx, Operand86.Iv),
        new("mov", Operand86.Dx, Operand86.Iv),
        new("mov", Operand86.Bx, Operand86.Iv),
        new("mov", Operand86.Sp, Operand86.Iv),
        new("mov", Operand86.Bp, Operand86.Iv),
        new("mov", Operand86.Si, Operand86.Iv),
        new("mov", Operand86.Di, Operand86.Iv),
        
        null,
        null,
        new("ret", Operand86.Iw),
        new("ret"),
        new("les", Operand86.Gv, Operand86.Mp),
        new("lds", Operand86.Gv, Operand86.Mp),
        new("mov", Operand86.Eb, Operand86.Ib),
        new("mov", Operand86.Ev, Operand86.Iv),
        
        null,
        null,
        new("retf", Operand86.Iw),
        new("retf"),  
        new("int3"),
        new("int", Operand86.Ib),
        new("into"),
        new("iret"),
        
        new(Opcode86Group.Group2, Operand86.Eb, Operand86._1),
        new(Opcode86Group.Group2, Operand86.Ev, Operand86._1),
        new(Opcode86Group.Group2, Operand86.Eb, Operand86.Cl),
        new(Opcode86Group.Group2, Operand86.Ev, Operand86.Cl),
        new("aam", Operand86.I0),
        new("aad", Operand86.I0),
        new("salc"),
        new("xlat"),
        
        new(Opcode86Group.Group8087), 
        new(Opcode86Group.Group8087), 
        new(Opcode86Group.Group8087), 
        new(Opcode86Group.Group8087), 
        new(Opcode86Group.Group8087), 
        new(Opcode86Group.Group8087), 
        new(Opcode86Group.Group8087), 
        new(Opcode86Group.Group8087),
        
        new("loopnz", Operand86.Jb),
        new("loopz", Operand86.Jb),
        new("loop", Operand86.Jb),
        new("jcxz", Operand86.Jb),
        new("in", Operand86.Al, Operand86.Ib),
        new("in", Operand86.Ax, Operand86.Ib),
        new("out", Operand86.Ib, Operand86.Al),
        new("out", Operand86.Ib, Operand86.Ax),
        
        new("call", Operand86.Jv),
        new("jmp", Operand86.Jv),
        new("jmp", Operand86.Ap),
        new("jmp", Operand86.Jb),
        new("in", Operand86.Al,Operand86.Dx),
        new("in", Operand86.Ax, Operand86.Dx),
        new("out", Operand86.Dx, Operand86.Al),
        new("out", Operand86.Dx, Operand86.Ax),
        
        new("lock"),
        null,
        new("repnz", Operand86.Rep),
        new("repz", Operand86.Rep),
        new("hlt"),
        new("cmc"),
        new(Opcode86Group.Group3A, Operand86.Eb),
        new(Opcode86Group.Group3B, Operand86.Ev),
        
        new("clc"),
        new("stc"),
        new("cli"),
        new("sti"),
        new("cld"),
        new("std"),
        new(Opcode86Group.Group4, Operand86.Eb),
        new(Opcode86Group.Group5, Operand86.Ev)
    ];

    private static readonly FrozenDictionary<int, Dictionary<int, Decoded86>> s_decoded8087 
        = new Dictionary<int, Dictionary<int, Decoded86>>()
    {
        {
            0xd8, new Dictionary<int, Decoded86>
            {
                { 0x00, new Decoded86("fadd", Operand86.Ed ) },
                { 0x08, new Decoded86("fmul", Operand86.Ed) },
                { 0x10, new Decoded86("fcom", Operand86.Ed ) },
                { 0x18, new Decoded86("fcomp", Operand86.Ed) },
                { 0x20, new Decoded86("fsub",  Operand86.Ed ) },
                { 0x28, new Decoded86("fsubr",  Operand86.Ed ) },
                { 0x30, new Decoded86("fdiv", Operand86.Ed) },
                { 0x38, new Decoded86("fdivr", Operand86.Ed) },
                { 0xc0, new Decoded86("fadd", Operand86.St, Operand86.St) },
                { 0xc8, new Decoded86("fmul", Operand86.St, Operand86.St) },
                { 0xd0, new Decoded86("fcom", Operand86.St) },
                { 0xd8, new Decoded86("fcomp", Operand86.St, Operand86.St) },
                { 0xe0, new Decoded86("fsub", Operand86.St, Operand86.St) },
                { 0xe8, new Decoded86("fsubr", Operand86.St, Operand86.St) },
                { 0xf8, new Decoded86("fdivr", Operand86.St, Operand86.St) },
                { 0xf0, new Decoded86("fdiv", Operand86.St, Operand86.St) }
            }
        },
        {
            0xd9, new Dictionary<int, Decoded86>
            {
                { 0xf0, new Decoded86("f2xm1") },
                { 0xe1, new Decoded86("fabs") },
                { 0xe0, new Decoded86("fchs") },
                { 0xff, new Decoded86("fcos") },
                { 0xf6, new Decoded86("fdecstp") },
                { 0xf7, new Decoded86("fincstp") },
                { 0x00, new Decoded86("fld", Operand86.Ed) },
                { 0xc0, new Decoded86("fld", Operand86.St) },
                { 0xe8, new Decoded86("fld1") },
                { 0xea, new Decoded86("fld2e") },
                { 0xe9, new Decoded86("fld2t") },
                { 0x28, new Decoded86("fldcw",  Operand86.Ew) },
                { 0x20, new Decoded86("fldenv", Operand86.M) },
                { 0xed, new Decoded86("fldln2") },
                { 0xeb, new Decoded86("fldpi") },
                { 0xee, new Decoded86("fldz") },
                { 0xd0, new Decoded86("fnop") },
                { 0xf3, new Decoded86("fpatan") },
                { 0xf8, new Decoded86("fprem") },
                { 0xf2, new Decoded86("fptan") },
                { 0xfc, new Decoded86("frndint") },
                { 0xfd, new Decoded86("fscale") },
                { 0xfe, new Decoded86("fsin") },
                { 0xfa, new Decoded86("fsqrt") },
                { 0x10, new Decoded86("fst", Operand86.Ed) },
                { 0x38, new Decoded86("fnstcw", Operand86.Ew) }, // extend from 9b
                { 0x30, new Decoded86("fnstenv", Operand86.M) }, // extend from 9b
                { 0x18, new Decoded86("fstp", Operand86.Ed) },
                { 0xe4, new Decoded86("ftst") },
                { 0xe5, new Decoded86("fxam") }, // extend from 9b
                { 0xc8, new Decoded86("fxch", Operand86.St) },
                { 0xf4, new Decoded86("fxtract") },
                { 0xf1, new Decoded86("fyl2x") },
                { 0xf9, new Decoded86("fyl2xp1") }
            }
        },
        {
            0xda, new Dictionary<int, Decoded86>
            {
                { 0x00, new Decoded86("fiadd", Operand86.Ed ) },
                { 0x10, new Decoded86("ficom", Operand86.Ed ) },
                { 0x18, new Decoded86("ficomp", Operand86.Ed ) },
                { 0x30, new Decoded86("fidiv", Operand86.Ed ) },
                { 0x38, new Decoded86("fidivr", Operand86.Ed ) },
                { 0x08, new Decoded86("fimul",  Operand86.Ed ) },
                { 0x20, new Decoded86("fisub", Operand86.Ed ) },
                { 0x28, new Decoded86("fisubr", Operand86.Ed) },
            }
        },
        {
            0xdb, new Dictionary<int, Decoded86>
            {
                { 0xe2, new Decoded86("fnclex") }, // extend from 9b
                { 0xe1, new Decoded86("fndisi") }, // extend from 9b
                { 0xe0, new Decoded86("fneni") }, // extend from 9b
                { 0x00, new Decoded86("fild", Operand86.Ed) },
                { 0x10, new Decoded86("fist",  Operand86.Ed) },
                { 0x18, new Decoded86("fistp", Operand86.Ed) },
                { 0xe3, new Decoded86("fninit") }, // extend from 9b
                { 0x28, new Decoded86("fld", Operand86.Et) },
                { 0x38, new Decoded86("fstp",  Operand86.Et) }
            }
        },
        {
            0xdc, new Dictionary<int, Decoded86>
            {
                { 0x00, new Decoded86("fadd", Operand86.Eq ) },
                { 0x08, new Decoded86("fmul", Operand86.Eq) },
                { 0x10, new Decoded86("fcom", Operand86.Eq ) },
                { 0x18, new Decoded86("fcomp", Operand86.Eq) },
                { 0x20, new Decoded86("fsub",  Operand86.Eq ) },
                { 0x28, new Decoded86("fsubr",  Operand86.Eq ) },
                { 0x30, new Decoded86("fdiv", Operand86.Eq) },
                { 0x38, new Decoded86("fdivr", Operand86.Eq) },
                { 0xc0, new Decoded86("fadd", Operand86.St, Operand86.St) },
                { 0xc8, new Decoded86("fmul", Operand86.St, Operand86.St) },
                { 0xd0, new Decoded86("fcom", Operand86.St, Operand86.St) },
                { 0xd8, new Decoded86("fcomp", Operand86.St, Operand86.St) },
                { 0xe8, new Decoded86("fsub", Operand86.St, Operand86.St) },
                { 0xe0, new Decoded86("fsubr", Operand86.St, Operand86.St) },
                { 0xf0, new Decoded86("fdivr", Operand86.St, Operand86.St) },
                { 0xf8, new Decoded86("fdiv", Operand86.St, Operand86.St) }
            }  
        },
        {
            0xdd, new Dictionary<int, Decoded86>
            {
                { 0x00, new Decoded86("fld", Operand86.Eq) },
                { 0x20, new Decoded86("frstor", Operand86.Ed) },
                { 0x30, new Decoded86("fnsave", Operand86.Ed) }, // extend from 9b
                { 0x10, new Decoded86("fst", Operand86.Eq) },
                { 0xd0, new Decoded86("fst", Operand86.St) },
                { 0x18, new Decoded86("fstp", Operand86.Eq) },
                { 0x38, new Decoded86("fstsw", Operand86.Ed) },
                { 0xc0, new Decoded86("ffree", Operand86.St) },
                { 0xd8, new Decoded86("fstp", Operand86.St) }
            }
        },
        {
            0xde, new Dictionary<int, Decoded86>
            {
                { 0xc0, new Decoded86("faddp", Operand86.St,Operand86.St) },
                { 0xd9, new Decoded86("fcompp") },
                { 0x10, new Decoded86("ficom", Operand86.Ew ) },
                { 0x18, new Decoded86("ficomp", Operand86.Ew) },
                { 0xf8, new Decoded86("fdivp", Operand86.St,Operand86.St) },
                { 0xf9, new Decoded86("fdivp") },
                { 0xf0, new Decoded86("fdivrp", Operand86.St, Operand86.St) },
                { 0x00, new Decoded86("fiadd", Operand86.Ew ) },
                { 0x30, new Decoded86("fidiv", Operand86.Ew ) },
                { 0x38, new Decoded86("fidivr", Operand86.Ew) },
                { 0x08, new Decoded86("fimul",  Operand86.Ew ) },
                { 0x20, new Decoded86("fisub", Operand86.Ew) },
                { 0x28, new Decoded86("fisubr", Operand86.Ew) },
                { 0xc8, new Decoded86("fmulp",  Operand86.St,Operand86.St) },
                { 0xc9, new Decoded86("fmulp") },
                { 0xe8, new Decoded86("fsubp", Operand86.St, Operand86.St) },
                { 0xe9, new Decoded86("fsubp") },
                { 0xe0, new Decoded86("fsubrp", Operand86.St, Operand86.St) },
            }
        },
        {
            0xdf, new Dictionary<int, Decoded86>
            {
                { 0x20, new Decoded86("fbld", Operand86.Et) },
                { 0x30, new Decoded86("fbstp", Operand86.Et) },
                { 0x00, new Decoded86("fild", Operand86.Ew) },
                { 0x10, new Decoded86("fist",  Operand86.Ew) },
                { 0x18, new Decoded86("fistp",Operand86.Ew) },
                { 0x38, new Decoded86("fistp", Operand86.Eq) },
                { 0x28, new Decoded86("fild", Operand86.Eq) },
                { 0xe0, new Decoded86("fnstsw") } // extend from 9b
            }
        }
    }.ToFrozenDictionary();

    private static readonly FrozenSet<Operand86> s_usesModRm =
        [
            Operand86.Eb, 
            Operand86.Ed,
            Operand86.Eq,
            Operand86.Et,
            Operand86.Ev, 
            Operand86.Ew, 
            Operand86.M, 
            Operand86.Mp
        ];
    
    private static readonly FrozenDictionary<int, string> s_strings86Formats = new Dictionary<int, string>()
    {
        { 0xa6, " BYTE PTR ds:[si],BYTE PTR es:[di]" },
        { 0xa7, " WORD PTR ds:[si],WORD PTR es:[di]" },
        { 0x6c, " BYTE PTR es:[di],dx" },
        { 0x6d, " WORD PTR es:[di],dx" },
        { 0x6e, " dx,BYTE PTR ds:[si]" },
        { 0x6f, " dx,WORD PTR ds:[si]" },
        { 0xac, " al,BYTE PTR ds:[si]" },
        { 0xad, " ax,WORD PTR ds:[si]" },
        { 0xa4, " BYTE PTR es:[di],BYTE PTR ds:[si]" },
        { 0xa5, " WORD PTR es:[di],WORD PTR ds:[si]" },
        { 0xae, " al,BYTE PTR es:[di]" },
        { 0xaf, " ax,WORD PTR es:[di]" },
        { 0xaa, " BYTE PTR es:[di],al" },
        { 0xab, " WORD PTR es:[di],ax" },
        { 0xd7, " BYTE PTR ds:[bx]" },
    }.ToFrozenDictionary();
    
    private static readonly FrozenDictionary<int, string> s_regs8 = new Dictionary<int, string>
    {
        { 0b000, "al" },
        { 0b001, "cl" },
        { 0b010, "dl" },
        { 0b011, "bl" },
        { 0b100, "ah" },
        { 0b101, "ch" },
        { 0b110, "dh" },
        { 0b111, "bh" }
    }.ToFrozenDictionary();
    
    private static readonly FrozenDictionary<int, string> s_regs16 = new Dictionary<int, string>()
    {
        { 0b000, "ax" },
        { 0b001, "cx" },
        { 0b010, "dx" },
        { 0b011, "bx" },
        { 0b100, "sp" },
        { 0b101, "bp" },
        { 0b110, "si" },
        { 0b111, "di" }
    }.ToFrozenDictionary();
    
    private static readonly FrozenDictionary<int, string> s_regsStack = new Dictionary<int, string>
    {
        { 0b000, "st0" },
        { 0b001, "st1" },
        { 0b010, "st2" },
        { 0b011, "st3" },
        { 0b100, "st4" },
        { 0b101, "st5" },
        { 0b110, "st6" },
        { 0b111, "st7" }
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<Opcode86Group, string[]> s_groups 
        = new Dictionary<Opcode86Group, string[]>
    {
        { Opcode86Group.Group1, ["add", "or", "adc", "sbb", "and", "sub", "xor", "cmp"] },
        { Opcode86Group.Group2, ["rol", "ror", "rcl", "rcr", "shl", "shr", "", "sar"] },
        { Opcode86Group.Group3A, ["test", "", "not", "neg", "mul", "imul", "div", "idiv"] },
        { Opcode86Group.Group3B, ["test", "", "not", "neg", "mul", "imul", "div", "idiv"] },
        { Opcode86Group.Group4, ["inc", "dec", "", "", "", "", "", ""] },
        { Opcode86Group.Group5, ["inc", "dec", "call", "call", "jmp", "jmp", "push", ""] }
    }.ToFrozenDictionary();
    
    private static readonly FrozenDictionary<int, string> s_modRmFormats 
        = new Dictionary<int, string>
    {
        { 0b00_000_000, "[bx+si]" },
        { 0b00_000_001, "[bx+di]" },
        { 0b00_000_010, "[bp+si]" },
        { 0b00_000_011, "[bp+di]" },
        { 0b00_000_100, "[si]" },
        { 0b00_000_101, "[di]" },
        { 0b00_000_110, "[0x{0:x}]" },
        { 0b00_000_111, "[bx]" },
        { 0b01_000_000, "[bx+si+0x{0:x}]" },
        { 0b01_000_001, "[bx+di+0x{0:x}]" },
        { 0b01_000_010, "[bp+si+0x{0:x}]" },
        { 0b01_000_011, "[bp+di+0x{0:x}]" },
        { 0b01_000_100, "[si+0x{0:x}]" },
        { 0b01_000_101, "[di+0x{0:x}]" },
        { 0b01_000_110, "[bp+0x{0:x}]" },
        { 0b01_000_111, "[bx+0x{0:x}]" },
        { 0b10_000_000, "[bx+si+0x{0:x}]" },
        { 0b10_000_001, "[bx+di+0x{0:x}]" },
        { 0b10_000_010, "[bp+si+0x{0:x}]" },
        { 0b10_000_011, "[bp+di+0x{0:x}]" },
        { 0b10_000_100, "[si+0x{0:x}]" },
        { 0b10_000_101, "[di+0x{0:x}]" },
        { 0b10_000_110, "[bp+0x{0:x}]" },
        { 0b10_000_111, "[bx+0x{0:x}]" },
        { 0b11_000_000, "ax" },
        { 0b11_000_001, "cx" },
        { 0b11_000_010, "dx" },
        { 0b11_000_011, "bx "},
        { 0b11_000_100, "sp" },
        { 0b11_000_101, "bp" },
        { 0b11_000_110, "si" },
        { 0b11_000_111, "di" }
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<int, string> s_segFormats = new Dictionary<int, string>
    {
        { 0x00, "es" },
        { 0x08, "cs" },
        { 0x10, "ss" },
        { 0x18, "ds" }
    }.ToFrozenDictionary();
    
    private static readonly FrozenDictionary<int, string> s_segments = new Dictionary<int, string>
    {
        { 0x26, "es" },
        { 0x2e, "cs" },
        { 0x36, "ss" },
        { 0x3e, "ds" }
    }.ToFrozenDictionary();
}