﻿using System.Diagnostics.CodeAnalysis;

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
            foreach (var piece in board.GetMap(forBlack).AllPieces())
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
            IEnumerable<Move> allMoves = (piece.Piece & PieceType.PieceMask) switch
            {
                PieceType.Pawn => GetValidPawnMoves(board, piece),
                PieceType.Knight => GetValidKnightMoves(board, piece),
                PieceType.Bishop => ProcessLinearMoves(board, piece, BishopVectors),
                PieceType.Rook => ProcessLinearMoves(board, piece, RookVectors),
                PieceType.Queen => ProcessLinearMoves(board, piece, AllVectors),
                PieceType.King => GetValidKingMoves(board, piece),
                _ => Enumerable.Empty<Move>(),
            };

            return allMoves;
        }

        #region Move Calculations

        private static IEnumerable<Move> GetValidPawnMoves(Board board, LocatedPiece piece)
        {
            int rankMovement = ((piece.Piece & PieceType.Black) != 0) ? DOWN : UP;

            var forwardSquare = piece.Location + rankMovement;
            if (board[forwardSquare] == PieceType.Empty)
            {
                yield return new Move(piece.Piece, PieceType.Empty, piece.Location, forwardSquare);
            }

            // initial 2-square jump
            int initialRank = ((piece.Piece & PieceType.Black) != 0) ? MAX_COORD - 1 : MIN_COORD + 1;
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
            foreach (var move in KnightMoves)
            {
                if (!(new SquareCoord(piece.Location) + move).IsValidSquare)
                {
                    continue;
                }

                var dest = piece.Location + move.DeltaIndex;
                var target = board[dest];
                if ((target == PieceType.Empty) || piece.Piece.IsOpponentPiece(target))
                {
                    yield return new Move(piece.Piece, target, piece.Location, dest);
                }
            }
        }

        public static readonly MoveVector[] KnightMoves =
        {
            new(1, 2),  new(-1, 2),
            new(1, -2), new(-1, -2),
            new(2, 1),  new(-2, 1),
            new(2, -1), new(-2, -1),
        };

        public static readonly MoveVector[] BishopVectors =
        {
            MoveVector.UP_RIGHT, MoveVector.DOWN_RIGHT, MoveVector.DOWN_LEFT, MoveVector.UP_LEFT,
        };

        public static readonly MoveVector[] RookVectors =
        {
            MoveVector.UP, MoveVector.RIGHT, MoveVector.DOWN, MoveVector.LEFT,
        };

        public static readonly MoveVector[] AllVectors = BishopVectors.Concat(RookVectors).ToArray();

        private static IEnumerable<Move> GetValidKingMoves(Board board, LocatedPiece piece)
        {
            foreach (var move in AllVectors)
            {
                if (!(new SquareCoord(piece.Location) + move).IsValidSquare)
                {
                    continue;
                }

                int dest = piece.Location + move.DeltaIndex;
                var target = board[dest];
                if (target == PieceType.Empty || piece.Piece.IsOpponentPiece(target))
                {
                    yield return new Move(piece.Piece, target, piece.Location, dest);
                }
            }
        }

        private static IEnumerable<Move> ProcessLinearMoves(Board board, LocatedPiece piece, IEnumerable<MoveVector> vectors)
        {
            foreach (var vector in vectors)
            {
                MoveVector move = vector;

                for (int offset = 1; offset <= 7; offset++)
                {
                    if (!(new SquareCoord(piece.Location) + move).IsValidSquare)
                    {
                        continue;
                    }

                    int dest = piece.Location + move.DeltaIndex;
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

        public static bool IsCheck(Board board, bool forBlack, [NotNullWhen(true)]out int? checkLocation)
        {
            var kingLocation = board.GetMap(forBlack).King;

            var bitboards = board.GetThreatMaps(forBlack);

            //foreach (var piece in board.GetOpponentMap(forBlack).AllPieces())
            //{
            //    var potentialMoves = GetPotentialMovesForPiece(board, piece);
            //    if (potentialMoves.Any(move => move.End == kingLocation))
            //    {
            //        checkLocation = kingLocation;
            //        return true;
            //    }
            //}

            ulong flag = bitboards.Threats & (1ul << kingLocation);
            if (flag != 0)
            {
                checkLocation = kingLocation;
                return true;
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
