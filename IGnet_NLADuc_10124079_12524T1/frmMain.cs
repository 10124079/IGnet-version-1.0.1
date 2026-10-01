using CrystalDecisions.CrystalReports.Engine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.AccessControl;
using System.Windows.Forms;

namespace IGnet_NLADuc_10124079_12524T1
{
    public partial class frmMain : Form
    {
        string username;
        int role;

        DBHelper db = new DBHelper();
        public frmMain(string username, int role)
        {
            InitializeComponent();
            this.username = "Người dùng: " + username;
            this.role = role;
        }
        //Load
        void LoadComputer()
        {
            DataTable dt = db.ExecuteQuery(@"
                SELECT c.ComputerID,
                       CASE WHEN s.SessionID IS NULL THEN 'Free' ELSE 'Using' END AS Status
                FROM Computer c
                LEFT JOIN Session s 
                ON c.ComputerID = s.ComputerID AND s.EndTime IS NULL
            ");

            foreach (Control c in flowPC.Controls)
            {
                Button btn = c as Button;
                if (btn == null) continue;

                int id = int.Parse(btn.Tag.ToString());

                DataRow[] rows = dt.Select("ComputerID = " + id);

                if (rows.Length > 0)
                {
                    string status = rows[0]["Status"].ToString();

                    if (status == "Using")
                        btn.BackColor = Color.LightGreen;
                    else
                        btn.BackColor = Color.White;

                    btn.Text = "PC " + id;
                }
            }
        }
        void LoadCustomer()
        {
            dgvCustomer.DataSource = db.ExecuteQuery(
                "SELECT CustomerID, CustomerName, Phone, Balance FROM Customer"
            );
        }
        void LoadHistory(string keyword = "")
        {
            string query = $@"
SELECT 
    c.ComputerName AS [Máy],
    ISNULL(cus.CustomerName, N'Khách lẻ') AS [Tài khoản],
    s.StartTime AS [Bắt đầu],
    s.EndTime AS [Kết thúc],
    s.GrandTotal AS [Tổng tiền]
FROM Session s
JOIN Computer c 
    ON s.ComputerID = c.ComputerID

LEFT JOIN Customer cus
    ON s.CustomerID = cus.CustomerID

WHERE s.EndTime IS NOT NULL
AND c.ComputerName LIKE N'%{keyword}%'

ORDER BY s.SessionID DESC";

            dgvHistory.DataSource = db.ExecuteQuery(query);

            dgvHistory.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;

            dgvHistory.Columns["Tổng tiền"]
                .DefaultCellStyle.Format = "N0";
        }
        void LoadCombo()
        {
            dgvCombo.DataSource = db.ExecuteQuery(
                    @"SELECT 
            ComboID AS[ID],
            ComboName AS[Combo],
            Hours AS[Giờ],
            Price AS[Giá],
            Description AS[Mô tả],
            StartHour AS[Bắt đầu],
            EndHour AS[Kết thúc]
                    FROM TimeCombo");

            dgvCombo.Columns["Giá"].DefaultCellStyle.Format = "N0";
            dgvCombo.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        }
        void LoadUser()
        {
            string query = @"
    SELECT 
        u.UserID AS [ID],
        u.Username AS [Tên đăng nhập],
        r.RoleName AS [Vai trò]
    FROM [User] u
    JOIN Role r ON u.RoleID = r.RoleID";
            
            dgvUserht.DataSource = db.ExecuteQuery(query);
            dgvUserht.Columns["ID"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        }
        void LoadRevenueReport(string condition = "")
        {
            string query = $@"
SELECT 
    c.ComputerName,
    cus.CustomerName,
    s.StartTime,
    s.EndTime,
    s.TotalTime,

    ISNULL((
        SELECT SUM(TotalAmount)
        FROM ServiceUsage su
        WHERE su.SessionID = s.SessionID
    ),0) AS ServiceMoney,

    CASE
        WHEN s.GrandTotal IS NULL 
            Then b.TotalAmount
        Else s.GrandTotal
    End AS TotalAmount

FROM Session s

JOIN Computer c
    ON s.ComputerID = c.ComputerID
LEFT JOIN Bill b
    ON s.SessionID = b.SessionID    

LEFT JOIN Customer cus
    ON s.CustomerID = cus.CustomerID

WHERE s.EndTime IS NOT NULL
{condition}

ORDER BY s.SessionID DESC";

            DataTable dt = db.ExecuteQuery(query);

            rptRevenue rpt = new rptRevenue();

            rpt.SetDataSource(dt);

            crystalReportViewer1.ReportSource = rpt;

            crystalReportViewer1.Refresh();
        }
        //Main load
        private void frmMain_Load(object sender, EventArgs e)
        {
            // TODO: This line of code loads data into the 'qLIG_NLADucDataSet.Role' table. You can move, or remove it, as needed.
            this.roleTableAdapter.Fill(this.qLIG_NLADucDataSet.Role);
            // gán tag và sự kiện click cho button PC
            int i = 1;
            foreach (Control c in flowPC.Controls)
            {
                Button btn = c as Button;
                if (btn != null)
                {
                    btn.Tag = i;
                    btn.Text = "PC " + i;
                    btn.Click += Btn_Click;
                    i++;
                }
            }
            // hiển thị thông tin người dùng
            lblRole.Text = role == 1 ? "Admin" : "Staff";
            lblUser.Text = username;
            // load dữ liệu
            LoadComputer();
            LoadCustomer();
            LoadHistory();
            LoadCombo();
            LoadUser();
            LoadRevenueReport();

            timer1.Interval = 1000;
            timer1.Start();
            // load role vào combobox trong quản lý tài khoản
            cboRoleht.Items.Add("Admin");
            cboRoleht.Items.Add("Nhân viên");

            if (role == 2)
            {
                groupBox1.Enabled = false;
                groupBox3.Enabled = false;
                tabPage5.Enabled = false;
            }
            // load máy vào combobox trong cấu hình giá
            var dt = db.ExecuteQuery(
    "SELECT ComputerName FROM Computer");

            cboFromPC.DataSource = dt.Copy();
            cboFromPC.DisplayMember = "ComputerName";

            cboToPC.DataSource = dt;
            cboToPC.DisplayMember = "ComputerName";
        }
        //Trang chủ
        private void Btn_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            int pcId = int.Parse(btn.Tag.ToString());

            ContextMenuStrip menu = new ContextMenuStrip();

            menu.Items.Add("Mở máy", null, (s, ev) => StartPC(pcId));
            menu.Items.Add("Tính tiền", null, (s, ev) => StopPC(pcId));
            menu.Items.Add("Dịch vụ", null, (s, ev) =>
            {
                frmService f = new frmService(pcId);
                f.ShowDialog();
            });

            menu.Show(btn, new Point(0, btn.Height));
        }
        void StartPC(int pcId)
        {
            string check = $@"
                SELECT * FROM Session
                WHERE ComputerID = {pcId} AND EndTime IS NULL";

            var dt = db.ExecuteQuery(check);

            if (dt.Rows.Count > 0)
            {
                MessageBox.Show("Máy đang được sử dụng!");
                return;
            }

            string query = $@"
                INSERT INTO Session (ComputerID, StartTime)
                VALUES ({pcId}, GETDATE())";

            db.ExecuteNonQuery(query);

            LoadComputer();
        }
        void StopPC(int pcId)
        {
            var dt = db.ExecuteQuery($@"
    SELECT TOP 1 *
    FROM Session
    WHERE ComputerID = {pcId} AND EndTime IS NULL
    ORDER BY StartTime DESC");

            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("Máy chưa mở!");
                return;
            }

            int sessionId =
                Convert.ToInt32(dt.Rows[0]["SessionID"]);

            DateTime start =
                Convert.ToDateTime(dt.Rows[0]["StartTime"]);

            DateTime end = DateTime.Now;

            double hours = (end - start).TotalHours;

            // tiền máy
            var priceDt = db.ExecuteQuery($@"
    SELECT PricePerHour
    FROM Computer
    WHERE ComputerID = {pcId}");

            double price =
                Convert.ToDouble(priceDt.Rows[0][0]);

            double moneyPC = hours * price;

            // tiền dịch vụ
            var serviceDt = db.ExecuteQuery($@"
    SELECT ISNULL(SUM(TotalAmount),0)
    FROM ServiceUsage
    WHERE SessionID = {sessionId}");

            double moneyService =
                Convert.ToDouble(serviceDt.Rows[0][0]);

            // tổng tiền
            double total = moneyPC + moneyService;

            // update session
            db.ExecuteNonQuery($@"
    UPDATE Session
    SET EndTime = GETDATE(),
        TotalTime = {hours},
        GrandTotal = {total}
    WHERE SessionID = {sessionId}");

            MessageBox.Show(
                $"Tiền máy: {moneyPC:N0} VNĐ\n" +
                $"Tiền dịch vụ: {moneyService:N0} VNĐ\n" +
                $"Tổng tiền: {total:N0} VNĐ"
            );

            LoadComputer();
            LoadHistory();
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            string query = @"
                SELECT ComputerID, StartTime
                FROM Session
                WHERE EndTime IS NULL";

            var dt = db.ExecuteQuery(query);

            foreach (DataRow row in dt.Rows)
            {
                int id = Convert.ToInt32(row["ComputerID"]);
                DateTime start = Convert.ToDateTime(row["StartTime"]);

                TimeSpan time = DateTime.Now - start;

                foreach (Control c in flowPC.Controls)
                {
                    Button btn = c as Button;
                    if (btn == null) continue;

                    if (btn.Tag != null && int.Parse(btn.Tag.ToString()) == id)
                    {
                        btn.Text = $"PC {id}\n{time:hh\\:mm\\:ss}";
                    }
                }
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            frmLogin login = new frmLogin();
            login.Show();
            this.Hide();
        }

        private void btnOut_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Application.Exit();
        }
        //Tài khoản & nạp tiền
        private void dgvCustomer_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dgvCustomer.Rows[e.RowIndex];

            txtName.Text = row.Cells[1].Value.ToString();
            txtPhone.Text = row.Cells[2].Value.ToString();
            txtBalance.Text = row.Cells[3].Value.ToString();
        }
        
        private void btnAdd_Click(object sender, EventArgs e)
        {
            db.ExecuteNonQuery($@"
        INSERT INTO Customer (CustomerName, Phone, Balance)
        VALUES (N'{txtName.Text}', '{txtPhone.Text}', 0)
                 ");
            txtName.Clear();
            txtPhone.Clear();
            txtBalance.Clear();

            LoadCustomer();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (dgvCustomer.CurrentRow == null) return;

            int id = Convert.ToInt32(dgvCustomer.CurrentRow.Cells[0].Value);

            db.ExecuteNonQuery($@"
        UPDATE Customer
        SET CustomerName = N'{txtName.Text}',
            Phone = '{txtPhone.Text}',
            Balance = '{txtBalance.Text}'
        WHERE CustomerID = {id}
    ");
            txtName.Clear();
            txtPhone.Clear();
            txtBalance.Clear();

            LoadCustomer();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dgvCustomer.CurrentRow == null) return;

            int id = Convert.ToInt32(dgvCustomer.CurrentRow.Cells[0].Value);

            db.ExecuteNonQuery($"DELETE FROM Session WHERE CustomerID = {id}");

            db.ExecuteNonQuery($"DELETE FROM Customer WHERE CustomerID = {id}");

            txtName.Clear();
            txtPhone.Clear();
            txtBalance.Clear();

            LoadCustomer();
        }

        private void btnFind_Click(object sender, EventArgs e)
        {
            string keyword = txtSearch.Text;

            dgvCustomer.DataSource = db.ExecuteQuery($@"
        SELECT CustomerID, CustomerName, Phone, Balance
        FROM Customer
        WHERE CustomerName LIKE N'%{keyword}%'
           OR Phone LIKE '%{keyword}%'
                                        ");
            txtSearch.Clear();
        }
        private void btnMoney_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;

            int current = 0;
            int.TryParse(txtDeposit.Text, out current);

            int add = Convert.ToInt32(btn.Tag);

            txtDeposit.Text = (current + add).ToString();
        }

        private void btnDeposit_Click(object sender, EventArgs e)
        {
            if (dgvCustomer.CurrentRow == null) return;

            int id = Convert.ToInt32(dgvCustomer.CurrentRow.Cells[0].Value);
            double money;

            if (!double.TryParse(txtDeposit.Text, out money))
            {
                MessageBox.Show("Tiền không hợp lệ");
                return;
            }

            db.ExecuteNonQuery($@"
        UPDATE Customer
        SET Balance = Balance + {money}
        WHERE CustomerID = {id}
                                ");
            txtName.Clear();
            txtPhone.Clear();
            txtBalance.Clear();
            txtDeposit.Clear();

            LoadCustomer();
        }
        //Lsu giao dịch
        private void dgvHistory_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvHistory.Columns[e.ColumnIndex].HeaderText == "Tổng tiền"&& e.Value != null)
            {
                e.Value = string.Format("{0:N0} đ", e.Value);
            }
        }

        private void btnFind2_Click(object sender, EventArgs e)
        {
            string keyword = txtFind2.Text.Trim();

            LoadHistory(txtFind2.Text.Trim());
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            txtFind2.Clear();
            LoadHistory();
        }
        //Hệ thống Combo
        private void dgvCombo_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvCombo.CurrentRow == null) return;

            txtComboName.Text =
                dgvCombo.CurrentRow.Cells["Combo"].Value.ToString();

            numHours.Value =
                Convert.ToDecimal(
                    dgvCombo.CurrentRow.Cells["Giờ"].Value);

            txtComboPrice.Text =
                dgvCombo.CurrentRow.Cells["Giá"].Value.ToString();

            txtDescription.Text =
                dgvCombo.CurrentRow.Cells["Mô tả"].Value.ToString();
        }

        private void btnAddCombo_Click(object sender, EventArgs e)
        {
            db.ExecuteNonQuery($@"
    INSERT INTO TimeCombo
    (
        ComboName,
        Hours,
        Price,
        Description
    )
    VALUES
    (
        N'{txtComboName.Text}',
        {numHours.Value},
        {txtComboPrice.Text},
        N'{txtDescription.Text}'
    )");

            MessageBox.Show("Đã thêm!");

            LoadCombo();
        }

        private void btnEditCombo_Click(object sender, EventArgs e)
        {
            if (dgvCombo.CurrentRow == null) return;

            int id =
                Convert.ToInt32(
                    dgvCombo.CurrentRow.Cells["ComboID"]
                    .Value);

            db.ExecuteNonQuery($@"
    UPDATE TimeCombo
    SET
        ComboName = N'{txtComboName.Text}',
        Hours = {numHours.Value},
        Price = {txtComboPrice.Text},
        Description = N'{txtDescription.Text}'
    WHERE ComboID = {id}");

            MessageBox.Show("Đã sửa!");

            LoadCombo();
        }

        private void btnDeleteCombo_Click(object sender, EventArgs e)
        {
            if (dgvCombo.CurrentRow == null) return;

            int id =
                Convert.ToInt32(
                    dgvCombo.CurrentRow.Cells["ComboID"]
                    .Value);

            db.ExecuteNonQuery($@"
    DELETE FROM TimeCombo
    WHERE ComboID = {id}");

            MessageBox.Show("Đã xoá!");

            LoadCombo();
        }

        private void btnSellCombo_Click(object sender, EventArgs e)
        {
            if (dgvCombo.CurrentRow == null) return;

            string comboName =
                dgvCombo.CurrentRow.Cells["Combo"]
                .Value.ToString();

            int startHour =
                Convert.ToInt32(
                    dgvCombo.CurrentRow.Cells["Bắt đầu"]
                    .Value);

            int endHour =
                Convert.ToInt32(
                    dgvCombo.CurrentRow.Cells["Kết thúc"]
                    .Value);

            double price =
                Convert.ToDouble(
                    dgvCombo.CurrentRow.Cells["Giá"]
                    .Value);

            Random rd = new Random();

            string username =
                "CB" + rd.Next(1000, 9999);

            string password =
                rd.Next(100000, 999999).ToString();

            DateTime start =
                DateTime.Today.AddHours(startHour);

            DateTime end;

            if (endHour < startHour)
            {
                end = DateTime.Today
                    .AddDays(1)
                    .AddHours(endHour);
            }
            else
            {
                end = DateTime.Today
                    .AddHours(endHour);
            }

            // lưu customer
            db.ExecuteNonQuery($@"
    INSERT INTO Customer
    (
        CustomerName,
        Phone,
        Balance,
        Password
    )
    VALUES
    (
        N'{username}',
        '',
        0,
        N'{password}'
    )");

            // lấy customer mới tạo
            var dt = db.ExecuteQuery(@"
    SELECT TOP 1 CustomerID
    FROM Customer
    ORDER BY CustomerID DESC");

            int customerId =
                Convert.ToInt32(dt.Rows[0][0]);

            // lưu session
            db.ExecuteNonQuery($@"
    INSERT INTO Session
    (
        ComputerID,
        CustomerID,
        StartTime,
        EndTime,
        TotalTime,
        GrandTotal
    )
    VALUES
    (
        1,
        {customerId},
        '{start:yyyy-MM-dd HH:mm:ss}',
        '{end:yyyy-MM-dd HH:mm:ss}',
        {(end - start).TotalHours},
        {price}
    )");

            MessageBox.Show(
                $"COMBO: {comboName}\n\n" +

                $"Tài khoản: {username}\n" +
                $"Mật khẩu: {password}\n\n" +

                $"Bắt đầu: {start}\n" +
                $"Kết thúc: {end}"
            );

            LoadHistory();
        }
        //Hệ thống QLtaikhoan
        private void dgvUserht_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvUserht.CurrentRow == null) return;

            txtUserht.Text =
                dgvUserht.CurrentRow.Cells["Username"].Value.ToString();

            cboRoleht.Text =
                dgvUserht.CurrentRow.Cells["RoleName"].Value.ToString();
        }

        private void btnAddht_Click(object sender, EventArgs e)
        {
            int roleId = cboRoleht.Text == "Admin" ? 1 : 2;

            string query = $@"
    INSERT INTO [User]
    (Username, Password, RoleID)
    VALUES
    (
        N'{txtUserht.Text}',
        N'{txtPassht.Text}',
        {roleId}
    )";

            db.ExecuteNonQuery(query);

            MessageBox.Show("Đã thêm!");

            LoadUser();
        }

        private void btnEditht_Click(object sender, EventArgs e)
        {
            if (dgvUserht.CurrentRow == null) return;

            int id = Convert.ToInt32(
                dgvUserht.CurrentRow.Cells["UserID"].Value);

            int roleId = cboRoleht.Text == "Admin" ? 1 : 2;

            string query = $@"
    UPDATE [User]
    SET
        Username = N'{txtUserht.Text}',
        Password = N'{txtPassht.Text}',
        RoleID = {roleId}
    WHERE UserID = {id}";

            db.ExecuteNonQuery(query);

            MessageBox.Show("Đã sửa!");

            LoadUser();
        }

        private void btnDeleteht_Click(object sender, EventArgs e)
        {
            if (dgvUserht.CurrentRow == null) return;

            int id = Convert.ToInt32(
                dgvUserht.CurrentRow.Cells["UserID"].Value);

            db.ExecuteNonQuery($@"
    DELETE FROM [User]
    WHERE UserID = {id}");

            MessageBox.Show("Đã xoá!");

            LoadUser();
        }
        //Hệ thống cấu hình giá
        private void btnSavePrice_Click(object sender, EventArgs e)
        {
            string from =
        cboFromPC.Text.Replace("PC", "");

            string to =
                cboToPC.Text.Replace("PC", "");

            int fromId = int.Parse(from);
            int toId = int.Parse(to);

            double price = (double)numPrice.Value;

            string query = $@"
    UPDATE Computer
    SET PricePerHour = {price}
    WHERE ComputerID
    BETWEEN {fromId} AND {toId}";

            db.ExecuteNonQuery(query);

            MessageBox.Show("Đã cập nhật giá!");
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            string date =
        dtpDate.Value.ToString("yyyy-MM-dd");

            string condition =
                $"AND CAST(s.EndTime AS DATE) = '{date}'";

            LoadRevenueReport(condition);
        }

        private void btnAll_Click(object sender, EventArgs e)
        {
            LoadRevenueReport();
        }

        private void btnResetdgv_Click(object sender, EventArgs e)
        {
            LoadCustomer();
        }
    }
}
