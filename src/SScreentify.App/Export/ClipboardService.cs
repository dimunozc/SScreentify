using System.Runtime.InteropServices;
using System.Windows;
using SScreentify.App.Imaging;
using SScreentify.Core.Imaging;
using SScreentify.Core.Settings;

namespace SScreentify.App.Export;

public enum ClipboardResult { Ok, Busy, Failed }

public static class ClipboardService
{
    private const int ClipboardCantOpen = unchecked((int)0x800401D0);
    // WPF already retries OleSetClipboard internally; keep the outer retry short so the UI never stalls for seconds.
    private const int Attempts = 2;

    /// <summary>Offers PNG (chats, modern apps) and a bitmap (CF_DIB, older apps). Retries while another app holds the clipboard.</summary>
    /// <param name="setter">Places the data; defaults to the system clipboard (flushed so it survives this app).</param>
    public static ClipboardResult Copy(PixelBuffer image, Action<IDataObject>? setter = null, TimeSpan? retryDelay = null)
    {
        var data = new DataObject();
        data.SetData("PNG", new MemoryStream(ImageEncoder.Encode(image, ImageFormat.Png, 90)), autoConvert: false);
        data.SetImage(image.ToBitmapSource());

        setter ??= d => Clipboard.SetDataObject(d, copy: true);
        var delay = retryDelay ?? TimeSpan.FromMilliseconds(100);
        for (var attempt = 1; attempt <= Attempts; attempt++)
        {
            try
            {
                setter(data);
                return ClipboardResult.Ok;
            }
            catch (COMException ex) when (ex.HResult == ClipboardCantOpen)
            {
                if (attempt < Attempts && delay > TimeSpan.Zero) Thread.Sleep(delay);
            }
            catch (ExternalException)
            {
                return ClipboardResult.Failed;
            }
        }
        return ClipboardResult.Busy;
    }
}
