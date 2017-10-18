﻿using DotNetAsm;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnit.Tests.TestDotNetAsm
{
    [TestFixture]
    public class NUnitTestMisc : NUnitAsmTestBase
    {
        public NUnitTestMisc()
        {
            Controller = new TestController();
            LineAssembler = new MiscAssembler(Controller);
        }

        [Test]
        public void TestAssert()
        {
            SourceLine line = new SourceLine();
            line.LineNumber = 1;
            line.Filename = "test";
            line.Instruction = ".assert";
            line.Operand = "5 == 6";
            LineAssembler.AssembleLine(line);

            Assert.IsTrue(Controller.Log.HasErrors);
            
            string error = Controller.Log.Entries.Last();
            Assert.AreEqual("Error in file 'test' at line 1: Assertion Failed: '5 == 6'", error);

            Controller.Log.ClearAll();

            line.Operand = "5 == 6, \"My custom error!\"";
            LineAssembler.AssembleLine(line);

            Assert.IsTrue(Controller.Log.HasErrors);
            error = Controller.Log.Entries.Last();
            Assert.AreEqual("Error in file 'test' at line 1: My custom error!", error);

            Controller.Log.ClearAll();

            line.Operand = "5 == 5";
            LineAssembler.AssembleLine(line);
            Assert.IsFalse(Controller.Log.HasErrors);
        }

        [Test]
        public void TestConditionals()
        {
            SourceLine line = new SourceLine();
            line.LineNumber = 1;
            line.Filename = "test";
            line.Instruction = ".errorif";
            line.Operand = "5 != 6, \"5 doesn't equal 6!\"";
            LineAssembler.AssembleLine(line);

            Assert.IsTrue(Controller.Log.HasErrors);
            string error = Controller.Log.Entries.Last();

            Assert.AreEqual("Error in file 'test' at line 1: 5 doesn't equal 6!", error);
            Controller.Log.ClearAll();

            line.Operand = "5 == 6, \"5 equals 6!\"";
            LineAssembler.AssembleLine(line);

            Assert.IsFalse(Controller.Log.HasErrors);
        }
    }
}
