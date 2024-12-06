using Chessie.Core.Engine;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Threading;

namespace Chessie.GUI.ViewModels
{
    using MoveDictionary = Dictionary<int, Move>;

    public class GameManager : INotifyPropertyChanged
    {
        public event Action? SelectedPieceChanged;
        public event Action? BoardUpdated;
        public event PropertyChangedEventHandler? PropertyChanged;

        private Board _board;

        public Board Board
        {
            get => _board;

            [MemberNotNull(nameof(_board))]
            private set
            {
                _board = value;
                _board.StateChanged += OnBoardStateChanged;
            }
        }

        private void OnBoardStateChanged()
        {
            if (!_isAIThinking || ShowAIThoughts)
            {
                BoardUpdated?.Invoke();

                if (_isAIThinking) AllowUIToUpdate();
            }
        }


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

        public bool BlackToMove => Board.BlackToMove;

        public string TurnIndicator
        {
            get
            {
                if (CheckMate)
                {
                    return BlackToMove ? "White Wins by Checkmate" : "Black Wins by Checkmate";
                }
                return BlackToMove ? "Black to Move" : "White to Move";
            }
        }

        private int? _selectedPiece;

        public int? SelectedPiece
        {
            get => _selectedPiece;
            set
            {
                if (value == _selectedPiece) return;
                _selectedPiece = value;

                HumanMoves.Clear();
                if (value != null)
                {
                    var piece = new LocatedPiece(Board[value.Value], value.Value);
                    foreach (var move in BoardCalculator.GetValidMovesForPiece(Board, piece))
                    {
                        HumanMoves.Add(move.End, move);
                    }
                }

                SelectedPieceChanged?.Invoke();
            }
        }

        public int? CheckLocation { get; private set; }
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

        private bool _isAIThinking = false;

        private bool _showAIThoughts = false;
        public bool ShowAIThoughts
        {
            get => _showAIThoughts;
            set
            {
                _showAIThoughts = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowAIThoughts)));
            }
        }

        public ObservableStack<MoveRecord> PreviousMoves { get; } = new ObservableStack<MoveRecord>();

        #endregion


        public GameManager()
        {
            Board = new Board();
            StartNewGame();
        }

        public void StartNewGame()
        {
            BlackIsCPU = true;

            Board.Reset();
            SelectedPiece = null;
            CheckLocation = null;
            CheckMate = false;
            PreviousMoves.Clear();

            AIMoves = null;
            AIThinkDuration = string.Empty;

            NotifyBoardChanged();
            SelectedPieceChanged?.Invoke();
        }

        public void ApplyGameState(Board newState)
        {
            WhiteIsCPU = BlackIsCPU = false;

            Board = newState;
            SelectedPiece = null;
            PreviousMoves.Clear();

            AIMoves = null;
            AIThinkDuration = string.Empty;

            if (BoardCalculator.IsCheck(Board, BlackToMove, out var checkedKing))
            {
                CheckLocation = checkedKing;

                if (BoardCalculator.IsMate(Board, BlackToMove))
                {
                    CheckMate = true;
                }
            }
            else
            {
                CheckLocation = null;
            }

            NotifyBoardChanged();
            SelectedPieceChanged?.Invoke();
        }

        public bool IsMovablePiece(SquareCoord square)
        {
            var squareState = Board[square];
            var ownColor = BlackToMove ? PieceType.Black : PieceType.White;
            return (squareState & ownColor) != 0;
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
            Board.ApplyMove(move, promotion);

            bool isCheck, isMate = false;
            if (isCheck = BoardCalculator.IsCheck(Board, BlackToMove, out var checkedKing))
            {
                CheckLocation = checkedKing;

                if (isMate = BoardCalculator.IsMate(Board, BlackToMove))
                {
                    CheckMate = true;
                }
            }
            else
            {
                CheckLocation = null;
            }

            var record = new MoveRecord(move, availableMoves, promotion, isCheck, isMate);
            PreviousMoves.Push(record);
        }

        public void SetupNextTurn()
        {
            // setup AI moves
            bool isAITurn = BlackToMove ? BlackIsCPU : WhiteIsCPU;

            if (isAITurn)
            {
                _isAIThinking = true;
                AllowUIToUpdate();
                AIMoves = ChessieBot.RankPotentialMoves(Board);
                AIThinkDuration = $"({ChessieBot.LastThinkDuration:f2} s), ({ChessieBot.StatesEvaluated} nodes)";
                _isAIThinking = false;
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

        private static void AllowUIToUpdate()
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
