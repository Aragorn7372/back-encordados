using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Model;

namespace BackEncordados.Materials.Mapper;

public static class CuerdaMapper {
    
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