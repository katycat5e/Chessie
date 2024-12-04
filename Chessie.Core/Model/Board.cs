using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chessie.Core.Model
{
    public class Board
    {
        public PieceType[] Squares { get; }

        public PieceType this[int index]
        {
            get => Squares[index];
            set => Squares[index] = value;
        }
        
        public PieceType this[int rank, int file]
        {
            get => Squares[(rank * 8) + file];
            set => Squares[(rank * 8) + file] = value;
        }
        
        public PieceType this[SquareCoord coord]
        {
            get => Squares[(coord.Rank * 8) + coord.File];
            set => Squares[(coord.Rank * 8) + coord.File] = value;
        }
        

        public CastleState CastleState { get; private set; }
        public int PlyNumber { get; private set; }
        public int? EnPassantSquare { get; private set; }
        public bool BlackToMove { get; private set; }

        private readonly Stack<UndoRecord> _moveHistory = new();

        public PieceMap WhitePieces { get; }
        public PieceMap BlackPieces { get; }

        public PieceMap GetMap(PieceType color) => (color & PieceType.Black) != 0 ? BlackPieces : WhitePieces;
        public PieceMap GetMap(bool forBlack) => forBlack ? BlackPieces : WhitePieces;

        public PieceMap GetOpponentMap(PieceType color) => (color & PieceType.Black) != 0 ? WhitePieces : BlackPieces;
        public PieceMap GetOpponentMap(bool forBlack) => forBlack ? WhitePieces : BlackPieces;

        public event Action? StateChanged;

        public Board()
        {
            Squares = new PieceType[64];

            WhitePieces = new PieceMap(PieceType.White);
            BlackPieces = new PieceMap(PieceType.Black);

            WhitePieces.InitFromBoard(Squares);
            BlackPieces.InitFromBoard(Squares);
        }

        public Board(PieceType[] squares, CastleState castleState, int plyNumber, int? enPassantSquare, bool blackToMove)
        {
            Squares = squares;
            CastleState = castleState;
            PlyNumber = plyNumber;
            EnPassantSquare = enPassantSquare;
            BlackToMove = blackToMove;

            WhitePieces = new PieceMap(PieceType.White);
            BlackPieces = new PieceMap(PieceType.Black);

            WhitePieces.InitFromBoard(Squares);
            BlackPieces.InitFromBoard(Squares);
        }

        public void Reset()
        {
            Array.Copy(START_STATE, Squares, 64);

            WhitePieces.InitFromBoard(Squares);
            BlackPieces.InitFromBoard(Squares);
        }

        public void ApplyMove(Move move, PieceType? promotion = null)
        {
            Squares[move.Start] = PieceType.Empty;
            var capture = Squares[move.End];
            Squares[move.End] = move.Piece;

            UndoEntry primaryUndo = UndoEntry.Move(move, capture == PieceType.Empty);
            UndoEntry? secondaryUndo = null;
            UndoEntry? tertiaryUndo = null;

            // en passant
            if (move.EnPassant)
            {
                var enPassantPawn = Squares[move.EnPassantCapture];
                Squares[move.EnPassantCapture] = PieceType.Empty;
                GetOpponentMap(move.Piece).RemovePiece(PieceType.Pawn, move.EnPassantCapture);

                secondaryUndo = UndoEntry.Capture(enPassantPawn, move.EnPassantCapture);
            }
            // capture
            else if (capture != PieceType.Empty)
            {
                GetMap(capture).RemovePiece(capture, move.End);
                secondaryUndo = UndoEntry.Capture(capture, move.End);
            }
            // castle
            else if (move.CastlingRookStart.HasValue)
            {
                int rookStart = move.CastlingRookStart.Value;
                int rookEnd = move.CastlingRookEnd;

                var rook = Squares[rookStart];
                Squares[rookStart] = PieceType.Empty;
                Squares[rookEnd] = rook;
                secondaryUndo = UndoEntry.Move(rook, rookStart, rookEnd, true);

                GetMap(rook).MovePiece(rook, rookStart, rookEnd);
            }

            int? oldPassantSquare = EnPassantSquare;
            EnPassantSquare = move.IsPawnDoubleMove ? move.EnPassantCapture : null;

            // promotion
            if (promotion.HasValue)
            {
                Squares[move.End] = promotion.Value;
                GetMap(move.Piece).RemovePiece(move.Piece, move.Start);
                GetMap(move.Piece).AddPiece(promotion.Value, move.End);

                tertiaryUndo = UndoEntry.Promotion(promotion.Value, move.End);
            }
            // regular move
            else
            {
                var toMoveMap = GetMap(move.Piece);
                toMoveMap.MovePiece(move.Piece, move);
            }

            // castling flags
            var oldCastleState = CastleState;
            if ((move.Piece & PieceType.King) != 0)
            {
                CastleState mask = BlackToMove ? CastleState.AllBlack : CastleState.AllWhite;
                CastleState &= ~mask;
            }
            else if ((move.Piece & PieceType.Rook) != 0)
            {
                if (move.Start == WKRookStart)
                {
                    CastleState &= ~CastleState.WhiteKingside;
                }
                else if (move.Start == WQRookStart)
                {
                    CastleState &= ~CastleState.WhiteQueenside;
                }
                else if (move.Start == BKRookStart)
                {
                    CastleState &= ~CastleState.BlackKingside;
                }
                else if (move.Start == BQRookStart)
                {
                    CastleState &= ~CastleState.BlackQueenside;
                }
            }
            else if((capture & PieceType.Rook) != 0)
            {
                if (move.End == WKRookStart)
                {
                    CastleState &= ~CastleState.WhiteKingside;
                }
                else if (move.End == WQRookStart)
                {
                    CastleState &= ~CastleState.WhiteQueenside;
                }
                else if (move.End == BKRookStart)
                {
                    CastleState &= ~CastleState.BlackKingside;
                }
                else if (move.End == BQRookStart)
                {
                    CastleState &= ~CastleState.BlackQueenside;
                }
            }

            BlackToMove = !BlackToMove;
            PlyNumber++;

            var undoEntry = new UndoRecord(oldCastleState, oldPassantSquare, primaryUndo, secondaryUndo, tertiaryUndo);
            _moveHistory.Push(undoEntry);

            ClearBitboardCache();

            StateChanged?.Invoke();
        }


        #region Bitboards

        private ulong? _allPiecesBitBoard = null;
        private BitBoard? _blackBitboards = null;
        private BitBoard? _whiteBitboards = null;

        private void ClearBitboardCache()
        {
            _blackBitboards = null;
            _whiteBitboards = null;
            _allPiecesBitBoard = null;
        }

        public BitBoard GetBitboards(bool forBlack)
        {
            var cached = forBlack ? _blackBitboards : _whiteBitboards;
            if (cached.HasValue)
            {
                return cached.Value;
            }

            if (!_allPiecesBitBoard.HasValue)
            {
                _allPiecesBitBoard = RegenPieceMap();
            }

            ulong threatMap = 0;
            ulong checkMap = 0;

            bool opponentIsBlack = !forBlack;
            PieceType ownColor = forBlack ? PieceType.Black : PieceType.White;
            PieceType opponentColor = forBlack ? PieceType.White : PieceType.Black;
            var opponentPieces = GetOpponentMap(forBlack).AllPieces();

            MoveVector[] potentialMoves;

            foreach (var enemy in opponentPieces)
            {
                switch (enemy.Piece & ~PieceType.ColorMask)
                {
                    // Simple moving pieces (non-slider)
                    case PieceType.Pawn:
                        potentialMoves = opponentIsBlack ? BLACK_PAWN_THREATS : WHITE_PAWN_THREATS;
                        goto checkSimpleThreat;

                    case PieceType.Knight:
                        potentialMoves = BoardCalculator.KnightMoves;
                        goto checkSimpleThreat;

                    case PieceType.King:
                        potentialMoves = BoardCalculator.AllVectors;

                    checkSimpleThreat:
                        foreach (var move in potentialMoves)
                        {
                            if (!ValidMove(enemy.Location, move))
                            {
                                continue;
                            }

                            int target = enemy.Location + move.DeltaIndex;
                            ulong bit = 1ul << target;
                            threatMap |= bit;
                            checkMap |= bit;
                        }
                        break;

                    // Slider pieces
                    case PieceType.Bishop:
                        potentialMoves = BoardCalculator.BishopVectors;
                        goto checkSliderThreat;

                    case PieceType.Rook:
                        potentialMoves = BoardCalculator.RookVectors;
                        goto checkSliderThreat;

                    case PieceType.Queen:
                        potentialMoves = BoardCalculator.AllVectors;

                    checkSliderThreat:
                        foreach (var vector in potentialMoves)
                        {
                            bool hitMyKing = false;
                            var move = vector;

                            for (int scale = 1; scale <= 7; scale++)
                            {
                                if (!ValidMove(enemy.Location, move))
                                {
                                    break;
                                }

                                int target = enemy.Location + move.DeltaIndex;

                                ulong bit = 1ul << target;
                                if (!hitMyKing) threatMap |= bit;
                                checkMap |= bit;

                                if ((Squares[target] & PieceType.PieceMask) != 0)
                                {
                                    // piece is blocking
                                    break;
                                }
                                else if (Squares[target] == (PieceType.King | ownColor))
                                {
                                    // intersecting own king, block threat but allow check to pass thru
                                    hitMyKing = true;
                                }

                                move += vector;
                            }
                        }
                        break;
                }
            }

            var result = new BitBoard(_allPiecesBitBoard.Value, threatMap, checkMap);
            if (forBlack)
            {
                _blackBitboards = result;
            }
            else
            {
                _whiteBitboards = result;
            }
            return result;
        }

        private ulong RegenPieceMap()
        {
            ulong result = 0;
            foreach (var piece in WhitePieces.AllPieces())
            {
                result |= (1ul << piece.Location);
            }
            foreach (var piece in BlackPieces.AllPieces())
            {
                result |= (1ul << piece.Location);
            }
            return result;
        }

        private static bool ValidMove(int start, MoveVector move)
        {
            int startRank = start >> 3;
            int startFile = start & 7;

            int endRank = startRank + move.DeltaRank;
            int endFile = startFile + move.DeltaFile;

            return (endRank >= 0) && (endRank <= 7) && (endFile >= 0) && (endFile <= 7);
        }

        private static readonly MoveVector[] WHITE_PAWN_THREATS = { MoveVector.UP_LEFT, MoveVector.UP_RIGHT };
        private static readonly MoveVector[] BLACK_PAWN_THREATS = { MoveVector.DOWN_LEFT, MoveVector.DOWN_RIGHT };

        #endregion


        private static readonly PieceType[] START_STATE =
        {
            Piece.R, Piece.N, Piece.B, Piece.Q, Piece.K, Piece.B, Piece.N, Piece.R,
            Piece.P, Piece.P, Piece.P, Piece.P, Piece.P, Piece.P, Piece.P, Piece.P,

            Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X,
            Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X,
            Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X,
            Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X, Piece.X,

            Piece.p, Piece.p, Piece.p, Piece.p, Piece.p, Piece.p, Piece.p, Piece.p,
            Piece.r, Piece.n, Piece.b, Piece.q, Piece.k, Piece.b, Piece.n, Piece.r,
        };

        // rook start position indices
        private static readonly int WQRookStart = 0;
        private static readonly int WKRookStart = 7;
        private static readonly int BQRookStart = 56;
        private static readonly int BKRookStart = 63;


        #region Undo Stack

        public void UndoLastMove()
        {
            var record = _moveHistory.Pop();

            if (record.Tertiary.HasValue) ExecuteUndoEntry(record.Tertiary.Value);
            if (record.Secondary.HasValue) ExecuteUndoEntry(record.Secondary.Value);
            ExecuteUndoEntry(record.Primary);

            CastleState = record.PrevCastleState;
            EnPassantSquare = record.PrevPassantSquare;

            BlackToMove = !BlackToMove;
            PlyNumber--;

            ClearBitboardCache();

            StateChanged?.Invoke();
        }

        private void ExecuteUndoEntry(UndoEntry entry)
        {
            switch (entry.Type)
            {
                case UndoEntryType.Moved:
                    Squares[entry.OriginalIndex] = entry.Piece;
                    GetMap(entry.Piece).MovePiece(entry.Piece, entry.NewIndex, entry.OriginalIndex);

                    if (entry.DestWasEmpty)
                    {
                        Squares[entry.NewIndex] = PieceType.Empty;
                    }
                    break;

                case UndoEntryType.Captured:
                    Squares[entry.OriginalIndex] = entry.Piece;
                    GetMap(entry.Piece).AddPiece(entry.Piece, entry.OriginalIndex);
                    break;

                case UndoEntryType.Promoted:
                    GetMap(entry.Piece).RemovePiece(entry.Piece, entry.OriginalIndex);
                    GetMap(entry.Piece).AddPiece(PieceType.Pawn, entry.OriginalIndex);
                    break;
            }
        }


        private readonly struct UndoRecord
        {
            public readonly CastleState PrevCastleState;
            public readonly int? PrevPassantSquare;
            public readonly UndoEntry Primary;
            public readonly UndoEntry? Secondary;
            public readonly UndoEntry? Tertiary;

            public UndoRecord(CastleState prevCastleState, int? passantSquare, UndoEntry primary, UndoEntry? secondary, UndoEntry? tertiary)
            {
                PrevCastleState = prevCastleState;
                PrevPassantSquare = passantSquare;
                Primary = primary;
                Secondary = secondary;
                Tertiary = tertiary;
            }
        }

        private readonly struct UndoEntry
        {
            public readonly UndoEntryType Type;
            public readonly PieceType Piece;
            public readonly int OriginalIndex;
            public readonly int NewIndex;
            public readonly bool DestWasEmpty;

            private UndoEntry(UndoEntryType type, PieceType piece, int oldIndex, int newIndex, bool destWasEmpty = true)
            {
                Type = type;
                Piece = piece;
                OriginalIndex = oldIndex;
                NewIndex = newIndex;
                DestWasEmpty = destWasEmpty;
            }

            public static UndoEntry Move(Move move, bool toEmptySquare) =>
                new(UndoEntryType.Moved, move.Piece, move.Start, move.End, toEmptySquare);

            public static UndoEntry Move(PieceType piece, int oldIndex, int newIndex, bool toEmptySquare) =>
                new(UndoEntryType.Moved, piece, oldIndex, newIndex, toEmptySquare);

            public static UndoEntry Capture(PieceType piece, int oldIndex) =>
                new(UndoEntryType.Captured, piece, oldIndex, -1);

            public static UndoEntry Promotion(PieceType promotion, int index) =>
                new(UndoEntryType.Promoted, promotion, index, index);

            public override string ToString()
            {
                return $"{Type} {Piece.TypeIcon()}{new SquareCoord(OriginalIndex)}->{new SquareCoord(NewIndex)}";
            }
        }

        private enum UndoEntryType
        {
            Moved,
            Captured,
            Promoted,
        }

        #endregion


        public string DebugView
        {
            get
            {
                var chars = new char[72];

                for (int rank = 7; rank >= 0; rank--)
                {
                    for (int file = 0; file <= 8; file++)
                    {
                        int stringIndex = ((7 - rank) * 9) + file;

                        if (file == 8)
                        {
                            chars[stringIndex] = (stringIndex == 71) ? '\0' : '\n';
                        }
                        else
                        {
                            int squareIndex = (rank << 3) | file;
                            chars[stringIndex] = Squares[squareIndex].FenId();
                        }
                    }
                }

                return new string(chars);
            }
        }
    }


    [Flags]
    public enum CastleState : byte
    {
        None = 0,

        WhiteKingside = 1,
        WhiteQueenside = 2,

        BlackKingside = 4,
        BlackQueenside = 8,

        AllWhite = WhiteKingside | WhiteQueenside,
        AllBlack = BlackKingside | BlackQueenside,
        All = AllWhite | AllBlack,
    }

    public readonly struct BitBoard
    {
        public readonly ulong AllPieces;
        public readonly ulong Threats;
        public readonly ulong KingThreats;

        public BitBoard(ulong allPieces, ulong threats, ulong kingThreats)
        {
            AllPieces = allPieces;
            Threats = threats;
            KingThreats = kingThreats;
        }
    }
}
