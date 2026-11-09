using QuizBattle.Domain;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Application.Game;

/// <summary>
/// Gom dữ liệu trận, gọi hàm thuần RewardCalculator rồi áp kết quả lên snapshot (cộng Users, thêm MatchReward và
/// CoinTransaction). Repository lưu tất cả cùng Room.Status = Finished trong một SaveChanges.
/// </summary>
internal static class RewardSettlement
{
    /// <summary>Tính và áp thưởng lúc kết thúc trận; trả DTO cho mọi người chơi (khách: "guest").</summary>
    public static IReadOnlyList<PlayerRewardDto> Settle(GameSnapshot snapshot, DateTimeOffset now)
    {
        var room = snapshot.Room;
        var participants = snapshot.Players
            .Select(p => new RewardParticipant(p.Id, p.UserId, p.Score, snapshot.Answers.Count(a => a.PlayerId == p.Id)))
            .ToList();
        var results = RewardCalculator.Calculate(participants, room.HostUserId).ToDictionary(r => r.PlayerId);

        var dtos = new List<PlayerRewardDto>();
        foreach (var player in snapshot.Players)
        {
            var result = results[player.Id];
            if (player.UserId is not { } userId)
            {
                dtos.Add(Guest(player, result.RewardRank));
                continue;
            }

            var user = snapshot.Users[userId];
            var xpBefore = user.TotalXp;
            var coinsTotal = result.Coins + result.RankBonus;
            var awarded = result.Status == RewardStatus.Awarded;

            var reward = new MatchReward
            {
                Id = Guid.NewGuid(),
                PlayerId = player.Id,
                UserId = userId,
                RoomId = room.Id,
                RoomCode = room.Code,
                RoomTopic = room.Topic,
                FinishedAt = room.EndedAt ?? now,
                Status = result.Status,
                DenialReason = result.DenialReason,
                Score = player.Score,
                RewardRank = result.RewardRank,
                Xp = result.Xp,
                Coins = result.Coins,
                RankBonus = result.RankBonus,
            };
            snapshot.NewMatchRewards.Add(reward);

            if (awarded)
            {
                user.TotalXp += result.Xp;
                user.CoinBalance += coinsTotal;
                if (coinsTotal > 0)
                {
                    snapshot.NewCoinTransactions.Add(new CoinTransaction
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Amount = coinsTotal,
                        Reason = CoinTransactionReason.MatchReward,
                        RoomId = room.Id,
                        MatchRewardId = reward.Id,
                        CreatedAt = now,
                    });
                }
            }

            dtos.Add(ToDto(reward, awarded ? user.TotalXp : null, awarded ? LevelCalculator.FromXp(xpBefore) : null));
        }

        return dtos;
    }

    /// <summary>Thưởng đã lưu của phòng Finished (cho RoomState khi vào lại). Phòng cũ trước tính năng: mọi người là khách.</summary>
    public static IReadOnlyList<PlayerRewardDto> FromStored(GameSnapshot snapshot)
    {
        var rewardsByPlayer = snapshot.Rewards.Where(r => r.PlayerId is not null).ToDictionary(r => r.PlayerId!.Value);
        var dtos = new List<PlayerRewardDto>();
        foreach (var player in snapshot.Players)
        {
            if (rewardsByPlayer.TryGetValue(player.Id, out var reward))
            {
                // shortcut: TotalXp lấy từ số hiện tại của tài khoản, nên nếu người đó chơi thêm trận khác sau đó thì
                // LevelBefore/LeveledUp có thể lệch; nâng cấp khi cần lưu TotalXp tại thời điểm kết thúc vào MatchReward.
                int? totalXp = reward.Status == RewardStatus.Awarded && snapshot.Users.TryGetValue(reward.UserId, out var u) ? u.TotalXp : null;
                dtos.Add(ToDto(reward, totalXp, totalXp is { } t ? LevelCalculator.FromXp(t - reward.Xp) : null));
            }
            else if (player.UserId is null)
            {
                dtos.Add(Guest(player, 1 + snapshot.Players.Count(o => o.Score > player.Score)));
            }
        }

        return dtos;
    }

    private static PlayerRewardDto Guest(Player player, int rank) =>
        new(player.Id, false, "guest", rank, player.Score, 0, 0, 0, null, null, null, null, null);

    private static PlayerRewardDto ToDto(MatchReward r, int? totalXp, int? levelBefore)
    {
        var awarded = r.Status == RewardStatus.Awarded;
        int? levelAfter = totalXp is { } t ? LevelCalculator.FromXp(t) : null;
        return new PlayerRewardDto(
            r.PlayerId ?? Guid.Empty,
            awarded,
            awarded ? null : NotAwardedReason(r.DenialReason),
            r.RewardRank,
            r.Score,
            r.Xp,
            r.Coins,
            r.RankBonus,
            awarded && r.Score == 0 && r.RewardRank <= 3 ? "zero_score" : null,
            totalXp,
            levelBefore,
            levelAfter,
            levelBefore is { } b && levelAfter is { } a ? a > b : null);
    }

    private static string NotAwardedReason(RewardDenialReason? reason) => reason switch
    {
        RewardDenialReason.Host => "host",
        RewardDenialReason.NoAnswers => "no_answers",
        _ => "not_enough_accounts",
    };
}
