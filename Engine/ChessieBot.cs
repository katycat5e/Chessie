using Chessie.Model;
using System.Diagnostics;

namespace Chessie.Engine
{
    public static class ChessieBot
    {
        const int SEARCH_DEPTH = 1;

        const int MAX = 999999;
        const int MIN = -MAX;

        public static float LastThinkDuration { get; private set; }

        public static List<RankedMove> RankPotentialMoves(BoardState board)
        {
            var watch = Stopwatch.StartNew();

            var nextMoves = BoardCalculator.GetAllValidMoves(board, board.BlackToMove);

            var sortedMoves = new List<RankedMove>();
            foreach (var move in nextMoves)
            {
                var result = board.ApplyMove(move);
                int bestEval = -Search(result, SEARCH_DEPTH, MAX, MIN, out string seq);// + Random.Shared.Next(-5, 5);
                sortedMoves.Add(new RankedMove(bestEval, move, seq));
            }

            static int CompareMoves(RankedMove a, RankedMove b)
            {
                return b.CompareTo(a);
            }

            sortedMoves.Sort(CompareMoves);

            watch.Stop();
            LastThinkDuration = watch.ElapsedMilliseconds / 1000f;

            return sortedMoves;
        }

        private static int Search(BoardState board, int depthLimit, int alpha, int beta, out string moveSeq)
        {
            if (depthLimit == 0)
            {
                moveSeq = string.Empty;
                return Evaluate(board);
            }

            //int best = MIN;
            moveSeq = string.Empty;
            var nextMoves = BoardCalculator.GetAllValidMoves(board, board.BlackToMove);

            foreach (var move in nextMoves)
            {
                var result = board.ApplyMove(move);
                int lookaheadEval = -Search(result, depthLimit - 1, -beta, -alpha, out string nextSeq);

                if (lookaheadEval >= beta)
                {
                    return beta;
                }
                //alpha = Math.Max(alpha, lookaheadEval);
                if (lookaheadEval > alpha)
                {
                    alpha = lookaheadEval;
                    moveSeq = move.ToString() + ',' + nextSeq;
                }
            }

            //return best;
            return alpha;
        }

        public static int Evaluate(BoardState board)
        {
            int eval = MaterialEval(board);

            if (BoardCalculator.IsMate(board, board.BlackToMove))
            {
                return EvalSignAdjust(-MATE_PENALTY, board.BlackToMove);
            }

            return EvalSignAdjust(eval, board.BlackToMove);
        }

        private static int EvalSignAdjust(int eval, bool forBlack)
        {
            return forBlack ? -eval : eval;
        }

        // returns white's material advantage
        private static int MaterialEval(BoardState board)
        {
            int total = 0;
            foreach (var piece in board)
            {
                if (piece.IsAnyPiece())
                {
                    total += PieceValue(piece);
                }
            }
            return total;
        }

        const int PAWN_VALUE = 100;
        const int KNIGHT_VALUE = 300;
        const int BISHOP_VALUE = 300;
        const int ROOK_VALUE = 500;
        const int QUEEN_VALUE = 900;

        const int CHECK_PENALTY = QUEEN_VALUE * 4;
        const int MATE_PENALTY = int.MaxValue;

        private static int PieceValue(PieceType piece)
        {
            return piece switch
            {
                Piece.P => PAWN_VALUE,
                Piece.N => KNIGHT_VALUE,
                Piece.B => BISHOP_VALUE,
                Piece.R => ROOK_VALUE,
                Piece.Q => QUEEN_VALUE,

                Piece.p => -PAWN_VALUE,
                Piece.n => -KNIGHT_VALUE,
                Piece.b => -BISHOP_VALUE,
                Piece.r => -ROOK_VALUE,
                Piece.q => -QUEEN_VALUE,
                _ => 0,
            };
        }

        private static int PST_Eval(BoardState board)
        {
            int eval = 0;
            foreach (var square in SquareCoord.AllSquares)
            {
                var piece = board[square];
                if (piece.IsWhitePiece())
                {
                    eval += PieceSquareTables.Evaluate(piece, square);
                }
                else if (piece.IsBlackPiece())
                {

                }
            }
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
