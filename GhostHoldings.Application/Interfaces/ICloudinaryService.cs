namespace GhostHoldings.Application.Interfaces;

public interface ICloudinaryService
{
    Task<string> UploadBase64ImageAsync(string base64String, string publicIdPrefix);
}