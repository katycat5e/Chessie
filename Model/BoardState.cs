using System;
using System.Diagnostics.CodeAnalysis;

namespace Chessie.Model
{
    public readonly struct SquareCoord
    {
        public const int MIN_RANK = 0;
        public const int MAX_RANK = 7;
        
        public const int MIN_FILE = 0;
        public const int MAX_FILE = 7;

        public readonly int Rank;
        public readonly int File;

        public readonly bool IsInFirstRank => Rank == MIN_RANK;
        public readonly bool IsInLastRank => Rank == MAX_RANK;

        public readonly bool IsInFirstFile => File == MIN_FILE;
        public readonly bool IsInLastFile => File == MAX_FILE;

        public readonly char RankId => (char)('1' + Rank);
        public readonly char FileId => (char)('a' + File);

        public readonly bool IsValidSquare =>
            (Rank >= MIN_RANK) && (Rank <= MAX_RANK) &&
            (File >= MIN_FILE) && (File <= MAX_FILE);

        public SquareCoord(int rank, int file)
        {
            Rank = rank;
            File = file;
        }

        public static bool operator ==(SquareCoord left, SquareCoord right) => (left.Rank == right.Rank) && (left.File == right.File);
        public static bool operator !=(SquareCoord left, SquareCoord right) => (left.Rank != right.Rank) || (left.File != right.File);

        public static SquareCoord operator +(SquareCoord left, SquareCoord right) => new(left.Rank + right.Rank, left.File + right.File);
        public static SquareCoord operator -(SquareCoord left, SquareCoord right) => new(left.Rank - right.Rank, left.File - right.File);

        public static SquareCoord operator *(SquareCoord coord, int multiplier) => new(coord.Rank * multiplier, coord.File * multiplier);

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return (obj is SquareCoord coord) && (this == coord);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Rank, File);
        }

        public override string ToString()
        {
            return $"{FileId}{RankId}";
        }

        public static IEnumerable<SquareCoord> AllSquares { get; } =
            Enumerable.Range(0, 8)
            .SelectMany(rank => Enumerable.Range(0, 8).Select(file => new SquareCoord(rank, file))).ToArray();
    }

    [Flags]
    public enum CastleState : byte
    {
        None = 0,

        WhiteKingside = 1,
        WhiteQueenside = 2,

        BlackKingside = 4,
        BlackQueenside = 8,

        AllWhite = WhiteKingside | WhiteQueenside,
        AllBlack = BlackKingside | BlackQueenside,
        All = AllWhite | AllBlack,
    }

    public struct BoardState
    {
        public PieceType[] Squares;
        public CastleState CastleState;
        public int MoveNumber;
        public SquareCoord? EnPassantSquare;
        public bool BlackToMove;

        public BoardState()
        {
            Squares = new PieceType[64];
            CastleState = CastleState.None;
            MoveNumber = 1;
            EnPassantSquare = null;
            BlackToMove = false;
        }

        public BoardState(
            PieceType[] squares, 
            CastleState castleState = CastleState.None,
            int moveNumber = 1,
            SquareCoord? enPassantSquare = null,
            bool blackToMove = false)
        {
            if (squares.Length != 64) throw new ArgumentException("Not a valid set of square states");

            Squares = squares;
            CastleState = castleState;
            MoveNumber = moveNumber;
            EnPassantSquare = enPassantSquare;
            BlackToMove = blackToMove;
        }

        public readonly PieceType this[int rank, int file]
        {
            get => Squares[rank * 8 + file];
            set => Squares[rank * 8 + file] = value;
        }

        public readonly PieceType this[SquareCoord coord]
        {
            get => Squares[coord.Rank * 8 + coord.File];
            set => Squares[coord.Rank * 8 + coord.File] = value;
        }

        public readonly SquareCoord GetKingPosition(bool black)
        {
            var searchPiece = PieceType.King | (black ? PieceType.Black : PieceType.White);
            int index = Array.IndexOf(Squares, searchPiece);
            return new SquareCoord(index / 8, index % 8);
        }

        public readonly BoardState ApplyMove(Move move, PieceType? promotion = null)
        {
            var pieceInfo = this[move.Start];

            var newState = new BoardState
            {
                BlackToMove = !BlackToMove,
                CastleState = CastleState,
                MoveNumber = BlackToMove ? (MoveNumber + 1) : MoveNumber,
            };

            Array.Copy(Squares, newState.Squares, 64);
            newState[move.Start] = PieceType.Empty;

            newState[move.End] = promotion ?? pieceInfo;

            if (move.CastlingRookStart.HasValue)
            {
                var rook = newState[move.CastlingRookStart.Value];
                newState[move.CastlingRookStart.Value] = PieceType.Empty;
                newState[move.CastlingRookEnd] = rook;
            }
            else if (move.EnPassant)
            {
                newState[move.EnPassantCapture] = PieceType.Empty;
            }

            // set en passant flag
            if (pieceInfo.IsPieceType(PieceType.Pawn) && (Math.Abs(move.DeltaRank) == 2))
            {
                newState.EnPassantSquare = new SquareCoord(move.End.Rank - Math.Sign(move.DeltaRank), move.End.File);
            }

            // castling flags
            if (pieceInfo.HasFlag(PieceType.King))
            {
                CastleState mask = BlackToMove ? CastleState.AllBlack : CastleState.AllWhite;
                newState.CastleState = CastleState & ~mask;
            }
            else if (pieceInfo.HasFlag(PieceType.Rook))
            {
                if (move.Start == WKRookStart)
                {
                    newState.CastleState &= ~CastleState.WhiteKingside;
                }
                else if (move.Start == WQRookStart)
                {
                    newState.CastleState &= ~CastleState.WhiteQueenside;
                }
                else if (move.Start == BKRookStart)
                {
                    newState.CastleState &= ~CastleState.BlackKingside;
                }
                else if (move.Start == BQRookStart)
                {
                    newState.CastleState &= ~CastleState.BlackQueenside;
                }
            }

            return newState;
        }

        private static readonly SquareCoord WQRookStart = new(0, 0);
        private static readonly SquareCoord WKRookStart = new(0, 7);
        private static readonly SquareCoord BQRookStart = new(7, 0);
        private static readonly SquareCoord BKRookStart = new(7, 7);
    }
}
