using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        private BackgroundWorker backgroundWorker = new BackgroundWorker();
        private Guid guid { get; set; }
        Dictionary<Guid, DataModel> DataModels = new Dictionary<Guid, DataModel>();
        private DataPack dataPack;
        private CipherMode ciphermode = CipherMode.CBC;
        private RsaCipher rsaCipher;
        private AesCipher aesCipher;
        private AesParams aesParams;
        private AesParams sessionKey;
        private byte[] sessionKeyBytesEnc;
        private string login;
        private byte[] processedBuffer;

        public MainWindow(RsaCipher rsaCipher, AesCipher aesCipher, AesParams aesParams, string login)
        {
            InitializeComponent();
            this.login = login;
            this.rsaCipher = rsaCipher;
            this.aesCipher = aesCipher;
            this.aesParams = aesParams;
            sessionKeyBytesEnc = rsaCipher.Encrypt(aesParams.StoreKey());
            connect();
            DataContext = this;
            progressBarWorker();
            if (!Directory.Exists("tmp"))
            {
                Directory.CreateDirectory("tmp");
            }
            Title = this.login;
        }
        
        private void connect()
        {
            try
            {
                TcpClient client = new TcpClient();
                client.Connect(IPAddress.Loopback, 1234);

                ifConnected.Text = "Connected with server on port: " + client.Client.RemoteEndPoint;
                send.IsEnabled = true;
                
                stream = client.GetStream();
                
                Thread receiveThread = new Thread(() => receiveMessages(client));
                receiveThread.Start();
                
            }
            catch (Exception ex)
            {
                ifConnected.Text = "Error: " + ex.Message;
            }
        }

        private void progressBarWorker()
        {
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.ProgressChanged += progressChanged;
            backgroundWorker.DoWork += doWork;
            backgroundWorker.RunWorkerCompleted += workerCompleted;
        }

        private void receiveMessages(TcpClient client)
        {
            try
            {
                while (client.Connected)
                {
                    // Odbierz dane pliku
                    byte[] buffer = new byte[Constants.ONE_KB];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    
                    
                    if (bytesRead == 1) // taka mała paczka oznacza wiadomość o dostępności
                    {
                        setAvailability(buffer);
                    }
                    else if (bytesRead == 16) // potwierdzenie otrzymania wiadomości
                    {
                        byte[] guidInBytes = new byte[Constants.GUID_BYTES_NUMBER];
                        Array.Copy(buffer, 0, guidInBytes, 0, guidInBytes.Length);
                        guid = new Guid(guidInBytes);
                    }
                    else
                    {
                        processedBuffer = buffer;
                        processBigPacket(processedBuffer, bytesRead);
                    }
                }
            }
            catch (Exception e)
            {
                this.Dispatcher.Invoke(() =>
                    Messages.Add(new DataModel("Error while processing messages from server: " + e.Message)));
            }
        }

        private void processBigPacket(byte[] buffer, int bytesRead)
        {
            buffer = aesCipher.DecryptByte(buffer, sessionKey);
            //guid
            byte[] guidInBytes = new byte[Constants.GUID_BYTES_NUMBER];
            Array.Copy(buffer, 0, guidInBytes, 0, guidInBytes.Length);

            //guid całej wiadomosci
            byte[] messageGuidIdInBytes = new byte[Constants.GUID_BYTES_NUMBER];
            Array.Copy(buffer, 16, messageGuidIdInBytes, 0, messageGuidIdInBytes.Length);
            Guid messageGuidId = new Guid(messageGuidIdInBytes);
            
            //extension
            byte[] extensionInBytes = new byte[Constants.EXTENSION_BYTES_NUMBER];
            Array.Copy(buffer, 32, extensionInBytes, 0, extensionInBytes.Length);
            long extension = BitConverter.ToInt64(extensionInBytes, 0);
            
            //wielkość pliku
            byte[] fileSizeInBytes = new byte[Constants.FILE_SIZE_BYTES_NUMBER];
            Array.Copy(buffer, 40, fileSizeInBytes, 0, fileSizeInBytes.Length);
            long fileSize = BitConverter.ToInt64(fileSizeInBytes, 0);
            
            //liczba paczek
            byte[] packetsNumberInBytes = new byte[Constants.PACKETS_NUMBER_BYTES_NUMBER];
            Array.Copy(buffer, 48, packetsNumberInBytes, 0, packetsNumberInBytes.Length);
            long packetsNumber = BitConverter.ToInt64(packetsNumberInBytes, 0);
            
            //numer paczki
            byte[] packetNumberInBytes = new byte[Constants.PACKET_NUMBER_BYTES_NUMBER];
            Array.Copy(buffer, 56, packetNumberInBytes, 0, packetNumberInBytes.Length);
            int packetNumber = BitConverter.ToInt32(packetNumberInBytes, 0);
            
            //czesc pliku co przyszla z paczką
            bytesRead -= Constants.HEADER_BYTES_NUMBER;
            byte[] file = new byte[bytesRead];
            Array.Copy(buffer, Constants.HEADER_BYTES_NUMBER, file, 0, bytesRead);

            try
            {
                if (packetNumber == 0)
                {
                    string filePath = "tmp/" + messageGuidId + "." +
                                      ExtensionMethods.getExtensionFromLongValue(extension);
                    DataModels[messageGuidId] = new DataModel(file, extension, filePath);
                }

                if (DataModels[messageGuidId].extension != Extensions.TEXT)
                {
                    using (FileStream fileStream = new FileStream(DataModels[messageGuidId].filePath,
                               FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    {
                        fileStream.Write(file, 0, file.Length);
                    }
                }
                if (packetNumber + 1 == packetsNumber)
                {
                    this.Dispatcher.Invoke(() => Messages.Add(DataModels[messageGuidId]));
                }
                stream.Write(guidInBytes, 0, guidInBytes.Length);
            }
            catch (Exception)
            {
                if (sessionKey.cipherMode == CipherMode.CBC)
                {
                    sessionKey.cipherMode = CipherMode.ECB;
                }
                else
                {
                    sessionKey.cipherMode = CipherMode.CBC;
                }
                processBigPacket(processedBuffer, bytesRead + Constants.HEADER_BYTES_NUMBER);
            }
        }

        private void setAvailability(byte[] buffer)
        {
            if (buffer[0] == 1)
            {
                if (login == "a")
                {
                    this.Dispatcher.Invoke(() => unavailabilityIcon.Visibility = Visibility.Collapsed);
                    this.Dispatcher.Invoke(() => availabilityIcon.Visibility = Visibility.Visible);
                    if (sessionKey == null)
                    {
                        sessionKey = aesParams;
                        stream.Write(sessionKeyBytesEnc, 0, sessionKeyBytesEnc.Length);
                    }
                }
                if (sessionKey == null && login == "b")
                {
                    byte[] sessionKeyBytes = new byte[1024];
                    stream.Read(sessionKeyBytes, 0, sessionKeyBytes.Length);
                    sessionKey = aesParams.LoadKey(rsaCipher.Decrypt(sessionKeyBytes));
                    this.Dispatcher.Invoke(() => unavailabilityIcon.Visibility = Visibility.Collapsed);
                    this.Dispatcher.Invoke(() => availabilityIcon.Visibility = Visibility.Visible);
                }
            }
            else
            {
                sessionKey = null;
                this.Dispatcher.Invoke(() => availabilityIcon.Visibility = Visibility.Collapsed);
                this.Dispatcher.Invoke(() => unavailabilityIcon.Visibility = Visibility.Visible);
            }
        }

        private void sendButton(object sender, RoutedEventArgs e)
        {
            string message;
            dataPack = new DataPack();
            if (textBox.Text == "")
            {
                message = _fName;
                dataPack.prepareHeaderToSend(message, true);
            }
            else
            {
                message = textBox.Text;
                dataPack.prepareHeaderToSend(message, false);
                textBox.Clear();
            }
            ProgressBar.Maximum = dataPack.packetsNumber;
            ProgressBar.Visibility = Visibility.Visible;
            send.IsEnabled = false;
            backgroundWorker.RunWorkerAsync();

        }

        private void doWork(object sender, DoWorkEventArgs e)
        {
            Guid messageId;
            int numberOfPacket = 0;
            while (dataPack.packetsNumber > 0)
            {
                messageId = Guid.NewGuid();
                messageId.ToByteArray();
                BitConverter.GetBytes(numberOfPacket);
                IEnumerable<byte> rv = messageId.ToByteArray().Concat(dataPack.header).Concat(BitConverter.GetBytes(numberOfPacket));
                var wholeHeader = rv.ToArray();
                byte[] filePacket;
                byte[] wholePacket;
                if (dataPack.fileToSend)
                {
                    if (dataPack.packetsNumber == 1)
                    {
                        filePacket = new byte[dataPack.messSize % (Constants.ONE_KB - Constants.HEADER_BYTES_NUMBER)];
                    }
                    else
                    {
                        filePacket = new byte[Constants.ONE_KB - Constants.HEADER_BYTES_NUMBER];
                    }
                    using (var fileStream = new FileStream(dataPack.filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        fileStream.Seek(numberOfPacket * (Constants.ONE_KB - Constants.HEADER_BYTES_NUMBER), SeekOrigin.Begin);

                        fileStream.Read(filePacket, 0, filePacket.Length);

                    }
                    wholePacket = wholeHeader.Concat(filePacket).ToArray();
                }
                else
                {
                    byte[] textData = Encoding.GetEncoding(28592).GetBytes(dataPack.filePath);
                    wholePacket = wholeHeader.Concat(textData).ToArray();
                }

                sessionKey.cipherMode = ciphermode;
                byte[] ciphertext = aesCipher.EncryptByte(wholePacket, sessionKey);
                stream.Write(ciphertext, 0, ciphertext.Length);
                while (true)
                {
                    if (messageId == guid)
                    {
                        break;
                    }
                }
                this.Dispatcher.Invoke(() => ProgressBar.Value += 1);
                dataPack.packetsNumber--;
                numberOfPacket++;
                backgroundWorker.ReportProgress(numberOfPacket);
            }

        }

        private void progressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
        }
        
        private void workerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ProgressBar.Visibility = Visibility.Collapsed;
            send.IsEnabled = true;
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
                    fileChosen.Text = "file is chosen";
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
                File.Copy(selectedItem.filePath, saveFileDialog.FileName);
            }
        }

        public void RadioButtonECB_Checked(object sender, RoutedEventArgs e)
        {
            ciphermode = CipherMode.ECB;
        }

        public void RadioButtonCBC_Checked(object sender, RoutedEventArgs e)
        {
            ciphermode = CipherMode.CBC;
        }
    }
}