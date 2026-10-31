using System.Security.Cryptography;

namespace QuizBattle.Application.Questions;

/// <summary>Xáo vị trí 4 đáp án bằng RandomNumberGenerator (Fisher-Yates) - AI hay đặt đáp án đúng ở vị trí đầu.</summary>
public static class OptionShuffler
{
    public static (string[] Options, int CorrectIndex) Shuffle(string[] options, int correctIndex)
    {
        var shuffled = (string[])options.Clone();
        var correctText = options[correctIndex];

        for (var i = shuffled.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return (shuffled, Array.IndexOf(shuffled, correctText));
    }
}
