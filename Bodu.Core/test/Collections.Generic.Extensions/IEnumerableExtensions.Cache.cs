// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Cache.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿using Bodu.Infrastructure;

namespace Bodu.Collections.Generic.Extensions
{
    [TestClass]
    public sealed partial class IEnumerableExtensionsTests_Cache : EnumerableTests
    {
        /// <summary>
        /// Verifies that Cache Defer Execution.
        /// </summary>
        [TestMethod]
        public void Cache_ShouldDeferExecution()
        {
            AssertExecutionIsDeferred("Cache", s => s.Cache(), YieldingSequence());
        }

        /// <summary>
        /// Verifies that Cache Enumerate On Demand.
        /// </summary>
        [TestMethod]
        public void Cache_ShouldEnumerateOnDemand()
        {
            AssertExecutionOccursOnEnumeration("Cache", s => s.Cache(), YieldingSequence());
        }

        /// <summary>
        /// Verifies that Cache, when Enumerated From Multiple Threads, returns Consistent Results.
        /// </summary>
        [TestMethod]
        public void Cache_WhenEnumeratedFromMultipleThreads_ShouldReturnConsistentResults()
        {
            var tracker = new TrackingEnumerable<int>(YieldingSequence());
            var actual = tracker.Cache();

            Parallel.For(0, 5, _ =>
            {
                var result = actual.ToList();
                CollectionAssert.AreEqual(YieldingSequence().ToArray(), result);
            });
        }

        /// <summary>
        /// Verifies that Cache, when Enumerated Twice, Enumerate Source Only Once.
        /// </summary>
        [TestMethod]
        public void Cache_WhenEnumeratedTwice_ShouldEnumerateSourceOnlyOnce()
        {
            var tracker = new TrackingEnumerable<int>(YieldingSequence());
            var actual = tracker.Cache();
            var first = actual.ToList();
            var second = actual.ToList();

            CollectionAssert.AreEqual(first, second);
            Assert.AreEqual(YieldingSequence().Count(), tracker.ItemsEnumerated);
        }

        /// <summary>
        /// Verifies that Cache, when Enumeration Is Interrupted, caches Partial Results.
        /// </summary>
        [TestMethod]
        public void Cache_WhenEnumerationIsInterrupted_ShouldCachePartialResults()
        {
            var source = YieldingSequence();
            var tracker = new TrackingEnumerable<int>(source);
            var actual = tracker.Cache();

            using var enumerator = actual.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext()); // 1
            Assert.IsTrue(enumerator.MoveNext()); // 2

            var result = actual.ToList();

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, result);
            Assert.AreEqual(5, tracker.ItemsEnumerated);
        }

        /// <summary>
        /// Verifies that Cache, when Source Is Already Cached, returns Same Instance.
        /// </summary>
        [TestMethod]
        public void Cache_WhenSourceIsAlreadyCached_ShouldReturnSameInstance()
        {
            var source = Enumerable.Range(1, 3).Cache();
            var result = source.Cache();

            Assert.AreSame(source, result);
        }

        /// <summary>
        /// Verifies that Cache, when Source Is Collection, returns Same Instance.
        /// </summary>
        [TestMethod]
        public void Cache_WhenSourceIsCollection_ShouldReturnSameInstance()
        {
            var source = new List<int> { 1, 2, 3 };
            var actual = source.Cache();

            Assert.AreSame(source, actual);
        }

        /// <summary>
        /// Verifies that Cache, when Source Is Null, throws Exactly.
        /// </summary>
        [TestMethod]
        public void Cache_WhenSourceIsNull_ShouldThrowExactly()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                IEnumerableExtensions.Cache<int>(null!);
            });
        }

        /// <summary>
        /// Verifies that Cache, when Source Is Read Only Collection, returns Same Instance.
        /// </summary>
        [TestMethod]
        public void Cache_WhenSourceIsReadOnlyCollection_ShouldReturnSameInstance()
        {
            var source = Array.AsReadOnly(new[] { 1, 2, 3 });
            var actual = source.Cache();

            Assert.AreSame(source, actual);
        }

        /// <summary>
        /// Verifies that Cache, when Source Throws During Enumeration, Rethrow On Second Enumeration.
        /// </summary>
        [TestMethod]
        public void Cache_WhenSourceThrowsDuringEnumeration_ShouldRethrowOnSecondEnumeration()
        {
            var source = new TrackingEnumerable<int>(ThrowingSequence());
            var cached = source.Cache();

            try
            {
                foreach (var _ in cached)
                {
                    // Force enumeration to trigger exception Only first value is valid
                }
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            // Assert: Re-enumerating throws the same exception again
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                {
                    foreach (var _ in cached)
                    {
                    }
                }
                ;
            });
        }

        /// <summary>
        /// Verifies that Cache, when Source Throws During Enumeration, throws On First Enumeration.
        /// </summary>
        [TestMethod]
        public void Cache_WhenSourceThrowsDuringEnumeration_ShouldThrowOnFirstEnumeration()
        {
            // Arrange
            var source = new TrackingEnumerable<int>(ThrowingSequence());
            var cached = source.Cache();
            var enumerator = cached.GetEnumerator();

            // Act: First item succeeds
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current);

            // Assert: Second item throws
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                {
                    enumerator.MoveNext();
                }
                ;
            });
        }

        /// <summary>
        /// Verifies that Dispose Clear Cache And Enumerator.
        /// </summary>
        [TestMethod]
        public void Dispose_ShouldClearCacheAndEnumerator()
        {
            // Arrange
            var source = new TrackingEnumerable<int>(YieldingSequence());
            var cached = source.Cache();

            // Act: Enumerate to force caching
            _ = cached.ToList();

            // Cast to IDisposable for disposal
            if (cached is IDisposable disposable)
            {
                disposable.Dispose();
            }
            else
            {
                Assert.Fail("Cached sequence should be IDisposable.");
            }

            // Re-enumeration should trigger a new enumeration
            var reenumerated = new TrackingEnumerable<int>(YieldingSequence());
            var recached = reenumerated.Cache();

            // Assert: Ensure second enumeration is treated as a fresh sequence
            AssertExecutionOccursOnEnumeration("Cache", s => s.Cache(), YieldingSequence());
        }

        /// <summary>
        /// Verifies that Enumerator, Current, when After End, throws Exception.
        /// </summary>
        [TestMethod]
        public void Enumerator_Current_WhenAfterEnd_ShouldThrowException()
        {
            var actual = YieldingSequence().Cache();
            var enumerator = actual.GetEnumerator();
            while (enumerator.MoveNext()) { }

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                {
                    _ = enumerator.Current;
                }
                ;
            });
        }

        /// <summary>
        /// Verifies that Enumerator, Current, when Before Move Next, throws Exception.
        /// </summary>
        [TestMethod]
        public void Enumerator_Current_WhenBeforeMoveNext_ShouldThrowException()
        {
            var actual = YieldingSequence().Cache();
            var enumerator = actual.GetEnumerator();

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                {
                    _ = enumerator.Current;
                }
                ;
            });
        }

        /// <summary>
        /// Verifies that Enumerator, Reset, throws Exception.
        /// </summary>
        [TestMethod]
        public void Enumerator_Reset_ShouldThrowException()
        {
            var actual = YieldingSequence().Cache();
            var enumerator = actual.GetEnumerator();

            Assert.ThrowsExactly<NotSupportedException>(() =>
            {
                enumerator.Reset();
            });
        }

        private static IEnumerable<int> ThrowingSequence()
        {
            yield return 1;
            throw new InvalidOperationException("Test");
        }

        private static IEnumerable<int> YieldingSequence()
        {
            yield return 1;
            yield return 2;
            yield return 3;
            yield return 4;
            yield return 5;
        }
    }
}