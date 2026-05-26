namespace BackEncordados.Common.Service.WhatsApp;

/// <summary>
/// Define el contrato para el envío de mensajes de WhatsApp a través de la WhatsApp Cloud API.
/// </summary>
/// <remarks>
/// <para>Proporciona métodos para enviar mensajes de texto libre y plantillas aprobadas por Meta,
/// así como notificaciones predefinidas para eventos del sistema (línea completada, cancelada, pedido cancelado).</para>
///
/// <para>Todos los métodos retornan <c>Task&lt;bool&gt;</c> — <c>true</c> si el mensaje se envió
/// correctamente, <c>false</c> si el servicio está deshabilitado, faltan datos o hubo un error.</para>
///
/// <para>Requiere las siguientes configuraciones en <c>AppConfig.Current</c>:</para>
/// <list type="bullet">
///   <item><description><c>WhatsAppEnabled</c> — habilita/deshabilita el servicio.</description></item>
///   <item><description><c>WhatsAppPhoneNumberId</c> — ID del número de negocio en Meta.</description></item>
///   <item><description><c>WhatsAppApiVersion</c> — versión de la Graph API (default: v21.0).</description></item>
/// </list>
/// </remarks>
public interface IWhatsAppService
{
    /// <summary>
    /// Envía un mensaje de texto libre a un número de teléfono.
    /// </summary>
    /// <param name="phoneNumber">Número de teléfono del destinatario (formato internacional).</param>
    /// <param name="message">Texto del mensaje a enviar.</param>
    /// <returns><c>true</c> si el mensaje se envió correctamente.</returns>
    Task<bool> SendMessageAsync(string phoneNumber, string message);

    /// <summary>
    /// Envía una plantilla de mensaje aprobada por Meta, con parámetros opcionales.
    /// </summary>
    /// <remarks>
    /// <para>Las plantillas deben estar aprobadas previamente en Meta Business Manager.
    /// El idioma de la plantilla se fija como <c>"es_ES"</c> (español de España).</para>
    /// </remarks>
    /// <param name="phoneNumber">Número de teléfono del destinatario.</param>
    /// <param name="templateName">Nombre de la plantilla en Meta Business Manager.</param>
    /// <param name="parameters">Parámetros para reemplazar variables en la plantilla (opcional).</param>
    /// <returns><c>true</c> si la plantilla se envió correctamente.</returns>
    Task<bool> SendTemplateMessageAsync(string phoneNumber, string templateName, Dictionary<string, string>? parameters = null);

    /// <summary>Envía notificación de que una línea de encordado ha sido completada.</summary>
    /// <param name="phoneNumber">Número del destinatario.</param>
    /// <param name="playerName">Nombre del jugador.</param>
    /// <param name="raquetModel">Modelo de la raqueta.</param>
    /// <param name="pedidoId">ID del pedido.</param>
    /// <returns><c>true</c> si se envió correctamente.</returns>
    Task<bool> SendLineaCompletedMessageAsync(string phoneNumber, string playerName, string raquetModel, string pedidoId);

    /// <summary>Envía notificación de cancelación de una línea de encordado.</summary>
    /// <param name="phoneNumber">Número del destinatario.</param>
    /// <param name="playerName">Nombre del jugador.</param>
    /// <param name="raquetModel">Modelo de la raqueta.</param>
    /// <param name="pedidoId">ID del pedido.</param>
    /// <returns><c>true</c> si se envió correctamente.</returns>
    Task<bool> SendLineaCanceledMessageAsync(string phoneNumber, string playerName, string raquetModel, string pedidoId);

    /// <summary>Envía notificación de cancelación de un pedido completo.</summary>
    /// <param name="phoneNumber">Número del destinatario.</param>
    /// <param name="playerName">Nombre del jugador.</param>
    /// <param name="pedidoId">ID del pedido cancelado.</param>
    /// <param name="lineasCount">Cantidad de líneas afectadas por la cancelación.</param>
    /// <returns><c>true</c> si se envió correctamente.</returns>
    Task<bool> SendPedidoCanceledMessageAsync(string phoneNumber, string playerName, string pedidoId, int lineasCount);
}