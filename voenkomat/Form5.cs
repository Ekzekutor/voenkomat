using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;

namespace voenkomat
{
    public partial class Form5 : Form
    {
        private Form parentForm;
        private string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=admin;Database=voenkomat";
        private ContextMenuStrip rowContextMenu;
        private DataGridViewRow currentRightClickRow;

        public Form5(Form parent)
        {
            InitializeComponent();
            parentForm = parent;
            this.Load += Form5_Load;
            this.FormClosing += Form5_FormClosing;

            // Создаем контекстное меню для удаления
            rowContextMenu = new ContextMenuStrip();
            var deleteItem = new ToolStripMenuItem("Удалить военнослужащего");
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

        private void Form5_Load(object sender, EventArgs e)
        {
            LoadMilitaryServiceMembers();
        }

        private void LoadMilitaryServiceMembers(string searchTerm = "")
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string query = @"
                    SELECT 
                        m.id,
                        m.last_name AS ""Фамилия"", 
                        m.first_name AS ""Имя"", 
                        m.middle_name AS ""Отчество"", 
                        m.birth_date AS ""Дата рождения"",  -- Изменено на birth_date
                        m.rank_id
                    FROM 
                        military_service_member m
                    LEFT JOIN 
                        military_ranks r ON m.rank_id = r.id"; // Соединяем с таблицей воинских званий

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query += @"
                    WHERE 
                        m.last_name ILIKE @searchTerm OR
                        m.first_name ILIKE @searchTerm OR
                        m.middle_name ILIKE @searchTerm OR
                        r.rank_name ILIKE @searchTerm"; // Добавлено условие для поиска по воинскому званию
                }

                query += " ORDER BY m.last_name, m.first_name";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                        command.Parameters.AddWithValue("@searchTerm", "%" + searchTerm + "%");

                    using (var adapter = new NpgsqlDataAdapter(command))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        dataGridView1.DataSource = dt;

                        if (dataGridView1.Columns.Contains("RankComboBox"))
                        {
                            dataGridView1.Columns.Remove("RankComboBox");
                        }

                        AddRankComboBoxColumn(connection);

                        SetColumnWidths();
                        ApplyCellAndHeaderStyles();

                        // Скрыть столбец id и rank_id
                        if (dataGridView1.Columns.Contains("id"))
                            dataGridView1.Columns["id"].Visible = false;
                        if (dataGridView1.Columns.Contains("rank_id"))
                            dataGridView1.Columns["rank_id"].Visible = false;

                        dataGridView1.ClearSelection();
                    }
                }
            }
        }

        private void AddRankComboBoxColumn(NpgsqlConnection connection)
        {
            DataTable ranksTable = new DataTable();
            string rankQuery = "SELECT id, rank_name FROM military_ranks ORDER BY id";
            using (var rankCmd = new NpgsqlCommand(rankQuery, connection))
            {
                using (var adapter = new NpgsqlDataAdapter(rankCmd))
                {
                    adapter.Fill(ranksTable);
                }
            }

            DataGridViewComboBoxColumn rankComboBox = new DataGridViewComboBoxColumn
            {
                Name = "RankComboBox",
                HeaderText = "Воинское звание",
                DataPropertyName = "rank_id", // Связываем с колонкой rank_id из базы
                DisplayMember = "rank_name",
                ValueMember = "id",
                DataSource = ranksTable,
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                DropDownWidth = 160,
                Width = (int)(dataGridView1.Width * 0.20)
            };

            dataGridView1.Columns.Add(rankComboBox);
        }

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                button3.PerformClick(); // Вызываем поиск при нажатии Enter
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

        

        private void SetColumnWidths()
        {
            int totalWidth = dataGridView1.Width;

            if (dataGridView1.Columns.Contains("Фамилия"))
                dataGridView1.Columns["Фамилия"].Width = (int)(totalWidth * 0.20);
            if (dataGridView1.Columns.Contains("Имя"))
                dataGridView1.Columns["Имя"].Width = (int)(totalWidth * 0.20);
            if (dataGridView1.Columns.Contains("Отчество"))
                dataGridView1.Columns["Отчество"].Width = (int)(totalWidth * 0.20);
            if (dataGridView1.Columns.Contains("Дата рождения")) // Изменено на "Дата рождения"
                dataGridView1.Columns["Дата рождения"].Width = (int)(totalWidth * 0.20);
            if (dataGridView1.Columns.Contains("RankComboBox"))
                dataGridView1.Columns["RankComboBox"].Width = (int)(totalWidth * 0.20);
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

            var confirm = MessageBox.Show("Удалить выбранного военнослужащего?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
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

                string deleteQuery = "DELETE FROM military_service_member WHERE id = @id";
                using (var cmd = new NpgsqlCommand(deleteQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@id", userId);
                    cmd.ExecuteNonQuery();
                }
            }

            LoadMilitaryServiceMembers();
        }

        private void Form5_FormClosing(object sender, FormClosingEventArgs e)
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
                    DateTime birthDate; // Изменено на DateTime
                    DateTime.TryParse(row.Cells["Дата рождения"].Value?.ToString(), out birthDate); // Изменено на "Дата рождения"
                    int rankId = 0;

                    if (dataGridView1.Columns.Contains("RankComboBox") && row.Cells["RankComboBox"].Value != null)
                    {
                        rankId = Convert.ToInt32(row.Cells["RankComboBox"].Value);
                    }

                    // Проверка на существование rankId
                    if (rankId == 0)
                    {
                        MessageBox.Show("Выберите корректное воинское звание для всех военнослужащих.");
                        return; // Прерываем выполнение, если rankId некорректен
                    }

                    if (userId > 0)
                    {
                        string updateQuery = @"
                            UPDATE military_service_member
                            SET last_name = @lastName,
                                first_name = @firstName,
                                middle_name = @middleName,
                                birth_date = @birthDate,  -- Изменено на birth_date
                                rank_id = @rankId
                            WHERE id = @userId";

                        using (var cmd = new NpgsqlCommand(updateQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@lastName", lastName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@firstName", firstName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@middleName", middleName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@birthDate", birthDate); // Изменено на birthDate
                            cmd.Parameters.AddWithValue("@rankId", rankId);
                            cmd.Parameters.AddWithValue("@userId", userId);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string insertQuery = @"
                            INSERT INTO military_service_member (last_name, first_name, middle_name, birth_date, rank_id)
                            VALUES (@lastName, @firstName, @middleName, @birthDate, @rankId)"; // Изменено на birth_date

                        using (var cmd = new NpgsqlCommand(insertQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@lastName", lastName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@firstName", firstName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@middleName", middleName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@birthDate", birthDate); // Изменено на birthDate
                            cmd.Parameters.AddWithValue("@rankId", rankId);

                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show("Изменения успешно сохранены в базе данных.");
                LoadMilitaryServiceMembers();
            }
        }

        private int GetRankId(string rankName, NpgsqlConnection connection)
        {
            if (string.IsNullOrEmpty(rankName))
                return 0;

            string query = "SELECT id FROM military_ranks WHERE rank_name = @rankName";
            using (var cmd = new NpgsqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@rankName", rankName);
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            string searchTerm = textBox1.Text;
            if (searchTerm == "Введите данные для поиска" || string.IsNullOrWhiteSpace(searchTerm))
            {
                LoadMilitaryServiceMembers();
            }
            else
            {
                LoadMilitaryServiceMembers(searchTerm);
            }
        }
    }
}
