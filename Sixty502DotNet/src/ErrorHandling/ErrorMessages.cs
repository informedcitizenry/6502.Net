//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    public static class Errors
    {
        public const string CannotResolve = "Quantity could not be resolved at first pass.";

        public const string ConstantAssignment = "Constant assignment requires constant right-hand side expression.";

        public const string ExpectedConstant = "Expected constant char, integer or string expression.";

        public const string ExpectedExpression = "Expected expression.";

        public const string ExpressionNotCondition = "Expression is not a condition.";

        public const string IllegalQuantity = "Illegal quantity.";

        public const string InvalidOperation = "Invalid operation.";

        public const string InvalidLabelContext = "Label is in an invalid context.";

        public const string ModeNotSupported = "Mode not supported for selected CPU.";

        public const string StringExpected = "String expected.";

        public const string SymbolExistsError = "Symbol '{0}' cannot be changed.";

        public const string TypeMismatchError = "Type mismatch error.";

        public const string UnexpectedExpression = "Unexpected expression.";
    }
}
