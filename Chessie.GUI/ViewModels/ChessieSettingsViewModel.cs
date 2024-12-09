using Chessie.Core.Engine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Chessie.GUI.ViewModels
{
    public class ChessieSettingsViewModel : INotifyPropertyChanged
    {
        public int[] DepthOptions { get; } = Enumerable.Range(1, 10).ToArray();

        public int Depth
        {
            get => ChessieBot.SearchDepth;
            set
            {
                ChessieBot.SearchDepth = value;
                Notify(nameof(Depth));
            }
        }

        public bool DeterministicSearch
        {
            get => ChessieBot.DeterministicSearch;
            set
            {
                ChessieBot.DeterministicSearch = value;
                PieceMap.SortPieces = value;
                Notify(nameof(DeterministicSearch));
            }
        }

        public bool UseMoveOrdering
        {
            get => ChessieBot.UseMoveOrdering;
            set
            {
                ChessieBot.UseMoveOrdering = value;
                Notify(nameof(UseMoveOrdering));
            }
        }

        public bool UseSEE
        {
            get => ChessieBot.UseSEE;
            set
            {
                ChessieBot.UseSEE = value;
                Notify(nameof(UseSEE));
            }
        }

        public bool UseABPruning
        {
            get => ChessieBot.UseABPruning;
            set
            {
                ChessieBot.UseABPruning = value;
                Notify(nameof(UseABPruning));
            }
        }

        private void Notify(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
