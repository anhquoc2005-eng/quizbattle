namespace QuizBattle.Application.Rooms;

public interface IRoomCodeGenerator
{
    /// <summary>Sinh một mã phòng 6 ký tự. Không đảm bảo duy nhất - gọi lại khi repository báo trùng.</summary>
    string Generate();
}
