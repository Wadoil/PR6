using System;
using System.Collections.Generic;
using System.Linq;
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

        private void GenerateCaptcha()
        {
            // Показываем сообщение о необходимости пройти капчу
            lblCaptcha.Visibility = Visibility.Visible;
            txtBlockCaptcha.Visibility = Visibility.Visible;
            txtBoxCaptcha.Visibility = Visibility.Visible;

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

            txtBlockTimer.Visibility = Visibility.Collapsed;

            tries = 0;
        }

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

                    if (isPasswordValid && isCaptchaValid)
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
    }
}