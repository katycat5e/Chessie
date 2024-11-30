using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chessie.Model
{
    public class Board
    {
        public PieceType[] Squares { get; }

        public PieceType this[int index] => Squares[index];
        public PieceType this[int rank, int file] => Squares[(rank * 8) + file];

        public CastleState CastleState { get; private set; }
        public int PlyNumber { get; private set; }
        public int? EnPassantSquare { get; private set; }
        public bool BlackToMove { get; private set; }

        private readonly Stack<Move> _moveHistory = new();

        public PieceMap WhitePieces { get; }
        public PieceMap BlackPieces { get; }

        public PieceMap GetMap(PieceType color) => color.HasFlag(PieceType.Black) ? BlackPieces : WhitePieces;
        public PieceMap GetMap(bool forBlack) => forBlack ? BlackPieces : WhitePieces;

        public PieceMap GetOpponentMap(PieceType color) => color.HasFlag(PieceType.Black) ? WhitePieces : BlackPieces;
        public PieceMap GetOpponentMap(bool forBlack) => forBlack ? WhitePieces : BlackPieces;

        public IEnumerable<LocatedPiece> EnumeratePieces(bool forBlack)
        {
            return forBlack ? BlackPieces : WhitePieces;
        }

        private ulong _threatsForWhite = 0;
        private ulong _threatsForBlack = 0;

        public ulong ThreatsForWhite
        {
            get
            {
                if (_threatsForWhite == 0) GenerateThreatMaps();
                return _threatsForWhite;
            }
        }

        public ulong ThreatsForBlack
        {
            get
            {
                if (_threatsForBlack == 0) GenerateThreatMaps();
                return _threatsForBlack;
            }
        }

        public Board()
        {
            Squares = new PieceType[64];
            Array.Copy(START_STATE, Squares, 64);

            WhitePieces = new PieceMap(Squares, PieceType.White);
            BlackPieces = new PieceMap(Squares, PieceType.Black);
        }

        public void ApplyMove(Move move)
        {
            Squares[move.Start] = PieceType.Empty;
            var capture = Squares[move.End];
            Squares[move.End] = move.Piece;

            var toMoveMap = GetMap(move.Piece);
            toMoveMap.MovePiece(move.Piece, move);

            // en passant
            if (move.EnPassant)
            {
                Squares[move.EnPassantCapture] = PieceType.Empty;
                GetOpponentMap(move.Piece).RemovePiece(PieceType.Pawn, move.EnPassantCapture);
            }
            // capture
            else if (capture.IsAnyPiece())
            {
                GetMap(capture).RemovePiece(capture, move.End);
            }
            // castle
            else if (move.CastlingRookStart.HasValue)
            {
                var rook = Squares[move.CastlingRookStart.Value];
                Squares[move.CastlingRookStart.Value] = PieceType.Empty;
                Squares[move.CastlingRookEnd] = rook;
            }
            else if (move.IsPawnDoubleMove)
            {
                EnPassantSquare = move.EnPassantCapture;
            }

            _moveHistory.Push(move);
        }

        public void UndoLastMove()
        {
            var move = _moveHistory.Pop();

            // do the thing
        }

        private static void GenerateThreatMaps()
        {

        }

        public static int SquareIndex(SquareCoord coord) => (coord.Rank << 3) | coord.File;

        public static int? SquareIndex(SquareCoord? coord) => coord.HasValue ? ((coord.Value.Rank << 3) | coord.Value.File) : null;

        private static readonly PieceType[] START_STATE =
        {
            Piece.R, Piece.N, Piece.B, Piece.Q, Piece.K, Piece.B, Piece.N, Piece.R,
            Piece.P, Piece.P, Piece.P, Piece.P, Piece.P, Piece.P, Piece.P, Piece.P,

            Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X,
            Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X,
            Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X,
            Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X,

            Piece.p, Piece.p, Piece.p, Piece.p, Piece.p, Piece.p, Piece.p, Piece.p,
            Piece.r, Piece.n, Piece.b, Piece.q, Piece.k, Piece.b, Piece.n, Piece.r,
        };
    }
}
