namespace Sixty502DotNet.Shared.Eval;

public enum TypeTag
{
    Array,
    String,
    Boolean,
    Char,
    Dictionary,
    Float,
    Function,
    Int,
    Int128,
    Address,
    Resolver,
    Tuple,
    Enumerable,
    Undefined
}


public static class ValueTypeExtensions
{
    public static string Stringified(this TypeTag typeTag)
    {
        return typeTag switch
        {
            TypeTag.Char or
                TypeTag.String => "Char or String",
            TypeTag.Address or TypeTag.Int or TypeTag.Int128 or TypeTag.Float => "Address or Number",
            TypeTag.Resolver => "type",
            _ => typeTag.ToString()
        };
    }
}