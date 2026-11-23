namespace QuizBattle.Application.Practice;

/// <summary>Xáo thứ tự câu của lượt ôn (Fisher-Yates). Nhận Random từ ngoài để test được với seed cố định.</summary>
public static class PracticeShuffler
{
    public static Guid[] Shuffle(IReadOnlyList<Guid> ids, Random random)
    {
        var result = ids.ToArray();
        for (var i = result.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return result;
    }
}
