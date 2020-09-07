using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OX
{
    public static class MathHelper
    {
        public static int[] ToFlags(this int X)
        {
            List<int> list = new List<int>();
            int i = 0;
            while (X > 0)
            {
                int m = X % 2;
                X = X / 2;
                if (m == 1)
                    list.Add((int)Math.Pow(2, i));
                i++;
            }
            return list.ToArray();
        }
    }
}
