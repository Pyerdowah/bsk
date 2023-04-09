using System.Windows;
using Microsoft.Win32;
using System.Windows.Media;
using System.Security.Cryptography;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Controls;
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
                
                ifConnected.Text = "Connected to server at " + client.Client.RemoteEndPoint.ToString();
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
                    byte[] buffer = new byte[1024 * 1024]; // 1 MB
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    //numer paczki
                    byte[] packetNumber = new byte[4];
                    Array.Copy(buffer, 0, packetNumber, 0, packetNumber.Length);
                    int packetNumberInt = BitConverter.ToInt32(packetNumber, 0);
                    //extension
                    byte[] extension = new byte[8];
                    Array.Copy(buffer, 4, extension, 0, extension.Length);
                    long extension64 = BitConverter.ToInt64(extension, 0);
                    //wielkość pliku
                    byte[] fileSize = new byte[4];
                    Array.Copy(buffer, 12, fileSize, 0, fileSize.Length);
                    int fileSizeInt = BitConverter.ToInt32(fileSize, 0);
                    //plik
                    int numberOfPackets = (int) Math.Ceiling((double) fileSize.Length / (1024 * 1020));
                    byte[] file = new byte[fileSizeInt];
                    bytesRead -= 16;
                    Array.Copy(buffer, 16, file, packetNumberInt * bytesRead, bytesRead);
                    
                    numberOfPackets--;
                    
                    while (numberOfPackets > 0)
                    {
                        byte[] buffer1 = new byte[1024 * 1024]; // 1 MB
                        int bytesRead1 = stream.Read(buffer1, 0, buffer1.Length); 
                        
                        //numer paczki
                        byte[] packetNumber1 = new byte[4];
                        Array.Copy(buffer1, 0, packetNumber1, 0, packetNumber1.Length);
                        int packetNumberInt1 = BitConverter.ToInt32(packetNumber1, 0);
                        
                        //file
                        Array.Copy(buffer1, 4, file, packetNumberInt1 * bytesRead1, bytesRead1);

                        numberOfPackets--;
                    }

                    this.Dispatcher.Invoke(() => Messages.Add(new DataModel(file, extension64)));

                }
            }
            catch (Exception e)
            {
                this.Dispatcher.Invoke(() => Messages.Add(new DataModel("Error receiving messages from server: " + e.Message)));
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
            if(saveFileDialog.ShowDialog() == true)
                File.WriteAllBytes(saveFileDialog.FileName, selectedItem.file);
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