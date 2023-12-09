using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chessed
{
    //class Piece
    //{
    //    public enum PieceType
    //    {
    //        Pawn,
    //        Rook,
    //        Knight,
    //        Bishop,
    //        Queen,
    //        King,
    //        air
    //    }

    //    public enum PieceColor
    //    {
    //        Black,
    //        White,
    //        neutral
    //    }

    //    public PieceType type;
    //    public PieceColor color;

    //    public bool HasMoved {
    //        get
    //        {
    //            return this.HasMoved;
    //        }
    //        set
    //        {
    //            if (this.HasMoved == false) return;

    //            this.HasMoved = value;
    //        }
    //    }

    //    public Piece(PieceColor color, PieceType pieceType) { }

    //    public Piece()
    //    {
    //        type = PieceType.air;
    //        color = PieceColor.neutral;
    //    }

    //    public int[,] CalculateMoves(Piece[,] board, int row, int col)
    //    {
    //        switch (type)
    //        {
    //            case PieceType.Pawn:
    //                return CalculatePawnMoves(board, row, col);
    //            default:
    //                return new int[8,8];
    //        }


    //    }

    //    int[,] CalculatePawnMoves(Piece[,] board, int row, int col)
    //    {
    //        int[,] toReturn = new int[8,8];
    //        if (board[row,col] != this) return toReturn;

    //        if (board[row+1,col].type == PieceType.air)
    //        {
    //            toReturn[row + 1, col] = 1;
    //        }

    //        if (!HasMoved && board[row + 2, col].type == PieceType.air)
    //        {
    //            toReturn[row + 2, col] = 1;
    //        }

    //        return toReturn;
    //    }
    ////}
}