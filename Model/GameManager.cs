using Chessie.Engine;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace Chessie.Model
{
    using MoveDictionary = Dictionary<SquareCoord, Move>;

    public class GameManager : INotifyPropertyChanged
    {
        public event Action<SquareCoord?>? SelectedPieceChanged;
        public event Action? BoardUpdated;
        public event PropertyChangedEventHandler? PropertyChanged;


        #region UI Properties

        private bool _whiteIsCPU;
        public bool WhiteIsCPU
        {
            get => _whiteIsCPU;
            set
            {
                _whiteIsCPU = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WhiteIsCPU)));
            }
        }

        private bool _blackIsCPU;
        public bool BlackIsCPU
        {
            get => _blackIsCPU;
            set
            {
                _blackIsCPU = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BlackIsCPU)));
            }
        }


        private BoardState _currentState;
        public BoardState CurrentState
        {
            get => _currentState;
            private set
            {
                _currentState = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BlackToMove)));
            }
        }

        public bool BlackToMove => CurrentState.BlackToMove;

        public string TurnIndicator
        {
            get
            {
                if (CheckMate)
                {
                    return CurrentState.BlackToMove ? "White Wins by Checkmate" : "Black Wins by Checkmate";
                }
                return CurrentState.BlackToMove ? "Black to Move" : "White to Move";
            }
        }

        private SquareCoord? _selectedPiece;

        public SquareCoord? SelectedPiece
        {
            get => _selectedPiece;
            set
            {
                if (value == _selectedPiece) return;
                _selectedPiece = value;

                HumanMoves.Clear();
                if (value != null)
                {
                    foreach (var move in BoardCalculator.GetValidMovesForPiece(CurrentState, value.Value))
                    {
                        HumanMoves.Add(move.End, move);
                    }
                }

                SelectedPieceChanged?.Invoke(value);
            }
        }

        public SquareCoord? CheckLocation { get; private set; }
        public bool CheckMate { get; private set; }

        public MoveDictionary HumanMoves { get; } = new MoveDictionary();

        private List<RankedMove>? _aiMoves;
        public List<RankedMove>? AIMoves
        {
            get => _aiMoves;
            set
            {
                _aiMoves = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AIMoves)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAITurn)));
            }
        }

        private string _aiThinkDuration = string.Empty;
        public string AIThinkDuration
        {
            get => _aiThinkDuration;
            set
            {
                _aiThinkDuration = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AIThinkDuration)));
            }
        }

        public bool IsAITurn => _aiMoves != null;

        public ObservableStack<MoveRecord> PreviousMoves { get; } = new ObservableStack<MoveRecord>();

        #endregion


        public GameManager()
        {
            StartNewGame();
        }

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

        public void StartNewGame()
        {
            BlackIsCPU = true;

            CurrentState = new BoardState(START_STATE)
            {
                CastleState = CastleState.All,
            };
            SelectedPiece = null;
            CheckLocation = null;
            CheckMate = false;
            PreviousMoves.Clear();

            AIMoves = null;
            AIThinkDuration = string.Empty;

            NotifyBoardChanged();
            SelectedPieceChanged?.Invoke(null);
        }

        public void ApplyGameState(BoardState newState)
        {
            WhiteIsCPU = BlackIsCPU = false;

            CurrentState = newState;
            SelectedPiece = null;
            PreviousMoves.Clear();

            AIMoves = null;
            AIThinkDuration = string.Empty;

            if (BoardCalculator.IsCheck(CurrentState, CurrentState.BlackToMove, out var checkedKing))
            {
                CheckLocation = checkedKing;

                if (BoardCalculator.IsMate(CurrentState, CurrentState.BlackToMove))
                {
                    CheckMate = true;
                }
            }
            else
            {
                CheckLocation = null;
            }

            NotifyBoardChanged();
            SelectedPieceChanged?.Invoke(null);
        }

        public bool IsMovablePiece(SquareCoord square)
        {
            var squareState = CurrentState[square];

            return (squareState != PieceType.Empty) && (squareState.IsBlackPiece() == CurrentState.BlackToMove);
        }

        public static bool IsPromotion(PieceType piece, SquareCoord destination)
        {
            return piece.IsPieceType(PieceType.Pawn) && (destination.IsInLastRank || destination.IsInFirstRank);
        }

        public void MakeMove(Move move, PieceType? promotion = null)
        {
            MakeMoveInternal(move, promotion, HumanMoves.Values);

            NotifyBoardChanged();
            SetupNextTurn();
        }

        public void MakeAIMove()
        {
            var move = AIMoves!.First();
            MakeMoveInternal(move.Move, move.Promotion, AIMoves!.Select(rm => rm.Move));

            NotifyBoardChanged();
            SetupNextTurn();
        }

        private void MakeMoveInternal(Move move, PieceType? promotion, IEnumerable<Move> availableMoves)
        {
            var previousState = CurrentState;
            CurrentState = CurrentState.ApplyMove(move, promotion);

            bool isCheck, isMate = false;
            if (isCheck = BoardCalculator.IsCheck(CurrentState, CurrentState.BlackToMove, out var checkedKing))
            {
                CheckLocation = checkedKing;

                if (isMate = BoardCalculator.IsMate(CurrentState, CurrentState.BlackToMove))
                {
                    CheckMate = true;
                }
            }
            else
            {
                CheckLocation = null;
            }

            var record = new MoveRecord(previousState, move, availableMoves, promotion, isCheck, isMate);
            PreviousMoves.Push(record);
        }

        private void SetupNextTurn()
        {
            // setup AI moves
            bool isAITurn = BlackToMove ? BlackIsCPU : WhiteIsCPU;

            if (isAITurn)
            {
                AllowUIToUpdate();
                AIMoves = ChessieBot.RankPotentialMoves(CurrentState);
                AIThinkDuration = $"({ChessieBot.LastThinkDuration:f2} s)";
            }
            else
            {
                AIMoves = null;
                AIThinkDuration = string.Empty;
            }
        }

        private void NotifyBoardChanged()
        {
            BoardUpdated?.Invoke();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TurnIndicator)));
        }

        private void AllowUIToUpdate()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new DispatcherOperationCallback(delegate (object parameter)
            {
                frame.Continue = false;
                return null;
            }), null);

            Dispatcher.PushFrame(frame);
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }
    }
}
