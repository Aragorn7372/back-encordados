using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Model;
using BackEncordados.Usuarios.Dto;

namespace BackEncordados.Talleres.Mapper;

public static class TournamentsMapper
{
        public static TournamentResponseDto ToTournamentResponseDto(this Tournaments tournament)
        {
            return new TournamentResponseDto(
                tournament.Id,
                tournament.Title,
                tournament.EndTournament,
                tournament.StartTournament,
                tournament.Logotype
            );
        }
        public static TournamentResponseDetailsDto ToTournamentResponseDetailsDto(this Tournaments tournament, List<UserResponseDto> users,UserResponseDto owner,List<UserResponseDto> supervisors)
        {
            return new TournamentResponseDetailsDto(
                tournament.Id,
                tournament.Title,
                tournament.StartTournament,
                tournament.EndTournament,
                tournament.Logotype,
                users,
                owner,
                supervisors
            );
        }

        public static Tournaments ToTournaments(this TournamentAdminRequestDto tournamentAdminRequestDto)
        {
            return new Tournaments
            {
                Owner = tournamentAdminRequestDto.OwnerId,
                Title = tournamentAdminRequestDto.Name,
                EndTournament = tournamentAdminRequestDto.EndTournament,
                StartTournament = tournamentAdminRequestDto.StartTournament,
                Logotype = tournamentAdminRequestDto.Logotype
            };
        }
        public static Tournaments ToTournaments(this TournamentRequestDto tournamentRequestDto, Ulid ownerId)
        {
            return new Tournaments
            {
                Owner = ownerId,
                Title = tournamentRequestDto.Name,
                EndTournament = tournamentRequestDto.EndTournament,
                StartTournament = tournamentRequestDto.StartTournament,
                Logotype = tournamentRequestDto.Logotype
            };
        }
        public static WorkerMachineAssignmentResponseDto ToWorkerMachineAssignmentResponseDto(this WorkerMachineAssignment workerMachineAssignment, UserResponseDto user)
        {
            return new WorkerMachineAssignmentResponseDto(
                workerMachineAssignment.MachineName,
                user
            );
        }
}