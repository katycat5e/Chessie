using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Chessie.Model
{
    public class PieceMap
    {
        const int N_TYPES = 5;
        const int MAX_PIECES = 8;

        public readonly PieceType Color;
        private readonly int[,] _locations = new int[N_TYPES, MAX_PIECES];
        private readonly int[] _pieceCounts = new int[N_TYPES];

        public int King { get; private set; }

        private ulong? _pieceBitboard;

        public ulong PieceBitboard
        {
            get
            {
                if (!_pieceBitboard.HasValue)
                {
                    _pieceBitboard = GenerateBitboard();
                }
                return _pieceBitboard.Value;
            }
        }

        private void BustBitBoard() => _pieceBitboard = null;

        public PieceMap(PieceType color)
        {
            Color = color;
        }

        public PieceMap(PieceType[] squares, PieceType color)
        {
            Color = color;
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

            _locations[index, _pieceCounts[index]] = location;
            _pieceCounts[index]++;

            BustBitBoard();
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
            _locations[typeIndex, pieceIndex] = destination;

            BustBitBoard();
        }

        public void RemovePiece(PieceType piece, int location)
        {
            int typeIndex = TypeIndex(piece);
            int pieceIndex = PieceIndex(typeIndex, location);

            int lastPieceIndex = _pieceCounts[typeIndex] - 1;
            _locations[typeIndex, pieceIndex] = _locations[typeIndex, lastPieceIndex];
            _pieceCounts[typeIndex]--;

            BustBitBoard();
        }

        private int PieceIndex(int typeIndex, int location)
        {
            for (int i = 0; i < _pieceCounts[typeIndex]; i++)
            {
                if (location == _locations[typeIndex, i]) return i;
            }
            throw new ArgumentException("piece not found");
        }

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

        private ulong GenerateBitboard()
        {
            ulong result = 0;
            foreach (var piece in AllPieces())
            {
                ulong mask = 1ul << piece.Location;
                result |= mask;
            }
            return result;
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
            var result = new LocatedPiece[16];
            int index = 0;

            for (int typeIdx = 0; typeIdx < N_TYPES; typeIdx++)
            {
                var type = _types[typeIdx] | Color;

                for (int i = 0; i < _pieceCounts[typeIdx]; i++)
                {
                    result[index] = new LocatedPiece(type, _locations[typeIdx, i]);
                    index++;
                }
            }

            result[index] = new LocatedPiece(PieceType.King | Color, King);
            
            return (index == 16) ? result : new ArraySegment<LocatedPiece>(result, 0, index);
        }
    }


    public readonly struct LocatedPiece
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
    }
}
