using System.Text;

namespace Chessie.Model
{
    public readonly struct Move
    {
        public readonly PieceType Piece;
        public readonly SquareCoord Start;
        public readonly SquareCoord End;
        public readonly SquareCoord? CastlingRookStart;
        public readonly bool EnPassant;

        public readonly SquareCoord CastlingRookEnd
        {
            get
            {
                int kingDir = Math.Sign(End.File - Start.File);
                return new SquareCoord(End.Rank, End.File - kingDir);
            }
        }

        public readonly SquareCoord EnPassantCapture => new(End.Rank - 1, End.File);

        public readonly int DeltaRank => End.Rank - Start.Rank;

        public Move(PieceType piece, SquareCoord start, SquareCoord end, SquareCoord? rook = null, bool enPassant = false)
        {
            Piece = piece;
            Start = start;
            End = end;
            CastlingRookStart = rook;
            EnPassant = enPassant;
        }

        public Move(PieceType piece, SquareCoord start, int dRank, int dFile, SquareCoord? rook = null, bool enPassant = false)
        {
            Piece = piece;
            Start = start;
            End = new(Start.Rank + dRank, Start.File + dFile);
            CastlingRookStart = rook;
            EnPassant = enPassant;
        }

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
