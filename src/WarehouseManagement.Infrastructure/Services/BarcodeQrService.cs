using System.IO.Compression;
using QRCoder;
using WarehouseManagement.Application.Contracts;

namespace WarehouseManagement.Infrastructure.Services;

public class BarcodeQrService : IBarcodeQrService
{
    public byte[] GenerateQrPng(string payload, int pixelsPerModule = 8)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qr = new PngByteQRCode(data);
        return qr.GetGraphic(pixelsPerModule);
    }

    public byte[] GenerateQrLabelsZip(IReadOnlyList<(string FileName, string Payload)> items)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            foreach (var (fileName, payload) in items)
            {
                var safeName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
                var entry = zip.CreateEntry($"{safeName}.png", CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                var png = GenerateQrPng(payload, 6);
                entryStream.Write(png, 0, png.Length);
            }
        }

        return ms.ToArray();
    }
}
