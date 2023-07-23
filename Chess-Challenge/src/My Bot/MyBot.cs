using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    private bool _iAmWhite = false;
    
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    private readonly int[] _pieceValues = { 0, 1, 3, 3, 5, 9, 100 };
    
    public Move Think(Board board, Timer timer)
    {
        if (board.PlyCount == 0 && board.IsWhiteToMove)
        {
            _iAmWhite = true;
            return new Move("e2e4", board);
        }
        Move[] moves = board.GetLegalMoves();
        return PickBestMove(moves, board);
    }

    private Move PickBestMove(Move[] moves, Board board)
    {
        var picked = moves.MaxBy(x => EvalMove(x, board));
        //Console.WriteLine($"{picked.ToString()} with value {EvalMove(picked, board)}");
        return picked;
    }

    private int EvalMove(Move move, Board board)
    {
        //Console.WriteLine($"{move.ToString()}");
        if (MoveIsMate(move, board))
        {
            return int.MaxValue;
        }
        var result = ValueAsAttack(move, board);
        
        
        //Console.WriteLine($"{move.ToString()} is valued at {result}");
        return result;
    }

    private int ValueAsAttack(Move move, Board board)
    {
        var targetSquareIsAttackedByXMine = ValueAttackers(move.TargetSquare, board);
        board.MakeMove(move);
        var targetSquareIsAttackedByXOpponents = ValueAttackers(move.TargetSquare, board);
        board.UndoMove(move);
        var additionalLoss = (_pieceValues[(int)board.GetPiece(move.TargetSquare).PieceType] - _pieceValues[(int)move.MovePieceType]);
        //Console.WriteLine($"{move.ToString()}: mineValue = {targetSquareIsAttackedByXMine}, theirValue = {targetSquareIsAttackedByXOpponents}, additional = {additionalLoss}");

        return (targetSquareIsAttackedByXMine - targetSquareIsAttackedByXOpponents) + additionalLoss;
    }


    private int ValueAttackers(Square targetSquare, Board board)
    {
        var result = 0;
        foreach (var myMove in board.GetLegalMoves())
        {
            if (targetSquare == myMove.TargetSquare) result+= _pieceValues[(int)myMove.MovePieceType];
        }

        return result;
    }

    private bool MoveIsMate(Move move, Board board)
    {
        board.MakeMove(move);
        var isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}