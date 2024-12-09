using System.Diagnostics.CodeAnalysis;

namespace Chessie.Core.Model
{
    public readonly struct SquareCoord
    {
        public const int MIN_RANK = 0;
        public const int MAX_RANK = 7;
        
        public const int MIN_FILE = 0;
        public const int MAX_FILE = 7;

        public readonly int Rank;
        public readonly int File;

        private readonly int _index;
        public readonly int Index => _index;
        public readonly ulong BitboardMask => 1ul << _index;

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
            _index = (Rank << 3) | File;
        }

        public SquareCoord(int squareIndex)
        {
            Rank = squareIndex >> 3;
            File = squareIndex & 0b111;
            _index = squareIndex;
        }

        public static bool operator ==(SquareCoord left, SquareCoord right) => (left.Rank == right.Rank) && (left.File == right.File);
        public static bool operator !=(SquareCoord left, SquareCoord right) => (left.Rank != right.Rank) || (left.File != right.File);

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return (obj is SquareCoord coord) && (this == coord);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Rank, File);
        }

        public static SquareCoord operator +(SquareCoord origin, MoveVector move) =>
            new(origin.Rank + move.DeltaRank, origin.File + move.DeltaFile);

        public static SquareCoord operator -(SquareCoord destination, MoveVector move) =>
            new(destination.Rank - move.DeltaRank, destination.File - move.DeltaFile);

        public override string ToString()
        {
            return $"{FileId}{RankId}";
        }
    }

    public readonly struct MoveVector
    {
        public readonly int DeltaRank;
        public readonly int DeltaFile;
        public readonly int DeltaIndex;

        public MoveVector(int deltaRank, int deltaFile)
        {
            DeltaRank = deltaRank;
            DeltaFile = deltaFile;
            DeltaIndex = (deltaRank * 8) + deltaFile;
        }

        public static MoveVector operator +(MoveVector left, MoveVector right) =>
            new(left.DeltaRank + right.DeltaRank, left.DeltaFile + right.DeltaFile);

        public static MoveVector operator *(MoveVector move, int scale) =>
            new(move.DeltaRank * scale, move.DeltaFile * scale);

        public readonly MoveVector FlipRankDirection() =>
            new(-DeltaRank, DeltaFile);

        public static readonly MoveVector UP = new(1, 0);
        public static readonly MoveVector DOWN = new(-1, 0);
        public static readonly MoveVector RIGHT = new(0, 1);
        public static readonly MoveVector LEFT = new(0, -1);

        public static readonly MoveVector UP_RIGHT = UP + RIGHT;
        public static readonly MoveVector UP_LEFT = UP + LEFT;
        public static readonly MoveVector DOWN_RIGHT = DOWN + RIGHT;
        public static readonly MoveVector DOWN_LEFT = DOWN + LEFT;

        public override string ToString()
        {
            return $"({DeltaRank}, {DeltaFile})";
        }
    }
}
