namespace QuizBattle.Application.Abstractions;

/// <summary>
/// Kiểm tra kết nối database. Application chỉ biết interface này,
/// phần cài đặt cụ thể (EF Core + PostgreSQL) nằm ở Infrastructure.
/// </summary>
public interface IDatabaseHealthCheck
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken);
}
