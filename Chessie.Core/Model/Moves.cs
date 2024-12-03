using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Chessie.Core.Model
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

        public readonly int StartRank => Start >> 3;
        public readonly int StartFile => Start & 7;

        public readonly int EndRank => End >> 3;
        public readonly int EndFile => End & 7;

        public readonly int CastlingRookEnd
        {
            get
            {
                int kingDir = Math.Sign(End - Start);
                return End - kingDir;
            }
        }

        public readonly int EnPassantCapture => (End > Start) ? (End - FILES_PER_RANK) : (End + FILES_PER_RANK);

        public readonly bool IsPawnDoubleMove => ((Piece & PieceType.Pawn) != 0) && (Math.Abs(End - Start) == (FILES_PER_RANK * 2));

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
                  start.Index,
                  end.Index,
                  rook?.Index,
                  enPassant)
        { }

        public Move(PieceType piece, PieceType targetPiece, SquareCoord start, int dRank, int dFile, SquareCoord? rook = null, bool enPassant = false)
            : this(piece, targetPiece,
                  start.Index,
                  start.Index + (dRank * FILES_PER_RANK) + dFile,
                  rook?.Index,
                  enPassant)
        { }

        public override string ToString()
        {
            return $"{Piece.TypeIcon()}{new SquareCoord(Start)}→{new SquareCoord(End)}";
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not Move other) return false;

            return (Piece == other.Piece) && (Start == other.Start) && (End == other.End);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Piece, Start, End);
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
        public readonly PieceType CapturedPiece;
        public readonly PieceType? Promotion;
        public readonly SquareCoord Origin;
        public readonly SquareCoord Destination;
        public readonly MoveType MoveType;
        public readonly string Algebraic { get; }
        public readonly string PrettyAlgebraic { get; }

        public MoveRecord(Move move, IEnumerable<Move> availableMoves, PieceType? promotion = null, bool isCheck = false, bool isMate = false)
        {
            Piece = move.Piece;
            CapturedPiece = move.CapturedPiece;

            Origin = new SquareCoord(move.Start);
            Destination = new SquareCoord(move.End);
            Promotion = promotion;

            bool isCapture = (CapturedPiece != PieceType.Empty) || move.EnPassant;

            if (move.CastlingRookStart.HasValue)
            {
                MoveType = MoveType.Castle;
            }
            else
            {
                if (isCapture)
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
            if (!isCapture)
            {
                // pawn move
                if ((Piece & PieceType.Pawn) != 0)
                {
                    string endSquare = Destination.ToString();
                    PrettyAlgebraic = Algebraic = promotion.HasValue ? $"{endSquare}={promotion.Value.TypeId()}" : endSquare;
                    return;
                }

                // castle
                if (move.CastlingRookStart.HasValue)
                {
                    // queenside == a file rook
                    PrettyAlgebraic = Algebraic = ((move.CastlingRookStart.Value & 7) == 0) ? "O-O-O" : "O-O"; // queenside : kingside
                    return;
                }
            }
            else if (move.EnPassant)
            {
                PrettyAlgebraic = Algebraic = $"{Origin.FileId}x{Destination} e.p.";
                return;
            }

            var ambiguousMoves = availableMoves.Where(m =>
                    (m.End == move.End) &&
                    (m.Start != move.Start) &&
                    (m.Piece == move.Piece))
                .ToList();

            var sb = new StringBuilder();
            char? pieceChar = null;

            if ((Piece & PieceType.Pawn) != 0)
            {
                sb.Append(Origin.FileId);
            }
            else
            {
                pieceChar = Piece.TypeId();
                sb.Append(pieceChar.Value);
            }

            if (ambiguousMoves.Any(m => m.StartRank == move.StartRank))
            {
                sb.Append(Origin.FileId);
            }
            if (ambiguousMoves.Any(m => m.StartFile == move.StartFile))
            {
                sb.Append(Origin.RankId);
            }

            if (isCapture) sb.Append('x');
            sb.Append(Destination.ToString());

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
                PrettyAlgebraic = Algebraic.Replace(pieceChar.Value, Piece.TypeIcon());
            }
            else
            {
                PrettyAlgebraic = Algebraic;
            }
        }
    }
}
