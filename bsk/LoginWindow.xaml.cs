using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace bsk
{
    /// <summary>
    /// Logika interakcji dla klasy LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        RsaCipher rsaCipher = new RsaCipher();
        AesCipher aesCipher = new AesCipher();
        AesParams aesParams = new AesParams(CipherMode.CBC);
        
        public LoginWindow()
        {
            InitializeComponent();
            if (!Directory.Exists("tmp/priv"))
            {
                Directory.CreateDirectory("tmp/priv");
            }
            if (!Directory.Exists("tmp/pub"))
            {
                Directory.CreateDirectory("tmp/pub");
            }
        }

        private void btnDecrypt(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Password == "")
            {
                correctPassword.Text = "Password cannot be blank!";
            }
            else
            {
                try
                {
                    byte[] key = aesCipher.DecryptPrivateKey(rsaCipher.ImportPrivateKeyFromFile("tmp/priv/" + LoginBox.Text + ".enc"),
                        aesParams, PasswordBox.Password);
                    rsaCipher.ImportPrivateKey(key);
                    rsaCipher.ImportPublicKeyFromFile("tmp/pub/" + LoginBox.Text + ".pub");
                    if (LoginBox.Text == "a")
                    {
                        rsaCipher.ImportOtherPublicKeyFromFile("tmp/pub/b.pub");
                    }
                    else
                    {
                        rsaCipher.ImportOtherPublicKeyFromFile("tmp/pub/a.pub"); 
                    }

                    string login = LoginBox.Text;
                    aesParams = new AesParams(CipherMode.CBC);
                    MainWindow mainWindow = new MainWindow(rsaCipher, aesCipher, aesParams, login);
                    mainWindow.Show();
                    this.Close();
                }
                catch (Exception)
                {
                    correctPassword.Text = "Bad login or password!";
                    correctPassword.Foreground = Brushes.Red;
                }
            }
            correctPassword.Visibility = Visibility.Visible;
        }
        
        private void btnCreateNew(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Password == "" || PasswordBox.Password == null)
            {
                correctPassword.Text = "Password cannot be blank!";
            }
            else
            {
                rsaCipher.ExportPublicKeyToFile("tmp/pub/" + LoginBox.Text + ".pub");
                byte[] key = aesCipher.EncryptPrivateKey(rsaCipher.ExportPrivateKey(), aesParams, PasswordBox.Password);
                rsaCipher.ExportPrivateKeyToFile(key, "tmp/priv/" + LoginBox.Text + ".enc");
                correctPassword.Text = "RSA key pair was created. You can decrypt your private key with password now.";
                correctPassword.Foreground = Brushes.Green;
            }
            correctPassword.Visibility = Visibility.Visible;

        }
    }
}
