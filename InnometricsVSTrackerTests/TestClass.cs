using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnometricsVSTrackerTests
{
    class TestClass
    {
        public TestClass()
        {
            //constructor 
        }

        public class TestInnerClass
        {
            TestInnerClass()
            {
                //constructor 
            }

            private void InnerMethod()
            {
                var i = 1;
                var t1 = new System.Threading.Thread
                (delegate()
                {
                    //anonymous
                    System.Console.Write("Hello, ");
                    System.Console.WriteLine("World!");
                });
            }
        }
    }
}
