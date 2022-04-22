
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;

namespace Sixty502DotNet.Tests
{
    public abstract class TestBase
    {
        protected AssemblyServices Services { get; set; }
        protected BlockVisitor Visitor { get; set; }

        public TestBase()
        {
            Services = new AssemblyServices(Options.FromArgs(new string[] { "test.dll", "--list=test_list.a65" }));
            Visitor = new CodeGenVisitor(Services, new M65xx(Services, "6502"));
        }

        protected Value ParseExpression(string source)
        {
            var parser = ParseSource(source, Services.Symbols);
            var exprTree = parser.expr();
            if (exprTree != null)
            {
                return Services.ExpressionVisitor.Visit(exprTree);
            }
            return Value.Undefined();
        }

        protected Value ParseExpressionList(string source)
        {
            var parser = ParseSource(source, Services.Symbols);
            var exprListTree = parser.expressionList();
            if (exprListTree != null)
            {
                return Services.ExpressionVisitor.Visit(exprListTree);
            }
            return Value.Undefined();
        }

        protected Sixty502DotNetParser ParseSource(string source, SymbolManager table, bool caseSensitive = false)
        {
            Services.Log.Clear();
            Services.Output.Reset();
            var input = CharStreams.fromString(source);
            var preprocessor = new Preprocessor(Services, input);
            var stream = new CommonTokenStream(preprocessor.Lexer);
            var parser = new Sixty502DotNetParser(stream)
            {
                Symbols = new SymbolManager(caseSensitive)
            };
            parser.Interpreter.PredictionMode = PredictionMode.SLL;
            parser.RemoveErrorListeners();
            parser.AddErrorListener(Services.Log);
            parser.Symbols = table;
            return parser;
        }

        protected Sixty502DotNetParser ParseSource(string source, bool reset, bool caseSensitive = false)
        {
            if (reset)
            {
                var args = caseSensitive ?
                    new string[] { "-C", "--list=test_list.a65" } :
                    new string[] { "--list=test_list.a65" };
                Services = new AssemblyServices(Options.FromArgs(args));
                Visitor = new CodeGenVisitor(Services, new M65xx(Services, Services.Options.CPU ?? "6502"));
            }
            return ParseSource(source, Services.Symbols, caseSensitive);
        }

        protected Sixty502DotNetParser ParseSource(string source, string cpu)
        {
            var args = new string[] { "-c", cpu, "--list=test_list.a65" };
            Services = new AssemblyServices(Options.FromArgs(args));
            var parser = ParseSource(source, Services.Symbols, false);
            Visitor = new CodeGenVisitor(Services, ((Sixty502DotNetLexer)parser.TokenStream.TokenSource).InstructionSet);
            return parser;
        }
    }
}
