//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;

namespace Sixty502DotNet
{
    /// <summary>
    /// A class that refers to a value of any type, and provides convience
    /// methods to cast the value to another type.
    /// </summary>
    public class Value
    {
        /// <summary>
        /// Construct a new instance of the <see cref="Value"/> class.
        /// </summary>
        public Value()
        {
            Data = double.NaN;
            Type = null;
        }

        /// <summary>
        /// Construct a new instance of the <see cref="Value"/> class.
        /// </summary>
        /// <param name="other">The other <see cref="Value"/> to copy.</param>
        public Value([NotNull] Value other)
        {
            Data = other.Data;
            Type = other.Type;
        }

        /// <summary>
        /// Construct a new instance of the <see cref="Value"/> class.
        /// </summary>
        /// <param name="val">The data for this value. The data's type is also
        /// inspected.</param>
        public Value([NotNull] object val)
        {
            Data = val;
            Type = Data.GetType();
            if (Type == typeof(string))
            {
                var str = (string)Data;
                if (str[0] == '\'')
                {
                    Type = typeof(char);
                }
                if (str[0] == '"' || str[0] == '\'')
                {
                    Data = str[1..^1];
                }
            }
            else if (Type == typeof(long))
            {
                Type = typeof(double);
                Data = Convert.ToDouble(val);
            }
        }

        /// <summary>
        /// Determine if this value's data is equal to that of the
        /// <see cref="Value"/> object's.
        /// </summary>
        /// <param name="other">The other <see cref="Value"/>
        /// instance.</param>
        /// <returns><c>true</c> if both items have data that are equal in value,
        /// <c>false</c> otherwise.</returns>
        public virtual bool Equals(Value? other)
        {
            if (IsPrimitiveType && other?.IsPrimitiveType == true)
            {
                if (IsNumeric && other.IsNumeric == true)
                {
                    return ToDouble() == other.ToDouble();
                }
                if (IsString && other.IsString || (DotNetType == TypeCode.Char && other.DotNetType == TypeCode.Char))
                {
                    return ((string)Data).Equals(other.ToString(true));
                }
                return DotNetType == other.DotNetType && ToBool() == other.ToBool();
            }
            return false;
        }

        /// <summary>
        /// Get the value's hash code.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode() => Data.GetHashCode();

        /// <summary>
        /// Determine if this value is equal to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns><c>true</c> if both are equal, <c>false</c>
        /// otherwise.</returns>
        public override bool Equals(object? obj)
        {
            if (obj is Value other) return Equals(other);
            return false;
        }

        public virtual bool IsDefined => DotNetType != TypeCode.Empty;

        /// <summary>
        /// Get the index value as an integer.
        /// </summary>
        /// <param name="index">The index value.</param>
        /// <returns>The integer form of the index if the index value is
        /// itself an integer, otherwise <c>-1</c>.</returns>
        protected int GetIndexValue(Value index)
        {
            int i = -1;
            if (index.IsIntegral)
            {
                i = index.ToInt();
                if (i < 0) { i = ElementCount + i; }
                if (i >= ElementCount) i = -1;
            }
            return i;
        }

        /// <summary>
        /// Get the range values of the range start and end.
        /// </summary>
        /// <param name="start">The start of the range</param>
        /// <param name="end">The end of the range.</param>
        /// <returns>The range as an integer tuple if the start and end
        /// are both integers and both valid, otherwise a default range
        /// where both the start and end values are <c>-1</c>.</returns>
        protected (int startIndex, int endIndex) GetRangeValues(Value start, Value end)
        {
            int s = GetIndexValue(start);
            int e = GetIndexValue(end);
            if (s >= 0 && e >= 0 && s < e)
            {
                return (s, e);
            }
            return (-1, -1);
        }

        /// <summary>
        /// Try to get an element at the specified index.
        /// </summary>
        /// <param name="index">The index at which to retrieved the
        /// element in this value's collection.</param>
        /// <param name="value">The element value.</param>
        /// <returns><c>true</c> if the element was successfully retrieved
        /// at the index, <c>false</c> otherwise.</returns>
        public virtual bool TryGetElement(Value index, out Value value)
        {
            if (Type == typeof(string) && index.IsIntegral)
            {
                var i = GetIndexValue(index);
                if (i >= 0)
                {
                    value = new Value($"'{((string)Data)[i]}'");
                    return true;
                }
            }
            value = Undefined;
            return false;
        }

        /// <summary>
        /// Try to get the subsequence of elements in a specified range.
        /// </summary>
        /// <param name="start">The range start value.</param>
        /// <param name="end">The range end value.</param>
        /// <param name="value">The subsequence value.</param>
        /// <returns><c>true</c> if the subsequence was sucessfully retrieved
        /// at the given range, <c>false</c> otherwise.</returns>
        public virtual bool TryGetElements(Value start, Value end, out Value value)
        {
            if (Type == typeof(string))
            {
                (int startIndex, int endIndex) = GetRangeValues(start, end);
                if (startIndex >= 0 && endIndex >= 0)
                {
                    var len = endIndex - startIndex + 1;
                    var str = (string)Data;
                    value = len == 1 ? new Value($"'{str[startIndex]}'")
                        : new Value($"\"{str.Substring(startIndex, len)}\"");
                    return true;
                }
            }
            value = Undefined;
            return false;
        }

        /// <summary>
        /// Convert the value's data to a <see cref="bool"/> and return its
        /// boolean value.
        /// </summary>
        /// <returns><c>true</c>if the value's data type is a <see cref="bool"/>
        /// and the boolean data is also true, <c>false</c> otherwise.</returns>
        public bool ToBool() => Type == typeof(bool) && (bool)Data;

        /// <summary>
        /// Set the value data to another <see cref="Value"/>.
        /// </summary>
        /// <param name="other">The other <see cref="Value"/> object.</param>
        /// <returns><c>true</c> if this value's data was able to be set
        /// to the other's, <c>false</c> otherwise.</returns>
        public virtual bool SetAs(Value other)
        {
            if (other is Value otherValue)
            {
                if (!IsDefined || Type == otherValue.Type || (IsNumeric && otherValue.IsNumeric))
                {
                    Data = otherValue.Data;
                    if (!IsDefined)
                    {
                        Type = otherValue.Type;
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Convert the data of this value to the type specified in the type
        /// parameter.
        /// </summary>
        /// <typeparam name="T">The type to which to cast this data.</typeparam>
        /// <returns>The data of this value converted to the type in the
        /// type parameter if successful, otherwise the default value for the
        /// specified type.</returns>
        public T? ToObject<T>()
        {
            try { return (T)Data; }
            catch { return default; }
        }

        /// <summary>
        /// Convert the data of this value to the value type specified in the
        /// type parameter.
        /// </summary>
        /// <typeparam name="T">The value type to which to cast this data.
        /// </typeparam>
        /// <returns>The data of this value converted to the value type in the
        /// type parameter if successful, otherwise the default value for the
        /// specified type..</returns>
        public T ToValue<T>() where T : struct
        {
            try
            {
                return (T)Convert.ChangeType(Data, typeof(T));
            }
            catch { return default; }
        }

        /// <summary>
        /// Convert the value data to an <see cref="int"/>.
        /// </summary>
        /// <returns>The value data as an integer if successfully converted,
        /// otherwise the minimum value of a signed 32-bit integer.</returns>
        public int ToInt()
        {
            try { return Convert.ToInt32(Data); } catch { return int.MinValue; }
        }

        /// <summary>
        /// Convert the value data to a <see cref="char"/>.
        /// </summary>
        /// <returns>The value data as a <see cref="char"/> if successfully
        /// converted, otherwise the minimum value of a <see cref="char"/>.
        /// </returns>
        public char ToChar()
        {
            try { return Convert.ToChar(Data); } catch { return char.MinValue; }
        }

        /// <summary>
        /// Convert the value data to a <see cref="long"/>.
        /// </summary>
        /// <returns>The value data as a <see cref="long"/> if successfully
        /// converted, otherwise the minimum value of signed 64-bit
        /// integer.</returns>
        public long ToLong()
        {
            try { return Convert.ToInt64(Data); } catch { return long.MinValue; }
        }

        /// <summary>
        /// Convert the value data to a <see cref="double"/>.
        /// </summary>
        /// <returns>The value data as a <see cref="double"/> if successfully
        /// convertd, otherwise <c>NaN</c>.</returns>
        public double ToDouble()
        {
            try { return Convert.ToDouble(Data); } catch { return double.NaN; }
        }

        public string ToString(bool unquoted)
        {
            if (unquoted || (DotNetType != TypeCode.Char && DotNetType != TypeCode.String))
                return Data?.ToString() ?? "null";
            return IsString ? $"\"{(string)Data}\"" : $"'{(string)Data}'";
        }

        public override string ToString()
        {
            if (IsIntegral)
            {
                return ToInt().ToString();
            }
            if (IsNumeric)
            {
                return ToDouble().ToString();
            }
            if (IsString)
            {
                return ToString(false);
            }
            if (DotNetType == TypeCode.Char)
            {
                return $"'{Data}'";
            }
            if (DotNetType == TypeCode.Boolean)
            {
                return ToBool().ToString().ToLower();
            }
            if (IsDefined)
            {
                return "[Object]";
            }
            return "undefined";
        }

        public bool IsNumeric => Type?.IsPrimitive == true && Type != typeof(bool) && Type != typeof(char);

        public bool IsString => Type == typeof(string);

        public bool IsPrimitiveType => Type?.IsPrimitive == true || IsString;

        public bool IsIntegral
            => Type == typeof(byte) ||
               Type == typeof(int) ||
               Type == typeof(uint);

        /// <summary>
        /// Get or set the value object's underlying data.
        /// </summary>
        protected object Data { get; set; }

        /// <summary>
        /// Get or set the value object's underlying data type.
        /// </summary>
        protected Type? Type { get; set; }

        public virtual int ElementCount
            => Type == typeof(string) ? ((string)Data).Length : 0;

        public TypeCode DotNetType => Type.GetTypeCode(Type);

        /// <summary>
        /// Get the undefined value, an object that is not null but is not
        /// yet defined.
        /// </summary>
        /// <returns>The value as an undefined object.</returns>
        public static Value Undefined => new();

        /// <summary>
        /// Get a value that represents a <see cref="double.NaN"/> value.
        /// </summary>
        /// <returns>The value as a <see cref="double"/> whose value is
        /// not a number.</returns>
        public static Value NaN => new()
        {
            Data = double.NaN,
            Type = typeof(double)
        };
    }
}
