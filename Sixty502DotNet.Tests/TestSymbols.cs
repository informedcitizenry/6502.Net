using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestSymbols : TestBase
    {
        [TestMethod]
        public void Constant()
        {
            var source = "myconst .equ 1";
            var parser = ParseSource(source, false);
            _ = parser.source();
            var myconst = parser.Symbols.Scope.Resolve("myconst") as Constant;
            Assert.IsNotNull(myconst);
        }

        [TestMethod]
        public void Global()
        {
            var source =
@"someblock .block
myglobal .global 2
  .endblock";
            var parser = ParseSource(source, false);
            _ = parser.source();
            var myglobal = parser.Symbols.GlobalScope.Resolve("myglobal") as Constant;
            Assert.IsNotNull(myglobal);
        }

        [TestMethod]
        public void LocalLabel()
        {
            var source =
@"mylabel
_local = 2";
            var parser = ParseSource(source, false);
            _ = parser.source();
            var mylabel = parser.Symbols.Scope.Resolve("mylabel") as Label;
            Assert.IsNotNull(mylabel);
            var _local = mylabel.ResolveMember("_local") as Constant;
            Assert.IsNotNull(_local);
            var mylabel_local = parser.Symbols.Scope.Resolve("mylabel_local");
            Assert.IsNotNull(mylabel_local);
            var _localFromGlobal = parser.Symbols.Scope.Resolve("_local");
            Assert.IsNotNull(_localFromGlobal);
            Assert.AreSame(_local, _localFromGlobal);
            var mylabeldot_local = parser.Symbols.Scope.Resolve("mylabel._local");
            Assert.IsNull(mylabeldot_local);
        }

        [TestMethod]
        public void MultipleLocalLabels()
        {
            var source =
@"mylabel
_local = 2
myother
_local = 4";
            var parser = ParseSource(source, false);
            _ = parser.source();
            Assert.IsFalse(Services.Log.HasErrors);
            var mylabel = parser.Symbols.Scope.Resolve("mylabel") as Label;
            Assert.IsNotNull(mylabel);
            var myother = parser.Symbols.Scope.Resolve("myother") as Label;
            Assert.IsNotNull(myother);
            var _local2 = parser.Symbols.Scope.Resolve("_local");
            Assert.IsNotNull(_local2);
            var myother_local = parser.Symbols.Scope.Resolve("myother_local");
            Assert.IsNotNull(myother_local);
            Assert.AreSame(_local2, myother_local);
        }
    }
}
