using Chessie.Core.Model;

namespace Chessie.Core
{
    internal class Program
    {
        static ulong _nCaptures;
        static ulong _nEnPassant;
        static ulong _nCastles;
        static ulong _nPromotions;
        

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("Invalid arguments");
            }

            int depth = int.Parse(args[0]);

            string[] moves = Array.Empty<string>();
            if (args.Length >= 3)
            {
                moves = args[2].Split(' ', StringSplitOptions.TrimEntries);
            }

            RunPerft(args[1], depth, moves);

            Console.Error.WriteLine($"Captures:   {_nCaptures}");
            Console.Error.WriteLine($"En Passant: {_nEnPassant}");
            Console.Error.WriteLine($"Castles:    {_nCastles}");
            Console.Error.WriteLine($"Promotions: {_nPromotions}");
            Console.Error.WriteLine();
        }

        private static void RunPerft(string fenCode, int depth, string[] preMoves)
        {
            var board = NotationProcessor.CreateBoardFromFEN(fenCode);

            foreach (string preMove in preMoves)
            {
                ApplyPreMove(board, preMove);
            }

            ulong totalNodes = 0;
            var moves = BoardCalculator.GetAllValidMoves(board, board.BlackToMove).ToList();

            foreach (var move in moves)
            {
                if (BoardCalculator.IsPromotion(move.Piece, move.End))
                {
                    var promotions = board.BlackToMove ? _blackPromotions : _whitePromotions;
                    foreach ((char indicator, PieceType promotion) in promotions)
                    {
                        if (move.CapturedPiece != PieceType.Empty) _nCaptures++;

                        board.ApplyMove(move, promotion);
                        ulong childNodeCount = Perft(board, depth - 1);
                        board.UndoLastMove();

                        Console.WriteLine($"{PerftMoveString(move)}{indicator} {childNodeCount}");

                        totalNodes += childNodeCount;
                    }
                    _nPromotions += (ulong)promotions.Length;
                }
                else
                {
                    if (move.CapturedPiece != PieceType.Empty) _nCaptures++;
                    if (move.EnPassant) _nEnPassant++;
                    if (move.CastlingRookStart.HasValue) _nCastles++;

                    board.ApplyMove(move);
                    ulong childNodeCount = Perft(board, depth - 1);
                    board.UndoLastMove();

                    Console.WriteLine($"{PerftMoveString(move)} {childNodeCount}");

                    totalNodes += childNodeCount;
                }
            }

            Console.WriteLine();
            Console.WriteLine(totalNodes);
        }

        private static readonly (char, PieceType)[] _whitePromotions = {
            ('n', Piece.N),
            ('b', Piece.B),
            ('r', Piece.R),
            ('q', Piece.Q),
        };

        private static readonly (char, PieceType)[] _blackPromotions = {
            ('n', Piece.n),
            ('b', Piece.b),
            ('r', Piece.r),
            ('q', Piece.q),
        };

        private static void ApplyPreMove(Board board, string preMove)
        {
            int startFile = preMove[0] - 'a';
            int startRank = preMove[1] - '1';
            int startIndex = startRank << 3 | startFile;

            int endFile = preMove[2] - 'a';
            int endRank = preMove[3] - '1';
            int endIndex = endRank << 3 | endFile;

            PieceType? promotion = null;
            if (preMove.Length == 5)
            {
                promotion = char.ToUpper(preMove[4]) switch
                {
                    'K' => PieceType.Knight,
                    'B' => PieceType.Bishop,
                    'R' => PieceType.Rook,
                    'Q' => PieceType.Queen,
                    _ => throw new ArgumentException("what dis: " + preMove[4]),
                };
            }

            var move = new Move(board[startIndex], board[endIndex], startIndex, endIndex);

            board.ApplyMove(move, promotion | (move.Piece & PieceType.ColorMask));
        }

        private static string PerftMoveString(Move move)
        {
            return $"{(char)('a' + move.StartFile)}{move.StartRank + 1}{(char)('a' + move.EndFile)}{move.EndRank + 1}";
        }

        private static ulong Perft(Board board, int depth)
        {
            if (depth == 0) return 1;

            ulong nodeCount = 0;

            var moves = BoardCalculator.GetAllValidMoves(board, board.BlackToMove).ToList();
            foreach (var move in moves)
            {
                if (BoardCalculator.IsPromotion(move.Piece, move.End))
                {
                    var promotions = board.BlackToMove ? _blackPromotions : _whitePromotions;
                    foreach ((_, PieceType promotion) in promotions)
                    {
                        if (move.CapturedPiece != PieceType.Empty) _nCaptures++;

                        board.ApplyMove(move, promotion);
                        nodeCount += Perft(board, depth - 1);
                        board.UndoLastMove();
                    }
                    _nPromotions += (ulong)promotions.Length;
                }
                else
                {
                    if (move.CapturedPiece != PieceType.Empty) _nCaptures++;
                    if (move.EnPassant) _nEnPassant++;
                    if (move.CastlingRookStart.HasValue) _nCastles++;

                    board.ApplyMove(move);
                    nodeCount += Perft(board, depth - 1);
                    board.UndoLastMove();
                }
            }

            return nodeCount;
        }
    }
}
