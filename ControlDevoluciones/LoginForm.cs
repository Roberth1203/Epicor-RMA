using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using System.Text.RegularExpressions;
using EpicorAdapters;
using Ice.Core;
using System.Configuration;
using System.Threading.Tasks;
using Utilities;
using MetroFramework.Forms;

namespace ControlDevoluciones
{
    public partial class LoginForm : DevComponents.DotNetBar.Metro.MetroForm
    {
        bool allowed = false;
        string catcher;
        String userName;
        String password;
        string eFullName;
        string eCompanyName;
        string eProduct;
        string ePC;
        String environment;
        String epiCompany = ConfigurationManager.AppSettings["epiCompany"].ToString();
        String TISERVER = ConfigurationManager.AppSettings["connRMADB"].ToString();
        List<string> lista = new List<string>();
        SQLUtilities sql = new SQLUtilities();

        public LoginForm()
        {
            InitializeComponent();

            if (ConfigurationManager.AppSettings["epiEnvironment"].ToString().Contains("TEST"))
                this.TitleText = "Devoluciones MAC TEST";
            else
                this.TitleText = "Devoluciones MAC";

            appVersion.Text = "v2.0.1";
        }

        private async void btnStartSession_Click(object sender, EventArgs e)
        {
            userName = txtUser.Text;
            password = txtPass.Text;
            cargando.Visible = true;
            await Task.Factory.StartNew(async () =>
            {
                await validacion(userName,password);
            }).Unwrap();

            if (!catcher.Equals(""))
                ToastNotification.Show(this, catcher, 2000);

            if (allowed == false)
            {
                txtUser.Text = "";
                txtPass.Text = "";
                txtUser.Focus();
            }
            else
            {
                PantallaPrincipal form = new PantallaPrincipal();
                form.Show();
                this.Hide();
                
                //Asignación de datos del usuario para mostrar en barra de estado
                form.lblUserName.Text = eFullName;
                form.lbCompanyName.Text = eCompanyName;
                form.lbProductName.Text = eProduct;
                form.epiWorkstation = ePC;
                asignarFolioTurno(userName, ePC);
                form.idSesion = lista;

                if (userName == "rarroyo" || userName == "RARROYO")
                    form.tabpreviewRMA.Visible = true;

                form.epiUser = userName;
                form.epiPass = password;
            }
            cargando.Visible = false;
        }

        private void LoginForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private async Task validacion(string eUser, string ePass)
        {
            Regex patron = new Regex(@"[a-zA-ZñÑ\s]");

            if (eUser == "" || ePass == "")
                MessageBox.Show("Los campos Usuario o Contraseña no pueden ser vacíos !!", "Error de Inicio de Sesión", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                if (patron.IsMatch(eUser))
                {
                    try
                    {
                        catcher = String.Empty;
                        environment = String.Format(ConfigurationManager.AppSettings["epiEnvironment"].ToString(), "Epicor10");
                        Session epiSession = new Session(eUser, ePass, Session.LicenseType.Default, environment);
                        if (epiSession != null)
                        {
                            //userName = txtUser.Text;
                            //password = txtPass.Text;
                            EpiFunctions epicor = new EpiFunctions(eUser, ePass);
                            epicor.setCompany(epiCompany);
                            //PantallaPrincipal form = new PantallaPrincipal();
                            //form.Show();
                            //this.Hide();
                            eFullName = epiSession.UserName;
                            eCompanyName = epiSession.CompanyName;
                            eProduct = epiSession.ProductName + epiSession.ServerBaseLevelApp.Major;
                            ePC = epiSession.TaskClientID;
                            //Asignación de datos del usuario para mostrar en barra de estado
                            /*
                            form.lblUserName.Text = epiSession.UserName;
                            form.lbCompanyName.Text = epiSession.CompanyName;
                            form.lbProductName.Text = epiSession.ProductName + epiSession.ServerBaseLevelApp.Major;
                            form.epiWorkstation = epiSession.TaskClientID;
                            asignarFolioTurno(epiSession.UserID,epiSession.TaskClientID);
                            form.idSesion = lista;
                            

                            if (epiSession.SecurityManager)
                                form.tabpreviewRMA.Visible = true;

                            epiSession.Dispose();
                            epiSession = null;

                            form.epiUser = eUser;
                            form.epiPass = ePass;
                            */
                        }
                        allowed = true;
                    }
                    catch (System.UnauthorizedAccessException ex)
                    {
                        //MetroFramework.MetroMessageBox.Show(this,"Error de inicio de sesión: " + ex.Message,"Epicor Server Side Exception",MessageBoxButtons.OK,MessageBoxIcon.Error);
                        eUser = String.Empty;
                        ePass = String.Empty;
                        catcher = ex.Message;
                    }

                    catch (Exception s)
                    {
                        catcher = s.Message;
                        //MetroFramework.MetroMessageBox.Show(this,s.Message,"Error al iniciar sesión",MessageBoxButtons.OK,MessageBoxIcon.Error);
                        eUser = String.Empty;
                        ePass = String.Empty;
                    }
                }
            }       
                
        }
        /*
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {

        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressLogin.Value = e.ProgressPercentage;
            progressLogin.Text = e.ProgressPercentage + "%";
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressLogin.Value = 0;
            progressLogin.Visible = false;
            progressLogin.ProgressTextVisible = false;
            //validacion(userName,password);
        }
        */

        private async void txtPass_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((int)e.KeyChar == (int)Keys.Enter)
            {
                //backgroundWorker1.RunWorkerAsync();
                //progressLogin.Visible = true;
                userName = txtUser.Text;
                password = txtPass.Text;
                cargando.Visible = true;
                await Task.Factory.StartNew(async () =>
                {
                    await validacion(userName, password);
                }).Unwrap();

                if (!catcher.Equals(""))
                    ToastNotification.Show(this, catcher, 2000);

                if (allowed == false)
                {
                    txtUser.Text = "";
                    txtPass.Text = "";
                    txtUser.Focus();
                }
                else
                {
                    PantallaPrincipal form = new PantallaPrincipal();
                    form.Show();
                    this.Hide();

                    //Asignación de datos del usuario para mostrar en barra de estado
                    form.lblUserName.Text = eFullName;
                    form.lbCompanyName.Text = eCompanyName;
                    form.lbProductName.Text = eProduct;
                    form.epiWorkstation = ePC;
                    asignarFolioTurno(userName, ePC);
                    form.idSesion = lista;

                    if (userName == "rarroyo" || userName == "RARROYO")
                    {
                        form.tabpreviewRMA.Visible = true;
                        form.tabAccionesAd.Visible = true;
                    }
                    form.epiUser = userName;
                    form.epiPass = password;
                }
                cargando.Visible = false;
            }
        }

        private void asignarFolioTurno(string userName,string pc)
        {
            try
            {
                string fecha = DateTime.Now.ToString("yyyyMMdd") + " 00:00:00";
                DataTable dt = sql.getRecords(String.Format("SELECT Id,Usuario,Terminal,FolioActivo,FechaInicio,Abierto FROM tb_Sesiones WHERE FechaInicio BETWEEN '{0}' AND GETDATE() AND Usuario = '{1}' AND Abierto = 1;", fecha,userName),null,TISERVER);

                if (dt.Rows.Count > 0)
                {
                    lista.Add(dt.Rows[dt.Rows.Count - 1].ItemArray[0].ToString());
                    lista.Add(dt.Rows[dt.Rows.Count - 1].ItemArray[3].ToString());
                    lista.Add(dt.Rows[dt.Rows.Count - 1].ItemArray[5].ToString());
                    
                }
                else
                {
                    sql.SQLstatement(String.Format("INSERT INTO dbo.tb_Sesiones(Usuario,Terminal,FolioActivo,FechaInicio,Abierto) VALUES('{0}','{1}','{2}',{3},{4});", userName, pc, "S/P", "GETDATE()","1"), TISERVER);
                    DataTable dt2 = sql.getRecords(String.Format("SELECT Id, Usuario, Terminal, FolioActivo, FechaInicio, Abierto FROM tb_Sesiones WHERE FechaInicio BETWEEN '{0}' AND GETDATE() AND Usuario = '{1}' AND Abierto = 1; ", fecha,userName),null,TISERVER);
                    lista.Add(dt2.Rows[dt2.Rows.Count - 1].ItemArray[0].ToString());
                    lista.Add(dt2.Rows[dt2.Rows.Count - 1].ItemArray[3].ToString());
                    lista.Add(dt2.Rows[dt2.Rows.Count - 1].ItemArray[5].ToString());
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void txtUser_Leave(object sender, EventArgs e)
        {
            txtUser.Text = txtUser.Text.ToUpper();
        }
    }
}