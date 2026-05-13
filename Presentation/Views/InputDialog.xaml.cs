using System.Windows;

namespace AlJohary.ServiceHub.Presentation.Views
{

    public partial class InputDialog : Window
    {
        public string Answer => AnswerTextBox.Text;

        public InputDialog(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent();
            TitleLabel.Text = title;
            PromptLabel.Text = prompt;
            AnswerTextBox.Text = defaultValue;
            Title = title;
            
            Loaded += (s, e) =>
            {
                AnswerTextBox.Focus();
                AnswerTextBox.SelectAll();
            };

            AnswerTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    DialogResult = true;
                    Close();
                }
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
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
