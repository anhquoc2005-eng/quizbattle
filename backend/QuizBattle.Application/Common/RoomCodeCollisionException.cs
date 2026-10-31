namespace QuizBattle.Application.Common;

/// <summary>
/// Ném khi IX_Rooms_Code bị trùng lúc insert. RoomService bắt để sinh mã khác và thử lại;
/// hết số lần thử thì để lọt ra GlobalExceptionHandler (rơi vào nhánh 500 mặc định, có log).
/// </summary>
public sealed class RoomCodeCollisionException() : Exception("Mã phòng bị trùng.");
