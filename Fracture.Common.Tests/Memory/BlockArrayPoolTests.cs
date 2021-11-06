using Xunit;

namespace Fracture.Common.Tests.Memory
{
    [Trait("Category", "Memory")]
    public class BlockArrayPoolTests
    {
        public BlockArrayPoolTests()
        {
        }
        
        public void Take_Returns_Array_Of_Supplied_Size_If_It_Exceeds_Max_Size()
        {
        }
        
        public void Take_Returns_Array_That_Has_Length_Inside_Block_Bounds()
        {
        }
        
        public void Take_Returns_Block_Sized_Array_If_Supplied_Size_Is_Smaller()
        {
        }
        
        public void Return_Disposes_Arrays_Larger_Than_Max_Size()
        {
        }
        
        public void Return_Stores_Arrays_Smaller_Than_Max_Size()
        {
        }
    }
}