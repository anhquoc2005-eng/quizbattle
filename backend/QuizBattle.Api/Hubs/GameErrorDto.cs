namespace QuizBattle.Api.Hubs;

/// <summary>
/// Payload sự kiện Error. Method = tên Hub method gây lỗi (vd "JoinRoom"), Code phân loại lỗi để client
/// tự quyết định xử lý (vd chỉ xóa sessionToken đã lưu khi Method="JoinRoom" và Code là "unauthorized"
/// hoặc "not_found" - lỗi tạm thời như "conflict"/"unknown" không được xóa, nếu không người chơi mất
/// quyền vào lại trận đang diễn ra chỉ vì một lỗi thoáng qua). Code "session_replaced": cùng tài khoản đã vào
/// lại phòng từ thiết bị khác - client xóa phiên đã lưu, dừng tự kết nối lại. Message hiển thị cho người dùng.
/// </summary>
public sealed record GameErrorDto(string Method, string Code, string Message);
