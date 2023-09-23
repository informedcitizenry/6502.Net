//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Describes a CPU instruction, including its opcode, byte size and disassembly
/// format.
/// </summary>
public sealed class Instruction
{
    /// <summary>
    /// Construct a new instance of an instruction.
    /// </summary>
    /// <param name="opcode">The instruction's opcode</param>
    /// <param name="size">The instruction's byte size.</param>
    public Instruction(int opcode, int size = 1)
        : this(opcode, size, false)
    {
      
    }

    /// <summary>
    /// Construct a new instance of an instruction.
    /// </summary>
    /// <param name="opcode">The instruction's opcode</param>
    /// <param name="size">The instruction's byte size.</param>
    /// <param name="isRelative">The flag determining whether the instruction uses
    /// relative mode addressing.</param>
    public Instruction(int opcode, int size, bool isRelative)
    {
        Operands = Array.Empty<int>();
        Opcode = opcode;
        Size = size;
        DisassemblyFormat = opcode.Size() == 1 ?
            $".byte {opcode:x2}" :
            $".word {opcode:x4}";
        IsRelative = isRelative;
        Is16BitRelative = false;
    }

    /// <summary>
    /// Construct a new instance of an instruction.
    /// </summary>
    /// <param name="disassemblyFormat">The instruction's disassembly format.</param>
    /// <param name="opcode">The instruction's opcode</param>
    /// <param name="size">The instruction's byte size.</param>
    /// <param name="isRelative">The flag determining whether the instruction uses
    /// relative mode addressing.</param>
    /// <param name="is16BitRelative">The flag determining whether the instruction uses
    /// 16-bit relative mode addressing.</param>
    public Instruction(string disassemblyFormat, int opcode = 0x00, int size = 1, bool isRelative = false, bool is16BitRelative = false)
    {
        DisassemblyFormat = disassemblyFormat;
        Opcode = opcode;
        Size = size;
        Operands = new int[1] { Size - opcode.Size() };
        IsRelative = isRelative;
        Is16BitRelative = IsRelative && is16BitRelative;
    }

    /// <summary>
    /// Construct a new instance of an instruction.
    /// </summary>
    /// <param name="disassemblyFormat">The instruction's disassembly format.</param>
    /// <param name="opcode">The instruction's opcodes.</param>
    /// <param name="operands">The size of each of the instruction's operands.</param>
    public Instruction(string disassemblyFormat, int opcode, params int[] operands)
        : this(disassemblyFormat, opcode, false, operands)
    {
    }

    /// <summary>
    /// Construct a new instance of an instruction.
    /// </summary>
    /// <param name="disassemblyFormat">The instruction's disassembly format.</param>
    /// <param name="opcode">The instruction's opcodes.</param>
    /// <param name="isRelative">The flag determining whether the instruction uses
    /// relative mode addressing.</param>
    /// <param name="operands">The size of each of the instruction's operands.</param>
    public Instruction(string disassemblyFormat, int opcode, bool isRelative, params int[] operands)
    {
        DisassemblyFormat = disassemblyFormat;
        Opcode = opcode;
        Operands = operands;
        Size = opcode.Size() + operands.Sum();
        IsRelative = isRelative;
        Is16BitRelative = false;
    }


    public override string ToString() => $"{Opcode:X2}: {DisassemblyFormat}";

    /// <summary>
    /// Gets the instruction's opcode.
    /// </summary>
    public int Opcode { get; init; }

    /// <summary>
    /// Gets the instruction's size in bytes.
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// Gets the operand sizes.
    /// </summary>
    public int[] Operands { get; }

    /// <summary>
    /// Gets the instruction's disassembly string.
    /// </summary>
    public string DisassemblyFormat { get; }

    /// <summary>
    /// Gets the flag indicating whether the instruction uses relative
    /// addressing.
    /// </summary>
    public bool IsRelative { get; }

    /// <summary>
    /// Gets the flag indicating whether the instruction uses 16-bit relative
    /// addressing.
    /// </summary>
    public bool Is16BitRelative { get; }
}

public struct M6xxOpcode
{
    public M6xxOpcode()
        : this(CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad)
    {

    }
    /*
     * int implied,
                    int accumulator,
                    int zeroPage,
                    int relative,
                    int absolute,
                    int immediate,
                    int zeroPageX,
                    int zeroPageY,
                    int absoluteX,
                    int absoluteY,
                    int indirect,
                    int indirectIndexed,
                    int indexedIndirect,
                    int indirectZeroPage,
     */
    public M6xxOpcode(int implied,
                    int zeroPage,
                    int relative,
                    int absolute,
                    int immediate,
                    int zeroPageX,
                    int relativeAbs,
                    int immediateAbs)
        : this(implied,
              CpuEncoderBase.Bad,
              zeroPage,
              relative,
              absolute,
              immediate,
              zeroPageX,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              relativeAbs,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              immediateAbs,
              CpuEncoderBase.Bad)
    {

    }

    public M6xxOpcode(int implied,
                    int accumulator,
                    int zeroPage,
                    int relative,
                    int absolute,
                    int immediate,
                    int zeroPageX,
                    int zeroPageY,
                    int absoluteX,
                    int absoluteY,
                    int indirect,
                    int indirectIndexed,
                    int indexedIndirect)
        : this(implied,
              accumulator,
              zeroPage,
              relative,
              absolute,
              immediate,
              zeroPageX,
              zeroPageY,
              absoluteX,
              absoluteY,
              indirect,
              indirectIndexed,
              indexedIndirect,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad)
    {

    }

    public M6xxOpcode(int implied,
                    int accumulator,
                    int zeroPage,
                    int relative,
                    int absolute,
                    int immediate,
                    int zeroPageX,
                    int zeroPageY,
                    int absoluteX,
                    int absoluteY,
                    int indirect,
                    int indirectIndexed,
                    int indexedIndirect,
                    int indirectZeroPage,
                    int indirectIndexedAbs)
        : this(implied,
              accumulator,
              zeroPage,
              relative,
              absolute,
              immediate,
              zeroPageX,
              zeroPageY,
              absoluteX,
              absoluteY,
              indirect,
              indirectIndexed,
              indexedIndirect,
              indirectZeroPage,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              indirectIndexedAbs,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad)
    {

    }

    public M6xxOpcode(int implied,
                    int accumulator,
                    int zeroPage,
                    int relative,
                    int absolute,
                    int immediate,
                    int zeroPageX,
                    int zeroPageY,
                    int absoluteX,
                    int absoluteY,
                    int indirect,
                    int indirectIndexed,
                    int indexedIndirect,
                    int indirectZeroPage,
                    int indirectIndexedAbs,
                    int bitTest,
                    int bitTestAbs)
        : this(implied,
              accumulator,
              zeroPage,
              relative,
              absolute,
              immediate,
              zeroPageX,
              zeroPageY,
              absoluteX,
              absoluteY,
              indirect,
              indirectIndexed,
              indexedIndirect,
              indirectZeroPage,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              bitTest,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              indirectIndexedAbs,
              bitTestAbs,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad,
              CpuEncoderBase.Bad)
    {

    }

    public M6xxOpcode(int implied,
                    int accumulator,
                    int zeroPage,
                    int relative,
                    int absolute,
                    int immediate,
                    int zeroPageX,
                    int zeroPageY,
                    int absoluteX,
                    int absoluteY,
                    int indirect,
                    int indirectIndexed,
                    int indexedIndirect,
                    int indirectZeroPage,
                    int longAddress,
                    int longX,
                    int bitTest,
                    int blockMove,
                    int zeroPageS,
                    int indirectS,
                    int indexedIndirectAbs,
                    int bitTestAbs,
                    int directIndexed,
                    int direct,
                    int directAbs,
                    int relativeAbs,
                    int bitTestAbsX,
                    int threeOperand,
                    int indirectZ,
                    int directZ,
                    int indirectSp,
                    int immediateAbs,
                    int bitTestZpX)
    {
        this.implied = implied;
        this.accumulator = accumulator;
        this.zeroPage = zeroPage;
        this.relative = relative;
        this.absolute = absolute;
        this.immediate = immediate;
        this.zeroPageX = zeroPageX;
        this.zeroPageY = zeroPageY;
        this.absoluteX = absoluteX;
        this.absoluteY = absoluteY;
        this.longAddress = longAddress;
        this.longX = longX;
        this.indirect = indirect;
        this.indirectIndexed = indirectIndexed;
        this.indexedIndirect = indexedIndirect;
        this.indirectZeroPage = indirectZeroPage;
        this.bitTest = bitTest;
        this.blockMove = blockMove;
        this.zeroPageS = zeroPageS;
        this.indirectS = indirectS;
        this.indexedIndirectAbs = indexedIndirectAbs;
        this.bitTestAbs = bitTestAbs;
        this.directIndexed = directIndexed;
        this.direct = direct;
        this.directAbs = directAbs;
        this.relativeAbs = relativeAbs;
        this.bitTestAbsX = bitTestAbsX;
        this.threeOperand = threeOperand;
        this.indirectZ = indirectZ;
        this.directZ = directZ;
        this.indirectSp = indirectSp;
        this.immediateAbs = immediateAbs;
        this.bitTestZpX = bitTestZpX;
    }

    public int implied;
    public int accumulator;
    public int zeroPage;
    public int relative;
    public int absolute;
    public int immediate;
    public int zeroPageX;
    public int zeroPageY;
    public int absoluteX;
    public int absoluteY;
    public int longAddress;
    public int longX;
    public int indirect;
    public int indirectIndexed;
    public int indexedIndirect;
    public int indirectZeroPage;
    public int bitTest;
    public int blockMove;
    public int zeroPageS;
    public int indirectS;
    public int indexedIndirectAbs;
    public int bitTestAbs;
    public int directIndexed;
    public int direct;
    public int directAbs;
    public int relativeAbs;
    public int bitTestAbsX;
    public int threeOperand;
    public int indirectZ;
    public int directZ;
    public int indirectSp;
    public int immediateAbs;
    public int bitTestZpX;
}