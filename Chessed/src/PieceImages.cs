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
    internal class PieceImages
    {
        static Dictionary<PieceType, int> WhiteImgs = new Dictionary<PieceType, int>()
        {
            { PieceType.Pawn, Resource.Drawable.pw },
            { PieceType.Rook, Resource.Drawable.rw },
            { PieceType.Knight, Resource.Drawable.nw },
            { PieceType.Bishop, Resource.Drawable.bw },
            { PieceType.Queen, Resource.Drawable.wq },
            { PieceType.King, Resource.Drawable.kw },
        };

        static Dictionary<PieceType, int> BlackImgs = new Dictionary<PieceType, int>()
        {
            { PieceType.Pawn, Resource.Drawable.pb },
            { PieceType.Rook, Resource.Drawable.rb },
            { PieceType.Knight, Resource.Drawable.nb },
            { PieceType.Bishop, Resource.Drawable.bb },
            { PieceType.Queen, Resource.Drawable.bq },
            { PieceType.King, Resource.Drawable.kb },
        };

        public static int GetImage(Player color, PieceType type)
        {
            return color switch
            {
                Player.White => WhiteImgs[type],
                Player.Black => BlackImgs[type],
                _ => 0
            };
        }

        public static int GetImage(Piece piece)
        {
            if (piece == null)
            {
                return 0;
            }

            return GetImage(piece.Color, piece.Type);
        }
    }
}