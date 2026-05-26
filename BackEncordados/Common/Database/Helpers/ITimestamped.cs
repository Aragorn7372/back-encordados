namespace BackEncordados.Common.Database.Helpers;


/// <summary>
/// Interfaz que define las propiedades de timestamps para entidades que requieren
/// auditoría temporal: <c>CreatedAt</c> (fecha de creación) y <c>UpdatedAt</c> (fecha de última modificación).
/// </summary>
/// <remarks>
/// Las entidades que implementan esta interfaz son procesadas automáticamente por
/// <see cref="TimestampInterceptor"/> para mantener los timestamps actualizados
/// en cada operación de guardado, y por <see cref="TimestampExtensions.ConfigureTimestamps"/>
/// para configurar valores por defecto <c>CURRENT_TIMESTAMP</c> en la base de datos.
/// </remarks>
public interface ITimestamped
{
    /// <summary>Fecha y hora de creación del registro en formato UTC.</summary>
    DateTime CreatedAt { get; }

    /// <summary>Fecha y hora de la última modificación del registro en formato UTC.</summary>
    DateTime UpdatedAt { get; }
}