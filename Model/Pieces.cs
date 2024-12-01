﻿using System;
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

        public static bool IsAnyPiece(this PieceType square)
        {
            return (square & PieceType.PieceMask) != 0;
        }

        public static bool IsPieceType(this PieceType piece, PieceType type)
        {
            return piece.HasFlag(type);
        }

        public static PieceType GetUncoloredType(this PieceType piece)
        {
            return piece & PieceType.PieceMask;
        }

        public static PieceType GetColor(this PieceType piece)
        {
            return piece & PieceType.ColorMask;
        }

        public static bool IsWhitePiece(this PieceType piece)
        {
            return piece.HasFlag(PieceType.White) && piece.IsAnyPiece();
        }

        public static bool IsBlackPiece(this PieceType piece)
        {
            return piece.HasFlag(PieceType.Black) && piece.IsAnyPiece();
        }

        public static bool IsOwnPiece(this PieceType piece, bool blackToMove)
        {
            var ownColor = blackToMove ? PieceType.Black : PieceType.White;
            return piece.HasFlag(ownColor);
        }

        public static bool IsOpponentPiece(this PieceType piece, bool blackToMove)
        {
            var opponentColor = blackToMove ? PieceType.White : PieceType.Black;
            return piece.HasFlag(opponentColor);
        }

        public static bool IsOpponentPiece(this PieceType piece, PieceType target)
        {
            return target.IsAnyPiece() && (piece.GetColor() != target.GetColor());
        }

        public static char TypeId(this PieceType piece)
        {
            return piece.GetUncoloredType() switch
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

        public static char TypeIcon(this PieceType piece)
        {
            if (piece.IsWhitePiece())
            {
                return piece.GetUncoloredType() switch
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

            return piece.GetUncoloredType() switch
            {
                PieceType.Pawn => '♟',
                PieceType.Knight => '♞',
                PieceType.Bishop => '♝',
                PieceType.Rook => '♜',
                PieceType.Queen => '♛',
                PieceType.King => '♚',
                _ => throw new NotImplementedException()
            };
        }
    }
}
