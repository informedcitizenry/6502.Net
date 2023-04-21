//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Text;

namespace Sixty502DotNet.Shared;

/// <summary>
/// The tagged value type.
/// </summary>
public enum ValueType
{
    /// <summary>
    /// The undefined value
    /// </summary>
    Undefined = 0,
    /// <summary>
    /// The null value
    /// </summary>
    Null,
    /// <summary>
    /// The array type
    /// </summary>
    Array,
    /// <summary>
    /// The boolean type
    /// </summary>
    Boolean,
    /// <summary>
    /// The callable type
    /// </summary>
    Callable,
    /// <summary>
    /// The character type
    /// </summary>
    Char,
    /// <summary>
    /// The dictionary type
    /// </summary>
    Dictionary,
    /// <summary>
    /// The integer type
    /// </summary>
    Integer,
    /// <summary>
    /// The number type (which can be an integer or float)
    /// </summary>
    Number,
    /// <summary>
    /// The raw value type
    /// </summary>
    Raw,
    /// <summary>
    /// The string type
    /// </summary>
    String,
    /// <summary>
    /// The tuple type
    /// </summary>
    Tuple
}

/// <summary>
/// Encapsulates the result of an expression evaluation, whether a scalar value
/// like a numeric value, or a function expression. This class must be
/// inherited.
/// </summary>
public abstract class ValueBase :
    IEquatable<ValueBase>,
    IComparable<ValueBase>,
    IConvertible
{
    /// <summary>
    /// Construct a new instance of the <see cref="ValueBase"/> class.
    /// </summary>
    protected ValueBase()
    {
        TextEncoding = Encoding.Default;
        ValueType = ValueType.Undefined;
        JsonPath = string.Empty;
        IsDefined = true;
    }

    /// <summary>
    /// Determines whether the value of this <see cref="ValueBase"/> is
    /// compatible with another's.
    /// </summary>
    /// <param name="other">The other <see cref="ValueBase"/> object.</param>
    /// <returns><c>true</c> if the types of the two values are compatible,
    /// <c>false</c> otherwise.</returns>
    public virtual bool TypeCompatible(ValueBase other)
    {
        return GetType() == other.GetType();
    }

    /// <summary>
    /// Attempts to set the underlying value of this <see cref="ValueBase"/> to
    /// another's. This method first checks if the two values are type-
    /// compatible.
    /// </summary>
    /// <param name="other">The other <see cref="ValueBase"/> object.</param>
    public void SetAs(ValueBase other)
    {
        if (TypeCompatible(other))
        {
            OnSetAs(other);
        }
    }

    /// <summary>
    /// Get a range of elements within this <see cref="ValueBase"/>'s collection
    /// of elements, if the value has elements.
    /// </summary>
    /// <param name="range">A <see cref="Range"/> of elements.</param>
    /// <returns>A new <see cref="ValueBase"/> containing the range of
    /// elements specified, if the value contains elements..</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase FromRange(Range range)
    {
        throw new InvalidOperationError(Expression?.Start);
    }

    /// <summary>
    /// Reverse the element's in the value.
    /// </summary>
    /// <exception cref="TypeMismatchError"></exception>
    public virtual void Reverse()
    {
        throw new TypeMismatchError(Expression?.Start);
    }

    /// <summary>
    /// Slices the <see cref="ValueBase"/> into an array of values, if the
    /// value has elements.
    /// </summary>
    /// <param name="start">The start position of the values.</param>
    /// <param name="length">The length of the array.</param>
    /// <returns></returns>
    /// <exception cref="TypeMismatchError"></exception>
    public virtual ValueBase[] Slice(int start, int length)
    {
        if (Expression != null)
        {
            throw new TypeMismatchError(Expression);
        }
        throw new TypeMismatchError(Expression?.Start);
    }

    /// <summary>
    /// Sorts the value's internal elements, if the value has elements.
    /// </summary>
    /// <exception cref="TypeMismatchError"></exception>
    public virtual void Sort()
    {
        throw new TypeMismatchError(Expression?.Start);
    }

    /// <summary>
    /// Sort the value's internal elements according to a <see cref="ValueComparer"/>,
    /// if the value has elements.
    /// </summary>
    /// <param name="comparer">The comparer method used to sort the value's
    /// elements.</param>
    /// <exception cref="TypeMismatchError"></exception>
    public virtual void Sort(ValueComparer comparer)
    {
        throw new TypeMismatchError(Expression?.Start);
    }

    public virtual ValueBase this[int index]
    {
        get => throw new TypeMismatchError(Expression?.Start);
        set => throw new TypeMismatchError(Expression?.Start);
    }

    public virtual ValueBase this[ValueBase key]
    {
        get => throw new TypeMismatchError(Expression?.Start);
        set => throw new TypeMismatchError(Expression?.Start);
    }

    /// <summary>
    /// Sets the underlying value of this <see cref="ValueBase"/> to that of
    /// another. This method must be inherited.
    /// </summary>
    /// <param name="other">The other <see cref="ValueBase"/> to which to
    /// set the underlying value.</param>
    protected abstract void OnSetAs(ValueBase other);

    /// <summary>
    /// Attempt the represent this <see cref="ValueBase"/> as a boolean. 
    /// </summary>
    /// <returns>A boolean representation of the value, if the underlying
    /// type is a boolean.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public virtual bool AsBool()
    {
        if (Expression != null)
        {
            throw new TypeMismatchError(Expression);
        }
        throw new TypeMismatchError(Expression?.Start);
    }

    /// <summary>
    /// Attempt the represent this <see cref="ValueBase"/> as a boolean. 
    /// </summary>
    /// <returns>A boolean representation of the value, if the underlying
    /// type is a boolean.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public virtual bool ToBoolean(IFormatProvider? provider)
    {
        if (Expression != null)
        {
            throw new TypeMismatchError(Expression);
        }
        throw new TypeMismatchError(Expression?.Start);
    }

    /// <summary>
    /// Attempt the represent this <see cref="ValueBase"/> as a double. 
    /// </summary>
    /// <returns>The double representation of the value, if the underlying
    /// type is a numeric or char value.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public virtual double AsDouble()
    {
        if (Expression != null)
        {
            throw new TypeMismatchError(Expression);
        }
        throw new TypeMismatchError(Expression?.Start);
    }

    /// <summary>
    /// Attempt the represent this <see cref="ValueBase"/> as a decimal. 
    /// </summary>
    /// <returns>The double representation of the value, if the underlying
    /// type is a numeric or char value.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public decimal ToDecimal(IFormatProvider? provider)
    {
        return new decimal(AsDouble());
    }

    /// <summary>
    /// Attempt the represent this <see cref="ValueBase"/> as a single floating
    /// point number.</summary>
    /// <returns>The double representation of the value, if the underlying
    /// type is a numeric or char value.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public float ToSingle(IFormatProvider? provider)
    {
        return Convert.ToSingle(AsDouble());
    }

    /// <summary>
    /// Attempt the represent this <see cref="ValueBase"/> as a double. 
    /// </summary>
    /// <returns>The double representation of the value, if the underlying
    /// type is a numeric or char value.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public double ToDouble(IFormatProvider? provider)
    {
        return AsDouble();
    }

    /// <summary>
    /// Attempt to represent this <see cref="ValueBase"/> as an integer.
    /// </summary>
    /// <returns>The integer representation of the value, if the underlying
    /// type is a numeric or char value.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public virtual int AsInt()
    {
        if (Expression != null)
        {
            throw new TypeMismatchError(Expression);
        }
        throw new TypeMismatchError(Expression?.Start);
    }

    /// <summary>
    /// Attempt to represent this <see cref="ValueBase"/> as a signed 16-bit
    /// integer.</summary>
    /// <returns>The integer representation of the value, if the underlying
    /// type is a numeric or char value.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public short ToInt16(IFormatProvider? provider)
    {
        return Convert.ToInt16(AsInt());
    }

    /// <summary>
    /// Attempt to represent this <see cref="ValueBase"/> as a signed 32-bit
    /// integer.</summary>
    /// <returns>The integer representation of the value, if the underlying
    /// type is a numeric or char value.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public int ToInt32(IFormatProvider? provider)
    {
        return AsInt();
    }

    /// <summary>
    /// Attempt to represent this <see cref="ValueBase"/> as a signed 64-bit
    /// integer.</summary>
    /// <returns>The integer representation of the value, if the underlying
    /// type is a numeric or char value.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public long ToInt64(IFormatProvider? provider)
    {
        return Convert.ToInt64(AsDouble());
    }

    /// <summary>
    /// Attempt to represent this <see cref="ValueBase"/> as a signed 8-bit
    /// integer.</summary>
    /// <returns>The integer representation of the value, if the underlying
    /// type is a numeric or char value.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public sbyte ToSByte(IFormatProvider? provider)
    {
        return Convert.ToSByte(AsInt());
    }

    /// <summary>
    /// Attempt to represent this <see cref="ValueBase"/> as an unsigned 8-bit
    /// integer.</summary>
    /// <returns>The integer representation of the value, if the underlying
    /// type is a numeric or char value.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public byte ToByte(IFormatProvider? provider)
    {
        return Convert.ToByte(AsInt());
    }

    /// <summary>
    /// Attempt to represent this <see cref="ValueBase"/> as an unsigned 16-bit
    /// integer.</summary>
    /// <returns>The integer representation of the value, if the underlying
    /// type is a numeric or char value.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public ushort ToUInt16(IFormatProvider? provider)
    {
        return Convert.ToUInt16(AsInt());
    }

    /// <summary>
    /// Attempt to represent this <see cref="ValueBase"/> as an unsigned 32-bit
    /// integer.</summary>
    /// <returns>The integer representation of the value, if the underlying
    /// type is a numeric or char value.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public uint ToUInt32(IFormatProvider? provider)
    {
        return Convert.ToUInt32(AsDouble());
    }

    /// <summary>
    /// Attempt to represent this <see cref="ValueBase"/> as an unsigned 64-bit
    /// integer.</summary>
    /// <returns>The integer representation of the value, if the underlying
    /// type is a numeric or char value.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public ulong ToUInt64(IFormatProvider? provider)
    {
        return Convert.ToUInt64(AsDouble());
    }

    /// <summary>
    /// Attempt the represent this <see cref="ValueBase"/> as a string. Note
    /// this method is not the same as the <see cref="object.ToString"/> method,
    /// as it first checks whether the underlying type is a string.
    /// </summary>
    /// <returns>A string representation of the value, if the underlying
    /// type is a string.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public virtual string AsString()
    {
        if (Expression != null)
        {
            throw new TypeMismatchError(Expression);
        }
        throw new TypeMismatchError(Expression?.Start);
    }

    /// <summary>
    /// Attempt the represent this <see cref="ValueBase"/> as a string. Note
    /// this method is not the same as the <see cref="object.ToString"/> method,
    /// as it first checks whether the underlying type is a string.
    /// </summary>
    /// <returns>A string representation of the value, if the underlying
    /// type is a string.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public string ToString(IFormatProvider? provider)
    {
        return AsString();
    }

    public DateTime ToDateTime(IFormatProvider? provider)
    {
        throw new NotImplementedException();
    }

    public object ToType(Type conversionType, IFormatProvider? provider)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the size of the value.
    /// </summary>
    /// <returns>The value's size.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public virtual int Size() => throw new TypeMismatchError(Expression?.Start);

    /// <summary>
    /// Attempt the represent this <see cref="ValueBase"/> as a char. 
    /// </summary>
    /// <returns>A char representation of the value, if the underlying
    /// type is a char.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public virtual char AsChar()
    {
        if (Expression != null)
        {
            throw new TypeMismatchError(Expression);
        }
        throw new TypeMismatchError(Expression?.Start);
    }

    /// <summary>
    /// Attempt the represent this <see cref="ValueBase"/> as a char. 
    /// </summary>
    /// <returns>A char representation of the value, if the underlying
    /// type is a char.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public char ToChar(IFormatProvider? provider)
    {
        return AsChar();
    }

    public override int GetHashCode()
    {
        throw new TypeMismatchError();
    }

    /// <summary>
    /// Casts the value into a list of its elements.
    /// </summary>
    /// <returns>A collection of <see cref="ValueBase"/> values.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public virtual IList<ValueBase> ToList()
    {
        throw new TypeMismatchError();
    }

    /// <summary>
    /// Casts the value as a dictionary of values.
    /// </summary>
    /// <returns>A collection of <see cref="ValueBase"/> values.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public virtual IDictionary<ValueBase, ValueBase> ToDictionary()
    {
        throw new TypeMismatchError();
    }

    public bool Equals(ValueBase? other)
    {
        if (other?.TypeCompatible(this) == true)
        {
            return OnEqualTo(other);
        }
        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is ValueBase val)
        {
            return Equals(val);
        }
        return false;
    }

    /// <summary>
    /// Get the positive form of the value.
    /// </summary>
    /// <returns>The <see cref="ValueBase"/>'s positive form.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase Positive() => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Get the negative form of the value.
    /// </summary>
    /// <returns>The <see cref="ValueBase"/>'s negative form.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase Negative() => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Get the logical negation of the value.
    /// </summary>
    /// <returns>The <see cref="ValueBase"/>'s logical negation.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase Not() => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Get the bitwise complement form of the value.
    /// </summary>
    /// <returns>The <see cref="ValueBase"/>'s bitwise complement.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase Complement() => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Get the least significant byte of the value.
    /// </summary>
    /// <returns>The <see cref="ValueBase"/>'s least significant byte.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase LSB() => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Get the most significant byte of the value.
    /// </summary>
    /// <returns>The <see cref="ValueBase"/>'s most significant byte.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase MSB() => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Get the least significant word (2 bytes) of the value.
    /// </summary>
    /// <returns>The <see cref="ValueBase"/>'s least significant word, which
    /// is the values lowest two bytes.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase Word() => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns the bank byte of the value.
    /// </summary>
    /// <returns>The <see cref="ValueBase"/>'s bank value, which is the byte
    /// of the 64KiB page.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase Bank() => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns the higher word of the 24-bit value.
    /// </summary>
    /// <returns>The <see cref="ValueBase"/>'s higher word, which is the values of
    /// the higher two bytes in a 24-bit value.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase HigherWord() => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns the value raised to a <see cref="ValueBase"/> exponent.
    /// </summary>
    /// <param name="rhs">The right-hand-side <see cref="ValueBase"/>.</param>
    /// <returns>The exponential of this base <see cref="ValueBase"/> raised to
    /// the power of the specified <see cref="ValueBase"/> exponent.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase PowerOf(ValueBase rhs) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns the product of this value and another <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="rhs">The right-hand side <see cref="ValueBase"/>.</param>
    /// <returns>The product of this <see cref="ValueBase"/> multiplier and the
    /// specified <see cref="ValueBase"/> multiplicand.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase MultiplyBy(ValueBase rhs) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns the modulus of this value to another <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="rhs">The right-hand side <see cref="ValueBase"/>.</param>
    /// <returns>The remainder of this <see cref="ValueBase"/> divident and the
    /// specified <see cref="ValueBase"/> divisor.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase Mod(ValueBase rhs) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns the quotient of this value and another <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="rhs">The right-hand side <see cref="ValueBase"/>.</param>
    /// <returns>The quotient of this <see cref="ValueBase"/> divident and the
    /// specified <see cref="ValueBase"/> divisor.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase DivideBy(ValueBase rhs) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns the sum or concatenation of this value and another <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="rhs">The right-hand side <see cref="ValueBase"/>.</param>
    /// <returns>Either the sum of this <see cref="ValueBase"/> term and the specified
    /// <see cref="ValueBase"/> term, or this value's concatenation with
    /// the other.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase AddWith(ValueBase rhs) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns the difference of this value and another <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="rhs">The right-hand side <see cref="ValueBase"/>.</param>
    /// <returns>The difference of this <see cref="ValueBase"/> minuend and the specified
    /// <see cref="ValueBase"/> subtrahend.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase Subtract(ValueBase rhs) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns the value bit-shifted to the left by the specified <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="rhs">The right-hand side <see cref="ValueBase"/>.</param>
    /// <returns>The value leftwise bit-shifted by the amount in the specified
    /// <see cref="ValueBase"/>.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase LeftShift(ValueBase rhs) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns the value bit-shifted to the right by the specified <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="rhs">The right-hand side <see cref="ValueBase"/>.</param>
    /// <returns>The value rightwise bit-shifted by the amount in the specified
    /// <see cref="ValueBase"/>.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase RightShift(ValueBase rhs) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns the value bit-shifted to the right by the specified <see cref="ValueBase"/>,
    /// with its original sign preserved.
    /// </summary>
    /// <param name="rhs">The right-hand side <see cref="ValueBase"/>.</param>
    /// <returns>The value rightwise bit-shifted by the amount in the specified
    /// <see cref="ValueBase"/> with its sign preserved.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase UnsignedRightShift(ValueBase rhs) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns whether this value is greater than another <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="rhs">The right-hand side <see cref="ValueBase"/>.</param>
    /// <returns>A <see cref="ValueBase"/> possibly but not necessarily true or
    /// that answers whether this <see cref="ValueBase"/> is greater than the
    /// specified <see cref="ValueBase"/>.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase GreaterThan(ValueBase rhs) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns whether this value is greater than or equal to another
    /// <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="rhs">The right-hand side <see cref="ValueBase"/>.</param>
    /// <returns>A <see cref="ValueBase"/> possibly but not necessarily true or
    /// that answers whether this <see cref="ValueBase"/> is greater than or
    /// equal to the specified <see cref="ValueBase"/>.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase GTE(ValueBase rhs) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns whether this value is less than or equal to another
    /// <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="rhs">The right-hand side <see cref="ValueBase"/>.</param>
    /// <returns>A <see cref="ValueBase"/> possibly but not necessarily true or
    /// that answers whether this <see cref="ValueBase"/> is less than or
    /// equal to the specified <see cref="ValueBase"/>.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase LTE(ValueBase rhs) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns whether this value is less than another <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="rhs">The right-hand side <see cref="ValueBase"/>.</param>
    /// <returns>A <see cref="ValueBase"/> possibly but not necessarily true or
    /// that answers whether this <see cref="ValueBase"/> is less than the
    /// specified <see cref="ValueBase"/>.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase LessThan(ValueBase rhs) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns the bitwise AND of this value and another <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="rhs">The right-hand side <see cref="ValueBase"/>.</param>
    /// <returns>The bitwise AND the specified
    /// <see cref="ValueBase"/>.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase BitwiseAnd(ValueBase rhs) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns the bitwise XOR of this value and another <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="rhs">The right-hand side <see cref="ValueBase"/>.</param>
    /// <returns>The bitwise OR the specified
    /// <see cref="ValueBase"/>.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase BitwiseXor(ValueBase rhs) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns the bitwise OR of this value and another <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="rhs">The right-hand side <see cref="ValueBase"/>.</param>
    /// <returns>The bitwise OR the specified
    /// <see cref="ValueBase"/>.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase BitwiseOr(ValueBase rhs) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns the logical AND of this value and another <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="rhs">The right-hand side <see cref="ValueBase"/>.</param>
    /// <returns>The logical AND the specified
    /// <see cref="ValueBase"/>.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase And(ValueBase rhs) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns the logical OR of this value and another <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="rhs">The right-hand side <see cref="ValueBase"/>.</param>
    /// <returns>The logical OR the specified
    /// <see cref="ValueBase"/>.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase Or(ValueBase rhs) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns the increment of this value.
    /// </summary>
    /// <returns>The increment of the value.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase Increment() => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns the decrement of this value.
    /// </summary>
    /// <returns>The decrement of the value.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual ValueBase Decrement() => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns whether the value contains a specified value.
    /// </summary>
    /// <param name="value">The value to check if it is contained in this value.</param>
    /// <returns><c>true</c> if the specified value is a member of this value,
    /// <c>false</c> otherwise.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual bool Contains(ValueBase value) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns whether the value contains a specified key.
    /// </summary>
    /// <param name="value">The key to check.</param>
    /// <returns><c>true</c> if the specified key is contained within this value,
    /// <c>false</c> otherwise.</returns>
    /// <exception cref="InvalidOperationError"></exception>
    public virtual bool ContainsKey(ValueBase value) => throw new InvalidOperationError(Expression?.Start);

    /// <summary>
    /// Returns whether this value is identical to the object being compared with.
    /// </summary>
    /// <param name="other">The other value to compare.</param>
    /// <returns><c>true</c> if the two items are equal, <c>false</c>
    /// otherwise.</returns>
    public bool IsIdenticalTo(ValueBase other)
    {
        return IsObject && ReferenceEquals(this, other);
    }

    /// <summary>
    /// Casts the value to a <see cref="object"/>.
    /// </summary>
    /// <returns>The value as a .Net <see cref="object"/>.</returns>
    public abstract object? ToObject();

    /// <summary>
    /// Casts the value to the specified type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The data of this value converted to the value type in the
    /// type parameter if successful, otherwise the default value for the
    /// specified type.</returns>
    public T? ToObject<T>()
    {
        try
        {
            object? o = ToObject();
            if (o != null)
            {
                return (T)Convert.ChangeType(o, typeof(T));
            }
            return default;
        }
        catch { return default; }
    }

    /// <summary>
    /// Get the representation of the value as a byte array.
    /// </summary>
    /// <returns>The value as an array of bytes.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public virtual byte[] ToBytes() => throw new TypeMismatchError(Expression?.Start);

    /// <summary>
    /// Get the representation of the value as a byte array in a specified order.
    /// </summary>
    /// <param name="little">If the order should be little endian.</param>
    /// <returns>The value as an array of bytes.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public virtual byte[] ToEndianBytes(bool little) => throw new TypeMismatchError(Expression?.Start);

    /// <summary>
    /// Compare the value to another.
    /// </summary>
    /// <param name="other">The other value to compare.</param>
    /// <returns></returns>
    /// <exception cref="TypeMismatchError"></exception>
    public virtual int CompareTo(ValueBase? other)
    {
        if (other?.TypeCompatible(this) != true)
        {
            throw new TypeMismatchError();
        }
        return OnCompareTo(other);
    }

    public static implicit operator ValueBase(string value) => new StringValue($"\"{value}\"");

    /// <summary>
    /// Get the value's type name.
    /// </summary>
    /// <returns></returns>
    public abstract string TypeName();

    /// <summary>
    /// Perform an equality comparison between this <see cref="ValueBase"/>
    /// and another, a type-safe operation. This method must be inherited.
    /// </summary>
    /// <param name="other">The other <see cref="ValueBase"/> object.</param>
    /// <returns><c>true</c> if the two values are equal, <c>false</c>
    /// otherwise.</returns>
    protected abstract bool OnEqualTo(ValueBase other);

    /// <summary>
    /// Get the original parsed expression the value was derived from.
    /// </summary>
    public SyntaxParser.ExprContext? Expression { get; set; }

    /// <summary>
    /// Get the value as a copy.
    /// </summary>
    /// <returns></returns>
    public virtual ValueBase AsCopy() => this;

    /// <summary>
    /// Get the comparison with the other <see cref="ValueBase"/>. This method
    /// must be inherited.
    /// </summary>
    /// <param name="other">The other <see cref="ValueBase"/> object to which
    /// to compare.</param>
    /// <returns></returns>
    protected abstract int OnCompareTo(ValueBase other);

    /// <summary>
    /// Get the .Net typecode of this value.
    /// </summary>
    /// <returns></returns>
    public TypeCode GetTypeCode()
    {
        return ValueType switch
        {
            ValueType.Number    => TypeCode.Double,
            ValueType.Integer   => TypeCode.Int64,
            ValueType.Boolean   => TypeCode.Boolean,
            ValueType.Char      => TypeCode.Char,
            ValueType.String    => TypeCode.String,
            ValueType.Null      => TypeCode.DBNull,
            _                   => TypeCode.Object
        };
    }

    /// <summary>
    /// Get or set the JSON path of this value as a JSON data element.
    /// </summary>
    public string JsonPath { get; set; }

    /// <summary>
    /// Get whether the value is defined.
    /// </summary>
    public bool IsDefined { get; init; }

    /// <summary>
    /// Get whether the value is a numeric type.
    /// </summary>
    public bool IsNumeric => ValueType == ValueType.Integer || ValueType == ValueType.Number;

    /// <summary>
    /// Get the value's element count, if the value is of a type that contains
    /// elements.
    /// </summary>
    public virtual int Count => throw new TypeMismatchError();

    /// <summary>
    /// Get whether the value represents a collection
    /// </summary>
    public bool IsCollection => ValueType == ValueType.Array ||
                                ValueType == ValueType.Dictionary ||
                                ValueType == ValueType.String ||
                                ValueType == ValueType.Tuple;

    /// <summary>
    /// Get whether the value is an object (non-primitive) type.
    /// </summary>
    public bool IsObject => ValueType == ValueType.Array ||
                            ValueType == ValueType.Callable ||
                            ValueType == ValueType.Dictionary ||
                            ValueType == ValueType.String ||
                            ValueType == ValueType.Tuple;

    /// <summary>
    /// Get or set the value's prototype
    /// </summary>
    public virtual Prototype? Prototype { get; set; }

    /// <summary>
    /// Get or set the value's parent value. This is particuarly useful for
    /// deserializing JSON into <see cref="ValueBase"/>'s.
    /// </summary>
    public ValueBase? Parent { get; set; }

    /// <summary>
    /// Get or set the value's text encoding for representation of textual
    /// data as numeric values.
    /// </summary>
    public Encoding TextEncoding { get; set; }

    /// <summary>
    /// Gets the value's <see cref="ValueType"/>.
    /// </summary>
    public ValueType ValueType { get; protected set; }
}

