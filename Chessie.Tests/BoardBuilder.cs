using Chessie.Core.Model;

namespace Chessie.Tests
{
    internal class BoardBuilder
    {
        private readonly List<LocatedPiece> _pieces = [];
        private CastleState _castleState = CastleState.None;
        private int _plyNumber = 0;
        private bool _blackToMove = false;
        private int? _enPassantSquare = null;

        public BoardBuilder()
        {

        }

        public static BoardBuilder CornerKings
        {
            get
            {
                return new BoardBuilder()
                    .AddPiece(Piece.K, Square.a1)
                    .AddPiece(Piece.k, Square.a8);
            }
        }

        public BoardBuilder WithCastleState(CastleState state)
        {
            _castleState = state;
            return this;
        }

        public BoardBuilder WithEnPassantSquare(Square? epSquare)
        {
            _enPassantSquare = (int?)epSquare;
            return this;
        }

        public BoardBuilder BlackToMove()
        {
            _blackToMove = true;
            return this;
        }

        public BoardBuilder AtPlyNumber(int plyNumber)
        {
            _plyNumber = plyNumber;
            return this;
        }

        public BoardBuilder AddPiece(PieceType piece, Square square)
        {
            _pieces.Add(new LocatedPiece(piece, (int)square));
            return this;
        }

        public Board Build()
        {
            var squares = new PieceType[64];

            foreach (var piece in _pieces)
            {
                squares[piece.Location] = piece.Piece;
            }

            return new Board(squares, _castleState, _plyNumber, _enPassantSquare, _blackToMove);
        }
    }
}
