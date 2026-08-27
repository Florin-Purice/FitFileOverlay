using FFMpegCore.Pipes;
using SkiaSharp;
using System.IO;

namespace FitFileOverlay.Models;

public class SKBitmapFrame(SKBitmap bitmap) : IVideoFrame, IDisposable
{
    public int Width => bitmap.Width;
    public int Height => bitmap.Height;
    public string Format => "bgra";

    public void Dispose()
    {
        bitmap.Dispose();
    }

    public void Serialize(Stream pipe)
    {
        pipe.Write(bitmap.Bytes, 0, bitmap.Bytes.Length);
    }

    public Task SerializeAsync(Stream pipe, CancellationToken token)
    {
        return pipe.WriteAsync(bitmap.Bytes, 0, bitmap.Bytes.Length, token);
    }
}
