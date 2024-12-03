using Chessie.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chessie.Engine
{
    public static class MoveOrdering
    {
        public static void Sort(Board board, List<Move> potentialMoves)
        {
            potentialMoves.Sort(new MoveComparer(board));
        }

        private static MoveEvaluation EvaluateMove(Board board, Move move)
        {
            return new MoveEvaluation(Piece.UnsignedValue(move.Piece), Piece.UnsignedValue(move.CapturedPiece));
        }

        private class MoveComparer : IComparer<Move>
        {
            private readonly Board _board;
            private readonly Dictionary<Move, MoveEvaluation> _evalCache = new();

            public MoveComparer(Board board)
            {
                _board = board;
            }

            public int Compare(Move x, Move y)
            {
                if (!_evalCache.TryGetValue(x, out var xEval))
                {
                    xEval = EvaluateMove(_board, x);
                    _evalCache.Add(x, xEval);
                }

                if (!_evalCache.TryGetValue(y, out var yEval))
                {
                    yEval = EvaluateMove(_board, y);
                    _evalCache.Add(y, yEval);
                }

                return xEval.CompareTo(yEval);
            }
        }

        private readonly struct MoveEvaluation : IComparable<MoveEvaluation> 
        {
            public readonly int PieceValue;
            public readonly int CaptureValue;

            public MoveEvaluation(int pieceValue, int captureValue)
            {
                PieceValue = pieceValue;
                CaptureValue = captureValue;
            }

            public int CompareTo(MoveEvaluation other)
            {
                // most valuable victim to front of line
                if (CaptureValue > other.CaptureValue)
                {
                    return -1;
                }
                if (CaptureValue < other.CaptureValue)
                {
                    return 1;
                }

                // any capture of equal value
                if (CaptureValue > 0)
                {
                    // push least valuable aggressor to back of list
                    return Math.Sign(PieceValue - other.PieceValue);
                }

                // non-capture
                return 0;
            }
        }
    }
}
