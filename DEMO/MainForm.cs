using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace DEMO
{
    public partial class MainForm : Form
    {
        private Panel pnlAuth, pnlAdmin, pnlUser;

        private TextBox txtLogin, txtPassword;
        private Button btnLogin, btnResetCaptcha;
        private PictureBox[] pieces = new PictureBox[4];
        private PictureBox[] targets = new PictureBox[4];
        private int[] targetContents = new int[4];
        private Label lblCaptchaStatus;
        private int consecutiveFailures = 0;

        private DataGridView dgvUsers;
        private TextBox txtNewLogin, txtNewPassword, txtSearch;
        private ComboBox cmbNewRole;
        private Button btnAddUser, btnUnblockUser, btnRefreshUsers, btnAdminLogout, btnEditPassword, btnSearch, btnExport;

        private Label lblWelcome;
        private Button btnUserLogout;

        private List<User> users;
        private string currentUser = "";
        private string currentRole = "";

        public MainForm()
        {
            this.Text = "ООО Бургер плюс - Информационная система";
            this.Size = new Size(1000, 800);
            this.MinimumSize = new Size(950, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            // Создание папки для капчи, если её нет
            string captchaDir = Path.Combine(Application.StartupPath, "CaptchaImages");
            if (!Directory.Exists(captchaDir))
            {
                Directory.CreateDirectory(captchaDir);
                MessageBox.Show("Создана папка CaptchaImages. Поместите туда файлы 1.png, 2.png, 3.png, 4.png",
                    "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            users = new List<User>
            {
                new User { Login = "admin", Password = "123", Role = "Admin", IsBlocked = false },
                new User { Login = "user1", Password = "123", Role = "User", IsBlocked = false }
            };

            InitializeAuthPanel();
            InitializeAdminPanel();
            InitializeUserPanel();
            ShowAuthPanel();
        }

        private bool IsValidLogin(string login)
        {
            if (string.IsNullOrEmpty(login)) return false;
            return Regex.IsMatch(login, @"^[a-zA-Z0-9а-яА-ЯёЁ._-]+$");
        }

        private bool ValidateUser(string login, string password, out string role, out bool isBlocked)
        {
            role = "";
            isBlocked = false;
            var user = users.FirstOrDefault(u => u.Login == login && u.Password == password);
            if (user != null)
            {
                role = user.Role;
                isBlocked = user.IsBlocked;
                return true;
            }
            return false;
        }

        private void BlockUser(string login)
        {
            var user = users.FirstOrDefault(u => u.Login == login);
            if (user != null) user.IsBlocked = true;
        }

        private void UnblockUser(string login)
        {
            var user = users.FirstOrDefault(u => u.Login == login);
            if (user != null) user.IsBlocked = false;
        }

        private void AddUser(string login, string password, string role)
        {
            if (users.Any(u => u.Login == login))
                throw new Exception("Пользователь с таким логином уже существует");
            if (!IsValidLogin(login))
                throw new Exception("Логин может содержать только буквы, цифры, точки, дефисы и подчеркивания");
            users.Add(new User { Login = login, Password = password, Role = role, IsBlocked = false });
        }

        private void ChangePassword(string login, string newPassword)
        {
            var user = users.FirstOrDefault(u => u.Login == login);
            if (user != null) user.Password = newPassword;
        }

        private void LoadUsersToGrid()
        {
            var list = users.Select(u => new {
                Логин = u.Login,
                Роль = u.Role,
                Заблокирован = u.IsBlocked ? "Да" : "Нет"
            }).ToList();
            dgvUsers.DataSource = null;
            dgvUsers.DataSource = list;
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            string searchText = txtSearch.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(searchText))
            {
                LoadUsersToGrid();
                return;
            }
            var filtered = users.Where(u => u.Login.ToLower().Contains(searchText))
                .Select(u => new {
                    Логин = u.Login,
                    Роль = u.Role,
                    Заблокирован = u.IsBlocked ? "Да" : "Нет"
                }).ToList();
            dgvUsers.DataSource = null;
            dgvUsers.DataSource = filtered;
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Сохранить отчет";
            saveFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt";
            saveFileDialog.DefaultExt = "txt";
            saveFileDialog.FileName = "Отчет_пользователи_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    List<string> lines = new List<string>();
                    lines.Add("═══════════════════════════════════════════════════════════════════════");
                    lines.Add("           ОТЧЕТ ПО ПОЛЬЗОВАТЕЛЯМ ИНФОРМАЦИОННОЙ СИСТЕМЫ");
                    lines.Add("═══════════════════════════════════════════════════════════════════════");
                    lines.Add("");
                    lines.Add("Дата и время формирования: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                    lines.Add("");
                    lines.Add("┌────────────────────────┬──────────────┬────────────────┐");
                    lines.Add("│         ЛОГИН          │     РОЛЬ     │  ЗАБЛОКИРОВАН  │");
                    lines.Add("├────────────────────────┼──────────────┼────────────────┤");

                    foreach (var user in users)
                    {
                        string blockedStatus = user.IsBlocked ? "ДА" : "НЕТ";
                        lines.Add($"│ {user.Login,-22} │ {user.Role,-12} │ {blockedStatus,-14} │");
                    }

                    lines.Add("└────────────────────────┴──────────────┴────────────────┘");
                    lines.Add("");
                    lines.Add("Всего пользователей: " + users.Count);
                    lines.Add("");
                    lines.Add("═══════════════════════════════════════════════════════════════════════");
                    lines.Add("Конец отчета");

                    File.WriteAllLines(saveFileDialog.FileName, lines, System.Text.Encoding.UTF8);

                    MessageBox.Show($"Отчет успешно сохранен!\n\nПапка: {Path.GetDirectoryName(saveFileDialog.FileName)}\nФайл: {Path.GetFileName(saveFileDialog.FileName)}",
                        "Экспорт выполнен", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении файла:\n{ex.Message}",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void InitializeAuthPanel()
        {
            pnlAuth = new Panel() { Dock = DockStyle.Fill, AutoScroll = true };

            Label lblTitle = new Label()
            {
                Text = "ООО Бургер плюс - Вход в систему",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Location = new Point(350, 20),
                AutoSize = true,
                ForeColor = Color.DarkBlue
            };

            Label lblLogin = new Label() { Text = "Логин:", Location = new Point(280, 80), AutoSize = true };
            txtLogin = new TextBox() { Location = new Point(370, 77), Width = 200 };

            Label lblPassword = new Label() { Text = "Пароль:", Location = new Point(280, 120), AutoSize = true };
            txtPassword = new TextBox() { Location = new Point(370, 117), Width = 200, PasswordChar = '*' };

            btnLogin = new Button() { Text = "ВОЙТИ", Location = new Point(370, 160), Width = 200, Height = 40, BackColor = Color.LightBlue, Font = new Font("Arial", 10, FontStyle.Bold) };

            Label lblCaptchaTitle = new Label() { Text = "СОБЕРИТЕ ПАЗЛ ПРАВИЛЬНО", Location = new Point(50, 230), AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold) };
            lblCaptchaStatus = new Label() { Text = "ПАЗЛ НЕ СОБРАН", Location = new Point(450, 260), AutoSize = true, ForeColor = Color.Red, Font = new Font("Arial", 10, FontStyle.Bold) };

            btnResetCaptcha = new Button()
            {
                Text = "СБРОСИТЬ ПАЗЛ",
                Location = new Point(350, 520),
                Width = 200,
                Height = 35,
                BackColor = Color.LightGray,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            btnResetCaptcha.Click += (s, e) => ResetCaptcha();

            string[] files = { "1.png", "2.png", "3.png", "4.png" };
            Point[] piecePos = { new Point(50, 300), new Point(150, 300), new Point(50, 400), new Point(150, 400) };
            Point[] targetPos = { new Point(500, 300), new Point(600, 300), new Point(500, 400), new Point(600, 400) };

            for (int i = 0; i < 4; i++)
            {
                string path = Path.Combine(Application.StartupPath, "CaptchaImages", files[i]);
                targetContents[i] = -1;

                pieces[i] = new PictureBox()
                {
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Location = piecePos[i],
                    Size = new Size(80, 80),
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = i,
                    AllowDrop = true
                };

                if (File.Exists(path))
                    pieces[i].Image = Image.FromFile(path);
                else
                    pieces[i].BackColor = Color.FromArgb(100 + i * 40, 150, 200);

                int captureIndex = i;
                pieces[i].MouseDown += (s, e) => pieces[captureIndex].DoDragDrop(pieces[captureIndex], DragDropEffects.Move);
                pieces[i].DragEnter += (s, e) => e.Effect = DragDropEffects.Move;

                targets[i] = new PictureBox()
                {
                    BackColor = Color.LightGray,
                    Location = targetPos[i],
                    Size = new Size(80, 80),
                    BorderStyle = BorderStyle.FixedSingle,
                    AllowDrop = true,
                    Tag = i
                };

                int targetIndex = i;
                targets[i].DragEnter += (s, e) => e.Effect = DragDropEffects.Move;
                targets[i].DragDrop += (s, e) =>
                {
                    PictureBox dragged = (PictureBox)e.Data.GetData(typeof(PictureBox));
                    int draggedIndex = (int)dragged.Tag;

                    if (dragged != null && targetContents[targetIndex] == -1)
                    {
                        targets[targetIndex].Image = pieces[draggedIndex].Image;
                        targets[targetIndex].SizeMode = PictureBoxSizeMode.StretchImage;
                        pieces[draggedIndex].Visible = false;
                        targetContents[targetIndex] = draggedIndex;
                        CheckCaptchaComplete();
                    }
                };

                pnlAuth.Controls.Add(pieces[i]);
                pnlAuth.Controls.Add(targets[i]);
            }

            pnlAuth.Controls.Add(lblTitle);
            pnlAuth.Controls.Add(lblLogin);
            pnlAuth.Controls.Add(txtLogin);
            pnlAuth.Controls.Add(lblPassword);
            pnlAuth.Controls.Add(txtPassword);
            pnlAuth.Controls.Add(btnLogin);
            pnlAuth.Controls.Add(lblCaptchaTitle);
            pnlAuth.Controls.Add(lblCaptchaStatus);
            pnlAuth.Controls.Add(btnResetCaptcha);

            btnLogin.Click += BtnLogin_Click;
            this.Controls.Add(pnlAuth);
        }

        private void CheckCaptchaComplete()
        {
            bool allFilled = targetContents.All(t => t != -1);

            if (allFilled)
            {
                bool isCorrect = true;
                for (int i = 0; i < 4; i++)
                {
                    if (targetContents[i] != i)
                    {
                        isCorrect = false;
                        break;
                    }
                }

                if (isCorrect)
                {
                    lblCaptchaStatus.Text = "ПАЗЛ СОБРАН ПРАВИЛЬНО";
                    lblCaptchaStatus.ForeColor = Color.Green;
                }
                else
                {
                    lblCaptchaStatus.Text = "ПАЗЛ СОБРАН НЕПРАВИЛЬНО";
                    lblCaptchaStatus.ForeColor = Color.Red;
                }
            }
            else
            {
                lblCaptchaStatus.Text = "ПАЗЛ НЕ СОБРАН";
                lblCaptchaStatus.ForeColor = Color.Red;
            }
        }

        private void ResetCaptcha()
        {
            for (int i = 0; i < 4; i++)
            {
                pieces[i].Visible = true;
                targets[i].Image = null;
                targets[i].BackColor = Color.LightGray;
                targetContents[i] = -1;
            }
            lblCaptchaStatus.Text = "ПАЗЛ НЕ СОБРАН";
            lblCaptchaStatus.ForeColor = Color.Red;
        }

        private bool IsCaptchaCorrect()
        {
            bool allFilled = targetContents.All(t => t != -1);
            if (!allFilled) return false;

            for (int i = 0; i < 4; i++)
            {
                if (targetContents[i] != i) return false;
            }
            return true;
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                consecutiveFailures++;
                MessageBox.Show($"Введите логин и пароль. Попытка {consecutiveFailures} из 3", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                if (consecutiveFailures >= 3)
                {
                    MessageBox.Show("Превышено количество попыток входа. Форма будет сброшена.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ResetForm();
                }
                return;
            }

            if (!IsValidLogin(login))
            {
                MessageBox.Show("Логин может содержать только буквы, цифры, точки, дефисы и подчеркивания", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var existingUser = users.FirstOrDefault(u => u.Login == login);
            if (existingUser != null && existingUser.IsBlocked)
            {
                MessageBox.Show("Вы заблокированы. Обратитесь к администратору", "Доступ запрещен", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!IsCaptchaCorrect())
            {
                consecutiveFailures++;
                int remaining = 3 - consecutiveFailures;
                MessageBox.Show($"Пазл собран НЕПРАВИЛЬНО. Осталось попыток: {remaining}", "Капча", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                if (consecutiveFailures >= 3 && existingUser != null)
                {
                    BlockUser(login);
                    MessageBox.Show($"Пользователь {login} заблокирован за превышение попыток", "Блокировка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ResetForm();
                }
                else if (consecutiveFailures >= 3)
                {
                    MessageBox.Show("Превышено количество попыток. Перезагрузка формы.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ResetForm();
                }
                return;
            }

            bool isValid = ValidateUser(login, password, out string role, out bool isBlocked);

            if (!isValid)
            {
                consecutiveFailures++;
                int remaining = 3 - consecutiveFailures;
                MessageBox.Show($"Неверный логин или пароль. Осталось попыток: {remaining}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                if (consecutiveFailures >= 3 && existingUser != null)
                {
                    BlockUser(login);
                    MessageBox.Show($"Пользователь {login} заблокирован за превышение попыток", "Блокировка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ResetForm();
                }
                return;
            }

            consecutiveFailures = 0;
            MessageBox.Show($"Вы успешно авторизовались. Добро пожаловать, {login}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

            currentUser = login;
            currentRole = role;

            if (role == "Admin") ShowAdminPanel();
            else ShowUserPanel();
        }

        private void ResetForm()
        {
            txtLogin.Clear();
            txtPassword.Clear();
            ResetCaptcha();
            consecutiveFailures = 0;
        }

        private void InitializeUserPanel()
        {
            pnlUser = new Panel() { Dock = DockStyle.Fill };
            lblWelcome = new Label() { Text = "", Font = new Font("Arial", 14, FontStyle.Bold), Location = new Point(50, 80), AutoSize = true, ForeColor = Color.DarkGreen };
            Label lblInfo = new Label() { Text = "Вы вошли как ПОЛЬЗОВАТЕЛЬ", Font = new Font("Arial", 12), Location = new Point(50, 130), AutoSize = true, ForeColor = Color.Gray };
            btnUserLogout = new Button() { Text = "ВЫЙТИ", Location = new Point(50, 200), Width = 150, Height = 40, BackColor = Color.LightCoral, Font = new Font("Arial", 10, FontStyle.Bold) };
            pnlUser.Controls.Add(lblWelcome);
            pnlUser.Controls.Add(lblInfo);
            pnlUser.Controls.Add(btnUserLogout);
            btnUserLogout.Click += (s, e) => Logout();
            this.Controls.Add(pnlUser);
        }

        private void ShowUserPanel()
        {
            pnlAuth.Visible = false;
            pnlAdmin.Visible = false;
            pnlUser.Visible = true;
            lblWelcome.Text = $"Добро пожаловать, {currentUser}";
        }

        private void InitializeAdminPanel()
        {
            pnlAdmin = new Panel() { Dock = DockStyle.Fill, AutoScroll = true };

            Label lblTitle = new Label() { Text = "ООО Бургер плюс - Панель администратора", Font = new Font("Arial", 14, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true };
            dgvUsers = new DataGridView() { Location = new Point(20, 60), Size = new Size(550, 300), AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };

            Label lblNewLogin = new Label() { Text = "Логин:", Location = new Point(600, 60), AutoSize = true };
            txtNewLogin = new TextBox() { Location = new Point(680, 57), Width = 180 };
            Label lblNewPassword = new Label() { Text = "Пароль:", Location = new Point(600, 100), AutoSize = true };
            txtNewPassword = new TextBox() { Location = new Point(680, 97), Width = 180 };
            Label lblNewRole = new Label() { Text = "Роль:", Location = new Point(600, 140), AutoSize = true };
            cmbNewRole = new ComboBox() { Location = new Point(680, 137), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbNewRole.Items.AddRange(new string[] { "User", "Admin" });
            cmbNewRole.SelectedIndex = 0;
            btnAddUser = new Button() { Text = "ДОБАВИТЬ", Location = new Point(680, 180), Width = 180, Height = 35, BackColor = Color.LightGreen, Font = new Font("Arial", 9, FontStyle.Bold) };

            Label lblSearch = new Label() { Text = "Поиск:", Location = new Point(20, 380), AutoSize = true };
            txtSearch = new TextBox() { Location = new Point(80, 377), Width = 200 };
            btnSearch = new Button() { Text = "НАЙТИ", Location = new Point(290, 375), Size = new Size(80, 25), BackColor = Color.LightBlue, Font = new Font("Arial", 9, FontStyle.Bold) };
            btnSearch.Click += BtnSearch_Click;

            btnExport = new Button() { Text = "ЭКСПОРТ ОТЧЕТА", Location = new Point(400, 375), Size = new Size(150, 25), BackColor = Color.LightGreen, Font = new Font("Arial", 9, FontStyle.Bold) };
            btnExport.Click += BtnExport_Click;

            btnUnblockUser = new Button() { Text = "РАЗБЛОКИРОВАТЬ", Location = new Point(20, 420), Width = 150, Height = 40, BackColor = Color.Gold, Font = new Font("Arial", 9, FontStyle.Bold) };
            btnEditPassword = new Button() { Text = "СМЕНИТЬ ПАРОЛЬ", Location = new Point(190, 420), Width = 150, Height = 40, BackColor = Color.LightBlue, Font = new Font("Arial", 9, FontStyle.Bold) };
            btnRefreshUsers = new Button() { Text = "ОБНОВИТЬ", Location = new Point(360, 420), Width = 150, Height = 40, BackColor = Color.LightBlue, Font = new Font("Arial", 9, FontStyle.Bold) };
            btnAdminLogout = new Button() { Text = "ВЫЙТИ", Location = new Point(700, 550), Width = 150, Height = 40, BackColor = Color.LightCoral, Font = new Font("Arial", 9, FontStyle.Bold) };

            pnlAdmin.Controls.Add(lblTitle);
            pnlAdmin.Controls.Add(dgvUsers);
            pnlAdmin.Controls.Add(lblNewLogin);
            pnlAdmin.Controls.Add(txtNewLogin);
            pnlAdmin.Controls.Add(lblNewPassword);
            pnlAdmin.Controls.Add(txtNewPassword);
            pnlAdmin.Controls.Add(lblNewRole);
            pnlAdmin.Controls.Add(cmbNewRole);
            pnlAdmin.Controls.Add(btnAddUser);
            pnlAdmin.Controls.Add(lblSearch);
            pnlAdmin.Controls.Add(txtSearch);
            pnlAdmin.Controls.Add(btnSearch);
            pnlAdmin.Controls.Add(btnExport);
            pnlAdmin.Controls.Add(btnUnblockUser);
            pnlAdmin.Controls.Add(btnEditPassword);
            pnlAdmin.Controls.Add(btnRefreshUsers);
            pnlAdmin.Controls.Add(btnAdminLogout);

            btnAddUser.Click += BtnAddUser_Click;
            btnUnblockUser.Click += BtnUnblockUser_Click;
            btnEditPassword.Click += BtnEditPassword_Click;
            btnRefreshUsers.Click += (s, e) => LoadUsersToGrid();
            btnAdminLogout.Click += (s, e) => Logout();

            this.Controls.Add(pnlAdmin);
        }

        private void BtnAddUser_Click(object sender, EventArgs e)
        {
            string login = txtNewLogin.Text.Trim();
            string password = txtNewPassword.Text.Trim();
            string role = cmbNewRole.SelectedItem.ToString();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Заполните логин и пароль", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!IsValidLogin(login))
            {
                MessageBox.Show("Логин может содержать только буквы, цифры, точки, дефисы и подчеркивания", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                AddUser(login, password, role);
                MessageBox.Show($"Пользователь {login} добавлен", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtNewLogin.Clear();
                txtNewPassword.Clear();
                LoadUsersToGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUnblockUser_Click(object sender, EventArgs e)
        {
            if (dgvUsers.SelectedRows.Count > 0 && dgvUsers.SelectedRows[0].Cells[0].Value != null)
            {
                string login = dgvUsers.SelectedRows[0].Cells[0].Value.ToString();
                UnblockUser(login);
                LoadUsersToGrid();
                MessageBox.Show($"Пользователь {login} разблокирован", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Выберите пользователя в таблице", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnEditPassword_Click(object sender, EventArgs e)
        {
            if (dgvUsers.SelectedRows.Count > 0 && dgvUsers.SelectedRows[0].Cells[0].Value != null)
            {
                string login = dgvUsers.SelectedRows[0].Cells[0].Value.ToString();

                Form passwordForm = new Form();
                passwordForm.Text = $"Смена пароля для {login}";
                passwordForm.Size = new Size(350, 150);
                passwordForm.StartPosition = FormStartPosition.CenterParent;
                passwordForm.FormBorderStyle = FormBorderStyle.FixedDialog;

                Label lblNewPass = new Label() { Text = "Новый пароль:", Location = new Point(20, 20), AutoSize = true };
                TextBox txtNewPass = new TextBox() { Location = new Point(130, 17), Width = 180, PasswordChar = '*' };
                Button btnSave = new Button() { Text = "Сохранить", Location = new Point(130, 60), Width = 100, Height = 30, BackColor = Color.LightGreen };

                passwordForm.Controls.Add(lblNewPass);
                passwordForm.Controls.Add(txtNewPass);
                passwordForm.Controls.Add(btnSave);

                btnSave.Click += (s, ev) =>
                {
                    string newPassword = txtNewPass.Text.Trim();
                    if (string.IsNullOrEmpty(newPassword))
                    {
                        MessageBox.Show("Введите новый пароль", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    ChangePassword(login, newPassword);
                    LoadUsersToGrid();
                    MessageBox.Show($"Пароль для пользователя {login} изменен", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    passwordForm.Close();
                };

                passwordForm.ShowDialog();
            }
            else
            {
                MessageBox.Show("Выберите пользователя в таблице", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ShowAdminPanel()
        {
            pnlAuth.Visible = false;
            pnlUser.Visible = false;
            pnlAdmin.Visible = true;
            LoadUsersToGrid();
        }

        private void ShowAuthPanel()
        {
            pnlAuth.Visible = true;
            pnlAdmin.Visible = false;
            pnlUser.Visible = false;
            txtLogin.Clear();
            txtPassword.Clear();
            ResetCaptcha();
            consecutiveFailures = 0;
        }

        private void Logout()
        {
            currentUser = "";
            currentRole = "";
            ShowAuthPanel();
        }
    }
}