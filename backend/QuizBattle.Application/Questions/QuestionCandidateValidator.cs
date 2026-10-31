namespace QuizBattle.Application.Questions;

/// <summary>Hàm thuần: lọc candidate AI sinh ra hợp lệ, bỏ câu sai định dạng, trùng trong lô và trùng câu đã có.</summary>
public static class QuestionCandidateValidator
{
    public static IReadOnlyList<QuestionCandidate> FilterValid(
        IReadOnlyList<QuestionCandidate> candidates, IReadOnlyCollection<string> existingTexts)
    {
        var seenTexts = new HashSet<string>(existingTexts.Select(Normalize), StringComparer.Ordinal);
        var result = new List<QuestionCandidate>();

        foreach (var candidate in candidates)
        {
            if (!QuestionRules.IsValidText(candidate.Text)
                || !QuestionRules.IsValidOptions(candidate.Options)
                || !QuestionRules.IsValidCorrectIndex(candidate.CorrectIndex))
            {
                continue;
            }

            if (!seenTexts.Add(Normalize(candidate.Text)))
            {
                continue; // trùng câu đã có trong phòng hoặc trùng câu khác trong cùng lô
            }

            result.Add(candidate);
        }

        return result;
    }

    private static string Normalize(string text) => text.Trim().ToUpperInvariant();
}
