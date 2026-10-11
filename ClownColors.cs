using System;
using System.Drawing;

namespace ClownId
{
    /// <summary>Deterministic object id → colour mapping.</summary>
    internal static class ClownColors
    {
        public static readonly Color Background = Color.Black;

        /// <summary>
        /// FNV-1a over the id bytes, low 24 bits as RGB. The same object gets the same
        /// colour in every session and every file it is saved in.
        /// </summary>
        public static Color For(Guid id)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (var b in id.ToByteArray())
                {
                    h ^= b;
                    h *= 16777619;
                }
                int r = (int)(h & 255), g = (int)((h >> 8) & 255), bl = (int)((h >> 16) & 255);
                // Keep clear of the black background.
                if (Math.Max(r, Math.Max(g, bl)) < 64)
                    r |= 0x80;
                return Color.FromArgb(255, r, g, bl);
            }
        }

        public static int Argb(Color c) => c.ToArgb() | unchecked((int)0xFF000000);

        public static string Hex(int argb) => "#" + (argb & 0xFFFFFF).ToString("X6");
    }
}
