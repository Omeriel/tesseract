﻿
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Tesseract
{
	/// <summary>
	/// Description of BitmapToPixConverter.
	/// </summary>
	public class BitmapToPixConverter
    {
        public BitmapToPixConverter()
		{
		}

        public Pix Convert(Bitmap img)
        {
            var pixDepth = GetPixDepth(img.PixelFormat);
            var pix = Pix.Create(img.Width, img.Height, pixDepth);
            
            BitmapData imgData = null;
            PixData pixData = null;
            try {
                // TODO: Set X and Y resolution

                if ((img.PixelFormat & PixelFormat.Indexed) == PixelFormat.Indexed) {
                    CopyColormap(img, pix);
                }

                // transfer data
                imgData = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, img.PixelFormat);
                pixData = pix.GetData();

                if (imgData.PixelFormat == PixelFormat.Format32bppArgb) {
                    TransferDataFormat32bppArgb(imgData, pixData);
                } else if (imgData.PixelFormat == PixelFormat.Format24bppRgb) {
                    TransferDataFormat24bppRgb(imgData, pixData);
                } else if (imgData.PixelFormat == PixelFormat.Format8bppIndexed) {
                    TransferDataFormat8bppIndexed(imgData, pixData);
                } else if (imgData.PixelFormat == PixelFormat.Format1bppIndexed) {
                    TransferDataFormat1bppIndexed(imgData, pixData);
                }
                return pix;
            } catch (Exception) {
                pix.Dispose();
                throw;
            } finally {
                if (imgData != null) {
                    img.UnlockBits(imgData);
                }
            }
        }


        private void CopyColormap(Bitmap img, Pix pix)
        {
            var imgPalette = img.Palette;
            var imgPaletteEntries = imgPalette.Entries;
            var pixColormap = PixColormap.Create(pix.Depth);
            try {
                for (int i = 0; i < imgPaletteEntries.Length; i++) {
                    if (!pixColormap.AddColor((PixColor)imgPaletteEntries[i])) {
                        throw new InvalidOperationException(String.Format("Failed to add colormap entry {0}.", i));
                    }
                }
                pix.Colormap = pixColormap;
            } catch (Exception) {
                pixColormap.Dispose();
                throw;
            }
        }

        private int GetPixDepth(PixelFormat pixelFormat)
        {
            switch (pixelFormat) {
                case PixelFormat.Format8bppIndexed:
                    return 8;
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format24bppRgb:
                    return 32;
                default:
                    throw new InvalidOperationException(String.Format("Source bitmap's pixel format {0} is not supported.", pixelFormat));
            }
        }

        private unsafe void TransferDataFormat32bppArgb(BitmapData imgData, PixData pixData)
        {
            var imgFormat = imgData.PixelFormat;
            var height = imgData.Height;
            var width = imgData.Width;
           
            for (int y = 0; y < height; y++) {
                byte* imgLine = (byte*)imgData.Scan0 + (y * imgData.Stride);
                uint* pixLine = (uint*)pixData.Data + (y * pixData.WordsPerLine);

                for (int x = 0; x < width; x++) {
                    byte* pixelPtr = imgLine + (x << 2);
                    byte blue = *pixelPtr;
                    byte green = *(pixelPtr + 1);
                    byte red = *(pixelPtr + 2);
                    byte alpha = *(pixelPtr + 3);
                    PixData.SetDataFourByte(pixLine, x, BitmapHelper.EncodeAsRGBA(red, green, blue, alpha));
                }
            }
        }

        private unsafe void TransferDataFormat24bppRgb(BitmapData imgData, PixData pixData)
        {
            var imgFormat = imgData.PixelFormat;
            var height = imgData.Height;
            var width = imgData.Width;

            for (int y = 0; y < height; y++) {
                byte* imgLine = (byte*)imgData.Scan0 + (y * imgData.Stride);
                uint* pixLine = (uint*)pixData.Data + (y * pixData.WordsPerLine);

                for (int x = 0; x < width; x++) {
                    byte* pixelPtr = imgLine + x*3;
                    byte blue = pixelPtr[0];
                    byte green = pixelPtr[1];
                    byte red = pixelPtr[2];
                    PixData.SetDataFourByte(pixLine, x, BitmapHelper.EncodeAsRGBA(red, green, blue, 255));
                }
            }
        }

        private unsafe void TransferDataFormat8bppIndexed(BitmapData imgData, PixData pixData)
        {
            var height = imgData.Height;
            var width = imgData.Width;

            for (int y = 0; y < height; y++) {
                byte* imgLine = (byte*)imgData.Scan0 + (y * imgData.Stride);
                uint* pixLine = (uint*)pixData.Data + (y * pixData.WordsPerLine);

                for (int x = 0; x < width; x++) {
                    byte pixelVal = *(imgLine + x);
                    PixData.SetDataByte(pixLine, x, pixelVal);
                }
            }
        }

        private unsafe void TransferDataFormat1bppIndexed(BitmapData imgData, PixData pixData)
        {
            var height = imgData.Height;
            var width = imgData.Width;
            for (int y = 0; y < height; y++) {
                byte* imgLine = (byte*)imgData.Scan0 + (y * imgData.Stride);
                uint* pixLine = (uint*)pixData.Data + (y * pixData.WordsPerLine);

                for (int x = 0; x < width; x++) {
                    byte pixelVal = BitmapHelper.GetDataBit(imgLine, x);
                    PixData.SetDataBit(pixLine, x, pixelVal);
                }
            }
        }
	}
}
