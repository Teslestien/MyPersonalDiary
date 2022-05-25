using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Diary
{
    public partial class MainWindow : Window
    {


        int date;
        int year;
        int month;

        readonly DispatcherTimer dispatcherTimer = new DispatcherTimer();

        string FontSize_str;
        string FontFamily_str;
        string BackgroundLocation;
        string LockScreenBackgroundLocation;
        string Password;
        string StorageLocation = Path.Combine(Environment.ExpandEnvironmentVariables("%userprofile%"), "Documents");
        string SecurityQuestion_str;
        string SecurityQuestionAnswer;
        bool Encryption = true;
        bool HideDataFolder;
        string SecurityQuestionAnswerPassword;

        private List<Control> AllChildren(DependencyObject parent)
        {
            var list = new List<Control> { };
            for (int count = 0; count < VisualTreeHelper.GetChildrenCount(parent); count++)
            {
                var child = VisualTreeHelper.GetChild(parent, count);
                if (child is Control)
                {
                    list.Add(child as Control);
                }
                list.AddRange(AllChildren(child));
            }
            return list;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public MainWindow()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            InitializeComponent();

            MainComponents.Visibility = Visibility.Collapsed;
            PasswordContainer.Visibility = Visibility.Visible;

            date = 0;

            using (var wb = new HttpClient())
            {
                var response = wb.GetStringAsync("http://api.weatherapi.com/v1/current.json?key=3e5f3bde7f6f4ebd91f92409221101&q=auto:ip");
                string data = response.Result.Split('"')[38].Replace(",", "").Replace(":", "");
                Temperature.Text = data + " °C/" + (float.Parse(data) * 9 / 5 + 32).ToString() + " °F";
            }

            dispatcherTimer.Tick += new EventHandler(Timer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
            Timer_Tick(null, null);

            date = DateTime.Now.Day;
            year = DateTime.Now.Year;
            month = DateTime.Now.Month;
            Month.Text = GetAbbreviatedName(month);
            Year.Text = year.ToString();

            LoadingBar.Value = 0;
            LoadingBar.Visibility = Visibility.Collapsed;
            try
            {
                string[] SettingsData = File.ReadAllLines("config.txt");
                FontSize_str = SettingsData[0];
                FontFamily_str = SettingsData[1];
                BackgroundLocation = SettingsData[2];
                LockScreenBackgroundLocation = SettingsData[3];
                Password = SettingsData[4];
                if (Directory.Exists(SettingsData[5]))
                {
                    StorageLocation = SettingsData[5];
                }
                SecurityQuestion_str = SettingsData[6];
                SecurityQuestionAnswer = SettingsData[7];
                HideDataFolder = bool.Parse(SettingsData[9]);
                Encryption = bool.Parse(SettingsData[10]);
                SecurityQuestionAnswerPassword = SettingsData[11];
            }
            catch
            {
                ;
            }

            FontSize_input.Text = FontSize_str;
            FontFamily_ComboBox.Text = FontFamily_str;
            BackgroundLocationTextBlock.Text = BackgroundLocation;
            LockScreenBackgroundLocationTextBlock.Text = LockScreenBackgroundLocation;
            StorageLocationTextBlock.Text = StorageLocation;
            SecurityQuestion_input.Text = SecurityQuestion_str;
            HideFolderToggle.IsChecked = HideDataFolder;
            EncryptionToggle.IsChecked = Encryption;


            try
            {
                BackgroundImage.Source = new BitmapImage(new Uri(LockScreenBackgroundLocation));
            }
            catch
            {
                BackgroundImage.Source = new BitmapImage(new Uri("pack://application:,,,/resources/" + "Streets.jpg"));
            }

            if (StorageLocation == "")
            {
                StorageLocation = Path.Combine(Environment.ExpandEnvironmentVariables("%userprofile%"), "Documents");
            }

            SaveSuccess.Visibility = Visibility.Collapsed;

            this.SizeChanged += OnWindowSizeChanged;

            FontFamily ff = new FontFamily(new Uri("pack://application:,,,/resources/"), "./#" + FontFamily_str);
            Journal.FontFamily = ff;
            try
            {
                Journal.FontSize = int.Parse(FontSize_str);
            }
            catch
            {
                FontSize_str = (28).ToString();
                FontSize_input.Text = FontSize_str;
                Journal.FontSize = int.Parse(FontSize_str);
            }

            BackgroundSelectButton.Content = "Browse...";
            StorageButton.Content = "Browse...";
            LockScreenBackgroundSelectButton.Content = "Browse...";
        }
        //Called on window size change
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        protected void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            float h = (float)e.NewSize.Height;
            float w = (float)e.NewSize.Width;

            Journal.Width = w / 1.6;
            Journal.MinHeight = w / 1.6;
            EnterPassword.Margin = new Thickness(30, h / 2.5, 0, 0);
        }
        static string GetAbbreviatedName(int month)
        {
            DateTime date = new DateTime(DateTime.Now.Year, month, DateTime.Now.Day);
            return date.ToString("MMM");
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            Time.Text = DateTime.Now.ToLongTimeString();

            Button Today = (Button)DefaultWindow.FindName("X" + DateTime.Now.Day.ToString());
            Today.Background = Brushes.Red;
        }

        private void VerifyPassword(object sender, RoutedEventArgs args)
        {
            string PasswordInputText = PasswordInput.Password;

            if (PasswordInputText == Decrypt(Password, PasswordInputText))
            {
                PasswordContainer.Visibility = Visibility.Collapsed;
                try
                {
                    BackgroundImage.Source = new BitmapImage(new Uri(BackgroundLocation));
                }
                catch
                {
                    BackgroundImage.Source = new BitmapImage(new Uri("pack://application:,,,/resources/" + "Sunset.jpg"));
                }
                BackgroundImage.Opacity = 1;
                MainComponents.Visibility = Visibility.Visible;
                ReadJournal();
            }
            else
            {
                Wrong_Password.Visibility = Visibility.Visible;
                PasswordInput.Clear();
            }
        }

        private void Password_Enter(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                VerifyPassword(null, null);
            }
        }
        void SelectDate(object sender, RoutedEventArgs e)
        {
            string dir = (StorageLocation + "\\Data\\" + year.ToString() + "\\" + GetAbbreviatedName(month) + "\\");
            string path = dir + (date.ToString() + ".txt");
            if (File.Exists(path))
            {
                if (Journal.Text != "" && Journal.Text != File.ReadAllText(path))
                {
                    SaveVoid(null, null);
                }
            }

            var button = sender as Button;
            date = int.Parse(button.Tag.ToString());
            Button SelectedButton = (Button)DefaultWindow.FindName("X" + date.ToString());
            List<Control> children = AllChildren(Dates);
            foreach (var child in children)
            {
                child.Style = (Style)DefaultWindow.FindResource("NormalDate");
            }
            SelectedButton.Style = (Style)DefaultWindow.FindResource("Today");

            ReadJournal();
        }
        static string Encrypt(string clearText, string EncryptionKey)
        {
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }

        static string Decrypt(string cipherText, string EncryptionKey)
        {
            byte[] cipherBytes;
            try
            {
                cipherBytes = Convert.FromBase64String(cipherText);
                using (Aes encryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.Close();
                        }
                        cipherText = Encoding.Unicode.GetString(ms.ToArray());
                    }
                }
            }
            catch
            {
                cipherText = null;
            }
            return cipherText;
        }

        private void BackgroundImage_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //FocusManager.SetFocusedElement(FocusManager.GetFocusScope(Journal), null);
            Journal.Focusable = false;
            Journal.Focusable = true;
        }

        private void SelectBackground(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = false,
                Filter = "JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif"
            };

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                string filename = dialog.FileName;
                if ((sender as Button).Tag.ToString() == "LSBG")
                {
                    LockScreenBackgroundLocationTextBlock.Text = filename;
                }
                else
                {
                    BackgroundLocationTextBlock.Text = filename;
                }
            }
        }
        private void SelectStorage(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                Title = "My Title",
                IsFolderPicker = true,
                InitialDirectory = "C:\\",

                AddToMostRecentlyUsedList = false,
                AllowNonFileSystemItems = false,
                DefaultDirectory = "C:\\",
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var folder = dlg.FileName;
                StorageLocationTextBlock.Text = folder;
            }
        }

        String[] contents = new string[12];
        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {

            FontSize_str = FontSize_input.Text;
            contents[0] = FontSize_str;

            FontFamily_str = FontFamily_ComboBox.Text;
            contents[1] = FontFamily_str;

            BackgroundLocation = BackgroundLocationTextBlock.Text;
            contents[2] = BackgroundLocation;

            LockScreenBackgroundLocation = LockScreenBackgroundLocationTextBlock.Text;
            contents[3] = LockScreenBackgroundLocation;

            //if (Password != "")
            //{
            //    if (Decrypt(Password, PasswordInput.Password) != NewPassword_input.Text)
            //    {
            //        decAll(PasswordInput.Password);
            //        encAll(NewPassword_input.Text);
            //        Password = Encrypt(NewPassword_input.Text, NewPassword_input.Text);
            //        contents[4] = Password;
            //    }
            //}
            contents[4] = Password;

            StorageLocation = StorageLocationTextBlock.Text;
            contents[5] = StorageLocation;

            SecurityQuestion_str = SecurityQuestion_input.Text;
            contents[6] = SecurityQuestion_str;


            contents[8] = "Useless Line";
            contents[9] = HideDataFolder.ToString();
            contents[10] = Encryption.ToString();
            contents[11] = SecurityQuestionAnswerPassword;

            File.WriteAllLines("config.txt", contents);

            try
            {
                BackgroundImage.Source = new BitmapImage(new Uri(BackgroundLocation));
            }
            catch
            {
                BackgroundImage.Source = new BitmapImage(new Uri("pack://application:,,,/resources/" + "Sunset.jpg"));
            }
            FontFamily ff = new FontFamily(new Uri("pack://application:,,,/resources/"), "./#" + FontFamily_str);
            Journal.FontFamily = ff;
            try
            {
                Journal.FontSize = int.Parse(FontSize_str);
            }
            catch
            {
                FontSize_str = (28).ToString();
                FontSize_input.Text = FontSize_str;
                Journal.FontSize = int.Parse(FontSize_str);
            }
        }

        void ChangeMonth(object sender, RoutedEventArgs e)
        {
            var obj = sender as Button;
            if ((string)obj.Tag == "+" && month < 12)
            {
                month++;
            }
            else if ((string)obj.Tag == "-" && month > 1)
            {
                month--;
            }
            Month.Text = new DateTime(2010, month, 1).ToString("MMM");
            Year.Text = year.ToString();
        }
        void ChangeYear(object sender, RoutedEventArgs e)
        {
            var obj = sender as Button;
            if ((string)obj.Tag == "+")
            {
                year++;
            }
            else if ((string)obj.Tag == "-" && year > 0)
            {
                year--;
            }
            Month.Text = new DateTime(2010, month, 1).ToString("MMM");
            Year.Text = year.ToString();
        }
        static byte[] EncryptBytes(byte[] clearBytes, string EncryptionKey)
        {
            byte[] cipherBytes;
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    cipherBytes = ms.ToArray();
                }
            }
            return cipherBytes;
        }
        static byte[] DecryptBytes(byte[] cipherBytes, string EncryptionKey)
        {
            byte[] plainBytes;
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        try
                        {
                            cs.Close();
                            plainBytes = ms.ToArray();
                        }
                        catch
                        {
                            plainBytes = new byte[0];
                        }
                    }
                }
            }
            return plainBytes;
        }
        void SaveVoid(object sender, RoutedEventArgs e)
        {
            try
            {
                string dir = (StorageLocation + "\\Data\\" + year.ToString() + "\\" + GetAbbreviatedName(month) + "\\");
                string path = dir + (date.ToString() + ".txt");

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                if (!File.Exists(path))
                {
                    File.Create(path).Dispose();
                }
                string JournalText = Journal.Text;
                File.WriteAllText(path, JournalText);
                if (Encryption)
                {
                    byte[] filebytes = File.ReadAllBytes(path);
                    File.WriteAllBytes(path, EncryptBytes(filebytes, PasswordInput.Password));
                }

                SaveSuccess.Text = "Save Success!!";
            }
            catch
            {
                SaveSuccess.Text = "Save Failed";
            }
            SaveSuccess.Visibility = Visibility.Visible;
        }
        void ReadJournal()
        {
            LoadingScreen.Visibility = Visibility.Visible;
            LoadingBar.Value = 0;
            string dir = StorageLocation + "\\Data\\" + year.ToString() + "\\" + GetAbbreviatedName(month) + "\\";
            string path = dir + (date.ToString() + ".txt");

            if (File.Exists(path))
            {
                if (Encryption)
                {
                    byte[] filebytes = DecryptBytes(File.ReadAllBytes(path), PasswordInput.Password);
                    File.WriteAllBytes(path, filebytes);
                    Journal.Text = File.ReadAllText(path);
                    byte[] encFilebytes = EncryptBytes(File.ReadAllBytes(path), PasswordInput.Password);
                    File.WriteAllBytes(path, encFilebytes);
                }
                else
                {
                    Journal.Text = File.ReadAllText(path);
                }
            }
            else
            {
                Journal.Text = "";
            }

            LoadingBar.Value = 100;
            LoadingScreen.Visibility = Visibility.Collapsed;
        }

        void ForgotPassword(object sender, RoutedEventArgs args)
        {
            if (SecurityQuestion_str != "")
            {
                SecurityQuestion.Text = SecurityQuestion_str;
                ForgotPasswordMenu.Visibility = Visibility.Visible;
            }
            else
            {
                PasswordContainer.Visibility = Visibility.Collapsed;
                ForgotPasswordMenu.Visibility = Visibility.Collapsed;
                try
                {
                    BackgroundImage.Source = new BitmapImage(new Uri(BackgroundLocation));
                }
                catch
                {
                    BackgroundImage.Source = new BitmapImage(new Uri("pack://application:,,,/resources/" + "Sunset.jpg"));
                }
                BackgroundImage.Opacity = 1;
                MainComponents.Visibility = Visibility.Visible;

                string messageBoxText = "Software was automatially unlocked because of no security question was found. Create one now.";
                string caption = "Fail Safe";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Information;

                MessageBox.Show(messageBoxText, caption, button, icon);
            }
        }

        void CloseForgotPassword(object sender, RoutedEventArgs args)
        {
            ForgotPasswordMenu.Visibility = Visibility.Collapsed;
        }

        private void VerifySecurityQuestionAnswer(object sender, RoutedEventArgs args)
        {
            string AnswerInputText = Answer.Text;
            try
            {
                if (AnswerInputText == Decrypt(SecurityQuestionAnswer, AnswerInputText))
                {
                    PasswordContainer.Visibility = Visibility.Collapsed;
                    ForgotPasswordMenu.Visibility = Visibility.Collapsed;
                    try
                    {
                        BackgroundImage.Source = new BitmapImage(new Uri(BackgroundLocation));
                    }
                    catch
                    {
                        BackgroundImage.Source = new BitmapImage(new Uri("pack://application:,,,/resources/" + "Sunset.jpg"));
                    }
                    BackgroundImage.Opacity = 1;
                    MainComponents.Visibility = Visibility.Visible;

                    string messageBoxText = "Your passsword is:" + Decrypt(SecurityQuestionAnswerPassword, AnswerInputText);
                    string caption = "Correct Answer";
                    MessageBoxButton button = MessageBoxButton.OK;
                    MessageBoxImage icon = MessageBoxImage.Error;
                    PasswordInput.Password = Decrypt(SecurityQuestionAnswerPassword, AnswerInputText);

                    MessageBox.Show(messageBoxText, caption, button, icon);
                }
                else
                {
                    string messageBoxText = "Wrong Answer";
                    string caption = "Wrong Answer";
                    MessageBoxButton button = MessageBoxButton.OK;
                    MessageBoxImage icon = MessageBoxImage.Error;

                    MessageBox.Show(messageBoxText, caption, button, icon);
                }
            }
            catch
            {
                ;
            }
        }

        private void SecurityQuestion_Enter(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                VerifySecurityQuestionAnswer(null, null);
            }
        }

        private void UnhideFolder(object sender, RoutedEventArgs e)
        {
            string temp = StorageLocation;
            temp += "\\Data";
            string HideInfoCmd = "attrib -h -s -r \"" + temp + "\"";
            if (!Directory.Exists(temp))
            {
                Directory.CreateDirectory(temp);
            }
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                WorkingDirectory = temp,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                Arguments = "/C " + HideInfoCmd
            };
            process.StartInfo = startInfo;
            process.Start();

            HideDataFolder = false;
        }
        private void HideFolder(object sender, RoutedEventArgs e)
        {
            string temp = StorageLocation;
            temp += "\\Data";
            string HideInfoCmd = "attrib +h +s +r \"" + temp + "\"";
            if (!Directory.Exists(temp))
            {
                Directory.CreateDirectory(temp);
            }
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                WorkingDirectory = temp,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                Arguments = "/C " + HideInfoCmd
            };
            process.StartInfo = startInfo;
            process.Start();

            HideDataFolder = true;
        }

        private void HideDataInfo(object sender, RoutedEventArgs e)
        {
            string messageBoxText = "On Enabling this setting the folder won't be accessible by any user. It will be ccessible only through some technical ways. This is recommended to hide your diary data from anyone else using this PC.";
            string caption = "Info";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Information;

            MessageBox.Show(messageBoxText, caption, button, icon);
        }

        private void EncryptionEnable(object sender, RoutedEventArgs e)
        {
            EncAll(Decrypt(Password, PasswordInput.Password));
            Encryption = true;
        }
        void EncAll(string key)
        {
            DirectoryInfo objDirectoryInfo = new DirectoryInfo(StorageLocation + "\\Data\\");
            FileInfo[] allFiles = objDirectoryInfo.GetFiles("*.*", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                try
                {
                    byte[] fileBytes = File.ReadAllBytes(file.FullName);
                    byte[] encBytes = EncryptBytes(fileBytes, key);
                    File.WriteAllBytes(file.FullName, encBytes);
                }
                catch
                {
                    ;
                }
            }
        }
        void DecAll(string key)
        {
            DirectoryInfo objDirectoryInfo = new DirectoryInfo(StorageLocation + "\\Data\\");
            FileInfo[] allFiles = objDirectoryInfo.GetFiles("*.*", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                try
                {
                    byte[] fileBytes = File.ReadAllBytes(file.FullName);
                    byte[] decBytes = DecryptBytes(fileBytes, key);
                    File.WriteAllBytes(file.FullName, decBytes);
                }
                catch
                {
                    continue;
                }
            }
        }
        private void EncryptionDisable(object sender, RoutedEventArgs e)
        {
            DecAll(Decrypt(Password, PasswordInput.Password));
            Encryption = false;
        }

        private void NewPassword(object sender, RoutedEventArgs e)
        {
            MyPersonalDiary.NewPasswordWindow dialog = new MyPersonalDiary.NewPasswordWindow();
            dialog.ShowDialog();


            var x = dialog.Password.ToCharArray();
            for (int i = 0; i < dialog.Password.Length; i++)
            {
                if (x[i] == " ".ToCharArray()[0])
                {
                    string messageBoxText = "Empty values or spaces are not allowed.";
                    string caption = "Error";
                    MessageBoxButton button = MessageBoxButton.OK;
                    MessageBoxImage icon = MessageBoxImage.Error;

                    MessageBox.Show(messageBoxText, caption, button, icon);

                    return;
                }
            }
            if (dialog.Password == "")
            {
                string messageBoxText = "Empty values or spaces are not allowed.";
                string caption = "Error";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Error;

                MessageBox.Show(messageBoxText, caption, button, icon);
                return;
            }

            if (dialog.Password != "" || dialog.Password != " ")
            {
                DecAll(Decrypt(Password, PasswordInput.Password));
                EncAll(dialog.Password);
                Password = Encrypt(dialog.Password, dialog.Password);
                PasswordInput.Password = dialog.Password;
                SaveSettings_Click(null, null);
            }
            else
            {
                //string messageBoxText = "Can't keep value empty.";
                //string caption = "Error";
                //MessageBoxButton button = MessageBoxButton.OK;
                //MessageBoxImage icon = MessageBoxImage.Error;

                //MessageBox.Show(messageBoxText, caption, button, icon);

                dialog.ShowDialog();
            }
        }

        private void ChangeSecurityAnswer(object sender, RoutedEventArgs e)
        {
            MyPersonalDiary.NewPasswordWindow dialog = new MyPersonalDiary.NewPasswordWindow();
            dialog.ShowDialog();

            var x = dialog.Password.ToCharArray();
            for (int i = 0; i < dialog.Password.Length; i++)
            {
                if (x[i] == " ".ToCharArray()[0])
                {
                    string messageBoxText = "Empty values or spaces are not allowed.";
                    string caption = "Error";
                    MessageBoxButton button = MessageBoxButton.OK;
                    MessageBoxImage icon = MessageBoxImage.Error;

                    MessageBox.Show(messageBoxText, caption, button, icon);

                    return;
                }
            }

            if (dialog.Password == "")
            {
                string messageBoxText = "Empty values or spaces are not allowed.";
                string caption = "Error";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Error;

                MessageBox.Show(messageBoxText, caption, button, icon);
                return;
            }

            if (dialog.Password != "" || dialog.Password != " ")
            {
                SecurityQuestionAnswer = Encrypt(dialog.Password, dialog.Password);
                contents[7] = SecurityQuestionAnswer;
                contents[11] = Encrypt(PasswordInput.Password, dialog.Password);
                SaveSettings_Click(null, null);
            }
            else
            {
                dialog.ShowDialog();
            }
        }
    }
}
