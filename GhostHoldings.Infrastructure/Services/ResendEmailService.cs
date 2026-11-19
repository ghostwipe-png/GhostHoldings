using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GhostHoldings.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GhostHoldings.Infrastructure.Services;

public class ResendEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly string _fromEmail;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(HttpClient httpClient, IConfiguration configuration, ILogger<ResendEmailService> logger)
    {
        _httpClient = httpClient;
        _fromEmail = configuration["Resend:FromEmail"] ?? "GhostHoldings <onboarding@resend.dev>";
        _logger = logger;

        var apiKey = configuration["Resend:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Resend API Key is missing in configuration.");
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var payload = new
            {
                from = _fromEmail,
                to = new[] { toEmail },
                subject = subject,
                html = htmlBody
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.resend.com/emails", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Resend API failed: {StatusCode} - {Error}", response.StatusCode, error);
                throw new Exception($"Email sending failed: {error}");
            }

            _logger.LogInformation("Email successfully sent to {Email} with subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending email to {Email}", toEmail);
            throw;
        }
    }
}