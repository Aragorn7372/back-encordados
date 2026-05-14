using System.Threading.Channels;

namespace BackEncordados.Common.Service.Email;


public class EmailBackgroundService(
    Channel<EmailMessage> emailChannel,
    IServiceProvider serviceProvider,
    ILogger<EmailBackgroundService> logger
) : BackgroundService {

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        
        logger.LogInformation("Servicio de email en segundo plano iniciado");

        await foreach (var emailMessage in emailChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                logger.LogInformation("Procesando email de la cola para: {To}", emailMessage.To);

                await emailService.SendEmailAsync(emailMessage);

                logger.LogInformation("Email procesado exitosamente para: {To}", emailMessage.To);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error procesando email para: {To}", emailMessage.To);
            }
        }

        logger.LogInformation("Servicio de email en segundo plano detenido");
    }
}