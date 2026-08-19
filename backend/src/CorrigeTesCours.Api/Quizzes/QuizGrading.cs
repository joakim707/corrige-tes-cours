using CorrigeTesCours.Domain.Entities;

namespace CorrigeTesCours.Api.Quizzes;

public static class QuizGrading
{
    /// <summary>
    /// QCM/vrai-faux : comparaison stricte normalisée. Questions ouvertes : correspondance souple
    /// (l'IA de correction structurée arrivera en v2 ; pour l'instant, une correspondance partielle suffit).
    /// </summary>
    public static bool IsCorrect(QuizQuestion question, string reponseUtilisateur)
    {
        var user = Normalize(reponseUtilisateur);
        var expected = Normalize(question.ReponseAttendue);

        if (question.Type == QuestionType.Ouverte)
            return user.Length > 0 && (user.Contains(expected) || expected.Contains(user));

        return user == expected;
    }

    private static string Normalize(string s) => s.Trim().ToLowerInvariant();
}
