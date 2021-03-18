using System;
using Fracture.Common.Events;
using Xunit;

namespace Fracture.Common.Tests.Events
{
   [Trait("Category", "Events")]
   public sealed class EventQueueTests
   {
      #region Static fields
      public static object TestTopic = new object();
      #endregion
      
      public delegate void TestEventHandler(ref int number);
      
      [Fact]
      public void EventQueue_Ctor_Test()
      {
         try
         {
            // Should throw with invalid bucket size.
            // ReSharper disable once ObjectCreationAsStatement - disabled for testing constructor.
            new UniqueEventQueue<object, TestEventHandler>(-1);
            
            Assert.True(false);
         }
         catch (ArgumentOutOfRangeException)
         {
         }
         
         try
         {
            // Should throw with invalid bucket size.
            // ReSharper disable once ObjectCreationAsStatement - disabled for testing constructor.
            new SharedEventQueue<object, TestEventHandler>(-1);
            
            Assert.True(false);
         }
         catch (ArgumentOutOfRangeException)
         {
         }
      }
      
      [Fact]
      public void Unique_Queue_Allows_One_Event_Per_Topic_Test()
      {
         // Arrange.
         var queue     = new UniqueEventQueue<object, TestEventHandler>(8);
         var testValue = 0;
         
         // Act.
         queue.Create(TestTopic);
         
         queue.Subscribe(TestTopic, (ref int i) => i++);
         
         // Publish 3 events, should get invoked once.
         queue.Publish(TestTopic, e => e(ref testValue));
         queue.Publish(TestTopic, e => e(ref testValue));
         queue.Publish(TestTopic, e => e(ref testValue));

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
         var queue     = new SharedEventQueue<object, TestEventHandler>(8);
         var testValue = 0;
         
         // Act.
         queue.Create(TestTopic);
         
         queue.Subscribe(TestTopic, (ref int i) => i++);

         // Publish 3 events, should get invoked 3 times.
         queue.Publish(TestTopic, e => e(ref testValue));
         queue.Publish(TestTopic, e => e(ref testValue));
         queue.Publish(TestTopic, e => e(ref testValue));
         
         // Do double dispatch, queue should be empty after first one.
         queue.Dispatch();
         queue.Dispatch();

         // Assert.
         Assert.Equal(3, testValue);
      }
   }
}