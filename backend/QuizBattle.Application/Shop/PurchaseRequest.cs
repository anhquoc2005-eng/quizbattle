namespace QuizBattle.Application.Shop;

/// <summary>Chỉ mang mã món đồ; giá luôn do server quyết định.</summary>
public sealed record PurchaseRequest(string? ItemKey);
