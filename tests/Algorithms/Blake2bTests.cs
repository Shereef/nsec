using System;
using NSec.Cryptography;
using Xunit;

namespace NSec.Tests.Algorithms
{
    public static class Blake2bTests
    {
        #region Properties

        [Fact]
        public static void Properties()
        {
            var a = new Blake2b();

            Assert.Equal(32, a.MinHashSize);
            Assert.Equal(32, a.DefaultHashSize);
            Assert.Equal(64, a.MaxHashSize);
        }

        #endregion
    }
}
