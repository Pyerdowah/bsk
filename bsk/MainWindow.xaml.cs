using System.Windows;
using Microsoft.Win32;
using System.Windows.Media;
using System.Security.Cryptography;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace bsk
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
 public partial class MainWindow : Window
    {
        private string _fName = "";
        private NetworkStream stream;
        public ObservableCollection<string> messages { get; set; } = new ObservableCollection<string>();
        
        public MainWindow()
        {
            InitializeComponent();
        }
        
        private void connectButton(object sender, RoutedEventArgs e)
        {
            try
            {
                // connect to the server on port 1234
                TcpClient client = new TcpClient();
                client.Connect(IPAddress.Loopback, 1234);

                //Console.WriteLine("Connected to server at " + client.Client.RemoteEndPoint.ToString());
                ifConnected.Text = "Connected to server at " + client.Client.RemoteEndPoint.ToString();
                send.IsEnabled = true;

                // start a new thread to receive messages from the server
                Thread receiveThread = new Thread(() => receiveMessages(client));
                receiveThread.Start();
                //lvMessages.ItemsSource = messagesReceiver.messages;
                // send messages to the server
                stream = client.GetStream();

            }
            catch (Exception ex)
            {
                ifConnected.Text = "Error: " + ex.Message;
            }
        }

        private void receiveMessages(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                while (client.Connected)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    Dispatcher.Invoke(() => messages.Add("Received message: " + message));

                }
            }
            catch (Exception e)
            {
                messages.Add("Error receiving messages from server: " + e.Message);
            }
        }

        private void sendButton(object sender, RoutedEventArgs e)
        {
                string message = textBox.Text;

                byte[] messageBytes = Encoding.ASCII.GetBytes(message);
                stream.Write(messageBytes, 0, messageBytes.Length);
        }

        /*    private void file_button_Click(object sender, RoutedEventArgs e)
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
            }*/
    }
}