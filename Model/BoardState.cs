using System.Collections;

namespace Chessie.Model
{
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

    public struct BoardState : IEnumerable<PieceType>
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

        public readonly IEnumerator<PieceType> GetEnumerator()
        {
            return ((IEnumerable<PieceType>)Squares).GetEnumerator();
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return Squares.GetEnumerator();
        }

        private static readonly SquareCoord WQRookStart = new(0, 0);
        private static readonly SquareCoord WKRookStart = new(0, 7);
        private static readonly SquareCoord BQRookStart = new(7, 0);
        private static readonly SquareCoord BKRookStart = new(7, 7);
    }
}
