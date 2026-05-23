using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Model;

namespace BackEncordados.Materials.Mapper;

public static class MaterialMapper {
    
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