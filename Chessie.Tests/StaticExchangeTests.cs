using Chessie.Core.Engine;
using Chessie.Core.Model;

namespace Chessie.Tests;

[TestClass]
public class StaticExchangeTests
{
    [TestMethod]
    public void TestWhitePawns2v1()
    {
        /* p
         *  p 
         * P P */
        var board = NotationProcessor.CreateBoardFromFEN("k7/8/3p4/4p3/3P1P2/8/8/K7 w - - 0 1");
        var initialMove = board.CreateUCIMove("d4e5"); // white pawn captures black

        int evaluation = ChessieBot.GetStaticExchangeEvaluation(board, initialMove);

        Assert.AreEqual(Piece.SignedValue(Piece.P), evaluation, "SEE should result in white +1 pawn");
    }

    [TestMethod]
    public void TestWhitePawns2v2()
    {
        /* p p
         *  p 
         * P P */
        var board = NotationProcessor.CreateBoardFromFEN("k7/8/3p1p2/4p3/3P1P2/8/8/K7 w - - 0 1");
        var initialMove = board.CreateUCIMove("d4e5"); // white pawn captures black

        int evaluation = ChessieBot.GetStaticExchangeEvaluation(board, initialMove);

        Assert.AreEqual(0, evaluation, "SEE should result in net zero material gain");
    }

    [TestMethod]
    public void TestWhitePawns1v2()
    {
        /* p p
         *  p 
         * P   */
        var board = NotationProcessor.CreateBoardFromFEN("k7/8/3p1p2/4p3/3P4/8/8/K7 w - - 0 1");
        var initialMove = board.CreateUCIMove("d4e5"); // white pawn captures black

        int evaluation = ChessieBot.GetStaticExchangeEvaluation(board, initialMove);

        Assert.AreEqual(0, evaluation, "SEE should result in net zero material gain");
    }

    [TestMethod]
    public void TestBlackPawns2v1()
    {
        /* p p
         *  P 
         * P   */
        var board = NotationProcessor.CreateBoardFromFEN("k7/8/3p1p2/4P3/3P4/8/8/K7 b - - 0 1");
        var initialMove = board.CreateUCIMove("d6e5"); // black pawn captures white

        int evaluation = ChessieBot.GetStaticExchangeEvaluation(board, initialMove);

        Assert.AreEqual(Piece.SignedValue(Piece.p), evaluation, "SEE should result in black +1 pawn");
    }

    [TestMethod]
    public void TestKnightBadPawnCapture()
    {
        /* p
         *  p
         * 
         * K  */
        var board = NotationProcessor.CreateBoardFromFEN("k7/8/3p4/4p3/8/3N4/8/K7 w - - 0 1");
        var move = board.CreateUCIMove("d3e5"); // white knight captures pawn

        int actualSEE = ChessieBot.GetStaticExchangeEvaluation(board, move);

        int expectedSEE = Piece.UnsignedValue(PieceType.Pawn) - Piece.UnsignedValue(PieceType.Knight); // white up 1 pawn, down 1 knight
        Assert.AreEqual(expectedSEE, actualSEE, "SEE should result in white +1 pawn -1 knight");
    }

    [TestMethod]
    public void TestQueenBadRookCapture()
    {
        /*  r
         *  r
         * Q  */
        var board = NotationProcessor.CreateBoardFromFEN("k7/8/4r3/4r3/3Q4/8/8/K7 w - - 0 1");
        var move = board.CreateUCIMove("d4e5"); // white queen captures rook

        int actualSEE = ChessieBot.GetStaticExchangeEvaluation(board, move);

        int expectedSEE = Piece.UnsignedValue(PieceType.Rook) - Piece.UnsignedValue(PieceType.Queen); // white up 1 rook, down 1 queen
        Assert.AreEqual(expectedSEE, actualSEE, "SEE should result in white +1 rook -1 queen");
    }

    [TestMethod]
    public void TestQueen2xRookExchange()
    {
        /*  r
         *  r
         * Q P */
        var board = NotationProcessor.CreateBoardFromFEN("k7/8/4r3/4r3/3Q1P2/8/8/K7 w - - 0 1");
        var move = board.CreateUCIMove("d4e5"); // white queen captures rook

        int actualSEE = ChessieBot.GetStaticExchangeEvaluation(board, move);

        int expectedSEE = 2 * Piece.UnsignedValue(PieceType.Rook) - Piece.UnsignedValue(PieceType.Queen); // white up 1 rook, down 1 queen
        Assert.AreEqual(expectedSEE, actualSEE, "SEE should result in white +1 rook -1 queen");
    }
}
