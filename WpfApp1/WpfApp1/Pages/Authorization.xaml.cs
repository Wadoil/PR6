using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.Pages
{
    /// <summary>
    /// Логика взаимодействия для Authorization.xaml
    /// </summary>
    public partial class Authorization : Page
    {
        private int click;
        private bool isCaptchaRequired;

        public Authorization()
        {
            InitializeComponent();
            click = 0;
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

            // Проверяем, что поля не пустые
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль!");
                return;
            }

            // Первая попытка входа - проверяем логин/пароль и просим пройти капчу
            if (click == 0)
            {
                CheckCredentialsAndRequestCaptcha(login, password);
            }
            // Вторая попытка - проверяем логин/пароль и капчу
            else if (click == 1 && isCaptchaRequired)
            {
                CheckCredentialsWithCaptcha(login, password);
            }
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
                        click = 1;
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
                        // Успешная авторизация
                        MessageBox.Show("Вы успешно авторизовались!");

                        // Проверяем, является ли пользователь работником или клиентом
                        var employee = db.Employees.FirstOrDefault(s => s.Authorisation_ID == authorisation.ID);
                        var customer = db.Clients.FirstOrDefault(c => c.Authorisation_ID == authorisation.ID);

                        if (employee != null)
                        {
                            LoadPage("Employee", authorisation, employee);
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
                    }
                    else
                    {
                        MessageBox.Show("Неверный логин или пароль попробуйте снова.");
                        ResetAuthorizationForm();
                    }
                }
                else
                {
                    MessageBox.Show("Пользователь с таким логином не найдены.");
                    ResetAuthorizationForm();
                }
            }
        }

        private void ResetAuthorizationForm()
        {
            click = 0;
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

        // Добавьте этот метод в ваш XAML для TextBlock капчи (если нужно обновить капчу)
        private void txtBlockCaptcha_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isCaptchaRequired)
            {
                GenerateCaptcha();
            }
        }
    }
}