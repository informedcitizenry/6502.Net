//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

public sealed partial class M65xxInstructionEncoder
{
    // s_6502

    private static readonly Dictionary<int, Instruction> s_6502Absolute = new()
    {
        { SyntaxParser.ORA, new Instruction("ora ${0:x4}", 0x0d, 3) },
        { SyntaxParser.ASL, new Instruction("asl ${0:x4}", 0x0e, 3) },
        { SyntaxParser.JSR, new Instruction("jsr ${0:x4}", 0x20, 3) },
        { SyntaxParser.BIT, new Instruction("bit ${0:x4}", 0x2c, 3) },
        { SyntaxParser.AND, new Instruction("and ${0:x4}", 0x2d, 3) },
        { SyntaxParser.ROL, new Instruction("rol ${0:x4}", 0x2e, 3) },
        { SyntaxParser.JMP, new Instruction("jmp ${0:x4}", 0x4c, 3) },
        { SyntaxParser.EOR, new Instruction("eor ${0:x4}", 0x4d, 3) },
        { SyntaxParser.LSR, new Instruction("lsr ${0:x4}", 0x4e, 3) },
        { SyntaxParser.ADC, new Instruction("adc ${0:x4}", 0x6d, 3) },
        { SyntaxParser.ROR, new Instruction("ror ${0:x4}", 0x6e, 3) },
        { SyntaxParser.STY, new Instruction("sty ${0:x4}", 0x8c, 3) },
        { SyntaxParser.STA, new Instruction("sta ${0:x4}", 0x8d, 3) },
        { SyntaxParser.STX, new Instruction("stx ${0:x4}", 0x8e, 3) },
        { SyntaxParser.LDY, new Instruction("ldy ${0:x4}", 0xac, 3) },
        { SyntaxParser.LDA, new Instruction("lda ${0:x4}", 0xad, 3) },
        { SyntaxParser.LDX, new Instruction("ldx ${0:x4}", 0xae, 3) },
        { SyntaxParser.CPY, new Instruction("cpy ${0:x4}", 0xcc, 3) },
        { SyntaxParser.CMP, new Instruction("cmp ${0:x4}", 0xcd, 3) },
        { SyntaxParser.DEC, new Instruction("dec ${0:x4}", 0xce, 3) },
        { SyntaxParser.CPX, new Instruction("cpx ${0:x4}", 0xec, 3) },
        { SyntaxParser.SBC, new Instruction("sbc ${0:x4}", 0xed, 3) },
        { SyntaxParser.INC, new Instruction("inc ${0:x4}", 0xee, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_6502AbsoluteX = new()
    {
        { SyntaxParser.ORA, new Instruction("ora ${0:x4},x", 0x1d, 3) },
        { SyntaxParser.ASL, new Instruction("asl ${0:x4},x", 0x1e, 3) },
        { SyntaxParser.AND, new Instruction("and ${0:x4},x", 0x3d, 3) },
        { SyntaxParser.ROL, new Instruction("rol ${0:x4},x", 0x3e, 3) },
        { SyntaxParser.EOR, new Instruction("eor ${0:x4},x", 0x5d, 3) },
        { SyntaxParser.LSR, new Instruction("lsr ${0:x4},x", 0x5e, 3) },
        { SyntaxParser.ADC, new Instruction("adc ${0:x4},x", 0x7d, 3) },
        { SyntaxParser.ROR, new Instruction("ror ${0:x4},x", 0x7e, 3) },
        { SyntaxParser.STA, new Instruction("sta ${0:x4},x", 0x9d, 3) },
        { SyntaxParser.LDY, new Instruction("ldy ${0:x4},x", 0xbc, 3) },
        { SyntaxParser.LDA, new Instruction("lda ${0:x4},x", 0xbd, 3) },
        { SyntaxParser.CMP, new Instruction("cmp ${0:x4},x", 0xdd, 3) },
        { SyntaxParser.DEC, new Instruction("dec ${0:x4},x", 0xde, 3) },
        { SyntaxParser.SBC, new Instruction("sbc ${0:x4},x", 0xfd, 3) },
        { SyntaxParser.INC, new Instruction("inc ${0:x4},x", 0xfe, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_6502AbsoluteY = new()
    {
        { SyntaxParser.ORA, new Instruction("ora ${0:x4},y", 0x19, 3) },
        { SyntaxParser.AND, new Instruction("and ${0:x4},y", 0x39, 3) },
        { SyntaxParser.EOR, new Instruction("eor ${0:x4},y", 0x59, 3) },
        { SyntaxParser.ADC, new Instruction("adc ${0:x4},y", 0x79, 3) },
        { SyntaxParser.STA, new Instruction("sta ${0:x4},y", 0x99, 3) },
        { SyntaxParser.LDA, new Instruction("lda ${0:x4},y", 0xb9, 3) },
        { SyntaxParser.LDX, new Instruction("ldx ${0:x4},y", 0xbe, 3) },
        { SyntaxParser.CMP, new Instruction("cmp ${0:x4},y", 0xd9, 3) },
        { SyntaxParser.SBC, new Instruction("sbc ${0:x4},y", 0xf9, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_m6502Accumulators = new()
    {
        { SyntaxParser.ASL, new Instruction(0x0a, 1) },
        { SyntaxParser.LSR, new Instruction(0x4a, 1) },
        { SyntaxParser.ROL, new Instruction(0x2a, 1) },
        { SyntaxParser.ROR, new Instruction(0x6a, 1) }
    };

    private static readonly Dictionary<int, Instruction> s_6502Immediate = new()
    {
        { SyntaxParser.ORA, new Instruction("ora #${0:x2}", 0x09, 2) },
        { SyntaxParser.AND, new Instruction("and #${0:x2}", 0x29, 2) },
        { SyntaxParser.EOR, new Instruction("eor #${0:x2}", 0x49, 2) },
        { SyntaxParser.ADC, new Instruction("adc #${0:x2}", 0x69, 2) },
        { SyntaxParser.LDY, new Instruction("ldy #${0:x2}", 0xa0, 2) },
        { SyntaxParser.LDX, new Instruction("ldx #${0:x2}", 0xa2, 2) },
        { SyntaxParser.LDA, new Instruction("lda #${0:x2}", 0xa9, 2) },
        { SyntaxParser.CPY, new Instruction("cpy #${0:x2}", 0xc0, 2) },
        { SyntaxParser.CMP, new Instruction("cmp #${0:x2}", 0xc9, 2) },
        { SyntaxParser.CPX, new Instruction("cpx #${0:x2}", 0xe0, 2) },
        { SyntaxParser.SBC, new Instruction("sbc #${0:x2}", 0xe9, 2) },
    };
    private static readonly Dictionary<int, Instruction> s_6502Implied = new()
    {
        { SyntaxParser.BRK, new Instruction("brk", 0x00) },
        { SyntaxParser.PHP, new Instruction("php", 0x08) },
        { SyntaxParser.ASL, new Instruction("asl", 0x0a) },
        { SyntaxParser.CLC, new Instruction("clc", 0x18) },
        { SyntaxParser.PLP, new Instruction("plp", 0x28) },
        { SyntaxParser.ROL, new Instruction("rol", 0x2a) },
        { SyntaxParser.SEC, new Instruction("sec", 0x38) },
        { SyntaxParser.RTI, new Instruction("rti", 0x40) },
        { SyntaxParser.PHA, new Instruction("pha", 0x48) },
        { SyntaxParser.LSR, new Instruction("lsr", 0x4a) },
        { SyntaxParser.CLI, new Instruction("cli", 0x58) },
        { SyntaxParser.RTS, new Instruction("rts", 0x60) },
        { SyntaxParser.PLA, new Instruction("pla", 0x68) },
        { SyntaxParser.ROR, new Instruction("ror", 0x6a) },
        { SyntaxParser.SEI, new Instruction("sei", 0x78) },
        { SyntaxParser.DEY, new Instruction("dey", 0x88) },
        { SyntaxParser.TXA, new Instruction("txa", 0x8a) },
        { SyntaxParser.TYA, new Instruction("tya", 0x98) },
        { SyntaxParser.TXS, new Instruction("txs", 0x9a) },
        { SyntaxParser.TAY, new Instruction("tay", 0xa8) },
        { SyntaxParser.TAX, new Instruction("tax", 0xaa) },
        { SyntaxParser.CLV, new Instruction("clv", 0xb8) },
        { SyntaxParser.TSX, new Instruction("tsx", 0xba) },
        { SyntaxParser.INY, new Instruction("iny", 0xc8) },
        { SyntaxParser.DEX, new Instruction("dex", 0xca) },
        { SyntaxParser.CLD, new Instruction("cld", 0xd8) },
        { SyntaxParser.INX, new Instruction("inx", 0xe8) },
        { SyntaxParser.NOP, new Instruction("nop", 0xea) },
        { SyntaxParser.SED, new Instruction("sed", 0xf8) },
    };

    private static readonly Dictionary<int, Instruction> s_6502IndAbs = new()
    {
        { SyntaxParser.JMP, new Instruction("jmp (${0:x4})", 0x6c, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_6502IndX = new()
    {
        { SyntaxParser.ORA, new Instruction("ora (${0:x2},x)", 0x01, 2) },
        { SyntaxParser.AND, new Instruction("and (${0:x2},x)", 0x21, 2) },
        { SyntaxParser.EOR, new Instruction("eor (${0:x2},x)", 0x41, 2) },
        { SyntaxParser.ADC, new Instruction("adc (${0:x2},x)", 0x61, 2) },
        { SyntaxParser.STA, new Instruction("sta (${0:x2},x)", 0x81, 2) },
        { SyntaxParser.LDA, new Instruction("lda (${0:x2},x)", 0xa1, 2) },
        { SyntaxParser.CMP, new Instruction("cmp (${0:x2},x)", 0xc1, 2) },
        { SyntaxParser.SBC, new Instruction("sbc (${0:x2},x)", 0xe1, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_6502IndY = new()
    {
        { SyntaxParser.ORA, new Instruction("ora (${0:x2}),y", 0x11, 2) },
        { SyntaxParser.AND, new Instruction("and (${0:x2}),y", 0x31, 2) },
        { SyntaxParser.EOR, new Instruction("eor (${0:x2}),y", 0x51, 2) },
        { SyntaxParser.ADC, new Instruction("adc (${0:x2}),y", 0x71, 2) },
        { SyntaxParser.STA, new Instruction("sta (${0:x2}),y", 0x91, 2) },
        { SyntaxParser.LDA, new Instruction("lda (${0:x2}),y", 0xb1, 2) },
        { SyntaxParser.CMP, new Instruction("cmp (${0:x2}),y", 0xd1, 2) },
        { SyntaxParser.SBC, new Instruction("sbc (${0:x2}),y", 0xf1, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_6502Relative = new()
    {
        { SyntaxParser.BPL, new Instruction("bpl ${0:x4}", 0x10, 2, true) },
        { SyntaxParser.BMI, new Instruction("bmi ${0:x4}", 0x30, 2, true) },
        { SyntaxParser.BVC, new Instruction("bvc ${0:x4}", 0x50, 2, true) },
        { SyntaxParser.BVS, new Instruction("bvs ${0:x4}", 0x70, 2, true) },
        { SyntaxParser.BCC, new Instruction("bcc ${0:x4}", 0x90, 2, true) },
        { SyntaxParser.BCS, new Instruction("bcs ${0:x4}", 0xb0, 2, true) },
        { SyntaxParser.BNE, new Instruction("bne ${0:x4}", 0xd0, 2, true) },
        { SyntaxParser.BEQ, new Instruction("beq ${0:x4}", 0xf0, 2, true) },
    };

    private static readonly Dictionary<int, Instruction> s_6502PseudoRelative = new()
    {
        { SyntaxParser.JPL, new Instruction("bpl ${0:x4}", 0x10, 2, true) },
        { SyntaxParser.JMI, new Instruction("bmi ${0:x4}", 0x30, 2, true) },
        { SyntaxParser.JVC, new Instruction("bvc ${0:x4}", 0x50, 2, true) },
        { SyntaxParser.JVS, new Instruction("bvs ${0:x4}", 0x70, 2, true) },
        { SyntaxParser.JCC, new Instruction("bcc ${0:x4}", 0x90, 2, true) },
        { SyntaxParser.JCS, new Instruction("bcs ${0:x4}", 0xb0, 2, true) },
        { SyntaxParser.JNE, new Instruction("bne ${0:x4}", 0xd0, 2, true) },
        { SyntaxParser.JEQ, new Instruction("beq ${0:x4}", 0xf0, 2, true) }
    };

    private static readonly Dictionary<int, int> s_6502PseudoToReal = new()
    {
        { SyntaxParser.JMI, 0x10 },
        { SyntaxParser.JPL, 0x30 },
        { SyntaxParser.JVS, 0x50 },
        { SyntaxParser.JVC, 0x70 },
        { SyntaxParser.JCS, 0x90 },
        { SyntaxParser.JCC, 0xb0 },
        { SyntaxParser.JEQ, 0xd0 },
        { SyntaxParser.JNE, 0xf0 }
    };

    private static readonly Dictionary<int, Instruction> s_6502BranchAlways = new()
    {
        { SyntaxParser.BRA, new Instruction("bvc ${0:x4}", 0x50, 2, true) },
    };

    private static readonly Dictionary<int, Instruction> s_6502ZeroPage = new()
    {
        { SyntaxParser.ORA, new Instruction("ora ${0:x2}", 0x05, 2) },
        { SyntaxParser.ASL, new Instruction("asl ${0:x2}", 0x06, 2) },
        { SyntaxParser.BIT, new Instruction("bit ${0:x2}", 0x24, 2) },
        { SyntaxParser.AND, new Instruction("and ${0:x2}", 0x25, 2) },
        { SyntaxParser.ROL, new Instruction("rol ${0:x2}", 0x26, 2) },
        { SyntaxParser.EOR, new Instruction("eor ${0:x2}", 0x45, 2) },
        { SyntaxParser.LSR, new Instruction("lsr ${0:x2}", 0x46, 2) },
        { SyntaxParser.ADC, new Instruction("adc ${0:x2}", 0x65, 2) },
        { SyntaxParser.ROR, new Instruction("ror ${0:x2}", 0x66, 2) },
        { SyntaxParser.STY, new Instruction("sty ${0:x2}", 0x84, 2) },
        { SyntaxParser.STA, new Instruction("sta ${0:x2}", 0x85, 2) },
        { SyntaxParser.STX, new Instruction("stx ${0:x2}", 0x86, 2) },
        { SyntaxParser.LDY, new Instruction("ldy ${0:x2}", 0xa4, 2) },
        { SyntaxParser.LDA, new Instruction("lda ${0:x2}", 0xa5, 2) },
        { SyntaxParser.LDX, new Instruction("ldx ${0:x2}", 0xa6, 2) },
        { SyntaxParser.CPY, new Instruction("cpy ${0:x2}", 0xc4, 2) },
        { SyntaxParser.CMP, new Instruction("cmp ${0:x2}", 0xc5, 2) },
        { SyntaxParser.DEC, new Instruction("dec ${0:x2}", 0xc6, 2) },
        { SyntaxParser.CPX, new Instruction("cpx ${0:x2}", 0xe4, 2) },
        { SyntaxParser.SBC, new Instruction("sbc ${0:x2}", 0xe5, 2) },
        { SyntaxParser.INC, new Instruction("inc ${0:x2}", 0xe6, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_6502ZeroPageX = new()
    {
        { SyntaxParser.ORA, new Instruction("ora ${0:x2},x", 0x15, 2) },
        { SyntaxParser.ASL, new Instruction("asl ${0:x2},x", 0x16, 2) },
        { SyntaxParser.AND, new Instruction("and ${0:x2},x", 0x35, 2) },
        { SyntaxParser.ROL, new Instruction("rol ${0:x2},x", 0x36, 2) },
        { SyntaxParser.EOR, new Instruction("eor ${0:x2},x", 0x55, 2) },
        { SyntaxParser.LSR, new Instruction("lsr ${0:x2},x", 0x56, 2) },
        { SyntaxParser.ADC, new Instruction("adc ${0:x2},x", 0x75, 2) },
        { SyntaxParser.ROR, new Instruction("ror ${0:x2},x", 0x76, 2) },
        { SyntaxParser.STY, new Instruction("sty ${0:x2},x", 0x94, 2) },
        { SyntaxParser.STA, new Instruction("sta ${0:x2},x", 0x95, 2) },
        { SyntaxParser.LDY, new Instruction("ldy ${0:x2},x", 0xb4, 2) },
        { SyntaxParser.LDA, new Instruction("lda ${0:x2},x", 0xb5, 2) },
        { SyntaxParser.CMP, new Instruction("cmp ${0:x2},x", 0xd5, 2) },
        { SyntaxParser.DEC, new Instruction("dec ${0:x2},x", 0xd6, 2) },
        { SyntaxParser.SBC, new Instruction("sbc ${0:x2},x", 0xf5, 2) },
        { SyntaxParser.INC, new Instruction("inc ${0:x2},x", 0xf6, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_6502ZeroPageY = new()
    {
        { SyntaxParser.STX, new Instruction("stx ${0:x2},y", 0x96, 2) },
        { SyntaxParser.LDX, new Instruction("ldx ${0:x2},y", 0xb6, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_6502AllOpcodes = new()
    {
        { 0x00, new Instruction("brk", 0x00) },
        { 0x01, new Instruction("ora (${0:x2},x)", 0x01, 2) },
        { 0x05, new Instruction("ora ${0:x2}", 0x05, 2) },
        { 0x06, new Instruction("asl ${0:x2}", 0x06, 2) },
        { 0x08, new Instruction("php", 0x08) },
        { 0x09, new Instruction("ora #${0:x2}", 0x09, 2) },
        { 0x0a, new Instruction("asl", 0x0a) },
        { 0x0d, new Instruction("ora ${0:x4}", 0x0d, 3) },
        { 0x0e, new Instruction("asl ${0:x4}", 0x0e, 3) },
        { 0x10, new Instruction("bpl ${0:x4}", 0x10, 2, true) },
        { 0x11, new Instruction("ora (${0:x2}),y", 0x11, 2) },
        { 0x15, new Instruction("ora ${0:x2},x", 0x15, 2) },
        { 0x16, new Instruction("asl ${0:x2},x", 0x16, 2) },
        { 0x18, new Instruction("clc", 0x18) },
        { 0x19, new Instruction("ora ${0:x4},y", 0x19, 3) },
        { 0x1d, new Instruction("ora ${0:x4},x", 0x1d, 3) },
        { 0x1e, new Instruction("asl ${0:x4},x", 0x1e, 3) },
        { 0x20, new Instruction("jsr ${0:x4}", 0x20, 3) },
        { 0x21, new Instruction("and (${0:x2},x)", 0x21, 2) },
        { 0x24, new Instruction("bit ${0:x2}", 0x24, 2) },
        { 0x25, new Instruction("and ${0:x2}", 0x25, 2) },
        { 0x26, new Instruction("rol ${0:x2}", 0x26, 2) },
        { 0x28, new Instruction("plp", 0x28) },
        { 0x29, new Instruction("and #${0:x2}", 0x29, 2) },
        { 0x2a, new Instruction("rol", 0x2a) },
        { 0x2c, new Instruction("bit ${0:x4}", 0x2c, 3) },
        { 0x2d, new Instruction("and ${0:x4}", 0x2d, 3) },
        { 0x2e, new Instruction("rol ${0:x4}", 0x2e, 3) },
        { 0x30, new Instruction("bmi ${0:x4}", 0x30, 2, true) },
        { 0x31, new Instruction("and (${0:x2}),y", 0x31, 2) },
        { 0x35, new Instruction("and ${0:x2},x", 0x35, 2) },
        { 0x36, new Instruction("rol ${0:x2},x", 0x36, 2) },
        { 0x38, new Instruction("sec", 0x38) },
        { 0x39, new Instruction("and ${0:x4},y", 0x39, 3) },
        { 0x3d, new Instruction("and ${0:x4},x", 0x3d, 3) },
        { 0x3e, new Instruction("rol ${0:x4},x", 0x3e, 3) },
        { 0x40, new Instruction("rti", 0x40) },
        { 0x41, new Instruction("eor (${0:x2},x)", 0x41, 2) },
        { 0x45, new Instruction("eor ${0:x2}", 0x45, 2) },
        { 0x46, new Instruction("lsr ${0:x2}", 0x46, 2) },
        { 0x48, new Instruction("pha", 0x48) },
        { 0x49, new Instruction("eor #${0:x2}", 0x49, 2) },
        { 0x4a, new Instruction("lsr", 0x4a) },
        { 0x4c, new Instruction("jmp ${0:x4}", 0x4c, 3) },
        { 0x4d, new Instruction("eor ${0:x4}", 0x4d, 3) },
        { 0x4e, new Instruction("lsr ${0:x4}", 0x4e, 3) },
        { 0x50, new Instruction("bvc ${0:x4}", 0x50, 2, true) },
        { 0x51, new Instruction("eor (${0:x2}),y", 0x51, 2) },
        { 0x55, new Instruction("eor ${0:x2},x", 0x55, 2) },
        { 0x56, new Instruction("lsr ${0:x2},x", 0x56, 2) },
        { 0x58, new Instruction("cli", 0x58) },
        { 0x59, new Instruction("eor ${0:x4},y", 0x59, 3) },
        { 0x5d, new Instruction("eor ${0:x4},x", 0x5d, 3) },
        { 0x5e, new Instruction("lsr ${0:x4},x", 0x5e, 3) },
        { 0x60, new Instruction("rts", 0x60) },
        { 0x61, new Instruction("adc (${0:x2},x)", 0x61, 2) },
        { 0x65, new Instruction("adc ${0:x2}", 0x65, 2) },
        { 0x66, new Instruction("ror ${0:x2}", 0x66, 2) },
        { 0x68, new Instruction("pla", 0x68) },
        { 0x69, new Instruction("adc #${0:x2}", 0x69, 2) },
        { 0x6a, new Instruction("ror", 0x6a) },
        { 0x6c, new Instruction("jmp (${0:x4})", 0x6c, 3) },
        { 0x6d, new Instruction("adc ${0:x4}", 0x6d, 3) },
        { 0x6e, new Instruction("ror ${0:x4}", 0x6e, 3) },
        { 0x70, new Instruction("bvs ${0:x4}", 0x70, 2, true) },
        { 0x71, new Instruction("adc (${0:x2}),y", 0x71, 2) },
        { 0x75, new Instruction("adc ${0:x2},x", 0x75, 2) },
        { 0x76, new Instruction("ror ${0:x2},x", 0x76, 2) },
        { 0x78, new Instruction("sei", 0x78) },
        { 0x79, new Instruction("adc ${0:x4},y", 0x79, 3) },
        { 0x7d, new Instruction("adc ${0:x4},x", 0x7d, 3) },
        { 0x7e, new Instruction("ror ${0:x4},x", 0x7e, 3) },
        { 0x81, new Instruction("sta (${0:x2},x)", 0x81, 2) },
        { 0x84, new Instruction("sty ${0:x2}", 0x84, 2) },
        { 0x85, new Instruction("sta ${0:x2}", 0x85, 2) },
        { 0x86, new Instruction("stx ${0:x2}", 0x86, 2) },
        { 0x88, new Instruction("dey", 0x88) },
        { 0x8a, new Instruction("txa", 0x8a) },
        { 0x8c, new Instruction("sty ${0:x4}", 0x8c, 3) },
        { 0x8d, new Instruction("sta ${0:x4}", 0x8d, 3) },
        { 0x8e, new Instruction("stx ${0:x4}", 0x8e, 3) },
        { 0x90, new Instruction("bcc ${0:x4}", 0x90, 2, true) },
        { 0x91, new Instruction("sta (${0:x2}),y", 0x91, 2) },
        { 0x94, new Instruction("sty ${0:x2},x", 0x94, 2) },
        { 0x95, new Instruction("sta ${0:x2},x", 0x95, 2) },
        { 0x96, new Instruction("stx ${0:x2},y", 0x96, 2) },
        { 0x98, new Instruction("tya", 0x98) },
        { 0x99, new Instruction("sta ${0:x4},y", 0x99, 3) },
        { 0x9a, new Instruction("txs", 0x9a) },
        { 0x9d, new Instruction("sta ${0:x4},x", 0x9d, 3) },
        { 0xa0, new Instruction("ldy #${0:x2}", 0xa0, 2) },
        { 0xa1, new Instruction("lda (${0:x2},x)", 0xa1, 2) },
        { 0xa2, new Instruction("ldx #${0:x2}", 0xa2, 2) },
        { 0xa4, new Instruction("ldy ${0:x2}", 0xa4, 2) },
        { 0xa5, new Instruction("lda ${0:x2}", 0xa5, 2) },
        { 0xa6, new Instruction("ldx ${0:x2}", 0xa6, 2) },
        { 0xa8, new Instruction("tay", 0xa8) },
        { 0xa9, new Instruction("lda #${0:x2}", 0xa9, 2) },
        { 0xaa, new Instruction("tax", 0xaa) },
        { 0xac, new Instruction("ldy ${0:x4}", 0xac, 3) },
        { 0xad, new Instruction("lda ${0:x4}", 0xad, 3) },
        { 0xae, new Instruction("ldx ${0:x4}", 0xae, 3) },
        { 0xb0, new Instruction("bcs ${0:x4}", 0xb0, 2, true) },
        { 0xb1, new Instruction("lda (${0:x2}),y", 0xb1, 2) },
        { 0xb4, new Instruction("ldy ${0:x2},x", 0xb4, 2) },
        { 0xb5, new Instruction("lda ${0:x2},x", 0xb5, 2) },
        { 0xb6, new Instruction("ldx ${0:x2},y", 0xb6, 2) },
        { 0xb8, new Instruction("clv", 0xb8) },
        { 0xb9, new Instruction("lda ${0:x4},y", 0xb9, 3) },
        { 0xba, new Instruction("tsx", 0xba) },
        { 0xbc, new Instruction("ldy ${0:x4},x", 0xbc, 3) },
        { 0xbd, new Instruction("lda ${0:x4},x", 0xbd, 3) },
        { 0xbe, new Instruction("ldx ${0:x4},y", 0xbe, 3) },
        { 0xc0, new Instruction("cpy #${0:x2}", 0xc0, 2) },
        { 0xc1, new Instruction("cmp (${0:x2},x)", 0xc1, 2) },
        { 0xc4, new Instruction("cpy ${0:x2}", 0xc4, 2) },
        { 0xc5, new Instruction("cmp ${0:x2}", 0xc5, 2) },
        { 0xc6, new Instruction("dec ${0:x2}", 0xc6, 2) },
        { 0xc8, new Instruction("iny", 0xc8) },
        { 0xc9, new Instruction("cmp #${0:x2}", 0xc9, 2) },
        { 0xca, new Instruction("dex", 0xca) },
        { 0xcc, new Instruction("cpy ${0:x4}", 0xcc, 3) },
        { 0xcd, new Instruction("cmp ${0:x4}", 0xcd, 3) },
        { 0xce, new Instruction("dec ${0:x4}", 0xce, 3) },
        { 0xd0, new Instruction("bne ${0:x4}", 0xd0, 2, true) },
        { 0xd1, new Instruction("cmp (${0:x2}),y", 0xd1, 2) },
        { 0xd5, new Instruction("cmp ${0:x2},x", 0xd5, 2) },
        { 0xd6, new Instruction("dec ${0:x2},x", 0xd6, 2) },
        { 0xd8, new Instruction("cld", 0xd8) },
        { 0xd9, new Instruction("cmp ${0:x4},y", 0xd9, 3) },
        { 0xdd, new Instruction("cmp ${0:x4},x", 0xdd, 3) },
        { 0xde, new Instruction("dec ${0:x4},x", 0xde, 3) },
        { 0xe0, new Instruction("cpx #${0:x2}", 0xe0, 2) },
        { 0xe1, new Instruction("sbc (${0:x2},x)", 0xe1, 2) },
        { 0xe4, new Instruction("cpx ${0:x2}", 0xe4, 2) },
        { 0xe5, new Instruction("sbc ${0:x2}", 0xe5, 2) },
        { 0xe6, new Instruction("inc ${0:x2}", 0xe6, 2) },
        { 0xe8, new Instruction("inx", 0xe8) },
        { 0xe9, new Instruction("sbc #${0:x2}", 0xe9, 2) },
        { 0xea, new Instruction("nop", 0xea) },
        { 0xec, new Instruction("cpx ${0:x4}", 0xec, 3) },
        { 0xed, new Instruction("sbc ${0:x4}", 0xed, 3) },
        { 0xee, new Instruction("inc ${0:x4}", 0xee, 3) },
        { 0xf0, new Instruction("beq ${0:x4}", 0xf0, 2, true) },
        { 0xf1, new Instruction("sbc (${0:x2}),y", 0xf1, 2) },
        { 0xf5, new Instruction("sbc ${0:x2},x", 0xf5, 2) },
        { 0xf6, new Instruction("inc ${0:x2},x", 0xf6, 2) },
        { 0xf8, new Instruction("sed", 0xf8) },
        { 0xf9, new Instruction("sbc ${0:x4},y", 0xf9, 3) },
        { 0xfd, new Instruction("sbc ${0:x4},x", 0xfd, 3) },
        { 0xfe, new Instruction("inc ${0:x4},x", 0xfe, 3) },
    };

    // s_6502i
    private static readonly Dictionary<int, Instruction> s_6502iAbsolute = new()
    {
        { SyntaxParser.TOP, new Instruction("top ${0:x4}", 0x0c, 3) },
        { SyntaxParser.SLO, new Instruction("slo ${0:x4}", 0x0f, 3) },
        { SyntaxParser.RLA, new Instruction("rla ${0:x4}", 0x2f, 3) },
        { SyntaxParser.SRE, new Instruction("sre ${0:x4}", 0x4f, 3) },
        { SyntaxParser.RRA, new Instruction("rra ${0:x4}", 0x6f, 3) },
        { SyntaxParser.SAX, new Instruction("sax ${0:x4}", 0x8f, 3) },
        { SyntaxParser.LAX, new Instruction("lax ${0:x4}", 0xaf, 3) },
        { SyntaxParser.DCP, new Instruction("dcp ${0:x4}", 0xcf, 3) },
        { SyntaxParser.ISB, new Instruction("isb ${0:x4}", 0xef, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_6502iAbsoluteX = new()
    {
        { SyntaxParser.SLO, new Instruction("slo ${0:x4},x", 0x1f, 3) },
        { SyntaxParser.RLA, new Instruction("rla ${0:x4},x", 0x3f, 3) },
        { SyntaxParser.SRE, new Instruction("sre ${0:x4},x", 0x5f, 3) },
        { SyntaxParser.RRA, new Instruction("rra ${0:x4},x", 0x7f, 3) },
        { SyntaxParser.SHY, new Instruction("shy ${0:x4},x", 0x9c, 3) },
        { SyntaxParser.DCP, new Instruction("dcp ${0:x4},x", 0xdf, 3) },
        { SyntaxParser.ISB, new Instruction("isb ${0:x4},x", 0xff, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_6502iAbsoluteY = new()
    {
        { SyntaxParser.SLO, new Instruction("slo ${0:x4},y", 0x1b, 3) },
        { SyntaxParser.RLA, new Instruction("rla ${0:x4},y", 0x3b, 3) },
        { SyntaxParser.SRE, new Instruction("sre ${0:x4},y", 0x5b, 3) },
        { SyntaxParser.RRA, new Instruction("rra ${0:x4},y", 0x7b, 3) },
        { SyntaxParser.TAS, new Instruction("tas ${0:x4},y", 0x9b, 3) },
        { SyntaxParser.SHX, new Instruction("shx ${0:x4},y", 0x9e, 3) },
        { SyntaxParser.SHA, new Instruction("sha ${0:x4},y", 0x9f, 3) },
        { SyntaxParser.LAS, new Instruction("las ${0:x4},y", 0xbb, 3) },
        { SyntaxParser.LAX, new Instruction("lax ${0:x4},y", 0xbf, 3) },
        { SyntaxParser.DCP, new Instruction("dcp ${0:x4},y", 0xdb, 3) },
        { SyntaxParser.ISB, new Instruction("isb ${0:x4},y", 0xfb, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_6502iImmediate = new()
    {
        { SyntaxParser.ANC, new Instruction("anc #${0:x2}", 0x2b, 2) },
        { SyntaxParser.ASR, new Instruction("asr #${0:x2}", 0x4b, 2) },
        { SyntaxParser.ARR, new Instruction("arr #${0:x2}", 0x6b, 2) },
        { SyntaxParser.DOP, new Instruction("dop #${0:x2}", 0x80, 2) },
        { SyntaxParser.ANE, new Instruction("ane #${0:x2}", 0x8b, 2) },
        { SyntaxParser.SAX, new Instruction("sax #${0:x2}", 0xcb, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_6502iImplied = new()
    {
        { SyntaxParser.JAM, new Instruction("jam", 0x03) },
        { SyntaxParser.TOP, new Instruction("top", 0x0c) },
        { SyntaxParser.STP, new Instruction("stp", 0x13) },
        { SyntaxParser.DOP, new Instruction("dop", 0x80) },
    };

    private static readonly Dictionary<int, Instruction> s_6502iIndX = new()
    {
        { SyntaxParser.SLO, new Instruction("slo (${0:x2},x)", 0x03, 2) },
        { SyntaxParser.RLA, new Instruction("rla (${0:x2},x)", 0x23, 2) },
        { SyntaxParser.SRE, new Instruction("sre (${0:x2},x)", 0x43, 2) },
        { SyntaxParser.RRA, new Instruction("rra (${0:x2},x)", 0x63, 2) },
        { SyntaxParser.SAX, new Instruction("sax (${0:x2},x)", 0x83, 2) },
        { SyntaxParser.LAX, new Instruction("lax (${0:x2},x)", 0xa3, 2) },
        { SyntaxParser.DCP, new Instruction("dcp (${0:x2},x)", 0xc3, 2) },
        { SyntaxParser.ISB, new Instruction("isb (${0:x2},x)", 0xe3, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_6502iIndY = new()
    {
        { SyntaxParser.SLO, new Instruction("slo (${0:x2}),y", 0x13, 2) },
        { SyntaxParser.RLA, new Instruction("rla (${0:x2}),y", 0x33, 2) },
        { SyntaxParser.SRE, new Instruction("sre (${0:x2}),y", 0x53, 2) },
        { SyntaxParser.RRA, new Instruction("rra (${0:x2}),y", 0x73, 2) },
        { SyntaxParser.SHA, new Instruction("sha (${0:x2}),y", 0x93, 2) },
        { SyntaxParser.LAX, new Instruction("lax (${0:x2}),y", 0xb3, 2) },
        { SyntaxParser.DCP, new Instruction("dcp (${0:x2}),y", 0xd3, 2) },
        { SyntaxParser.ISB, new Instruction("isb (${0:x2}),y", 0xf3, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_6502iZeroPage = new()
    {
        { SyntaxParser.DOP, new Instruction("dop ${0:x2}", 0x04, 2) },
        { SyntaxParser.SLO, new Instruction("slo ${0:x2}", 0x07, 2) },
        { SyntaxParser.RLA, new Instruction("rla ${0:x2}", 0x27, 2) },
        { SyntaxParser.SRE, new Instruction("sre ${0:x2}", 0x47, 2) },
        { SyntaxParser.RRA, new Instruction("rra ${0:x2}", 0x67, 2) },
        { SyntaxParser.SAX, new Instruction("sax ${0:x2}", 0x87, 2) },
        { SyntaxParser.LAX, new Instruction("lax ${0:x2}", 0xa7, 2) },
        { SyntaxParser.DCP, new Instruction("dcp ${0:x2}", 0xc7, 2) },
        { SyntaxParser.ISB, new Instruction("isb ${0:x2}", 0xe7, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_6502iZeroPageX = new()
    {
        { SyntaxParser.DOP, new Instruction("dop ${0:x2},x", 0x14, 2) },
        { SyntaxParser.SLO, new Instruction("slo ${0:x2},x", 0x17, 2) },
        { SyntaxParser.RLA, new Instruction("rla ${0:x2},x", 0x37, 2) },
        { SyntaxParser.SRE, new Instruction("sre ${0:x2},x", 0x57, 2) },
        { SyntaxParser.RRA, new Instruction("rra ${0:x2},x", 0x77, 2) },
        { SyntaxParser.DCP, new Instruction("dcp ${0:x2},x", 0xd7, 2) },
        { SyntaxParser.ISB, new Instruction("isb ${0:x2},x", 0xf7, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_6502iZeroPageY = new()
    {
        { SyntaxParser.SAX, new Instruction("sax ${0:x2},y", 0x97, 2) },
        { SyntaxParser.LAX, new Instruction("lax ${0:x2},y", 0xb7, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_6502iAllOpcodes = new()
    {
        { 0x03, new Instruction("slo (${0:x2},x)", 0x03, 2) },
        { 0x04, new Instruction("dop ${0:x2}", 0x04, 2) },
        { 0x07, new Instruction("slo ${0:x2}", 0x07, 2) },
        { 0x0c, new Instruction("top ${0:x4}", 0x0c, 3) },
        { 0x0f, new Instruction("slo ${0:x4}", 0x0f, 3) },
        { 0x13, new Instruction("slo (${0:x2}),y", 0x13, 2) },
        { 0x14, new Instruction("dop ${0:x2},x", 0x14, 2) },
        { 0x17, new Instruction("slo ${0:x2},x", 0x17, 2) },
        { 0x1b, new Instruction("slo ${0:x4},y", 0x1b, 3) },
        { 0x1f, new Instruction("slo ${0:x4},x", 0x1f, 3) },
        { 0x23, new Instruction("rla (${0:x2},x)", 0x23, 2) },
        { 0x27, new Instruction("rla ${0:x2}", 0x27, 2) },
        { 0x2b, new Instruction("anc #${0:x2}", 0x2b, 2) },
        { 0x2f, new Instruction("rla ${0:x4}", 0x2f, 3) },
        { 0x33, new Instruction("rla (${0:x2}),y", 0x33, 2) },
        { 0x37, new Instruction("rla ${0:x2},x", 0x37, 2) },
        { 0x3b, new Instruction("rla ${0:x4},y", 0x3b, 3) },
        { 0x3f, new Instruction("rla ${0:x4},x", 0x3f, 3) },
        { 0x43, new Instruction("sre (${0:x2},x)", 0x43, 2) },
        { 0x47, new Instruction("sre ${0:x2}", 0x47, 2) },
        { 0x4b, new Instruction("asr #${0:x2}", 0x4b, 2) },
        { 0x4f, new Instruction("sre ${0:x4}", 0x4f, 3) },
        { 0x53, new Instruction("sre (${0:x2}),y", 0x53, 2) },
        { 0x57, new Instruction("sre ${0:x2},x", 0x57, 2) },
        { 0x5b, new Instruction("sre ${0:x4},y", 0x5b, 3) },
        { 0x5f, new Instruction("sre ${0:x4},x", 0x5f, 3) },
        { 0x63, new Instruction("rra (${0:x2},x)", 0x63, 2) },
        { 0x67, new Instruction("rra ${0:x2}", 0x67, 2) },
        { 0x6b, new Instruction("arr #${0:x2}", 0x6b, 2) },
        { 0x6f, new Instruction("rra ${0:x4}", 0x6f, 3) },
        { 0x73, new Instruction("rra (${0:x2}),y", 0x73, 2) },
        { 0x77, new Instruction("rra ${0:x2},x", 0x77, 2) },
        { 0x7b, new Instruction("rra ${0:x4},y", 0x7b, 3) },
        { 0x7f, new Instruction("rra ${0:x4},x", 0x7f, 3) },
        { 0x80, new Instruction("dop #${0:x2}", 0x80, 2) },
        { 0x83, new Instruction("sax (${0:x2},x)", 0x83, 2) },
        { 0x87, new Instruction("sax ${0:x2}", 0x87, 2) },
        { 0x8b, new Instruction("ane #${0:x2}", 0x8b, 2) },
        { 0x8f, new Instruction("sax ${0:x4}", 0x8f, 3) },
        { 0x93, new Instruction("sha (${0:x2}),y", 0x93, 2) },
        { 0x97, new Instruction("sax ${0:x2},y", 0x97, 2) },
        { 0x9b, new Instruction("tas ${0:x4},y", 0x9b, 3) },
        { 0x9c, new Instruction("shy ${0:x4},x", 0x9c, 3) },
        { 0x9e, new Instruction("shx ${0:x4},y", 0x9e, 3) },
        { 0x9f, new Instruction("sha ${0:x4},y", 0x9f, 3) },
        { 0xa3, new Instruction("lax (${0:x2},x)", 0xa3, 2) },
        { 0xa7, new Instruction("lax ${0:x2}", 0xa7, 2) },
        { 0xaf, new Instruction("lax ${0:x4}", 0xaf, 3) },
        { 0xb3, new Instruction("lax (${0:x2}),y", 0xb3, 2) },
        { 0xb7, new Instruction("lax ${0:x2},y", 0xb7, 2) },
        { 0xbb, new Instruction("las ${0:x4},y", 0xbb, 3) },
        { 0xbf, new Instruction("lax ${0:x4},y", 0xbf, 3) },
        { 0xc3, new Instruction("dcp (${0:x2},x)", 0xc3, 2) },
        { 0xc7, new Instruction("dcp ${0:x2}", 0xc7, 2) },
        { 0xcb, new Instruction("sax #${0:x2}", 0xcb, 2) },
        { 0xcf, new Instruction("dcp ${0:x4}", 0xcf, 3) },
        { 0xd3, new Instruction("dcp (${0:x2}),y", 0xd3, 2) },
        { 0xd7, new Instruction("dcp ${0:x2},x", 0xd7, 2) },
        { 0xdb, new Instruction("dcp ${0:x4},y", 0xdb, 3) },
        { 0xdf, new Instruction("dcp ${0:x4},x", 0xdf, 3) },
        { 0xe3, new Instruction("isb (${0:x2},x)", 0xe3, 2) },
        { 0xe7, new Instruction("isb ${0:x2}", 0xe7, 2) },
        { 0xef, new Instruction("isb ${0:x4}", 0xef, 3) },
        { 0xf3, new Instruction("isb (${0:x2}),y", 0xf3, 2) },
        { 0xf7, new Instruction("isb ${0:x2},x", 0xf7, 2) },
        { 0xfb, new Instruction("isb ${0:x4},y", 0xfb, 3) },
        { 0xff, new Instruction("isb ${0:x4},x", 0xff, 3) },
    };

    // s_65816
    private static readonly Dictionary<int, Instruction> s_65816Absolute = new()
    {
        { SyntaxParser.PEA, new Instruction("pea ${0:x4}", 0xf4, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_65816Dir = new()
    {
        { SyntaxParser.ORA, new Instruction("ora [${0:x2}]", 0x07, 2) },
        { SyntaxParser.AND, new Instruction("and [${0:x2}]", 0x27, 2) },
        { SyntaxParser.EOR, new Instruction("eor [${0:x2}]", 0x47, 2) },
        { SyntaxParser.ADC, new Instruction("adc [${0:x2}]", 0x67, 2) },
        { SyntaxParser.STA, new Instruction("sta [${0:x2}]", 0x87, 2) },
        { SyntaxParser.LDA, new Instruction("lda [${0:x2}]", 0xa7, 2) },
        { SyntaxParser.CMP, new Instruction("cmp [${0:x2}]", 0xc7, 2) },
        { SyntaxParser.SBC, new Instruction("sbc [${0:x2}]", 0xe7, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_65816DirAbs = new()
    {
        { SyntaxParser.JMP, new Instruction("jmp [${0:x4}]", 0xdc, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_65816DirY = new()
    {
        { SyntaxParser.ORA, new Instruction("ora [${0:x2}],y", 0x17, 2) },
        { SyntaxParser.AND, new Instruction("and [${0:x2}],y", 0x37, 2) },
        { SyntaxParser.EOR, new Instruction("eor [${0:x2}],y", 0x57, 2) },
        { SyntaxParser.ADC, new Instruction("adc [${0:x2}],y", 0x77, 2) },
        { SyntaxParser.STA, new Instruction("sta [${0:x2}],y", 0x97, 2) },
        { SyntaxParser.LDA, new Instruction("lda [${0:x2}],y", 0xb7, 2) },
        { SyntaxParser.CMP, new Instruction("cmp [${0:x2}],y", 0xd7, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_65816ImmAbs = new()
    {
        { SyntaxParser.PEA, new Instruction("pea #${0:x4}", 0xf4, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_65816Immediate = new()
    {
        { SyntaxParser.COP, new Instruction("cop #${0:x2}", 0x02, 2) },
        { SyntaxParser.REP, new Instruction("rep #${0:x2}", 0xc2, 2) },
        { SyntaxParser.SEP, new Instruction("sep #${0:x2}", 0xe2, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_65816Implied = new()
    {
        { SyntaxParser.PHD, new Instruction("phd", 0x0b) },
        { SyntaxParser.TCS, new Instruction("tcs", 0x1b) },
        { SyntaxParser.PLD, new Instruction("pld", 0x2b) },
        { SyntaxParser.TSC, new Instruction("tsc", 0x3b) },
        { SyntaxParser.WDM, new Instruction("wdm", 0x42) },
        { SyntaxParser.PHK, new Instruction("phk", 0x4b) },
        { SyntaxParser.TCD, new Instruction("tcd", 0x5b) },
        { SyntaxParser.RTL, new Instruction("rtl", 0x6b) },
        { SyntaxParser.TDC, new Instruction("tdc", 0x7b) },
        { SyntaxParser.PHB, new Instruction("phb", 0x8b) },
        { SyntaxParser.TXY, new Instruction("txy", 0x9b) },
        { SyntaxParser.PLB, new Instruction("plb", 0xab) },
        { SyntaxParser.TYX, new Instruction("tyx", 0xbb) },
        { SyntaxParser.XBA, new Instruction("xba", 0xeb) },
        { SyntaxParser.XCE, new Instruction("xce", 0xfb) },
    };

    private static readonly Dictionary<int, Instruction> s_65816IndAbsX = new()
    {
        { SyntaxParser.JSR, new Instruction("jsr (${0:x4},x)", 0xfc, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_65816IndS = new()
    {
        { SyntaxParser.ORA, new Instruction("ora (${0:x2},s),y", 0x13, 2) },
        { SyntaxParser.AND, new Instruction("and (${0:x2},s),y", 0x33, 2) },
        { SyntaxParser.EOR, new Instruction("eor (${0:x2},s),y", 0x53, 2) },
        { SyntaxParser.ADC, new Instruction("adc (${0:x2},s),y", 0x73, 2) },
        { SyntaxParser.STA, new Instruction("sta (${0:x2},s),y", 0x93, 2) },
        { SyntaxParser.LDA, new Instruction("lda (${0:x2},s),y", 0xb3, 2) },
        { SyntaxParser.CMP, new Instruction("cmp (${0:x2},s),y", 0xd3, 2) },
        { SyntaxParser.SBC, new Instruction("sbc (${0:x2},s),y", 0xf3, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_65816IndZp = new()
    {
        { SyntaxParser.PEI, new Instruction("pei (${0:x2})", 0xd4, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_65816Long = new()
    {
        { SyntaxParser.ORA, new Instruction("ora ${0:x6}", 0x0f, 4) },
        { SyntaxParser.JSL, new Instruction("jsl ${0:x6}", 0x22, 4) },
        { SyntaxParser.JSR, new Instruction("jsr ${0:x6}", 0x22, 4) },
        { SyntaxParser.AND, new Instruction("and ${0:x6}", 0x2f, 4) },
        { SyntaxParser.EOR, new Instruction("eor ${0:x6}", 0x4f, 4) },
        { SyntaxParser.JML, new Instruction("jml ${0:x6}", 0x5c, 4) },
        { SyntaxParser.JMP, new Instruction("jmp ${0:x6}", 0x5c, 4) },
        { SyntaxParser.ADC, new Instruction("adc ${0:x6}", 0x6f, 4) },
        { SyntaxParser.STA, new Instruction("sta ${0:x6}", 0x8f, 4) },
        { SyntaxParser.LDA, new Instruction("lda ${0:x6}", 0xaf, 4) },
        { SyntaxParser.CMP, new Instruction("cmp ${0:x6}", 0xcf, 4) },
        { SyntaxParser.SBC, new Instruction("sbc ${0:x6}", 0xef, 4) },
    };

    private static readonly Dictionary<int, Instruction> s_65816LongX = new()
    {
        { SyntaxParser.ORA, new Instruction("ora ${0:x6},x", 0x1f, 4) },
        { SyntaxParser.AND, new Instruction("and ${0:x6},x", 0x3f, 4) },
        { SyntaxParser.EOR, new Instruction("eor ${0:x6},x", 0x5f, 4) },
        { SyntaxParser.ADC, new Instruction("adc ${0:x6},x", 0x7f, 4) },
        { SyntaxParser.STA, new Instruction("sta ${0:x6},x", 0x9f, 4) },
        { SyntaxParser.LDA, new Instruction("lda ${0:x6},x", 0xbf, 4) },
        { SyntaxParser.CMP, new Instruction("cmp ${0:x6},x", 0xdf, 4) },
        { SyntaxParser.SBC, new Instruction("sbc ${0:x6},x", 0xff, 4) },
    };

    private static readonly Dictionary<int, Instruction> s_65816RelativeAbs = new()
    {
        { SyntaxParser.PER, new Instruction("per ${0:x4}", 0x62, 3, true, true) },
        { SyntaxParser.BRL, new Instruction("brl ${0:x4}", 0x82, 3, true, true) },
    };

    private static readonly Dictionary<int, Instruction> s_65816TwoOperand = new()
    {
        { SyntaxParser.MVP, new Instruction("mvp ${1:x2},${0:x2}", 0x44, 1,1) },
        { SyntaxParser.MVN, new Instruction("mvn ${1:x2},${0:x2}", 0x54, 1,1) },
    };

    private static readonly Dictionary<int, Instruction> s_65816ZeroPageS = new()
    {
        { SyntaxParser.ORA, new Instruction("ora ${0:x2},s", 0x03, 2) },
        { SyntaxParser.AND, new Instruction("and ${0:x2},s", 0x23, 2) },
        { SyntaxParser.EOR, new Instruction("eor ${0:x2},s", 0x43, 2) },
        { SyntaxParser.ADC, new Instruction("adc ${0:x2},s", 0x63, 2) },
        { SyntaxParser.STA, new Instruction("sta ${0:x2},s", 0x83, 2) },
        { SyntaxParser.LDA, new Instruction("lda ${0:x2},s", 0xa3, 2) },
        { SyntaxParser.CMP, new Instruction("cmp ${0:x2},s", 0xc3, 2) },
        { SyntaxParser.SBC, new Instruction("sbc ${0:x2},s", 0xe3, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_65816AllOpcodes = new()
    {
        { 0x02, new Instruction("cop #${0:x2}", 0x02, 2) },
        { 0x03, new Instruction("ora ${0:x2},s", 0x03, 2) },
        { 0x07, new Instruction("ora [${0:x2}]", 0x07, 2) },
        { 0x0b, new Instruction("phd", 0x0b) },
        { 0x0f, new Instruction("ora ${0:x6}", 0x0f, 4) },
        { 0x13, new Instruction("ora (${0:x2},s),y", 0x13, 2) },
        { 0x17, new Instruction("ora [${0:x2}],y", 0x17, 2) },
        { 0x1b, new Instruction("tcs", 0x1b) },
        { 0x1f, new Instruction("ora ${0:x6},x", 0x1f, 4) },
        { 0x22, new Instruction("jsr ${0:x6}", 0x22, 4) },
        { 0x23, new Instruction("and ${0:x2},s", 0x23, 2) },
        { 0x27, new Instruction("and [${0:x2}]", 0x27, 2) },
        { 0x2b, new Instruction("pld", 0x2b) },
        { 0x2f, new Instruction("and ${0:x6}", 0x2f, 4) },
        { 0x33, new Instruction("and (${0:x2},s),y", 0x33, 2) },
        { 0x37, new Instruction("and [${0:x2}],y", 0x37, 2) },
        { 0x3b, new Instruction("tsc", 0x3b) },
        { 0x3f, new Instruction("and ${0:x6},x", 0x3f, 4) },
        { 0x42, new Instruction("wdm", 0x42) },
        { 0x43, new Instruction("eor ${0:x2},s", 0x43, 2) },
        { 0x44, new Instruction("mvp ${1:x2},${0:x2}", 0x44, 1,1) },
        { 0x47, new Instruction("eor [${0:x2}]", 0x47, 2) },
        { 0x4b, new Instruction("phk", 0x4b) },
        { 0x4f, new Instruction("eor ${0:x6}", 0x4f, 4) },
        { 0x53, new Instruction("eor (${0:x2},s),y", 0x53, 2) },
        { 0x54, new Instruction("mvn ${1:x2},${0:x2}", 0x54, 1,1) },
        { 0x57, new Instruction("eor [${0:x2}],y", 0x57, 2) },
        { 0x5b, new Instruction("tcd", 0x5b) },
        { 0x5c, new Instruction("jmp ${0:x6}", 0x5c, 4) },
        { 0x5f, new Instruction("eor ${0:x6},x", 0x5f, 4) },
        { 0x62, new Instruction("per ${0:x4}", 0x62, 3, true, true) },
        { 0x63, new Instruction("adc ${0:x2},s", 0x63, 2) },
        { 0x67, new Instruction("adc [${0:x2}]", 0x67, 2) },
        { 0x6b, new Instruction("rtl", 0x6b) },
        { 0x6f, new Instruction("adc ${0:x6}", 0x6f, 4) },
        { 0x73, new Instruction("adc (${0:x2},s),y", 0x73, 2) },
        { 0x77, new Instruction("adc [${0:x2}],y", 0x77, 2) },
        { 0x7b, new Instruction("tdc", 0x7b) },
        { 0x7f, new Instruction("adc ${0:x6},x", 0x7f, 4) },
        { 0x82, new Instruction("brl ${0:x4}", 0x82, 3, true, true) },
        { 0x83, new Instruction("sta ${0:x2},s", 0x83, 2) },
        { 0x87, new Instruction("sta [${0:x2}]", 0x87, 2) },
        { 0x8b, new Instruction("phb", 0x8b) },
        { 0x8f, new Instruction("sta ${0:x6}", 0x8f, 4) },
        { 0x93, new Instruction("sta (${0:x2},s),y", 0x93, 2) },
        { 0x97, new Instruction("sta [${0:x2}],y", 0x97, 2) },
        { 0x9b, new Instruction("txy", 0x9b) },
        { 0x9f, new Instruction("sta ${0:x6},x", 0x9f, 4) },
        { 0xa3, new Instruction("lda ${0:x2},s", 0xa3, 2) },
        { 0xa7, new Instruction("lda [${0:x2}]", 0xa7, 2) },
        { 0xab, new Instruction("plb", 0xab) },
        { 0xaf, new Instruction("lda ${0:x6}", 0xaf, 4) },
        { 0xb3, new Instruction("lda (${0:x2},s),y", 0xb3, 2) },
        { 0xb7, new Instruction("lda [${0:x2}],y", 0xb7, 2) },
        { 0xbb, new Instruction("tyx", 0xbb) },
        { 0xbf, new Instruction("lda ${0:x6},x", 0xbf, 4) },
        { 0xc2, new Instruction("rep #${0:x2}", 0xc2, 2) },
        { 0xc3, new Instruction("cmp ${0:x2},s", 0xc3, 2) },
        { 0xc7, new Instruction("cmp [${0:x2}]", 0xc7, 2) },
        { 0xcf, new Instruction("cmp ${0:x6}", 0xcf, 4) },
        { 0xd3, new Instruction("cmp (${0:x2},s),y", 0xd3, 2) },
        { 0xd4, new Instruction("pei (${0:x2})", 0xd4, 2) },
        { 0xd7, new Instruction("cmp [${0:x2}],y", 0xd7, 2) },
        { 0xdc, new Instruction("jmp [${0:x4}]", 0xdc, 3) },
        { 0xdf, new Instruction("cmp ${0:x6},x", 0xdf, 4) },
        { 0xe2, new Instruction("sep #${0:x2}", 0xe2, 2) },
        { 0xe3, new Instruction("sbc ${0:x2},s", 0xe3, 2) },
        { 0xe7, new Instruction("sbc [${0:x2}]", 0xe7, 2) },
        { 0xeb, new Instruction("xba", 0xeb) },
        { 0xef, new Instruction("sbc ${0:x6}", 0xef, 4) },
        { 0xf3, new Instruction("sbc (${0:x2},s),y", 0xf3, 2) },
        { 0xf4, new Instruction("pea ${0:x4}", 0xf4, 3) },
        { 0xfb, new Instruction("xce", 0xfb) },
        { 0xfc, new Instruction("jsr (${0:x4},x)", 0xfc, 3) },
        { 0xff, new Instruction("sbc ${0:x6},x", 0xff, 4) },
    };

    // s_65c02

    private static readonly Dictionary<int, Instruction> s_65c02Absolute = new()
    {
        { SyntaxParser.TSB, new Instruction("tsb ${0:x4}", 0x0c, 3) },
        { SyntaxParser.TRB, new Instruction("trb ${0:x4}", 0x1c, 3) },
        { SyntaxParser.STZ, new Instruction("stz ${0:x4}", 0x9c, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_65c02AbsoluteX = new()
    {
        { SyntaxParser.BIT, new Instruction("bit ${0:x4},x", 0x3c, 3) },
        { SyntaxParser.STZ, new Instruction("stz ${0:x4},x", 0x9e, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_65c02Immediate = new()
    {
        { SyntaxParser.BIT, new Instruction("bit #${0:x2}", 0x89, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_65c02Implied = new()
    {
        { SyntaxParser.INC, new Instruction("inc", 0x1a) },
        { SyntaxParser.DEC, new Instruction("dec", 0x3a) },
        { SyntaxParser.PHY, new Instruction("phy", 0x5a) },
        { SyntaxParser.PLY, new Instruction("ply", 0x7a) },
        { SyntaxParser.PHX, new Instruction("phx", 0xda) },
        { SyntaxParser.PLX, new Instruction("plx", 0xfa) },
    };

    private static readonly Dictionary<int, Instruction> s_65c02Accumulator = new()
    {
        { SyntaxParser.INC, new Instruction("inc", 0x1a) },
        { SyntaxParser.DEC, new Instruction("dec", 0x3a) }
    };

    private static readonly Dictionary<int, Instruction> s_65c02IndAbsX = new()
    {
        { SyntaxParser.JMP, new Instruction("jmp (${0:x4},x)", 0x7c, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_65c02IndZp = new()
    {
        { SyntaxParser.ORA, new Instruction("ora (${0:x2})", 0x12, 2) },
        { SyntaxParser.AND, new Instruction("and (${0:x2})", 0x32, 2) },
        { SyntaxParser.EOR, new Instruction("eor (${0:x2})", 0x52, 2) },
        { SyntaxParser.ADC, new Instruction("adc (${0:x2})", 0x72, 2) },
        { SyntaxParser.STA, new Instruction("sta (${0:x2})", 0x92, 2) },
        { SyntaxParser.LDA, new Instruction("lda (${0:x2})", 0xb2, 2) },
        { SyntaxParser.CMP, new Instruction("cmp (${0:x2})", 0xd2, 2) },
        { SyntaxParser.SBC, new Instruction("sbc (${0:x2})", 0xf2, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_65c02Relative = new()
    {
        { SyntaxParser.BRA, new Instruction("bra ${0:x4}", 0x80, 2, true) },
    };

    private static readonly Dictionary<int, Instruction> s_65c02ZeroPage = new()
    {
        { SyntaxParser.TSB, new Instruction("tsb ${0:x2}", 0x04, 2) },
        { SyntaxParser.TRB, new Instruction("trb ${0:x2}", 0x14, 2) },
        { SyntaxParser.STZ, new Instruction("stz ${0:x2}", 0x64, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_65c02ZeroPageX = new()
    {
        { SyntaxParser.BIT, new Instruction("bit ${0:x2},x", 0x34, 2) },
        { SyntaxParser.STZ, new Instruction("stz ${0:x2},x", 0x74, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_65c02AllOpcodes = new()
    {
        { 0x04, new Instruction("tsb ${0:x2}", 0x04, 2) },
        { 0x0c, new Instruction("tsb ${0:x4}", 0x0c, 3) },
        { 0x12, new Instruction("ora (${0:x2})", 0x12, 2) },
        { 0x14, new Instruction("trb ${0:x2}", 0x14, 2) },
        { 0x1a, new Instruction("inc", 0x1a) },
        { 0x1c, new Instruction("trb ${0:x4}", 0x1c, 3) },
        { 0x32, new Instruction("and (${0:x2})", 0x32, 2) },
        { 0x34, new Instruction("bit ${0:x2},x", 0x34, 2) },
        { 0x3a, new Instruction("dec", 0x3a) },
        { 0x3c, new Instruction("bit ${0:x4},x", 0x3c, 3) },
        { 0x52, new Instruction("eor (${0:x2})", 0x52, 2) },
        { 0x5a, new Instruction("phy", 0x5a) },
        { 0x64, new Instruction("stz ${0:x2}", 0x64, 2) },
        { 0x72, new Instruction("adc (${0:x2})", 0x72, 2) },
        { 0x74, new Instruction("stz ${0:x2},x", 0x74, 2) },
        { 0x7a, new Instruction("ply", 0x7a) },
        { 0x7c, new Instruction("jmp (${0:x4},x)", 0x7c, 3) },
        { 0x80, new Instruction("bra ${0:x4}", 0x80, 2, true) },
        { 0x89, new Instruction("bit #${0:x2}", 0x89, 2) },
        { 0x92, new Instruction("sta (${0:x2})", 0x92, 2) },
        { 0x9c, new Instruction("stz ${0:x4}", 0x9c, 3) },
        { 0x9e, new Instruction("stz ${0:x4},x", 0x9e, 3) },
        { 0xb2, new Instruction("lda (${0:x2})", 0xb2, 2) },
        { 0xd2, new Instruction("cmp (${0:x2})", 0xd2, 2) },
        { 0xda, new Instruction("phx", 0xda) },
        { 0xf2, new Instruction("sbc (${0:x2})", 0xf2, 2) },
        { 0xfa, new Instruction("plx", 0xfa) },
    };

    // s_65ce02

    private static readonly Dictionary<int, Instruction> s_65ce02Absolute = new()
    {
        { SyntaxParser.BSR, new Instruction("bsr ${0:x4}", 0x63, 3) },
        { SyntaxParser.LDZ, new Instruction("ldz ${0:x4}", 0xab, 3) },
        { SyntaxParser.ASW, new Instruction("asw ${0:x4}", 0xcb, 3) },
        { SyntaxParser.CPZ, new Instruction("cpz ${0:x4}", 0xdc, 3) },
        { SyntaxParser.INW, new Instruction("inw ${0:x4}", 0xe3, 3) },
        { SyntaxParser.ROW, new Instruction("row ${0:x4}", 0xeb, 3) },
        { SyntaxParser.PHW, new Instruction("phw ${0:x4}", 0xfc, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_65ce02AbsoluteX = new()
    {
        { SyntaxParser.STY, new Instruction("sty ${0:x4},x", 0x8b, 3) },
        { SyntaxParser.LDZ, new Instruction("ldz ${0:x4},x", 0xbb, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_65ce02AbsoluteY = new()
    {
        { SyntaxParser.STX, new Instruction("stx ${0:x4},y", 0x9b, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_65ce02ImmAbs = new()
    {
        { SyntaxParser.PHW, new Instruction("phw #${0:x4}", 0xf4, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_65ce02Immediate = new()
    {
        { SyntaxParser.RTS, new Instruction("rts #${0:x2}", 0x62, 2) },
        { SyntaxParser.LDZ, new Instruction("ldz #${0:x2}", 0xa3, 2) },
        { SyntaxParser.CPZ, new Instruction("cpz #${0:x2}", 0xc2, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_65ce02Implied = new()
    {
        { SyntaxParser.CLE, new Instruction("cle", 0x02) },
        { SyntaxParser.SEE, new Instruction("see", 0x03) },
        { SyntaxParser.TSY, new Instruction("tsy", 0x0b) },
        { SyntaxParser.INZ, new Instruction("inz", 0x1b) },
        { SyntaxParser.TYS, new Instruction("tys", 0x2b) },
        { SyntaxParser.DEZ, new Instruction("dez", 0x3b) },
        { SyntaxParser.NEG, new Instruction("neg", 0x42) },
        { SyntaxParser.ASR, new Instruction("asr", 0x43) },
        { SyntaxParser.TAZ, new Instruction("taz", 0x4b) },
        { SyntaxParser.TAB, new Instruction("tab", 0x5b) },
        { SyntaxParser.RTN, new Instruction("rtn", 0x63) },
        { SyntaxParser.TZA, new Instruction("tza", 0x6b) },
        { SyntaxParser.TBA, new Instruction("tba", 0x7b) },
        { SyntaxParser.PHZ, new Instruction("phz", 0xdb) },
        { SyntaxParser.PLZ, new Instruction("plz", 0xfb) },
    };

    private static readonly Dictionary<int, Instruction> s_65ce02IndAbs = new()
    {
        { SyntaxParser.JSR, new Instruction("jsr (${0:x4})", 0x22, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_65ce02IndAbsX = new()
    {
        { SyntaxParser.JSR, new Instruction("jsr (${0:x4},x)", 0x23, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_65ce02IndSp = new()
    {
        { SyntaxParser.STA, new Instruction("sta (${0:x2},sp),y", 0x82, 2) },
        { SyntaxParser.LDA, new Instruction("lda (${0:x2},sp),y", 0xe2, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_65ce02IndZ = new()
    {
        { SyntaxParser.ORA, new Instruction("ora (${0:x2}),z", 0x12, 2) },
        { SyntaxParser.AND, new Instruction("and (${0:x2}),z", 0x32, 2) },
        { SyntaxParser.EOR, new Instruction("eor (${0:x2}),z", 0x52, 2) },
        { SyntaxParser.ADC, new Instruction("adc (${0:x2}),z", 0x72, 2) },
        { SyntaxParser.STA, new Instruction("sta (${0:x2}),z", 0x92, 2) },
        { SyntaxParser.LDA, new Instruction("lda (${0:x2}),z", 0xb2, 2) },
        { SyntaxParser.CMP, new Instruction("cmp (${0:x2}),z", 0xd2, 2) },
        { SyntaxParser.SBC, new Instruction("sbc (${0:x2}),z", 0xf2, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_65ce02RelativeAbs = new()
    {
        { SyntaxParser.BPL, new Instruction("bpl ${0:x4}", 0x13, 3, true, true) },
        { SyntaxParser.BMI, new Instruction("bmi ${0:x4}", 0x33, 3, true, true) },
        { SyntaxParser.BVC, new Instruction("bvc ${0:x4}", 0x53, 3, true, true) },
        { SyntaxParser.BVS, new Instruction("bvs ${0:x4}", 0x73, 3, true, true) },
        { SyntaxParser.BRA, new Instruction("bra ${0:x4}", 0x83, 3, true, true) },
        { SyntaxParser.BCC, new Instruction("bcc ${0:x4}", 0x93, 3, true, true) },
        { SyntaxParser.BLT, new Instruction("blt ${0:x4}", 0x93, 3, true, true) },
        { SyntaxParser.BCS, new Instruction("bcs ${0:x4}", 0xb3, 3, true, true) },
        { SyntaxParser.BGE, new Instruction("bge ${0:x4}", 0xb3, 3, true, true) },
        { SyntaxParser.BNE, new Instruction("bne ${0:x4}", 0xd3, 3, true, true) },
        { SyntaxParser.BEQ, new Instruction("beq ${0:x4}", 0xf3, 3, true, true) },
        { SyntaxParser.JPL, new Instruction("bpl ${0:x4}", 0x13, 3, true, true) },
        { SyntaxParser.JMI, new Instruction("bmi ${0:x4}", 0x33, 3, true, true) },
        { SyntaxParser.JVC, new Instruction("bvc ${0:x4}", 0x53, 3, true, true) },
        { SyntaxParser.JVS, new Instruction("bvs ${0:x4}", 0x73, 3, true, true) },
        { SyntaxParser.JCC, new Instruction("bcc ${0:x4}", 0x93, 3, true, true) },
        { SyntaxParser.JCS, new Instruction("bcs ${0:x4}", 0xb3, 3, true, true) },
        { SyntaxParser.JNE, new Instruction("bne ${0:x4}", 0xd3, 3, true, true) },
        { SyntaxParser.JEQ, new Instruction("jne ${0:x4}", 0xf3, 3, true, true) }
    };

    private static readonly Dictionary<int, Instruction> s_65ce02ZeroPage = new()
    {
        { SyntaxParser.ASR, new Instruction("asr ${0:x2}", 0x44, 2) },
        { SyntaxParser.DEW, new Instruction("dew ${0:x2}", 0xc3, 2) },
        { SyntaxParser.CPZ, new Instruction("cpz ${0:x2}", 0xd4, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_65ce02ZeroPageX = new()
    {
        { SyntaxParser.ASR, new Instruction("asr ${0:x2},x", 0x54, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_65ce02AllOpcodes = new()
    {
        { 0x02, new Instruction("cle", 0x02) },
        { 0x03, new Instruction("see", 0x03) },
        { 0x0b, new Instruction("tsy", 0x0b) },
        { 0x12, new Instruction("ora (${0:x2}),z", 0x12, 2) },
        { 0x13, new Instruction("bpl ${0:x4}", 0x13, 3, true, true) },
        { 0x1b, new Instruction("inz", 0x1b) },
        { 0x22, new Instruction("jsr (${0:x4})", 0x22, 3) },
        { 0x23, new Instruction("jsr (${0:x4},x)", 0x23, 3) },
        { 0x2b, new Instruction("tys", 0x2b) },
        { 0x32, new Instruction("and (${0:x2}),z", 0x32, 2) },
        { 0x33, new Instruction("bmi ${0:x4}", 0x33, 3, true, true) },
        { 0x3b, new Instruction("dez", 0x3b) },
        { 0x42, new Instruction("neg", 0x42) },
        { 0x43, new Instruction("asr", 0x43) },
        { 0x44, new Instruction("asr ${0:x2}", 0x44, 2) },
        { 0x4b, new Instruction("taz", 0x4b) },
        { 0x52, new Instruction("eor (${0:x2}),z", 0x52, 2) },
        { 0x53, new Instruction("bvc ${0:x4}", 0x53, 3, true, true) },
        { 0x54, new Instruction("asr ${0:x2},x", 0x54, 2) },
        { 0x5b, new Instruction("tab", 0x5b) },
        { 0x62, new Instruction("rts #${0:x2}", 0x62, 2) },
        { 0x63, new Instruction("bsr ${0:x4}", 0x63, 3) },
        { 0x6b, new Instruction("tza", 0x6b) },
        { 0x72, new Instruction("adc (${0:x2}),z", 0x72, 2) },
        { 0x73, new Instruction("bvs ${0:x4}", 0x73, 3, true, true) },
        { 0x7b, new Instruction("tba", 0x7b) },
        { 0x82, new Instruction("sta (${0:x2},sp),y", 0x82, 2) },
        { 0x83, new Instruction("bra ${0:x4}", 0x83, 3, true, true) },
        { 0x8b, new Instruction("sty ${0:x4},x", 0x8b, 3) },
        { 0x92, new Instruction("sta (${0:x2}),z", 0x92, 2) },
        { 0x93, new Instruction("blt ${0:x4}", 0x93, 3, true, true) },
        { 0x9b, new Instruction("stx ${0:x4},y", 0x9b, 3) },
        { 0xa3, new Instruction("ldz #${0:x2}", 0xa3, 2) },
        { 0xab, new Instruction("ldz ${0:x4}", 0xab, 3) },
        { 0xb2, new Instruction("lda (${0:x2}),z", 0xb2, 2) },
        { 0xb3, new Instruction("bge ${0:x4}", 0xb3, 3, true, true) },
        { 0xbb, new Instruction("ldz ${0:x4},x", 0xbb, 3) },
        { 0xc2, new Instruction("cpz #${0:x2}", 0xc2, 2) },
        { 0xc3, new Instruction("dew ${0:x2}", 0xc3, 2) },
        { 0xcb, new Instruction("asw ${0:x4}", 0xcb, 3) },
        { 0xd2, new Instruction("cmp (${0:x2}),z", 0xd2, 2) },
        { 0xd3, new Instruction("bne ${0:x4}", 0xd3, 3, true, true) },
        { 0xd4, new Instruction("cpz ${0:x2}", 0xd4, 2) },
        { 0xdb, new Instruction("phz", 0xdb) },
        { 0xdc, new Instruction("cpz ${0:x4}", 0xdc, 3) },
        { 0xe2, new Instruction("lda (${0:x2},sp),y", 0xe2, 2) },
        { 0xe3, new Instruction("inw ${0:x4}", 0xe3, 3) },
        { 0xeb, new Instruction("row ${0:x4}", 0xeb, 3) },
        { 0xf2, new Instruction("sbc (${0:x2}),z", 0xf2, 2) },
        { 0xf3, new Instruction("beq ${0:x4}", 0xf3, 3, true, true) },
        { 0xf4, new Instruction("phw #${0:x4}", 0xf4, 3) },
        { 0xfb, new Instruction("plz", 0xfb) },
        { 0xfc, new Instruction("phw ${0:x4}", 0xfc, 3) },
    };

    // s_c64dtv2

    private static readonly Dictionary<int, Instruction> s_c64dtv2Immediate = new()
    {
        { SyntaxParser.SAC, new Instruction("sac #${0:x2}", 0x32, 2) },
        { SyntaxParser.SIR, new Instruction("sir #${0:x2}", 0x42, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_c64dtv2Relative = new()
    {
        { SyntaxParser.BRA, new Instruction("bra ${0:x4}", 0x12, 2, true) },
    };

    private static readonly Dictionary<int, Instruction> s_c64dtv2AllOpcodes = new()
    {
        { 0x12, new Instruction("bra ${0:x4}", 0x12, 2) },
        { 0x32, new Instruction("sac #${0:x2}", 0x32, 2) },
        { 0x42, new Instruction("sir #${0:x2}", 0x42, 2) },
    };

    // s_huC6280

    private static readonly Dictionary<int, Instruction> s_huC6280Immediate = new()
    {
        { SyntaxParser.ST1, new Instruction("st1 #${0:x2}", 0x13, 2) },
        { SyntaxParser.ST2, new Instruction("st2 #${0:x2}", 0x23, 2) },
        { SyntaxParser.TMA, new Instruction("tma #${0:x2}", 0x43, 2) },
        { SyntaxParser.TAM, new Instruction("tam #${0:x2}", 0x53, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_huC6280Implied = new()
    {
        { SyntaxParser.SXY, new Instruction("sxy", 0x03) },
        { SyntaxParser.SAX, new Instruction("sax", 0x23) },
        { SyntaxParser.SAY, new Instruction("say", 0x43) },
        { SyntaxParser.CSL, new Instruction("csl", 0x54) },
        { SyntaxParser.CLA, new Instruction("cla", 0x62) },
        { SyntaxParser.CLX, new Instruction("clx", 0x82) },
        { SyntaxParser.CLY, new Instruction("cly", 0xc2) },
        { SyntaxParser.CSH, new Instruction("csh", 0xd4) },
        { SyntaxParser.SET, new Instruction("set", 0xf4) },
    };

    private static readonly Dictionary<int, Instruction> s_huC6280IndAbs = new()
    {
        { SyntaxParser.ORA, new Instruction("ora (${0:x4})", 0x12, 3) },
        { SyntaxParser.AND, new Instruction("and (${0:x4})", 0x32, 3) },
        { SyntaxParser.EOR, new Instruction("eor (${0:x4})", 0x52, 3) },
        { SyntaxParser.ADC, new Instruction("adc (${0:x4})", 0x72, 3) },
        { SyntaxParser.STA, new Instruction("sta (${0:x4})", 0x92, 3) },
        { SyntaxParser.LDA, new Instruction("lda (${0:x4})", 0xb2, 3) },
        { SyntaxParser.CMP, new Instruction("cmp (${0:x4})", 0xd2, 3) },
        { SyntaxParser.SBC, new Instruction("sbc (${0:x4})", 0xf2, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_huC6280TestBitAbs = new()
    {
        { SyntaxParser.TST, new Instruction("tst #${0:x2},${1:x4}", 0x93, 1,2) },
    };

    private static readonly Dictionary<int, Instruction> s_huC6280TestBitAbsX = new()
    {
        { SyntaxParser.TST, new Instruction("tst #${0:x2},${1:x4},x", 0xb3, 1,2) },
    };

    private static readonly Dictionary<int, Instruction> s_huC6280TestBitZp = new()
    {
        { SyntaxParser.TST, new Instruction("tst #${0:x2},${1:x2}", 0x83, 1,1) },
    };

    private static readonly Dictionary<int, Instruction> s_huC6280TestBitZpX = new()
    {
        { SyntaxParser.TST, new Instruction("tst #${0:x2},${1:x2},x", 0xa3, 1,1) },
    };

    private static readonly Dictionary<int, Instruction> s_huC6280ThreeOpAbs = new()
    {
        { SyntaxParser.TII, new Instruction("tii ${0:x4},${1:x4},${2:x4}", 0x73, 2,2,2) },
        { SyntaxParser.TDD, new Instruction("tdd ${0:x4},${1:x4},${2:x4}", 0xc3, 2,2,2) },
        { SyntaxParser.TIN, new Instruction("tin ${0:x4},${1:x4},${2:x4}", 0xd3, 2,2,2) },
        { SyntaxParser.TIA, new Instruction("tia ${0:x4},${1:x4},${2:x4}", 0xe3, 2,2,2) },
        { SyntaxParser.TAI, new Instruction("tai ${0:x4},${1:x4},${2:x4}", 0xf3, 2,2,2) },
    };

    private static readonly Dictionary<int, Instruction> s_huC6280AllOpcodes = new()
    {
        { 0x03, new Instruction("sxy", 0x03) },
        { 0x12, new Instruction("ora (${0:x4})", 0x12, 3) },
        { 0x13, new Instruction("st1 #${0:x2}", 0x13, 2) },
        { 0x23, new Instruction("st2 #${0:x2}", 0x23, 2) },
        { 0x32, new Instruction("and (${0:x4})", 0x32, 3) },
        { 0x43, new Instruction("tma #${0:x2}", 0x43, 2) },
        { 0x52, new Instruction("eor (${0:x4})", 0x52, 3) },
        { 0x53, new Instruction("tam #${0:x2}", 0x53, 2) },
        { 0x54, new Instruction("csl", 0x54) },
        { 0x62, new Instruction("cla", 0x62) },
        { 0x72, new Instruction("adc (${0:x4})", 0x72, 3) },
        { 0x73, new Instruction("tii ${0:x4},${1:x4},${2:x4}", 0x73, 2,2,2) },
        { 0x82, new Instruction("clx", 0x82) },
        { 0x83, new Instruction("tst #${0:x2},${1:x2}", 0x83, 1,1) },
        { 0x92, new Instruction("sta (${0:x4})", 0x92, 3) },
        { 0x93, new Instruction("tst #${0:x2},${1:x4}", 0x93, 1,2) },
        { 0xa3, new Instruction("tst #${0:x2},${1:x2},x", 0xa3, 1,1) },
        { 0xb2, new Instruction("lda (${0:x4})", 0xb2, 3) },
        { 0xb3, new Instruction("tst #${0:x2},${1:x4},x", 0xb3, 1,2) },
        { 0xc2, new Instruction("cly", 0xc2) },
        { 0xc3, new Instruction("tdd ${0:x4},${1:x4},${2:x4}", 0xc3, 2,2,2) },
        { 0xd2, new Instruction("cmp (${0:x4})", 0xd2, 3) },
        { 0xd3, new Instruction("tin ${0:x4},${1:x4},${2:x4}", 0xd3, 2,2,2) },
        { 0xd4, new Instruction("csh", 0xd4) },
        { 0xe3, new Instruction("tia ${0:x4},${1:x4},${2:x4}", 0xe3, 2,2,2) },
        { 0xf2, new Instruction("sbc (${0:x4})", 0xf2, 3) },
        { 0xf3, new Instruction("tai ${0:x4},${1:x4},${2:x4}", 0xf3, 2,2,2) },
        { 0xf4, new Instruction("set", 0xf4) },
    };

    // s_m65

    private static readonly Dictionary<int, Instruction> s_m65Absolute = new()
    {
        { SyntaxParser.ORQ, new Instruction("orq ${0:x4}", 0x0d4242, 5) },
        { SyntaxParser.ASLQ, new Instruction("aslq ${0:x4}", 0x0f4242, 5) },
        { SyntaxParser.BITQ, new Instruction("bitq ${0:x4}", 0x2c4242, 5) },
        { SyntaxParser.ANDQ, new Instruction("andq ${0:x4}", 0x2d4242, 5) },
        { SyntaxParser.ROLQ, new Instruction("rolq ${0:x4}", 0x2e4242, 5) },
        { SyntaxParser.EORQ, new Instruction("eorq ${0:x4}", 0x4d4242, 5) },
        { SyntaxParser.LSRQ, new Instruction("lsrq ${0:x4}", 0x4e4242, 5) },
        { SyntaxParser.ADCQ, new Instruction("adcq ${0:x4}", 0x6d4242, 5) },
        { SyntaxParser.RORQ, new Instruction("rorq ${0:x4}", 0x6e4242, 5) },
        { SyntaxParser.STQ, new Instruction("stq ${0:x4}", 0x8d4242, 5) },
        { SyntaxParser.LDQ, new Instruction("ldq ${0:x4}", 0xad4242, 5) },
        { SyntaxParser.CPQ, new Instruction("cpq ${0:x4}", 0xcd4242, 5) },
        { SyntaxParser.DEQ, new Instruction("deq ${0:x4}", 0xce4242, 5) },
        { SyntaxParser.SBCQ, new Instruction("sbcq ${0:x4}", 0xed4242, 5) },
        { SyntaxParser.INQ, new Instruction("inq ${0:x4}", 0xee4242, 5) },
    };

    private static readonly Dictionary<int, Instruction> s_m65AbsoluteX = new()
    {
        { SyntaxParser.ASLQ, new Instruction("aslq ${0:x4},x", 0x1e4242, 5) },
        { SyntaxParser.ROLQ, new Instruction("rolq ${0:x4},x", 0x3e4242, 5) },
        { SyntaxParser.LSRQ, new Instruction("lsrq ${0:x4},x", 0x5e4242, 5) },
        { SyntaxParser.RORQ, new Instruction("rorq ${0:x4},x", 0x7e4242, 5) },
        { SyntaxParser.LDQ, new Instruction("ldq ${0:x4},x", 0xbd4242, 5) },
        { SyntaxParser.DEQ, new Instruction("deq ${0:x4},x", 0xde4242, 5) },
        { SyntaxParser.INQ, new Instruction("inq ${0:x4},x", 0xfe4242, 5) },
    };

    private static readonly Dictionary<int, Instruction> s_m65AbsoluteY = new()
    {
        { SyntaxParser.LDQ, new Instruction("ldq ${0:x4},y", 0xb94242, 5) },
    };

    private static readonly Dictionary<int, Instruction> s_m65Dir = new()
    {
        { SyntaxParser.ORQ, new Instruction("orq [${0:x2}]", 0x12ea4242, 5) },
        { SyntaxParser.ANDQ, new Instruction("andq [${0:x2}]", 0x32ea4242, 5) },
        { SyntaxParser.EORQ, new Instruction("eorq [${0:x2}]", 0x52ea4242, 5) },
        { SyntaxParser.ADCQ, new Instruction("adcq [${0:x2}]", 0x72ea4242, 5) },
        { SyntaxParser.STQ, new Instruction("stq [${0:x2}]", -1830141374, 5) },
        { SyntaxParser.LDQ, new Instruction("ldq [${0:x2}]", -1293270462, 5) },
        { SyntaxParser.CPQ, new Instruction("cpq [${0:x2}]", -756399550, 5) },
        { SyntaxParser.SBCQ, new Instruction("sbcq [${0:x2}]", -219528638, 5) }
    };

    private static readonly Dictionary<int, Instruction> s_m65DirZ = new()
    {
        { SyntaxParser.ORA, new Instruction("ora [${0:x2}],z", 0x12ea, 3) },
        { SyntaxParser.AND, new Instruction("and [${0:x2}],z", 0x32ea, 3) },
        { SyntaxParser.EOR, new Instruction("eor [${0:x2}],z", 0x52ea, 3) },
        { SyntaxParser.ADC, new Instruction("adc [${0:x2}],z", 0x72ea, 3) },
        { SyntaxParser.STA, new Instruction("sta [${0:x2}],z", 0x92ea, 3) },
        { SyntaxParser.LDA, new Instruction("lda [${0:x2}],z", 0xb2ea, 3) },
        { SyntaxParser.CMP, new Instruction("cmp [${0:x2}],z", 0xd2ea, 3) },
        { SyntaxParser.SBC, new Instruction("sbc [${0:x2}],z", 0xf2ea, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_m65Implied = new()
    {
        { SyntaxParser.MAP, new Instruction("map", 0x5c, 1) },
        { SyntaxParser.EOM, new Instruction("eom", 0xea, 1) },
        { SyntaxParser.ASLQ, new Instruction("aslq", 0x0a4242, 3) },
        { SyntaxParser.INQ, new Instruction("inq", 0x1a4242, 3) },
        { SyntaxParser.ROLQ, new Instruction("rolq", 0x2a4242, 3) },
        { SyntaxParser.DEQ, new Instruction("deq", 0x3a4242, 3) },
        { SyntaxParser.ASRQ, new Instruction("asrq", 0x434242, 3) },
        { SyntaxParser.LSRQ, new Instruction("lsrq", 0x4a4242, 3) },
        { SyntaxParser.RORQ, new Instruction("rorq", 0x6a4242, 3) },
    };

    private static readonly Dictionary<int, Instruction> s_m65IndS = new()
    {
        { SyntaxParser.LDQ, new Instruction("ldq (${0:x2},s),y", 0xe24242, 4) },
    };

    private static readonly Dictionary<int, Instruction> s_m65IndY = new()
    {
        { SyntaxParser.LDQ, new Instruction("ldq (${0:x2}),y", 0xb14242, 4) },
    };

    private static readonly Dictionary<int, Instruction> s_m65IndZp = new()
    {
        { SyntaxParser.ORQ, new Instruction("orq (${0:x2})", 0x124242, 4) },
        { SyntaxParser.ANDQ, new Instruction("andq (${0:x2})", 0x324242, 4) },
        { SyntaxParser.EORQ, new Instruction("eorq (${0:x2})", 0x524242, 4) },
        { SyntaxParser.ADCQ, new Instruction("adcq (${0:x2})", 0x724242, 4) },
        { SyntaxParser.STQ, new Instruction("stq (${0:x2})", 0x924242, 4) },
        { SyntaxParser.LDQ, new Instruction("ldq (${0:x2})", 0xb24242, 4) },
        { SyntaxParser.CPQ, new Instruction("cpq (${0:x2})", 0xd24242, 4) },
        { SyntaxParser.SBCQ, new Instruction("sbcq (${0:x2})", 0xf24242, 4) },
    };

    private static readonly Dictionary<int, Instruction> s_m65ZeroPage = new()
    {
        { SyntaxParser.ORQ, new Instruction("orq ${0:x2}", 0x054242, 4) },
        { SyntaxParser.ASLQ, new Instruction("aslq ${0:x2}", 0x064242, 4) },
        { SyntaxParser.BITQ, new Instruction("bitq ${0:x2}", 0x244242, 4) },
        { SyntaxParser.ANDQ, new Instruction("andq ${0:x2}", 0x254242, 4) },
        { SyntaxParser.ROLQ, new Instruction("rolq ${0:x2}", 0x264242, 4) },
        { SyntaxParser.ASRQ, new Instruction("asrq ${0:x2}", 0x444242, 4) },
        { SyntaxParser.EORQ, new Instruction("eorq ${0:x2}", 0x454242, 4) },
        { SyntaxParser.LSRQ, new Instruction("lsrq ${0:x2}", 0x464242, 4) },
        { SyntaxParser.ADCQ, new Instruction("adcq ${0:x2}", 0x654242, 4) },
        { SyntaxParser.RORQ, new Instruction("rorq ${0:x2}", 0x664242, 4) },
        { SyntaxParser.STQ, new Instruction("stq ${0:x2}", 0x854242, 4) },
        { SyntaxParser.LDQ, new Instruction("ldq ${0:x2}", 0xa54242, 4) },
        { SyntaxParser.CPQ, new Instruction("cpq ${0:x2}", 0xc54242, 4) },
        { SyntaxParser.DEQ, new Instruction("deq ${0:x2}", 0xc64242, 4) },
        { SyntaxParser.SBCQ, new Instruction("sbcq ${0:x2}", 0xe54242, 4) },
        { SyntaxParser.INQ, new Instruction("inq ${0:x2}", 0xe64242, 4) },
    };

    private static readonly Dictionary<int, Instruction> s_m65ZeroPageX = new()
    {
        { SyntaxParser.ASLQ, new Instruction("aslq ${0:x2},x", 0x164242, 4) },
        { SyntaxParser.ASRQ, new Instruction("asrq ${0:x2},x", 0x544242, 4) },
        { SyntaxParser.LSRQ, new Instruction("lsrq ${0:x2},x", 0x564242, 4) },
        { SyntaxParser.RORQ, new Instruction("rorq ${0:x2},x", 0x764242, 4) },
        { SyntaxParser.LDQ, new Instruction("ldq ${0:x2},x", 0xb54242, 4) },
        { SyntaxParser.DEQ, new Instruction("deq ${0:x2},x", 0xd64242, 4) },
        { SyntaxParser.INQ, new Instruction("inq ${0:x2},x", 0xf64242, 4) },
    };

    private static readonly Dictionary<int, Instruction> s_m65AllOpcodes = new()
    {
        { 0x054242, new Instruction("orq ${0:x2}", 0x054242, 4) },
        { 0x064242, new Instruction("aslq ${0:x2}", 0x064242, 4) },
        { 0x0a4242, new Instruction("aslq", 0x0a4242, 3) },
        { 0x0d4242, new Instruction("orq ${0:x4}", 0x0d4242, 5) },
        { 0x0f4242, new Instruction("aslq ${0:x4}", 0x0f4242, 5) },
        { 0x124242, new Instruction("orq (${0:x2})", 0x124242, 4) },
        { 0x12ea, new Instruction("ora [${0:x2}],z", 0x12ea, 3) },
        { 0x12ea4242, new Instruction("orq [${0:x2}]", 0x12ea4242, 5) },
        { 0x164242, new Instruction("aslq ${0:x2},x", 0x164242, 4) },
        { 0x1a4242, new Instruction("inq", 0x1a4242, 3) },
        { 0x1e4242, new Instruction("aslq ${0:x4},x", 0x1e4242, 5) },
        { 0x244242, new Instruction("bitq ${0:x2}", 0x244242, 4) },
        { 0x254242, new Instruction("andq ${0:x2}", 0x254242, 4) },
        { 0x264242, new Instruction("rolq ${0:x2}", 0x264242, 4) },
        { 0x2a4242, new Instruction("rolq", 0x2a4242, 3) },
        { 0x2c4242, new Instruction("bitq ${0:x4}", 0x2c4242, 5) },
        { 0x2d4242, new Instruction("andq ${0:x4}", 0x2d4242, 5) },
        { 0x2e4242, new Instruction("rolq ${0:x4}", 0x2e4242, 5) },
        { 0x324242, new Instruction("andq (${0:x2})", 0x324242, 4) },
        { 0x32ea, new Instruction("and [${0:x2}],z", 0x32ea, 3) },
        { 0x32ea4242, new Instruction("andq [${0:x2}]", 0x32ea4242, 5) },
        { 0x3a4242, new Instruction("deq", 0x3a4242, 3) },
        { 0x3e4242, new Instruction("rolq ${0:x4},x", 0x3e4242, 5) },
        { 0x434242, new Instruction("asrq", 0x434242, 3) },
        { 0x444242, new Instruction("asrq ${0:x2}", 0x444242, 4) },
        { 0x454242, new Instruction("eorq ${0:x2}", 0x454242, 4) },
        { 0x464242, new Instruction("lsrq ${0:x2}", 0x464242, 4) },
        { 0x4a4242, new Instruction("lsrq", 0x4a4242, 3) },
        { 0x4d4242, new Instruction("eorq ${0:x4}", 0x4d4242, 5) },
        { 0x4e4242, new Instruction("lsrq ${0:x4}", 0x4e4242, 5) },
        { 0x524242, new Instruction("eorq (${0:x2})", 0x524242, 4) },
        { 0x52ea, new Instruction("eor [${0:x2}],z", 0x52ea, 3) },
        { 0x52ea4242, new Instruction("eorq [${0:x2}]", 0x52ea4242, 5) },
        { 0x544242, new Instruction("asrq ${0:x2},x", 0x544242, 4) },
        { 0x564242, new Instruction("lsrq ${0:x2},x", 0x564242, 4) },
        { 0x5e4242, new Instruction("lsrq ${0:x4},x", 0x5e4242, 5) },
        { 0x654242, new Instruction("adcq ${0:x2}", 0x654242, 4) },
        { 0x664242, new Instruction("rorq ${0:x2}", 0x664242, 4) },
        { 0x6a4242, new Instruction("rorq", 0x6a4242, 3) },
        { 0x6d4242, new Instruction("adcq ${0:x4}", 0x6d4242, 5) },
        { 0x6e4242, new Instruction("rorq ${0:x4}", 0x6e4242, 5) },
        { 0x724242, new Instruction("adcq (${0:x2})", 0x724242, 4) },
        { 0x72ea, new Instruction("adc [${0:x2}],z", 0x72ea, 3) },
        { 0x72ea4242, new Instruction("adcq [${0:x2}]", 0x72ea4242, 5) },
        { 0x764242, new Instruction("rorq ${0:x2},x", 0x764242, 4) },
        { 0x7e4242, new Instruction("rorq ${0:x4},x", 0x7e4242, 5) },
        { 0x854242, new Instruction("stq ${0:x2}", 0x854242, 4) },
        { 0x8d4242, new Instruction("stq ${0:x4}", 0x8d4242, 5) },
        { 0x924242, new Instruction("stq (${0:x2})", 0x924242, 4) },
        { 0x92ea, new Instruction("sta [${0:x2}],z", 0x92ea, 3) },
        { -1830141374, new Instruction("stq [${0:x2}]", -1830141374, 5) },
        { 0xa54242, new Instruction("ldq ${0:x2}", 0xa54242, 4) },
        { 0xad4242, new Instruction("ldq ${0:x4}", 0xad4242, 5) },
        { 0xb14242, new Instruction("ldq (${0:x2}),y", 0xb14242, 4) },
        { 0xb24242, new Instruction("ldq (${0:x2})", 0xb24242, 4) },
        { 0xb2ea, new Instruction("lda [${0:x2}],z", 0xb2ea, 3) },
        { -1293270462, new Instruction("ldq [${0:x2}]", -1293270462, 5) },
        { 0xb54242, new Instruction("ldq ${0:x2},x", 0xb54242, 4) },
        { 0xb94242, new Instruction("ldq ${0:x4},y", 0xb94242, 5) },
        { 0xbd4242, new Instruction("ldq ${0:x4},x", 0xbd4242, 5) },
        { 0xc54242, new Instruction("cpq ${0:x2}", 0xc54242, 4) },
        { 0xc64242, new Instruction("deq ${0:x2}", 0xc64242, 4) },
        { 0xcd4242, new Instruction("cpq ${0:x4}", 0xcd4242, 5) },
        { 0xce4242, new Instruction("deq ${0:x4}", 0xce4242, 5) },
        { 0xd24242, new Instruction("cpq (${0:x2})", 0xd24242, 4) },
        { 0xd2ea, new Instruction("cmp [${0:x2}],z", 0xd2ea, 3) },
        {-756399550, new Instruction("cpq [${0:x2}]", -756399550, 5) },
        { 0xd64242, new Instruction("deq ${0:x2},x", 0xd64242, 4) },
        { 0xde4242, new Instruction("deq ${0:x4},x", 0xde4242, 5) },
        { 0xe24242, new Instruction("ldq (${0:x2},s),y", 0xe24242, 4) },
        { 0xe54242, new Instruction("sbcq ${0:x2}", 0xe54242, 4) },
        { 0xe64242, new Instruction("inq ${0:x2}", 0xe64242, 4) },
        { 0xed4242, new Instruction("sbcq ${0:x4}", 0xed4242, 5) },
        { 0xee4242, new Instruction("inq ${0:x4}", 0xee4242, 5) },
        { 0xf24242, new Instruction("sbcq (${0:x2})", 0xf24242, 4) },
        { 0xf2ea, new Instruction("sbc [${0:x2}],z", 0xf2ea, 3) },
        { -219528638, new Instruction("sbcq [${0:x2}", -219528638, 5) },
        { 0xf64242, new Instruction("inq ${0:x2},x", 0xf64242, 4) },
        { 0xfe4242, new Instruction("inq ${0:x4},x", 0xfe4242, 5) }
    };

    // s_r65c02

    private static readonly Dictionary<int, Instruction> s_r65c02ThreeOpRel0 = new()
    {
        { SyntaxParser.BBR, new Instruction("bbr 0,${0:x2},${1:x4}", 0x0f, true, 1,1) },
        { SyntaxParser.BBS, new Instruction("bbs 0,${0:x2},${1:x4}", 0x8f, true, 1,1) },
    };

    private static readonly Dictionary<int, Instruction> s_r65c02ThreeOpRel1 = new()
    {
        { SyntaxParser.BBR, new Instruction("bbr 1,${0:x2},${1:x4}", 0x1f, true, 1,1) },
        { SyntaxParser.BBS, new Instruction("bbs 1,${0:x2},${1:x4}", 0x9f, true, 1,1) },
    };

    private static readonly Dictionary<int, Instruction> s_r65c02ThreeOpRel2 = new()
    {
        { SyntaxParser.BBR, new Instruction("bbr 2,${0:x2},${1:x4}", 0x2f, true, 1,1) },
        { SyntaxParser.BBS, new Instruction("bbs 2,${0:x2},${1:x4}", 0xaf, true, 1,1) },
    };

    private static readonly Dictionary<int, Instruction> s_r65c02ThreeOpRel3 = new()
    {
        { SyntaxParser.BBR, new Instruction("bbr 3,${0:x2},${1:x4}", 0x3f, true, 1,1) },
        { SyntaxParser.BBS, new Instruction("bbs 3,${0:x2},${1:x4}", 0xbf, true, 1,1) },
    };

    private static readonly Dictionary<int, Instruction> s_r65c02ThreeOpRel4 = new()
    {
        { SyntaxParser.BBR, new Instruction("bbr 4,${0:x2},${1:x4}", 0x4f, true, 1,1) },
        { SyntaxParser.BBS, new Instruction("bbs 4,${0:x2},${1:x4}", 0xcf, true, 1,1) },
    };

    private static readonly Dictionary<int, Instruction> s_r65c02ThreeOpRel5 = new()
    {
        { SyntaxParser.BBR, new Instruction("bbr 5,${0:x2},${1:x4}", 0x5f, true, 1,1) },
        { SyntaxParser.BBS, new Instruction("bbs 5,${0:x2},${1:x4}", 0xdf, true, 1,1) },
    };

    private static readonly Dictionary<int, Instruction> s_r65c02ThreeOpRel6 = new()
    {
        { SyntaxParser.BBR, new Instruction("bbr 6,${0:x2},${1:x4}", 0x6f, true, 1,1) },
        { SyntaxParser.BBS, new Instruction("bbs 6,${0:x2},${1:x4}", 0xef, true, 1,1) },
    };

    private static readonly Dictionary<int, Instruction> s_r65c02ThreeOpRel7 = new()
    {
        { SyntaxParser.BBR, new Instruction("bbr 7,${0:x2},${1:x4}", 0x7f, true, 1,1) },
        { SyntaxParser.BBS, new Instruction("bbs 7,${0:x2},${1:x4}", 0xff, true, 1,1) },
    };

    private static readonly Dictionary<int, Instruction> s_r65c02Zp0 = new()
    {
        { SyntaxParser.RMB, new Instruction("rmb 0,${0:x2}", 0x07, 2) },
        { SyntaxParser.SMB, new Instruction("smb 0,${0:x2}", 0x87, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_r65c02Zp1 = new()
    {
        { SyntaxParser.RMB, new Instruction("rmb 1,${0:x2}", 0x17, 2) },
        { SyntaxParser.SMB, new Instruction("smb 1,${0:x2}", 0x97, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_r65c02Zp2 = new()
    {
        { SyntaxParser.RMB, new Instruction("rmb 2,${0:x2}", 0x27, 2) },
        { SyntaxParser.SMB, new Instruction("smb 2,${0:x2}", 0xa7, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_r65c02Zp3 = new()
    {
        { SyntaxParser.RMB, new Instruction("rmb 3,${0:x2}", 0x37, 2) },
        { SyntaxParser.SMB, new Instruction("smb 3,${0:x2}", 0xb7, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_r65c02Zp4 = new()
    {
        { SyntaxParser.RMB, new Instruction("rmb 4,${0:x2}", 0x47, 2) },
        { SyntaxParser.SMB, new Instruction("smb 4,${0:x2}", 0xc7, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_r65c02Zp5 = new()
    {
        { SyntaxParser.RMB, new Instruction("rmb 5,${0:x2}", 0x57, 2) },
        { SyntaxParser.SMB, new Instruction("smb 5,${0:x2}", 0xd7, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_r65c02Zp6 = new()
    {
        { SyntaxParser.RMB, new Instruction("rmb 6,${0:x2}", 0x67, 2) },
        { SyntaxParser.SMB, new Instruction("smb 6,${0:x2}", 0xe7, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_r65c02Zp7 = new()
    {
        { SyntaxParser.RMB, new Instruction("rmb 7,${0:x2}", 0x77, 2) },
        { SyntaxParser.SMB, new Instruction("smb 7,${0:x2}", 0xf7, 2) },
    };

    private static readonly Dictionary<int, Instruction> s_r65c02AllOpcodes = new()
    {
        { 0x07, new Instruction("rmb 0,${0:x2}", 0x07, 2) },
        { 0x0f, new Instruction("bbr 0,${0:x2},${1:x4}", 0x0f, true, 1,1) },
        { 0x17, new Instruction("rmb 1,${0:x2}", 0x17, 2) },
        { 0x1f, new Instruction("bbr 1,${0:x2},${1:x4}", 0x1f, true, 1,1) },
        { 0x27, new Instruction("rmb 2,${0:x2}", 0x27, 2) },
        { 0x2f, new Instruction("bbr 2,${0:x2},${1:x4}", 0x2f, true, 1,1) },
        { 0x37, new Instruction("rmb 3,${0:x2}", 0x37, 2) },
        { 0x3f, new Instruction("bbr 3,${0:x2},${1:x4}", 0x3f, true, 1,1) },
        { 0x47, new Instruction("rmb 4,${0:x2}", 0x47, 2) },
        { 0x4f, new Instruction("bbr 4,${0:x2},${1:x4}", 0x4f, true, 1,1) },
        { 0x57, new Instruction("rmb 5,${0:x2}", 0x57, 2) },
        { 0x5f, new Instruction("bbr 5,${0:x2},${1:x4}", 0x5f, true, 1,1) },
        { 0x67, new Instruction("rmb 6,${0:x2}", 0x67, 2) },
        { 0x6f, new Instruction("bbr 6,${0:x2},${1:x4}", 0x6f, true, 1,1) },
        { 0x77, new Instruction("rmb 7,${0:x2}", 0x77, 2) },
        { 0x7f, new Instruction("bbr 7,${0:x2},${1:x4}", 0x7f, true, 1,1) },
        { 0x87, new Instruction("smb 0,${0:x2}", 0x87, 2) },
        { 0x8f, new Instruction("bbs 0,${0:x2},${1:x4}", 0x8f, true, 1,1) },
        { 0x97, new Instruction("smb 1,${0:x2}", 0x97, 2) },
        { 0x9f, new Instruction("bbs 1,${0:x2},${1:x4}", 0x9f, true, 1,1) },
        { 0xa7, new Instruction("smb 2,${0:x2}", 0xa7, 2) },
        { 0xaf, new Instruction("bbs 2,${0:x2},${1:x4}", 0xaf, true, 1,1) },
        { 0xb7, new Instruction("smb 3,${0:x2}", 0xb7, 2) },
        { 0xbf, new Instruction("bbs 3,${0:x2},${1:x4}", 0xbf, true, 1,1) },
        { 0xc7, new Instruction("smb 4,${0:x2}", 0xc7, 2) },
        { 0xcf, new Instruction("bbs 4,${0:x2},${1:x4}", 0xcf, true, 1,1) },
        { 0xd7, new Instruction("smb 5,${0:x2}", 0xd7, 2) },
        { 0xdf, new Instruction("bbs 5,${0:x2},${1:x4}", 0xdf, true, 1,1) },
        { 0xe7, new Instruction("smb 6,${0:x2}", 0xe7, 2) },
        { 0xef, new Instruction("bbs 6,${0:x2},${1:x4}", 0xef, true, 1,1) },
        { 0xf7, new Instruction("smb 7,${0:x2}", 0xf7, 2) },
        { 0xff, new Instruction("bbs 7,${0:x2},${1:x4}", 0xff, true, 1,1) },
    };

    // s_w65c02

    private static readonly Dictionary<int, Instruction> s_w65c02Implied = new()
    {
        { SyntaxParser.WAI, new Instruction("wai", 0xcb) },
        { SyntaxParser.STP, new Instruction("stp", 0xdb) },
    };

    private static readonly Dictionary<int, Instruction> s_w65c02AllOpcodes = new()
    {
        { 0xcb, new Instruction("wai", 0xcb) },
        { 0xdb, new Instruction("stp", 0xdb) },
    };

}

