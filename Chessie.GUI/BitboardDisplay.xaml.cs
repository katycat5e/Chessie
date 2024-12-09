using Chessie.GUI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Chessie.GUI
{
    /// <summary>
    /// Interaction logic for BoardDisplay.xaml
    /// </summary>
    public partial class BitboardDisplay : UserControl
    {
        public string BoardTitle
        {
            get { return (string)GetValue(BoardTitleProperty); }
            set { SetValue(BoardTitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BoardTitle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BoardTitleProperty =
            DependencyProperty.Register("BoardTitle", typeof(string), typeof(BitboardDisplay), new PropertyMetadata(string.Empty));


        public BitboardFilesModel Bits
        {
            get { return (BitboardFilesModel)GetValue(BitsProperty); }
            set { SetValue(BitsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Bits.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BitsProperty =
            DependencyProperty.Register("Bits", typeof(BitboardFilesModel), typeof(BitboardDisplay), new PropertyMetadata(null));



        public BitboardDisplay()
        {
            InitializeComponent();
        }

    }
}
