using Asm6502.Net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NUnitTest6502.Net
{
    [TestFixture]
    public class CompilationTest
    {
        [Test]
        public void TestAddUninitialized()
        {
            Compilation output = new Compilation();
            output.AddUninitialized(4);
            Assert.AreEqual(4, output.GetPC());

            output.SetPC(0xC000);
            output.AddUninitialized(16);
            Assert.AreEqual(0xC010, output.GetPC());
        }

        [Test]
        public void TestProgramStart()
        {
            Compilation output = new Compilation();

            output.SetPC(0x02);
            output.AddUninitialized(0x32);
            Assert.AreEqual(0x02 + 0x32, output.GetPC());

            // program start will not be set until we add
            // initialized data
            output.SetPC(0x0801);
            output.Add(0xffd220, 3);
            Assert.AreEqual(0x0801, output.ProgramStart);
            Assert.AreEqual(0x0804, output.GetPC());
        }

        [Test]
        public void TestLittleEndian()
        {
            Compilation output = new Compilation(true);

            output.Add(0xffd2, 2);
            Assert.AreEqual(0x0002, output.GetPC());

            var bytes = output.GetCompilation();
            Assert.AreEqual(0xd2, bytes[0]);
            Assert.AreEqual(0xff, bytes[1]);
        }

        [Test]
        public void TestAlignFillRepeat()
        {
            Compilation output = new Compilation();
            output.SetPC(0x4015);

            int foragoodtime = 0xffd220; // for a good time jsr $ffd2
            int align = output.Align(0x10, foragoodtime); // fill 11 bytes with 0xffd220...
            Assert.AreEqual(0x4020 - 0x4015, align);

            var bytes1 = output.GetCompilation();
            Assert.AreEqual(bytes1.Count, align);

            var expected1 = new byte[] { 0x20, 0xd2, 0xff, 
                                         0x20, 0xd2, 0xff,
                                         0x20, 0xd2, 0xff,
                                         0x20, 0xd2 };

            Assert.IsTrue(expected1.SequenceEqual(bytes1));

            output.Reset();
            output.Fill(7, foragoodtime, false);

            Assert.AreEqual(0x0007, output.GetPC());

            var bytes2 = output.GetCompilation();
            var expected2 = new byte[] { 0x20, 0xd2, 0xff,
                                         0x20, 0xd2, 0xff, 0x20 };

            Assert.IsTrue(expected2.SequenceEqual(bytes2));

            output.Reset();

            output.Fill(7, foragoodtime, true);

            Assert.AreEqual(0x0015, output.GetPC());

            var bytes3 = output.GetCompilation();
            var expected3 = new byte[] { 0x20, 0xd2, 0xff,
                                         0x20, 0xd2, 0xff,
                                         0x20, 0xd2, 0xff,
                                         0x20, 0xd2, 0xff,
                                         0x20, 0xd2, 0xff,
                                         0x20, 0xd2, 0xff,
                                         0x20, 0xd2, 0xff };

            Assert.IsTrue(expected3.SequenceEqual(bytes3));
        }

        [Test]
        public void TestLogicalRelocation()
        {
            Compilation output = new Compilation();

            output.SetPC(0xc000);
            Assert.AreEqual(0xc000, output.GetPC());
            Assert.AreEqual(0xc000, output.ProgramCounter);

            // nope can't do this!
            Assert.Throws<Compilation.InvalidPCAssignmentException>(() => output.SetPC(0x8000));

            output.SetLogicalPC(0xf000);
            Assert.AreNotEqual(output.ProgramCounter, output.GetPC());

            output.Add(0xfeedface, 4);
            Assert.AreEqual(0xc000, output.ProgramStart);
            Assert.AreEqual(0xc004, output.ProgramCounter);
            Assert.AreEqual(0xf004, output.GetPC());

            output.SynchPC();
            Assert.AreEqual(output.ProgramCounter, output.GetPC());

            output.SetLogicalPC(0x0002); // set to zp
            Assert.AreEqual(0x0002, output.GetPC());

            output.AddUninitialized(0x80);
            Assert.AreEqual(0x0082, output.GetPC());
        }
    }
}
