namespace Chessie.Model
{
    public static class BoardCalculator
    {
        private const int MIN_COORD = 0;
        private const int MAX_COORD = 7;

        private const int UP = 8;
        private const int DOWN = -8;
        private const int LEFT = -1;
        private const int RIGHT = 1;

        public static IEnumerable<Move> GetAllValidMoves(Board board, bool forBlack)
        {
            foreach (var piece in board.EnumeratePieces(forBlack))
            {
                var potentialMoves = GetValidMovesForPiece(board, piece);
                foreach (var move in potentialMoves)
                {
                    yield return move;
                }
            }
        }

        public static IEnumerable<Move> GetValidMovesForPiece(Board board, LocatedPiece piece)
        {
            var allMoves = GetPotentialMovesForPiece(board, piece).ToList();

            foreach (var move in allMoves)
            {
                if (!DoesMoveCauseCheck(board, move, board.BlackToMove))
                {
                    yield return move;
                }
            }
        }

        public static IEnumerable<Move> GetPotentialMovesForPiece(Board board, LocatedPiece piece)
        {
            IEnumerable<Move> allMoves = piece.Piece.GetUncoloredType() switch
            {
                PieceType.Pawn => GetValidPawnMoves(board, piece),
                PieceType.Knight => GetValidKnightMoves(board, piece),
                PieceType.Bishop => ProcessLinearMoves(board, piece, _bishopVectors),
                PieceType.Rook => ProcessLinearMoves(board, piece, _rookVectors),
                PieceType.Queen => ProcessLinearMoves(board, piece, _allVectors),
                PieceType.King => GetValidKingMoves(board, piece),
                _ => Enumerable.Empty<Move>(),
            };

            return allMoves;
        }

        #region Move Calculations

        private static IEnumerable<Move> GetValidPawnMoves(Board board, LocatedPiece piece)
        {
            int rankMovement = board.BlackToMove ? DOWN : UP;

            var forwardSquare = piece.Location + rankMovement;
            if (board[forwardSquare] == PieceType.Empty)
            {
                yield return new Move(piece.Piece, PieceType.Empty, piece.Location, forwardSquare);
            }

            // initial 2-square jump
            int initialRank = board.BlackToMove ? MAX_COORD - 1 : MIN_COORD + 1;
            if (piece.Rank == initialRank)
            {
                forwardSquare = piece.Location + rankMovement * 2;
                if (board[forwardSquare] == PieceType.Empty)
                {
                    yield return new Move(piece.Piece, PieceType.Empty, piece.Location, forwardSquare);
                }
            }

            // captures
            if (piece.File != MIN_COORD)
            {
                int target = piece.Location + rankMovement + LEFT;
                
                if (piece.Piece.IsOpponentPiece(board[target]))
                {
                    yield return new Move(piece.Piece, board[target], piece.Location, target);
                }
                else if (board.EnPassantSquare == target)
                {
                    yield return new Move(piece.Piece, PieceType.Empty, piece.Location, target, enPassant: true);
                }
            }

            if (piece.File != MAX_COORD)
            {
                int target = piece.Location + rankMovement + RIGHT;

                if (piece.Piece.IsOpponentPiece(board[target]))
                {
                    yield return new Move(piece.Piece, board[target], piece.Location, target);
                }
                else if (board.EnPassantSquare == target)
                {
                    yield return new Move(piece.Piece, PieceType.Empty, piece.Location, target, enPassant: true);
                }
            }
        }

        private static IEnumerable<Move> GetValidKnightMoves(Board board, LocatedPiece piece)
        {
            foreach (var move in _knightMoves)
            {
                int dRank = move >> 3;
                int dFile = move & 7;

                if ((piece.Rank + dRank > MAX_COORD) ||
                    (piece.Rank + dRank < MIN_COORD) ||
                    (piece.File + dFile > MAX_COORD) ||
                    (piece.File + dFile < MIN_COORD))
                {
                    continue;
                }

                var dest = piece.Location + move;
                var target = board[dest];
                if ((target == PieceType.Empty) || piece.Piece.IsOpponentPiece(target))
                {
                    yield return new Move(piece.Piece, target, piece.Location, dest);
                }
            }
        }

        private static readonly int[] _knightMoves =
            new (int, int)[]
        {
            (1, 2),  (-1, 2),
            (1, -2), (-1, -2),
            (2, 1),  (-2, 1),
            (2, -1), (-2, -1),
        }
        .Select(rf => rf.Item1 * 8 + rf.Item2).ToArray();

        private static readonly int[] _bishopVectors =
        {
            UP + RIGHT, DOWN + RIGHT, DOWN + LEFT, UP + LEFT,
        };

        private static readonly int[] _rookVectors =
        {
            UP, RIGHT, DOWN, LEFT,
        };

        private static readonly int[] _allVectors = _bishopVectors.Concat(_rookVectors).ToArray();

        private static IEnumerable<Move> GetValidKingMoves(Board board, LocatedPiece piece)
        {
            foreach (var move in _allVectors)
            {
                int dRank = move >> 3;
                int dFile = move & 7;

                if ((piece.Rank + dRank > MAX_COORD) ||
                    (piece.Rank + dRank < MIN_COORD) ||
                    (piece.File + dFile > MAX_COORD) ||
                    (piece.File + dFile < MIN_COORD))
                {
                    continue;
                }

                int dest = piece.Location + move;
                var target = board[dest];
                if (target == PieceType.Empty || piece.Piece.IsOpponentPiece(target))
                {
                    yield return new Move(piece.Piece, target, piece.Location, dest);
                }
            }
        }

        private static IEnumerable<Move> ProcessLinearMoves(Board board, LocatedPiece piece, IEnumerable<int> vectors)
        {
            foreach (var vector in vectors)
            {
                int move = vector;

                for (int offset = 1; offset <= 7; offset++)
                {
                    int dRank = move >> 3;
                    int dFile = move & 7;

                    if ((piece.Rank + dRank > MAX_COORD) ||
                        (piece.Rank + dRank < MIN_COORD) ||
                        (piece.File + dFile > MAX_COORD) ||
                        (piece.File + dFile < MIN_COORD))
                    {
                        continue;
                    }

                    int dest = piece.Location + move;
                    var target = board[dest];
                    if (target == PieceType.Empty)
                    {
                        yield return new Move(piece, target, dest);
                    }
                    else if (piece.Piece.IsOpponentPiece(target))
                    {
                        yield return new Move(piece, target, dest);
                        break;
                    }
                    else break;

                    move += vector;
                }
            }
        }

        #endregion


        private static bool DoesMoveCauseCheck(Board board, Move move, bool forBlack)
        {
            board.ApplyMove(move);
            bool isCheck = IsCheck(board, forBlack, out _);
            board.UndoLastMove();
            return isCheck;
        }

        public static bool IsCheck(Board board, bool forBlack, out int? checkLocation)
        {
            var kingLocation = board.GetMap(forBlack).King;

            foreach (var piece in board.GetOpponentMap(forBlack))
            {
                var potentialMoves = GetPotentialMovesForPiece(board, piece);
                if (potentialMoves.Any(move => move.End == kingLocation))
                {
                    checkLocation = kingLocation;
                    return true;
                }
            }

            checkLocation = null;
            return false;
        }

        public static bool IsMate(Board board, bool forBlack)
        {
            return !GetAllValidMoves(board, forBlack).Any();
        }
    }
}
