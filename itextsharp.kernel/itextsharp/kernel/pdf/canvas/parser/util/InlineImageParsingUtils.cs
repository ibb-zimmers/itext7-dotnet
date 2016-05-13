/*
$Id: bb2d5998491ae594bae11143fb0bf8978548a440 $

This file is part of the iText (R) project.
Copyright (c) 1998-2016 iText Group NV
Authors: Bruno Lowagie, Paulo Soares, et al.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/
using System;
using System.Collections.Generic;
using System.IO;
using com.itextpdf.io.source;
using com.itextpdf.kernel;
using com.itextpdf.kernel.pdf;
using com.itextpdf.kernel.pdf.filters;

namespace com.itextpdf.kernel.pdf.canvas.parser.util
{
	/// <summary>Utility methods to help with processing of inline images</summary>
	public sealed class InlineImageParsingUtils
	{
		private InlineImageParsingUtils()
		{
		}

		/// <summary>
		/// Simple class in case users need to differentiate an exception from processing
		/// inline images vs other exceptions
		/// </summary>
		public class InlineImageParseException : PdfException
		{
			private const long serialVersionUID = 233760879000268548L;

			public InlineImageParseException(String message)
				: base(message)
			{
			}
		}

		/// <summary>
		/// Map between key abbreviations allowed in dictionary of inline images and their
		/// equivalent image dictionary keys
		/// </summary>
		private static readonly IDictionary<PdfName, PdfName> inlineImageEntryAbbreviationMap;

		/// <summary>Map between value abbreviations allowed in dictionary of inline images for COLORSPACE
		/// 	</summary>
		private static readonly IDictionary<PdfName, PdfName> inlineImageColorSpaceAbbreviationMap;

		/// <summary>Map between value abbreviations allowed in dictionary of inline images for FILTER
		/// 	</summary>
		private static readonly IDictionary<PdfName, PdfName> inlineImageFilterAbbreviationMap;

		static InlineImageParsingUtils()
		{
			// static initializer
			// Map between key abbreviations allowed in dictionary of inline images and their
			// equivalent image dictionary keys
			inlineImageEntryAbbreviationMap = new Dictionary<PdfName, PdfName>();
			// allowed entries - just pass these through
			inlineImageEntryAbbreviationMap[PdfName.BitsPerComponent] = PdfName.BitsPerComponent;
			inlineImageEntryAbbreviationMap[PdfName.ColorSpace] = PdfName.ColorSpace;
			inlineImageEntryAbbreviationMap[PdfName.Decode] = PdfName.Decode;
			inlineImageEntryAbbreviationMap[PdfName.DecodeParms] = PdfName.DecodeParms;
			inlineImageEntryAbbreviationMap[PdfName.Filter] = PdfName.Filter;
			inlineImageEntryAbbreviationMap[PdfName.Height] = PdfName.Height;
			inlineImageEntryAbbreviationMap[PdfName.ImageMask] = PdfName.ImageMask;
			inlineImageEntryAbbreviationMap[PdfName.Intent] = PdfName.Intent;
			inlineImageEntryAbbreviationMap[PdfName.Interpolate] = PdfName.Interpolate;
			inlineImageEntryAbbreviationMap[PdfName.Width] = PdfName.Width;
			// abbreviations - transform these to corresponding correct values
			inlineImageEntryAbbreviationMap[new PdfName("BPC")] = PdfName.BitsPerComponent;
			inlineImageEntryAbbreviationMap[new PdfName("CS")] = PdfName.ColorSpace;
			inlineImageEntryAbbreviationMap[new PdfName("D")] = PdfName.Decode;
			inlineImageEntryAbbreviationMap[new PdfName("DP")] = PdfName.DecodeParms;
			inlineImageEntryAbbreviationMap[new PdfName("F")] = PdfName.Filter;
			inlineImageEntryAbbreviationMap[new PdfName("H")] = PdfName.Height;
			inlineImageEntryAbbreviationMap[new PdfName("IM")] = PdfName.ImageMask;
			inlineImageEntryAbbreviationMap[new PdfName("I")] = PdfName.Interpolate;
			inlineImageEntryAbbreviationMap[new PdfName("W")] = PdfName.Width;
			// Map between value abbreviations allowed in dictionary of inline images for COLORSPACE
			inlineImageColorSpaceAbbreviationMap = new Dictionary<PdfName, PdfName>();
			inlineImageColorSpaceAbbreviationMap[new PdfName("G")] = PdfName.DeviceGray;
			inlineImageColorSpaceAbbreviationMap[new PdfName("RGB")] = PdfName.DeviceRGB;
			inlineImageColorSpaceAbbreviationMap[new PdfName("CMYK")] = PdfName.DeviceCMYK;
			inlineImageColorSpaceAbbreviationMap[new PdfName("I")] = PdfName.Indexed;
			// Map between value abbreviations allowed in dictionary of inline images for FILTER
			inlineImageFilterAbbreviationMap = new Dictionary<PdfName, PdfName>();
			inlineImageFilterAbbreviationMap[new PdfName("AHx")] = PdfName.ASCIIHexDecode;
			inlineImageFilterAbbreviationMap[new PdfName("A85")] = PdfName.ASCII85Decode;
			inlineImageFilterAbbreviationMap[new PdfName("LZW")] = PdfName.LZWDecode;
			inlineImageFilterAbbreviationMap[new PdfName("Fl")] = PdfName.FlateDecode;
			inlineImageFilterAbbreviationMap[new PdfName("RL")] = PdfName.RunLengthDecode;
			inlineImageFilterAbbreviationMap[new PdfName("CCF")] = PdfName.CCITTFaxDecode;
			inlineImageFilterAbbreviationMap[new PdfName("DCT")] = PdfName.DCTDecode;
		}

		/// <summary>Parses an inline image from the provided content parser.</summary>
		/// <remarks>
		/// Parses an inline image from the provided content parser.  The parser must be positioned immediately following the BI operator in the content stream.
		/// The parser will be left with current position immediately following the EI operator that terminates the inline image
		/// </remarks>
		/// <param name="ps">the content parser to use for reading the image.</param>
		/// <param name="colorSpaceDic">a color space dictionary</param>
		/// <returns>the parsed image</returns>
		/// <exception cref="System.IO.IOException">if anything goes wring with the parsing</exception>
		/// <exception cref="InlineImageParseException">if parsing of the inline image failed due to issues specific to inline image processing
		/// 	</exception>
		public static PdfStream Parse(PdfCanvasParser ps, PdfDictionary colorSpaceDic)
		{
			PdfDictionary inlineImageDict = ParseDictionary(ps);
			byte[] samples = ParseSamples(inlineImageDict, colorSpaceDic, ps);
			PdfStream inlineImageAsStreamObject = new PdfStream(samples);
			inlineImageAsStreamObject.PutAll(inlineImageDict);
			return inlineImageAsStreamObject;
		}

		/// <summary>Parses the next inline image dictionary from the parser.</summary>
		/// <remarks>
		/// Parses the next inline image dictionary from the parser.  The parser must be positioned immediately following the BI operator.
		/// The parser will be left with position immediately following the whitespace character that follows the ID operator that ends the inline image dictionary.
		/// </remarks>
		/// <param name="ps">the parser to extract the embedded image information from</param>
		/// <returns>the dictionary for the inline image, with any abbreviations converted to regular image dictionary keys and values
		/// 	</returns>
		/// <exception cref="System.IO.IOException">if the parse fails</exception>
		private static PdfDictionary ParseDictionary(PdfCanvasParser ps)
		{
			// by the time we get to here, we have already parsed the BI operator
			PdfDictionary dict = new PdfDictionary();
			for (PdfObject key = ps.ReadObject(); key != null && !"ID".Equals(key.ToString())
				; key = ps.ReadObject())
			{
				PdfObject value = ps.ReadObject();
				PdfName resolvedKey = inlineImageEntryAbbreviationMap[(PdfName)key];
				if (resolvedKey == null)
				{
					resolvedKey = (PdfName)key;
				}
				dict.Put(resolvedKey, GetAlternateValue(resolvedKey, value));
			}
			int ch = ps.GetTokeniser().Read();
			if (!PdfTokenizer.IsWhitespace(ch))
			{
				throw new InlineImageParsingUtils.InlineImageParseException(PdfException.UnexpectedCharacter1FoundAfterIDInInlineImage
					).SetMessageParams(ch);
			}
			return dict;
		}

		/// <summary>Transforms value abbreviations into their corresponding real value</summary>
		/// <param name="key">the key that the value is for</param>
		/// <param name="value">the value that might be an abbreviation</param>
		/// <returns>if value is an allowed abbreviation for the key, the expanded value for that abbreviation.  Otherwise, value is returned without modification
		/// 	</returns>
		private static PdfObject GetAlternateValue(PdfName key, PdfObject value)
		{
			if (key == PdfName.Filter)
			{
				if (value is PdfName)
				{
					PdfName altValue = inlineImageFilterAbbreviationMap[(PdfName)value];
					if (altValue != null)
					{
						return altValue;
					}
				}
				else
				{
					if (value is PdfArray)
					{
						PdfArray array = ((PdfArray)value);
						PdfArray altArray = new PdfArray();
						int count = array.Size();
						for (int i = 0; i < count; i++)
						{
							altArray.Add(GetAlternateValue(key, array.Get(i)));
						}
						return altArray;
					}
				}
			}
			else
			{
				if (key == PdfName.ColorSpace && value is PdfName)
				{
					PdfName altValue = inlineImageColorSpaceAbbreviationMap[(PdfName)value];
					if (altValue != null)
					{
						return altValue;
					}
				}
			}
			return value;
		}

		/// <param name="colorSpaceName">the name of the color space. If null, a bi-tonal (black and white) color space is assumed.
		/// 	</param>
		/// <returns>the components per pixel for the specified color space</returns>
		private static int GetComponentsPerPixel(PdfName colorSpaceName, PdfDictionary colorSpaceDic
			)
		{
			if (colorSpaceName == null)
			{
				return 1;
			}
			if (colorSpaceName.Equals(PdfName.DeviceGray))
			{
				return 1;
			}
			if (colorSpaceName.Equals(PdfName.DeviceRGB))
			{
				return 3;
			}
			if (colorSpaceName.Equals(PdfName.DeviceCMYK))
			{
				return 4;
			}
			if (colorSpaceDic != null)
			{
				PdfArray colorSpace = colorSpaceDic.GetAsArray(colorSpaceName);
				if (colorSpace != null)
				{
					if (PdfName.Indexed.Equals(colorSpace.GetAsName(0)))
					{
						return 1;
					}
				}
				else
				{
					PdfName tempName = colorSpaceDic.GetAsName(colorSpaceName);
					if (tempName != null)
					{
						return GetComponentsPerPixel(tempName, colorSpaceDic);
					}
				}
			}
			throw new InlineImageParsingUtils.InlineImageParseException(PdfException.UnexpectedColorSpace1
				).SetMessageParams(colorSpaceName);
		}

		/// <summary>Computes the number of unfiltered bytes that each row of the image will contain.
		/// 	</summary>
		/// <remarks>
		/// Computes the number of unfiltered bytes that each row of the image will contain.
		/// If the number of bytes results in a partial terminating byte, this number is rounded up
		/// per the PDF specification
		/// </remarks>
		/// <param name="imageDictionary">the dictionary of the inline image</param>
		/// <returns>the number of bytes per row of the image</returns>
		private static int ComputeBytesPerRow(PdfDictionary imageDictionary, PdfDictionary
			 colorSpaceDic)
		{
			PdfNumber wObj = imageDictionary.GetAsNumber(PdfName.Width);
			PdfNumber bpcObj = imageDictionary.GetAsNumber(PdfName.BitsPerComponent);
			int cpp = GetComponentsPerPixel(imageDictionary.GetAsName(PdfName.ColorSpace), colorSpaceDic
				);
			int w = wObj.IntValue();
			int bpc = bpcObj != null ? bpcObj.IntValue() : 1;
			return (w * bpc * cpp + 7) / 8;
		}

		/// <summary>Parses the samples of the image from the underlying content parser, ignoring all filters.
		/// 	</summary>
		/// <remarks>
		/// Parses the samples of the image from the underlying content parser, ignoring all filters.
		/// The parser must be positioned immediately after the ID operator that ends the inline image's dictionary.
		/// The parser will be left positioned immediately following the EI operator.
		/// This is primarily useful if no filters have been applied.
		/// </remarks>
		/// <param name="imageDictionary">the dictionary of the inline image</param>
		/// <param name="ps">the content parser</param>
		/// <returns>the samples of the image</returns>
		/// <exception cref="System.IO.IOException">if anything bad happens during parsing</exception>
		private static byte[] ParseUnfilteredSamples(PdfDictionary imageDictionary, PdfDictionary
			 colorSpaceDic, PdfCanvasParser ps)
		{
			// special case:  when no filter is specified, we just read the number of bits
			// per component, multiplied by the width and height.
			if (imageDictionary.ContainsKey(PdfName.Filter))
			{
				throw new ArgumentException("Dictionary contains filters");
			}
			PdfNumber h = imageDictionary.GetAsNumber(PdfName.Height);
			int bytesToRead = ComputeBytesPerRow(imageDictionary, colorSpaceDic) * h.IntValue
				();
			byte[] bytes = new byte[bytesToRead];
			PdfTokenizer tokeniser = ps.GetTokeniser();
			int shouldBeWhiteSpace = tokeniser.Read();
			// skip next character (which better be a whitespace character - I suppose we could check for this)
			// from the PDF spec:  Unless the image uses ASCIIHexDecode or ASCII85Decode as one of its filters, the ID operator shall be followed by a single white-space character, and the next character shall be interpreted as the first byte of image data.
			// unfortunately, we've seen some PDFs where there is no space following the ID, so we have to capture this case and handle it
			int startIndex = 0;
			if (!PdfTokenizer.IsWhitespace(shouldBeWhiteSpace) || shouldBeWhiteSpace == 0)
			{
				// tokeniser treats 0 as whitespace, but for our purposes, we shouldn't
				bytes[0] = (byte)shouldBeWhiteSpace;
				startIndex++;
			}
			for (int i = startIndex; i < bytesToRead; i++)
			{
				int ch = tokeniser.Read();
				if (ch == -1)
				{
					throw new InlineImageParsingUtils.InlineImageParseException(PdfException.EndOfContentStreamReachedBeforeEndOfImageData
						);
				}
				bytes[i] = (byte)ch;
			}
			PdfObject ei = ps.ReadObject();
			if (!ei.ToString().Equals("EI"))
			{
				// Some PDF producers seem to add another non-whitespace character after the image data.
				// Let's try to handle that case here.
				PdfObject ei2 = ps.ReadObject();
				if (!ei2.ToString().Equals("EI"))
				{
					throw new InlineImageParsingUtils.InlineImageParseException(PdfException.OperatorEINotFoundAfterEndOfImageData
						);
				}
			}
			return bytes;
		}

		/// <summary>
		/// Parses the samples of the image from the underlying content parser, accounting for filters
		/// The parser must be positioned immediately after the ID operator that ends the inline image's dictionary.
		/// </summary>
		/// <remarks>
		/// Parses the samples of the image from the underlying content parser, accounting for filters
		/// The parser must be positioned immediately after the ID operator that ends the inline image's dictionary.
		/// The parser will be left positioned immediately following the EI operator.
		/// <b>Note:</b>This implementation does not actually apply the filters at this time
		/// </remarks>
		/// <param name="imageDictionary">the dictionary of the inline image</param>
		/// <param name="ps">the content parser</param>
		/// <returns>the samples of the image</returns>
		/// <exception cref="System.IO.IOException">if anything bad happens during parsing</exception>
		private static byte[] ParseSamples(PdfDictionary imageDictionary, PdfDictionary colorSpaceDic
			, PdfCanvasParser ps)
		{
			// by the time we get to here, we have already parsed the ID operator
			if (!imageDictionary.ContainsKey(PdfName.Filter) && ImageColorSpaceIsKnown(imageDictionary
				, colorSpaceDic))
			{
				return ParseUnfilteredSamples(imageDictionary, colorSpaceDic, ps);
			}
			// read all content until we reach an EI operator surrounded by whitespace.
			// The following algorithm has two potential issues: what if the image stream
			// contains <ws>EI<ws> ?
			// Plus, there are some streams that don't have the <ws> before the EI operator
			// it sounds like we would have to actually decode the content stream, which
			// I'd rather avoid right now.
			MemoryStream baos = new MemoryStream();
			MemoryStream accumulated = new MemoryStream();
			int ch;
			int found = 0;
			PdfTokenizer tokeniser = ps.GetTokeniser();
			while ((ch = tokeniser.Read()) != -1)
			{
				if (found == 0 && PdfTokenizer.IsWhitespace(ch))
				{
					found++;
					accumulated.Write(ch);
				}
				else
				{
					if (found == 1 && ch == 'E')
					{
						found++;
						accumulated.Write(ch);
					}
					else
					{
						if (found == 1 && PdfTokenizer.IsWhitespace(ch))
						{
							// this clause is needed if we have a white space character that is part of the image data
							// followed by a whitespace character that precedes the EI operator.  In this case, we need
							// to flush the first whitespace, then treat the current whitespace as the first potential
							// character for the end of stream check.  Note that we don't increment 'found' here.
							baos.Write(accumulated.ToArray());
							accumulated.JReset();
							accumulated.Write(ch);
						}
						else
						{
							if (found == 2 && ch == 'I')
							{
								found++;
								accumulated.Write(ch);
							}
							else
							{
								if (found == 3 && PdfTokenizer.IsWhitespace(ch))
								{
									byte[] tmp = baos.ToArray();
									if (InlineImageStreamBytesAreComplete(tmp, imageDictionary))
									{
										return tmp;
									}
									baos.Write(accumulated.ToArray());
									accumulated.JReset();
									baos.Write(ch);
									found = 0;
								}
								else
								{
									baos.Write(accumulated.ToArray());
									accumulated.JReset();
									baos.Write(ch);
									found = 0;
								}
							}
						}
					}
				}
			}
			throw new InlineImageParsingUtils.InlineImageParseException(PdfException.CannotFindImageDataOrEI
				);
		}

		private static bool ImageColorSpaceIsKnown(PdfDictionary imageDictionary, PdfDictionary
			 colorSpaceDic)
		{
			PdfName cs = imageDictionary.GetAsName(PdfName.ColorSpace);
			if (cs == null || cs.Equals(PdfName.DeviceGray) || cs.Equals(PdfName.DeviceRGB) ||
				 cs.Equals(PdfName.DeviceCMYK))
			{
				return true;
			}
			return colorSpaceDic != null && colorSpaceDic.ContainsKey(cs);
		}

		/// <summary>This method acts like a check that bytes that were parsed are really all image bytes.
		/// 	</summary>
		/// <remarks>
		/// This method acts like a check that bytes that were parsed are really all image bytes. If it's true,
		/// then decoding will succeed, but if not all image bytes were read and "<ws>EI<ws>" bytes were just a part of the image,
		/// then decoding should fail.
		/// Not the best solution, but probably there is no better and more reliable way to check this.
		/// <p>
		/// Drawbacks: slow; images with DCTDecode, JBIG2Decode and JPXDecode filters couldn't be checked as iText doesn't
		/// support these filters; what if decoding will succeed eventhough it's not all bytes?; also I'm not sure that all
		/// filters throw an exception in case data is corrupted (For example, FlateDecodeFilter seems not to throw an exception).
		/// </remarks>
		private static bool InlineImageStreamBytesAreComplete(byte[] samples, PdfDictionary
			 imageDictionary)
		{
			try
			{
				IDictionary<PdfName, IFilterHandler> filters = new Dictionary<PdfName, IFilterHandler
					>(FilterHandlers.GetDefaultFilterHandlers());
				DoNothingFilter stubfilter = new DoNothingFilter();
				filters[PdfName.DCTDecode] = stubfilter;
				filters[PdfName.JBIG2Decode] = stubfilter;
				filters[PdfName.JPXDecode] = stubfilter;
				PdfReader.DecodeBytes(samples, imageDictionary, filters);
			}
			catch (Exception)
			{
				return false;
			}
			return true;
		}
	}
}