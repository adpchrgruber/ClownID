using System;
using System.Collections.Generic;

namespace ClownId
{
    /// <summary>Cleans up a captured clown pass so every pixel is exactly one palette colour.</summary>
    internal static class ClownMask
    {
        /// <summary>
        /// Replaces off-palette pixels (anti-aliased edges) with the closest colour among
        /// their exact neighbours, so an edge pixel can only become one of the objects it
        /// borders. Falls back to the closest palette colour when no neighbour is exact.
        /// Returns the number of pixels changed.
        /// </summary>
        public static int Snap(int[] pixels, int width, int height, HashSet<int> palette)
        {
            var source = (int[])pixels.Clone();
            int changed = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    int c = source[i];
                    if (palette.Contains(c))
                        continue;
                    pixels[i] = ClosestNeighbour(source, width, height, x, y, c, palette) ?? Closest(c, palette);
                    changed++;
                }
            }
            return changed;
        }

        static int? ClosestNeighbour(int[] source, int width, int height, int x, int y, int c, HashSet<int> palette)
        {
            for (int radius = 1; radius <= 2; radius++)
            {
                int? best = null;
                int bestDistance = int.MaxValue;
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int ny = y + dy;
                    if (ny < 0 || ny >= height)
                        continue;
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int nx = x + dx;
                        if (nx < 0 || nx >= width || (Math.Abs(dx) != radius && Math.Abs(dy) != radius))
                            continue;
                        int n = source[ny * width + nx];
                        if (!palette.Contains(n))
                            continue;
                        int d = Distance(c, n);
                        if (d < bestDistance)
                        {
                            bestDistance = d;
                            best = n;
                        }
                    }
                }
                if (best.HasValue)
                    return best;
            }
            return null;
        }

        static int Closest(int c, HashSet<int> palette)
        {
            int best = unchecked((int)0xFF000000), bestDistance = int.MaxValue;
            foreach (var p in palette)
            {
                int d = Distance(c, p);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = p;
                }
            }
            return best;
        }

        static int Distance(int a, int b)
        {
            int dr = ((a >> 16) & 255) - ((b >> 16) & 255);
            int dg = ((a >> 8) & 255) - ((b >> 8) & 255);
            int db = (a & 255) - (b & 255);
            return dr * dr + dg * dg + db * db;
        }
    }
}
