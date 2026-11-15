namespace QuizBattle.Application.Avatars;

public interface IAvatarService
{
    Task<MyAvatarDto> GetMyAvatarAsync(Guid userId, CancellationToken cancellationToken);

    Task<SavedAvatarDto> SaveMyAvatarAsync(Guid userId, SaveAvatarRequest request, CancellationToken cancellationToken);
}
