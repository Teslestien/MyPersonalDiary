using System.Windows;

namespace MyPersonalDiary
{
    /// <summary>
    /// Interaction logic for NewPasswordWindow.xaml
    /// </summary>
    public partial class NewPasswordWindow : Window
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public NewPasswordWindow()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            InitializeComponent();
        }

        void ButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string Password
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            get { return ResponseTextBox.Text; }
        }
    }
}
