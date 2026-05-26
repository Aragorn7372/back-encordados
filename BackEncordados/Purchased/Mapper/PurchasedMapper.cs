using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Model;
using BackEncordados.Usuarios.Dto;

namespace BackEncordados.Purchased.Mapper;

/// <summary>
/// Provides extension methods for mapping between Purchased domain models
/// (<see cref="Pedidos"/>, <see cref="PedidoLinea"/>, <see cref="StringSetup"/>) and
/// their corresponding DTOs across create, read, and patch operations.
/// </summary>
/// <remarks>
/// <para>
/// This mapper is a static helper used primarily by <see cref="Service.PurchasedService"/>
/// to convert between layers. It covers three directions:
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Direction</term>
///     <description>Methods</description>
///   </listheader>
///   <item>
///     <term>Entity → Response DTO</term>
///     <description><see cref="ToDto(PedidoLinea)"/>, <see cref="ToDto(Pedidos, UserResponseDto, UserResponseDto)"/></description>
///   </item>
///   <item>
///     <term>Request DTO → Entity</term>
///     <description><see cref="ToEntity(PurchasedRequestDto, Ulid, Ulid)"/>, <see cref="ToEntity(PedidoLineaRequestDto, Ulid)"/></description>
///   </item>
///   <item>
///     <term>Patch DTO → Entity (merge)</term>
///     <description><see cref="ToEntity(PedidoLineaPatchDto, PedidoLinea)"/></description>
///   </item>
///   <item>
///     <term>DTO → Value object</term>
///     <description><see cref="ToModel(StringSetupDto)"/></description>
///   </item>
/// </list>
/// <para>
/// All create-side methods generate new <see cref="Ulid"/> identifiers and set UTC timestamps.
/// Patch methods only overwrite fields that are non-null on the DTO, following the
/// "partial update" (merge patch) pattern.
/// </para>
/// </remarks>
public static class PurchasedMapper
{
    /// <summary>
    /// Maps a <see cref="PedidoLinea"/> entity to a <see cref="PedidoLineaResponseDto"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a direct 1:1 property copy with no transformations.
    /// Called from <see cref="ToDto(Pedidos, UserResponseDto, UserResponseDto)"/> for each line item
    /// and from <see cref="Service.PurchasedService"/> when updating or canceling individual lines.
    /// </para>
    /// <para>The <see cref="PedidoLinea.StringSetup"/> value object is included as-is (not flattened).</para>
    /// </remarks>
    /// <param name="linea">The <see cref="PedidoLinea"/> entity to map.</param>
    /// <returns>A <see cref="PedidoLineaResponseDto"/> with all scalar properties copied.</returns>
    public static PedidoLineaResponseDto ToDto(this PedidoLinea linea)
    {
        return new PedidoLineaResponseDto
        (
            Id: linea.Id,
            RaquetModel: linea.RaquetModel,
            Nudos: linea.Nudos,
            DateString: linea.DateString,
            Logotype: linea.Logotype,
            Color: linea.Color,
            Status: linea.Status,
            StringSetup: linea.StringSetup
        );
    }

    /// <summary>
    /// Maps a <see cref="Pedidos"/> entity together with its resolved player and encorder DTOs
    /// into a full <see cref="PurchasedResponseDto"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the main response assembly method. It combines the order header with two
    /// pre-resolved <see cref="UserResponseDto"/> instances (fetched from cache or DB by the service layer)
    /// and maps each <see cref="PedidoLinea"/> through <see cref="ToDto(PedidoLinea)"/>.
    /// </para>
    /// <para>The <c>PayStatus</c> enum is converted to its string representation via <c>ToString()</c>
    /// so the API consumer receives a readable value (e.g. "PAID", "PENDING") rather than an integer.</para>
    /// </remarks>
    /// <param name="pedido">The <see cref="Pedidos"/> aggregate root.</param>
    /// <param name="playerDto">The resolved <see cref="UserResponseDto"/> for the player (fetched upstream).</param>
    /// <param name="encorderDto">The resolved <see cref="UserResponseDto"/> for the encorder (fetched upstream).</param>
    /// <returns>A <see cref="PurchasedResponseDto"/> including the nested line items.</returns>
    public static PurchasedResponseDto ToDto(this Pedidos pedido, UserResponseDto playerDto, UserResponseDto encorderDto)
    {
        return new PurchasedResponseDto
        (
            Id: pedido.Id,
            TournamentId: pedido.TournamentId,
            Player: playerDto,
            Encorder: encorderDto,
            Machine: pedido.Machine,
            Comments: pedido.Comments,
            PayStatus: pedido.PayStatus.ToString(),
            CreatedAt: pedido.CreatedAt,
            UpdatedAt: pedido.UpdatedAt,
            Price: pedido.Price,
            Lineas: pedido.Lineas.Select(l => l.ToDto()).ToList()
        );
    }

    /// <summary>
    /// Creates a new <see cref="Pedidos"/> entity from a <see cref="PurchasedRequestDto"/>.
    /// </summary>
    /// <remarks>
    /// <para>Mapping flow:</para>
    /// <list type="number">
    ///   <item><description>Generate a new <see cref="Ulid"/> for the order header.</description></item>
    ///   <item><description>Copy scalar fields (TournamentId, Machine, Comments, Price) directly from the DTO.</description></item>
    ///   <item><description>Parse <c>PayStatus</c> from its string representation using case-insensitive parsing.</description></item>
    ///   <item><description>Assign the resolved player and encorder Ulid values provided by the service layer.</description></item>
    ///   <item><description>Set <c>CreatedAt</c> and <c>UpdatedAt</c> to the current UTC time.</description></item>
    ///   <item><description>Iterate over line item DTOs, mapping each to a <see cref="PedidoLinea"/> via <see cref="ToEntity(PedidoLineaRequestDto, Ulid)"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="dto">The incoming create request DTO from the API.</param>
    /// <param name="playerId">The resolved <see cref="Ulid"/> of the player (fetched by username upstream).</param>
    /// <param name="encorderId">The resolved <see cref="Ulid"/> of the encorder (fetched by username upstream).</param>
    /// <returns>A fully populated <see cref="Pedidos"/> aggregate ready for persistence.</returns>
    public static Pedidos ToEntity(this PurchasedRequestDto dto, Ulid playerId, Ulid encorderId)
    {
        var pedido = new Pedidos
        {
            Id = Ulid.NewUlid(),
            TournamentId = dto.TournamentId,
            PlayerId = playerId,
            AssignedTo = encorderId,
            Machine = dto.Machine,
            Comments = dto.Comments,
            PayStatus = Enum.Parse<PaymentStatus>(dto.PayStatus, true),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Price = dto.Price,
            Lineas = new List<PedidoLinea>()
        };

        foreach (var lineaDto in dto.Lineas)
        {
            pedido.Lineas.Add(lineaDto.ToEntity(pedido.Id));
        }

        return pedido;
    }

    /// <summary>
    /// Creates a new <see cref="PedidoLinea"/> entity from a <see cref="PedidoLineaRequestDto"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The line is initialized with <see cref="Status.PENDING"/> (the default starting state)
    /// and a freshly generated <see cref="Ulid"/>. The <see cref="StringSetup"/> value object
    /// is mapped via <see cref="ToModel(StringSetupDto)"/>.
    /// </para>
    /// </remarks>
    /// <param name="dto">The line item create request.</param>
    /// <param name="pedidoId">The parent <see cref="Pedidos"/> ID to associate this line with.</param>
    /// <returns>A <see cref="PedidoLinea"/> entity ready for persistence.</returns>
    public static PedidoLinea ToEntity(this PedidoLineaRequestDto dto, Ulid pedidoId)
    {
        return new PedidoLinea
        {
            Id = Ulid.NewUlid(),
            PedidoId = pedidoId,
            RaquetModel = dto.RaquetModel,
            Nudos = dto.Nudos,
            DateString = dto.DateString,
            Logotype = dto.Logotype,
            Color = dto.Color,
            Status = Status.PENDING,
            StringSetup = dto.StringSetup.ToModel(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Applies a partial patch from a <see cref="PedidoLineaPatchDto"/> onto an existing <see cref="PedidoLinea"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method implements the merge-patch pattern:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Only fields that are non-null on the DTO overwrite the corresponding property on the entity.</description></item>
    ///   <item><description><c>RaquetModel</c>, <c>Color</c> — replaced if non-null.</description></item>
    ///   <item><description><c>Nudos</c>, <c>DateString</c>, <c>Logotype</c> — replaced if <c>HasValue</c> is true.</description></item>
    ///   <item><description><c>Status</c> — parsed case-insensitively from string, applied if non-null.</description></item>
    ///   <item><description><c>StringSetup</c> — fully replaced (not merged) if non-null.</description></item>
    ///   <item><description><c>UpdatedAt</c> is always set to the current UTC time regardless of which fields changed.</description></item>
    /// </list>
    /// <para>
    /// Fields omitted from the DTO (null) retain their existing values on the entity.
    /// The same <paramref name="existing"/> instance is mutated and returned for convenience.
    /// </para>
    /// </remarks>
    /// <param name="dto">The patch DTO with optional (nullable) fields.</param>
    /// <param name="existing">The existing <see cref="PedidoLinea"/> entity to mutate in place.</param>
    /// <returns>The same <paramref name="existing"/> instance after applying changes.</returns>
    public static PedidoLinea ToEntity(this PedidoLineaPatchDto dto, PedidoLinea existing)
    {
        if (dto.RaquetModel != null) existing.RaquetModel = dto.RaquetModel;
        if (dto.Nudos.HasValue) existing.Nudos = dto.Nudos.Value;
        if (dto.DateString.HasValue) existing.DateString = dto.DateString.Value;
        if (dto.Logotype.HasValue) existing.Logotype = dto.Logotype.Value;
        if (dto.Color != null) existing.Color = dto.Color;
        if (dto.Status != null) existing.Status = Enum.Parse<Status>(dto.Status, true);
        if (dto.StringSetup != null) existing.StringSetup = dto.StringSetup.ToModel();
        existing.UpdatedAt = DateTime.UtcNow;
        return existing;
    }

    /// <summary>
    /// Maps a <see cref="StringSetupDto"/> to a <see cref="StringSetup"/> model (value object).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Copies all six string-configuration fields directly:
    /// vertical and horizontal string name, tension (kg), and pre-stretch (%).
    /// The model is a simple data container with no behavior; it is embedded in <see cref="PedidoLinea"/>.
    /// </para>
    /// <para>Called from both <see cref="ToEntity(PedidoLineaRequestDto, Ulid)"/> (create) and
    /// <see cref="ToEntity(PedidoLineaPatchDto, PedidoLinea)"/> (patch) when a <see cref="StringSetupDto"/> is present.</para>
    /// </remarks>
    /// <param name="stringSetupDto">The source DTO with string configuration values.</param>
    /// <returns>A new <see cref="StringSetup"/> value object.</returns>
    public static StringSetup ToModel(this StringSetupDto stringSetupDto)
    {
        return new StringSetup
        {
            StringV = stringSetupDto.StringV,
            TensionV = stringSetupDto.TensionV,
            PreStetchV = stringSetupDto.PreStetchV,
            StringH = stringSetupDto.StringH,
            TensionH = stringSetupDto.TensionH,
            PreStetchH = stringSetupDto.PreStetchH
        };
    }
}
