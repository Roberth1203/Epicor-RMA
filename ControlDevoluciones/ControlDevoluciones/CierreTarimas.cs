using System;
using System.Data;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using System.Configuration;
using Utilities;
using System.Text.RegularExpressions;

namespace ControlDevoluciones
{
    public partial class CierreTarimas : DevComponents.DotNetBar.Metro.MetroForm
    {
        SQLUtilities sql;
        public string folioGral;
        public string epiUser;
        string TISERVER = ConfigurationManager.AppSettings["connRMADB"].ToString();

        public CierreTarimas()
        {
            InitializeComponent();
        }

        private void getPallet()
        {
            sql = new SQLUtilities();
            DataTable dt = sql.getRecords(String.Format("SELECT Folio,Ubicacion FROM tb_Tarimas WHERE FolioRMA = '{0}';",folioGral),null,TISERVER);

            dgvTarimas.DataSource = dt;
        }

        private void CierreTarimas_Load(object sender, EventArgs e)
        {
            getPallet();
        }

        private void chkTodo_CheckedChanged(object sender, EventArgs e)
        {
            int index = 0;
            if (chkTodo.Checked)
            {
                foreach (DataGridViewRow row in dgvTarimas.Rows)
                {
                    dgvTarimas.Rows[index].Cells[0].Value = true;
                    index++;
                }   
            }
            else
            {
                foreach (DataGridViewRow row in dgvTarimas.Rows)
                {
                    dgvTarimas.Rows[index].Cells[0].Value = false;
                    index++;
                }
            }
        }

        private void btnCorteTarima_Click(object sender, EventArgs e)
        {
            int ind = 0;
            sql = new SQLUtilities();
            string newID;
            foreach(DataGridViewRow row in dgvTarimas.Rows)
            {
                if (Convert.ToBoolean(dgvTarimas.Rows[ind].Cells[0].Value) == true)
                {
                    string foo = dgvTarimas.Rows[ind].Cells[1].Value.ToString(); // Folio a cerrar

                    int increment = Convert.ToInt32(Regex.Replace(dgvTarimas.Rows[ind].Cells[1].Value.ToString().Substring(foo.Length - 3), @"[^\d]", "", RegexOptions.None, TimeSpan.FromSeconds(1.5))) + 1;
                    
                    if (increment > 10)
                        newID = foo.Substring(0, foo.Length - 2) + increment;
                    else
                        newID = foo.Substring(0, foo.Length - 1) + increment;

                    sql.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}');", foo, folioGral, epiUser, "GETDATE()", dgvTarimas.Rows[ind].Cells[2].Value.ToString()), TISERVER);
                    sql.SQLstatement(String.Format("UPDATE tb_Tarimas SET Folio = '{0}' WHERE FolioRMA = '{1}' AND Ubicacion = '{2}'", newID, folioGral, dgvTarimas.Rows[ind].Cells[2].Value.ToString()), TISERVER);
                }
                ind++;
            }

            this.Close();
        }
    }
}