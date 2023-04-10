using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace bsk
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _fName = "";
        private NetworkStream stream;
        public ObservableCollection<DataModel> Messages { get; set; } = new ObservableCollection<DataModel>();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void connectButton(object sender, RoutedEventArgs e)
        {
            try
            {
                TcpClient client = new TcpClient();
                client.Connect(IPAddress.Loopback, 1234);

                ifConnected.Text = "Połączono z serwerem na porcie " + client.Client.RemoteEndPoint;
                send.IsEnabled = true;

                Thread receiveThread = new Thread(() => receiveMessages(client));
                receiveThread.Start();

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
                while (client.Connected)
                {
                    // Odbierz dane pliku
                    byte[] buffer = new byte[Constants.ONE_MB]; // 1 MB
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 1) // taka mała paczka oznacza wiadomość o dostępności
                    {
                        setVisibility(buffer);
                    }
                    else
                    {
                        //numer paczki
                        byte[] packetNumberInBytes = new byte[Constants.PACKET_NUMBER_BYTES_NUMBER];
                        Array.Copy(buffer, 0, packetNumberInBytes, 0, packetNumberInBytes.Length);
                        int packetNumber = BitConverter.ToInt32(packetNumberInBytes, 0);
                        //extension
                        byte[] extensionInBytes = new byte[Constants.EXTENSION_BYTES_NUMBER];
                        Array.Copy(buffer, 4, extensionInBytes, 0, extensionInBytes.Length);
                        long extension = BitConverter.ToInt64(extensionInBytes, 0);
                        //wielkość pliku
                        byte[] fileSizeInBytes = new byte[Constants.FILE_SIZE_BYTES_NUMBER];
                        Array.Copy(buffer, 12, fileSizeInBytes, 0, fileSizeInBytes.Length);
                        int fileSize = BitConverter.ToInt32(fileSizeInBytes, 0);
                        //plik
                        int numberOfPackets = (int)Math.Ceiling((double)fileSizeInBytes.Length / (Constants.ONE_MB - Constants.PACKET_NUMBER_BYTES_NUMBER));
                        byte[] file = new byte[fileSize];
                        bytesRead -= Constants.HEADER_BYTES_NUMBER;
                        Array.Copy(buffer, Constants.HEADER_BYTES_NUMBER, file, packetNumber * bytesRead, bytesRead);

                        numberOfPackets--;

                        while (numberOfPackets > 0)
                        {
                            byte[] buffer1 = new byte[Constants.ONE_MB]; // 1 MB
                            int bytesRead1 = stream.Read(buffer1, 0, buffer1.Length);
                            if (bytesRead == 1)
                            {
                                setVisibility(buffer);
                            }
                            else
                            {
                                //numer paczki
                                byte[] packetNumber1InBytes = new byte[Constants.PACKET_NUMBER_BYTES_NUMBER];
                                Array.Copy(buffer1, 0, packetNumber1InBytes, 0, packetNumber1InBytes.Length);
                                int packetNumber1 = BitConverter.ToInt32(packetNumber1InBytes, 0);

                                //file
                                Array.Copy(buffer1, Constants.PACKET_NUMBER_BYTES_NUMBER, file, packetNumber1 * bytesRead1, bytesRead1);

                                numberOfPackets--;
                            }
                        }

                        this.Dispatcher.Invoke(() => Messages.Add(new DataModel(file, extension)));
                    }
                }
            }
            catch (Exception e)
            {
                this.Dispatcher.Invoke(() =>
                    Messages.Add(new DataModel("Błąd podczas otrzymywania wiadomości od serwera: " + e.Message)));
            }
        }

        private void setVisibility(byte[] buffer)
        {
            if (buffer[0] == 1)
            {
                this.Dispatcher.Invoke(() => unavailabilityIcon.Visibility = Visibility.Collapsed);
                this.Dispatcher.Invoke(() => availabilityIcon.Visibility = Visibility.Visible);
            }
            else
            {
                this.Dispatcher.Invoke(() => availabilityIcon.Visibility = Visibility.Collapsed);
                this.Dispatcher.Invoke(() => unavailabilityIcon.Visibility = Visibility.Visible);
            }
        }

        private void sendButton(object sender, RoutedEventArgs e)
        {
            string message;
            DataPack dataPack = new DataPack();
            List<byte[]> bytes;
            if (textBox.Text == "")
            {
                message = _fName;
                bytes = dataPack.preparePacketsToSend(message, true);
                foreach (byte[] pack in bytes)
                {
                    stream.Write(pack, 0, pack.Length);
                }
            }
            else
            {
                message = textBox.Text;
                bytes = dataPack.preparePacketsToSend(message, false);
                foreach (byte[] pack in bytes)
                {
                    stream.Write(pack, 0, pack.Length);
                }

                textBox.Clear();
            }
        }

        private void fileButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                _fName = openFileDialog.FileName;
                if (_fName != "")
                {
                    file_name_box.Text = _fName;
                    fileChosen.Foreground = Brushes.Yellow;
                    fileChosen.Text = "wybrano plik";
                }
            }
        }

        private void selectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataModel selectedItem = (DataModel)ListBox.SelectedItem;

            if (selectedItem == null || selectedItem.extension == Extensions.TEXT)
            {
                downloadButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                downloadButton.Visibility = Visibility.Visible;
            }
        }

        private void downloadFile(object sender, RoutedEventArgs e)
        {
            DataModel selectedItem = (DataModel)ListBox.SelectedItem;
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = ExtensionMethods.setFileFilter(selectedItem.extension);
            if (saveFileDialog.ShowDialog() == true)
            {
                
                File.WriteAllBytes(saveFileDialog.FileName, selectedItem.file);
            }
        }
        /*
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