namespace BackEncordados.Common.Database.Helpers;

/// <summary>
/// Interfaz de auditoría para entidades.
/// Implementada por: Categoria, Producto, User, Pedido.
/// Propiedades: CreatedAt (readonly), UpdatedAt.
/// </summary>
public interface ITimestamped
{
    /// <summary>Fecha de creación.</summary>
    DateTime CreatedAt { get; }

    /// <summary>Fecha de última modificación.</summary>
    DateTime UpdatedAt { get; }
}