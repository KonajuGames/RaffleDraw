using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RaffleDraw
{
    public static class RandomTools
    {
        // The Random object this method uses.
        private static Random rand = null;

        // Return num_items random values.
        public static List<T> PickRandom<T>(this IList<T> values, int numValues)
        {
            // Create the Random object if it doesn't exist.
            if (rand == null)
                rand = new Random();

            // Don't exceed the array's length.
            if (numValues >= values.Count)
                numValues = values.Count - 1;

            // Make an array of indexes 0 through values.Length - 1.
            int[] indexes = Enumerable.Range(0, values.Count).ToArray();

            // Build the return list.
            List<T> results = new List<T>();

            // Randomize the first num_values indexes.
            for (int i = 0; i < numValues; i++)
            {
                // Pick a random entry between i and values.Length - 1.
                int j = rand.Next(i, values.Count);

                // Swap the values.
                int temp = indexes[i];
                indexes[i] = indexes[j];
                indexes[j] = temp;

                // Save the ith value.
                results.Add(values[indexes[i]]);
            }

            // Return the selected items.
            return results;
        }
    }
}
