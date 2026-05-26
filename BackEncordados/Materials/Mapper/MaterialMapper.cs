using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Model;

namespace BackEncordados.Materials.Mapper;

/// <summary>
/// Mapper estático para conversiones entre <see cref="Material"/> y sus DTOs.
/// </summary>
/// <remarks>
/// <para>Proporciona métodos de extensión para transformar entidades <see cref="Material"/>
/// a <see cref="MaterialResponseDto"/> y <see cref="MaterialRequestDto"/> a entidad.</para>
/// <para>Depende de <see cref="ICloudinaryService"/> para resolver URLs de imágenes
/// y transformar string enum a <see cref="MaterialType"/> mediante <c>Enum.Parse</c>.</para>
/// </remarks>
public static class MaterialMapper {

    /// <summary>
    /// Convierte una entidad <see cref="Material"/> a <see cref="MaterialResponseDto"/>.
    /// </summary>
    /// <param name="material">Entidad origen.</param>
    /// <param name="cloudinary">Servicio Cloudinary para resolución de URLs.</param>
    /// <returns>DTO de respuesta con datos del material.</returns>
    public static MaterialResponseDto ToDto(this Material material, ICloudinaryService cloudinary) {
        return new MaterialResponseDto(
            material.Id,
            TournamentId: material.TournamentId,
            material.Marca,
            material.Modelo,
            material.Stock,
            material.Precio,
            material.Type.ToString(),
            cloudinary.ResolveImageUrl(material.ImageUrl, CloudinaryConstants.FOLDER_MATERIES)
        );
    }

    /// <summary>
    /// Convierte un <see cref="MaterialRequestDto"/> a una nueva entidad <see cref="Material"/>.
    /// </summary>
    /// <param name="material">DTO de origen.</param>
    /// <returns>Entidad <see cref="Material"/> lista para persistir.</returns>
    public static Material ToModel(this MaterialRequestDto material) {
        return new Material {
            Marca = material.Marca,
            TournamentId = material.TournamentId,
            Modelo = material.Modelo,
            Stock = material.Stock,
            Precio = material.Precio,
            Type = Enum.Parse<MaterialType>(material.Type, true)
        };
    }
}