using System.Text;

namespace Chessie.Model
{
    public readonly struct Move
    {
        private const int FILES_PER_RANK = 8;

        public readonly PieceType Piece;
        public readonly PieceType CapturedPiece;
        public readonly int Start;
        public readonly int End;
        public readonly int? CastlingRookStart;
        public readonly bool EnPassant;

        public readonly SquareCoord StartCoord => new(Start);
        public readonly SquareCoord EndCoord => new(End);

        public readonly int CastlingRookEnd
        {
            get
            {
                int kingDir = Math.Sign(End - Start);
                return End - kingDir;
            }
        }

        public readonly int EnPassantCapture => (End > Start) ? (End - FILES_PER_RANK) : (End + FILES_PER_RANK);

        public readonly bool IsPawnDoubleMove => Piece.IsPieceType(PieceType.Pawn) && (Math.Abs(End - Start) == (FILES_PER_RANK * 2));

        public Move(PieceType piece, PieceType targetPiece, int startIndex, int endIndex, int? rook = null, bool enPassant = false)
        {
            Piece = piece;
            CapturedPiece = targetPiece;
            Start = startIndex;
            End = endIndex;
            CastlingRookStart = rook;
            EnPassant = enPassant;
        }

        public Move(LocatedPiece piece, PieceType targetPiece, int endIndex, int? rook = null, bool enPassant = false)
            : this(piece.Piece, targetPiece, piece.Location, endIndex, rook, enPassant)
        { }

        public Move(PieceType piece, PieceType targetPiece, SquareCoord start, SquareCoord end, SquareCoord? rook = null, bool enPassant = false)
            : this(piece, targetPiece,
                  Board.SquareIndex(start),
                  Board.SquareIndex(end),
                  Board.SquareIndex(rook),
                  enPassant)
        { }

        public Move(PieceType piece, PieceType targetPiece, SquareCoord start, int dRank, int dFile, SquareCoord? rook = null, bool enPassant = false)
            : this(piece, targetPiece,
                  Board.SquareIndex(start),
                  Board.SquareIndex(start) + (dRank * FILES_PER_RANK) + dFile,
                  Board.SquareIndex(rook),
                  enPassant)
        { }

        public override string ToString()
        {
            return $"{Piece.TypeIcon()}{Start}→{End}";
        }
    }

    [Flags]
    public enum MoveType
    {
        Simple = 0,
        Capture = 1,
        EnPassant = 2 | Capture,
        Castle = 4,
        Promotion = 8,
        Check = 16,
    }

    public readonly struct MoveRecord
    {
        public readonly PieceType Piece;
        public readonly PieceType? Promotion;
        public readonly SquareCoord Origin;
        public readonly SquareCoord Destination;
        public readonly MoveType MoveType;
        public readonly string Algebraic { get; }
        public readonly string PrettyAlgebraic { get; }

        public MoveRecord(BoardState board, Move move, IEnumerable<Move> availableMoves, PieceType? promotion = null, bool isCheck = false, bool isMate = false)
        {
            Piece = board[move.Start];
            Origin = move.Start;
            Destination = move.End;
            Promotion = promotion;

            if (move.CastlingRookStart.HasValue)
            {
                MoveType = MoveType.Castle;
            }
            else
            {
                if ((board[move.End] != PieceType.Empty) || move.EnPassant)
                {
                    MoveType = move.EnPassant ? MoveType.EnPassant : MoveType.Capture;
                }
                else
                {
                    MoveType = MoveType.Simple;
                }

                if (promotion.HasValue) MoveType |= MoveType.Promotion;
            }

            if (isCheck) MoveType |= MoveType.Check;

            // generate algebraic
            var piece = board[move.Start];
            var destPiece = board[move.End];

            bool isCapture = (destPiece != PieceType.Empty) || move.EnPassant;

            if (!isCapture)
            {
                // pawn move
                if (piece.IsPieceType(PieceType.Pawn))
                {
                    string endSquare = move.End.ToString();
                    PrettyAlgebraic = Algebraic = promotion.HasValue ? $"{endSquare}={promotion.Value.TypeId()}" : endSquare;
                    return;
                }

                // castle
                if (move.CastlingRookStart.HasValue)
                {
                    PrettyAlgebraic = Algebraic = move.CastlingRookStart.Value.IsInFirstRank ? "O-O-O" : "O-O"; // queenside : kingside
                    return;
                }
            }
            else if (move.EnPassant)
            {
                PrettyAlgebraic = Algebraic = $"{move.Start.FileId}x{move.End} e.p.";
                return;
            }

            var ambiguousMoves = availableMoves.Where(m =>
                    (m.End == move.End) &&
                    (m.Start != move.Start) &&
                    (m.Piece == piece))
                .ToList();

            var sb = new StringBuilder();
            char? pieceChar = null;

            if (piece.IsPieceType(PieceType.Pawn))
            {
                sb.Append(move.Start.FileId);
            }
            else
            {
                pieceChar = piece.TypeId();
                sb.Append(pieceChar.Value);
            }

            if (ambiguousMoves.Any(m => m.Start.Rank == move.Start.Rank))
            {
                sb.Append(move.Start.FileId);
            }
            if (ambiguousMoves.Any(m => m.Start.File == move.Start.File))
            {
                sb.Append(move.Start.RankId);
            }

            if (isCapture) sb.Append('x');
            sb.Append(move.End.ToString());

            if (promotion.HasValue)
            {
                sb.Append('=');
                sb.Append(promotion.Value.TypeId());
            }

            if (isMate)
            {
                sb.Append('#');
            }
            else if (isCheck)
            {
                sb.Append('+');
            }

            Algebraic = sb.ToString();
            if (pieceChar.HasValue)
            {
                PrettyAlgebraic = Algebraic.Replace(pieceChar.Value, piece.TypeIcon());
            }
            else
            {
                PrettyAlgebraic = Algebraic;
            }
        }
    }
}
