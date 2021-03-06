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

namespace SharpReport.PDF {
	public class FontGlyph {

		public FontGlyph(int width, int unicode) {
			this.width = width;
			this.unicode = unicode;
		}

		public FontGlyph(int width, ushort leftSideBearing) {
			this.width = width;
			this.leftSideBearing = leftSideBearing;
		}

		public void SetFilePosition(int offset, int length) {
			m_offsetFile = offset;
			m_lengthFile = length;
		}		

		public int offsetFile => m_offsetFile;

		public int lengthFile => m_lengthFile;

		public int width { get; set; }

		public int unicode  { get; set; }	

		public ushort leftSideBearing { get; set; }		

        /// <summary>
        /// The offset of the glyph in the file
        /// </summary>
        private int m_offsetFile;

        /// <summary>
        /// The length of the glyph in the file
        /// </summary>
        private int m_lengthFile;
	}
}
