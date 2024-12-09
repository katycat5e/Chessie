using Chessie.Core.Model;
using System.Diagnostics;

namespace Chessie.Core.Engine
{
    public static class ChessieBot
    {
        #if DEBUG
            const bool DEBUG_MODE = true;
        #else
            const bool DEBUG_MODE = false;
        #endif

        public static int SearchDepth { get; set; } = 3;
        public static bool DeterministicSearch { get; set; } = DEBUG_MODE;
        
        public static bool UseMoveOrdering { get; set; } = true;
        public static bool UseSEE { get; set; } = !DEBUG_MODE;
        public static bool UseABPruning { get; set; } = true;

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
            if (UseMoveOrdering) MoveOrdering.Sort(board, nextMoves);

            var sortedMoves = new List<RankedMove>();
            foreach (var move in nextMoves)
            {
                // if capture leads to a bad exchange, skip this move
                if (UseSEE && (move.CapturedPiece != PieceType.Empty))
                {
                    int exchangeEval = GetStaticExchangeEvaluation(board, move);

                    int goodExchangeSign = board.BlackToMove ? -1 : 1;
                    int currentExchangeSign = Math.Sign(exchangeEval);
                    if ((currentExchangeSign != 0) && (currentExchangeSign != goodExchangeSign))
                    {
                        continue;
                    }
                }

                board.ApplyMove(move, silent: true);
                int bestEval = Search(board, SearchDepth, playingAsBlack, MAX, MIN, out string seq);// + Random.Shared.Next(-5, 5);
                board.UndoLastMove(true);
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
            var nextMoves = BoardCalculator.GetAllValidMoves(board, board.BlackToMove).ToList();
            if (UseMoveOrdering) MoveOrdering.Sort(board, nextMoves);

            int bestEval;
            Move bestMove = new();
            string bestSeq = string.Empty;

            if (maximizing)
            {
                bestEval = MIN;

                foreach (var move in nextMoves)
                {
                    // if capture leads to a bad exchange, skip this move
                    if (UseSEE & (move.CapturedPiece != PieceType.Empty))
                    {
                        int exchangeEval = GetStaticExchangeEvaluation(board, move);

                        int goodExchangeSign = board.BlackToMove ? -1 : 1;
                        int currentExchangeSign = Math.Sign(exchangeEval);
                        if ((currentExchangeSign != 0) && (currentExchangeSign != goodExchangeSign))
                        {
                            continue;
                        }
                    }

                    board.ApplyMove(move, silent: true);
                    int bestMoveResult = Search(board, depthLimit - 1, !maximizing, minimumMaximizer, maximumMinimizer, out string nextSeq);
                    board.UndoLastMove(true);

                    if (bestMoveResult > bestEval)
                    {
                        bestEval = bestMoveResult;
                        bestMove = move;
                        bestSeq = nextSeq;
                    }

                    if (UseABPruning && bestEval > minimumMaximizer) break;

                    maximumMinimizer = Math.Max(maximumMinimizer, bestEval);
                }
            }
            else
            {
                bestEval = MAX;

                foreach (var move in nextMoves)
                {
                    // if capture leads to a bad exchange, skip this move
                    if (UseSEE && move.CapturedPiece != PieceType.Empty)
                    {
                        int exchangeEval = GetStaticExchangeEvaluation(board, move);

                        int goodExchangeSign = board.BlackToMove ? -1 : 1;
                        int currentExchangeSign = Math.Sign(exchangeEval);
                        if ((currentExchangeSign != 0) && (currentExchangeSign != goodExchangeSign))
                        {
                            continue;
                        }
                    }

                    board.ApplyMove(move, silent: true);
                    int bestMoveResult = Search(board, depthLimit - 1, !maximizing, minimumMaximizer, maximumMinimizer, out string nextSeq);
                    board.UndoLastMove(true);

                    if (bestMoveResult < bestEval)
                    {
                        bestEval = bestMoveResult;
                        bestMove = move;
                        bestSeq = nextSeq;
                    }

                    if (UseABPruning && bestEval < maximumMinimizer) break;

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
        const int MATE_PENALTY = MAX - 1;

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

        private static int GetStaticExchangeEvaluation(Board board, Move move)
        {
            var whiteAttackers = board.WhitePieces.GetAttackers(move.End, board.BlackPieces.PieceBitboard);
            whiteAttackers.Sort();

            var blackAttackers = board.BlackPieces.GetAttackers(move.End, board.WhitePieces.PieceBitboard);
            blackAttackers.Sort();

            bool blackToMove = board.BlackToMove;
            int whiteIndex = 0;
            int blackIndex = 0;

            int netMaterial = -Piece.SignedValue(move.CapturedPiece);
            PieceType currentOccupant = move.Piece;

            if ((whiteAttackers.Count > 0) && (whiteAttackers[whiteIndex].Location == move.Start)) whiteIndex++;
            if ((blackAttackers.Count > 0) && (blackAttackers[blackIndex].Location == move.Start)) blackIndex++;

            while ((!blackToMove && whiteIndex < whiteAttackers.Count) || (blackToMove && blackIndex < blackAttackers.Count))
            {
                netMaterial -= Piece.SignedValue(currentOccupant);

                if (blackToMove)
                {
                    if (blackAttackers[blackIndex].Location == move.Start) blackIndex++;
                    if (blackIndex >= blackAttackers.Count) break;

                    currentOccupant = blackAttackers[blackIndex].Piece;
                    blackIndex++;
                }
                else
                {
                    if (whiteAttackers[whiteIndex].Location == move.Start) whiteIndex++;
                    if (whiteIndex >= whiteAttackers.Count) break;

                    currentOccupant = whiteAttackers[whiteIndex].Piece;
                    whiteIndex++;
                }

                blackToMove = !blackToMove;
            }

            return netMaterial;
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
            int difference = Evaluation - other.Evaluation;
            if (difference != 0) return difference;
            return Move.GetHashCode() - other.Move.GetHashCode();
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
