using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.Pages
{
    public partial class AdminPagePerson : Page
    {
        private Employees _currentEmployee;
        private Entities _db;
        private Authorisation _currentAuth;

        public AdminPagePerson(User user)
        {
            InitializeComponent();

            _db = new Entities();
            _currentEmployee = _db.Employees.FirstOrDefault(x => x.Surname == user.Surname && x.Name == user.Name && x.Patronymic == user.Patronymic);
            _currentAuth = _db.Authorisation.FirstOrDefault(x => x.ID == _currentEmployee.Authorisation_ID);
            if (_currentEmployee != null)
                {
                // Заполняем поля данными
                NameEntry.Text = _currentEmployee.Name;
                SurnameEntry.Text = _currentEmployee.Surname;
                PatronymicEntry.Text = _currentEmployee.Patronymic;
                PositionEntry.Text = _db.Position.Where(x => x.ID == _currentEmployee.Position_ID).FirstOrDefault().Name;
                DepartmentEntry.Text = _db.Departments.Where(x => x.ID == _currentEmployee.Department_ID).FirstOrDefault().Name;
                HireDateEntry.Text = _currentEmployee.Hire_Date.ToString();

                LoginEntry.Text = _currentAuth.Login;
            }
        }
        

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentEmployee == null)
                {
                    _currentEmployee = new Employees();
                    _db.Employees.Add(_currentEmployee);
                }

                // Обновляем данные
                _currentEmployee.Name = NameEntry.Text;
                _currentEmployee.Surname = SurnameEntry.Text;
                _currentEmployee.Patronymic = PatronymicEntry.Text;
                _currentEmployee.Position_ID = _db.Position.Where(x => x.Name == PositionEntry.Text).FirstOrDefault().ID;
                _currentEmployee.Department_ID = _db.Departments.Where(x => x.Name == DepartmentEntry.Text).FirstOrDefault().ID;

                if (DateTime.TryParse(HireDateEntry.Text, out DateTime hireDate))
                {
                    _currentEmployee.Hire_Date = hireDate;
                }

                _currentAuth.Login = LoginEntry.Text;
                if (!string.IsNullOrEmpty(PasswordEntry.Text))
                    _currentAuth.Password = Hash.HashPassword(PasswordEntry.Text);

                _db.SaveChanges();

                MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            NameEntry.Clear();
            SurnameEntry.Clear();
            PatronymicEntry.Clear();
            PositionEntry.Clear();
            DepartmentEntry.Clear();
            HireDateEntry.Clear();
            LoginEntry.Clear();
            PasswordEntry.Clear();
        }
    }
}