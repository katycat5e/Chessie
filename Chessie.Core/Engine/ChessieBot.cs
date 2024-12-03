using Chessie.Core.Model;
using System.Diagnostics;

namespace Chessie.Core.Engine
{
    public static class ChessieBot
    {
        const int SEARCH_DEPTH = 3;

        const int MAX = 999999;
        const int MIN = -MAX;

        public static float LastThinkDuration { get; private set; }
        public static int StatesEvaluated { get; private set; }

        public static List<RankedMove> RankPotentialMoves(Board board)
        {
            StatesEvaluated = 0;
            bool playingAsBlack = board.BlackToMove;

            var watch = Stopwatch.StartNew();

            var nextMoves = BoardCalculator.GetAllValidMoves(board, playingAsBlack).ToList();
            MoveOrdering.Sort(board, nextMoves);

            var sortedMoves = new List<RankedMove>();
            foreach (var move in nextMoves)
            {
                board.ApplyMove(move);
                int bestEval = Search(board, SEARCH_DEPTH, playingAsBlack, MAX, MIN, out string seq);// + Random.Shared.Next(-5, 5);
                board.UndoLastMove();
                sortedMoves.Add(new RankedMove(bestEval, move, seq));
            }

            static int ReverseSorter(RankedMove a, RankedMove b)
            {
                return b.CompareTo(a);
            }

            if (board.BlackToMove)
            {
                sortedMoves.Sort();
            }
            else
            {
                sortedMoves.Sort(ReverseSorter);
            }

            watch.Stop();
            LastThinkDuration = watch.ElapsedMilliseconds / 1000f;

            string player = board.BlackToMove ? "black" : "white";
            Trace.WriteLine($"{StatesEvaluated} states evaluated for {player}");

            return sortedMoves;
        }

        private static int Search(Board board, int depthLimit, bool maximizing, int minimumMaximizer, int maximumMinimizer, out string moveSeq)
        {
            if (depthLimit == 0)
            {
                StatesEvaluated++;
                moveSeq = string.Empty;
                return Evaluate(board);
            }

            //int best = MIN;
            moveSeq = string.Empty;
            var nextMoves = BoardCalculator.GetAllValidMoves(board, board.BlackToMove);

            int bestEval;
            Move bestMove = new();
            string bestSeq = string.Empty;

            if (maximizing)
            {
                bestEval = MIN;

                foreach (var move in nextMoves)
                {
                    board.ApplyMove(move);
                    int bestMoveResult = Search(board, depthLimit - 1, !maximizing, minimumMaximizer, maximumMinimizer, out string nextSeq);
                    board.UndoLastMove();

                    if (bestMoveResult > bestEval)
                    {
                        bestEval = bestMoveResult;
                        bestMove = move;
                        bestSeq = nextSeq;
                    }

                    if (bestEval > minimumMaximizer) break;

                    maximumMinimizer = Math.Max(maximumMinimizer, bestEval);
                }
            }
            else
            {
                bestEval = MAX;

                foreach (var move in nextMoves)
                {
                    board.ApplyMove(move);
                    int bestMoveResult = Search(board, depthLimit - 1, !maximizing, minimumMaximizer, maximumMinimizer, out string nextSeq);
                    board.UndoLastMove();

                    if (bestMoveResult < bestEval)
                    {
                        bestEval = bestMoveResult;
                        bestMove = move;
                        bestSeq = nextSeq;
                    }

                    if (bestEval < maximumMinimizer) break;

                    minimumMaximizer = Math.Min(minimumMaximizer, bestEval);
                }
            }

            moveSeq = bestMove.ToString() + bestSeq;
            return bestEval;
        }

        public static int Evaluate(Board board)
        {
            int eval = MaterialEval(board);
            eval += PST_Eval(board);

            if (BoardCalculator.IsCheck(board, board.BlackToMove, out _))
            {
                if (BoardCalculator.IsMate(board, board.BlackToMove))
                {
                    return board.BlackToMove ? MATE_PENALTY : -MATE_PENALTY;
                }
            }

            return eval;
        }

        // returns white's material advantage
        private static int MaterialEval(Board board)
        {
            int total = 0;
            foreach (var piece in board.GetMap(false).AllPieces())
            {
                total += Piece.SignedValue(piece.Piece);
            }
            foreach (var piece in board.GetMap(true).AllPieces())
            {
                total += Piece.SignedValue(piece.Piece);
            }
            return total;
        }

        const int CHECK_PENALTY = Piece.QUEEN_VALUE * 4;
        const int MATE_PENALTY = int.MaxValue;

        // returns white's position advantage
        private static int PST_Eval(Board board)
        {
            int eval = 0;
            foreach (var whitePiece in board.GetMap(false).AllPieces())
            {
                eval += PieceSquareTables.Evaluate(whitePiece);
            }
            foreach (var blackPiece in board.GetMap(true).AllPieces())
            {
                eval -= PieceSquareTables.Evaluate(blackPiece);
            }

            return eval;
        }
    }



    public readonly struct RankedMove : IComparable<RankedMove>
    {
        public readonly int Evaluation;
        public readonly Move Move;
        public readonly PieceType? Promotion;
        public readonly string Sequence;

        public RankedMove(int evaluation, Move move, string sequence, PieceType? promotion = null)
        {
            Evaluation = evaluation;
            Move = move;
            Promotion = promotion;
            Sequence = sequence;
        }

        public int CompareTo(RankedMove other)
        {
            return Evaluation - other.Evaluation;
        }

        public override string ToString()
        {
            if (Promotion.HasValue)
            {
                return $"({Evaluation}) {Move}={Promotion.Value.TypeIcon()},{Sequence}";
            }
            return $"({Evaluation}) {Move},{Sequence}";
        }
    }
}
