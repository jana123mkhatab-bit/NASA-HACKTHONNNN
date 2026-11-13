using System;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Collections.Tests
{
    internal class UnsafeRingQueueTests
    {
        [Test]
        public void UnsafeRingQueue_Enqueue_Dequeue()
        {
            var test = new UnsafeRingQueue<int>(16, Allocator.Persistent);

            Assert.AreEqual(0, test.Length);

            int item;
            Assert.False(test.TryDequeue(out item));

            test.Enqueue(123);
            Assert.AreEqual(1, test.Length);

            Assert.True(test.TryEnqueue(456));
            Assert.AreEqual(2, test.Length);

            Assert.True(test.TryDequeue(out item));
            Assert.AreEqual(123, item);
            Assert.AreEqual(1, test.Length);

            Assert.AreEqual(456, test.Dequeue());
            Assert.AreEqual(0, test.Length);

            test.Dispose();
        }

        [Test]
        public unsafe void UnsafeRingQueue_Enqueue_Dequeue_View()
        {
            var list = new UnsafeList<int>(16, Allocator.Persistent);
            list.Length = 16;
            var test = new UnsafeRingQueue<int>(list.Ptr, list.Length);

            Assert.AreEqual(0, test.Length);

            int item;
            Assert.False(test.TryDequeue(out item));

            test.Enqueue(123);
            Assert.AreEqual(1, test.Length);

            Assert.True(test.TryEnqueue(456));
            Assert.AreEqual(2, test.Length);

            Assert.True(test.TryDequeue(out item));
            Assert.AreEqual(123, item);
            Assert.AreEqual(1, test.Length);

            Assert.AreEqual(456, test.Dequeue());
            Assert.AreEqual(0, test.Length);

            test.Dispose();
            list.Dispose();
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks]
        public void UnsafeRingQueue_Throws()
        {
            using (var test = new UnsafeRingQueue<int>(1, Allocator.Persistent))
            {
                Assert.Throws<InvalidOperationException>(() => { test.Dequeue(); });

                Assert.DoesNotThrow(() => { test.Enqueue(123); });
                Assert.Throws<InvalidOperationException>(() => { test.Enqueue(456); });

                int item = 0;
                Assert.DoesNotThrow(() => { item = test.Dequeue(); });
                Assert.AreEqual(123, item);

                Assert.DoesNotThrow(() => { test.Enqueue(456); });
                Assert.DoesNotThrow(() => { item = test.Dequeue(); });
                Assert.AreEqual(456, item);

                Assert.Throws<InvalidOperationException>(() => { test.Dequeue(); });
            }
        }

        [Test]
        public void UnsafeRingQueue_CustomAllocatorTest()
        {
            AllocatorManager.Initialize();
            var allocatorHelper = new AllocatorHelper<CustomAllocatorTests.CountingAllocator>(AllocatorManager.Persistent);
            ref var allocator = ref allocatorHelper.Allocator;
            allocator.Initialize();

            using (var container = new UnsafeRingQueue<int>(1, allocator.Handle))
            {
            }

            Assert.IsTrue(allocator.WasUsed);
            allocator.Dispose();
            allocatorHelper.Dispose();
            AllocatorManager.Shutdown();
        }

        [BurstCompile]
        struct BurstedCustomAllocatorJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public unsafe CustomAllocatorTests.CountingAllocator* Allocator;

            public void Execute()
            {
                unsafe
                {
                    using (var container = new UnsafeRingQueue<int>(1, Allocator->Handle))
                    {
                    }
                }
            }
        }

        [Test]
        public unsafe void UnsafeRingQueue_BurstedCustomAllocatorTest()
        {
            AllocatorManager.Initialize();
            var allocatorHelper = new AllocatorHelper<CustomAllocatorTests.CountingAllocator>(AllocatorManager.Persistent);
            ref var allocator = ref allocatorHelper.Allocator;
            allocator.Initialize();

            var allocatorPtr = (CustomAllocatorTests.CountingAllocator*)UnsafeUtility.AddressOf<CustomAllocatorTests.CountingAllocator>(ref allocator);
            unsafe
            {
                var handle = new BurstedCustomAllocatorJob { Allocator = allocatorPtr }.Schedule();
                handle.Complete();
            }

            Assert.IsTrue(allocator.WasUsed);
            allocator.Dispose();
            allocatorHelper.Dispose();
            AllocatorManager.Shutdown();
        }

        [Test]
        [Explicit("Uses in excess of 8GB memory, which often results in an out of memory condition in CI and sometimes a crash or hang on a local workstation.  You can run a script at Tools/CI/scripts/run-memory-heavy-collections-tests.sh which will run this and all other high memory usage max capacity Collections tests in individual editor instances.")]
        // Keep below [Explicit]: NUnit applies attributes in order and Explicit overwrites this skip
        [UnityPlatform(RuntimePlatform.WindowsPlayer, RuntimePlatform.OSXPlayer, RuntimePlatform.LinuxPlayer)]
        public void UnsafeRingQueue_Initialize_NumElements_To_MaxCapacity()
        {
            var container = new UnsafeRingQueue<int>(UnsafeRingQueue<int>.MaxCapacity, Allocator.Persistent);
            try
            {
                Assert.AreEqual(UnsafeRingQueue<int>.MaxCapacity, container.Capacity);
            }
            finally
            {
                container.Dispose();
            }
        }
    }
}
