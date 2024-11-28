using System.Diagnostics;

namespace Chessie.Model
{
    public static class BoardCalculator
    {
        public static IEnumerable<Move> GetAllValidMoves(BoardState board, bool forBlack)
        {
            foreach (var square in SquareCoord.AllSquares)
            {
                var piece = board[square];
                if (piece.IsOwnPiece(forBlack))
                {
                    var potentialMoves = GetValidMovesForPiece(board, square);
                    foreach (var move in potentialMoves)
                    {
                        yield return move;
                    }
                }
            }
        }

        public static IEnumerable<Move> GetValidMovesForPiece(BoardState board, SquareCoord start)
        {
            var allMoves = GetPotentialMovesForPiece(board, start).ToList();

            foreach (var move in allMoves)
            {
                if (!DoesMoveCauseCheck(board, move, board.BlackToMove))
                {
                    yield return move;
                }
            }
        }

        public static IEnumerable<Move> GetPotentialMovesForPiece(BoardState board, SquareCoord start)
        {
            var piece = board[start];
            IEnumerable<Move> allMoves = piece.GetUncoloredType() switch
            {
                PieceType.Pawn => GetValidPawnMoves(board, start, piece),
                PieceType.Knight => GetValidKnightMoves(board, start, piece),
                PieceType.Bishop => ProcessLinearMoves(board, start, _bishopVectors, piece),
                PieceType.Rook => ProcessLinearMoves(board, start, _rookVectors, piece),
                PieceType.Queen => ProcessLinearMoves(board, start, _allVectors, piece),
                PieceType.King => GetValidKingMoves(board, start, piece),
                _ => Enumerable.Empty<Move>(),
            };

            return allMoves;
        }

        #region Move Calculations

        private static IEnumerable<Move> GetValidPawnMoves(BoardState board, SquareCoord start, PieceType piece)
        {
            int rankMovement = board.BlackToMove ? -1 : 1;

            var forwardSquare = new SquareCoord(start.Rank + rankMovement, start.File);
            if (board[forwardSquare] == PieceType.Empty)
            {
                yield return new Move(piece, start, forwardSquare);
            }

            // initial 2-square jump
            int initialRank = board.BlackToMove ? SquareCoord.MAX_RANK - 1 : SquareCoord.MIN_RANK + 1;
            if (start.Rank == initialRank)
            {
                forwardSquare = new SquareCoord(start.Rank + rankMovement * 2, start.File);
                if (board[forwardSquare] == PieceType.Empty)
                {
                    yield return new Move(piece, start, forwardSquare);
                }
            }

            // captures
            if (!start.IsInFirstFile)
            {
                var target = new SquareCoord(start.Rank + rankMovement, start.File - 1);
                
                if (piece.IsOpponentPiece(board[target]))
                {
                    yield return new Move(piece, start, rankMovement, -1);
                }
                else if (board.EnPassantSquare == target)
                {
                    yield return new Move(piece, start, rankMovement, -1, enPassant: true);
                }
            }

            if (!start.IsInLastFile)
            {
                var target = new SquareCoord(start.Rank + rankMovement, start.File + 1);

                if (piece.IsOpponentPiece(board[target]))
                {
                    yield return new Move(piece, start, rankMovement, 1);
                }
                else if (board.EnPassantSquare == target)
                {
                    yield return new Move(piece, start, rankMovement, 1, enPassant: true);
                }
            }
        }

        private static IEnumerable<Move> GetValidKnightMoves(BoardState board, SquareCoord start, PieceType piece)
        {
            foreach (var move in _knightMoves)
            {
                var dest = start + move;
                if (dest.IsValidSquare)
                {
                    var target = board[dest];
                    if ((target == PieceType.Empty) || board[start].IsOpponentPiece(target))
                    {
                        yield return new Move(piece, start, dest);
                    }
                }
            }
        }

        private static readonly SquareCoord[] _knightMoves =
        {
            new(1, 2),  new(-1, 2),
            new(1, -2), new(-1, -2),
            new(2, 1),  new(-2, 1),
            new(2, -1), new(-2, -1),
        };

        private static readonly SquareCoord[] _bishopVectors =
        {
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
        };

        private static readonly SquareCoord[] _rookVectors =
        {
            new(1, 0), new(0, 1), new(-1, 0), new(0, -1),
        };

        private static readonly SquareCoord[] _allVectors =
        {
            new(1, 0), new(0, 1), new(-1, 0), new(0, -1),
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
        };

        private static IEnumerable<Move> GetValidKingMoves(BoardState board, SquareCoord start, PieceType piece)
        {
            foreach (var move in _allVectors)
            {
                var dest = start + move;
                if (!dest.IsValidSquare) continue;

                var target = board[dest];
                if (target == PieceType.Empty || board[start].IsOpponentPiece(target))
                {
                    yield return new Move(piece, start, dest);
                }
            }
        }

        private static IEnumerable<Move> ProcessLinearMoves(BoardState board, SquareCoord start, IEnumerable<SquareCoord> vectors, PieceType piece)
        {
            foreach (var vector in vectors)
            {
                for (int offset = 1; offset <= 7; offset++)
                {
                    var dest = start + (vector * offset);
                    if (!dest.IsValidSquare) continue;

                    var target = board[dest];
                    if (target == PieceType.Empty)
                    {
                        yield return new Move(piece, start, dest);
                    }
                    else if (board[start].IsOpponentPiece(target))
                    {
                        yield return new Move(piece, start, dest);
                        break;
                    }
                    else break;
                }
            }
        }

        #endregion


        private static bool DoesMoveCauseCheck(BoardState board, Move move, bool forBlack)
        {
            var moveResult = board.ApplyMove(move);
            return IsCheck(moveResult, forBlack, out _);
        }

        public static bool IsCheck(BoardState board, bool forBlack, out SquareCoord? checkLocation)
        {
            var kingLocation = board.GetKingPosition(forBlack);

            foreach (var square in SquareCoord.AllSquares)
            {
                var piece = board[square];
                if (piece.IsOpponentPiece(forBlack))
                {
                    var potentialMoves = GetPotentialMovesForPiece(board, square);
                    if (potentialMoves.Any(move => move.End == kingLocation))
                    {
                        checkLocation = kingLocation;
                        return true;
                    }
                }
            }

            checkLocation = null;
            return false;
        }

        public static bool IsMate(BoardState board, bool forBlack)
        {
            return !GetAllValidMoves(board, forBlack).Any();
        }
    }
}
