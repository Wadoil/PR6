using System;
using System.Data.Entity.Validation;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.Pages
{
    public partial class AdminPagePerson : Page
    {
        private Employees _currentEmployee;
        private Entities _db;
        private Authorisation _currentAuth;
        private bool _isNewEmployee;

        public AdminPagePerson(User user)
        {
            InitializeComponent();
            _db = new Entities();

            if (user == null)
            {
                // Создание нового сотрудника
                _isNewEmployee = true;
                _currentEmployee = new Employees();
                _currentAuth = new Authorisation();

                // Устанавливаем дату найма по умолчанию
                HireDateEntry.Text = DateTime.Now.ToString("dd.MM.yyyy");

                TitleTextBlock.Text = "Добавление нового сотрудника";
            }
            else
            {
                // Редактирование существующего сотрудника
                _isNewEmployee = false;
                _currentEmployee = _db.Employees.FirstOrDefault(x =>
                    x.Surname == user.Surname &&
                    x.Name == user.Name &&
                    x.Patronymic == user.Patronymic);

                if (_currentEmployee != null)
                {
                    _currentAuth = _db.Authorisation.FirstOrDefault(x => x.ID == _currentEmployee.Authorisation_ID);
                    LoadEmployeeData();
                }

                TitleTextBlock.Text = "Редактирование сотрудника";
            }
        }

        private void LoadEmployeeData()
        {
            try
            {
                NameEntry.Text = _currentEmployee.Name;
                SurnameEntry.Text = _currentEmployee.Surname;
                PatronymicEntry.Text = _currentEmployee.Patronymic;

                var position = _db.Position.FirstOrDefault(x => x.ID == _currentEmployee.Position_ID);
                PositionEntry.Text = position?.Name ?? "";

                var department = _db.Departments.FirstOrDefault(x => x.ID == _currentEmployee.Department_ID);
                DepartmentEntry.Text = department?.Name ?? "";

                if (_currentEmployee.Hire_Date != null)
                    HireDateEntry.Text = _currentEmployee.Hire_Date.ToString("dd.MM.yyyy");

                if (_currentAuth != null)
                {
                    LoginEntry.Text = _currentAuth.Login;
                    PasswordEntry.Password = ""; // Не отображаем пароль
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    // Валидация
                    if (string.IsNullOrWhiteSpace(SurnameEntry.Text) ||
                        string.IsNullOrWhiteSpace(NameEntry.Text) ||
                        string.IsNullOrWhiteSpace(PositionEntry.Text))
                    {
                        MessageBox.Show("Заполните обязательные поля (Фамилия, Имя, Должность)",
                            "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Обработка должности
                    Position position = null;
                    if (!string.IsNullOrWhiteSpace(PositionEntry.Text))
                    {
                        position = _db.Position.FirstOrDefault(x => x.Name == PositionEntry.Text.Trim());
                        if (position == null)
                        {
                            position = new Position { Name = PositionEntry.Text.Trim() };
                            _db.Position.Add(position);
                            _db.SaveChanges();
                        }
                    }

                    // Обработка отдела
                    Departments department = null;
                    if (!string.IsNullOrWhiteSpace(DepartmentEntry.Text))
                    {
                        department = _db.Departments.FirstOrDefault(x => x.Name == DepartmentEntry.Text.Trim());
                        if (department == null)
                        {
                            department = new Departments { Name = DepartmentEntry.Text.Trim() };
                            _db.Departments.Add(department);
                            _db.SaveChanges();
                        }
                    }

                    if (_isNewEmployee)
                    {
                        // Создание нового сотрудника
                        _currentEmployee = new Employees
                        {
                            Name = NameEntry.Text.Trim(),
                            Surname = SurnameEntry.Text.Trim(),
                            Patronymic = string.IsNullOrWhiteSpace(PatronymicEntry.Text) ? null : PatronymicEntry.Text.Trim(),
                            Position_ID = position.ID,
                            Department_ID = department.ID
                        };

                        // Устанавливаем дату найма
                        if (DateTime.TryParse(HireDateEntry.Text, out DateTime hireDate))
                        {
                            _currentEmployee.Hire_Date = hireDate;
                        }

                        // Добавляем сотрудника сначала
                        _db.Employees.Add(_currentEmployee);
                        _db.SaveChanges();

                        // Создание записи авторизации
                        _currentAuth = new Authorisation
                        {
                            Login = string.IsNullOrWhiteSpace(LoginEntry.Text)
                                ? GenerateDefaultLogin()
                                : LoginEntry.Text.Trim(),
                            Password = Hash.HashPassword(string.IsNullOrWhiteSpace(PasswordEntry.Password) ? "12345" : PasswordEntry.Password),
                            ID = _currentEmployee.ID // Используем тот же ID
                        };

                        _db.Authorisation.Add(_currentAuth);

                        // Обновляем ссылку на авторизацию у сотрудника
                        _currentEmployee.Authorisation_ID = _currentAuth.ID;
                    }
                    else
                    {
                        // Обновление существующего сотрудника
                        _currentEmployee.Name = NameEntry.Text.Trim();
                        _currentEmployee.Surname = SurnameEntry.Text.Trim();
                        _currentEmployee.Patronymic = string.IsNullOrWhiteSpace(PatronymicEntry.Text) ? null : PatronymicEntry.Text.Trim();
                        _currentEmployee.Position_ID = position.ID;
                        _currentEmployee.Department_ID = department.ID;

                        if (DateTime.TryParse(HireDateEntry.Text, out DateTime hireDate))
                        {
                            _currentEmployee.Hire_Date = hireDate;
                        }

                        if (_currentAuth != null)
                        {
                            _currentAuth.Login = string.IsNullOrWhiteSpace(LoginEntry.Text)
                                ? GenerateDefaultLogin()
                                : LoginEntry.Text.Trim();

                            if (!string.IsNullOrWhiteSpace(PasswordEntry.Password))
                            {
                                _currentAuth.Password = Hash.HashPassword(PasswordEntry.Password);
                            }
                        }
                    }

                    _db.SaveChanges();
                    transaction.Commit();

                    MessageBox.Show(_isNewEmployee ? "Сотрудник успешно добавлен!" : "Данные успешно сохранены!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    NavigationService?.GoBack();
                }
                catch (DbEntityValidationException ex)
                {
                    transaction.Rollback();
                    string errors = "";
                    foreach (var validationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            errors += $"Свойство: {validationError.PropertyName}, Ошибка: {validationError.ErrorMessage}\n";
                        }
                    }
                    MessageBox.Show($"Ошибка валидации:\n{errors}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GenerateDefaultLogin()
        {
            string baseLogin = $"{SurnameEntry.Text.Trim().ToLower()}.{NameEntry.Text.Trim().ToLower().Substring(0, 1)}";
            int counter = 1;
            string login = baseLogin;

            while (_db.Authorisation.Any(a => a.Login == login))
            {
                login = $"{baseLogin}{counter}";
                counter++;
            }

            return login;
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            NameEntry.Clear();
            SurnameEntry.Clear();
            PatronymicEntry.Clear();
            PositionEntry.Clear();
            DepartmentEntry.Clear();
            HireDateEntry.Text = DateTime.Now.ToString("dd.MM.yyyy");
            LoginEntry.Clear();
            PasswordEntry.Clear();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}