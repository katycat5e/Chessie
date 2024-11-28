using Chessie.Model;
using Chessie.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace Chessie
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public BoardViewModel BoardView { get; }
        public GameManager Game { get; }

        public MainWindow()
        {
            Game = new GameManager();
            BoardView = new BoardViewModel(Game);

            InitializeComponent();
        }

        private void MainBoard_SquareMousedown(object sender, SquareActionEventArgs e)
        {
            if (Game.IsMovablePiece(e.Coordinate))
            {
                Game.SelectedPiece = e.Coordinate;
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

            if (Game.HumanMoves.ContainsKey(e.Coordinate))
            {
                var move = Game.HumanMoves[e.Coordinate];
                var piece = Game.CurrentState[move.Start];

                PieceType? promotion = null;
                if (GameManager.IsPromotion(piece, move.End))
                {
                    promotion = PromotionSelector.PromptPromotion(this, piece.GetColor());
                }

                Game.MakeMove(Game.HumanMoves[e.Coordinate], promotion);
                Game.SelectedPiece = null;
            }
            else if (e.Coordinate != Game.SelectedPiece.Value)
            {
                Game.SelectedPiece = null;
            }

            Cursor = prevCursor;
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
    }
}