﻿// This file is part of SharpReport.
// 
// SharpReport is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// SharpReport is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with SharpReport.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;

namespace SharpReport.PDF
{
	public abstract class XrefFont : Xref
	{		
		/// <summary>
		/// The maximum height above the baseline reached by glyphs in this font.  
		/// The height of glyphs for accented characters shall be excluded. 
		/// </summary>
		public short Ascendent { get; set; }

		/// <summary>
		/// The maximum depth below the baseline reached by glyphs in this font. 
		/// The value shall be a negative number. 
		/// </summary>
		public short Descendent { get; set; }

		public string FontName { get; set; }

		public string FullFileName { get; set; }


		public float GetAscendent(float size) {
			return Ascendent * 0.001f * size;
		}

		public float GetDescendent(float size) {
			return Descendent * 0.001f * size;
		}

		public float GetWidthPointKerned(string text, float size) {
			float currentSize = 0.0f;
			float kerning = 0.0f;
			int previousChar = -1;

			foreach (char ch in text) {
				if (dctCharCodeToGlyphID.ContainsKey((int)ch))
					currentSize += Glypth[dctCharCodeToGlyphID[(int)ch]].width;

				if (previousChar >= 0) {
					int key = (previousChar << 16) + (int)ch;
					if (dctKerning.ContainsKey(key))
						kerning += dctKerning[key];
				}

				previousChar = ch;
			}


			return (currentSize + kerning) * 0.001f * size;
		}


		/// <summary>
		/// The width.
		/// </summary>
		public int Width { get; set; }

        internal FontGlyph[] Glypth;

		public Dictionary<int, short> dctKerning = new Dictionary<int, short>();

		protected Dictionary<int, int> dctCharCodeToGlyphID = new Dictionary<int, int>();

		/// <summary>
		/// The boundingBox of all glyphs
		/// specifying the lower-left x, lower-left y, upper-right x, and upper-right y coordinates of the rectangle.
		/// </summary>
		public short[] boundingBox = new short[4];

		/// <summary>
		/// 0 for short offsets (Offset16), 1 for long (Offset32).
		/// </summary>
		internal short indexToLocFormat { get; set; }

		/// <summary>
		/// The angle, expressed in degrees counterclockwise from the vertical, of the dominant vertical strokes of the font. 
		/// EXAMPLE: The 9-o’clock position is  90  degrees,  and  the  3-o’clock position is –90 degrees. 
		/// The value shall be negative for fonts that slope to the right, as almost all italic fonts do.
		/// </summary>
		internal int ItalicAngle { get; set; }

		/// <summary>
		/// The spacing between baselines of consecutive lines of text. Default value: 0. 
		/// </summary>
		internal int Leading { get; set; }

		/// <summary>
		/// The vertical coordinate of the top of flat capital letters, measured from the baseline. 
		/// </summary>
		internal int CapHeight = 729;
        // TODO OS/2

		/// <summary>
		/// The thickness, measured horizontally, of the dominant vertical stems of glyphs in the font. 
        /// https://stackoverflow.com/questions/35485179/stemv-value-of-the-truetype-font
        /// This value is not used
		/// </summary>
		public int StemV = 80;

		/// <summary>
		/// TTF Font
		/// </summary>
		protected byte[] TTFFont;


        internal int? FirstChar;
        internal int LastChar  = -1;
        internal readonly HashSet<int> hashChar = new HashSet<int>();

        internal bool isUnicode = false;

        internal virtual int GetGlyphId(int ch) {
            return dctCharCodeToGlyphID[ch];
        }

        internal virtual FontGlyph GetGlyph(int gliphtId) {
            return Glypth[gliphtId];
        }

        /// <summary>
        /// Escriben texto con esta fuente, me apunto cosas
        /// </summary>
        /// <param name="text">Text.</param>
        public virtual void SetText(string text) {
            foreach (char c in text) {
                AddNewChar(c);
            }
        }

        /// <summary>
        /// Get the font byte array
        /// </summary>
        /// <returns>The font.</returns>
        public virtual byte[] GetFont() {
            return new byte[0];
        }

        protected void AddNewChar(char c) {
            hashChar.Add((int)c);

            if (FirstChar == null || c < FirstChar)
                FirstChar = c;
            if (c > LastChar)
                LastChar = c;
        }
    }
}
