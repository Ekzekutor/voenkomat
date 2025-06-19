using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;

namespace voenkomat
{
    public partial class Form7 : Form
    {
        private Form parentForm;
        private string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=admin;Database=voenkomat";
        private ContextMenuStrip rowContextMenu;
        private DataGridViewRow currentRightClickRow;

        public Form7(Form parent)
        {
            InitializeComponent();
            parentForm = parent;
            this.Load += Form7_Load;
            this.FormClosing += Form7_FormClosing;
            dataGridView1.EditingControlShowing += DataGridView1_EditingControlShowing;

            // Создаем контекстное меню для удаления
            rowContextMenu = new ContextMenuStrip();
            var deleteItem = new ToolStripMenuItem("Удалить пользователя");
            deleteItem.Click += DeleteItem_Click;
            rowContextMenu.Items.Add(deleteItem);

            dataGridView1.MouseDown += DataGridView1_MouseDown;

            // Обработчики для textBox1
            textBox1.GotFocus += TextBox1_GotFocus;
            textBox1.LostFocus += TextBox1_LostFocus;
            textBox1.KeyDown += TextBox1_KeyDown;

            // Центрирование текста в текстбоксе
            textBox1.TextAlign = HorizontalAlignment.Center;

            textBox1.Text = "Введите данные для поиска";
            textBox1.ForeColor = Color.Gray;
        }

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                button3.PerformClick();
            }
        }

        private void Form7_Load(object sender, EventArgs e)
        {
            LoadUsers();
        }

        private void LoadUsers(string searchTerm = "")
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string query = @"
                    SELECT 
                        u.id,
                        u.last_name AS ""Фамилия"", 
                        u.first_name AS ""Имя"", 
                        u.middle_name AS ""Отчество"", 
                        u.login AS ""Логин"", 
                        u.password AS ""Пароль"", 
                        r.role_name AS ""Роль""
                    FROM 
                        users u
                    JOIN 
                        roles r ON u.role_id = r.id";

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query += @"
                    WHERE 
                        u.last_name ILIKE @searchTerm OR
                        u.first_name ILIKE @searchTerm OR
                        u.middle_name ILIKE @searchTerm OR
                        u.login ILIKE @searchTerm OR
                        r.role_name ILIKE @searchTerm";
                }

                query += " ORDER BY u.last_name, u.first_name";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                        command.Parameters.AddWithValue("@searchTerm", "%" + searchTerm + "%");

                    using (var adapter = new NpgsqlDataAdapter(command))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        dataGridView1.DataSource = dt;

                        SetColumnWidths();
                        ApplyCellAndHeaderStyles();

                        if (!dataGridView1.Columns.Contains("RoleComboBox"))
                        {
                            DataGridViewComboBoxColumn rolesColumn = new DataGridViewComboBoxColumn
                            {
                                DataPropertyName = "Роль",
                                HeaderText = "Роль",
                                Name = "RoleComboBox",
                                FlatStyle = FlatStyle.Flat,
                                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                                DropDownWidth = 120
                            };

                            string roleQuery = "SELECT role_name FROM roles ORDER BY id";
                            using (var roleCmd = new NpgsqlCommand(roleQuery, connection))
                            {
                                using (var roleReader = roleCmd.ExecuteReader())
                                {
                                    while (roleReader.Read())
                                    {
                                        rolesColumn.Items.Add(roleReader.GetString(0));
                                    }
                                }
                            }

                            int roleColumnIndex = dataGridView1.Columns["Роль"].Index;
                            dataGridView1.Columns.Remove("Роль");
                            dataGridView1.Columns.Insert(roleColumnIndex, rolesColumn);
                        }

                        if (dataGridView1.Columns.Contains("id"))
                        {
                            dataGridView1.Columns["id"].Visible = false;
                        }

                        dataGridView1.RowsDefaultCellStyle.BackColor = Color.White;
                        dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
                        dataGridView1.AllowUserToAddRows = true;
                        dataGridView1.ClearSelection();
                    }
                }
            }
        }

        private void TextBox1_GotFocus(object sender, EventArgs e)
        {
            if (textBox1.Text == "Введите данные для поиска")
            {
                textBox1.Text = "";
                textBox1.ForeColor = Color.Black;
            }
        }

        private void TextBox1_LostFocus(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                textBox1.Text = "Введите данные для поиска";
                textBox1.ForeColor = Color.Gray;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string searchTerm = textBox1.Text;
            if (searchTerm == "Введите данные для поиска" || string.IsNullOrWhiteSpace(searchTerm))
            {
                LoadUsers();
            }
            else
            {
                LoadUsers(searchTerm);
            }
        }

        private void SetColumnWidths()
        {
            int totalWidth = dataGridView1.Width;

            if (dataGridView1.Columns.Contains("Фамилия"))
                dataGridView1.Columns["Фамилия"].Width = (int)(totalWidth * 0.20);
            if (dataGridView1.Columns.Contains("Имя"))
                dataGridView1.Columns["Имя"].Width = (int)(totalWidth * 0.20);
            if (dataGridView1.Columns.Contains("Отчество"))
                dataGridView1.Columns["Отчество"].Width = (int)(totalWidth * 0.20);
            if (dataGridView1.Columns.Contains("Логин"))
                dataGridView1.Columns["Логин"].Width = (int)(totalWidth * 0.15);
            if (dataGridView1.Columns.Contains("Пароль"))
                dataGridView1.Columns["Пароль"].Width = (int)(totalWidth * 0.15);
            if (dataGridView1.Columns.Contains("RoleComboBox"))
                dataGridView1.Columns["RoleComboBox"].Width = (int)(totalWidth * 0.10);
        }

        private void ApplyCellAndHeaderStyles()
        {
            var cellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 20f, FontStyle.Regular),
                SelectionBackColor = Color.LightSkyBlue,
                SelectionForeColor = Color.Black,
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };

            var headerStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                Font = new Font("Segoe UI", 30f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                ForeColor = Color.Black
            };

            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                col.DefaultCellStyle = cellStyle;
                col.HeaderCell.Style = headerStyle;
                col.Resizable = DataGridViewTriState.False;
            }

            dataGridView1.RowHeadersDefaultCellStyle.BackColor = Color.White;
            dataGridView1.RowHeadersDefaultCellStyle.SelectionBackColor = Color.LightSkyBlue;
            dataGridView1.RowHeadersDefaultCellStyle.SelectionForeColor = Color.Black;

            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.RowTemplate.Height = 40;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.AllowUserToResizeColumns = false;
        }

        private void DataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is ComboBox comboBox)
            {
                comboBox.BackColor = Color.White;
                comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            }
        }

        private void DataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hit = dataGridView1.HitTest(e.X, e.Y);
                if (hit.RowIndex >= 0)
                {
                    dataGridView1.ClearSelection();
                    dataGridView1.Rows[hit.RowIndex].Selected = true;
                    currentRightClickRow = dataGridView1.Rows[hit.RowIndex];
                    rowContextMenu.Show(dataGridView1, e.Location);
                }
            }
        }

        private void DeleteItem_Click(object sender, EventArgs e)
        {
            if (currentRightClickRow == null) return;

            var confirm = MessageBox.Show("Удалить выбранного пользователя?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                DeleteUser(currentRightClickRow);
                currentRightClickRow = null;
            }
        }

        private void DeleteUser(DataGridViewRow row)
        {
            if (row.Cells["id"].Value == null) return;

            int userId;
            if (!int.TryParse(row.Cells["id"].Value.ToString(), out userId)) return;

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string deleteQuery = "DELETE FROM users WHERE id = @id";
                using (var cmd = new NpgsqlCommand(deleteQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@id", userId);
                    cmd.ExecuteNonQuery();
                }
            }

            LoadUsers();
        }

        private void Form7_FormClosing(object sender, FormClosingEventArgs e)
        {
            parentForm.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveChanges();
        }

        private void SaveChanges()
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.IsNewRow) continue;

                    int userId = 0;
                    if (dataGridView1.Columns.Contains("id") && row.Cells["id"].Value != DBNull.Value)
                    {
                        int.TryParse(row.Cells["id"].Value?.ToString(), out userId);
                    }

                    string lastName = row.Cells["Фамилия"].Value?.ToString();
                    string firstName = row.Cells["Имя"].Value?.ToString();
                    string middleName = row.Cells["Отчество"].Value?.ToString();
                    string login = row.Cells["Логин"].Value?.ToString();
                    string password = row.Cells["Пароль"].Value?.ToString();

                    string roleName = null;
                    if (dataGridView1.Columns.Contains("RoleComboBox") && row.Cells["RoleComboBox"].Value != null)
                    {
                        roleName = row.Cells["RoleComboBox"].Value.ToString();
                    }
                    else if (dataGridView1.Columns.Contains("Роль") && row.Cells["Роль"].Value != null)
                    {
                        roleName = row.Cells["Роль"].Value.ToString();
                    }

                    int roleId = GetRoleId(roleName, connection);

                    if (userId > 0)
                    {
                        if (!IsLoginUnique(login, userId, connection))
                        {
                            MessageBox.Show($"Логин '{login}' уже существует. Пожалуйста, выберите другой логин.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        string updateQuery = @"
                            UPDATE users
                            SET last_name = @lastName,
                                first_name = @firstName,
                                middle_name = @middleName,
                                login = @login,
                                password = @password,
                                role_id = @roleId
                            WHERE id = @userId";

                        using (var cmd = new NpgsqlCommand(updateQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@lastName", lastName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@firstName", firstName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@middleName", middleName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@login", login);
                            cmd.Parameters.AddWithValue("@password", password ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@roleId", roleId);
                            cmd.Parameters.AddWithValue("@userId", userId);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        if (!IsLoginUniqueNew(login, connection))
                        {
                            MessageBox.Show($"Логин '{login}' уже существует. Пожалуйста, выберите другой логин.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        string insertQuery = @"
                            INSERT INTO users (last_name, first_name, middle_name, login, password, role_id)
                            VALUES (@lastName, @firstName, @middleName, @login, @password, @roleId)";

                        using (var cmd = new NpgsqlCommand(insertQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@lastName", lastName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@firstName", firstName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@middleName", middleName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@login", login);
                            cmd.Parameters.AddWithValue("@password", password ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@roleId", roleId);

                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show("Изменения успешно сохранены в базе данных.");
                LoadUsers();
            }
        }

        private int GetRoleId(string roleName, NpgsqlConnection connection)
        {
            if (string.IsNullOrEmpty(roleName))
                return 0;

            string query = "SELECT id FROM roles WHERE role_name = @roleName";
            using (var cmd = new NpgsqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@roleName", roleName);
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        private bool IsLoginUnique(string login, int userId, NpgsqlConnection connection)
        {
            string query = "SELECT COUNT(*) FROM users WHERE login = @login AND id <> @userId";
            using (var cmd = new NpgsqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@login", login);
                cmd.Parameters.AddWithValue("@userId", userId);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count == 0;
            }
        }

        private bool IsLoginUniqueNew(string login, NpgsqlConnection connection)
        {
            string query = "SELECT COUNT(*) FROM users WHERE login = @login";
            using (var cmd = new NpgsqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@login", login);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count == 0;
            }
        }
    }
}
