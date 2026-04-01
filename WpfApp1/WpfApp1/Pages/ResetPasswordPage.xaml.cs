using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
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
    /// Логика взаимодействия для ResetPasswordPage.xaml
    /// </summary>
    
    public partial class ResetPasswordPage : Page
    {
        int code;
        Authorisation auth;
        public ResetPasswordPage()
        {
            InitializeComponent();
        }

        private async void SendCodeBtn_Click(object sender, RoutedEventArgs e)
        {
            Random random = new Random();
            code = random.Next(1000, 9999);
            var db = new Entities();
            try
            {
                string mail = DataBox.Text;
                auth = db.Authorisation.FirstOrDefault(x => x.Login == mail);
                if (auth != null)
                {
                    MailAddress from = new MailAddress("SMTPtest123456789@mail.ru", "Izhoga");
                    MailAddress to = new MailAddress("SMTPtest123456789@mail.ru");

                    MailMessage m = new MailMessage(from, to);
                    m.Subject = "Тест";
                    m.Body = $"<tr>\r\n                        <td style=\"padding: 20px 30px;\">\r\n                            <p style=\"color: #555555; font-size: 16px; line-height: 1.5; margin: 0 0 15px 0;\">\r\n                                Здравствуйте!\r\n                            </p>\r\n                            <p style=\"color: #555555; font-size: 16px; line-height: 1.5; margin: 0 0 20px 0;\">\r\n                                Чтобы сбросить пароль, используйте следующий код:\r\n                            </p>\r\n                            \r\n                            <!-- Код подтверждения -->\r\n                            <div style=\"background-color: #f8f9fa; border: 1px solid #e9ecef; border-radius: 5px; padding: 20px; text-align: center; margin: 20px 0;\">\r\n                                <span style=\"font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #007bff;\">{code}</span>\r\n                            </div>\r\n                            \r\n                                                        <p style=\"color: #777777; font-size: 12px; line-height: 1.5; margin: 10px 0 0 0;\">\r\n                                Если вы не запрашивали код подтверждения, просто проигнорируйте это письмо.\r\n                            </p>\r\n                        </td>\r\n                    </tr>\r\n                    \r\n                    <!-- Футер -->\r\n                    <tr>\r\n                        <td style=\"padding: 20px; text-align: center; border-top: 1px solid #e9ecef;\">\r\n                            <p style=\"color: #999999; font-size: 12px; margin: 0 0 5px 0;\">\r\n                                © 2026 Izhoga. Никаких прав нет.\r\n                            </p>\r\n                            <p style=\"color: #999999; font-size: 12px; margin: 0;\">\r\n                                Это автоматическое сообщение, пожалуйста, не отвечайте на него.\r\n                            </p>\r\n                        </td>\r\n                    </tr>";
                    m.IsBodyHtml = true;

                    // адрес smtp-сервера и порт, с которого будем отправлять письмо
                    SmtpClient smtp = new SmtpClient("smtp.mail.ru", 587);
                    smtp.Credentials = new NetworkCredential("SMTPtest123456789@mail.ru", "ISRW1CITUePmmuDCF1VL");
                    smtp.EnableSsl = true;
                    await smtp.SendMailAsync(m);

                    DataTextBlock.Text = "Введите код подтверждения из письма\nКод подтверждения:";
                    DataBox.Text = "";
                    SendCodeBtn.Visibility = Visibility.Collapsed;
                    CheckCodeBtn.Visibility = Visibility.Visible;
                }
                else
                {
                    MessageBox.Show("Попробуйте другую почту");
                }
            }
            catch
            {
                MessageBox.Show("Что-то пошло не так");
            }
        }
        private void CheckCodeBtn_Click(object sender, EventArgs e) 
        { 
            if (DataBox.Text == code.ToString())
            {
                CodeStack.Visibility = Visibility.Collapsed;
                PasswordReset.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show("Неверный код подтверждения");
            }
        }
        private void SetNewPasswordBtn_Click(object sender, EventArgs e)
        {
            var db = new Entities();
            if (PasswordBox.Password == AgreePasswordBox.Password)
            {
                db.Authorisation.FirstOrDefault(x => x.ID == auth.ID).Password = Hash.HashPassword(PasswordBox.Password);
                db.SaveChanges();
                MessageBox.Show("Пароль изменён!");
                NavigationService.Navigate(new Authorization());
            }
            else
            {
                MessageBox.Show("Пароли должны совпадать");
            }
        }
    }
}
