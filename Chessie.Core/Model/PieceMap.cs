﻿using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace Chessie.Core.Model
{
    public class PieceMap
    {
        const int N_TYPES = 5;
        const int MAX_PIECES = 8;

        public readonly PieceType Color;
        private readonly int[][] _locations = new int[N_TYPES][];
        private readonly int[] _pieceCounts = new int[N_TYPES];
        private int _totalCount = 1;

        public int King { get; private set; }

        public ulong PieceBitboard { get; private set; }

        private readonly ulong[] _bitBoards = new ulong[N_TYPES];

        public ulong GetBitboard(PieceType type) => _bitBoards[TypeIndex(type)];

        public PieceMap(PieceType color)
        {
            Color = color;
            for (int i = 0; i < N_TYPES; i++)
            {
                _locations[i] = new int[MAX_PIECES];
            }
        }

        public PieceMap(PieceType[] squares, PieceType color)
        {
            Color = color;
            for (int i = 0; i < N_TYPES; i++)
            {
                _locations[i] = new int[MAX_PIECES];
            }
            InitFromBoard(squares);
        }

        public void InitFromBoard(PieceType[] squares)
        {
            Array.Fill(_pieceCounts, 0);

            for (int index = 0; index < 64; index++)
            {
                var piece = squares[index];
                if ((piece & Color) != 0)
                {
                    AddPiece(piece, index);
                }
            }
        }

        public void AddPiece(PieceType piece, int location)
        {
            if ((piece & PieceType.King) != 0)
            {
                King = location;
                return;
            }

            int index = TypeIndex(piece);
            if (_pieceCounts[index] == MAX_PIECES)
            {
                throw new IndexOutOfRangeException("Too many pieces of type " + piece.ToString());
            }

            _locations[index][_pieceCounts[index]] = location;
            _pieceCounts[index]++;
            _totalCount++;

            SetBitboard(index, location);
        }

        public void MovePiece(PieceType piece, Move move) =>
            MovePiece(piece, move.Start, move.End);

        public void MovePiece(PieceType piece, int origin, int destination)
        {
            if ((piece & PieceType.King) != 0)
            {
                King = destination;
                return;
            }

            int typeIndex = TypeIndex(piece);
            int pieceIndex = PieceIndex(typeIndex, origin);
            _locations[typeIndex][pieceIndex] = destination;

            UnsetBitboard(typeIndex, origin);
            SetBitboard(typeIndex, destination);
        }

        public void RemovePiece(PieceType piece, int location)
        {
            int typeIndex = TypeIndex(piece);
            int pieceIndex = PieceIndex(typeIndex, location);

            int lastPieceIndex = _pieceCounts[typeIndex] - 1;
            _locations[typeIndex][pieceIndex] = _locations[typeIndex][lastPieceIndex];
            _pieceCounts[typeIndex]--;
            _totalCount--;

            UnsetBitboard(typeIndex, location);
        }

        private int PieceIndex(int typeIndex, int location)
        {
            for (int i = 0; i < _pieceCounts[typeIndex]; i++)
            {
                if (location == _locations[typeIndex][i]) return i;
            }
            throw new ArgumentException("piece not found");
        }

        private const int PAWN_INDEX = 0;
        private const int KNIGHT_INDEX = 1;
        private const int BISHOP_INDEX = 2;
        private const int ROOK_INDEX = 3;
        private const int QUEEN_INDEX = 4;

        private static int TypeIndex(PieceType pieceType)
        {
            return (pieceType & PieceType.PieceMask) switch
            {
                PieceType.Pawn => 0,
                PieceType.Knight => 1,
                PieceType.Bishop => 2,
                PieceType.Rook => 3,
                PieceType.Queen => 4,
                _ => throw new ArgumentException("Invalid piece"),
            };
        }

        private void SetBitboard(int pieceTypeIndex, int location)
        {
            ulong mask = 1ul << location;
            PieceBitboard |= mask;
            _bitBoards[pieceTypeIndex] |= mask;
        }

        private void UnsetBitboard(int pieceTypeIndex, int location)
        {
            ulong mask = 1ul << location;
            PieceBitboard &= ~mask;
            _bitBoards[pieceTypeIndex] &= ~mask;
        }

        private static readonly PieceType[] _types =
        {
            PieceType.Pawn,
            PieceType.Knight,
            PieceType.Bishop,
            PieceType.Rook,
            PieceType.Queen,
        };

        public IList<LocatedPiece> AllPieces()
        {
            var result = new LocatedPiece[_totalCount];
            int index = 0;

            for (int typeIdx = 0; typeIdx < N_TYPES; typeIdx++)
            {
                var type = _types[typeIdx] | Color;

                for (int i = 0; i < _pieceCounts[typeIdx]; i++)
                {
                    result[index] = new LocatedPiece(type, _locations[typeIdx][i]);
                    index++;
                }
            }

            result[index] = new LocatedPiece(PieceType.King | Color, King);
            return result;
        }

        public List<LocatedPiece> GetAttackers(int square, ulong defenderPieceBB)
        {
            var result = new List<LocatedPiece>();
            var target = new SquareCoord(square);

            // pawns
            int pawnDir = Color == PieceType.Black ? -1 : 1;

            var leftCapture = target - new MoveVector(pawnDir, -1);
            if (leftCapture.IsValidSquare && ((_bitBoards[PAWN_INDEX] & leftCapture.BitboardMask) != 0))
            {
                result.Add(new(PieceType.Pawn | Color, leftCapture.Index));
            }

            var rightCapture = target - new MoveVector(pawnDir, 1);
            if (rightCapture.IsValidSquare && ((_bitBoards[PAWN_INDEX] & rightCapture.BitboardMask) != 0))
            {
                result.Add(new(PieceType.Pawn | Color, rightCapture.Index));
            }

            // knights
            foreach (var move in BoardCalculator.KnightMoves)
            {
                var attackSquare = target - move;
                if (attackSquare.IsValidSquare && ((_bitBoards[KNIGHT_INDEX] & attackSquare.BitboardMask) != 0))
                {
                    result.Add(new(PieceType.Knight | Color, attackSquare.Index));
                }
            }

            foreach (var vector in BoardCalculator.AllVectors)
            {
                var attackSquare = target - vector;
                bool diagonal = (vector.DeltaRank & vector.DeltaFile) != 0;

                for (int scale = 1; scale <= 7; scale++)
                {
                    ulong attackSquareMask = attackSquare.BitboardMask;

                    // out of bounds or occupied by own piece
                    if (!attackSquare.IsValidSquare || ((defenderPieceBB & attackSquareMask) != 0)) break;

                    // empty square
                    if ((PieceBitboard & attackSquareMask) == 0) continue;

                    // attacker
                    if (diagonal)
                    {
                        // bishop
                        if ((_bitBoards[BISHOP_INDEX] & attackSquare.BitboardMask) != 0)
                        {
                            result.Add(new(PieceType.Bishop | Color, attackSquare.Index));
                            break;
                        }
                        // queen
                        else if ((_bitBoards[QUEEN_INDEX] & attackSquare.BitboardMask) != 0)
                        {
                            result.Add(new(PieceType.Queen | Color, attackSquare.Index));
                            break;
                        }
                    }
                    else
                    {
                        // rook
                        if ((_bitBoards[ROOK_INDEX] & attackSquare.BitboardMask) != 0)
                        {
                            result.Add(new(PieceType.Rook | Color, attackSquare.Index));
                            break;
                        }
                    }

                    attackSquare -= vector;
                }
            }

            //// bishops
            //CheckForSlideAttackers(result, defenderPieceBB, target, BoardCalculator.BishopVectors, PieceType.Bishop | Color, BISHOP_INDEX);

            //// rooks
            //CheckForSlideAttackers(result, defenderPieceBB, target, BoardCalculator.RookVectors, PieceType.Rook | Color, ROOK_INDEX);

            //// queens
            //CheckForSlideAttackers(result, defenderPieceBB, target, BoardCalculator.AllVectors, PieceType.Queen | Color, QUEEN_INDEX);

            return result;
        }

        private void CheckForSlideAttackers(List<LocatedPiece> attackers, ulong defenders, SquareCoord target, IEnumerable<MoveVector> vectorsToCheck, PieceType piece, int typeIndex)
        {
            foreach (var vector in vectorsToCheck)
            {
                var attackSquare = target - vector;

                for (int scale = 1; scale <= 7; scale++)
                {
                    ulong attackSquareMask = attackSquare.BitboardMask;

                    // out of bounds or occupied by own piece
                    if (!attackSquare.IsValidSquare || ((defenders & attackSquareMask) != 0)) break;

                    // empty square
                    if ((PieceBitboard & attackSquareMask) == 0) continue;

                    // attacker
                    if ((_bitBoards[typeIndex] & attackSquare.BitboardMask) != 0)
                    {
                        attackers.Add(new(piece, attackSquare.Index));
                        break;
                    }

                    attackSquare -= vector;
                }
            }
        }
    }


    public readonly struct LocatedPiece : IComparable<LocatedPiece>
    {
        public readonly PieceType Piece;
        public readonly int Location;

        public readonly int Rank => Location >> 3;
        public readonly int File => Location & 7;

        public LocatedPiece(PieceType piece, int location)
        {
            Piece = piece;
            Location = location;
        }

        public static bool operator ==(LocatedPiece left, LocatedPiece right)
        {
            return (left.Piece == right.Piece) && (left.Location == right.Location);
        }

        public static bool operator !=(LocatedPiece left, LocatedPiece right)
        {
            return (left.Piece != right.Piece) || (left.Location != right.Location);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return (obj is LocatedPiece other) && (this == other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Piece, Location);
        }

        public int CompareTo(LocatedPiece other)
        {
            return Model.Piece.UnsignedValue(Piece).CompareTo(Model.Piece.UnsignedValue(other.Piece));
        }
    }
}
