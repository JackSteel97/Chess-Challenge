using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class EvilBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    private readonly int[] _pieceValues = { 0, 1, 3, 3, 5, 9, 200 };
    
    private bool _iAmWhite;

    public Move Think(Board board, Timer timer)
    {
        if (board.PlyCount == 0 && board.IsWhiteToMove)
        {
            _iAmWhite = true;
            return new Move("e2e4", board);
        }

        return RootNegaMax(board);
    }

    private static Move RootNegaMax(Board board)
    {
        var maxMove = Move.NullMove;
        var max = double.NegativeInfinity;
        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves);
        foreach (var move in moves)
        {
            board.MakeMove(move);
            var score = NegaMax(board, 2);
            board.UndoMove(move);
            if (score > max)
            {
                max = score;
                maxMove = move;
            }
        }

        return maxMove;
    }

    private static double NegaMax(Board board, int depth)
    {
        if (depth == 0) return Evaluate(board);
        var max = double.NegativeInfinity;
        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves);
        foreach (var move in moves)
        {
            board.MakeMove(move);
            var score = -NegaMax(board, depth - 1);
            board.UndoMove(move);
            if (score > max)
            {
                max = score;
            }
        }

        return max;
    }

    private static double Evaluate(Board board)
    {
        return (MaterialScore(board) + MobilityScore(board)) * (board.IsWhiteToMove ? 1 : -1);
    }

    private static double MaterialScore(Board board)
    {
        var pieces = board.GetAllPieceLists();
        
        return 9 * (pieces[4].Count - pieces[10].Count)
               + 5 * (pieces[3].Count - pieces[9].Count)
               + 3 * ((pieces[2].Count - pieces[8].Count) + (pieces[1].Count - pieces[7].Count))
               + (pieces[0].Count - pieces[6].Count);
    }

    private static double MobilityScore(Board board)
    {
        var aIsWhite = board.IsWhiteToMove;
        Span<Move> movesA = stackalloc Move[256];
        Span<Move> movesB = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref movesA);
        board.ForceSkipTurn();
        board.GetLegalMovesNonAlloc(ref movesB);
        board.UndoSkipTurn();

        var whiteMobility = aIsWhite ? movesA.Length : movesB.Length;
        var blackMobility = aIsWhite ? movesB.Length : movesA.Length;
        return 0.1 * (whiteMobility - blackMobility);
    }
}