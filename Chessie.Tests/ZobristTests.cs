using Chessie.Core.Model;

namespace Chessie.Tests
{
    [TestClass]
    public sealed class ZobristTests
    {
        [TestMethod]
        public void TestPawnMoveUnmove()
        {
            var actual = BoardBuilder.CornerKings
                .AddPiece(Piece.P, Square.e2)
                .Build();

            ulong originalHash = actual.ZobristHash;

            var expected = BoardBuilder.CornerKings
                .AddPiece(Piece.P, Square.e3)
                .BlackToMove()
                .Build();

            var move = actual.CreateUCIMove("e2e3");
            actual.ApplyMove(move);

            Assert.AreEqual(expected.ZobristHash, actual.ZobristHash);

            actual.UndoLastMove();
            Assert.AreEqual(originalHash, actual.ZobristHash);
        }

        [TestMethod]
        public void TestPawnDoubleMoveUnmove()
        {
            var actual = BoardBuilder.CornerKings
                .AddPiece(Piece.P, Square.e2)
                .Build();

            ulong originalHash = actual.ZobristHash;

            var expected = BoardBuilder.CornerKings
                .AddPiece(Piece.P, Square.e4)
                .WithEnPassantSquare(Square.e3)
                .BlackToMove()
                .Build();

            var move = actual.CreateUCIMove("e2e4");
            actual.ApplyMove(move);

            Assert.AreEqual(expected.ZobristHash, actual.ZobristHash);
            
            actual.UndoLastMove();
            Assert.AreEqual(originalHash, actual.ZobristHash);
        }

        [TestMethod]
        public void TestKingsideCastle()
        {
            var actual = new BoardBuilder()
                .AddPiece(Piece.K, Square.e1)
                .AddPiece(Piece.R, Square.a1)
                .AddPiece(Piece.R, Square.h1)
                .AddPiece(Piece.k, Square.e8)
                .WithCastleState(CastleState.AllWhite)
                .Build();

            ulong originalHash = actual.ZobristHash;

            var expected = new BoardBuilder()
                .AddPiece(Piece.K, Square.g1)
                .AddPiece(Piece.R, Square.a1)
                .AddPiece(Piece.R, Square.f1)
                .AddPiece(Piece.k, Square.e8)
                .WithCastleState(CastleState.None)
                .BlackToMove()
                .Build();

            var move = actual.CreateUCIMove("e1g1");
            actual.ApplyMove(move);

            Assert.AreEqual(expected.ZobristHash, actual.ZobristHash);

            actual.UndoLastMove();
            Assert.AreEqual(originalHash, actual.ZobristHash);
        }
    }
}
