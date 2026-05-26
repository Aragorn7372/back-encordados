using BackEncordados.Common.Database.Helpers;

namespace BackEncordados.Purchased.Model;

/// <summary>
/// Entidad que representa una línea individual de pedido (una raqueta a encordar).
/// </summary>
/// <remarks>
/// <para>Cada <see cref="PedidoLinea"/> pertenece a un <see cref="Pedidos"/> y contiene
/// la configuración específica de encordado para una raqueta: modelo, nudos, fecha,
/// logotipo, color, configuración de cuerdas y estado.</para>
/// <para>La fecha por defecto es <c>DateTime.UtcNow.AddDays(7)</c> y el estado inicial es <see cref="Status.PENDING"/>.</para>
/// <para>Implementa <see cref="ITimestamped"/> para gestión automática de <see cref="CreatedAt"/> y <see cref="UpdatedAt"/>.</para>
/// </remarks>
public class PedidoLinea: ITimestamped
{
    /// <summary>Identificador ULID de la línea de pedido.</summary>
    public Ulid Id { get; set; }
    /// <summary>ULID del pedido padre al que pertenece esta línea.</summary>
    public Ulid PedidoId { get; set; }
    /// <summary>Modelo de la raqueta a encordar.</summary>
    public string RaquetModel { get; set; } = string.Empty;
    /// <summary>Número de nudos del encordado.</summary>
    public byte Nudos { get; set; }
    /// <summary>Fecha programada para el encordado. Valor por defecto: UTC +7 días.</summary>
    public DateTime DateString { get; set; } = DateTime.UtcNow.AddDays(7);
    /// <summary>Indica si la raqueta lleva logotipo personalizado.</summary>
    public bool Logotype { get; set; }
    /// <summary>Color del encordado.</summary>
    public string Color { get; set; } = string.Empty;
    /// <summary>Configuración de cuerdas (vertical, horizontal, tensiones, pre-stretch).</summary>
    public StringSetup StringSetup { get; set; } = null!;
    /// <summary>Estado actual de la línea. Valor por defecto: <see cref="Status.PENDING"/>.</summary>
    public Status Status { get; set; } = Status.PENDING;
    /// <summary>Fecha de creación (UTC).</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    /// <summary>Fecha de última actualización (UTC).</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Navegación EF Core al pedido padre.</summary>
    public Pedidos Pedido { get; set; } = null!;
}