using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestSections : TestBase
    {
        [TestMethod]
        public void DefineSectionWithStart()
        {
            var parse = ParseSource(
@"  .dsection ""test"",0x0400
    .section ""test""
myconst = *", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0x0400, Services.Output.ProgramCounter);
            var myconst = Services.Symbols.GlobalScope.Resolve("myconst") as Constant;
            Assert.IsNotNull(myconst);
            Assert.IsNotNull(myconst.Value);
            Assert.IsTrue(myconst.Value.IsDefined);
            Assert.IsTrue(myconst.Value.IsIntegral);
            Assert.AreEqual(0x0400, myconst.Value.ToInt());
        }

        [TestMethod]
        public void DefineSectionWithStartAndEnd()
        {
            var parse = ParseSource(
@"  .dsection ""test"",0x0400,0x0800
    .section ""test""
myconst = *", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0x0400, Services.Output.ProgramCounter);
            var myconst = Services.Symbols.GlobalScope.Resolve("myconst") as Constant;
            Assert.IsNotNull(myconst);
            Assert.IsNotNull(myconst.Value);
            Assert.IsTrue(myconst.Value.IsDefined);
            Assert.IsTrue(myconst.Value.IsIntegral);
            Assert.AreEqual(0x0400, myconst.Value.ToInt());
        }

        [TestMethod]
        public void DefineMultipleSections()
        {
            var parse = ParseSource(
@"  .dsection ""himem"",0xc000
    .dsection ""zp"",0x02,0x100
    .section ""himem""
himemconst = *
    .section ""zp""
zpconst = *", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0x02, Services.Output.ProgramCounter);
            var himemconst = Services.Symbols.GlobalScope.Resolve("himemconst") as Constant;
            Assert.IsNotNull(himemconst);
            Assert.IsNotNull(himemconst.Value);
            Assert.IsTrue(himemconst.Value.IsDefined);
            Assert.IsTrue(himemconst.Value.IsIntegral);
            Assert.AreEqual(0xc000, himemconst.Value.ToInt());
            var zpconst = Services.Symbols.GlobalScope.Resolve("zpconst") as Constant;
            Assert.IsNotNull(zpconst);
            Assert.IsNotNull(zpconst.Value);
            Assert.IsTrue(zpconst.Value.IsDefined);
            Assert.IsTrue(zpconst.Value.IsIntegral);
            Assert.AreEqual(0x02, zpconst.Value.ToInt());
        }

        [TestMethod]
        public void SectionFunction()
        {
            var parse = ParseSource(
@"  .dsection ""himem"",0xc000
    .dsection ""zp"",0x02,0x100
    .section ""himem""
himemloc = section(""himem"")", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc000, Services.Output.ProgramCounter);
            var himemloc = Services.Symbols.GlobalScope.Resolve("himemloc") as Constant;
            Assert.IsNotNull(himemloc);
            Assert.IsNotNull(himemloc.Value);
            Assert.IsTrue(himemloc.Value.IsDefined);
            Assert.IsTrue(himemloc.Value.IsIntegral);
            Assert.AreEqual(0xc000, himemloc.Value.ToInt());
        }
    }
}
