using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Chessie.Model
{
    public readonly struct HalfBitBoard
    {
        public readonly ulong[] Maps;

        public HalfBitBoard()
        {
            Maps = new ulong[6];
        }

        public readonly ulong Pawns => Maps[0];
        public readonly ulong Knights => Maps[1];
        public readonly ulong Bishops => Maps[2];
        public readonly ulong Rooks => Maps[3];
        public readonly ulong Queens => Maps[4];
        public readonly ulong King => Maps[5];
    }

    public readonly struct BitBoard
    {
        public readonly HalfBitBoard White;
        public readonly HalfBitBoard Black;
    }
}
