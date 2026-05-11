namespace WarehouseManagement.Application.Contracts;

public interface IBarcodeQrService
{
    byte[] GenerateQrPng(string payload, int pixelsPerModule = 8);

    /// <summary>ZIP archive containing one PNG QR per item (fileName without extension).</summary>
    byte[] GenerateQrLabelsZip(IReadOnlyList<(string FileName, string Payload)> items);
}
