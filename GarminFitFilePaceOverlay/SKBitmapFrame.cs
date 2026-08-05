using FFMpegCore.Pipes;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GarminFitFilePaceOverlay
{
    internal class SKBitmapFrame : IVideoFrame, IDisposable
    {
        public int Width => Source.Width;
        public int Height => Source.Height;
        public string Format => "bgra";

        private SKBitmap Source;

        public SKBitmapFrame(SKBitmap bitmap)
        {
            Source = bitmap;
        }

        public void Dispose()
        {
            Source.Dispose();
        }

        public void Serialize(Stream pipe)
        {
            pipe.Write(Source.Bytes, 0, Source.Bytes.Length);
        }

        public Task SerializeAsync(Stream pipe, CancellationToken token)
        {
            return pipe.WriteAsync(Source.Bytes, 0, Source.Bytes.Length, token);
        }
    }
}
