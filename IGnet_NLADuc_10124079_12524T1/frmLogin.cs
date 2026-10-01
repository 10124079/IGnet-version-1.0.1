using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IGnet_NLADuc_10124079_12524T1
{
    public partial class frmLogin : Form
    {
        string connStr = @"Data Source=ANHDUC\SQLEXPRESS;Initial Catalog=QLIG_NLADuc;Integrated Security=True;";
        public frmLogin()
        {
            InitializeComponent();
        }

        private void frmLogin_Load(object sender, EventArgs e)
        {

        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string user = txtUser.Text.Trim();
            string pass = txtPass.Text.Trim();

            if (user == "" || pass == "")
            {
                MessageBox.Show("Vui lòng nhập đầy đủ!");
                return;
            }

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                string query = @"SELECT Username, RoleID 
                         FROM [User] 
                         WHERE Username=@user AND Password=@pass AND Status=1";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@user", user);
                cmd.Parameters.AddWithValue("@pass", pass);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    int role = Convert.ToInt32(reader["RoleID"]);

                    frmMain f = new frmMain(user, role);
                    f.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("Sai tài khoản hoặc mật khẩu!");
                }
            }
        }

        private void txtPass_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnLogin.PerformClick();
            }
        }

        private void btnOut_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Application.Exit();
        }

        private void btnMore_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show(
        "Phần mềm quản lý quán nét\nVersion 1.0\nDev by AnhDuc\nContact: 0836133266",
        "Thông tin",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information
            );
        }
    }
}
