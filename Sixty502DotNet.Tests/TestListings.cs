using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestListings : TestBase
    {

        [TestMethod]
        public void LabelOnly()
        {
            var parse = ParseSource(
@" * = $c000
label", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc000, Services.Output.ProgramCounter);
            Assert.IsTrue(Services.StatementListings.Count > 0);
            Assert.AreEqual(".c000                                      label",
                Services.StatementListings[^1]);
        }

        [TestMethod]
        public void ConstantAssignment()
        {
            var parse = ParseSource("chrout = $ffd2", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsTrue(Services.StatementListings.Count > 0);
            Assert.AreEqual("=$ffd2                                     chrout = $ffd2",
                Services.StatementListings[^1]);

            parse = ParseSource("chrout := $ffd2", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsFalse(Services.StatementListings.Count > 0);
        }

        [TestMethod]
        public void ConstantAssignmentAfterComment()
        {
            var parse = ParseSource(
@";; copyright disclaimer

CBM         .block

R6510               =   $01                 //Processor port used for ROM/RAM hiding
FREETOP             =   $33                 ;Pointer to bottom of string text storage
MEMSIZE             =   $37                 ;Pointer to highest address used by BASIC
FAC1                =   $61                 ;Floating point accumulator #1
FACHO               =   $62                 ;Floating point accumulator #1 mantissa
            .endblock", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var CBM = Services.Symbols.GlobalScope.Resolve("CBM") as IScope;
            Assert.IsNotNull(CBM);
            var R6510 = CBM.Resolve("R6510") as Constant;
            Assert.IsNotNull(R6510);
            Assert.IsNotNull(R6510.Value);
            Assert.IsTrue(R6510.Value.IsDefined);
            Assert.IsTrue(R6510.Value.IsIntegral);
            Assert.AreEqual(1, R6510.Value.ToInt());
        }

        [TestMethod]
        public void ConstantEqu()
        {
            var parse = ParseSource("chrout .equ $ffd2", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsTrue(Services.StatementListings.Count > 0);
            Assert.AreEqual("=$ffd2                                     chrout .equ $ffd2",
                Services.StatementListings[^1]);

            parse = ParseSource("chrout .global $ffd2", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsTrue(Services.StatementListings.Count > 0);
            Assert.AreEqual("=$ffd2                                     chrout .global $ffd2",
                Services.StatementListings[^1]);
        }

        [TestMethod]
        public void PCAssignment()
        {
            var parse = ParseSource("* = $c000", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc000, Services.Output.ProgramCounter);
            Assert.AreEqual(1, Services.StatementListings.Count);
            Assert.AreEqual(".c000                                      * = $c000",
                Services.StatementListings[0]);
        }

        [TestMethod]
        public void PCEqu()
        {
            var parse = ParseSource("* .equ $c000", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc000, Services.Output.ProgramCounter);
            Assert.AreEqual(1, Services.StatementListings.Count);
            Assert.AreEqual(".c000                                      * .equ $c000",
                Services.StatementListings[0]);
        }

        [TestMethod]
        public void Org()
        {
            var parse = ParseSource(".org $c000", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc000, Services.Output.ProgramCounter);
            Assert.AreEqual(1, Services.StatementListings.Count);
            Assert.AreEqual(".c000                                      .org $c000",
                Services.StatementListings[0]);
        }


        [TestMethod]
        public void DataGenSingleLine()
        {
            var parse = ParseSource(
@" * = $c000
  .byte 1,2,3,4", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc004, Services.Output.ProgramCounter);
            Assert.IsTrue(Services.StatementListings.Count > 0);
            Assert.AreEqual(">c000     01 02 03 04                        .byte 1,2,3,4",
                Services.StatementListings[^1]);
        }

        [TestMethod]
        public void DataGenMultiLine()
        {
            var parse = ParseSource(
@" * = $c000
   .cstring ""HE SAID, \""HELLO, WORLD!\"" TO ME""", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc01f, Services.Output.ProgramCounter);
            Assert.IsTrue(Services.StatementListings.Count > 0);
            var disString =
@">c000     48 45 20 53 41 49 44 2c             .cstring ""HE SAID, \""HELLO, WORLD!\"" TO ME""
>c008     20 22 48 45 4c 4c 4f 2c
>c010     20 57 4f 52 4c 44 21 22
>c018     20 54 4f 20 4d 45 00";
            Assert.AreEqual(disString, Services.StatementListings[^1]);
        }

        [TestMethod]
        public void CodeGen()
        {
            var parse = ParseSource(
@" * = $c000
loop        lda 49167,x", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc003, Services.Output.ProgramCounter);
            Assert.IsTrue(Services.StatementListings.Count > 0);
            Assert.AreEqual(@".c000     bd 0f c0       lda $c00f,x       loop        lda 49167,x",
                Services.StatementListings[^1]);

            parse = ParseSource(
@"loop      LDA   ($FC) , Y", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0x02, Services.Output.ProgramCounter);
            Assert.IsTrue(Services.StatementListings.Count > 0);
            Assert.AreEqual(".0000     b1 fc          lda ($fc),y       loop      LDA   ($FC) , Y",
                Services.StatementListings[^1]);

            parse = ParseSource(
@" * = $1000
            leax [,x++]", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0x1002, Services.Output.ProgramCounter);
            Assert.IsTrue(Services.StatementListings.Count > 0);
            Assert.AreEqual(".1000     30 91          leax [,x++]                   leax [,x++]",
                Services.StatementListings[^1]);
        }

        [TestMethod]
        public void CommentedSource()
        {
            var parse = ParseSource(
@" * = $c000
loop        nop // do not do anything here", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc001, Services.Output.ProgramCounter);
            Assert.IsTrue(Services.StatementListings.Count > 0);
            Assert.AreEqual(@".c000     ea             nop               loop        nop // do not do anything here",
                Services.StatementListings[^1]);

        }
    }
}
