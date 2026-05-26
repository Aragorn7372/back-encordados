using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Model;
using BackEncordados.Usuarios.Dto;

namespace BackEncordados.Talleres.Mapper;

/// <summary>
/// Métodos de extensión para mapear entre la entidad <see cref="Tournaments"/> y sus DTOs.
/// </summary>
/// <remarks>
/// <para>Proporciona cinco métodos de mapeo:</para>
/// <list type="table">
///   <listheader>
///     <term>Método</term>
///     <description>Origen → Destino</description>
///     <description>Uso</description>
///   </listheader>
///   <item>
///     <term><c>ToTournamentResponseDto</c></term>
///     <description><see cref="Tournaments"/> → <see cref="TournamentResponseDto"/></description>
///     <description>Listados y vistas resumidas.</description>
///   </item>
///   <item>
///     <term><c>ToTournamentResponseDetailsDto</c></term>
///     <description><see cref="Tournaments"/> → <see cref="TournamentResponseDetailsDto"/></description>
///     <description>Vista detallada con usuarios, owner y supervisores.</description>
///   </item>
///   <item>
///     <term><c>ToTournaments</c> (Admin)</term>
///     <description><see cref="TournamentAdminRequestDto"/> → <see cref="Tournaments"/></description>
///     <description>Creación desde administrador (incluye OwnerId).</description>
///   </item>
///   <item>
///     <term><c>ToTournaments</c> (Owner)</term>
///     <description><see cref="TournamentRequestDto"/> → <see cref="Tournaments"/></description>
///     <description>Creación desde propietario (ownerId se pasa como parámetro).</description>
///   </item>
///   <item>
///     <term><c>ToWorkerMachineAssignmentResponseDto</c></term>
///     <description><see cref="WorkerMachineAssignment"/> → <see cref="WorkerMachineAssignmentResponseDto"/></description>
///     <description>Asignaciones trabajador-máquina.</description>
///   </item>
/// </list>
/// <para>Todas las rutas de imagen se resuelven mediante <see cref="ICloudinaryService.ResolveImageUrl"/>
/// usando la carpeta <c>CloudinaryConstants.FOLDER_TALLERES</c>.</para>
/// </remarks>
public static class TournamentsMapper
{
        /// <summary>
        /// Convierte un <see cref="Tournaments"/> a <see cref="TournamentResponseDto"/> resolviendo la URL del logotipo.
        /// </summary>
        /// <param name="tournament">Entidad de torneo a mapear.</param>
        /// <param name="cloudinary">Servicio de Cloudinary para resolución de URLs de imágenes.</param>
        /// <returns>DTO básico con ID, nombre, fechas y logotipo.</returns>
        public static TournamentResponseDto ToTournamentResponseDto(this Tournaments tournament,ICloudinaryService cloudinary)
        {
            return new TournamentResponseDto(
                tournament.Id,
                tournament.Title,
                tournament.EndTournament,
                tournament.StartTournament,
                cloudinary.ResolveImageUrl(tournament.Logotype, CloudinaryConstants.FOLDER_TALLERES)
            );
        }
        /// <summary>
        /// Convierte un <see cref="Tournaments"/> a <see cref="TournamentResponseDetailsDto"/> con listas de usuarios.
        /// </summary>
        /// <param name="tournament">Entidad de torneo a mapear.</param>
        /// <param name="users">Lista de DTOs de usuarios (rol USER) asociados al torneo.</param>
        /// <param name="owner">DTO del propietario del torneo.</param>
        /// <param name="supervisors">Lista de DTOs de supervisores asignados.</param>
        /// <param name="cloudinary">Servicio de Cloudinary para resolución de URLs de imágenes.</param>
        /// <returns>DTO detallado con usuarios, owner y supervisores.</returns>
        public static TournamentResponseDetailsDto ToTournamentResponseDetailsDto(
            this Tournaments tournament,
            List<UserResponseDto> users,
            UserResponseDto owner,
            List<UserResponseDto> supervisors,
            ICloudinaryService cloudinary
            ) {
            return new TournamentResponseDetailsDto(
                tournament.Id,
                tournament.Title,
                tournament.StartTournament,
                tournament.EndTournament,
                cloudinary.ResolveImageUrl(tournament.Logotype, CloudinaryConstants.FOLDER_TALLERES),
                users,
                owner,
                supervisors
            );
        }

        /// <summary>
        /// Convierte un <see cref="TournamentAdminRequestDto"/> a entidad <see cref="Tournaments"/> (creación por admin).
        /// </summary>
        /// <param name="tournamentAdminRequestDto">DTO de solicitud con Name, OwnerId, fechas y logotipo.</param>
        /// <param name="filename">URL del logotipo (subido a Cloudinary).</param>
        /// <param name="imageId">Public ID del logotipo en Cloudinary para futura eliminación.</param>
        /// <returns>Entidad <see cref="Tournaments"/> lista para persistir.</returns>
        public static Tournaments ToTournaments(
            this TournamentAdminRequestDto tournamentAdminRequestDto, 
            string filename,
            string? imageId
            )
        {
            return new Tournaments
            {
                Owner = tournamentAdminRequestDto.OwnerId,
                Title = tournamentAdminRequestDto.Name,
                EndTournament = tournamentAdminRequestDto.EndTournament,
                StartTournament = tournamentAdminRequestDto.StartTournament,
                Logotype = filename,
                LogotypePublicId = imageId
            };
        }
        /// <summary>
        /// Convierte un <see cref="TournamentRequestDto"/> a entidad <see cref="Tournaments"/> (creación por owner).
        /// </summary>
        /// <param name="tournamentRequestDto">DTO de solicitud con Name, fechas y logotipo.</param>
        /// <param name="ownerId">ULID del propietario del torneo (extraído del JWT).</param>
        /// <param name="filename">URL del logotipo (subido a Cloudinary).</param>
        /// <param name="imageId">Public ID del logotipo en Cloudinary para futura eliminación.</param>
        /// <returns>Entidad <see cref="Tournaments"/> lista para persistir.</returns>
        public static Tournaments ToTournaments(
            this TournamentRequestDto tournamentRequestDto, 
            Ulid ownerId, 
            string filename,
            string? imageId
            ) {
            return new Tournaments {
                
                Owner = ownerId,
                Title = tournamentRequestDto.Name,
                EndTournament = tournamentRequestDto.EndTournament,
                StartTournament = tournamentRequestDto.StartTournament,
                Logotype = filename,
                LogotypePublicId = imageId
            };
        }
        /// <summary>
        /// Convierte una asignación <see cref="WorkerMachineAssignment"/> a <see cref="WorkerMachineAssignmentResponseDto"/>.
        /// </summary>
        /// <param name="workerMachineAssignment">Asignación trabajador-máquina.</param>
        /// <param name="user">DTO del usuario asignado.</param>
        /// <returns>DTO con nombre de máquina y datos del usuario.</returns>
        public static WorkerMachineAssignmentResponseDto ToWorkerMachineAssignmentResponseDto(this WorkerMachineAssignment workerMachineAssignment, UserResponseDto user) {
            return new WorkerMachineAssignmentResponseDto(
                workerMachineAssignment.MachineName,
                user
            );
        }
}