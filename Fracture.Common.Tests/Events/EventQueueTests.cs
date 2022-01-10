using Fracture.Common.Events;
using Xunit;

namespace Fracture.Common.Tests.Events
{
   [Trait("Category", "Events")]
   public sealed class EventQueueTests
   {
      #region Static fields
      private static readonly object TestTopic = new object();
      #endregion
      
      public delegate void TestEventHandler(object key, int value);
      
      [Fact]
      public void UniqueEventQueue_Ctor_Test()
         => Assert.Null(Record.Exception(() => new UniqueEventBacklog<object, int>()));
      
      [Fact]
      public void SharedEventQueue_Ctor_Test()
         => Assert.Null(Record.Exception(() => new SharedEventBacklog<object, int>()));

      [Fact]
      public void Unique_Queue_Allows_One_Event_Per_Topic_Test()
      {
         // Arrange.
         var queue     = new UniqueEventBacklog<object, int>();
         var testValue = 0;
         
         // Act.
         queue.Create(TestTopic);
         
         queue.Subscribe(TestTopic, delegate { testValue++; });
         
         // Publish 3 events, should get invoked once.
         queue.Publish(TestTopic, 1);
         queue.Publish(TestTopic, 1);
         queue.Publish(TestTopic, 1);

         // Do double dispatch, queue should be empty after first one.
         queue.Dispatch();
         queue.Dispatch();
         
         // Assert.
         Assert.Equal(1, testValue);
      }
      
      [Fact]
      public void Shared_QueueAllows_Multiple_Events_Per_Topic_Test()
      {
         // Arrange.
         var queue     = new SharedEventBacklog<object, int>();
         var testValue = 0;
         
         // Act.
         queue.Create(TestTopic);
         
         queue.Subscribe(TestTopic, delegate { testValue++; });

         // Publish 3 events, should get invoked 3 times.
         queue.Publish(TestTopic, 0);
         queue.Publish(TestTopic, 0);
         queue.Publish(TestTopic, 0);
         
         // Do double dispatch, queue should be empty after first one.
         queue.Dispatch();
         queue.Dispatch();

         // Assert.
         Assert.Equal(3, testValue);
      }
   }
}