//-----------------------------------------------------------------------------
// Copyright (c) 2017 Nate Burnett <informedcitizenry@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to 
// deal in the Software without restriction, including without limitation the 
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asm6502.Net
{
    /// <summary>
    /// A Compilation manages the internal state of a compiled assembly, including
    /// Program Counters and binary data.
    /// </summary>
    public class Compilation
    {
        #region Exception

        public class InvalidPCAssignmentException : Exception
        {
            private int pc_;

            public InvalidPCAssignmentException(int value)
            {
                pc_ = value;
            }
            public override string Message
            {
                get
                {
                    return string.Format("Assigning value of program counter to ${0:X4} is invalid", pc_);
                }
            }
        }

        #endregion

        #region Members

        int log_pc_;

        int pc_;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new compilation
        /// </summary>
        /// <param name="isLittleEndian">Determines whether to compile as little endian</param>
        public Compilation(bool isLittleEndian)
        {
            Transforms = new Stack<Func<byte, byte>>();
            IsLittleEndian = isLittleEndian;
            Bytes = new List<byte>();
            Reset();
        }

        /// <summary>
        /// Initializes a new compilation.
        /// </summary>
        public Compilation()
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
            int align = 0;
            while ((pc + align) % amount != 0)
                align++;
            return align;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Reset the compilation, clearing all bytes and resetting the Program Counter.
        /// </summary>
        public void Reset()
        {
            Bytes.Clear();
            pc_ = log_pc_ = 0;
            MaxAddress = ushort.MaxValue;
        }

        /// <summary>
        /// Gets the current logical Program Counter.
        /// </summary>
        /// <returns></returns>
        public int GetPC()
        {
            return LogicalPC;
        }

        /// <summary>
        /// Change the first value in the compilation
        /// </summary>
        /// <param name="value">The value of the first compiled data</param>
        /// <param name="size">The size of the data to change</param>
        public void ChangeFirst(int value, int size)
        {
            if ((size % 5) > Bytes.Count)
                size = Bytes.Count;
            if (size == 0)
                return;
            var bytes = BitConverter.GetBytes(value).ToList().GetRange(0, size);
            if (BitConverter.IsLittleEndian != this.IsLittleEndian)
                bytes.Reverse();
            Bytes.RemoveRange(0, size);
            Bytes.InsertRange(0, bytes);
        }

        /// <summary>
        /// Change the last value in the compilation
        /// </summary>
        /// <param name="value">The value of the last compiled data</param>
        /// <param name="size">The size of the data to change</param>
        public void ChangeLast(int value, int size)
        {
            if ((size % 5) > Bytes.Count)
                size = Bytes.Count;
            if (size == 0)
                return;
            var ix = Bytes.Count - size;
            var bytes = BitConverter.GetBytes(value).ToList().GetRange(0, size);
            if (BitConverter.IsLittleEndian != this.IsLittleEndian)
                bytes.Reverse();
            Bytes.RemoveAt(ix);
            Bytes.AddRange(bytes);
        }

        /// <summary>
        /// Set the logical Program Counter. Also sets the internal Program Counter.
        /// </summary>
        /// <param name="value">The program counter value</param>
        /// <exception cref="InvallidPCAssignmentException"></exception>
        public void SetPC(int value)
        {
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
            log_pc_ = value;
        }

        /// <summary>
        /// Reset the logical Program Counter back to the internal Program Counter value.
        /// Used with SetLogicalPC().
        /// </summary>
        /// <returns>Returns the new logical Program Counter</returns>
        public int SynchPC()
        {
            //LogicalPC = ProgramCounter;
            log_pc_ = ProgramCounter;
            return LogicalPC;
        }

        /// <summary>
        /// Add a value to the compilation
        /// </summary>
        /// <param name="value">The value to add</param>
        /// <param name="size">The size, in bytes, of the value</param>
        public void Add(Int64 value, int size)
        {
            var bytes = BitConverter.GetBytes(value);
            AddBytes(bytes, size, false);
        }

        /// <summary>
        /// Add uninitialized memory to the compilation. This is the same as incrementing the 
        /// logical Program Counter by the specified size, but without adding any data to the
        /// compilation.
        /// </summary>
        /// <param name="size">The number of bytes to add to the memory space</param>
        public void AddUninitialized(int size)
        {
            ProgramCounter += size;
            LogicalPC += size;
        }

        /// <summary>
        /// Add the bytes for an ASCII-encoded string to the compilation
        /// </summary>
        /// <param name="value">The value to add</param>
        public void Add(string value)
        {
            byte[] asciibytes = Encoding.ASCII.GetBytes(value);
            AddBytes(asciibytes);
        }

        /// <summary>
        /// Add a 32-bit integral value to the compilation
        /// </summary>
        /// <param name="value">The value to add</param>
        public void Add(int value)
        {
            Add(value, 4);
        }

        /// <summary>
        /// Add a byte value to the compilation
        /// </summary>
        /// <param name="value">The value to add</param>
        public void Add(byte value)
        {
            Add(Convert.ToInt32(value), 1);
        }

        /// <summary>
        /// Add a 16-bit integral value to the compilation
        /// </summary>
        /// <param name="value">The value to add</param>
        public void Add(ushort value)
        {
            Add(Convert.ToInt32(value), 2);
        }

        /// <summary>
        /// Reserve uninitialized memory in the compilation by an unspecified amount
        /// </summary>
        /// <param name="amount">The amount to reserve</param>
        public void Fill(int amount)
        {
            LogicalPC += amount;
            ProgramCounter += amount;
        }

        /// <summary>
        /// Fill memory with the specified values by the specified amount.
        /// </summary>
        /// <param name="amount">The amount to fill</param>
        /// <param name="value">The fill value</param>
        /// <param name="repeatNotFill">Repeat the value not fill to the amount.</param>
        public void Fill(int amount, Int64 value, bool repeatNotFill)
        {
            int size = value.Size();
            byte[] fillbytes;

            if (BitConverter.IsLittleEndian)
            {
                // d2 ff 00 00 
                fillbytes = BitConverter.GetBytes(value).Take(size).ToArray(); // d2 ff
                if (!IsLittleEndian)
                    fillbytes = fillbytes.Reverse().ToArray(); // ff d2
            }
            else
            {
                // 00 00 ff d2
                fillbytes = BitConverter.GetBytes(value).Reverse().Take(size).ToArray(); // d2 ff
                if (!IsLittleEndian)
                    fillbytes = fillbytes.Reverse().ToArray(); // ff d2
            }
            List<byte> repeated = new List<byte>();
            for (int i = 0; i < amount; i++)
            {
                for (int j = 0; j < size; j++)
                    repeated.Add(fillbytes[j]);

            }
            if (repeatNotFill)
                AddBytes(repeated, true);
            else
                AddBytes(repeated.GetRange(0, amount), true);
        }

        /// <summary>
        /// Offset the compilation by a specified amount without updating the logical Program Counter. 
        /// This can be used to create re-locatable code.
        /// </summary>
        /// <param name="amount">The offset amount</param>
        public void Offset(int amount)
        {
            AddBytes(new List<byte>(amount), amount, true, false);
        }

        /// <summary>
        /// Align the compilation to the specified boundary and fill with the specified values.
        /// For instance, to align the next byte(s) in the compilation to a page boundary you would
        /// set the align amount to 256.
        /// </summary>
        /// <param name="amount">The amount to align the compilation</param>
        /// <param name="value">The value to fill before the alignment</param>
        /// <returns>Returns the offset needed to align the Program Counter</returns>
        public int Align(int amount, long value)
        {
            int align = GetAlignmentSize(LogicalPC, amount);
            Fill(align, value, false);
            return align;
        }

        /// <summary>
        /// Align the compilation to the specified boundary. For instance, to align the next byte(s)
        /// in the compilation to a page boundary you would set the align amount to 256.
        /// </summary>
        /// <param name="amount">The amount to align</param>
        /// <returns>Returns the offset needed to align the Program Counter</returns>
        public int Align(int amount)
        {
            int align = GetAlignmentSize(LogicalPC, amount);
            LogicalPC += align;
            ProgramCounter += align;
            return align;
        }

        /// <summary>
        /// Add a range of bytes to the compilation.
        /// </summary>
        /// <param name="bytes">The collection of bytes to add</param>
        /// <param name="size">The number of bytes in the collection to add</param>
        /// <param name="ignoreEndian">Ignore the endianness when adding to the compilation</param>
        /// <param name="updateProgramCounter">Update the Program Counter automatically</param>
        public void AddBytes(IEnumerable<byte> bytes, int size, bool ignoreEndian, bool updateProgramCounter)
        {
            if (CompilingHasStarted == false)
            {
                ProgramStart = ProgramCounter;
            }
            else
            {
                int diff = ProgramCounter - (ProgramStart + Bytes.Count);
                if (diff > 0)
                    Bytes.AddRange(new byte[diff]);
            }

            if (updateProgramCounter)
            {
                ProgramCounter += size;
                LogicalPC += size;
            }

            if (ignoreEndian == false && BitConverter.IsLittleEndian != IsLittleEndian)
                bytes = bytes.Reverse();

            if (Transforms.Count > 0)
            {
                var transformed = bytes.ToList();
                for(int i = 0; i < size; i++)
                {
                    foreach(var t in Transforms)
                        transformed[i] = t(transformed[i]);
                }
                Bytes.AddRange(transformed.GetRange(0, size));
            }
            else
            {
                Bytes.AddRange(bytes.ToList().GetRange(0, size));
            }
        }

        /// <summary>
        /// Add a range of bytes to the compilation.
        /// </summary>
        /// <param name="bytes">The collection of bytes to add</param>
        /// <param name="size">The number of bytes in the collection to add</param>
        /// <param name="ignoreEndian">Ignore the endianness when adding to the compilation</param>
        public void AddBytes(IEnumerable<byte> bytes, int size, bool ignoreEndian)
        {
            AddBytes(bytes, size, ignoreEndian, true);
        }

        /// <summary>
        /// Add a range of bytes to the compilation.
        /// </summary>
        /// <param name="bytes">The collection of bytes to add</param>
        /// <param name="ignoreEndian">Ignore the endianness when adding to the compilation</param>
        public void AddBytes(IEnumerable<byte> bytes, bool ignoreEndian)
        {
            AddBytes(bytes, bytes.Count(), ignoreEndian);
        }

        /// <summary>
        /// Add a range of bytes to the compilation.
        /// </summary>
        /// <param name="bytes">The collection of bytes to add</param>
        /// <param name="size">The number of bytes in the collection to add</param>
        public void AddBytes(IEnumerable<byte> bytes, int size)
        {
            AddBytes(bytes, size, true);
        }

        /// <summary>
        /// Add a range of bytes to the compilation.
        /// </summary>
        /// <param name="Bytes">The collection of bytes to add</param>
        public void AddBytes(IEnumerable<byte> bytes)
        {
            AddBytes(bytes, true);
        }

        /// <summary>
        /// Get the compilation bytes
        /// </summary>
        /// <returns>The bytes of the compilation.</returns>
        public System.Collections.ObjectModel.ReadOnlyCollection<byte> GetCompilation()
        {
            return Bytes.AsReadOnly();
        }

        /// <summary>
        /// Get the relative offset between two addresses. Useful for calculating short jumps.
        /// </summary>
        /// <param name="address1">Current address</param>
        /// <param name="address2">Destination address</param>
        /// <returns>Returns the relative offset between the two addresses</returns>
        public int GetRelativeOffset(int address1, int address2)
        {
            int offset = address1 - address2;
            if (Math.Abs(offset) > (MaxAddress / 2))
            {
                if (offset < 0)
                    offset = Math.Abs(offset) - (MaxAddress + 1);
                else
                    offset = MaxAddress + 1 - offset;
            }
            return offset;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the program start addressed based on the value of the Program Counter
        /// when compilation first occurred.
        /// </summary>
        public int ProgramStart { get; private set; }

        /// <summary>
        /// Gets or sets a the collections of functions that apply transforms to the bytes before
        /// they are added to the compilation. The transform functions are called from last to
        /// first in the order they are pushed onto the transform stack.
        /// </summary>
        public Stack<Func<byte, byte>> Transforms { get; set; }

        /// <summary>
        /// Gets or sets the maximum address allowed for the Program Counter until
        /// it overflows.
        /// </summary>
        public int MaxAddress { get; set; }

        /// <summary>
        /// Gets or sets the byte buffer of the compilation
        /// </summary>
        List<byte> Bytes { get; set; }

        /// <summary>
        /// Gets the status of the compilation, if it is currently compiling
        /// </summary>
        bool CompilingHasStarted { get { return Bytes.Count > 0; } }

        /// <summary>
        /// Gets the actual Program Counter
        /// </summary>
        public int ProgramCounter 
        {
            get { return pc_; }
            private set 
            {
                if (value < 0 || value < pc_ || value > MaxAddress)
                    throw new InvalidPCAssignmentException(value);
                pc_ = value;
            }
        }

        /// <summary>
        /// Gets the endianness of the compilation
        /// </summary>
        public bool IsLittleEndian { get; private set; }

        /// <summary>
        /// Gets or sets the logical Program Counter
        /// </summary>
        int LogicalPC
        {
            get { return log_pc_; }
            set
            {
                if (value < 0 || value < log_pc_ || value > MaxAddress)
                    throw new InvalidPCAssignmentException(value);
                log_pc_ = value;
            }
        }

        #endregion
    }
}
