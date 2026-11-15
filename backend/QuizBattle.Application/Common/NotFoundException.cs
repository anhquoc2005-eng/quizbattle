namespace QuizBattle.Application.Common;

/// <summary>
/// Ném khi không tìm thấy tài nguyên, hoặc tài nguyên thuộc về người khác (để không lộ sự tồn tại). GlobalExceptionHandler map sang 404.
/// Code (tùy chọn, vd "item_not_found") để client phân biệt nguyên nhân; có giá trị thì ghi vào ProblemDetails.
/// </summary>
public sealed class NotFoundException(string message, string? code = null) : Exception(message)
{
    public string? Code { get; } = code;
}
