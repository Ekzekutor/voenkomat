namespace voenkomat
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form4 nextForm = new Form4(this);
            nextForm.Show();
            this.Hide(); // Скрываем Form2
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form5 nextForm = new Form5(this);
            nextForm.Show();
            this.Hide(); // Скрываем Form2
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form6 nextForm = new Form6(this);
            nextForm.Show();
            this.Hide(); // Скрываем Form2
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Form7 nextForm = new Form7(this);
            nextForm.Show();
            this.Hide(); // Скрываем Form2
        }
    }
}
