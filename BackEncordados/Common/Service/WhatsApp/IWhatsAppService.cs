namespace BackEncordados.Common.Service.WhatsApp;

public interface IWhatsAppService
{
    Task<bool> SendMessageAsync(string phoneNumber, string message);
    Task<bool> SendTemplateMessageAsync(string phoneNumber, string templateName, Dictionary<string, string>? parameters = null);
    Task<bool> SendLineaCompletedMessageAsync(string phoneNumber, string playerName, string raquetModel, string pedidoId);
    Task<bool> SendLineaCanceledMessageAsync(string phoneNumber, string playerName, string raquetModel, string pedidoId);
    Task<bool> SendPedidoCanceledMessageAsync(string phoneNumber, string playerName, string pedidoId, int lineasCount);
}