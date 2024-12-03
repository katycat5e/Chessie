using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chessie.Model
{
    [Flags]
    public enum PieceType : byte
    {
        Empty = 0,

        Pawn = 1,
        Knight = 2,
        Bishop = 4,
        Rook = 8,
        Queen = 16,
        King = 32,

        White = 64,
        Black = 128,

        PieceMask = Pawn | Knight | Bishop | Rook | Queen | King,
        BlackPieceMask = Black | PieceMask,
        WhitePieceMask = White | PieceMask,
        ColorMask = White | Black,
    }

    public static class Piece
    {
        public const PieceType X = PieceType.Empty;

        public const PieceType P = PieceType.White | PieceType.Pawn;
        public const PieceType N = PieceType.White | PieceType.Knight;
        public const PieceType B = PieceType.White | PieceType.Bishop;
        public const PieceType R = PieceType.White | PieceType.Rook;
        public const PieceType Q = PieceType.White | PieceType.Queen;
        public const PieceType K = PieceType.White | PieceType.King;

        public const PieceType p = PieceType.Black | PieceType.Pawn;
        public const PieceType n = PieceType.Black | PieceType.Knight;
        public const PieceType b = PieceType.Black | PieceType.Bishop;
        public const PieceType r = PieceType.Black | PieceType.Rook;
        public const PieceType q = PieceType.Black | PieceType.Queen;
        public const PieceType k = PieceType.Black | PieceType.King;

        public static bool IsOpponentPiece(this PieceType piece, PieceType target)
        {
            return (piece != PieceType.Empty) && (target != PieceType.Empty) &&
                (piece & PieceType.ColorMask) != (target & PieceType.ColorMask);
        }

        public static char TypeId(this PieceType piece)
        {
            return (piece & PieceType.PieceMask) switch
            {
                PieceType.Knight => 'N',
                PieceType.Bishop => 'B',
                PieceType.Rook => 'R',
                PieceType.Queen => 'Q',
                PieceType.King => 'K',
                PieceType.Pawn => 'P',
                _ => throw new NotImplementedException()
            };
        }

        public static char FenId(this PieceType piece)
        {
            return piece switch
            {
                P => 'P',
                N => 'N',
                B => 'B',
                R => 'R',
                Q => 'Q',
                K => 'K',

                p => 'p',
                n => 'n',
                b => 'b',
                r => 'r',
                q => 'q',
                k => 'k',

                _ => '-',
            };
        }

        public static char TypeIcon(this PieceType piece)
        {
            if ((piece & PieceType.White) != 0)
            {
                return (piece & PieceType.PieceMask) switch
                {
                    PieceType.Pawn => '♙',
                    PieceType.Knight => '♘',
                    PieceType.Bishop => '♗',
                    PieceType.Rook => '♖',
                    PieceType.Queen => '♕',
                    PieceType.King => '♔',
                    _ => throw new NotImplementedException()
                };
            }

            return (piece & PieceType.PieceMask) switch
            {
                PieceType.Pawn => '♟',
                PieceType.Knight => '♞',
                PieceType.Bishop => '♝',
                PieceType.Rook => '♜',
                PieceType.Queen => '♛',
                PieceType.King => '♚',
                PieceType.Empty => '-',
                _ => throw new NotImplementedException()
            };
        }

        public const int PAWN_VALUE = 100;
        public const int KNIGHT_VALUE = 300;
        public const int BISHOP_VALUE = 300;
        public const int ROOK_VALUE = 500;
        public const int QUEEN_VALUE = 900;

        public static int SignedValue(PieceType piece)
        {
            return piece switch
            {
                P => PAWN_VALUE,
                N => KNIGHT_VALUE,
                B => BISHOP_VALUE,
                R => ROOK_VALUE,
                Q => QUEEN_VALUE,

                p => -PAWN_VALUE,
                n => -KNIGHT_VALUE,
                b => -BISHOP_VALUE,
                r => -ROOK_VALUE,
                q => -QUEEN_VALUE,
                _ => 0,
            };
        }

        public static int UnsignedValue(PieceType piece)
        {
            return (piece & PieceType.PieceMask) switch
            {
                PieceType.Pawn => PAWN_VALUE,
                PieceType.Knight => KNIGHT_VALUE,
                PieceType.Bishop => BISHOP_VALUE,
                PieceType.Rook => ROOK_VALUE,
                PieceType.Queen => QUEEN_VALUE,

                _ => 0,
            };
        }
    }
}
