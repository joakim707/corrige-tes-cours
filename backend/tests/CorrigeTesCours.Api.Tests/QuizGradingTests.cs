using CorrigeTesCours.Api.Quizzes;
using CorrigeTesCours.Domain.Entities;
using Xunit;

namespace CorrigeTesCours.Api.Tests;

public class QuizGradingTests
{
    [Theory]
    [InlineData("Paris", "Paris", true)]
    [InlineData("Paris", "paris", true)] // insensible à la casse
    [InlineData("Paris", " Paris ", true)] // insensible aux espaces superflus
    [InlineData("Paris", "Lyon", false)]
    public void IsCorrect_Qcm_ExigeCorrespondanceExacteNormalisee(string attendue, string reponse, bool correcte)
    {
        var question = new QuizQuestion { Type = QuestionType.Qcm, ReponseAttendue = attendue, Options = { attendue, "Lyon" } };

        Assert.Equal(correcte, QuizGrading.IsCorrect(question, reponse));
    }

    [Theory]
    [InlineData("Vrai", "Vrai", true)]
    [InlineData("Vrai", "Faux", false)]
    public void IsCorrect_VraiFaux_ExigeCorrespondanceExacte(string attendue, string reponse, bool correcte)
    {
        var question = new QuizQuestion { Type = QuestionType.VraiFaux, ReponseAttendue = attendue };

        Assert.Equal(correcte, QuizGrading.IsCorrect(question, reponse));
    }

    [Theory]
    [InlineData("Révolution française", "la Révolution française de 1789", true)] // contient la réponse
    [InlineData("mitochondrie", "mitochondrie", true)]
    [InlineData("", "", false)] // réponse vide jamais comptée correcte
    [InlineData("photosynthèse", "je ne sais pas", false)]
    public void IsCorrect_Ouverte_AccepteCorrespondancePartielle(string attendue, string reponse, bool correcte)
    {
        var question = new QuizQuestion { Type = QuestionType.Ouverte, ReponseAttendue = attendue };

        Assert.Equal(correcte, QuizGrading.IsCorrect(question, reponse));
    }
}
