using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;

namespace voenkomat
{
    public partial class Form4 : Form
    {
        private Form parentForm;
        private string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=admin;Database=voenkomat";
        private ContextMenuStrip rowContextMenu;
        private DataGridViewRow currentRightClickRow;

        public Form4(Form parent)
        {
            InitializeComponent();
            parentForm = parent;

            // Initialize context menu for dataGridView1
            rowContextMenu = new ContextMenuStrip();
            var deleteItem = new ToolStripMenuItem("Удалить запись журнала");
            deleteItem.Click += DeleteItem_Click;
            rowContextMenu.Items.Add(deleteItem);

            // Attach events
            dataGridView1.MouseDown += DataGridView1_MouseDown;
            dataGridView1.ColumnHeaderMouseClick += DataGridView1_ColumnHeaderMouseClick;
            dataGridView1.EditingControlShowing += DataGridView1_EditingControlShowing;

            textBox1.GotFocus += TextBox1_GotFocus;
            textBox1.LostFocus += TextBox1_LostFocus;
            textBox1.KeyDown += TextBox1_KeyDown;
            textBox1.Text = "Введите данные для поиска";
            textBox1.ForeColor = Color.Gray;



            dataGridView1.AutoGenerateColumns = false; // manual columns
            InitializeDataGridViewColumns();
            LoadJournalEntries();
        }

        private void InitializeDataGridViewColumns()
        {
            // Clear existing columns
            dataGridView1.Columns.Clear();

            // ID column (hidden)
            DataGridViewTextBoxColumn idCol = new DataGridViewTextBoxColumn
            {
                Name = "id",
                DataPropertyName = "id",
                Visible = false
            };
            dataGridView1.Columns.Add(idCol);

            // Service Member ComboBox
            var serviceMemberCombo = new DataGridViewComboBoxColumn
            {
                Name = "ServiceMemberComboBox",
                HeaderText = "Военнослужащий",
                DataPropertyName = "service_member_id",
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                Width = 200
            };
            dataGridView1.Columns.Add(serviceMemberCombo);

            // Rank ComboBox
            var rankCombo = new DataGridViewComboBoxColumn
            {
                Name = "RankComboBox",
                HeaderText = "Воинское звание",
                DataPropertyName = "rank_id",
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                Width = 150
            };
            dataGridView1.Columns.Add(rankCombo);

            // Death and discharge date
            DataGridViewTextBoxColumn deathCol = new DataGridViewTextBoxColumn
            {
                Name = "Дата гибели/увольнения",
                HeaderText = "Дата гибели/увольнения",
                DataPropertyName = "death_and_discharge_date",
                Width = 150
            };
            dataGridView1.Columns.Add(deathCol);

            // Circumstances and reason
            DataGridViewTextBoxColumn circumstanceCol = new DataGridViewTextBoxColumn
            {
                Name = "Обстоятельства и причина",
                HeaderText = "Обстоятельства и причина",
                DataPropertyName = "circumstances_and_reason",
                Width = 200
            };
            dataGridView1.Columns.Add(circumstanceCol);

            // Message date
            DataGridViewTextBoxColumn msgDateCol = new DataGridViewTextBoxColumn
            {
                Name = "Дата сообщения",
                HeaderText = "Дата сообщения",
                DataPropertyName = "message_date",
                Width = 120
            };
            dataGridView1.Columns.Add(msgDateCol);

            // Document ComboBox
            var docCombo = new DataGridViewComboBoxColumn
            {
                Name = "DocumentComboBox",
                HeaderText = "Документ",
                DataPropertyName = "document_id",
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                Width = 180
            };
            dataGridView1.Columns.Add(docCombo);

            // Notes
            DataGridViewTextBoxColumn notesCol = new DataGridViewTextBoxColumn
            {
                Name = "Примечание",
                HeaderText = "Примечание",
                DataPropertyName = "notes",
                Width = 200
            };
            dataGridView1.Columns.Add(notesCol);

            // Sorting configuration
            SetColumnSortModes();
        }

        private void LoadJournalEntries(string searchTerm = "")
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string query = @"
                    SELECT 
                        j.id,
                        j.service_member_id,
                        m.last_name || ' ' || m.first_name || ' ' || COALESCE(m.middle_name,'') AS full_name,
                        j.rank_id,
                        r.rank_name,
                        j.death_and_discharge_date,
                        j.circumstances_and_reason,
                        j.message_date,
                        j.document_id,
                        d.title AS document_title,
                        j.notes
                    FROM journal j
                    JOIN military_service_member m ON j.service_member_id = m.id
                    JOIN military_ranks r ON j.rank_id = r.id
                    LEFT JOIN documents d ON j.document_id = d.id";

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query += " WHERE ";

                    // Разбиваем поисковый запрос на части
                    string[] searchParts = searchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    var conditions = new List<string>();

                    // Условия для поиска по ФИО
                    if (searchParts.Length == 1)
                    {
                        conditions.Add("(m.last_name ILIKE @search1 OR r.rank_name ILIKE @search1)");
                    }
                    else if (searchParts.Length == 2)
                    {
                        conditions.Add("(m.last_name ILIKE @search1 AND m.first_name ILIKE @search2)");
                    }
                    else if (searchParts.Length >= 3)
                    {
                        conditions.Add("(m.last_name ILIKE @search1 AND m.first_name ILIKE @search2 AND m.middle_name ILIKE @search3)");
                    }

                    // Условия для поиска по званию, обстоятельствам и документам
                    conditions.Add("(r.rank_name ILIKE @searchTerm OR j.circumstances_and_reason ILIKE @searchTerm OR d.title ILIKE @searchTerm)");

                    query += string.Join(" OR ", conditions);
                }

                query += " ORDER BY m.last_name, m.first_name";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        string[] searchParts = searchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (searchParts.Length >= 1)
                            command.Parameters.AddWithValue("@search1", $"%{searchParts[0]}%");
                        if (searchParts.Length >= 2)
                            command.Parameters.AddWithValue("@search2", $"%{searchParts[1]}%");
                        if (searchParts.Length >= 3)
                            command.Parameters.AddWithValue("@search3", $"%{searchParts[2]}%");

                        command.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
                    }

                    using (var adapter = new NpgsqlDataAdapter(command))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        // Load combo box data sources
                        var serviceMembers = new DataTable();
                        using (var smCmd = new NpgsqlCommand("SELECT id, last_name || ' ' || first_name || ' ' || COALESCE(middle_name,'') AS full_name FROM military_service_member ORDER BY last_name, first_name", connection))
                        using (var smAdapter = new NpgsqlDataAdapter(smCmd))
                        {
                            smAdapter.Fill(serviceMembers);
                        }
                        ((DataGridViewComboBoxColumn)dataGridView1.Columns["ServiceMemberComboBox"]).DataSource = serviceMembers;
                        ((DataGridViewComboBoxColumn)dataGridView1.Columns["ServiceMemberComboBox"]).DisplayMember = "full_name";
                        ((DataGridViewComboBoxColumn)dataGridView1.Columns["ServiceMemberComboBox"]).ValueMember = "id";

                        var ranks = new DataTable();
                        using (var rankCmd = new NpgsqlCommand("SELECT id, rank_name FROM military_ranks ORDER BY rank_name", connection))
                        using (var rankAdapter = new NpgsqlDataAdapter(rankCmd))
                        {
                            rankAdapter.Fill(ranks);
                        }
                        ((DataGridViewComboBoxColumn)dataGridView1.Columns["RankComboBox"]).DataSource = ranks;
                        ((DataGridViewComboBoxColumn)dataGridView1.Columns["RankComboBox"]).DisplayMember = "rank_name";
                        ((DataGridViewComboBoxColumn)dataGridView1.Columns["RankComboBox"]).ValueMember = "id";

                        var documents = new DataTable();
                        using (var docCmd = new NpgsqlCommand("SELECT id, title FROM documents ORDER BY title", connection))
                        using (var docAdapter = new NpgsqlDataAdapter(docCmd))
                        {
                            docAdapter.Fill(documents);
                        }
                        ((DataGridViewComboBoxColumn)dataGridView1.Columns["DocumentComboBox"]).DataSource = documents;
                        ((DataGridViewComboBoxColumn)dataGridView1.Columns["DocumentComboBox"]).DisplayMember = "title";
                        ((DataGridViewComboBoxColumn)dataGridView1.Columns["DocumentComboBox"]).ValueMember = "id";

                        // Set DataSource
                        dataGridView1.DataSource = dt;
                        dataGridView1.ClearSelection();
                    }
                }
            }
        }

        private void SetColumnSortModes()
        {
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                if (col.Name == "ServiceMemberComboBox")
                    col.SortMode = DataGridViewColumnSortMode.Programmatic;
                else
                    col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }

        private void DataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dataGridView1.CurrentCell.OwningColumn is DataGridViewComboBoxColumn)
            {
                if (e.Control is ComboBox combo)
                    combo.DropDownStyle = ComboBoxStyle.DropDownList;
            }
        }

        private void DataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var col = dataGridView1.Columns[e.ColumnIndex];
            if (col.Name == "ServiceMemberComboBox")
            {
                LoadJournalEntries();
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

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                PerformSearch();
            }
        }

        

        private void PerformSearch()
        {
            string searchTerm = textBox1.Text.Trim();
            if (searchTerm == "Введите данные для поиска")
                searchTerm = "";

            LoadJournalEntries(searchTerm);
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

            var confirm = MessageBox.Show("Удалить выбранную запись журнала?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                DeleteJournalEntry(currentRightClickRow);
                currentRightClickRow = null;
            }
        }

        private void DeleteJournalEntry(DataGridViewRow row)
        {
            if (row == null || row.Cells["id"].Value == null) return;

            if (!int.TryParse(row.Cells["id"].Value.ToString(), out int journalId)) return;

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string deleteQuery = "DELETE FROM journal WHERE id = @id";
                using (var cmd = new NpgsqlCommand(deleteQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@id", journalId);
                    cmd.ExecuteNonQuery();
                }
            }

            LoadJournalEntries();
        }

        private void SaveChanges()
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.IsNewRow) continue;

                    int journalId = 0;
                    if (dataGridView1.Columns.Contains("id") && row.Cells["id"].Value != DBNull.Value)
                        int.TryParse(row.Cells["id"].Value?.ToString(), out journalId);

                    if (!dataGridView1.Columns.Contains("ServiceMemberComboBox") || !dataGridView1.Columns.Contains("RankComboBox"))
                    {
                        MessageBox.Show("Список Военнослужащих или Воинских званий не загружен корректно.");
                        return;
                    }

                    int serviceMemberId = 0;
                    if (row.Cells["ServiceMemberComboBox"].Value != DBNull.Value && row.Cells["ServiceMemberComboBox"].Value != null)
                        serviceMemberId = Convert.ToInt32(row.Cells["ServiceMemberComboBox"].Value);

                    int rankId = 0;
                    if (row.Cells["RankComboBox"].Value != DBNull.Value && row.Cells["RankComboBox"].Value != null)
                        rankId = Convert.ToInt32(row.Cells["RankComboBox"].Value);

                    int documentId = 0;
                    if (dataGridView1.Columns.Contains("DocumentComboBox") && row.Cells["DocumentComboBox"].Value != DBNull.Value && row.Cells["DocumentComboBox"].Value != null)
                        documentId = Convert.ToInt32(row.Cells["DocumentComboBox"].Value);

                    string deathAndDischargeDate = row.Cells["Дата гибели/увольнения"]?.Value?.ToString();
                    string circumstancesAndReason = row.Cells["Обстоятельства и причина"]?.Value?.ToString();

                    DateTime messageDate = DateTime.MinValue;
                    var msgCellValue = row.Cells["Дата сообщения"]?.Value;
                    if (msgCellValue != DBNull.Value && msgCellValue != null)
                        DateTime.TryParse(msgCellValue.ToString(), out messageDate);

                    string notes = null;
                    if (dataGridView1.Columns.Contains("Примечание") && row.Cells["Примечание"].Value != DBNull.Value)
                        notes = row.Cells["Примечание"].Value.ToString();

                    if (serviceMemberId == 0)
                    {
                        MessageBox.Show("Выберите военнослужащего для всех записей.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (rankId == 0)
                    {
                        MessageBox.Show("Выберите воинское звание для всех записей.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (journalId > 0)
                    {
                        string updateSql = @"
                            UPDATE journal
                            SET service_member_id = @serviceMemberId,
                                rank_id = @rankId,
                                death_and_discharge_date = @deathAndDischargeDate,
                                circumstances_and_reason = @circumstancesAndReason,
                                message_date = @messageDate,
                                document_id = @documentId,
                                notes = @notes
                            WHERE id = @journalId";

                        using (var cmd = new NpgsqlCommand(updateSql, connection))
                        {
                            cmd.Parameters.AddWithValue("@serviceMemberId", serviceMemberId);
                            cmd.Parameters.AddWithValue("@rankId", rankId);
                            cmd.Parameters.AddWithValue("@deathAndDischargeDate", string.IsNullOrWhiteSpace(deathAndDischargeDate) ? (object)DBNull.Value : deathAndDischargeDate);
                            cmd.Parameters.AddWithValue("@circumstancesAndReason", string.IsNullOrWhiteSpace(circumstancesAndReason) ? (object)DBNull.Value : circumstancesAndReason);
                            cmd.Parameters.AddWithValue("@messageDate", messageDate == DateTime.MinValue ? (object)DBNull.Value : messageDate);
                            cmd.Parameters.AddWithValue("@documentId", documentId != 0 ? (object)documentId : DBNull.Value);
                            cmd.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(notes) ? (object)DBNull.Value : notes);
                            cmd.Parameters.AddWithValue("@journalId", journalId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string insertSql = @"
                            INSERT INTO journal (service_member_id, rank_id, death_and_discharge_date, circumstances_and_reason, message_date, document_id, notes)
                            VALUES (@serviceMemberId, @rankId, @deathAndDischargeDate, @circumstancesAndReason, @messageDate, @documentId, @notes)";

                        using (var cmd = new NpgsqlCommand(insertSql, connection))
                        {
                            cmd.Parameters.AddWithValue("@serviceMemberId", serviceMemberId);
                            cmd.Parameters.AddWithValue("@rankId", rankId);
                            cmd.Parameters.AddWithValue("@deathAndDischargeDate", string.IsNullOrWhiteSpace(deathAndDischargeDate) ? (object)DBNull.Value : deathAndDischargeDate);
                            cmd.Parameters.AddWithValue("@circumstancesAndReason", string.IsNullOrWhiteSpace(circumstancesAndReason) ? (object)DBNull.Value : circumstancesAndReason);
                            cmd.Parameters.AddWithValue("@messageDate", messageDate == DateTime.MinValue ? (object)DBNull.Value : messageDate);
                            cmd.Parameters.AddWithValue("@documentId", documentId != 0 ? (object)documentId : DBNull.Value);
                            cmd.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(notes) ? (object)DBNull.Value : notes);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            MessageBox.Show("Изменения успешно сохранены в базе данных.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadJournalEntries();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveChanges();
        }

        private void Form4_FormClosing(object sender, FormClosingEventArgs e)
        {
            parentForm.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            PerformSearch();
        }
    }
}