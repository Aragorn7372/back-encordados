using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Model;
using BackEncordados.Usuarios.Dto;

namespace BackEncordados.Talleres.Mapper;

public static class TournamentsMapper
{
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
        public static WorkerMachineAssignmentResponseDto ToWorkerMachineAssignmentResponseDto(this WorkerMachineAssignment workerMachineAssignment, UserResponseDto user) {
            return new WorkerMachineAssignmentResponseDto(
                workerMachineAssignment.MachineName,
                user
            );
        }
}