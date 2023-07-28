using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using Chess_Challenge.My_Bot;
using ConsoleTables;

public class MyBot_First_Attempt : IChessBot
{
    private bool _iAmWhite = false;

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    private readonly int[] _pieceValues = { 0, 1, 3, 3, 5, 9, 100 };
    private (Square, Square)? _lastMove;

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
        var valuedMoves = moves.Select(m => new ValuedMove(m, EvalMove(m, board)));
        //DebugMoves(valuedMoves, board);
        var picked = valuedMoves.MaxBy(x => x.Val).Move;
        _lastMove = (picked.StartSquare, picked.TargetSquare);
        Console.WriteLine($"{picked.ToString()} with value {EvalMove(picked, board)}");
        return picked;
    }

    private int EvalMove(Move move, Board board)
    {
        if (MoveIsMate(move, board))
        {
            return int.MaxValue;
        }

        var result = ValueAsAttack(move, board);
        result += ValueCastling(move, board);
        result += ValueMoveFromBackRank(move, board);
        result += ValueMoveReversal(move);
        result += ValueControllingCentralPawns(move, board);
        result += ValueKeepingTheOptionToCastle(move, board);
        result += ValuePreventingChecks(move, board);
        result += ValueChecking(move, board);
        result += ValueNotGivingUpPieces(move, board);
        result += ValueNotLeavingPiecesHanging(move, board);
        return result;
    }

    private void DebugMoves(IEnumerable<ValuedMove> moves, Board board)
    {
        var table = new ConsoleTable("Move", "Piece", "Is Mate", "Attack Value", "Castle Value", "Back Rank", "Reversal", "Central Pawns", "Keep Castle Option", "Check Prevention", "Checking", "Giving Up Pieces", "Not Hanging", "Total");
        foreach (var m in moves.OrderByDescending(x => x.Val))
        {
            var move = m.Move;
            var mate = MoveIsMate(move, board);
            var attackVal = ValueAsAttack(move, board);
            var castleVal = ValueCastling(move, board);
            var backRankVal = ValueMoveFromBackRank(move, board);
            var reversalVal = ValueMoveReversal(move);
            var centralPawnsVal = ValueControllingCentralPawns(move, board);
            var castleOptionVal = ValueKeepingTheOptionToCastle(move, board);
            var checkPreventionVal = ValuePreventingChecks(move, board);
            var checkVal = ValueChecking(move, board);
            var givingUpPiecesVal = ValueNotGivingUpPieces(move, board);
            var notHangingVal = ValueNotLeavingPiecesHanging(move, board);
            var total = attackVal + castleVal + backRankVal + reversalVal + centralPawnsVal;

            table.AddRow(move.ToString(), move.MovePieceType.ToString(), mate, attackVal, castleVal, backRankVal, reversalVal, centralPawnsVal, castleOptionVal, checkPreventionVal, checkVal, givingUpPiecesVal, notHangingVal, total);
        }

        Console.WriteLine(table.ToString());
    }

    private int ValueNotGivingUpPieces(Move move, Board board)
    {
        var score = 0;
       board.MakeMove(move);
       if (board.GetLegalMoves().Any(opponentMove => opponentMove.TargetSquare == move.TargetSquare))
       {
           score = -2 * _pieceValues[(int)move.MovePieceType];
       }
       board.UndoMove(move);
       return score;
    }

    private int ValueNotLeavingPiecesHanging(Move move, Board board)
    {
        var score = 0;
        board.MakeMove(move);
        foreach (var opponentMove in board.GetLegalMoves())
        {
            var materialOfCapture = _pieceValues[(int)board.GetPiece(opponentMove.TargetSquare).PieceType];
            if (materialOfCapture > 1) score = -materialOfCapture;
        }
        
        board.UndoMove(move);
        return score;
    }
    
    private int ValueMoveReversal(Move move)
    {
        if (_lastMove != null)
        {
            if (move.TargetSquare == _lastMove.Value.Item1 && move.StartSquare == _lastMove.Value.Item2) return -5;
        }

        return 0;
    }

    private int ValueChecking(Move move, Board board)
    {
        var score = 0;
        board.MakeMove(move);
        if (board.IsInCheck())
        {
            score = 100;
        }
        if (board.GetLegalMoves().Any(opponentMove => opponentMove.TargetSquare == move.TargetSquare))
        {
            score = 0;
        }
        
        board.UndoMove(move);
        return score;
    }

    private int ValuePreventingChecks(Move move, Board board)
    {
        var score = 0;
        board.MakeMove(move);
        foreach (var opponentMove in board.GetLegalMoves())
        {
            board.MakeMove(opponentMove);
            if (board.IsInCheckmate())
            {
                score = -10000;
                board.UndoMove(opponentMove);
                break;
            }

            if (board.IsInCheck())
            {
                score-=5;
            }
            board.UndoMove(opponentMove);
        }
        board.UndoMove(move);
        return score;
    }

    private int ValueKeepingTheOptionToCastle(Move move, Board board)
    {
        if (move.IsCastles) return 0;
        var beforeCastleValue = board.HasKingsideCastleRight(_iAmWhite) ? 1 : 0;
        beforeCastleValue += board.HasQueensideCastleRight(_iAmWhite) ? 1 : 0;
        board.MakeMove(move);
        var afterCastleValue = board.HasKingsideCastleRight(_iAmWhite) ? 1 : 0;
        afterCastleValue += board.HasQueensideCastleRight(_iAmWhite) ? 1 : 0;
        board.UndoMove(move);
        return afterCastleValue - beforeCastleValue;

    }

    private int ValueMoveFromBackRank(Move move, Board board)
    {
        var backRank = _iAmWhite ? 0 : 7;
        if (move.MovePieceType != PieceType.King && move.StartSquare.Rank == backRank && move.TargetSquare.Rank != backRank) return _pieceValues[(int)move.MovePieceType];
        if (move.MovePieceType != PieceType.King && move.TargetSquare.Rank == backRank) return -10;
        return 0;
    }

    private int ValueControllingCentralPawns(Move move, Board board)
    {
        if (board.PlyCount < 10 && move is { MovePieceType: PieceType.Pawn, TargetSquare.Rank: >= 2 and <= 5, TargetSquare.File: 3 or 4, }) return 10;
        return 0;
    }

    private int ValueAsAttack(Move move, Board board)
    {
        var materialOfTargetPiece = _pieceValues[(int)board.GetPiece(move.TargetSquare).PieceType];
        var myMaterialAttackingSquare = ValueAttackers(move.TargetSquare, board);
        board.MakeMove(move);
        var theirMaterialAttackingSquare = ValueAttackers(move.TargetSquare, board);
        board.UndoMove(move);

        return (myMaterialAttackingSquare + materialOfTargetPiece) - theirMaterialAttackingSquare;
    }


    private int ValueAttackers(Square targetSquare, Board board)
    {
        var result = 0;
        foreach (var myMove in board.GetLegalMoves())
        {
            if (targetSquare == myMove.TargetSquare && myMove.MovePieceType != PieceType.King) result += _pieceValues[(int)myMove.MovePieceType];
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

    private int ValueCastling(Move move, Board board)
    {
        if (!move.IsCastles) return 0;
        board.MakeMove(move);
        var castleMovesRookToOpenFile = false;
        var pawnsInFrontOfKing = 0;

        if (_iAmWhite)
        {
            var d1 = board.GetPiece(new Square("d1"));
            var f1 = board.GetPiece(new Square("f1"));
            // Rook should end up on d1 or f1;
            if (d1.PieceType == PieceType.Rook)
            {
                castleMovesRookToOpenFile = FileIsOpen(3, board);
                if (PawnOnSquare("b2", board)) pawnsInFrontOfKing++;
                if (PawnOnSquare("c2", board)) pawnsInFrontOfKing++;
            }
            else if (f1.PieceType == PieceType.Rook)
            {
                castleMovesRookToOpenFile = FileIsOpen(5, board);
                if (PawnOnSquare("f2", board)) pawnsInFrontOfKing++;
                if (PawnOnSquare("g2", board)) pawnsInFrontOfKing++;
                if (PawnOnSquare("h2", board)) pawnsInFrontOfKing++;
            }
        }
        else
        {
            var d8 = board.GetPiece(new Square("d8"));
            var f8 = board.GetPiece(new Square("d8"));
            // Rook should end up on d8 or f8;
            if (d8.PieceType == PieceType.Rook)
            {
                castleMovesRookToOpenFile = FileIsOpen(3, board);
                if (PawnOnSquare("b7", board)) pawnsInFrontOfKing++;
                if (PawnOnSquare("c7", board)) pawnsInFrontOfKing++;
            }
            else if (f8.PieceType == PieceType.Rook)
            {
                castleMovesRookToOpenFile = FileIsOpen(5, board);
                if (PawnOnSquare("f7", board)) pawnsInFrontOfKing++;
                if (PawnOnSquare("g7", board)) pawnsInFrontOfKing++;
                if (PawnOnSquare("h7", board)) pawnsInFrontOfKing++;
            }
        }

        board.UndoMove(move);

        return pawnsInFrontOfKing + (castleMovesRookToOpenFile ? 1 : 0);
    }

    private bool FileIsOpen(int file, Board board)
    {
        for (var rank = 1; rank < 6; ++rank)
        {
            if (board.GetPiece(new Square(file, rank)).IsWhite == _iAmWhite) return true;
        }

        return false;
    }

    private bool PawnOnSquare(string square, Board board)
    {
        return board.GetPiece(new Square(square)).PieceType == PieceType.Pawn;
    }
}