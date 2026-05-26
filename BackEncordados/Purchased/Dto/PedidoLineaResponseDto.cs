using BackEncordados.Purchased.Model;

namespace BackEncordados.Purchased.Dto;

/// <summary>
/// DTO de respuesta con la información completa de una línea de pedido.
/// </summary>
/// <remarks>
/// <para>Incluye todos los datos de la raqueta encordada: modelo, nudos, fecha, logotipo, color,
/// estado actual y configuración detallada de cuerdas (vertical y horizontal con tensiones y pre-stretch).</para>
/// <para>Se utiliza dentro de <see cref="PurchasedResponseDto"/> como parte de la lista <c>Lineas</c>.</para>
/// </remarks>
/// <param name="Id">Identificador ULID de la línea de pedido.</param>
/// <param name="RaquetModel">Modelo de la raqueta.</param>
/// <param name="Nudos">Número de nudos del encordado.</param>
/// <param name="DateString">Fecha programada o realizada del encordado.</param>
/// <param name="Logotype">Indica si la raqueta lleva logotipo personalizado.</param>
/// <param name="Color">Color del encordado.</param>
/// <param name="Status">Estado actual de la línea (Pendiente, EnProceso, Completado, etc.).</param>
/// <param name="StringSetup">Configuración de tensiones y tipos de cuerda.</param>
public record PedidoLineaResponseDto(
    Ulid Id,
    string RaquetModel,
    byte Nudos,
    DateTime DateString,
    bool Logotype,
    string Color,
    Status Status,
    StringSetup StringSetup
);