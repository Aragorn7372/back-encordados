using System.Text;
using System.Text.Json;
using BackEncordados.Infraestructure;
using Microsoft.Extensions.Logging;

namespace BackEncordados.Common.Service.WhatsApp;

public class WhatsAppService : IWhatsAppService
{
    private readonly ILogger<WhatsAppService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public WhatsAppService(
        ILogger<WhatsAppService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> SendMessageAsync(string phoneNumber, string message)
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("WhatsApp service is disabled. Message not sent.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            _logger.LogWarning("Phone number is empty. Message not sent.");
            return false;
        }

        phoneNumber = NormalizePhoneNumber(phoneNumber);

        var payload = new
        {
            messaging_product = "whatsapp",
            to = phoneNumber,
            type = "text",
            text = new { body = message }
        };

        return await SendWhatsAppRequestAsync(payload);
    }

    public async Task<bool> SendTemplateMessageAsync(string phoneNumber, string templateName, Dictionary<string, string>? parameters = null)
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("WhatsApp service is disabled. Template message not sent.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            _logger.LogWarning("Phone number is empty. Template message not sent.");
            return false;
        }

        phoneNumber = NormalizePhoneNumber(phoneNumber);

        var components = new List<object>();
        
        if (parameters != null && parameters.Any())
        {
            foreach (var param in parameters)
            {
                components.Add(new
                {
                    type = "body",
                    parameters = new[]
                    {
                        new { type = "text", text = param.Value }
                    }
                });
            }
        }

        var payload = new
        {
            messaging_product = "whatsapp",
            to = phoneNumber,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = "es_ES" },
                components = components.Any() ? components : null
            }
        };

        return await SendWhatsAppRequestAsync(payload);
    }

    public async Task<bool> SendLineaCompletedMessageAsync(string phoneNumber, string playerName, string raquetModel, string pedidoId)
    {
        var message = $"Hola {playerName}, tu raqueta {raquetModel} esta lista. Pedido #{pedidoId}";
        return await SendMessageAsync(phoneNumber, message);
    }

    public async Task<bool> SendLineaCanceledMessageAsync(string phoneNumber, string playerName, string raquetModel, string pedidoId)
    {
        var message = $"Hola {playerName}, tu raqueta {raquetModel} ha sido cancelada. Pedido #{pedidoId}";
        return await SendMessageAsync(phoneNumber, message);
    }

    public async Task<bool> SendPedidoCanceledMessageAsync(string phoneNumber, string playerName, string pedidoId, int lineasCount)
    {
        var message = $"Hola {playerName}, tu pedido #{pedidoId} ha sido cancelado. {lineasCount} lineas afectadas.";
        return await SendMessageAsync(phoneNumber, message);
    }

    private bool IsEnabled()
    {
        return AppConfig.Current.WhatsAppEnabled;
    }

    private string NormalizePhoneNumber(string phoneNumber)
    {
        return phoneNumber.Trim();
    }

    private async Task<bool> SendWhatsAppRequestAsync(object payload)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("WhatsApp");
            
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var apiVersion = string.IsNullOrEmpty(AppConfig.Current.WhatsAppApiVersion) ? "v21.0" : AppConfig.Current.WhatsAppApiVersion;
            var phoneNumberId = AppConfig.Current.WhatsAppPhoneNumberId;

            var url = $"https://graph.facebook.com/{apiVersion}/{phoneNumberId}/messages";

            _logger.LogInformation("Sending WhatsApp message to URL: {Url}", url);

            var response = await client.PostAsync(url, content);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("WhatsApp message sent successfully");
                return true;
            }

            _logger.LogError("Failed to send WhatsApp message. Status: {Status}, Response: {Response}", 
                response.StatusCode, responseBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WhatsApp message");
            return false;
        }
    }
}