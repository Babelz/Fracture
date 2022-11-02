using Fracture.Common.Memory;
using Xunit;

namespace Fracture.Common.Tests.Memory
{
    [Trait("Category", "Memory")]
    public sealed class ClearableTests
    {
        #region Private clarable test class
        private sealed class ClearableTestClass
        {
            #region Fields
            public readonly int X;

            public int I, J;

            public object O;
            #endregion

            #region Properties
            public string A
            {
                get;
                set;
            }

            public int B
            {
                get;
                set;
            }

            public float C
            {
                get;
                private set;
            }

            public object P
            {
                get;
                set;
            }
            #endregion

            public ClearableTestClass()
                => X = 500;

            public void MutateC()
                => C = 200.0f;
        }
        #endregion

        public ClearableTests()
        {
        }

        [Fact]
        public void Default_Clear_Delegate_Clears_All_Fields_And_Properties_That_Are_Public()
        {
            var clear = ClearableUtils.CreateClearDelegate<ClearableTestClass>();

            var value = new ClearableTestClass()
            {
                A = "hello",
                B = 200,
                I = 300,
                J = 15,
                O = new object(),
                P = new object(),
            };

            value.MutateC();

            clear(ref value);

            Assert.Equal(string.Empty, value.A);
            Assert.Equal(0, value.B);
            Assert.Equal(0, value.I);
            Assert.Equal(0, value.J);
            Assert.Null(value.O);
        }
    }
}