using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
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

        return RootAlphaBeta(board);
    }

    private Move RootAlphaBeta(Board board)
    {
        var bestMove = Move.NullMove;
        var bestScore = double.NegativeInfinity;
        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves);
        foreach (var move in moves)
        {
            board.MakeMove(move);
            var score = -AlphaBeta(board, double.NegativeInfinity, double.PositiveInfinity, 1);
            board.UndoMove(move);
            if (score >= bestScore)
            {
                bestMove = move;
                bestScore = score;
            }
        }

        return bestMove;
    }
    private double AlphaBeta(Board board, double alpha, double beta, int depthLeft)
    {
        if (depthLeft == 0) return Quiesce(board, alpha, beta);
        
        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves);
        foreach (var move in moves)
        {
            board.MakeMove(move);
            var score = -AlphaBeta(board, -beta, -alpha, depthLeft - 1);
            board.UndoMove(move);
            if (score >= beta) return beta;
            if (score > alpha) alpha = score;
        }

        return alpha;
    }

    private double Quiesce(Board board, double alpha, double beta, int depth = 0)
    {
        var standPat = Evaluate(board);
        if (standPat >= beta || depth > 3) return beta;

        if (alpha < standPat)
            alpha = standPat;

        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves, true);
        OrderMoves(ref moves);
        foreach (var capture in moves)
        {
            board.MakeMove(capture);
            var score = -Quiesce(board, alpha, beta, depth+1);
            board.UndoMove(capture);
            if (score >= beta) return beta;
            if (score > alpha) alpha = score;
        }

        return alpha;
    }
    
    private void OrderMoves(ref Span<Move> moves)
    {
        double[] scores = new double[moves.Length];
        for (int i = 0; i < moves.Length; ++i)
        {
            if (moves[i].CapturePieceType != PieceType.None)
                scores[i] = 10 * _pieceValues[(int)(moves[i].CapturePieceType)] - _pieceValues[(int)(moves[i].MovePieceType)];
            else
                scores[i] = double.NegativeInfinity;

            scores[i] = -scores[i];
        }
        Move[] movesArray = moves.ToArray();
        Array.Sort(scores, movesArray);
        moves = movesArray.AsSpan();

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