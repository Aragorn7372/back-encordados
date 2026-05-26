using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Model;

namespace BackEncordados.Materials.Mapper;

/// <summary>
/// Mapper estático para conversiones entre <see cref="Cuerdas"/> y sus DTOs.
/// </summary>
/// <remarks>
/// <para>Proporciona métodos de extensión para transformar entidades <see cref="Cuerdas"/>
/// a <see cref="CuerdaResponseDto"/> y <see cref="CuerdaRequestDto"/> a entidad.</para>
/// <para>Resuelve URLs de imágenes via <see cref="ICloudinaryService"/> y parsea
/// enums <see cref="FormatoCuerda"/> y <see cref="StringsType"/> desde strings.</para>
/// </remarks>
public static class CuerdaMapper {

    /// <summary>
    /// Convierte una entidad <see cref="Cuerdas"/> a <see cref="CuerdaResponseDto"/>.
    /// </summary>
    /// <param name="cuerda">Entidad origen.</param>
    /// <param name="cloudinary">Servicio Cloudinary para resolución de URLs.</param>
    /// <returns>DTO de respuesta con datos de la cuerda.</returns>
    public static CuerdaResponseDto ToDto(this Cuerdas cuerda, ICloudinaryService cloudinary) {
        return new CuerdaResponseDto(
            Id: cuerda.Id,
            TournamentId: cuerda.TournamentId,
            Marca: cuerda.Marca,
            Modelo: cuerda.Modelo,
            Stock: cuerda.Stock,
            Precio: cuerda.Precio,
            StringFormat: cuerda.StringFormat.ToString(),
            StringsType: cuerda.StringsType.ToString(),
            Calibre: cuerda.Calibre,
            ImageUrl: cloudinary.ResolveImageUrl(cuerda.ImageUrl, CloudinaryConstants.FOLDER_MATERIES)
        );
    }

    /// <summary>
    /// Convierte un <see cref="CuerdaRequestDto"/> a una nueva entidad <see cref="Cuerdas"/>.
    /// </summary>
    /// <param name="cuerda">DTO de origen.</param>
    /// <returns>Entidad <see cref="Cuerdas"/> lista para persistir.</returns>
    public static Cuerdas ToModel(this CuerdaRequestDto cuerda) {
        return new Cuerdas {
            Marca = cuerda.Marca,
            TournamentId = cuerda.TournamentId,
            Modelo = cuerda.Modelo,
            Stock = cuerda.Stock,
            Precio = cuerda.Precio,
            StringFormat = Enum.Parse<FormatoCuerda>(cuerda.StringFormat, true),
            StringsType = Enum.Parse<StringsType>(cuerda.StringsType, true),
            Calibre = cuerda.Calibre
        };
    }
}