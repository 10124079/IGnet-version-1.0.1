using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IGnet_NLADuc_10124079_12524T1
{
    public partial class frmService : Form
    {
        int pcId;
        int sessionId;
        DBHelper db = new DBHelper();
        public frmService(int pcId)
        {
            InitializeComponent();
            this.pcId = pcId;
        }

        private void frmService_Load(object sender, EventArgs e)
        {
            dgvService.DataSource = db.ExecuteQuery("SELECT * FROM Service");

            GetSession();
            LoadOrder();

        }
        void GetSession()
        {
            string query = $@"
        SELECT TOP 1 SessionID
        FROM Session
        WHERE ComputerID = {pcId} AND EndTime IS NULL";

            var dt = db.ExecuteQuery(query);

            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("Máy chưa mở!");
                this.Close();
                return;
            }

            sessionId = Convert.ToInt32(dt.Rows[0][0]);
        }
        void LoadOrder()
        {
            dgvOrder.Rows.Clear();

            var dt = db.ExecuteQuery($@"
                SELECT su.ServiceID, s.ServiceName, s.Price, su.Quantity, su.TotalAmount
                FROM ServiceUsage su
                JOIN Service s ON su.ServiceID = s.ServiceID
                WHERE su.SessionID = {sessionId}");

            foreach (DataRow row in dt.Rows)
            {
                dgvOrder.Rows.Add(
                    row["ServiceID"],
                    row["ServiceName"],
                    row["Price"],
                    row["Quantity"],
                    row["TotalAmount"]
                );
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (dgvService.CurrentRow == null) return;

            int serviceId = Convert.ToInt32(dgvService.CurrentRow.Cells[0].Value);
            string serviceName = dgvService.CurrentRow.Cells[1].Value.ToString();
            double price = Convert.ToDouble(dgvService.CurrentRow.Cells[2].Value);
            int qty = (int)numQty.Value;

            double total = price * qty;

            dgvOrder.Rows.Add(serviceId, serviceName, price, qty, total);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvOrder.Rows)
            {
                if (row.IsNewRow) continue;

                int serviceId = Convert.ToInt32(row.Cells[0].Value);
                int qty = Convert.ToInt32(row.Cells[3].Value);
                double total = Convert.ToDouble(row.Cells[4].Value);

                db.ExecuteNonQuery($@"
            INSERT INTO ServiceUsage (SessionID, ServiceID, Quantity, TotalAmount)
            VALUES ({sessionId}, {serviceId}, {qty}, {total})");
            }

            MessageBox.Show("Đã lưu!");
            this.Close();
        }

        private void dgvService_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            btnAdd.PerformClick();
        }
    }
}
