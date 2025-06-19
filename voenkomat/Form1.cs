using Npgsql; // ���������� ���������� ��� ������ � PostgreSQL

namespace voenkomat
{
    public partial class Form1 : Form
    {
        string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=admin;Database=voenkomat"; // �������� �� ���� ������

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetPlaceholderText();

            textBox1.Enter += textBox1_Enter;
            textBox1.Leave += textBox1_Leave;
            textBox1.KeyDown += textBox1_KeyDown;

            textBox2.Enter += textBox2_Enter;
            textBox2.Leave += textBox2_Leave;
            textBox2.KeyDown += textBox2_KeyDown;

            // ������������� ����� �� ����� ��� ��������
            this.Focus(); // ������������� ����� �� �����
        }

        private void LoadUserCredentials()
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT login, password, role_id FROM users WHERE login = @login";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("login", textBox1.Text);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string passwordFromDb = reader.GetString(1);
                            if (textBox2.Text == passwordFromDb)
                            {
                                int roleId = reader.GetInt32(2);
                                OpenFormByRole(roleId);
                            }
                            else
                            {
                                MessageBox.Show("�������� ������.");
                                textBox2.Clear(); // ������� ��������� ������
                                textBox2.Focus(); // ������������� ����� �� ��������� ������
                            }
                        }
                        else
                        {
                            MessageBox.Show("����� ������������ �� ������.");
                        }
                    }
                }
            }
        }


        private void OpenFormByRole(int roleId)
        {
            Form nextForm;
            if (roleId == 1) // admin
            {
                nextForm = new Form2(); // �������� �� ���� ����� ��� ������
            }
            else if (roleId == 2) // user
            {
                nextForm = new Form3(); // �������� �� ���� ����� ��� ������������
            }
            else
            {
                MessageBox.Show("����������� ����.");
                return;
            }

            nextForm.Show();
            this.Hide();
        }

        private void SetPlaceholderText()
        {
            textBox1.Text = "������� �����";
            textBox1.ForeColor = Color.Gray;

            textBox2.Text = "������� ������";
            textBox2.ForeColor = Color.Gray;
            textBox2.UseSystemPasswordChar = false;
        }

        private void textBox1_Enter(object sender, EventArgs e)
        {
            if (textBox1.Text == "������� �����")
            {
                textBox1.Text = "";
                textBox1.ForeColor = Color.Black;
            }
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                textBox1.Text = "������� �����";
                textBox1.ForeColor = Color.Gray;
            }
        }

        private void textBox2_Enter(object sender, EventArgs e)
        {
            if (textBox2.Text == "������� ������")
            {
                textBox2.Text = "";
                textBox2.ForeColor = Color.Black;
                textBox2.UseSystemPasswordChar = true;
            }
        }

        private void textBox2_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox2.Text))
            {
                textBox2.UseSystemPasswordChar = false;
                textBox2.Text = "������� ������";
                textBox2.ForeColor = Color.Gray;
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                textBox2.Focus();
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                button1_Click(this, EventArgs.Empty);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // ���������, ��������� �� ��� ���������� � �� �������� �� ��� �����-�����������
            if (string.IsNullOrWhiteSpace(textBox1.Text) || textBox1.Text == "������� �����" ||
                string.IsNullOrWhiteSpace(textBox2.Text) || textBox2.Text == "������� ������")
            {
                MessageBox.Show("����� � ������ ������ ���� ���������.", "������", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // ������� �� ������, ���� ���� �� ���������
            }

            LoadUserCredentials();
        }
    }
}
