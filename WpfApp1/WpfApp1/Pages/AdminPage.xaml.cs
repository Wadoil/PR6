using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Initials { get; set; }
        public string OtherInfo { get; set; }
        public string Surname { get; set; }
        public string Name { get; set; }
        public string Patronymic { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }
        public string Login { get; set; }
    }
    public partial class AdminPage : Page
    {
            private Entities _db;
            private List<User> _allEmployees;

            public AdminPage()
            {
                InitializeComponent();
                _db = new Entities();
                LoadEmployees();
            }
            /// <summary>
            /// Выгружает данные сотрудников из базы в таблицу
            /// </summary>
            private void LoadEmployees()
            {
                try
                {
                    var employees = _db.Employees.ToList();
                    _allEmployees = new List<User>();

                    foreach (var emp in employees)
                    {
                        var position = _db.Position.FirstOrDefault(p => p.ID == emp.Position_ID);
                        var department = _db.Departments.FirstOrDefault(d => d.ID == emp.Department_ID);
                        var auth = _db.Authorisation.FirstOrDefault(a => a.ID == emp.Authorisation_ID);

                        string initials = "";
                        if (!string.IsNullOrEmpty(emp.Name) && !string.IsNullOrEmpty(emp.Surname))
                        {
                            initials = $"{emp.Surname} {emp.Name.Substring(0, 1)}.";
                            if (!string.IsNullOrEmpty(emp.Patronymic))
                                initials += $"{emp.Patronymic.Substring(0, 1)}.";
                        }

                        string otherInfo = $"Должность: {position?.Name ?? "Не указана"} | Отдел: {department?.Name ?? "Не указан"}";
                        if (emp.Hire_Date != null)
                            otherInfo += $" | Дата найма: {emp.Hire_Date}";

                        _allEmployees.Add(new User
                        {
                            Id = emp.ID,
                            FullName = $"{emp.Surname} {emp.Name} {emp.Patronymic}".Trim(),
                            Initials = initials,
                            OtherInfo = otherInfo,
                            Surname = emp.Surname,
                            Name = emp.Name,
                            Patronymic = emp.Patronymic,
                            Position = position?.Name,
                            Department = department?.Name,
                            Login = auth?.Login
                        });
                    }

                    MainListView.ItemsSource = _allEmployees;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке сотрудников: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            private void SearchBtn_Click(object sender, RoutedEventArgs e)
            {
                ApplyFilter();
            }

            private void SearcLine_KeyDown(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter)
                {
                    ApplyFilter();
                }
            }

            private void ApplyFilter()
            {
                string searchText = searcLine.Text?.ToLower().Trim();

                if (string.IsNullOrWhiteSpace(searchText) || searchText == "поиск")
                {
                    MainListView.ItemsSource = _allEmployees;
                    return;
                }

                var filteredEmployees = _allEmployees.Where(emp =>
                    emp.FullName.ToLower().Contains(searchText) ||
                    emp.Surname.ToLower().Contains(searchText) ||
                    emp.Name.ToLower().Contains(searchText) ||
                    (emp.Patronymic != null && emp.Patronymic.ToLower().Contains(searchText)) ||
                    (emp.Position != null && emp.Position.ToLower().Contains(searchText)) ||
                    (emp.Department != null && emp.Department.ToLower().Contains(searchText))
                ).ToList();

                MainListView.ItemsSource = filteredEmployees;
            }

            private void CreateBtn_Click(object sender, RoutedEventArgs e)
            {
                try
                {
                    // Создаем нового сотрудника
                    var navigationService = NavigationService;
                    if (navigationService != null)
                    {
                        navigationService.Navigate(new AdminPagePerson(null));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при создании сотрудника: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            private void OpenPerson(object sender, MouseButtonEventArgs e)
            {
                try
                {
                    var border = sender as Border;
                    var grid = border?.Child as Grid;
                    var dataContext = grid?.DataContext as User;

                    if (dataContext != null)
                    {
                        // Находим пользователя для редактирования
                        var employee = _db.Employees.FirstOrDefault(emp => emp.ID == dataContext.Id);
                        if (employee != null)
                        {
                            var user = new User
                            {
                                Surname = employee.Surname,
                                Name = employee.Name,
                                Patronymic = employee.Patronymic,
                                Login = dataContext.Login
                            };

                            var navigationService = NavigationService;
                            if (navigationService != null)
                            {
                                navigationService.Navigate(new AdminPagePerson(user));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии сотрудника: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
    }
}
