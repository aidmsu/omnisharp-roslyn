using Xunit;

namespace Main.Test
{
    public class MainTest
    {
        [Fact]
        public void Test()
        {
            Assert.True(true);
        }
        
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void DataDrivenTest1(int i)
        {
            Assert.True(i > 0);
        }
        
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void DataDrivenTest2(int i)
        {
            Assert.True(i >= 0);
        }
        
        private void UtilityFunction()
        {
            
        }

        [Fact(DisplayName = "My Test Name")]
        public void UsesDisplayName()
        {
            Assert.True(true);
        }

        [Fact]
        public void TestWithSimilarName()
        {
            Assert.True(true);
        }

        [Fact]
        public void TestWithSimilarNameFooBar()
        {
            Assert.True(true);
        }
    }
}
