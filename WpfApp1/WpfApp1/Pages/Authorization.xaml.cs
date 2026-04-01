using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.Pages
{
    /// <summary>
    /// Логика взаимодействия для Authorization.xaml
    /// </summary>
    public partial class Authorization : Page
    {
        private int tries;
        private bool isCaptchaRequired;
        DispatcherTimer timer;
        int timeRemaining;

        int code;
        bool TwoFactorAuthentification = true;
        private Authorisation currentAuthorisation;
        private string currentLogin;
        private string currentPassword;

        public Authorization()
        {
            InitializeComponent();
            InitializeBlockTimer();
            tries = 0;
            isCaptchaRequired = false;

            // Скрываем капчу при запуске
            txtBlockCaptcha.Visibility = Visibility.Collapsed;
            txtBoxCaptcha.Visibility = Visibility.Collapsed;
            lblCaptcha.Visibility = Visibility.Collapsed;
        }

        private void btnEnterGuests_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Client());
        }

        /// <summary>
        /// Делает элементы капчи видимыми, обращается к сервису для генерации капчи
        /// </summary>
        private void GenerateCaptcha()
        {
            // Показываем сообщение о необходимости пройти капчу
            lblCaptcha.Visibility = Visibility.Visible;
            txtBlockCaptcha.Visibility = Visibility.Visible;
            txtBoxCaptcha.Visibility = Visibility.Visible;
            txtBoxCaptcha.Focus();

            string captchaText = CaptchaGenerator.GenerateCaptchaText(6);
            txtBlockCaptcha.Text = captchaText;
            txtBlockCaptcha.TextDecorations = TextDecorations.Strikethrough;

            isCaptchaRequired = true;
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = pswbPassword.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль!");
                return;
            }
            
            if (tries == 0 && !isCaptchaRequired)
            {
                CheckCredentialsAndRequestCaptcha(login, password);
            }
            else if ((tries % 3 != 0 || tries == 0) && isCaptchaRequired) // три попытки ввода капчи
            {
                CheckCredentialsWithCaptcha(login, password);
            }
            else // блокировка экрана
            {
                timeRemaining = 10;
                timer.Start();
                LockUI();
                tries = 0;
                CheckCredentialsWithCaptcha(login, password);
            }
        }
        
        private void InitializeBlockTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += BlockTimer;
        }
        private void BlockTimer(object sender, EventArgs e)
        {
            timeRemaining--;

            if (timeRemaining <= 0)
            {
                UnlockUI();
                timer.Stop();
            }
            else
            {
                txtBlockTimer.Text = $"До разблокировки осталось: {timeRemaining} сек";
            }
        }
        
        private void LockUI()
        {
            btnEnter.IsEnabled = false;
            btnEnterGuests.IsEnabled = false;
            txtLogin.IsEnabled = false;
            pswbPassword.IsEnabled = false;
            txtBoxCaptcha.IsEnabled = false;
            _2FAbox.IsEnabled = false;

            txtBlockTimer.Visibility = Visibility.Visible;
            txtBlockTimer.Foreground = Brushes.Red;
            txtBlockTimer.Text = $"До разблокировки осталось: {timeRemaining} сек";
        }

        private void UnlockUI()
        {
            btnEnter.IsEnabled = true;
            btnEnterGuests.IsEnabled = true;
            txtLogin.IsEnabled = true;
            pswbPassword.IsEnabled = true;
            txtBoxCaptcha.IsEnabled = true;
            _2FAbox.IsEnabled = true;

            txtBlockTimer.Visibility = Visibility.Collapsed;

            tries = 0;
        }
        /// <summary>
        /// Проверяет правильность ввода логина и пароля и запрашивает пройти капчу
        /// </summary>
        /// <param name="login">SE</param>
        /// <param name="password">SE</param>
        private void CheckCredentialsAndRequestCaptcha(string login, string password)
        {
            using (var db = new Entities())
            {
                // Находим пользователя по логину
                var authorisation = db.Authorisation.Where(x => x.Login == login).FirstOrDefault();

                if (authorisation != null)
                {
                    // Хэшируем введенный пароль и сравниваем с хэшем в базе
                    string hashedPassword = Hash.HashPassword(password);
                    bool isPasswordValid = authorisation.Password == hashedPassword;

                    if (isPasswordValid)
                    {
                        // Логин и пароль верные, просим пройти капчу
                        tries = 1;
                        MessageBox.Show("Пройдите капчу для завершения входа.");
                        GenerateCaptcha();
                    }
                    else
                    {
                        MessageBox.Show("Неверный данные попробуйте снова.");
                        pswbPassword.Password = "";
                    }
                }
                else
                {
                    MessageBox.Show("Пользователь с таким логином не найден! Пожалуйста, проверьте данные и попробуйте снова.");
                    txtLogin.Text = "";
                    pswbPassword.Password = "";
                }
            }
        }
        /// <summary>
        /// Проверяет правильность ввода логина, пароля и капчи. Если включена двухфакторная аутентификация - запрашивает код и письма
        /// </summary>
        /// <param name="login">SE</param>
        /// <param name="password">SE</param>
        private void CheckCredentialsWithCaptcha(string login, string password)
        {
            // Проверяем, что капча введена
            if (string.IsNullOrEmpty(txtBoxCaptcha.Text.Trim()))
            {
                MessageBox.Show("Пройдите капчу!");
                return;
            }

            using (var db = new Entities())
            {
                // Находим пользователя по логину
                var authorisation = db.Authorisation.Where(x => x.Login == login).FirstOrDefault();

                if (authorisation != null)
                {
                    // Хэшируем введенный пароль и сравниваем с хэшем в базе
                    string hashedPassword = Hash.HashPassword(password);
                    bool isPasswordValid = authorisation.Password == hashedPassword;
                    bool isCaptchaValid = txtBoxCaptcha.Text.Trim() == txtBlockCaptcha.Text;
                    if (isPasswordValid && isCaptchaValid && !TwoFactorAuthentification)
                    {
                        // Проверяем, является ли пользователь работником или клиентом
                        var employee = db.Employees.FirstOrDefault(s => s.Authorisation_ID == authorisation.ID);
                        var customer = db.Clients.FirstOrDefault(c => c.Authorisation_ID == authorisation.ID);
                        
                        if (employee != null && TimeHelper.IsWithinWorkingHours())
                        {
                            LoadPage("Employee", authorisation, employee);
                        }
                        else if (employee != null && !TimeHelper.IsWithinWorkingHours())
                        {
                            MessageBox.Show("Дождитесь рабочего времени");
                        }
                        else if (customer != null)
                        {
                            LoadPage("Client", authorisation, customer);
                        }
                        else
                        {
                            NavigationService.Navigate(new Client());
                        }
                    }
                    else if (isPasswordValid && isCaptchaValid && TwoFactorAuthentification)
                    {
                        // Сохраняем данные пользователя перед 2FA
                        currentAuthorisation = authorisation;
                        currentLogin = login;
                        currentPassword = password;
                        _2FactorAuthentification();
                    }
                    else if (!isCaptchaValid)
                    {
                        MessageBox.Show("Попробуйте снова.");
                        txtBoxCaptcha.Text = "";
                        GenerateCaptcha(); // Генерируем новую капчу
                        tries += 1;
                    }
                    else
                    {
                        MessageBox.Show("Неверный логин или пароль попробуйте снова.");
                        ResetAuthorizationForm();
                        tries += 1;
                    }
                }
                else
                {
                    MessageBox.Show("Пользователь с таким логином не найден.");
                    ResetAuthorizationForm();
                    tries += 1;
                }
            }
        }
        // 2FA
        /// <summary>
        /// Отправляет код 2FA на почту, делает элементы 2FA видимыми
        /// </summary>
        private void _2FactorAuthentification()
        {
            MessageBox.Show("Пройдите двухфакторную аутентификацию!\nКод отправлен на вашу почту.");
            SendCode();
            _2FAtextBlock.Visibility = Visibility.Visible;
            _2FAbox.Visibility = Visibility.Visible;
            _2FAbox.Focus();

            // Отключаем другие поля во время 2FA
            LoginTextBlock.Visibility = Visibility.Collapsed;
            PasswordTextBlock.Visibility = Visibility.Collapsed;
            lblCaptcha.Visibility = Visibility.Collapsed;
            txtBlockCaptcha.Visibility = Visibility.Collapsed;
            txtLogin.Visibility = Visibility.Collapsed;
            pswbPassword.Visibility = Visibility.Collapsed;
            txtBoxCaptcha.Visibility = Visibility.Collapsed;
            btnEnter.Visibility = Visibility.Collapsed;
            btnEnterGuests.Visibility = Visibility.Collapsed;
        }
        private void _2FAbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CheckCredentialsWith2FA(currentLogin, currentPassword, _2FAbox.Text.Trim());
            }
        }
        /// <summary>
        /// Проверяет правильность ввода кода из письма
        /// </summary>
        /// <param name="login">SE</param>
        /// <param name="password">SE</param>
        /// <param name="enteredCode">Данные поля ввода кода двухфакторной аутентификации</param>
        public void CheckCredentialsWith2FA(string login, string password, string enteredCode)
        {
            if (string.IsNullOrEmpty(enteredCode))
            {
                MessageBox.Show("Введите код подтверждения!");
                return;
            }

            if (int.TryParse(enteredCode, out int userCode))
            {
                if (userCode == code)
                {
                    // Код верный, выполняем вход
                    using (var db = new Entities())
                    {
                        var authorisation = db.Authorisation.Where(x => x.Login == login).FirstOrDefault();

                        if (authorisation != null)
                        {
                            var employee = db.Employees.FirstOrDefault(s => s.Authorisation_ID == authorisation.ID);
                            var customer = db.Clients.FirstOrDefault(c => c.Authorisation_ID == authorisation.ID);

                            if (employee != null && TimeHelper.IsWithinWorkingHours())
                            {
                                LoadPage("Employee", authorisation, employee);
                            }
                            else if (employee != null && !TimeHelper.IsWithinWorkingHours())
                            {
                                MessageBox.Show("Дождитесь рабочего времени");
                                ResetAuthorizationForm();
                            }
                            else if (customer != null)
                            {
                                LoadPage("Client", authorisation, customer);
                            }
                            else
                            {
                                NavigationService.Navigate(new Client());
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Неверный код подтверждения! Попробуйте снова.");
                    _2FAbox.Text = "";
                    _2FAbox.Focus();
                    tries += 1;

                    // После 3 неудачных попыток блокируем
                    if (tries >= 3)
                    {
                        timeRemaining = 10;
                        timer.Start();
                        LockUI();
                        tries = 0;
                    }
                }
            }
            else
            {
                MessageBox.Show("Введите корректный числовой код!");
                _2FAbox.Text = "";
                _2FAbox.Focus();
            }
        }
        /// <summary>
        /// Отправляет код подтверждения на почту-заглушку
        /// </summary>
        public async void SendCode()
        {
            Random random = new Random();
            code = random.Next(1000, 9999);
            var db = new Entities();
            try
            {
                MailAddress from = new MailAddress("SMTPtest123456789@mail.ru", "Izhoga");
                MailAddress to = new MailAddress("SMTPtest123456789@mail.ru");

                MailMessage m = new MailMessage(from, to);
                m.Subject = "Тест";
                m.Body = $"<tr>\r\n                        <td style=\"padding: 20px 30px;\">\r\n                            <p style=\"color: #555555; font-size: 16px; line-height: 1.5; margin: 0 0 15px 0;\">\r\n                                Здравствуйте!\r\n                            </p>\r\n                            <p style=\"color: #555555; font-size: 16px; line-height: 1.5; margin: 0 0 20px 0;\">\r\n                                Для подтверждения входа, используйте следующий код:\r\n                            </p>\r\n                            \r\n                            <!-- Код подтверждения -->\r\n                            <div style=\"background-color: #f8f9fa; border: 1px solid #e9ecef; border-radius: 5px; padding: 20px; text-align: center; margin: 20px 0;\">\r\n                                <span style=\"font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #007bff;\">{code}</span>\r\n                            </div>\r\n                            \r\n                                                        <p style=\"color: #777777; font-size: 12px; line-height: 1.5; margin: 10px 0 0 0;\">\r\n                                Если вы не запрашивали код подтверждения, просто проигнорируйте это письмо.\r\n                            </p>\r\n                        </td>\r\n                    </tr>\r\n                    \r\n                    <!-- Футер -->\r\n                    <tr>\r\n                        <td style=\"padding: 20px; text-align: center; border-top: 1px solid #e9ecef;\">\r\n                            <p style=\"color: #999999; font-size: 12px; margin: 0 0 5px 0;\">\r\n                                © 2026 Izhoga. Никаких прав нет.\r\n                            </p>\r\n                            <p style=\"color: #999999; font-size: 12px; margin: 0;\">\r\n                                Это автоматическое сообщение, пожалуйста, не отвечайте на него.\r\n                            </p>\r\n                        </td>\r\n                    </tr>";
                m.IsBodyHtml = true;

                // адрес smtp-сервера и порт, с которого будем отправлять письмо
                SmtpClient smtp = new SmtpClient("smtp.mail.ru", 587);
                smtp.Credentials = new NetworkCredential("SMTPtest123456789@mail.ru", "ISRW1CITUePmmuDCF1VL");
                smtp.EnableSsl = true;
                await smtp.SendMailAsync(m);
            }
            catch
            {
                MessageBox.Show("Что-то пошло не так");
            }
        }

        private void ResetAuthorizationForm()
        {
            tries = 0;
            isCaptchaRequired = false;
            txtLogin.Text = "";
            pswbPassword.Password = "";
            txtBoxCaptcha.Text = "";
            txtBlockCaptcha.Visibility = Visibility.Collapsed;
            txtBoxCaptcha.Visibility = Visibility.Collapsed;
            lblCaptcha.Visibility = Visibility.Collapsed;
            _2FAbox.Visibility = Visibility.Collapsed;
            _2FAtextBlock.Visibility = Visibility.Collapsed;
        }

        private void LoadPage(string role, Authorisation authorisation, dynamic userData)
        {
            // Сбрасываем форму авторизации
            ResetAuthorizationForm();

            switch (role)
            {
                case "Employee":
                    NavigationService.Navigate(new AuthEmployee(authorisation, (Employees)userData));
                    break;
                case "Client":
                    NavigationService.Navigate(new AuthoCustomer(authorisation, (Clients)userData));
                    break;
                default:
                    NavigationService.Navigate(new Client());
                    break;
            }
        }

        private void txtBlockCaptcha_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isCaptchaRequired)
            {
                GenerateCaptcha();
            }
        }

        private void ForgotPasswordBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ResetPasswordPage());
        }
    }
}