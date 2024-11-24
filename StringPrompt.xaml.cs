using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Chessie
{
    /// <summary>
    /// Interaction logic for StringPrompt.xaml
    /// </summary>
    public partial class StringPrompt : Window
    {
        public string EnteredText => TextInput.Text;

        public StringPrompt()
        {
            InitializeComponent();
        }

        public static string? ShowPrompt(Window parent, string prompt)
        {
            var dialog = new StringPrompt()
            {
                Owner = parent,
                Title = prompt,
            };
            
            if (dialog.ShowDialog() == true)
            {
                return dialog.EnteredText;
            }
            return null;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
