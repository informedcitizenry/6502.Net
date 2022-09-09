//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    public class FunctionValue : Value
    {
        private readonly UserFunctionDefinition _fcnDefinition;

        public FunctionValue(Sixty502DotNetParser.ArrowFuncContext arrowCtx, AssemblyServices services)
        {
            Visitor = new BlockVisitor(services);
            _fcnDefinition = UserFunctionDefinition.Declare(arrowCtx, services.Symbols);
            Type = typeof(FunctionValue); // just to get the DotNetType
        }

        public Value? Invoke(ArrayValue args)
        {
            _fcnDefinition.Visitor = Visitor;
            return _fcnDefinition.Invoke(args);
        }

        public override string ToString() => "() =>";

        public BlockVisitor Visitor { get; init; }
    }
}