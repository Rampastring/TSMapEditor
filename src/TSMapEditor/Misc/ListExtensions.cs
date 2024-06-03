using System;
using System.Collections.Generic;

namespace TSMapEditor.Misc
{
    public static class ListExtensions
    {
        /// <summary>
        /// Swaps two list items.
        /// </summary>
        public static void Swap<T>(this List<T> list, int index1, int index2)
        {
            (list[index1], list[index2]) = (list[index2], list[index1]);
        }

        /// <summary>
        /// Fetches an element at the given index.
        /// If the element is out of bounds, returns null.
        /// </summary>
        public static T GetElementIfInRange<T>(this IList<T> list, int index)
        {
            if (index < 0 || index >= list.Count)
                return default;

            return list[index];
        }

        /// <summary>
        /// Fetches a random element from the list using the provided Random instance.
        /// </summary>
        public static int GetRandomElementIndex<T>(this List<T> list, Random random, int disallowedIndex)
        {
            int randomIndex = random.Next(0, list.Count);
            if (randomIndex == disallowedIndex)
                randomIndex = (randomIndex + 1) % list.Count;

            return randomIndex;
        }
    }

    public static class ArrayExtensions
    {
        public static void Swap<T>(this T[] array, int index1, int index2)
        {
            (array[index1], array[index2]) = (array[index2], array[index1]);
        }
    }
}
