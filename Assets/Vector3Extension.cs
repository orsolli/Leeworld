using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Extension
{

    public static class Vector3Extension
    {
        public static float epsilon = 0.0005f;
        // override Vector3.Equals
        public static bool Equals(this Vector3 self, Vector3 obj)
        {
            return Mathf.Abs(obj.x - self.x) < epsilon && Mathf.Abs(obj.y - self.y) < epsilon && Mathf.Abs(obj.z - self.z) < epsilon;
        }

        public static int GetHashCode(this Vector3 self)
        {
            return HashCode.Combine(self.x.GetHashCode(), self.y.GetHashCode(), self.z.GetHashCode());
        }
    }
    public static class intArrayExtension
    {
        // override int[].Equals
        public static bool Equals(this int[] self, int[] obj)
        {
            int min = self.Min();
            int selfMinIdx = 0;
            int objMinIdx = 0;
            for (int i = 0; i < self.Length; i++)
            {
                if (self[i] == min)
                    selfMinIdx = i;
                if (obj[i] == min)
                    objMinIdx = i;
            }
            if (self.Length != obj.Length)
                return false;
            for (int i = 0; i < self.Length; i++)
            {
                if (self[(i + selfMinIdx) % self.Length] != obj[(i + objMinIdx) % self.Length])
                    return false;
            }
            return true;
        }

        public static int GetHashCode(this int[] self)
        {
            int min = self.Min();
            int selfMinIdx = 0;
            for (int i = 0; i < self.Length; i++)
            {
                if (self[i] == min)
                    selfMinIdx = i;
            }
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < self.Length; i++)
                {
                    hash = hash * 23 + self[(i + selfMinIdx) % self.Length];
                }
                return hash;
            }
        }
    }
    public class IntArrayEqualityComparer : IEqualityComparer<int[]>
    {
        public bool Equals(int[] self, int[] obj)
        {
            int min = self.Min();
            int selfMinIdx = 0;
            int objMinIdx = 0;
            for (int i = 0; i < self.Length; i++)
            {
                if (self[i] == min)
                    selfMinIdx = i;
                if (obj[i] == min)
                    objMinIdx = i;
            }
            if (self.Length != obj.Length)
                return false;
            for (int i = 0; i < self.Length; i++)
            {
                if (self[(i + selfMinIdx) % self.Length] != obj[(i + objMinIdx) % self.Length])
                    return false;
            }
            return true;
        }

        public int GetHashCode(int[] self)
        {
            int min = self.Min();
            int selfMinIdx = 0;
            for (int i = 0; i < self.Length; i++)
            {
                if (self[i] == min)
                    selfMinIdx = i;
            }
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < self.Length; i++)
                {
                    hash = hash * 23 + self[(i + selfMinIdx) % self.Length];
                }
                return hash;
            }
        }
    }

    public static class Tuple3dExtension
    {
        public static float epsilon = 0.0005f;
        // override Net3dBool.Tuple3d.Equals
        public static bool Equals(this Net3dBool.Tuple3d self, Net3dBool.Tuple3d obj)
        {
            return Mathf.Abs((float)obj.x - (float)self.x) < epsilon && Mathf.Abs((float)obj.y - (float)self.y) < epsilon && Mathf.Abs((float)obj.z - (float)self.z) < epsilon;
        }

        public static int GetHashCode(this Net3dBool.Tuple3d self)
        {
            return HashCode.Combine(self.x.GetHashCode(), self.y.GetHashCode(), self.z.GetHashCode());
        }
    }

}
