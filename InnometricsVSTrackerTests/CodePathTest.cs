using System;
using System.IO;
using InnometricsVSTracker;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnometricsVSTrackerTests
{
    [TestClass]
    public class CodePathTest
    {
        [TestMethod]
        public void TestNamespace()
        {
            var tracker = new Tracker("TestProj", "C#");

            var code = new StreamReader("..\\..\\TestClass.cs").ReadToEnd();
            var tree = CSharpSyntaxTree.ParseText(code);
            var caretPosition = 161;

            var path = tracker.GetCurrentPath(tree, caretPosition);
            var expected = "PROJ:TestProj|LANG:C#|NS:InnometricsVSTrackerTests|LINE:7";

            Assert.AreEqual(path, expected);
        }
        [TestMethod]
        public void TestClass()
        {
            var tracker = new Tracker("TestProj", "C#");

            var code = new StreamReader("..\\..\\TestClass.cs").ReadToEnd();
            var tree = CSharpSyntaxTree.ParseText(code);
            var caretPosition = 189;

            var path = tracker.GetCurrentPath(tree, caretPosition);
            var expected = "PROJ:TestProj|LANG:C#|NS:InnometricsVSTrackerTests|CLASS:TestClass|LINE:9";

            Assert.AreEqual(path, expected);
        }
        [TestMethod]
        public void TestFunc()
        {
            var tracker = new Tracker("TestProj", "C#");

            var code = new StreamReader("..\\..\\TestClass.cs").ReadToEnd();
            var tree = CSharpSyntaxTree.ParseText(code);
            var caretPosition = 257;

            var path = tracker.GetCurrentPath(tree, caretPosition);
            var expected = "PROJ:TestProj|LANG:C#|NS:InnometricsVSTrackerTests|CLASS:TestClass|FUNC:TestClass|LINE:13";

            Assert.AreEqual(path, expected);
        }
        [TestMethod]
        public void TestInnerClass()
        {
            var tracker = new Tracker("TestProj", "C#");

            var code = new StreamReader("..\\..\\TestClass.cs").ReadToEnd();
            var tree = CSharpSyntaxTree.ParseText(code);
            var caretPosition = 317;

            var path = tracker.GetCurrentPath(tree, caretPosition);
            var expected = "PROJ:TestProj|LANG:C#|NS:InnometricsVSTrackerTests|CLASS:TestClass|CLASS:TestInnerClass|LINE:16";

            Assert.AreEqual(path, expected);
        }
        [TestMethod]
        public void TestInnerClassConstr()
        {
            var tracker = new Tracker("TestProj", "C#");

            var code = new StreamReader("..\\..\\TestClass.cs").ReadToEnd();
            var tree = CSharpSyntaxTree.ParseText(code);
            var caretPosition = 394;

            var path = tracker.GetCurrentPath(tree, caretPosition);
            var expected = "PROJ:TestProj|LANG:C#|NS:InnometricsVSTrackerTests|CLASS:TestClass|CLASS:TestInnerClass|FUNC:TestInnerClass|LINE:20";

            Assert.AreEqual(path, expected);
        }
        [TestMethod]
        public void TestInnerClassFunc()
        {
            var tracker = new Tracker("TestProj", "C#");

            var code = new StreamReader("..\\..\\TestClass.cs").ReadToEnd();
            var tree = CSharpSyntaxTree.ParseText(code);
            var caretPosition = 494;

            var path = tracker.GetCurrentPath(tree, caretPosition);
            var expected = "PROJ:TestProj|LANG:C#|NS:InnometricsVSTrackerTests|CLASS:TestClass|CLASS:TestInnerClass|FUNC:InnerMethod|LINE:24";

            Assert.AreEqual(path, expected);
        }
        [TestMethod]
        public void TestInnerClassAnon()
        {
            var tracker = new Tracker("TestProj", "C#");

            var code = new StreamReader("..\\..\\TestClass.cs").ReadToEnd();
            var tree = CSharpSyntaxTree.ParseText(code);
            var caretPosition = 630;

            var path = tracker.GetCurrentPath(tree, caretPosition);
            var expected = "PROJ:TestProj|LANG:C#|NS:InnometricsVSTrackerTests|CLASS:TestClass|CLASS:TestInnerClass|FUNC:InnerMethod|FUNC:[ANONYMOUS]|LINE:29";

            Assert.AreEqual(path, expected);
        }
    }
}
