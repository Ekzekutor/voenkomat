using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Npgsql;

namespace voenkomat
{
    public partial class Form6 : Form
    {
        private Form parentForm;
        private string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=admin;Database=voenkomat";
        private ContextMenuStrip rowContextMenu;
        private ContextMenuStrip fileContextMenu;
        private DataGridViewRow currentRightClickRow;
        private DataGridViewCell currentRightClickCell;
        private DataTable originalDataTable;

        public Form6(Form parent)
        {
            InitializeComponent();
            parentForm = parent;

            // Инициализация контекстного меню для строки
            rowContextMenu = new ContextMenuStrip();
            var deleteItem = new ToolStripMenuItem("Удалить документ");
            deleteItem.Click += DeleteItem_Click;
            rowContextMenu.Items.Add(deleteItem);

            // Инициализация контекстного меню для файла
            fileContextMenu = new ContextMenuStrip();

            var addFileItem = new ToolStripMenuItem("Добавить файл");
            addFileItem.Name = "addFileItem";
            addFileItem.Click += AddFileItem_Click;
            fileContextMenu.Items.Add(addFileItem);

            var openFileItem = new ToolStripMenuItem("Открыть файл");
            openFileItem.Name = "openFileItem";
            openFileItem.Click += OpenFileItem_Click;
            fileContextMenu.Items.Add(openFileItem);

            var downloadFileItem = new ToolStripMenuItem("Скачать файл");
            downloadFileItem.Name = "downloadFileItem";
            downloadFileItem.Click += DownloadFileItem_Click;
            fileContextMenu.Items.Add(downloadFileItem);

            var replaceFileItem = new ToolStripMenuItem("Заменить файл");
            replaceFileItem.Name = "replaceFileItem";
            replaceFileItem.Click += ReplaceFileItem_Click;
            fileContextMenu.Items.Add(replaceFileItem);

            var deleteFileItem = new ToolStripMenuItem("Удалить файл");
            deleteFileItem.Name = "deleteFileItem";
            deleteFileItem.Click += DeleteFileItem_Click;
            fileContextMenu.Items.Add(deleteFileItem);

            dataGridView1.MouseDown += DataGridView1_MouseDown;
            dataGridView1.AllowUserToAddRows = true;
            dataGridView1.AllowUserToDeleteRows = true;
            dataGridView1.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
            dataGridView1.ColumnHeaderMouseClick += (s, e) => { };

            // Настройка текстбокса для поиска
            textBox1.GotFocus += TextBox1_GotFocus;
            textBox1.LostFocus += TextBox1_LostFocus;
            textBox1.KeyDown += TextBox1_KeyDown;
            textBox1.TextAlign = HorizontalAlignment.Center;
            textBox1.Text = "Введите данные для поиска";
            textBox1.ForeColor = Color.Gray;

            dataGridView1.CellFormatting += DataGridView1_CellFormatting;
        }

        private void Form6_Load(object sender, EventArgs e)
        {
            LoadDocuments();
        }

        private void LoadDocuments(string searchTerm = "")
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT id, title, file IS NOT NULL AS has_file FROM documents";
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query += " WHERE title ILIKE @searchTerm";
                }
                query += " ORDER BY title";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                        command.Parameters.AddWithValue("@searchTerm", "%" + searchTerm + "%");

                    using (var adapter = new NpgsqlDataAdapter(command))
                    {
                        originalDataTable = new DataTable();
                        adapter.Fill(originalDataTable);

                        dataGridView1.DataSource = originalDataTable;
                        ConfigureDataGridViewColumns();
                    }
                }
            }
        }

        private void ConfigureDataGridViewColumns()
        {
            if (dataGridView1.Columns.Contains("id"))
            {
                dataGridView1.Columns["id"].Visible = false;
                dataGridView1.Columns["id"].SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            if (dataGridView1.Columns.Contains("title"))
            {
                dataGridView1.Columns["title"].SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            if (dataGridView1.Columns.Contains("has_file"))
            {
                dataGridView1.Columns["has_file"].Visible = false;
            }

            if (!dataGridView1.Columns.Contains("Файл"))
            {
                DataGridViewTextBoxColumn fileColumn = new DataGridViewTextBoxColumn
                {
                    Name = "Файл",
                    HeaderText = "Файл",
                    FillWeight = 20,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    Resizable = DataGridViewTriState.False,
                };
                dataGridView1.Columns.Add(fileColumn);
            }

            dataGridView1.ClearSelection();
        }

        private void DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dataGridView1.Columns[e.ColumnIndex].Name == "Файл")
            {
                if (originalDataTable == null || e.RowIndex < 0 || e.RowIndex >= originalDataTable.Rows.Count)
                {
                    e.Value = "";
                    return;
                }

                DataRow row = originalDataTable.Rows[e.RowIndex];
                bool hasFile = row.Table.Columns.Contains("has_file") &&
                              row["has_file"] != DBNull.Value &&
                              Convert.ToBoolean(row["has_file"]);

                e.Value = hasFile ? "Файл" : "";
                e.FormattingApplied = true;
            }
        }

        private void DataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hit = dataGridView1.HitTest(e.X, e.Y);
                if (hit.RowIndex >= 0 && !dataGridView1.Rows[hit.RowIndex].IsNewRow)
                {
                    dataGridView1.ClearSelection();
                    dataGridView1.Rows[hit.RowIndex].Selected = true;
                    currentRightClickRow = dataGridView1.Rows[hit.RowIndex];

                    if (hit.ColumnIndex == dataGridView1.Columns["Файл"].Index)
                    {
                        currentRightClickCell = currentRightClickRow.Cells[hit.ColumnIndex];
                        bool hasFile = currentRightClickRow.Cells["has_file"].Value != null &&
                                      currentRightClickRow.Cells["has_file"].Value != DBNull.Value &&
                                      Convert.ToBoolean(currentRightClickRow.Cells["has_file"].Value);

                        UpdateFileContextMenuVisibility(hasFile);
                        fileContextMenu.Show(dataGridView1, e.Location);
                    }
                    else
                    {
                        rowContextMenu.Show(dataGridView1, e.Location);
                    }
                }
            }
        }

        private void UpdateFileContextMenuVisibility(bool hasFile)
        {
            fileContextMenu.Items["openFileItem"].Visible = hasFile;
            fileContextMenu.Items["downloadFileItem"].Visible = hasFile;
            fileContextMenu.Items["replaceFileItem"].Visible = hasFile;
            fileContextMenu.Items["deleteFileItem"].Visible = hasFile;
            fileContextMenu.Items["addFileItem"].Visible = !hasFile;
        }

        private void AddFileItem_Click(object sender, EventArgs e)
        {
            if (currentRightClickRow == null) return;
            int documentId = Convert.ToInt32(currentRightClickRow.Cells["id"].Value);
            AddOrReplaceDocumentFile(documentId, false);
        }

        private void OpenFileItem_Click(object sender, EventArgs e)
        {
            if (currentRightClickRow == null) return;
            int documentId = Convert.ToInt32(currentRightClickRow.Cells["id"].Value);
            OpenDocumentFile(documentId);
        }

        private void DownloadFileItem_Click(object sender, EventArgs e)
        {
            if (currentRightClickRow == null) return;
            int documentId = Convert.ToInt32(currentRightClickRow.Cells["id"].Value);
            SaveDocumentFile(documentId);
        }

        private void ReplaceFileItem_Click(object sender, EventArgs e)
        {
            if (currentRightClickRow == null) return;
            int documentId = Convert.ToInt32(currentRightClickRow.Cells["id"].Value);
            AddOrReplaceDocumentFile(documentId, true);
        }

        private void DeleteFileItem_Click(object sender, EventArgs e)
        {
            if (currentRightClickRow == null) return;
            int documentId = Convert.ToInt32(currentRightClickRow.Cells["id"].Value);

            if (MessageBox.Show("Удалить файл из документа?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                UpdateDocumentFile(documentId, null, null);
            }
        }

        private void DeleteItem_Click(object sender, EventArgs e)
        {
            if (currentRightClickRow == null) return;

            if (MessageBox.Show("Удалить выбранный документ?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                DeleteDocument(currentRightClickRow);
            }
        }

        private void AddOrReplaceDocumentFile(int documentId, bool isReplace)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = isReplace ? "Выберите новый файл" : "Добавьте файл к документу";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        byte[] fileData = File.ReadAllBytes(openFileDialog.FileName);
                        string newTitle = Path.GetFileName(openFileDialog.FileName);
                        UpdateDocumentFile(documentId, fileData, newTitle);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OpenDocumentFile(int documentId)
        {
            try
            {
                byte[] fileData = GetFileData(documentId);
                if (fileData == null || fileData.Length == 0)
                {
                    MessageBox.Show("Файл не найден или пуст.", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string documentTitle = GetDocumentTitle(documentId);
                string tempFilePath = Path.Combine(Path.GetTempPath(), documentTitle);
                File.WriteAllBytes(tempFilePath, fileData);

                // Используем стандартный диалог Windows "Открыть с помощью"
                Process.Start(new ProcessStartInfo
                {
                    FileName = "rundll32.exe",
                    Arguments = $"shell32.dll,OpenAs_RunDLL \"{tempFilePath}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии файла: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveDocumentFile(int documentId)
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                string documentTitle = GetDocumentTitle(documentId);
                saveFileDialog.FileName = documentTitle;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    byte[] fileData = GetFileData(documentId);
                    if (fileData != null && fileData.Length > 0)
                    {
                        File.WriteAllBytes(saveFileDialog.FileName, fileData);
                        MessageBox.Show("Файл успешно сохранен.", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Файл не найден или пуст.", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private byte[] GetFileData(int documentId)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT file FROM documents WHERE id = @id";
                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@id", documentId);
                    return cmd.ExecuteScalar() as byte[];
                }
            }
        }

        private string GetDocumentTitle(int documentId)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT title FROM documents WHERE id = @id";
                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@id", documentId);
                    return cmd.ExecuteScalar()?.ToString();
                }
            }
        }

        private void UpdateDocumentFile(int documentId, byte[] fileData, string newTitle)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string updateQuery;
                if (fileData != null && !string.IsNullOrEmpty(newTitle))
                {
                    updateQuery = "UPDATE documents SET file = @file, title = @title WHERE id = @id";
                }
                else if (fileData != null)
                {
                    updateQuery = "UPDATE documents SET file = @file WHERE id = @id";
                }
                else
                {
                    updateQuery = "UPDATE documents SET file = NULL WHERE id = @id";
                }

                using (var cmd = new NpgsqlCommand(updateQuery, connection))
                {
                    if (fileData != null)
                    {
                        cmd.Parameters.AddWithValue("@file", fileData);
                        if (!string.IsNullOrEmpty(newTitle))
                        {
                            cmd.Parameters.AddWithValue("@title", newTitle);
                        }
                    }
                    cmd.Parameters.AddWithValue("@id", documentId);
                    cmd.ExecuteNonQuery();
                }
            }

            LoadDocuments();
        }

        private void DeleteDocument(DataGridViewRow row)
        {
            if (row.Cells["id"].Value == null) return;

            if (!int.TryParse(row.Cells["id"].Value.ToString(), out int documentId)) return;

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string deleteQuery = "DELETE FROM documents WHERE id = @id";
                using (var cmd = new NpgsqlCommand(deleteQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@id", documentId);
                    cmd.ExecuteNonQuery();
                }
            }

            LoadDocuments();
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            dataGridView1.NotifyCurrentCellDirty(true);
            dataGridView1.EndEdit();
            dataGridView1.Rows.Add();
        }

        private void AddDocument(string title, byte[] fileData)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string insertQuery = "INSERT INTO documents (title, file) VALUES (@title, @file)";
                using (var cmd = new NpgsqlCommand(insertQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@title", title);
                    cmd.Parameters.AddWithValue("@file", fileData ?? (object)DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }

            LoadDocuments();
        }

        private void UpdateDocument(int id, string newTitle)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string updateQuery = "UPDATE documents SET title = @title WHERE id = @id";
                using (var cmd = new NpgsqlCommand(updateQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@title", newTitle);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (dataGridView1.IsCurrentCellInEditMode)
            {
                dataGridView1.EndEdit();
            }

            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.IsNewRow) continue;

                    var idCell = row.Cells["id"].Value;
                    var titleCell = row.Cells["title"].Value;
                    string title = titleCell?.ToString()?.Trim();

                    if (string.IsNullOrWhiteSpace(title))
                    {
                        continue;
                    }

                    if (idCell == null || idCell == DBNull.Value)
                    {
                        AddDocument(title, null);
                    }
                    else
                    {
                        int id;
                        if (int.TryParse(idCell.ToString(), out id))
                        {
                            UpdateDocument(id, title);
                        }
                    }
                }

                MessageBox.Show("Все изменения сохранены.", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDocuments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении изменений: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                button3.PerformClick();
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

        private void Form6_FormClosing(object sender, FormClosingEventArgs e)
        {
            parentForm.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string searchTerm = textBox1.Text;
            LoadDocuments(searchTerm == "Введите данные для поиска" ? "" : searchTerm);
        }
    }
}