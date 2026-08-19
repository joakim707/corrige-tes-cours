namespace CorrigeTesCours.Api.Dtos;

public record DashboardStatsResponse(
    int MatieresCount,
    int FichesCount,
    int QuizCount,
    double? ScoreMoyen);
