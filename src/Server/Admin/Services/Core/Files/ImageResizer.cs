// Copyright (c) DevInstance LLC. All rights reserved.

using SkiaSharp;
using System;
using System.IO;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Core.Files;

/// <summary>
/// Shared SkiaSharp image resizing. Consolidates the decode/scale/encode sequence that
/// services otherwise copy-paste (UserProfileService is the first caller).
///
/// The encode format is a parameter rather than fixed: profile pictures are stored as JPEG
/// and their ProfilePictureContentType column has to agree with the bytes, so a helper that
/// only emitted PNG would silently change what is persisted.
/// </summary>
public static class ImageResizer
{
    /// <summary>
    /// Scales <paramref name="imageData"/> to fit inside <paramref name="maxWidth"/> x
    /// <paramref name="maxHeight"/>, preserving aspect ratio, and re-encodes it as
    /// <paramref name="format"/>. Images already smaller than the box are re-encoded but
    /// never enlarged. <paramref name="quality"/> is ignored by formats that have no quality
    /// setting (PNG).
    /// </summary>
    /// <returns>Encoded bytes, or null when the data could not be decoded as an image.</returns>
    public static byte[]? Resize(byte[] imageData, int maxWidth, int maxHeight,
                                 SKEncodedImageFormat format, int quality = 90)
    {
        using var original = SKBitmap.Decode(imageData);
        if (original == null)
            return null;

        var ratio = Math.Min(
            Math.Min((double)maxWidth / original.Width, (double)maxHeight / original.Height),
            1.0);

        // Round rather than truncate, and never below 1 — a very wide or very tall source
        // otherwise scales to a zero-length side, which SKImageInfo rejects.
        var targetWidth = Math.Max(1, (int)Math.Round(original.Width * ratio));
        var targetHeight = Math.Max(1, (int)Math.Round(original.Height * ratio));

        using var resized = original.Resize(
            new SKImageInfo(targetWidth, targetHeight),
            new SKSamplingOptions(SKCubicResampler.Mitchell));
        if (resized == null)
            return null;

        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(format, quality);
        return data.ToArray();
    }

    /// <summary>Stream overload of <see cref="Resize(byte[], int, int, SKEncodedImageFormat, int)"/>.</summary>
    public static byte[]? Resize(Stream imageStream, int maxWidth, int maxHeight,
                                 SKEncodedImageFormat format, int quality = 90)
    {
        using var buffer = new MemoryStream();
        imageStream.CopyTo(buffer);
        return Resize(buffer.ToArray(), maxWidth, maxHeight, format, quality);
    }

    /// <summary>PNG convenience overload — for callers with no stored content type to keep in step.</summary>
    public static byte[]? ResizeToPng(byte[] imageData, int maxWidth, int maxHeight, int quality = 90)
        => Resize(imageData, maxWidth, maxHeight, SKEncodedImageFormat.Png, quality);

    /// <summary>Stream overload of <see cref="ResizeToPng(byte[], int, int, int)"/>.</summary>
    public static byte[]? ResizeToPng(Stream imageStream, int maxWidth, int maxHeight, int quality = 90)
        => Resize(imageStream, maxWidth, maxHeight, SKEncodedImageFormat.Png, quality);
}
