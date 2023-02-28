using System.Windows;
using Microsoft.Win32;
using System.Windows.Media;
using System.Security.Cryptography;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System;

namespace bsk
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
 public partial class MainWindow : Window
    {
        private string _fName = "";
        public MainWindow()
        {
            InitializeComponent();
        }

        private void file_button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                _fName = openFileDialog.FileName;
                if (_fName != "")
                {
                    file_name_box.Text = _fName;
                    error_name_box.Foreground = Brushes.Yellow;
                    error_name_box.Text = "wybrano plik";
                }
            }
        }

        private void start_button_Click(object sender, RoutedEventArgs e)
        {
            if (_fName != "")
            {

                error_name_box.Foreground = Brushes.LightGreen;
                error_name_box.Visibility = Visibility.Collapsed;
                error_name_box.Text = "sukces";

                int err = -1;
                int decryptError = 0;

                if(error_byte.Text != "")
                {
                    err = Int32.Parse(error_byte.Text);
                }

                Cryptography cryptography = new Cryptography();
                
                if (ecb.IsChecked == true)
                {
                    decryptError = cryptography.Cipher((bool)decipher.IsChecked, _fName, CipherMode.ECB, password.Password, err);
                }
                else if (cbc.IsChecked == true)
                {
                    decryptError = cryptography.Cipher((bool)decipher.IsChecked, _fName, CipherMode.CBC, password.Password, err);
                }
                else if (cfb.IsChecked == true)
                {
                    decryptError = cryptography.Cipher((bool)decipher.IsChecked, _fName, CipherMode.CFB, password.Password, err);
                }
                else
                {
                    MessageBox.Show("Wystąpił błąd przy wybieraniu trybu.", " ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

                switch (decryptError)
                {
                    case -1:
                        MessageBox.Show("Wystąpił błąd przy odczycie parametrów deszyfrowania.\nPodano niepoprawny tryb lub hasło.", 
                            " ",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    case -2:
                        MessageBox.Show("Miała miejsce nieautoryzowana zmiana w pliku.", 
                            " ",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    case -3:
                        MessageBox.Show("Suma kontrolna się nie zgadza.", " ",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    
                }

                error_name_box.Visibility = Visibility.Visible;
                

            }
        }
        private void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}