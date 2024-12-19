using Chessie.Core.Model;

namespace Chessie.Core.Engine
{
    public class TranspositionTable
    {
        const int TABLE_SIZE = (1 << 20) + 7;

        private readonly TranspositionEntry[] _lines = new TranspositionEntry[TABLE_SIZE];

        public void Clear()
        {
            Array.Clear(_lines);
        }

        public bool TryGetValue(ulong zobrist, out TranspositionEntry entry)
        {
            int index = (int)(zobrist % TABLE_SIZE);

            if (_lines[index].Key == zobrist)
            {
                entry = _lines[index];
                return true;
            }

            entry = new TranspositionEntry();
            return false;
        }

        public void Record(ulong zobrist, int depth, int eval, Move move, TT_NodeType nodeType)
        {
            int index = (int)(zobrist % TABLE_SIZE);

            //var current = _lines[index];

            //if ((current.Key == 0) || (current.SearchDepth > depth))
            //{
            _lines[index] = new TranspositionEntry(zobrist, depth, move, eval, nodeType);
            //}
        }
    }

    public readonly struct TranspositionEntry
    {
        public readonly ulong Key;
        public readonly int SearchDepth;
        public readonly Move BestMove;
        public readonly int Evaluation;
        public readonly TT_NodeType Type;

        public TranspositionEntry(ulong key, int searchDepth, Move bestMove, int evaluation, TT_NodeType type)
        {
            Key = key;
            SearchDepth = searchDepth;
            BestMove = bestMove;
            Evaluation = evaluation;
            Type = type;
        }
    }

    public enum TT_NodeType
    {
        Exact,
        Minimum,
        Maximum,
    }
}
