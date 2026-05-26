using System.Text;
using System.Text.Json;
using BackEncordados.Infraestructure;
using Microsoft.Extensions.Logging;

namespace BackEncordados.Common.Service.WhatsApp;

/// <summary>
/// Implementación de <see cref="IWhatsAppService"/> que envía mensajes a través
/// de la WhatsApp Cloud API (Meta Graph API).
/// </summary>
/// <remarks>
/// <para>Utiliza la API de Meta para enviar mensajes de WhatsApp desde un número
/// de negocio registrado en Meta Business Platform.</para>
///
/// <para><b>Configuración requerida (via <see cref="AppConfig"/>):</b></para>
/// <list type="table">
///   <listheader>
///     <term>Propiedad</term>
///     <description>Descripción</description>
///     <description>Default</description>
///   </listheader>
///   <item>
///     <term><c>WhatsAppEnabled</c></term>
///     <description>Habilita/deshabilita el envío de mensajes</description>
///     <description>—</description>
///   </item>
///   <item>
///     <term><c>WhatsAppApiVersion</c></term>
///     <description>Versión de la Graph API</description>
///     <description><c>v21.0</c></description>
///   </item>
///   <item>
///     <term><c>WhatsAppPhoneNumberId</c></term>
///     <description>ID del número de teléfono de negocio en Meta</description>
///     <description>—</description>
///   </item>
/// </list>
///
/// <para><b>Comportamiento:</b></para>
/// <list type="bullet">
///   <item><description>Si <c>WhatsAppEnabled</c> es <c>false</c>, todos los métodos retornan <c>false</c> sin enviar.</description></item>
///   <item><description>Si el número de teléfono está vacío, retorna <c>false</c> con warning.</description></item>
///   <item><description>Los mensajes de texto libre se envían como <c>type: "text"</c>.</description></item>
///   <item><description>Las plantillas se envían con idioma <c>"es_ES"</c> (español).</description></item>
///   <item><description>Los errores HTTP y de red se capturan, loggean y retornan <c>false</c> (fail-safe).</description></item>
/// </list>
/// </remarks>
/// <param name="logger">Logger para seguimiento de envíos y errores.</param>
/// <param name="httpClientFactory">Fábrica de HttpClient para named client <c>"WhatsApp"</c> (con token de acceso configurado en DI).</param>
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

    /// <summary>
    /// Envía un mensaje de texto libre a un número de teléfono vía WhatsApp Cloud API.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Verifica si el servicio está habilitado (<see cref="IsEnabled"/>).</description></item>
    ///   <item><description>Valida que el número de teléfono no esté vacío.</description></item>
    ///   <item><description>Normaliza el número (<see cref="NormalizePhoneNumber"/>).</description></item>
    ///   <item><description>Construye payload <c>{ messaging_product: "whatsapp", type: "text", text: { body } }</c>.</description></item>
    ///   <item><description>Envía la petición HTTP mediante <see cref="SendWhatsAppRequestAsync"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="phoneNumber">Número de teléfono del destinatario.</param>
    /// <param name="message">Texto del mensaje.</param>
    /// <returns><c>true</c> si el mensaje se envió correctamente.</returns>
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

    /// <summary>
    /// Envía una plantilla de mensaje aprobada por Meta con parámetros opcionales.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Verifica si el servicio está habilitado.</description></item>
    ///   <item><description>Valida y normaliza el número de teléfono.</description></item>
    ///   <item><description>Construye payload <c>type: "template"</c> con <c>name</c>, <c>language.code: "es_ES"</c>
    ///   y componentes de tipo <c>body</c> con los parámetros proporcionados.</description></item>
    ///   <item><description>Envía la petición HTTP.</description></item>
    /// </list>
    /// <para>Si no se proporcionan parámetros, el componente se envía como <c>null</c>.</para>
    /// </remarks>
    /// <param name="phoneNumber">Número de teléfono del destinatario.</param>
    /// <param name="templateName">Nombre de la plantilla en Meta Business Manager.</param>
    /// <param name="parameters">Parámetros para reemplazar variables <c>{{1}}</c>, <c>{{2}}</c>, etc. en la plantilla.</param>
    /// <returns><c>true</c> si la plantilla se envió correctamente.</returns>
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

    /// <summary>Envía notificación de línea de encordado completada.</summary>
    /// <param name="phoneNumber">Número del destinatario.</param>
    /// <param name="playerName">Nombre del jugador.</param>
    /// <param name="raquetModel">Modelo de la raqueta.</param>
    /// <param name="pedidoId">ID del pedido.</param>
    /// <returns><c>true</c> si se envió correctamente.</returns>
    public async Task<bool> SendLineaCompletedMessageAsync(string phoneNumber, string playerName, string raquetModel, string pedidoId)
    {
        var message = $"Hola {playerName}, tu raqueta {raquetModel} esta lista. Pedido #{pedidoId}";
        return await SendMessageAsync(phoneNumber, message);
    }

    /// <summary>Envía notificación de cancelación de una línea de encordado.</summary>
    /// <param name="phoneNumber">Número del destinatario.</param>
    /// <param name="playerName">Nombre del jugador.</param>
    /// <param name="raquetModel">Modelo de la raqueta.</param>
    /// <param name="pedidoId">ID del pedido.</param>
    /// <returns><c>true</c> si se envió correctamente.</returns>
    public async Task<bool> SendLineaCanceledMessageAsync(string phoneNumber, string playerName, string raquetModel, string pedidoId)
    {
        var message = $"Hola {playerName}, tu raqueta {raquetModel} ha sido cancelada. Pedido #{pedidoId}";
        return await SendMessageAsync(phoneNumber, message);
    }

    /// <summary>Envía notificación de cancelación de un pedido completo con detalle de líneas afectadas.</summary>
    /// <param name="phoneNumber">Número del destinatario.</param>
    /// <param name="playerName">Nombre del jugador.</param>
    /// <param name="pedidoId">ID del pedido cancelado.</param>
    /// <param name="lineasCount">Cantidad de líneas afectadas.</param>
    /// <returns><c>true</c> si se envió correctamente.</returns>
    public async Task<bool> SendPedidoCanceledMessageAsync(string phoneNumber, string playerName, string pedidoId, int lineasCount)
    {
        var message = $"Hola {playerName}, tu pedido #{pedidoId} ha sido cancelado. {lineasCount} lineas afectadas.";
        return await SendMessageAsync(phoneNumber, message);
    }

    /// <summary>
    /// Indica si el servicio de WhatsApp está habilitado en la configuración.
    /// </summary>
    /// <returns><c>true</c> si <c>AppConfig.Current.WhatsAppEnabled</c> es <c>true</c>.</returns>
    private bool IsEnabled()
    {
        return AppConfig.Current.WhatsAppEnabled;
    }

    /// <summary>
    /// Normaliza el número de teléfono eliminando espacios en blanco al inicio/final.
    /// </summary>
    /// <param name="phoneNumber">Número de teléfono a normalizar.</param>
    /// <returns>Número normalizado.</returns>
    private string NormalizePhoneNumber(string phoneNumber)
    {
        return phoneNumber.Trim();
    }

    /// <summary>
    /// Ejecuta la petición HTTP POST a la WhatsApp Cloud API.
    /// </summary>
    /// <remarks>
    /// <para>Construye la URL como <c>https://graph.facebook.com/{apiVersion}/{phoneNumberId}/messages</c>.
    /// La versión de API se obtiene de <c>AppConfig.Current.WhatsAppApiVersion</c> (default: <c>v21.0</c>).</para>
    /// <para>El payload se serializa a JSON con <see cref="JsonSerializer"/> y se envía
    /// mediante el named client <c>"WhatsApp"</c> de <see cref="IHttpClientFactory"/>
    /// (que debe tener configurado el token de acceso Bearer en DI).</para>
    /// <para>Cualquier excepción de red o respuesta HTTP no exitosa retorna <c>false</c>
    /// después de registrar el error en log.</para>
    /// </remarks>
    /// <param name="payload">Objeto anónimo con la estructura del mensaje WhatsApp.</param>
    /// <returns><c>true</c> si la API respondió con status 2xx, <c>false</c> en caso contrario.</returns>
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