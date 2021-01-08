//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Core6502DotNet
{

    /// <summary>
    /// An error for an invalid Program Counter assignment.
    /// </summary>
    public class InvalidPCAssignmentException : Exception
    {
        readonly int _pc;

        /// <summary>
        /// Creates a new instance of an invalid PC assignment exception.
        /// </summary>
        /// <param name="value">The Program Counter value.</param>
        public InvalidPCAssignmentException(int value) => _pc = value;

        /// <summary>
        /// Creates a new instance of an invalid PC assignment exception.
        /// </summary>
        /// <param name="value">The Program Counter value.</param>
        /// <param name="sectionNotUsedError">The error was due to a section not being set.</param>
        public InvalidPCAssignmentException(int value, bool sectionNotUsedError)
            => (_pc, SectionNotUsedError) = (value, sectionNotUsedError);

        public override string Message
        {
            get
            {
                if (SectionNotUsedError)
                    return "A section was defined but not set.";
                return _pc.ToString();
            }
        }

        /// <summary>
        /// Gets a flag that determines the cause of the error was due to one or more sections
        /// being defined but never set.
        /// </summary>
        public bool SectionNotUsedError { get; }
    }

    /// <summary>
    /// An error for a Program Counter rollover.
    /// </summary>
    public class ProgramOverflowException : Exception
    {
        /// <summary>
        /// Creates a new instance of a program overflow error.
        /// </summary>
        /// <param name="message">The custom overflow message.</param>
        public ProgramOverflowException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// A class that manages the internal state of a compiled assembly, including
    /// Program Counters and binary data.
    /// </summary>
    public sealed class BinaryOutput
    {
        #region Constants

        const int BufferSize = 0x10000;

        /// <summary>
        /// Represents the maximum address the Program Counter can reach before
        /// subsequent output is considered an overflow.
        /// </summary>
        public const int MaxAddress = 0xFFFF;

        #endregion

        #region Members

        readonly SectionCollection _sectionCollection;
        readonly byte[] _bytes;
        bool _compilingStarted;
        int _logicalPc;
        int _pc;
        bool _started;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new compilation
        /// </summary>
        /// <param name="isLittleEndian">Determines whether to compile as little endian</param>
        public BinaryOutput(bool isLittleEndian, bool caseSensitive)
        {
            _compilingStarted = false;
            _bytes = new byte[BufferSize];
            _sectionCollection = new SectionCollection(caseSensitive);
            IsLittleEndian = isLittleEndian;
            Reset();
        }


        /// <summary>
        /// Initializes a new compilation.
        /// </summary>
        public BinaryOutput()
            : this(true, false)
        {

        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Get the actual amount by which the PC must be increased to align to the given amount.
        /// </summary>
        /// <param name="amount">The amount to align</param>
        /// <returns></returns>
        public static int GetAlignmentSize(int pc, int amount)
        {
            var align = 0;
            while ((pc + align) % amount != 0)
                align++;
            return align;
        }

        #endregion

        /// <summary>
        /// Returns byte string of the
        /// numeric value, excluding all bytes containing only leading sign bits, 
        /// returning only the minimum amount of bytes needed for the representation.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>The byte string converted.</returns>
        public IEnumerable<byte> ConvertToBytes(double value)
        {
            var bytes = BitConverter.GetBytes(Convert.ToInt64(value)).ToList();
            int nonZero;
            if (value < 0)
                nonZero = bytes.FindLastIndex(b => b != 255);
            else
                nonZero = bytes.FindLastIndex(b => b != 0);
            if (nonZero < 0)
                nonZero++;
            var bytesTaken = bytes.Take(nonZero + 1);
            if (BitConverter.IsLittleEndian != IsLittleEndian)
                bytesTaken = bytesTaken.Reverse();
            return bytesTaken;
        }

        #region Methods

        void AssemblerPassesChanged(object sender, EventArgs args) => Reset();

        /// <summary>
        /// Reset the compilation, clearing all bytes and resetting the Program Counter.
        /// </summary>
        public void Reset()
        {
            Array.Clear(_bytes, 0, BufferSize);
            Transform = null;
            PCOverflow = _compilingStarted = _started = false;
            _sectionCollection.Reset();
            CurrentBank = ProgramEnd = PreviousPC = _pc = _logicalPc = 0;
           
        }

        /// <summary>
        /// Sets the current bank.
        /// </summary>
        /// <param name="bank"></param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public void SetBank(int bank)
        {
            if (bank < 0)
                throw new ArgumentOutOfRangeException();
            if (!AddressIsValid(0))
                throw new InvalidPCAssignmentException(0);
            _logicalPc = _pc = 0;
            CurrentBank = bank;
        }

        /// <summary>
        /// Sets both the logical and real Program Counters.
        /// </summary>
        /// <param name="value">The program counter value</param>
        public void SetPC(int value)
        {
            if (!AddressIsValid(value))
                throw new InvalidPCAssignmentException(value, 
                    !_sectionCollection.IsEmpty && !_sectionCollection.SectionSelected);
            
            LogicalPC = value;
            ProgramCounter = value;
        }

        /// <summary>
        /// Set the logical Program Counter. This is useful when compiling re-locatable code.
        /// </summary>
        /// <param name="value">The logical program counter value</param>
        public void SetLogicalPC(int value)
        {
            if (value < -(MaxAddress / 2) || value > MaxAddress)
                throw new InvalidPCAssignmentException(value);
            _started = true;
            _logicalPc = value & 0xFFFF;
        }

        /// <summary>
        /// Reset the logical Program Counter back to the internal Program Counter value.
        /// Used with SetLogicalPC().
        /// </summary>
        /// <returns>The new logical Program Counter.</returns>
        public int SynchPC()
        {
            _logicalPc = ProgramCounter;
            return _logicalPc;
        }

        /// <summary>
        /// Add a value to the compilation
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <param name="size">The size, in bytes, of the value.</param>
        /// of the bytes added to the compilation.</returns>
        public void Add(long value, int size)
            => AddBytes(BitConvertLittleEndian(value), size, false);

        /// <summary>
        /// Add a value to the compilation
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <param name="size">The size, in bytes, of the value.</param>
        /// of the bytes added to the compilation.</returns>
        public void Add(double value, int size) =>
            AddBytes(BitConvertLittleEndian((long)value), size, false);

        /// <summary>
        /// Add a value to the compilation
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void Add(double value) => AddBytes(ConvertToBytes(value));

        /// <summary>
        /// Add uninitialized memory to the compilation. This is the same as incrementing the 
        /// logical Program Counter by the specified size, but without adding any data to the
        /// compilation.
        /// </summary>
        /// <param name="size">The number of bytes to add to the memory space.</param>
        public void AddUninitialized(int size)
        {
            if (ProgramEnd > MaxAddress)
                throw new ProgramOverflowException($"Program overflowed {size} bytes.");
            if (!AddressIsValid(ProgramCounter))
                throw new InvalidPCAssignmentException(ProgramCounter, !_sectionCollection.IsEmpty && !_sectionCollection.SectionSelected);
            _pc += size;
            _logicalPc += size;
            if (_sectionCollection.SectionSelected)
                _sectionCollection.SetOutputCount(_pc - _sectionCollection.SelectedStartAddress);
        }

        /// <summary>
        /// Add the bytes for a string to the compilation.
        /// </summary>
        /// <param name="s">The string to add.</param>
        /// <returns>A <see cref="List&lt;byte&gt;"/> 
        /// of the bytes added to the compilation.</returns>
        public void Add(string s) => Add(s, Encoding.UTF8);

        /// <summary>
        /// Add the bytes for a string to the compilation.
        /// </summary>
        /// <param name="s">The string to add.</param>
        /// <param name="encoding">The <see cref="Encoding"/> class to encode the output</param>
        /// <returns>A <see cref="List&lt;byte&gt;"/> of the bytes added to the compilation.</returns>
        public void Add(string s, Encoding encoding) => AddBytes(encoding.GetBytes(s));

        /// <summary>
        /// Add a 32-bit integral value to the compilation.
        /// </summary>
        /// <param name="value">The value to add</param>
        /// <returns>A <see cref="List&lt;byte&gt;"/> of the bytes added to the compilation.</returns>
        public void Add(int value) => Add(value, 4);

        /// <summary>
        /// Add a byte value to the compilation.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>A <see cref="List&lt;byte&gt;"/> of the bytes added to the compilation.</returns>
        public void Add(byte value) => Add(Convert.ToInt32(value), 1);

        /// <summary>
        /// Add a 16-bit integral value to the compilation.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void Add(ushort value) => Add(Convert.ToInt32(value), 2);

        /// <summary>
        /// Reserve uninitialized memory in the compilation by an unspecified amount.
        /// </summary>
        /// <param name="amount">The amount to reserve.</param>
        public void Fill(int amount) => AddUninitialized(amount);

        /// <summary>
        /// Fill memory with the specified values by the specified amount.
        /// </summary>
        /// <param name="amount">The amount to fill.</param>
        /// <param name="value">The fill value.</param>
        public void Fill(int amount, long value)
        {
            var size = value.Size();
            byte[] fillbytes = BitConverter.GetBytes(value).Take(size).ToArray();
            if (BitConverter.IsLittleEndian != IsLittleEndian)
                fillbytes = fillbytes.Reverse().ToArray();

            var repeated = new List<byte>(amount);
            for (var i = 0; i < amount; i++)
                repeated.AddRange(fillbytes);

            AddBytes(repeated.GetRange(0, amount), true);
        }

        /// <summary>
        /// Align the compilation to the specified boundary and fill with the specified values.
        /// For instance, to align the next byte(s) in the compilation to a page boundary you would
        /// set the align amount to 256.
        /// </summary>
        /// <param name="amount">The amount to align the compilation.</param>
        /// <param name="value">The value to fill before the alignment.</param>
        public void Align(int amount, long value)
        {
            var align = GetAlignmentSize(LogicalPC, amount);
            Fill(align, value);
        }

        /// <summary>
        /// Align the compilation to the specified boundary. For instance, to align the next byte(s)
        /// in the compilation to a page boundary you would set the align amount to 256.
        /// </summary>
        /// <param name="amount">The amount to align.</param>
        public void Align(int amount)
        {
            var align = GetAlignmentSize(LogicalPC, amount);
            AddUninitialized(align);
        }

        /// <summary>
        /// Add a range of bytes to the compilation.
        /// </summary>
        /// <param name="bytes">The collection of bytes to add.</param>
        /// <param name="size">The number of bytes in the collection to add.</param>
        /// <param name="ignoreEndian">Ignore the endianness when adding to the compilation.</param>
        /// <exception cref="ProgramOverflowException"/>
        public void AddBytes(IEnumerable<byte> bytes, int size, bool ignoreEndian)
        {
            if (!_compilingStarted)
            {
                if (!_sectionCollection.IsEmpty && !_sectionCollection.SectionSelected)
                    throw new InvalidPCAssignmentException(_pc, true);
                _started =
                _compilingStarted = true;
                ProgramStart = ProgramCounter;
            }
            else if (ProgramCounter < ProgramStart)
            {
                ProgramStart = ProgramCounter;
            }
            _logicalPc += size;

            if (Transform != null)
                bytes = bytes.Select(b => Transform(b));

            var bytesAdded = bytes.ToList().GetRange(0, size);
            if (!ignoreEndian && BitConverter.IsLittleEndian != IsLittleEndian)
                bytesAdded.Reverse();

            foreach (var b in bytesAdded)
            {
                if (_pc > MaxAddress)
                    throw new ProgramOverflowException($"Program overflow.");
                if (_sectionCollection.SectionSelected && !_sectionCollection.AddressInBounds(_pc))
                    throw new ProgramOverflowException($"${ProgramCounter:x4} exceeds bounds of current section.");
                _bytes[_pc++] = b;
            }
            if (ProgramEnd < ProgramCounter)
                ProgramEnd = ProgramCounter;
            if (_sectionCollection.SectionSelected)
                _sectionCollection.SetOutputCount(_pc - _sectionCollection.SelectedStartAddress);
        }

        /// <summary>
        /// Add a range of bytes to the compilation.
        /// </summary>
        /// <param name="bytes">The collection of bytes to add.</param>
        /// <param name="ignoreEndian">Ignore the endianness when adding to the compilation.</param>
        public void AddBytes(IEnumerable<byte> bytes, bool ignoreEndian) => AddBytes(bytes, bytes.Count(), ignoreEndian);

        /// <summary>
        /// Add a range of bytes to the compilation.
        /// </summary>
        /// <param name="bytes">The collection of bytes to add.</param>
        /// <param name="size">The number of bytes in the collection to add.</param>
        public void AddBytes(IEnumerable<byte> bytes, int size) => AddBytes(bytes, size, true);

        /// <summary>
        /// Add a range of bytes to the compilation.
        /// </summary>
        /// <param name="bytes">The collection of bytes to add.</param>
        public void AddBytes(IEnumerable<byte> bytes) => AddBytes(bytes, true);

        /// <summary>
        /// Get the compilation bytes.
        /// </summary>
        /// <returns>The object bytes.</returns>
        public ReadOnlyCollection<byte> GetCompilation()
            => new ReadOnlyCollection<byte>(_bytes.Skip(ProgramStart).Take(ProgramEnd - ProgramStart).ToArray());

        /// <summary>
        /// Gets the compilation bytes.
        /// </summary>
        /// <param name="section">Gets the bytes of the defined section only.</param>
        /// <returns>The object bytes.</returns>
        public ReadOnlyCollection<byte> GetCompilation(string section)
        {
            if (string.IsNullOrEmpty(section))
                return GetCompilation();

            int count;
            if (_sectionCollection.SetCurrentSection(section) == CollectionResult.NotFound ||
                (count = _sectionCollection.GetSectionOutputCount()) == -1)
                throw new Exception($"Could not get object for section {section}");
            var start = _sectionCollection.SelectedStartAddress;
            return new ReadOnlyCollection<byte>(_bytes.Skip(start).Take(count).ToArray());
        }

        /// <summary>
        /// Get the relative offset between an address and the Program Counter. Useful for calculating
        /// branchess.
        /// </summary>
        /// <param name="address1">An address to offset from.</param>
        /// <param name="offsetfromPc">Additional offset from the Program Counter.</param>
        public int GetRelativeOffset(int address1, int offsetfromPc)
        {
            var address2 = LogicalPC + offsetfromPc;
            var offset = address1 - address2;
            if (Math.Abs(offset) > (MaxAddress / 2))
            {
                if (offset < 0)
                    offset = Math.Abs(offset) - (MaxAddress + 1);
                else
                    offset = MaxAddress + 1 - offset;

                if (address1 > address2)
                    offset = -offset;
            }
            return offset;
        }

        /// <summary>
        /// Gets the bytes in the compilation from a specified 
        /// address to the Program End address. 
        /// </summary>
        /// <param name="start">The start address.</param>
        /// <returns>The set of bytes from the specified start address to Program End.</returns>
        public ReadOnlyCollection<byte> GetBytesFrom(int start)
        {
            if (!_compilingStarted || ProgramCounter < ProgramStart)
                return new List<byte>().AsReadOnly();
            if (ProgramCounter != _logicalPc)
            {
                var diff = _logicalPc - start;
                start = ProgramCounter - diff;
            }
            if (start < ProgramStart || start >= ProgramEnd)
                return new List<byte>().AsReadOnly();
            int range;
            if (!_sectionCollection.SectionSelected ||
                _sectionCollection.AddressInBounds(ProgramEnd))
                range = ProgramEnd - start;
            else
                range = ProgramCounter - start;

            if (range < 0 || range > MaxAddress)
                return new List<byte>().AsReadOnly();
            return _bytes.Skip(start).Take(range).ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets a range of bytes from the output assembly.
        /// </summary>
        /// <param name="start">The start address.</param>
        /// <param name="amount">The length.</param>
        /// <returns>The set of bytes from the specified start
        /// address to the specified amount.</returns>
        public ReadOnlyCollection<byte> GetRange(int start, int amount)
        {
            if (!_compilingStarted || start + amount > MaxAddress)
                return new List<byte>(amount).AsReadOnly();
            return _bytes.Skip(start).Take(amount).ToList().AsReadOnly();
        }

        /// <summary>
        /// Initialize memory from Program Start to the specified value.
        /// </summary>
        /// <param name="value">The value to initialize memory.</param>
        public void InitMemory(byte value)
        {
            var i = !_compilingStarted ? ProgramCounter : ProgramEnd;
            while (i < 0x10000)
                _bytes[i++] = value;
        }

        /// <summary>
        /// Read an arbitrary byte from the output.
        /// </summary>
        /// <param name="address">The address of assembled output.</param>
        /// <returns>The byte value at the address.</returns>
        /// <exception cref="ProgramOverflowException"></exception>
        public byte Peek(int address)
        {
            if (address < ProgramStart || address >= ProgramCounter)
                throw new ProgramOverflowException($"Cannot read address ${address:x4}.");
            return _bytes[address];
        }

        /// <summary>
        /// Write a byte to an address.
        /// </summary>
        /// <param name="address">The address to write.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ProgramOverflowException"></exception>
        public void Poke(int address, byte value)
        {
            if (!AddressIsValid(address))
                throw new ProgramOverflowException(
                    $"Address ${address:x4} is not within the bounds of program or section space.");

            _started = _compilingStarted = true;
            var index = address - ProgramStart;
            if (address < ProgramStart)
                ProgramStart = address;
            else if (address > ProgramCounter)
                ProgramEnd = address;
            else
                _bytes[index] = value;
        }

        string GetHashForOutput(int start, int count)
        {
            using var md5 = MD5.Create();
            using var stream = new MemoryStream(_bytes, start, count);
            var hash = BitConverter.ToString(md5.ComputeHash(stream));
            return hash.Replace("-", string.Empty).ToLower();
        }

        /// <summary>
        /// Gets the MD5 hash of the binary output.
        /// </summary>
        /// <returns>The calculated checksum of the binary output.</returns>
        public string GetOutputHash() => GetHashForOutput(ProgramStart, ProgramEnd - ProgramStart);

        /// <summary>
        /// Gets the MD5 hash of the binary output.
        /// </summary>
        /// <param name="section">The specified section's hash.</param>
        /// <returns>The calculated checksum of the binary output.</returns>
        public string GetOutputHash(string section)
        {
            if (string.IsNullOrEmpty(section))
                return GetOutputHash();
            int count;
            if (_sectionCollection.SetCurrentSection(section) == CollectionResult.NotFound ||
                (count = _sectionCollection.GetSectionOutputCount()) == -1)
                throw new Exception($"Could not get object bytes for section {section}.");
            return GetHashForOutput(_sectionCollection.SelectedStartAddress, count);
        }

        /// <summary>
        /// Get the start address of a defined section.
        /// </summary>
        /// <param name="name">The section name.</param>
        /// <returns>The section start address, if it is defined.</returns>
        /// <exception cref="Exception"></exception>
        public int GetSectionStart(StringView name)
        {
            var start = _sectionCollection.GetSectionStart(name);
            if (start > int.MinValue)
                return start;
            throw new Exception($"Section {name} is not defined.");
        }

        /// <summary>
        /// Defines a named section for the binary output.
        /// </summary>
        /// <param name="name">The section name.</param>
        /// <param name="starts">The section start address.</param>
        /// <param name="ends">The section end address..</param>
        /// <exception cref="SectionException"/>
        public void DefineSection(StringView name, int starts, int ends)
        {
            if (_started || _sectionCollection.SectionSelected)
                throw new SectionException(1,
                    "Cannot define a section after assembly has started.");
            if (starts < 0)
                throw new SectionException(1,
                    $"Section {name} start address {starts} is not valid.");
            if (starts >= ends)
                throw new SectionException(1,
                    $"Section {name} start address cannot be equal or greater than end address.");
            if (ends > MaxAddress + 1)
                throw new SectionException(1,
                    $"Section {name} end address {ends} is not valid.");
            switch (_sectionCollection.Add(name, starts, ends))
            {
                case CollectionResult.Duplicate:
                    throw new SectionException(1, $"Section {name} already defined.");
                case CollectionResult.RangeOverlap:
                    throw new SectionException(1,
                        $"Section {name} start and end address intersect existing section's.");
                default:
                    break;
            }
        }

        /// <summary>
        /// Sets the current defined section.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <returns><c>true</c> if the section was able to be selected, otherwise <c>false</c>.</returns>
        /// <exception cref="SectionException"></exception>
        public bool SetSection(StringView section)
        {
            var result = _sectionCollection.SetCurrentSection(section);
            if (result == CollectionResult.NotFound)
                return false;
            _pc =
            _logicalPc = _sectionCollection.SelectedStartAddress + _sectionCollection.GetSectionOutputCount();
            return true;
        }

        public override string ToString() => $"{ProgramStart:X4}:{ProgramCounter:X4} [{LogicalPC:X4}]";

        IEnumerable<byte> BitConvertLittleEndian(long value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                return bytes.Reverse();
            return bytes;
        }

        /// <summary>
        /// Determines if the given address is valid to read from and write to the output.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns><c>true</c> if the address is valid; otherwise <c>false</c>.</returns>
        public bool AddressIsValid(int address)
        {
            if (_sectionCollection.SectionSelected)
                return _sectionCollection.AddressInBounds(address);
            else if (!_sectionCollection.IsEmpty)
                return false;
            return address >= 0 && address <= MaxAddress;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the program start addressed based on the value of the Program Counter
        /// when compilation first occurred.
        /// </summary>
        public int ProgramStart { get; private set; }

        /// <summary>
        /// Gets the program end address, which is the address of the final assembled byte
        /// from the program start.
        /// </summary>
        public int ProgramEnd { get; private set; }

        /// <summary>
        /// Gets the previous Program Counter value before the most recent 
        /// output operation.
        /// </summary>
        public int PreviousPC { get; private set; }

        /// <summary>
        /// Gets the real Program Counter
        /// </summary>
        public int ProgramCounter
        {
            get => _pc;
            private set
            {
                if (!AddressIsValid(value) || value < _pc)
                    throw new InvalidPCAssignmentException(value, !_sectionCollection.IsEmpty && !_sectionCollection.SectionSelected);
                _pc = value;
            }
        }

        /// <summary>
        /// Gets the names of any defined sections not used during 
        /// assembly.
        /// </summary>
        public IEnumerable<string> UnusedSections
            => _sectionCollection.SectionsNotSelected;

        /// <summary>
        /// Gets the current memory bank.
        /// </summary>
        public int CurrentBank { get; private set; }

        /// <summary>
        /// Gets the endianness of the compilation
        /// </summary>
        public bool IsLittleEndian { get; set; }

        /// <summary>
        /// Gets a flag that indicates if a PC overflow has occurred. This flag will 
        /// only be cleared with a call to the Reset method.
        /// </summary>
        public bool PCOverflow { get; private set; }

        /// <summary>
        /// Gets the current logical Program Counter.
        /// </summary>
        public int LogicalPC
        {
            get => _logicalPc;
            set
            {
                if (!AddressIsValid(value) || value < _logicalPc)
                    throw new InvalidPCAssignmentException(value, !_sectionCollection.IsEmpty && !_sectionCollection.SectionSelected);
                if (!PCOverflow)
                    PCOverflow = value > MaxAddress;
                _started = true;
                PreviousPC = _logicalPc;
                _logicalPc = value & MaxAddress;
            }
        }


        /// <summary>
        /// Sets a transform function that will transform bytes 
        /// as they are written to the output.
        /// </summary>
        public Func<byte, byte> Transform { private get; set; }

        /// <summary>
        /// Gets a flag that indicates that output exists.
        /// </summary>
        public bool HasOutput => _compilingStarted;

        #endregion
    }
}