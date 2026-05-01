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
        public static TournamentResponseDetailsDto ToTournamentResponseDetailsDto(this Tournaments tournament, List<UserResponseDto> users)
        {
            return new TournamentResponseDetailsDto(
                tournament.Id,
                tournament.Title,
                tournament.StartTournament,
                tournament.EndTournament,
                tournament.Logotype,
                users
            );
        }

        public static Tournaments ToTournaments(this TournamentRequestDto tournamentRequestDto)
        {
            return new Tournaments
            {
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