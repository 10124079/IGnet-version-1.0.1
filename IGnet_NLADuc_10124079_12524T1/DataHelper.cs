using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IGnet_NLADuc_10124079_12524T1
{
    public class DBHelper
    {
        string connStr = @"Data Source=ANHDUC\SQLEXPRESS;Initial Catalog=QLIG_NLADuc;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";

        public DataTable ExecuteQuery(string query)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public void ExecuteNonQuery(string query)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
