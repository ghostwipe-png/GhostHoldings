using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using GhostHoldings.Application.Interfaces; // 🔥 Imports the interface from the Application layer

namespace GhostHoldings.Infrastructure.Services;

// 🔥 REMOVED: The interface definition is now ONLY in the Application layer.
// This file now ONLY contains the implementation.

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration configuration)
    {
        var account = new Account(
            configuration["Cloudinary__CloudName"],
            configuration["Cloudinary__ApiKey"],
            configuration["Cloudinary__ApiSecret"]
        );
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadBase64ImageAsync(string base64String, string publicIdPrefix)
    {
        // 1. Clean the Base64 string (remove "data:image/jpeg;base64," if present)
        var base64Data = Regex.Replace(base64String, @"^data:image/[a-z]+;base64,", "");
        var imageBytes = Convert.FromBase64String(base64Data);

        // 2. Create upload parameters
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription($"{publicIdPrefix}_{Guid.NewGuid()}.jpg", new MemoryStream(imageBytes)),
            Folder = "GhostHoldings/KYC", 
            Transformation = new Transformation()
                .Width(800).Height(800).Crop("limit") 
                .Quality("auto") 
                .FetchFormat("auto") 
        };

        // 3. Upload to Cloudinary
        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            throw new Exception($"Cloudinary Upload Error: {uploadResult.Error.Message}");
        }

        // 4. Return the secure URL
        return uploadResult.SecureUrl.ToString();
    }
}