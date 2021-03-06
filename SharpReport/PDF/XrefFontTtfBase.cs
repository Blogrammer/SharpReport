// This file is part of SharpReport.
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
using System.IO;
using System.Text;

namespace SharpReport.PDF {		
	public class XrefFontTtfBase: XrefFont {
		internal XrefFontDescriptor m_descriptor;

		internal FontTypes fontsFlags;

        internal Dictionary<string, Table> dctTables = new Dictionary<string, Table>();

        internal XrefFontTtfBase(string ttfFileName) {
            FullFileName = ttfFileName;
            fontsFlags = FontTypes.Nonsymbolic;
            boundingBox[0] = -1166;
            boundingBox[1] = -638;
            boundingBox[2] = 2260;
            boundingBox[3] = 1050;

            Ascendent = 800;
            Descendent = -200;

            Width = 1000;

            Process(ttfFileName);
        }


        private void Process(string TTFFileName) {
			TTFFont = File.ReadAllBytes(TTFFileName);

			int version = GetUInt32();

			if (version != 0x00010000 && version != 0x4F54544F) {
				throw new NotSupportedException("TTF version not supported: " + version.ToString("X4"));
			}
						
			ushort numTables = GetUInt16();	// Number of tables.
			int searchRange = GetUInt16();
            Skip(2);						// entrySelector
            int rangeShift = GetUInt16();

            if (rangeShift != numTables * 16 - searchRange) {
				throw new FontException("rangeShift is not correct");
			}

			for (int i = 0; i < numTables; i++) {
				string tag = GetString(4);
				
				dctTables.Add(tag, new Table{
                    checksum = GetUInt32(),
					offset = GetUInt32(),
					length = GetUInt32()
				});
			}

			ProcessHead(dctTables["head"]);
			ProcessHHEA(dctTables["hhea"]);
			ProcessHMTX(dctTables["hmtx"]);
			ProcessCMAP(dctTables["cmap"]);

			// simplificación
			FontName = Path.GetFileNameWithoutExtension(TTFFileName);

			// kerning
			ProcessGLYPH(dctTables["glyf"], dctTables["loca"]);
		}

        private void ProcessGLYPH(Table tableGlyph, Table tableLoca) {
			filePosition = tableLoca.offset;

			int[] glyphOffset;

			if (indexToLocFormat == 0) {
				int numEntries = tableLoca.length / 2;	

    			glyphOffset = new int[numEntries];

				for (int i = 0; i < numEntries; i++) {
					glyphOffset[i] = GetUInt16() * 2;
				}
			} else {
				int numEntries = tableLoca.length / 4;	

				glyphOffset = new int[numEntries];

				for (int i = 0; i < numEntries; i++) {
					glyphOffset[i] = GetUInt32();
				}
			}
				
			// puede que ahora tengamos mas glyphs que hmea
			if (Glypth.Length < glyphOffset.Length) {
				Array.Resize(ref Glypth, glyphOffset.Length);
			}

			// In order to compute the length of the last glyph element, there is an extra entry after the last valid index.
			for (int i = 0; i < glyphOffset.Length-1; i++) {
				filePosition = tableGlyph.offset + glyphOffset[i] + 2;

				Glypth[i].SetFilePosition(tableGlyph.offset + glyphOffset[i], glyphOffset[i+1] - glyphOffset[i]);

				Skip(8);  	// lowerX, lowerY, upperX, uppery				
			}
		}

       	private void ProcessHead(Table table) {
			filePosition = table.offset;
        
			ushort majorVersion = GetUInt16();
			ushort minorVersion = GetUInt16();

			if (majorVersion != 1 || minorVersion != 0) {
				throw new NotSupportedException("TTF head version not supported: " + majorVersion + "." + minorVersion);
			}

			Skip(8);		// fontRevision, checkSumAdjustment

			int magicNumber = GetUInt32();
			if (magicNumber != 0x5F0F3CF5) {
				throw new NotSupportedException("TTF version not supported head.magicNumber: " + magicNumber.ToString("X4"));
			}

			Skip(2);	// flags
						// Bit 0: Baseline for font at y=0
						// Bit 1: Left sidebearing point at x=0 (relevant only for TrueType rasterizers) — see the note below regarding variable fonts
						// Bit 2: Instructions may depend on point size
						// Bit 3: Force ppem to integer values for all internal scaler math; may use fractional ppem sizes if this bit is clear
						// Bit 4: Instructions may alter advance width (the advance widths might not scale linearly)
						// Bit 5: This bit is not used in OpenType, and should not be set in order to ensure compatible behavior on all platforms. If set, it may result in different behavior for vertical layout in some platforms. (See Apple's specification for details regarding behavior in Apple platforms.)
						// Bits 6–10: These bits are not used in Opentype and should always be cleared. (See Apple's specification for details regarding legacy used in Apple platforms.)
						// Bit 11: Font data is ‘lossless’ as a results of having been subjected to optimizing transformation and/or compression (such as e.g. compression mechanisms defined by ISO/IEC 14496-18, MicroType Express, WOFF 2.0 or similar) where the original font functionality and features are retained but the binary compatibility between input and output font files is not guaranteed. As a result of the applied transform, the ‘DSIG’ Table may also be invalidated.
						// Bit 12: Font converted (produce compatible metrics)
						// Bit 13: Font optimized for ClearType™. Note, fonts that rely on embedded bitmaps (EBDT) for rendering should not be considered optimized for ClearType, and therefore should keep this bit cleared.
						// Bit 14: Last Resort font. If set, indicates that the glyphs encoded in the cmap subtables are simply generic symbolic representations of code point ranges and don’t truly represent support for those code points. If unset, indicates that the glyphs encoded in the cmap subtables represent proper support for those code points.
						// Bit 15: Reserved, set to 0 

			unitsPerEm = GetUInt16();

			Skip(16);	// creationtime modifiedtime

			boundingBox[0] = GetInt16();
			boundingBox[1] = GetInt16();
			boundingBox[2] = GetInt16();
			boundingBox[3] = GetInt16();
			
			Skip(6);	// macStyle						
						// Bit 0: Bold (if set to 1)
						// Bit 1: Italic (if set to 1)
						// Bit 2: Underline (if set to 1)
						// Bit 3: Outline (if set to 1)
						// Bit 4: Shadow (if set to 1)
						// Bit 5: Condensed (if set to 1)
						// Bit 6: Extended (if set to 1)
						// Bits 7–15: Reserved (set to 0).
						// lowestRecPPEM
						// fontDirectionHint
						
			indexToLocFormat = GetInt16();
		}

		private void ProcessHHEA(Table table) {
			filePosition = table.offset;

			ushort majorVersion = GetUInt16();
			ushort minorVersion = GetUInt16();

			if (majorVersion != 1 || minorVersion != 0) {
				throw new NotSupportedException("TTF head version not supported: " + majorVersion + "." + minorVersion);
			}

			Ascendent = GetInt16();
			Descendent = GetInt16();

			Skip(26);	// LineGap:  Typographic line gap. Negative LineGap values are treated as zero in Windows 3.1, and in Mac OS System 6 and System 7.
						// advanceWidthMax: Maximum advance width value in 'hmtx' table.
						// minLeftSideBearing: Minimum left sidebearing value in 'hmtx' table.
						// minRightSideBearing: Minimum right sidebearing value; calculated as Min(aw - lsb - (xMax - xMin)).
						// xMaxExtent: Max(lsb + (xMax - xMin)).
						// caretSlopeRise: Used to calculate the slope of the cursor (rise/run); 1 for vertical.
						// caretSlopeRun: 0 for vertical.
						// caretOffset: The amount by which a slanted highlight on a glyph needs to be shifted to produce the best appearance. Set to 0 for non-slanted fonts	
						// 8						
						// metricDataFormat

			numberOfHMetrics = GetUInt16();
		}

		private void ProcessCMAP(Table table) {
			filePosition = table.offset;
			Skip(2);	// cmapVersion
			ushort cmapNumTables = GetUInt16();

            int CMAP_10 = 0;
            int CMAP_310 = 0;

			for (int i = 0; i < cmapNumTables; i++) {
				ushort platformID = GetUInt16();
				ushort encodingID = GetUInt16();
				int offset = GetUInt32();
                if (platformID == 1 && encodingID == 0) {
                    CMAP_10 = table.offset + offset;
                } else if (platformID == 3 && encodingID == 10) {
                    CMAP_310 = table.offset + offset;					
				}
			}

            if (CMAP_310 > 0) {
                filePosition = CMAP_310;
                int format = GetUInt16();

                switch (format) {
                    case 0:
                        ProcessCMAP0();
                        break;
                    case 4:
                        ProcessCMAP4();
                        break;
                    case 6:
                        ProcessCMAP6();
                        break;
                    case 12:
                        ProcessCMAP12();
                        break;
                }
            } else if (CMAP_10 > 0) {
                filePosition = CMAP_10;
                int format = GetUInt16();
                switch (format) {
                    case 0:
                        ProcessCMAP0();
                        break;
                    case 4:
                        ProcessCMAP4();
                        break;
                    case 6:
                        ProcessCMAP6();
                        break;
                }
            }
		}

        private void ProcessCMAP0() {
            Skip(4);
            for (int i = 0; i < 256; i++) {
                int glyphId = GetUInt8();
                dctCharCodeToGlyphID.Add(i, glyphId);
                Glypth[glyphId].unicode = i;
            }
        }


        private void ProcessCMAP4() {
            int length = GetUInt16();           // This is the length in bytes of the subtable.
			Skip(2);				            // language, Please see "Note on the language field in 'cmap' subtables" in this document.
            int segCount = GetUInt16() >> 1;    // 2 x segCount.
            int searchRange = GetUInt16();
            Skip(2);							// entrySelector
            int rangeShift = GetUInt16();
            if (rangeShift != 2 * segCount - searchRange)
                throw new FontException("Invalid CMAP 4 format");

            int[] endCount = new int[segCount];
            for (int i = 0; i < segCount; i++) {
                endCount[i] = GetUInt16();
            }

			Skip(2); 							// reservedPad

            int[] startCount = new int[segCount];
            for (int i = 0; i < segCount; i++) {
                startCount[i] = GetUInt16();
            }
            int[] idDelta = new int[segCount];
            for (int i = 0; i < segCount; i++) {
                idDelta[i] = GetUInt16();
            }
            int[] idRangeOffset = new int[segCount];
            for (int i = 0; i < segCount; i++) {
                idRangeOffset[i] = GetUInt16();
            }
            int[] glyphIdArray = new int[(length >> 1) - 8 - segCount << 2];
            for (int i = 0; i < glyphIdArray.Length; i++) {
                glyphIdArray[i] = GetUInt16();
            }

            for (int segmentIndex = 0; segmentIndex < segCount; segmentIndex++) {
                for (int charIndex = startCount[segmentIndex]; charIndex <= endCount[segmentIndex]; charIndex++) {
                    int glyphId; 
                    if (idRangeOffset[segmentIndex] == 0) {
                        glyphId = idDelta[segmentIndex] + charIndex;
                    } else {
                        int j = charIndex - startCount[segmentIndex] + segmentIndex + idRangeOffset[segmentIndex] / 2 - segCount;
                        glyphId = glyphIdArray[j] ;
                    }

                    dctCharCodeToGlyphID.Add(charIndex, glyphId);
                    Glypth[glyphId].unicode = charIndex;
                }
            }
        }

        private void ProcessCMAP6() {
            Skip(4);
            int ini = GetUInt16();
            int count = GetUInt16();

            for (int i = ini; i < ini + count; i++) {
                int glyphId = GetUInt16();
                dctCharCodeToGlyphID.Add(i, glyphId);
                Glypth[glyphId].unicode = i;
            }
        }

		private void ProcessCMAP12() {
			Skip(10); 	// reserved (16)
						// length (32)
						// language (32)
			int numGroups = GetUInt32();

			for (int i = 0; i < numGroups; i++) {
				int startCharCode = GetUInt32();
				int endCharCode = GetUInt32();
				int startGlyphID = GetUInt32();

				int z = 0;
				for (int j = startCharCode; j <= endCharCode; j++) {
					dctCharCodeToGlyphID.Add(j, startGlyphID + z);
					Glypth[startGlyphID + z].unicode = j;
					z++;
				}
			}
		}

		private void ProcessHMTX(Table table)
		{
			filePosition = table.offset;
			Glypth = new FontGlyph[numberOfHMetrics];

			for (int i = 0; i < numberOfHMetrics; i++) {		
				Glypth[i] = new FontGlyph(GetUInt16() * 1000 / unitsPerEm,  GetUInt16());
			}
		}

        public override byte[] GetFont() {
            return TTFFont;
        }

      	internal struct Table {
			internal int offset;
			internal int length;
            internal int checksum;
		}		


		/// <summary>
		/// Number of hMetric entries in 'hmtx' table
		/// </summary>
		private ushort numberOfHMetrics;

		/// <summary>
		/// Valid range is from 16 to 16384. This value should be a power of 2 for fonts that have TrueType outlines.
		/// </summary>
		internal ushort unitsPerEm;

		private int filePosition;

		private short GetInt16() {
			short result = (short) (TTFFont[filePosition] << 8 | TTFFont[filePosition+1]);
			filePosition += 2;
			return result;
		}

        private ushort GetUInt8() {           
            ushort result = (ushort) (TTFFont[filePosition]);
            filePosition += 1;
            return result;
        }

		private ushort GetUInt16() {			
			ushort result = (ushort) (TTFFont[filePosition] << 8 | TTFFont[filePosition+1]);
			filePosition += 2;
			return result;
		}

		private int GetUInt32() {
			int result = (TTFFont[filePosition] << 24 | TTFFont[filePosition+1] << 16 | TTFFont[filePosition+2] << 8 | TTFFont[filePosition+3]);
			filePosition += 4;
			return result;
		}

		private string GetString(int length) {
			string ret = System.Text.Encoding.ASCII.GetString(TTFFont, filePosition, length);
			filePosition += length;
			return ret;
		}

		private void Skip(int bytes) {
			filePosition += bytes;
		}

		public override byte[] Write() {	
			StringBuilder sb = new StringBuilder();

			if (FirstChar.HasValue) {
				sb.Append(Glypth[dctCharCodeToGlyphID[FirstChar.Value]].width);
				for (int i = FirstChar.Value + 1; i < LastChar+1; i++) {
					sb.Append(" ");
					if (!hashChar.Contains(i)) {
						sb.Append("0");
					} else if (!dctCharCodeToGlyphID.ContainsKey(i)) {
						sb.Append(this.Width);
					} else {
						sb.Append(Glypth[dctCharCodeToGlyphID[i]].width);
					}
				}
			}
			return GetBytes("<</Encoding/WinAnsiEncoding/Type/Font/Subtype/TrueType/Widths [" + sb + "]/FirstChar " + FirstChar + "/LastChar " + LastChar + "/FontDescriptor " + m_descriptor.ID + " 0 R/BaseFont/" + FontName + ">>");
		}		
	}
}