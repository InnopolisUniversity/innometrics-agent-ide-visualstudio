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
            var expected = "PROJ:TestProj|LANG:C#|NS:InnometricsVSTrackerTests|LINE:8";

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
            var expected = "PROJ:TestProj|LANG:C#|NS:InnometricsVSTrackerTests|CLASS:TestClass|LINE:10";

            Assert.AreEqual(path, expected);
        }
        [TestMethod]
        public void TestFunc()
        {
            var tracker = new Tracker("TestProj", "C#");

            var code = new StreamReader("..\\..\\TestClass.cs").ReadToEnd();
            var tree = CSharpSyntaxTree.ParseText(code);
            var caretPosition = 229;

            var path = tracker.GetCurrentPath(tree, caretPosition);
            var expected = "PROJ:TestProj|LANG:C#|NS:InnometricsVSTrackerTests|CLASS:TestClass|FUNC:TestClass|LINE:12";

            Assert.AreEqual(path, expected);
        }
        [TestMethod]
        public void TestInnerClass()
        {
            var tracker = new Tracker("TestProj", "C#");

            var code = new StreamReader("..\\..\\TestClass.cs").ReadToEnd();
            var tree = CSharpSyntaxTree.ParseText(code);
            var caretPosition = 318;

            var path = tracker.GetCurrentPath(tree, caretPosition);
            var expected = "PROJ:TestProj|LANG:C#|NS:InnometricsVSTrackerTests|CLASS:TestClass|CLASS:TestInnerClass|LINE:17";

            Assert.AreEqual(path, expected);
        }
        [TestMethod]
        public void TestInnerClassConstr()
        {
            var tracker = new Tracker("TestProj", "C#");

            var code = new StreamReader("..\\..\\TestClass.cs").ReadToEnd();
            var tree = CSharpSyntaxTree.ParseText(code);
            var caretPosition = 363;

            var path = tracker.GetCurrentPath(tree, caretPosition);
            var expected = "PROJ:TestProj|LANG:C#|NS:InnometricsVSTrackerTests|CLASS:TestClass|CLASS:TestInnerClass|FUNC:TestInnerClass|LINE:19";

            Assert.AreEqual(path, expected);
        }
        [TestMethod]
        public void TestInnerClassFunc()
        {
            var tracker = new Tracker("TestProj", "C#");

            var code = new StreamReader("..\\..\\TestClass.cs").ReadToEnd();
            var tree = CSharpSyntaxTree.ParseText(code);
            var caretPosition = 466;

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
            var caretPosition = 685;

            var path = tracker.GetCurrentPath(tree, caretPosition);
            var expected = "PROJ:TestProj|LANG:C#|NS:InnometricsVSTrackerTests|CLASS:TestClass|CLASS:TestInnerClass|FUNC:InnerMethod|FUNC:[ANONYMOUS]|LINE:30";

            Assert.AreEqual(path, expected);
        }
    }
}
