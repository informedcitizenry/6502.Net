//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Core6502DotNet
{

    /// <summary>
    /// An error for an invalid Program Counter assignment;
    /// </summary>
    public class InvalidPCAssignmentException : Exception
    {
        readonly int _pc;

        public InvalidPCAssignmentException(int value) => _pc = value;

        public override string Message => _pc.ToString();
    }

    public class ProgramOverflowException : Exception
    {
        
    }

    /// <summary>
    /// A class that manages the internal state of a compiled assembly, including
    /// Program Counters and binary data.
    /// </summary>
    public sealed class BinaryOutput : IFunctionEvaluator
    {
        #region Members
        byte[] _bytes;
        int _logicalPc;
        int _pc;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new compilation
        /// </summary>
        /// <param name="isLittleEndian">Determines whether to compile as little endian</param>
        public BinaryOutput(bool isLittleEndian)
        {
            IsLittleEndian = isLittleEndian;
            Assembler.PassChanged += AssemblerPassesChanged;
            Assembler.SymbolManager.AddValidSymbolNameCriterion(s => !s.Equals("peek") && !s.Equals("poke"));
            Evaluator.AddFunctionEvaluator(this);
            Reset();
        }


        /// <summary>
        /// Initializes a new compilation.
        /// </summary>
        public BinaryOutput()
            : this(true)
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

        /// <summary>
        /// Returns an <see cref="IEnumerable{byte}"/> string of the
        /// numeric value, excluding all bytes containing only leading sign bits, 
        /// returning only the minimum amount of bytes needed for the representation.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>The <see cref="IEnumerable{byte}"/> conversion.</returns>
        public static IEnumerable<byte> ConvertToBytes(double value)
        {
            var bytes = BitConverter.GetBytes(Convert.ToInt64(value));
            int nonZero;
            if (value < 0)
                nonZero = bytes.ToList().FindLastIndex(b => b != 255);
            else
                nonZero = bytes.ToList().FindLastIndex(b => b != 0);
            if (nonZero < 0)
                nonZero++;

            return bytes.Take(nonZero + 1);
        }

        #endregion

        #region Methods

        void AssemblerPassesChanged(object sender, EventArgs args) => Reset();

        /// <summary>
        /// Reset the compilation, clearing all bytes and resetting the Program Counter.
        /// </summary>
        public void Reset()
        {
            _bytes = new byte[0x10000];
            CurrentBank = 0;
            PreviousPC = _pc = _logicalPc = 0;
            MaxAddress = ushort.MaxValue;
            PCOverflow = false;
            SetBank(0);
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
            CurrentBank = bank;
        }

        /// <summary>
        /// Sets both the logical and real Program Counters.
        /// </summary>
        /// <param name="value">The program counter value</param>
        public void SetPC(int value)
        {
            if (value < 0 || value > MaxAddress)
                throw new InvalidPCAssignmentException(value);
            LogicalPC = value;
            ProgramCounter = value;
        }

        /// <summary>
        /// Set the logical Program Counter. This is useful when compiling re-locatable code.
        /// </summary>
        /// <param name="value">The logical program counter value</param>
        public void SetLogicalPC(int value)
        {
            if (value < 0 || value > MaxAddress)
                throw new InvalidPCAssignmentException(value);
            LogicalPC = value;
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
        /// <returns>A <see cref="List{byte}"/> 
        /// of the bytes added to the compilation.</returns>
        public List<byte> Add(long value, int size)
        {
            var bytes = BitConverter.GetBytes(value);
            return AddBytes(bytes, size, false);
        }

        /// <summary>
        /// Add a value to the compilation
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <param name="size">The size, in bytes, of the value.</param>
        /// <returns>A <see cref="List{byte}"/> 
        /// of the bytes added to the compilation.</returns>
        public List<byte> Add(double value, int size) =>
            AddBytes(BitConverter.GetBytes((long)value), size, false);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public List<byte> Add(double value) => AddBytes(ConvertToBytes(value));

        /// <summary>
        /// Add uninitialized memory to the compilation. This is the same as incrementing the 
        /// logical Program Counter by the specified size, but without adding any data to the
        /// compilation.
        /// </summary>
        /// <param name="size">The number of bytes to add to the memory space.</param>
        public void AddUninitialized(int size)
        {
            ProgramCounter += size;
            LogicalPC += size;
        }

        /// <summary>
        /// Add the bytes for a string to the compilation.
        /// </summary>
        /// <param name="s">The string to add.</param>
        /// <returns>A <see cref="List&lt;byte&gt;"/> 
        /// of the bytes added to the compilation.</returns>
        public List<byte> Add(string s) => Add(s, Encoding.UTF8);

        /// <summary>
        /// Add the bytes for a string to the compilation.
        /// </summary>
        /// <param name="s">The string to add.</param>
        /// <param name="encoding">The <see cref="Encoding"/> class to encode the output</param>
        /// <returns>A <see cref="List&lt;byte&gt;"/> of the bytes added to the compilation.</returns>
        public List<byte> Add(string s, Encoding encoding) => AddBytes(encoding.GetBytes(s));

        /// <summary>
        /// Add a 32-bit integral value to the compilation.
        /// </summary>
        /// <param name="value">The value to add</param>
        /// <returns>A <see cref="List&lt;byte&gt;"/> of the bytes added to the compilation.</returns>
        public List<byte> Add(int value) => Add(value, 4);

        /// <summary>
        /// Add a byte value to the compilation.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>A <see cref="List&lt;byte&gt;"/> of the bytes added to the compilation.</returns>
        public List<byte> Add(byte value) => Add(Convert.ToInt32(value), 1);

        /// <summary>
        /// Add a 16-bit integral value to the compilation.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>A <see cref="List&lt;byte&gt;"/> of the bytes added to the compilation.</returns>
        public List<byte> Add(ushort value) => Add(Convert.ToInt32(value), 2);

        /// <summary>
        /// Reserve uninitialized memory in the compilation by an unspecified amount.
        /// </summary>
        /// <param name="amount">The amount to reserve.</param>
        public void Fill(int amount)
        {
            LogicalPC += amount;
            ProgramCounter += amount;
        }

        /// <summary>
        /// Fill memory with the specified values by the specified amount.
        /// </summary>
        /// <param name="amount">The amount to fill.</param>
        /// <param name="value">The fill value.</param>
        /// <returns>A <see cref="List&lt;byte&gt;"/> of the bytes added to the compilation.</returns>
        public List<byte> Fill(int amount, long value)
        {
            var size = value.Size();
            byte[] fillbytes;

            if (BitConverter.IsLittleEndian != IsLittleEndian)
                fillbytes = BitConverter.GetBytes(value).Reverse().Take(size).ToArray();
            else
                fillbytes = BitConverter.GetBytes(value).Take(size).ToArray();

            var repeated = new List<byte>(amount);
            for (var i = 0; i < amount; i++)
                repeated.AddRange(fillbytes);

            return AddBytes(repeated.GetRange(0, amount), true);
        }

        /// <summary>
        /// Align the compilation to the specified boundary and fill with the specified values.
        /// For instance, to align the next byte(s) in the compilation to a page boundary you would
        /// set the align amount to 256.
        /// </summary>
        /// <param name="amount">The amount to align the compilation.</param>
        /// <param name="value">The value to fill before the alignment.</param>
        /// <returns>A <see cref="List&lt;byte&gt;"/> of the bytes added to the compilation.</returns>
        public List<byte> Align(int amount, long value)
        {
            var align = GetAlignmentSize(LogicalPC, amount);
            return Fill(align, value);
        }

        /// <summary>
        /// Align the compilation to the specified boundary. For instance, to align the next byte(s)
        /// in the compilation to a page boundary you would set the align amount to 256.
        /// </summary>
        /// <param name="amount">The amount to align.</param>
        public void Align(int amount)
        {
            var align = GetAlignmentSize(LogicalPC, amount);
            LogicalPC += align;
            ProgramCounter += align;
            //return align;
        }

        /// <summary>
        /// Add a range of bytes to the compilation.
        /// </summary>
        /// <param name="bytes">The collection of bytes to add.</param>
        /// <param name="size">The number of bytes in the collection to add.</param>
        /// <param name="ignoreEndian">Ignore the endianness when adding to the compilation.</param>
        /// <returns>A <see cref="List&lt;byte&gt;"/> of the bytes added to the compilation.</returns>
        /// <exception cref="ProgramOverflowException"/>
        public List<byte> AddBytes(IEnumerable<byte> bytes, int size, bool ignoreEndian)
        {    
            if (CompilingHasStarted == false)
            {
                CompilingHasStarted = true;
                ProgramStart = ProgramCounter;
            }
            LogicalPC += size;

            if (ignoreEndian == false && BitConverter.IsLittleEndian != IsLittleEndian)
                bytes = bytes.Reverse();

            if (Transform != null)
                bytes = bytes.Select(b => Transform(b));

            var bytesAdded = bytes.ToList().GetRange(0, size);
            foreach (var b in bytesAdded)
            {
                if (ProgramCounter > MaxAddress)
                    throw new ProgramOverflowException();
                _bytes[ProgramCounter++] = b;
            }

           if (ProgramEnd < ProgramCounter)
                ProgramEnd = ProgramCounter;

            return bytesAdded;
        }

        /// <summary>
        /// Add a range of bytes to the compilation.
        /// </summary>
        /// <param name="bytes">The collection of bytes to add.</param>
        /// <param name="ignoreEndian">Ignore the endianness when adding to the compilation.</param>
        /// <returns>A <see cref="List&lt;byte&gt;"/> of the bytes added to the compilation.</returns>
        public List<byte> AddBytes(IEnumerable<byte> bytes, bool ignoreEndian) => AddBytes(bytes, bytes.Count(), ignoreEndian);

        /// <summary>
        /// Add a range of bytes to the compilation.
        /// </summary>
        /// <param name="bytes">The collection of bytes to add.</param>
        /// <param name="size">The number of bytes in the collection to add.</param>
        /// <returns>A <see cref="List&lt;byte&gt;"/> of the bytes added to the compilation.</returns>
        public List<byte> AddBytes(IEnumerable<byte> bytes, int size) => AddBytes(bytes, size, true);

        /// <summary>
        /// Add a range of bytes to the compilation.
        /// </summary>
        /// <param name="bytes">The collection of bytes to add.</param>
        /// <returns>A <see cref="List&lt;byte&gt;"/> of the bytes added to the compilation.</returns>
        public List<byte> AddBytes(IEnumerable<byte> bytes) => AddBytes(bytes, true);

        /// <summary>
        /// Get the compilation bytes.
        /// </summary>
        /// <returns>The bytes of the compilation.</returns>
        public ReadOnlyCollection<byte> GetCompilation()
            => new ReadOnlyCollection<byte>(_bytes.Skip(ProgramStart).Take(ProgramEnd - ProgramStart).ToArray());

        /// <summary>
        /// Get the relative offset between an address and the Program Counter. Useful for calculating short jumps.
        /// </summary>
        /// <param name="address1">An address to offset from.</param>
        /// <param name="offsetfromPc">Additional offset from the Program Counter.</param>
        /// <returns>Returns the relative offset between the two addresses.</returns>
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
        /// Initialize memory from Program Start to the specified value.
        /// </summary>
        /// <param name="value">The value to initialize memory.</param>
        public void InitMemory(byte value)
        {
            var i = ProgramCounter;
            while (i < 0x10000)
                _bytes[i++] = value;
        }

        public bool EvaluatesFunction(Token function) =>
            function.Name.Equals("peek") || function.Name.Equals("poke");

        public double EvaluateFunction(Token function, Token parameters)
        {
            if (parameters.HasChildren)
            {
                var address = (int)Evaluator.Evaluate(parameters.Children[0], ushort.MinValue, ushort.MaxValue);
                var index = address - ProgramStart;

                if (function.Name.Equals("peek"))
                {
                    if (parameters.Children.Count != 1)
                        throw new ExpressionException(parameters.Position, "Too many arguments passed for function \"peek\".");

                    if (address < ProgramStart || address >= ProgramCounter)
                        throw new ExpressionException(parameters.Position, $"Cannot read address ${address:x4}.");
                    return _bytes[address];
                }
                else
                {
                    if (parameters.Children.Count != 2)
                        throw new ExpressionException(parameters.Position, "Too many arguments passed for function \"poke\".");
                    var value = (byte)Evaluator.Evaluate(parameters.Children[1], sbyte.MinValue, byte.MaxValue);

                    _bytes[address] = value;
                    if (address < ProgramStart)
                        ProgramStart = address;
                    else if (address > ProgramCounter)
                        ProgramEnd = address;
                    else
                        _bytes[index] = value;
                    return value;
                }
            }
            throw new ExpressionException(parameters.Position, $"Too few arguments passed for function \"{function.Name}\".");
        }

        /// <summary>
        /// Gets the SHA1 hash of the binary output.
        /// </summary>
        /// <returns>The calculated checksum of the binary output.</returns>
        public string GetOutputHash()
        {
            var sha1 = SHA1.Create();
            var sha1Sb = new StringBuilder();
            var bytes = sha1.ComputeHash(_bytes.Skip(ProgramStart).Take(ProgramEnd - ProgramStart).ToArray());
            foreach (var b in bytes)
                sha1Sb.Append($"{b:x2}");
            return sha1Sb.ToString();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the program start addressed based on the value of the Program Counter
        /// when compilation first occurred.
        /// </summary>
        public int ProgramStart 
        { get; private set; }

        /// <summary>
        /// Gets the program end address, which is the address of the final assembled byte
        /// from the program start.
        /// </summary>
        public int ProgramEnd { get; private set; }

        /// <summary>
        /// Gets the previous Program Counter value before the most recent 
        /// output operation.
        /// </summary>
        public int PreviousPC { get; set; }

        /// <summary>
        /// Gets or sets the maximum address allowed for the Program Counter until
        /// it overflows.
        /// </summary>
        public int MaxAddress { get; set; }

        /// <summary>
        /// Gets the status of the compilation, if it is currently compiling
        /// </summary>
        public bool CompilingHasStarted { get; private set; }//=> _bytes.Count > 0;

        /// <summary>
        /// Gets the real Program Counter
        /// </summary>
        public int ProgramCounter
        {
            get => _pc;
            set
            {
                if (value < 0 || value < _pc)
                    throw new InvalidPCAssignmentException(value);
                _pc = value;
            }
        }

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
        public bool PCOverflow { get; set; }

        /// <summary>
        /// Gets the current logical Program Counter.
        /// </summary>
        public int LogicalPC
        {
            get => _logicalPc;
            set
            {
                if (value < 0 || value < _logicalPc)
                    throw new InvalidPCAssignmentException(value);
                if (!PCOverflow)
                    PCOverflow = value > MaxAddress;
                PreviousPC = _logicalPc;
                _logicalPc = value & MaxAddress;
            }
        }

        public Func<byte, byte> Transform { get; set; }

        #endregion
    }
}