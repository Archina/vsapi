﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Vintagestory.API.Util
{
    public class FastSetOfLongs : IEnumerable<long>
    {
        int maxSize = 27;
        int size = 0;
        long[] set;
        long last = long.MinValue;

        public FastSetOfLongs()
        {
            set = new long[maxSize];
        }

        /// <summary>
        /// Return false if the set already contained this value; return true if the Add was successful
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Add(long value)
        {
            if (value == last) return false;
            last = value;

            // fast search, start from the most recently added - this should be faster than any HashSet up to several hundred elements in size
            int i = size;
            while (--i >= 0)
            {
                if (set[i] == value) return false;
            }

            // now actually add the value
            if (size + 1 >= maxSize) expandArray();
            set[size++] = value;
            return true;

            //TODO: we could make it a sorted array and boolean search - maybe worthwhile for larger sets?
        }

        private void expandArray()
        {
            int newSize = maxSize * 3 / 2 + 1;
            long[] newArray = new long[newSize];
            for (int i = 0; i < size; i++) newArray[i] = set[i];
            set = newArray;
        }


        public IEnumerator<long> GetEnumerator()
        {
            for (int i = 0; i < size; i++)
                yield return set[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
