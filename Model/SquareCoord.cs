using System.Diagnostics.CodeAnalysis;

namespace Chessie.Model
{
    public readonly struct SquareCoord
    {
        public const int MIN_RANK = 0;
        public const int MAX_RANK = 7;
        
        public const int MIN_FILE = 0;
        public const int MAX_FILE = 7;

        public readonly int Rank;
        public readonly int File;

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
        }

        public static bool operator ==(SquareCoord left, SquareCoord right) => (left.Rank == right.Rank) && (left.File == right.File);
        public static bool operator !=(SquareCoord left, SquareCoord right) => (left.Rank != right.Rank) || (left.File != right.File);

        public static SquareCoord operator +(SquareCoord left, SquareCoord right) => new(left.Rank + right.Rank, left.File + right.File);
        public static SquareCoord operator -(SquareCoord left, SquareCoord right) => new(left.Rank - right.Rank, left.File - right.File);

        public static SquareCoord operator *(SquareCoord coord, int multiplier) => new(coord.Rank * multiplier, coord.File * multiplier);

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return (obj is SquareCoord coord) && (this == coord);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Rank, File);
        }

        public override string ToString()
        {
            return $"{FileId}{RankId}";
        }

        public static IEnumerable<SquareCoord> AllSquares { get; } =
            Enumerable.Range(0, 8)
            .SelectMany(rank => Enumerable.Range(0, 8).Select(file => new SquareCoord(rank, file))).ToArray();
    }
}
