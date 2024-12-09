using Chessie.Core.Model;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace Chessie.Tests
{
    [TestClass]
    public sealed class BoardMoveTests
    {
        private const string START_STATE = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        private static (Board expected, Board actual) SetupFixture(string? fen = null)
        {
            fen ??= START_STATE;
            var expected = NotationProcessor.CreateBoardFromFEN(fen);
            var actual = expected.Clone();

            return (expected, actual);
        }

        private const string WHITE_MOVE_TEST = "r3k2r/1qb1p1n1/ppp5/8/8/PPP5/1QB1P1N1/R3K2R w KQkq - 0 1";
        private const string BLACK_MOVE_TEST = "r3k2r/1qb1p1n1/ppp5/8/8/PPP5/1QB1P1N1/R3K2R b KQkq - 0 1";

        [TestMethod]
        [DataRow("e2e3", WHITE_MOVE_TEST, DisplayName = "Pawn Single")]
        [DataRow("e2e4", WHITE_MOVE_TEST, DisplayName = "Pawn Double")]
        [DataRow("g2f4", WHITE_MOVE_TEST, DisplayName = "Knight")]
        [DataRow("c2h7", WHITE_MOVE_TEST, DisplayName = "Bishop")]
        [DataRow("a1d1", WHITE_MOVE_TEST, DisplayName = "Rook")]
        [DataRow("b2c1", WHITE_MOVE_TEST, DisplayName = "Queen")]
        [DataRow("e1f2", WHITE_MOVE_TEST, DisplayName = "King")]
        [DataRow("e1g1", WHITE_MOVE_TEST, DisplayName = "Kingside Castle")]
        [DataRow("e1c1", WHITE_MOVE_TEST, DisplayName = "Queenside Castle")]

        [DataRow("e7e6", BLACK_MOVE_TEST, DisplayName = "Black Pawn Single")]
        [DataRow("e7e5", BLACK_MOVE_TEST, DisplayName = "Black Pawn Double")]
        [DataRow("g7f5", BLACK_MOVE_TEST, DisplayName = "Black Knight")]
        [DataRow("c7h2", BLACK_MOVE_TEST, DisplayName = "Black Bishop")]
        [DataRow("h8f8", BLACK_MOVE_TEST, DisplayName = "Black Rook")]
        [DataRow("b7a7", BLACK_MOVE_TEST, DisplayName = "Black Queen")]
        [DataRow("e8d8", BLACK_MOVE_TEST, DisplayName = "Black King")]
        [DataRow("e8g8", BLACK_MOVE_TEST, DisplayName = "Black Kingside Castle")]
        [DataRow("e8c8", BLACK_MOVE_TEST, DisplayName = "Black Queenside Castle")]
        public void TestSimpleMove(string algebraic, string startFEN)
        {
            (var expected, var actual) = SetupFixture(startFEN);

            var move = actual.CreateUCIMove(algebraic);
            actual.ApplyMove(move);
            actual.UndoLastMove();

            AssertBoardsEqual(expected, actual);
        }

        [TestMethod]
        public void TestEnPassantWhite()
        {
            (var expected, var actual) = SetupFixture("k7/3p4/8/4P3/8/8/8/K7 b - - 0 1");

            var blackPawnDouble = actual.CreateUCIMove("d7d5");
            actual.ApplyMove(blackPawnDouble);

            var capture = actual.CreateUCIMove("e5d6");
            actual.ApplyMove(capture);

            Assert.AreNotEqual(Piece.p, actual[4, 3], "Black pawn was not captured");

            actual.UndoLastMove();
            actual.UndoLastMove();

            AssertBoardsEqual(expected, actual);
        }

        [TestMethod]
        public void TestEnPassantBlack()
        {
            (var expected, var actual) = SetupFixture("k7/8/8/8/3p4/8/4P3/K7 w - - 0 1");

            var whitePawnDouble = actual.CreateUCIMove("e2e4");
            actual.ApplyMove(whitePawnDouble);

            var capture = actual.CreateUCIMove("d4e3");
            actual.ApplyMove(capture);

            Assert.AreNotEqual(Piece.p, actual[3, 4], "White pawn was not captured");

            actual.UndoLastMove();
            actual.UndoLastMove();

            AssertBoardsEqual(expected, actual);
        }

        private static void AssertBoardsEqual(Board expected, Board actual)
        {
            CollectionAssert.AreEqual(expected.Squares, actual.Squares, "Board states differ");

            Assert.AreEqual(expected.CastleState, actual.CastleState, "Castle states differ");
            Assert.AreEqual(expected.BlackToMove, actual.BlackToMove, "Turn indicator differs");
            Assert.AreEqual(expected.EnPassantSquare, actual.EnPassantSquare, "En passant square differs");
            Assert.AreEqual(expected.PlyNumber, actual.PlyNumber, "Turn/ply number differs");

            Assert.AreEqual(expected.WhitePieces, actual.WhitePieces, "White piece maps differ");
            Assert.AreEqual(expected.BlackPieces, actual.BlackPieces, "Black piece maps differ");
        }
    }
}
