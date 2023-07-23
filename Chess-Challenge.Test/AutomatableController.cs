using System.Runtime.ExceptionServices;
using ChessChallenge.API;
using ChessChallenge.Application;
using ChessChallenge.Chess;
using ChessChallenge.Example;
using Board = ChessChallenge.Chess.Board;
using Move = ChessChallenge.Chess.Move;

namespace Chess_Challenge.Test;

public class AutomatableController
{
    private MoveGenerator _moveGenerator;
    private Board _board;
    private ChessPlayer _playerWhite;
    private ChessPlayer _playerBlack;
    private bool _isPlaying = false;
    private ChessPlayer PlayerToMove => _board.IsWhiteToMove ? _playerWhite : _playerBlack;
    private bool _botAPlaysWhite = false;
    
    public BotMatchStats BotStatsA { get; private set; }
    public BotMatchStats BotStatsB {get;private set;}

    public void StartNewMatch(IChessBot botA, ChallengeController.PlayerType botAType, IChessBot botB, ChallengeController.PlayerType botBType)
    {
        _moveGenerator = new MoveGenerator();
        _board = new Board();
        _board.LoadStartPosition();
        _botAPlaysWhite = true;
        string nameA = botAType.ToString();
        string nameB = botBType.ToString();
        BotStatsA = new BotMatchStats(nameA);
        BotStatsB = new BotMatchStats(nameB);
        
        _playerWhite = new ChessPlayer(botA, botAType, 60 * 1000);
        _playerBlack = new ChessPlayer(botB, botBType, 60 * 1000);
        _isPlaying = true;
        NotifyTurnToMove();
    }

    public void StartNextGame()
    {
        _isPlaying = true;
        NotifyTurnToMove();
    }

    private void NotifyTurnToMove()
    {
        var startThinkTime = DateTime.UtcNow;
        var move = GetBotMove();
        var endThinkTime = DateTime.UtcNow;
        PlayerToMove.UpdateClock((endThinkTime-startThinkTime).TotalMilliseconds);
        OnMoveChosen(move);
    }

    private Move GetBotMove()
    {
        ChessChallenge.API.Board botBoard = new(new(_board));
        ChessChallenge.API.Timer timer = new(PlayerToMove.TimeRemainingMs);
        ChessChallenge.API.Move move = PlayerToMove.Bot.Think(botBoard, timer);
        return new Move(move.RawValue);
    }

    private void OnMoveChosen(Move chosenMove)
    {
        if (!IsLegal(chosenMove))
        {
            var result = PlayerToMove == _playerWhite ? GameResult.WhiteIllegalMove : GameResult.BlackIllegalMove;
            EndGame(result);
        }
        
        PlayMove(chosenMove);
    }

    private void PlayMove(Move move)
    {
        if (_isPlaying)
        {
            _board.MakeMove(move, false);
            var result = Arbiter.GetGameState(_board);
            if (result == GameResult.InProgress)
            {
                NotifyTurnToMove();
            }
            else
            {
                EndGame(result);
            }
        }
    }
    
    bool IsLegal(Move givenMove)
    {
        var moves = _moveGenerator.GenerateMoves(_board);
        foreach (var legalMove in moves)
        {
            if (givenMove.Value == legalMove.Value)
            {
                return true;
            }
        }

        return false;
    }

    private void EndGame(GameResult result)
    {
        if (_isPlaying)
        {
            _isPlaying = false;
            UpdateBotMatchStats(result);
            _playerWhite.UpdateClock(0);
            _playerBlack.UpdateClock(0);
            _botAPlaysWhite = !_botAPlaysWhite;
            SwapPlayers();
            _board = new Board();
            _board.LoadStartPosition();
        }
    }

    private void SwapPlayers()
    {
        (_playerWhite, _playerBlack) = (_playerBlack, _playerWhite);
    }
    
    private void UpdateBotMatchStats(GameResult result)
    {
        UpdateStats(BotStatsA, _botAPlaysWhite);
        UpdateStats(BotStatsB, !_botAPlaysWhite);

        void UpdateStats(BotMatchStats stats, bool isWhiteStats)
        {
            // Draw
            if (Arbiter.IsDrawResult(result))
            {
                stats.NumDraws++;
            }
            // Win
            else if (Arbiter.IsWhiteWinsResult(result) == isWhiteStats)
            {
                stats.NumWins++;
            }
            // Loss
            else
            {
                stats.NumLosses++;
                stats.NumTimeouts += (result is GameResult.WhiteTimeout or GameResult.BlackTimeout) ? 1 : 0;
                stats.NumIllegalMoves += (result is GameResult.WhiteIllegalMove or GameResult.BlackIllegalMove) ? 1 : 0;
            }
        }
    }
    
    public struct BotMatchStats
    {
        public string BotName;
        public int NumWins;
        public int NumLosses;
        public int NumDraws;
        public int NumTimeouts;
        public int NumIllegalMoves;

        public BotMatchStats(string name) => BotName = name;


        public static BotMatchStats operator +(BotMatchStats statsA, BotMatchStats statsB)
        {
            var result = new BotMatchStats(statsA.BotName);
            result.NumWins = statsA.NumWins + statsB.NumWins;
            result.NumLosses = statsA.NumLosses + statsB.NumLosses;
            result.NumDraws = statsA.NumDraws + statsB.NumDraws;
            result.NumTimeouts = statsA.NumTimeouts + statsB.NumTimeouts;
            result.NumIllegalMoves = statsA.NumIllegalMoves + statsB.NumIllegalMoves;
            return result;
        } 
    }
}