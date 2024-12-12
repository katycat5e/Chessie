namespace Chessie.Core.Model
{
    public static class Zobrist
    {
        const int SEED = 1733923798;

        const int HASHES_PER_SQUARE = 16;
        private static readonly ulong[][] _pieceLocations;
        private static readonly ulong _blackToMove;

        const int CASTLE_PERMUTATIONS = 16;
        private static readonly ulong[] _castleFlags;

        const int N_FILES = 8;
        private static readonly ulong[] _enPassantFiles;

        static Zobrist()
        {
            var rand = new Random(SEED);

            // piece locations
            _pieceLocations = new ulong[64][];

            for (int index = 0; index < _pieceLocations.Length; index++)
            {
                _pieceLocations[index] = new ulong[HASHES_PER_SQUARE];

                for (int typeIndex = 0; typeIndex < HASHES_PER_SQUARE; typeIndex++)
                {
                    _pieceLocations[index][typeIndex] = (ulong)rand.NextInt64();
                }
            }

            // turn indicator
            _blackToMove = (ulong)rand.NextInt64();

            // castling flags
            _castleFlags = new ulong[CASTLE_PERMUTATIONS];

            for (int combo = 0; combo < CASTLE_PERMUTATIONS; combo++)
            {
                _castleFlags[combo] = (ulong)rand.NextInt64();
            }

            // en passant file
            _enPassantFiles = new ulong[N_FILES + 1];

            for (int file = 0; file < N_FILES + 1; file++)
            {
                _enPassantFiles[file] = (ulong)rand.NextInt64();
            }
        }

        public static ulong GetHash(Board board)
        {
            ulong hash = 0;

            foreach (var piece in board.WhitePieces.AllPieces())
            {
                hash ^= _pieceLocations[piece.Location][(int)(piece.Piece - 8)];
            }

            foreach (var piece in board.BlackPieces.AllPieces())
            {
                hash ^= _pieceLocations[piece.Location][(int)(piece.Piece - 8)];
            }

            if (board.BlackToMove)
            {
                hash ^= _blackToMove;
            }

            hash ^= _castleFlags[(int)board.CastleState];

            int epFile = board.EnPassantSquare.HasValue ? board.EnPassantSquare.Value & 7 : N_FILES;
            hash ^= _enPassantFiles[epFile];

            return hash;
        }

        public static void TogglePiece(ref ulong hash, PieceType piece, int location)
        {
            hash ^= _pieceLocations[location][(int)(piece - 8)];
        }

        public static ulong ApplyMove(ulong startHash, Move move,
            CastleState oldCastleState, CastleState newCastleState,
            int? oldEnPassant, int? newEnPassant, PieceType? promotion)
        {
            // movement & capture
            ulong hash = startHash;
            TogglePiece(ref hash, move.Piece, move.Start);

            if (move.CapturedPiece != PieceType.Empty)
            {
                TogglePiece(ref hash, move.CapturedPiece, move.End);
            }
            else if (move.EnPassant)
            {
                var captured = (move.Piece == Piece.P) ? Piece.p : Piece.P;
                TogglePiece(ref hash, captured, move.EnPassantCapture);
            }

            PieceType destSquarePiece = promotion ?? move.Piece;
            TogglePiece(ref hash, destSquarePiece, move.End);

            // castling
            if (move.CastlingRookStart.HasValue)
            {
                var rook = PieceType.Rook | (move.Piece & PieceType.ColorMask);
                TogglePiece(ref hash, rook, move.CastlingRookStart.Value);
                TogglePiece(ref hash, rook, move.CastlingRookEnd);
            }

            if (oldCastleState != newCastleState)
            {
                hash ^= _castleFlags[(int)oldCastleState];
                hash ^= _castleFlags[(int)newCastleState];
            }

            // en passant
            if (oldEnPassant != newEnPassant)
            {
                int oldEpFile = oldEnPassant.HasValue ? oldEnPassant.Value & 7 : N_FILES;
                hash ^= _enPassantFiles[oldEpFile];

                int newEpFile = newEnPassant.HasValue ? newEnPassant.Value & 7 : N_FILES;
                hash ^= _enPassantFiles[newEpFile];
            }

            hash ^= _blackToMove;

            return hash;
        }

        public static ulong UndoMove(ulong hash, Board.UndoRecord record, CastleState currentCastleState, int? currentEnPassant)
        {
            if (record.Tertiary.HasValue) UndoMoveEntry(ref hash, record.Tertiary.Value);
            if (record.Secondary.HasValue) UndoMoveEntry(ref hash, record.Secondary.Value);
            UndoMoveEntry(ref hash, record.Primary);

            // castling
            if (currentCastleState != record.PrevCastleState)
            {
                hash ^= _castleFlags[(int)currentCastleState];
                hash ^= _castleFlags[(int)record.PrevCastleState];
            }

            // en passant
            if (currentEnPassant != record.PrevPassantSquare)
            {
                //if (currentEnPassant.HasValue) hash ^= _enPassantFiles[currentEnPassant.Value & 7];
                //if (record.PrevPassantSquare.HasValue) hash ^= _enPassantFiles[record.PrevPassantSquare.Value & 7];

                int oldEpFile = currentEnPassant.HasValue ? currentEnPassant.Value & 7 : N_FILES;
                hash ^= _enPassantFiles[oldEpFile];

                int newEpFile = record.PrevPassantSquare.HasValue ? record.PrevPassantSquare.Value & 7 : N_FILES;
                hash ^= _enPassantFiles[newEpFile];
            }

            hash ^= _blackToMove;
            return hash;
        }

        private static void UndoMoveEntry(ref ulong hash, Board.UndoEntry entry)
        {
            switch (entry.Type)
            {
                case Board.UndoEntryType.Moved:
                    TogglePiece(ref hash, entry.Piece, entry.NewIndex);
                    TogglePiece(ref hash, entry.Piece, entry.OriginalIndex);
                    break;

                case Board.UndoEntryType.Captured:
                    TogglePiece(ref hash, entry.Piece, entry.OriginalIndex);
                    break;

                case Board.UndoEntryType.Promoted:
                    var pawn = PieceType.Pawn | (entry.Piece & PieceType.ColorMask);
                    TogglePiece(ref hash, entry.Piece, entry.OriginalIndex);
                    TogglePiece(ref hash, pawn, entry.OriginalIndex);
                    break;
            }
        }
    }
}
