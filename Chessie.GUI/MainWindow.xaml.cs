using Chessie.GUI.ViewModels;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Chessie.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public BoardViewModel BoardView { get; }
        public GameManager Game { get; }
        public ChessieSettingsViewModel BotSettings { get; } = new();


        public bool AutoMove
        {
            get { return (bool)GetValue(AutoMoveProperty); }
            set { SetValue(AutoMoveProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AutoMove.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AutoMoveProperty =
            DependencyProperty.Register("AutoMove", typeof(bool), typeof(MainWindow), new PropertyMetadata(true));



        private readonly DispatcherTimer _autoMoveDispatcher;

        public MainWindow()
        {
            Game = new GameManager();
            BoardView = new BoardViewModel(Game);

            _autoMoveDispatcher = new DispatcherTimer
            {
                Interval = new TimeSpan(1),
            };
            _autoMoveDispatcher.Tick += AutoMove_Tick;

            InitializeComponent();
        }

        private void MainBoard_SquareMousedown(object sender, SquareActionEventArgs e)
        {
            if (Game.IsMovablePiece(e.Coordinate))
            {
                Game.SelectedPiece = e.Coordinate.Index;
            }
            else
            {
                Game.SelectedPiece = null;
            }
        }

        private void MainBoard_SquareDrop(object sender, SquareActionEventArgs e)
        {
            if (!Game.SelectedPiece.HasValue) return;

            var prevCursor = Cursor;
            Cursor = Cursors.Wait;

            if (Game.HumanMoves.TryGetValue(e.Coordinate.Index, out var move))
            {
                var piece = Game.Board[move.Start];

                PieceType? promotion = null;
                if (BoardCalculator.IsPromotion(piece, move.End))
                {
                    promotion = PromotionSelector.PromptPromotion(this, piece & PieceType.ColorMask);
                }

                Game.MakeMove(move, promotion);
                Game.SelectedPiece = null;

                if (AutoMove && Game.IsAITurn)
                {
                    _autoMoveDispatcher.Start();
                }
            }
            else if (e.Coordinate.Index != Game.SelectedPiece)
            {
                Game.SelectedPiece = null;
            }

            Cursor = prevCursor;
            PreviousMovesScroll.ScrollToBottom();
        }

        private void AutoMove_Tick(object? sender, EventArgs e)
        {
            _autoMoveDispatcher.Stop();

            if (!Game.IsAITurn) return;

            var prevCursor = Cursor;
            Cursor = Cursors.Wait;

            Game.MakeAIMove();

            Cursor = prevCursor;

            PreviousMovesScroll.ScrollToBottom();

            if (AutoMove && Game.IsAITurn)
            {
                _autoMoveDispatcher.Start();
            }
        }


        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            Game.StartNewGame();
        }

        private void ImportFEN_Click(object sender, RoutedEventArgs e)
        {
            string? fen = StringPrompt.ShowPrompt(this, "Enter FEN code");
            if (!string.IsNullOrEmpty(fen) )
            {
                try
                {
                    var board = NotationProcessor.CreateBoardFromFEN(fen);
                    Game.ApplyGameState(board);
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show($"Error processing FEN code: \n{ex.Message}", "FEN Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AIMoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (Game.IsAITurn)
            {
                Game.MakeAIMove();
            }
        }

        private void RunPerft_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PerftDialog()
            {
                Owner = this,
                Board = Game.Board,
            };

            dialog.ShowDialog();
        }

        private void RefreshBot_Click(object sender, RoutedEventArgs e)
        {
            Game.SetupNextTurn();
        }

        private BitboardDialog? _bitboardWindow = null;
        private void ShowBitboards_Click(object sender, RoutedEventArgs e)
        {
            if (_bitboardWindow is not null)
            {
                _bitboardWindow.Focus();
                return;
            }

            _bitboardWindow = new BitboardDialog(Game.Board)
            {
                Owner = this,
            };

            _bitboardWindow.Show();
            _bitboardWindow.Closed += (_, _) => _bitboardWindow = null;
        }
    }
}