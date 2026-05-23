using BackEncordados.Common.Database.Helpers;
using BackEncordados.Common.Service.Cloudinary;

namespace BackEncordados.Materials.Model;

public class Cuerdas: ITimestamped
{
        public long Id { get; set; }
        public Ulid TournamentId { get; set; }
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public int Stock { get; set; } = -1;
        public double Precio { get; set; } = -1;
        public FormatoCuerda StringFormat { get; set; } = FormatoCuerda.Reel;
        public StringsType StringsType { get; set; } = StringsType.Polyester;
        
        /// <summary>Fecha de creación en UTC.</summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>Fecha de última modificación en UTC.</summary>
        public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }=false;
        public string ImageUrl { get; set; }=CloudinaryConstants.DEFAULT_IMAGE_MATERIALES;
        public string? CloudinaryPublicId { get; set; }
}