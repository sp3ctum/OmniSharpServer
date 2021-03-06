﻿using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace OmniSharp.Tests
{
    public static class ObjectExtensions
    {
        public static void ShouldEqual(this string actual, string expected)
        {
            Assert.AreEqual(expected, actual);
        }

        public static void ShouldEqual<T>(this IEnumerable<T> actual, params T[] expected)
        {
            CollectionAssert.AreEqual(expected, actual.ToArray());
        }
        
        public static void ShouldContainOnly<T>(this IEnumerable<T> actual, params T[] expected)
        {
            actual.ShouldContainOnly(new List<T>(expected));
        }

        public static void ShouldContain<T>(this IEnumerable<T> actual, params T[] expected)
        {
            var actualList = new List<T>(actual);
            foreach (T item in expected)
            {
                Assert.Contains(item, actualList);
            }
        }

        public static void ShouldNotContain<T>(this IEnumerable<T> actual, params T[] expected)
        {
            var actualList = new List<T>(actual);
            foreach (T item in expected)
            {
                CollectionAssert.DoesNotContain(actualList, item);
            }
        }

        public static void ShouldContainOnly<T>(this IEnumerable<T> actual, IEnumerable<T> expected)
        {
            var actualList = new List<T>(actual);
            var remainingList = new List<T>(actualList);
            foreach (T item in expected)
            {
                Assert.Contains(item, actualList);
                remainingList.Remove(item);
            }
            Assert.IsEmpty(remainingList);
        }
    }
}
