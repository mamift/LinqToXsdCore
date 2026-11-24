using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;


namespace Xml.Schema.Linq.Tests
{
    public class FacetCheckerTests
    {

        [Test]
        public void InitMappingIsThreadSafe()
        {
            // The InitMapping method needs to be thread-safe as FacetsChecker.GetFacetsChecker
            // can call it multiple times in a multi-threaded scenario

            var initMappingMethod = typeof(FacetsChecker).GetMethod("InitMapping",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            Assert.NotNull(initMappingMethod); // may fail if the InitMapping method is removed
                                               // in future

            Assert.DoesNotThrow(() =>
                Parallel.For(0, 4, _ => initMappingMethod!.Invoke(null, []))
            );

        }
    }
}