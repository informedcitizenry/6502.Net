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

using Sixty502DotNet.Shared.Error;
using Sixty502DotNet.Shared.Eval;
using System.Security.Cryptography;

namespace Sixty502DotNet.Shared.Compile;

public enum ByteOrder
{
    LittleEndian,
    BigEndian
}

public class Output(StringComparer comparer)
{
    private byte[] _bytes = new byte[Address.BankSize];

    private int _offset;

    private int _pc;

    private readonly SectionCollection _sections = new(comparer);

    public void Synch()
    {
        Synched = true;
        _pc = _offset;
    }

    public void Reset()
    {
        InitMemValues(0x00);
        Started = false;
        _sections.Reset();
        Synched = true;
        _pc = 0;
        _offset = 0;
        Start = 0;
        End = 0;
        ProgramCounterStart = 0;
        ProgramCounterEnd = 0;
    }

    public void Fill(int amount)
    {
        if (!AddressInSection(_offset + amount - 1))
        {
            throw new OutputException(OutputExceptionType.InvalidFillAmount);
        }
        _offset += amount;
        _pc += amount;
        if (_sections.SectionSelected)
        {
            _sections.SetOutputCount(_offset - _sections.SelectedStartAddress);
        }
    }

    public void Fill(int amount, long value, ByteOrder order)
    {
        if (!AddressInSection(_offset + amount - 1))
        {
            throw new OutputException(OutputExceptionType.InvalidFillAmount);
        }
        var size = value.Size();
        var fillBytes = BitConverter.GetBytes(value).Take(size).ToArray();
        var repeated = new List<byte>(amount);
        for (var i = 0; i < amount; i++)
            repeated.AddRange(fillBytes);
        EmitBytes(repeated.GetRange(0, amount).ToArray(), order);
    }
    
    public void Align(int amount)
    {
        amount = CalculateAlignment(amount);
        if (!AddressInSection(_offset + amount - 1))
        {
            throw new OutputException(OutputExceptionType.InvalidAlignAmount);
        }
        _offset += amount;
        _pc += amount;
    }

    public void Align(int amount, long value, ByteOrder order) 
        => Fill(CalculateAlignment(amount), value, order);
    
    public void EmitByte(byte b)
    {
        if (!Started)
        {
            if (_sections is { IsEmpty: false, SectionSelected: false })
            {
                throw new SectionException("One or more sections is defined but not selected");
            }
            Started = true;
            Start = _offset;
            ProgramCounterStart = _pc;
        }
        else if (Start > _offset && 
                 _sections.SectionSelected && 
                 _sections.SelectedStartAddress == _offset)
        {
            Start = _offset;
        }
        if (ProgramCounterStart > _pc)
        {
            ProgramCounterStart = _pc;
        }
        if (_offset >= Address.MaxAddress || 
            (_sections.SectionSelected && !_sections.AddressInBounds(_offset)))
        {
            throw new OutputException(OutputExceptionType.AddressOverflow);
        }
        while (_bytes.Length <= _offset)
        {
            Array.Resize(ref _bytes, _bytes.Length * 2);
        }
        _bytes[_offset++] = (byte)(b ^ Xor);
        _pc++;
        if (End < _offset) 
        {
            End = _offset;
        }
        if (ProgramCounterEnd < _pc)
        {
            ProgramCounterEnd = _pc;
        }
        if (_sections.SectionSelected)
        {
            _sections.SetOutputCount(_offset - _sections.SelectedStartAddress);
        }
    }
    
    public void EmitBytes(byte[] bytes, ByteOrder order)
    {
        if ((BitConverter.IsLittleEndian && order == ByteOrder.BigEndian) || 
           (!BitConverter.IsLittleEndian && order == ByteOrder.LittleEndian))
        {
            bytes = bytes.Reverse().ToArray();
        }
        for(var i = 0; i < bytes.Length; i++)
            EmitByte(bytes[i]);
    }

    private void EmitBytes(byte[] bytes, int size, ByteOrder order)
    {
        if (size == 0) return;
        EmitBytes(bytes.Take(size).ToArray(), order);
    }

    public void EmitValue(int value, ByteOrder order)
        => EmitBytes(BitConverter.GetBytes(value), value.Size(), order);
    
    public void EmitValue(long value, ByteOrder order)
        => EmitValueSized(value, value.Size(), order);
    
    public void EmitValueSized(long value, int sized, ByteOrder order)
        => EmitBytes(BitConverter.GetBytes(value), sized, order);

    public void EmitIeee754Value(double value, ByteOrder order)
    {
        var bytes = BitConverter.GetBytes(value);
        EmitBytes(bytes, order);
    }
    
    public void DefineSection(string name, int starts, int ends)
    {
        if (Started || _sections.SectionSelected)
            throw new SectionException("Cannot define a section after assembly has started");
        if (starts < 0)
            throw new SectionException($"Section {name} start address {starts} is not valid");
        if (starts >= ends)
            throw new SectionException($"Section {name} start address cannot be equal or greater than end address");
        if (ends > 0x10000)
            throw new SectionException($"Section {name} end address {ends} is not valid");
        switch (_sections.Add(name, starts, ends))
        {
            case SectionResult.Duplicate:
                throw new SectionException($"Section {name} already defined");
            case SectionResult.RangeOverlap:
                throw new SectionException($"Section {name} start and end address intersect existing section's");
        }
    }

    public void PokeInAssembledSpace(int address, byte value)
    {
        address = GetEffectiveAddress(address);
        if (!AddressInCodespace(address) || address < Start || address >= End)
        {
            throw new OutputException(OutputExceptionType.InvalidPokeAddress);
        }
        _bytes[address] = value;
    }

    public void Poke(int address, byte value)
    {
        address = GetEffectiveAddress(address);
        if (address > Address.MaxAddress)
        {
            throw new OutputException(OutputExceptionType.InvalidPokeAddress);
        }
        _bytes[address] = value;
    }

    
    public byte PeekInAssembledSpace(int address)
    {
        address = GetEffectiveAddress(address);
        return AddressInCodespace(address) && address >= Start && address < End
            ? _bytes[address]
            : throw new OutputException(OutputExceptionType.InvalidPeekAddress);
    }
    
    
    public byte Peek(int address)
    {
        address = GetEffectiveAddress(address);
        return address <= Address.MaxAddress
            ? _bytes[address]
            : throw new OutputException(OutputExceptionType.InvalidPeekAddress);
    }

    public int GetSectionOrProgramStart() 
        => _sections.SectionSelected ? _sections.SelectedStartAddress : Start;


    public void SetSection(string section)
    {
        var result = _sections.SetCurrentSection(section);
        if (result == SectionResult.NotFound)
            throw new SectionException($"Section `{section}` is not defined");
        _pc = _offset = _sections.SelectedStartAddress + _sections.GetSectionOutputCount();
    }

    private string GetOutputHash() => GetHashForOutput(Start, End - Start);
    
    public string GetOutputHash(string? section)
    {
        if (string.IsNullOrEmpty(section))
            return GetOutputHash();
        int count;
        if (_sections.SetCurrentSection(section) == SectionResult.NotFound ||
            (count = _sections.GetSectionOutputCount()) == -1)
            throw new OutputException(OutputExceptionType.NoObjectBytesForSection);
        return GetHashForOutput(_sections.SelectedStartAddress, count);
    }
    
    private string GetHashForOutput(int start, int count)
    {
        using var md5 = MD5.Create();
        // include start address in the computed hash
        var list = new List<byte>
        {
            (byte)(start & 0xff),
            (byte)((start & 0xff) / 256)
        };
        list.AddRange(_bytes);
        using var stream = new MemoryStream(list.ToArray(), start, count);
        var hash = BitConverter.ToString(md5.ComputeHash(stream));
        return hash.Replace("-", string.Empty).ToLower();
    }
    
    public override string ToString() => $"Start:    ${Start:X4}\nEnd:      ${End:X4}";

    
    public IList<string> GetUnusedSections() => _sections.GetUnusedSections();

    public ReadOnlySpan<byte> GetCompilation()
        => _bytes.Skip(Start)
            .Take(End - Start)
            .Select(b => (byte)(b ^ Xor))
            .ToArray();

    public ReadOnlySpan<byte> GetCompilation(string section)
    {
        if (_sections.SetCurrentSection(section) == SectionResult.NotFound)
        {
            throw new OutputException(OutputExceptionType.SectionNotFound);
        }
        return _bytes
            .Skip(_sections.GetSectionStart(section))
            .Take(_sections.GetSectionOutputCount())
            .ToArray();
    }

    public ReadOnlySpan<byte> BytesFrom(int offset)
    {
        if (!Started || offset < Start || offset >= End) return [];
        int range;
        if (!_sections.SectionSelected || _sections.AddressInBounds(End))
        {
            range = End - offset;
        }
        else
        {
            range = _offset - offset;
        }
        return range is < 0 or > Address.MaxAddress ? [] : _bytes.AsSpan(offset, range);
    }

    public int SectionStart(string section) => _sections.GetSectionStart(section);
    
    private int CalculateAlignment(int amount)
    {
        if (amount < 0) throw new OutputException(OutputExceptionType.InvalidAlignAmount);
        var align = 0;
        while ((_offset + align) % amount != 0)
        {
            align++;
            if (align + _offset >= _bytes.Length ||
                align + _pc >= _bytes.Length)
            {
                throw new OutputException(OutputExceptionType.InvalidAlignAmount);
            }
        }
        return align;
    }

    private bool AddressInCodespace(int address) =>
        address < _bytes.Length && AddressValid(address);

    private bool AddressValid(int address) =>
        (!Started || (address >= Start && address <= End)) &&
        AddressInSection(address);

    private bool AddressInSection(int address) =>
        address >= 0 &&
        (_sections.IsEmpty ||
         !_sections.SectionSelected ||
         _sections.AddressInBounds(address));
    
    private int GetEffectiveAddress(int address)
    {
        if (ProgramCounter == Offset) return address;
        var diff = Offset - ProgramCounter;
        return address - diff;
    }

    private void InitMemValues(byte value)
    {
        Array.Fill(_bytes, value, 0, Start);
        Array.Fill(_bytes, value, End, _bytes.Length - End);
    }
    
    public byte InitMem
    {
        set => InitMemValues(value);
    }

    public int Start { get; private set; }
    
    public int ProgramCounterStart { get; private set; }

    public int End { get; private set; }
    
    public int ProgramCounterEnd { get; private set; }
    
    public int Offset
    {
        get => _offset;
        set
        {
            if (!AddressInSection(value) || value < Start || value > Address.MaxAddress)
            {
                throw new OutputException(OutputExceptionType.InvalidProgramCounter);
            }
            _offset = value;
        }
    }
    
    public byte Xor { get; set; }

    public int ProgramCounter
    {
        get => _pc;
        set
        {
            if (value < Start || value > Address.MaxAddress || !AddressInSection(value)) 
            {
                throw new OutputException(OutputExceptionType.InvalidProgramCounter);
            }
            _pc = value;
        }
    }

    public bool Synched { get; set; } = true;

    public bool Started { get; private set; }
}