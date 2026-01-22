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
    /// Логика взаимодействия для AdminPage.xaml
    /// </summary>
    public class User
    {
        public string Initials { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string OtherInfo { get; set; }
    }
    public partial class AdminPage : Page
    {
        public AdminPage()
        {
            var db = new Entities();
            var clients = db.Clients.ToList();
            var employees = db.Employees.ToList();
            List<User> users = new List<User>();
            
            for (int i = 0; i != clients.Count; i++) {
                User user = new User();
                user.Initials = employees[i].Surname + employees[i].Name[0] + '.' + employees[i].Patronymic[0];
                user.FullName = employees[i].Surname + employees[i].Name + employees[i].Patronymic;
                try
                {
                    user.Role = db.Position.Where(x => x.ID == employees[i].Position_ID).FirstOrDefault().Name;
                }
                catch (Exception ex)
                {
                    debugbox.Text = $"{ex.ToString()}";
                }
                user.OtherInfo = $"Дата найма {employees[i].Hire_Date}, зарплата {employees[i].Salary}";
                users.Add(user);
            }

            InitializeComponent();

            MainListView.ItemsSource = users;
        }
    }
}
