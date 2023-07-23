using System.Collections.Concurrent;
using ChessChallenge.API;
using ChessChallenge.Application;
using ChessChallenge.Chess;
using ChessChallenge.Example;
using FluentAssertions;

namespace Chess_Challenge.Test;

public class Tests
{
    [Test]
    public void MyBotIsBetter()
    {
        var (myStats, theirStats) = PlayXGamesInParallel(10000);

        myStats.NumIllegalMoves.Should().Be(0, "The bot should not make illegal moves");
        myStats.NumTimeouts.Should().Be(0, "The bot should run fast enough");
        myStats.NumLosses.Should().BeLessThan(theirStats.NumLosses, "Less losses is better");
        myStats.NumWins.Should().BeGreaterThan(theirStats.NumWins, "More wins is even better");
    }

    public (AutomatableController.BotMatchStats myBotStats, AutomatableController.BotMatchStats evilBotStats) PlayXGamesInParallel(int gamesToPlay)
    {
        var range = Partitioner.Create(0, gamesToPlay);
        var myBotStats = new ConcurrentBag<AutomatableController.BotMatchStats>();
        var theirBotStats = new ConcurrentBag<AutomatableController.BotMatchStats>();
        Parallel.ForEach(range, new ParallelOptions{MaxDegreeOfParallelism = Environment.ProcessorCount/2}, (r, loopState) =>
        {
            var controller = new AutomatableController();
            var botA = new MyBot();
            var botAType = ChallengeController.PlayerType.MyBot;
            var botB = new EvilBot();
            var botBType = ChallengeController.PlayerType.EvilBot;

            controller.StartNewMatch(botA, botAType, botB, botBType);

            for (int i = r.Item1; i < r.Item2; ++i)
            {
                controller.StartNextGame();
            }
            
            myBotStats.Add(controller.BotStatsA);
            theirBotStats.Add(controller.BotStatsB);
        });

        return (SumStats(myBotStats), SumStats(theirBotStats));
    }

    public (AutomatableController.BotMatchStats myBotStats, AutomatableController.BotMatchStats evilBotStats) PlayXGames(int gamesToPlay)
    {
        var controller = new AutomatableController();
        var botA = new MyBot();
        var botAType = ChallengeController.PlayerType.MyBot;
        var botB = new EvilBot();
        var botBType = ChallengeController.PlayerType.EvilBot;
        
        controller.StartNewMatch(botA, botAType, botB, botBType);
        
        for (int i = 0; i < gamesToPlay; i++)
        {
            controller.StartNextGame();
        }

        return (controller.BotStatsA, controller.BotStatsB);
    }

    public AutomatableController.BotMatchStats SumStats(IEnumerable<AutomatableController.BotMatchStats> allStats)
    {
        var result = new AutomatableController.BotMatchStats();
        foreach (var stats in allStats)
        {
            result += stats;
        }

        return result;
    }
}