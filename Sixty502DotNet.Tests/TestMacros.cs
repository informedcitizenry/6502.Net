using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestMacros : TestBase
    {
        [TestMethod]
        public void Simple()
        {
            var parse = ParseSource(
@"mymacro .macro
    nop
    .endmacro
rts
.mymacro", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 1);
            var rtsStat = tree.block().stat()[0].asmStat()?.cpuStat()?.implStat();
            Assert.IsNotNull(rtsStat);
            Assert.IsNotNull(rtsStat.mnemonic().RTS());
            var blockStat = tree.block().stat()[1];
            Assert.IsNotNull(blockStat.blockStat());
            Assert.IsNotNull(blockStat.blockStat().block());
            Assert.IsTrue(blockStat.blockStat().block().stat().Length > 0);
            var nopstat = blockStat.blockStat().block().stat()[0].asmStat();
            Assert.IsNotNull(nopstat.cpuStat()?.implStat());
            Assert.IsNotNull(nopstat.cpuStat().implStat().mnemonic());
            Assert.IsNotNull(nopstat.cpuStat().implStat().mnemonic().NOP());
        }

        [TestMethod]
        public void Illegal()
        {
            var parse = ParseSource(
@".mymacro", true);
            var tree = parse.source();
            Assert.IsTrue(Services.Log.HasErrors);

            parse = ParseSource(
@"mymacro .macro
    lda \1
    .endmacro
.mymacro $1000
.yourmacro", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors); // bad macros are treated like directives
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 1);
            Assert.IsNotNull(tree.block().stat()[1].asmStat()?.directiveStat()?.asmDirective());
            Assert.IsNotNull(tree.block().stat()[1].asmStat()?.directiveStat().asmDirective().BadMacro());
        }

        [TestMethod]
        public void SimpleUnnamedSubstitution()
        {
            var parse = ParseSource(
@"mymacro .macro
    lda \1
    .endmacro
.mymacro 42", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 0);
            var blockStat = tree.block().stat()[0];
            Assert.IsNotNull(blockStat.blockStat());
            Assert.IsNotNull(blockStat.blockStat().block());
            Assert.IsTrue(blockStat.blockStat().block().stat().Length > 0);
            var ldaStat = blockStat.blockStat().block().stat()[0].asmStat()?.cpuStat()?.zpAbsStat();
            Assert.IsNotNull(ldaStat);
            Assert.IsNotNull(ldaStat.mnemonic().LDA());
            Assert.IsNotNull(ldaStat.expr());
            Assert.AreEqual("42", ldaStat.expr().Start.Text); // lda 42
        }

        [TestMethod]
        public void SimpleSubstitution()
        {
            var parse = ParseSource(
@"mymacro .macro addr
    lda \addr
    .endmacro
.mymacro 42", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 0);
            var blockStat = tree.block().stat()[0];
            Assert.IsNotNull(blockStat.blockStat());
            Assert.IsNotNull(blockStat.blockStat().block());
            Assert.IsTrue(blockStat.blockStat().block().stat().Length > 0);
            var ldaStat = blockStat.blockStat().block().stat()[0].asmStat()?.cpuStat()?.zpAbsStat();
            Assert.IsNotNull(ldaStat);
            Assert.IsNotNull(ldaStat.mnemonic().LDA());
            Assert.IsNotNull(ldaStat.expr());
            Assert.AreEqual("42", ldaStat.expr().Start.Text); // lda 42
        }

        [TestMethod]
        public void DefaultArgument()
        {
            var parse = ParseSource(
@"mymacro .macro addr = 42
    lda \addr
    .endmacro
.mymacro", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 0);
            var blockStat = tree.block().stat()[0];
            Assert.IsNotNull(blockStat.blockStat());
            Assert.IsNotNull(blockStat.blockStat().block());
            Assert.IsTrue(blockStat.blockStat().block().stat().Length > 0);
            var ldaStat = blockStat.blockStat().block().stat()[0].asmStat()?.cpuStat()?.zpAbsStat();
            Assert.IsNotNull(ldaStat);
            Assert.IsNotNull(ldaStat.mnemonic().LDA());
            Assert.IsNotNull(ldaStat.expr());
            Assert.AreEqual("42", ldaStat.expr().Start.Text); // lda 42
        }

        [TestMethod]
        public void StringSubstitution()
        {
            var parse = ParseSource(
@"mymacro .macro name
    .string ""hello, @{name}""
    .endmacro
.mymacro ""world""", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 0);
            var blockStat = tree.block().stat()[0];
            Assert.IsNotNull(blockStat.blockStat());
            Assert.IsNotNull(blockStat.blockStat().block());
            Assert.IsTrue(blockStat.blockStat().block().stat().Length > 0);
            var stringStat = blockStat.blockStat().block().stat()[0].asmStat()?.pseudoOpStat();
            Assert.IsNotNull(stringStat);
            Assert.IsNotNull(stringStat.pseudoOp()?.String());
            Assert.IsTrue(stringStat.pseudoOpList().pseudoOpArg().Length > 0);
            Assert.AreEqual("\"hello, world\"", stringStat.pseudoOpList().pseudoOpArg()[0].expr().GetText());
        }

        [TestMethod]
        public void MultipleArgs()
        {
            var parse = ParseSource(
@"mymacro .macro val, addr
    lda #\val
    sta \addr
    .endmacro
.mymacro 42, $c100", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 0);
            var blockStat = tree.block().stat()[0];
            Assert.IsNotNull(blockStat.blockStat());
            Assert.IsNotNull(blockStat.blockStat().block());
            Assert.IsTrue(blockStat.blockStat().block().stat().Length > 1);
            var immStat = blockStat.blockStat().block().stat()[0].asmStat()?.cpuStat()?.immStat();
            Assert.IsNotNull(immStat);
            Assert.IsNotNull(immStat.mnemonic().LDA());
            Assert.IsNotNull(immStat.Hash());
            Assert.IsNotNull(immStat.expr());
            Assert.AreEqual("42", immStat.expr().GetText()); // lda #42
            var staStat = blockStat.blockStat().block().stat()[1].asmStat()?.cpuStat()?.zpAbsStat();
            Assert.IsNotNull(staStat);
            Assert.IsNotNull(staStat.mnemonic().STA());
            Assert.AreEqual("$c100", staStat.expr().Start.Text); // sta $c100

        }

        [TestMethod]
        public void MultipleStringArgs()
        {
            var parse = ParseSource(
@"mymacro .macro greet, name
    .string "" @{greet}, @{name} pleasure to meet you.""
    .endmacro
    .mymacro ""hello"", ""world""", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 0);
            var blockStat = tree.block().stat()[0];
            Assert.IsNotNull(blockStat.blockStat());
            Assert.IsNotNull(blockStat.blockStat().block());
            Assert.IsTrue(blockStat.blockStat().block().stat().Length > 0);
            var stringStat = blockStat.blockStat().block().stat()[0].asmStat()?.pseudoOpStat();
            Assert.IsNotNull(stringStat);
            Assert.IsNotNull(stringStat.pseudoOp()?.String());
            Assert.IsTrue(stringStat.pseudoOpList().pseudoOpArg().Length > 0);
            Assert.AreEqual("\" hello, world pleasure to meet you.\"",
                stringStat.pseudoOpList().pseudoOpArg()[0].expr().GetText());
        }

        [TestMethod]
        public void MacroInvokeMacro()
        {
            var parse = ParseSource(
@"m1 .macro a1,a2
    lda \a1
    adc \a2
    .endmacro
m2 .macro a1,a2,a3
    .m1 \a1,\a2
    sta \a3
    .endmacro
    .m2 $1000,$2000,$3000", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 0);
            var blockStat = tree.block().stat()[0];
            Assert.IsNotNull(blockStat.blockStat());
            Assert.IsNotNull(blockStat.blockStat().block());
            Assert.IsTrue(blockStat.blockStat().block().stat().Length > 1);
            var subBlockStat = blockStat.blockStat().block().stat()[0].blockStat();
            Assert.IsNotNull(subBlockStat);
            Assert.IsTrue(subBlockStat.block().stat().Length > 0);
            var ldaStat = subBlockStat.block().stat()[0].asmStat()?.cpuStat()?.zpAbsStat();
            Assert.IsNotNull(ldaStat);
            Assert.IsNotNull(ldaStat.mnemonic().LDA());
            Assert.IsNotNull(ldaStat.expr());
            Assert.AreEqual("$1000", ldaStat.expr().Start.Text); // lda $1000
            var adcStat = subBlockStat.block().stat()[1].asmStat()?.cpuStat()?.zpAbsStat();
            Assert.IsNotNull(adcStat);
            Assert.IsNotNull(adcStat.mnemonic().ADC());
            Assert.IsNotNull(adcStat.expr());
            Assert.AreEqual("$2000", adcStat.expr().Start.Text); // adc $2000
            var staStat = blockStat.blockStat().block().stat()[1].asmStat()?.cpuStat()?.zpAbsStat();
            Assert.IsNotNull(staStat);
            Assert.IsNotNull(staStat.mnemonic().STA());
            Assert.IsNotNull(staStat.expr());
            Assert.AreEqual("$3000", staStat.expr().Start.Text); // sta $3000
        }

        [TestMethod]
        public void Multiple()
        {
            var parse = ParseSource(
@"inc16   .macro
        inc \1
        bne +
        inc \1+1
+       .endmacro
dec16   .macro
        lda \1
        bne +
        dec \1+1
+       dec \1
        .endmacro
.inc16 $2000
.dec16 $3000", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 1);
            var blockStat = tree.block().stat()[0];
            Assert.IsNotNull(blockStat.blockStat());
            Assert.IsNotNull(blockStat.blockStat().block());
            Assert.IsTrue(blockStat.blockStat().block().stat().Length > 1);
            var incStat = blockStat.blockStat().block().stat()[0].asmStat()?.cpuStat()?.zpAbsStat();
            Assert.IsNotNull(incStat);
            Assert.IsNotNull(incStat.mnemonic()?.INC());
            Assert.IsNotNull(incStat.expr());
            Assert.AreEqual("$2000", incStat.expr().Start.Text);
            var blockStat2 = tree.block().stat()[1];
            Assert.IsNotNull(blockStat2);
            Assert.IsNotNull(blockStat2.blockStat().block());
            Assert.IsTrue(blockStat2.blockStat().block().stat().Length > 1);
            var decStat = blockStat2.blockStat().block().stat()[^1].asmStat()?.cpuStat()?.zpAbsStat();
            Assert.IsNotNull(decStat);
            Assert.IsNotNull(decStat.mnemonic()?.DEC());
            Assert.IsNotNull(decStat.expr());
            Assert.AreEqual("$3000", decStat.expr().Start.Text);
        }

        [TestMethod]
        public void WithInclude()
        {
            var parse = ParseSource(
@"mymacro .macro
    .include """ + TestInclude.GetExamplesDir() + @"simple.a65""
    .endmacro
.mymacro", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree);
            Assert.IsTrue(tree.block().stat().Length > 0);
            var blockStat = tree.block().stat()[0];
            Assert.IsNotNull(blockStat.blockStat());
            Assert.IsNotNull(blockStat.blockStat().block());
            Assert.IsTrue(blockStat.blockStat().block().stat().Length > 0);
            var jsrStat = blockStat.blockStat().block().stat()[0].asmStat()?.cpuStat()?.zpAbsStat();
            Assert.IsNotNull(jsrStat);
            Assert.IsNotNull(jsrStat.mnemonic().JSR());
            Assert.IsNotNull(jsrStat.expr());
            Assert.AreEqual("$ffd2", jsrStat.expr().Start.Text); // jsr $ffd2
        }

        [TestMethod]
        public void FromLibrary()
        {
            var parse = ParseSource(
@".include """ + TestInclude.GetExamplesDir() + @"lib2.a65""
.inc16 $2000
.dec24 $3000", true);
            var tree = parse.source();
            var treestring = tree.ToStringTree(parse);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree);
            Assert.IsTrue(tree.block().stat().Length > 0);
            var blockStat = tree.block().stat()[0];
            Assert.IsNotNull(blockStat.blockStat());
            Assert.IsNotNull(blockStat.blockStat().block());
            Assert.IsTrue(blockStat.blockStat().block().stat().Length > 0);
            var incStat = blockStat.blockStat().block().stat()[0].asmStat()?.cpuStat()?.zpAbsStat();
            Assert.IsNotNull(incStat);
            Assert.IsNotNull(incStat.mnemonic().INC());
            Assert.IsNotNull(incStat.expr());
            Assert.AreEqual("$2000", incStat.expr().Start.Text); // inc $2000
            var block2Stat = tree.block().stat()[1];
            Assert.IsNotNull(block2Stat.blockStat());
            Assert.IsNotNull(block2Stat.blockStat().block());
            Assert.IsTrue(block2Stat.blockStat().block().stat().Length > 0);

            var decStat = block2Stat.blockStat().block().stat()[^1].asmStat()?.cpuStat()?.zpAbsStat();
            Assert.IsNotNull(decStat);
            Assert.IsNotNull(decStat.mnemonic().DEC());
            Assert.AreEqual("$3000", decStat.expr().Start.Text); // dec $3000

        }

        [TestMethod]
        public void BasicStub()
        {
            var parse = ParseSource(
@"basicstub    .macro sob=2049,start=2061
            * = \sob
            .word eob,10        ;Create a basic loader
            .byte $9e           ;10 SYS{parameter}
            .cstring format(""{0}"", \start)
eob         .word 0
            .endmacro

            .basicstub // basic stub", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2061, Services.Output.ProgramCounter);
        }

        [TestMethod]
        public void PseudoStruct()
        {
            var parse = ParseSource(@"
player1     .block
h           .char 0
v           .char 0
            .endblock", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            parse = ParseSource(
@"direction     .macro x=0,y=0
h               .char \x
v               .char \y
                .endmacro
player1         .direction 
player2         .direction -1,-1
                lda #32
                sta player1.h
                lda #128
                sta player1.v
                ldx #-1
                stx player2.h
                stx player2.v", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0x0012, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0, bytes[0]);
            Assert.AreEqual(0, bytes[1]);
            Assert.AreEqual(0xff, bytes[2]);
            Assert.AreEqual(0xff, bytes[3]);
        }
    }
}
