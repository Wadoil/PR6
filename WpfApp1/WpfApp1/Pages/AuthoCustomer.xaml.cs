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

namespace WpfApp1.Pages
{
    /// <summary>
    /// Логика взаимодействия для AuthoCustomer.xaml
    /// </summary>
    public partial class AuthoCustomer : Page
    {

        public AuthoCustomer(Authorisation auth, Clients CustomerData)
        {
            InitializeComponent();

            var db = new Entities();
            if (auth != null && CustomerData != null)
            {
                // Формируем полное имя

                // Обновляем текстовые блоки
                WelcomeBlock.Text = $"{TimeHelper.GetTimeOfDayGreeting()}, {CustomerData.Name}!";
                NameBlock.Text = CustomerData.Name;
                MarketBlock.Text = db.Markets.FirstOrDefault(x => x.ID == CustomerData.Market_ID).Name ?? "Не указан";
                ContactsBlock.Text = CustomerData.Contact_Info ?? "Не указаны";
            }
            else
            {
                WelcomeBlock.Text = "Добро пожаловать!";
                NameBlock.Text = "Информация недоступна";
                MarketBlock.Text = "Информация недоступна";
                ContactsBlock.Text = "Информация недоступна";
            }
        }
    }
}
