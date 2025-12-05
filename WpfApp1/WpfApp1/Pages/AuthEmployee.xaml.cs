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
    /// Логика взаимодействия для AuthEmployee.xaml
    /// </summary>
    public partial class AuthEmployee : Page
    {
        public AuthEmployee(Authorisation auth, Employees EmployeeData)
        {
            InitializeComponent();

            var db = new Entities();
            if (auth != null && EmployeeData != null)
            {
                // Формируем полное имя
                string fullName = $"{EmployeeData.Surname} {EmployeeData.Name} {EmployeeData.Patronymic}".Replace("  ", " ").Trim();

                // Обновляем текстовые блоки
                WelcomeBlock.Text = $"{TimeHelper.GetTimeOfDayGreeting()}, {EmployeeData.Name}!";
                FullNameBlock.Text = fullName;
                DepartmentBlock.Text = db.Departments.FirstOrDefault(x => x.ID == EmployeeData.Department_ID).Name ?? "Не указан";
                PositionBlock.Text = db.Position.FirstOrDefault(x => x.ID == EmployeeData.Position_ID).Name ?? "Не указан";
            }
            else
            {
                WelcomeBlock.Text = "Добро пожаловать!";
                FullNameBlock.Text = "Информация недоступна";
                DepartmentBlock.Text = "Информация недоступна";
                PositionBlock.Text = "Информация недоступна";
            }
        }
    }
}
