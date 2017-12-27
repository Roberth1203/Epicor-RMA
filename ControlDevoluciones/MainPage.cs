using DevComponents.DotNetBar;
using EpicorAdapters;
using MetroFramework.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utilities;

namespace ControlDevoluciones
{
    public partial class PantallaPrincipal : Form
    {
        FileManager file;
        SQLUtilities util = new SQLUtilities();
        Orchestador functions = new Orchestador();
        Config conf = new Config();
        DataTable facturas = new DataTable();
        DataTable dtResumenTarimas = new DataTable();
        string folderBase = ConfigurationManager.AppSettings["mainFolder"].ToString();
        string conMultistop = ConfigurationManager.AppSettings["connMultistop"].ToString();
        string conEpicor = ConfigurationManager.AppSettings["connEpicor"].ToString();
        string TISERVER = ConfigurationManager.AppSettings["connRMADB"].ToString();
        string recolectorEventos;
        public List<string> partesRMA = new List<string>();
        public List<String> ReasonsList = new List<string>();
        public List<int> invoiceProcList = new List<int>(); //Lista de facturas ya procesadas
        List<int> selectedRows = new List<int>(); //Lista de filas para facturas a procesar
        public List<string> idSesion = new List<string>();
        ImageList imgInvoiceTree = new ImageList();
        public string epiUser;
        public string epiPass;
        public string epiWorkstation;
        public int progreso = 0;
        public string folioGeneral;
        public string folioProceso;
        public string folioVIR;
        public string folioBES;
        public string folioT1;
        public string folioT2;
        public string folioT3;
        public string folioDEF;
        public string folioEDA;
        public string folioGAR;

        public PantallaPrincipal()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            lblUserName.Text = epiUser;
        }

        #region Eventos del form
        private void PantallaPrincipal_Load(object sender, EventArgs e)
        {
            //getDrivers();
            tmrLoader.Enabled = true;
            sincronizaFacturas();
            fillImageLists();
        }

        private async void tmrLoader_Tick(object sender, EventArgs e)
        {
            LoadInitData data = new LoadInitData();
            tmrLoader.Stop();
            data.Show();
            await Task.Factory.StartNew(async () =>
            {
                await fillAdvTree();
            }).Unwrap();
            data.Close();
            tmrLoader.Enabled = false;
        }


        private void PantallaPrincipal_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
        #endregion

        #region Eventos de botones
        private async void btnObtRelacion_Click(object sender, EventArgs e)
        {
            /*
            DataTable res = new DataTable();
            gifSearchInvc.Visible = true;
            sincronizaFacturas();

            txtFacturasProcesadas.Text += "Obteniendo facturas del chofer " + listaChoferes.SelectedItem.ToString() + "...\n";
            txtFacturasProcesadas.SelectionStart = txtFacturasProcesadas.TextLength;

            ToastNotification.DefaultToastPosition = eToastPosition.MiddleCenter;
            ToastNotification.Show(this, "Buscando facturas del chofer seleccionado", 3000);

            await Task.Factory.StartNew(async () =>
            {
                res = await functions.obtenerFacturas(listaChoferes.SelectedItem.ToString(), conEpicor);
            }).Unwrap();

            if (res.Rows.Count > 0)
            {
                dgvFacturas.DataSource = res;
                txtFacturasProcesadas.Text += "Facturas pendientes obtenidas correctamente. \n";
                txtFacturasProcesadas.SelectionStart = txtFacturasProcesadas.TextLength;
            }
            else
            {
                txtFacturasProcesadas.Text += "No se encontró ninguna factura para el chofer seleccionado.\n";
                txtFacturasProcesadas.SelectionStart = txtFacturasProcesadas.TextLength;
            }
            
            ToastNotification.DefaultToastPosition = eToastPosition.MiddleCenter;
            ToastNotification.Show(this, "Facturas cargadas correctamente", 2000);

            dgvFacturas.Visible = true;   
            gifSearchInvc.Visible = false;
            */
        }

        private async void btnRMA_Click(object sender, EventArgs e)
        {
            try
            {
                file = new FileManager();
                LoaderForm loader = new LoaderForm();
                loader.ShowDialog(); // Presentación de form Loader (Petición a epicor)
                obtFolioProceso(); // Obtener folio de turno y folios de tarimas
                disableButtons();
                panelAwaitAsync.Visible = true;

                DataTable dtLineasDevolucion = new DataTable();
                int ind = 0;
                foreach (DataGridViewRow row in dgvFacturas.Rows)
                {
                    if (Convert.ToBoolean(dgvFacturas.Rows[ind].Cells[0].Value) == true)
                    {
                        // Obtención del detallado de líneas para la devolución
                        selectedRows.Add(ind);
                        txtFacturasProcesadas.Text += "Obteniendo lineas de la factura  " + dgvFacturas.Rows[ind].Cells[1].Value.ToString() + "...\n";
                        txtFacturasProcesadas.SelectionStart = txtFacturasProcesadas.TextLength;
                        string facturaActual = dgvFacturas.Rows[ind].Cells[1].Value.ToString();
                        await Task.Factory.StartNew(async () =>
                        {
                            dtLineasDevolucion = await functions.getInvoiceDtl(ind, conEpicor, facturaActual);
                        }).Unwrap();

                        if (functions.catcher.Equals(""))
                        {
                            txtFacturasProcesadas.Text += "Se otuvieron las líneas a devolver.\n";
                            txtFacturasProcesadas.SelectionStart = txtFacturasProcesadas.TextLength;
                            dgvDetFactura.DataSource = dtLineasDevolucion;
                        }
                        else
                        {
                            txtFacturasProcesadas.Text += "Ocurrió un problema al obtener las líneas de la factura. " + functions.catcher + "\n";
                            txtFacturasProcesadas.SelectionStart = txtFacturasProcesadas.TextLength;
                        }

                        /* Depuración final de las filas para mostrar solo las correspondientes a la factura actual
                        int pos = 0;
                        foreach (DataGridViewRow w in dgvDetFactura.Rows)
                        {
                            if (!dgvDetFactura.Rows[pos].Cells[0].Value.ToString().Equals(facturaActual))
                                dgvDetFactura.Rows.Remove(w);

                            pos++;
                        }
                        */

                        string folioRelacion = dgvFacturas.Rows[ind].Cells[5].Value.ToString(); //se obtiene el número de relación para crear el archivo log
                        file.createLog(folioRelacion);
                        file.writeContentToFile("\n");
                        file.writeContentToFile("\nFactura en proceso actual " + dgvFacturas.Rows[ind].Cells[1].Value.ToString());

                        //Generación de RMA a partir del detallado
                        List<string> listDatosFactura = new List<string>();

                        listDatosFactura.Add(dgvFacturas.Rows[ind].Cells[1].Value.ToString()); // InvoiceNum
                        listDatosFactura.Add(dgvFacturas.Rows[ind].Cells[2].Value.ToString()); // Legal Number
                        listDatosFactura.Add(dgvFacturas.Rows[ind].Cells[3].Value.ToString()); // CustNum
                        listDatosFactura.Add(dgvFacturas.Rows[ind].Cells[4].Value.ToString()); // InvoiceLines
                        listDatosFactura.Add(dgvFacturas.Rows[ind].Cells[5].Value.ToString()); // Relathioship

                        txtFacturasProcesadas.Text += "Comienza la generación de RMA para la factura: " + listDatosFactura[0] + "\n";
                        txtFacturasProcesadas.SelectionStart = txtFacturasProcesadas.TextLength;
                        panelAwaitAsync.Visible = true;

                        char[] separadores = { ',', ' ' };

                        //MessageBox.Show("Lineas en dtLineasDevolucion: " + dtLineasDevolucion.Rows.Count);
                        
                        await Task.Factory.StartNew(async () =>
                        {
                            await generarRMA(listDatosFactura, dtLineasDevolucion, Convert.ToInt32(listDatosFactura[0]), listDatosFactura[1].ToString().Trim(separadores), Convert.ToInt32(Regex.Replace(listDatosFactura[2], @"[^\d]", "", RegexOptions.None, TimeSpan.FromSeconds(1.5))), listDatosFactura[4], idSesion[1]);
                        }).Unwrap();
                        
                        txtFacturasProcesadas.Text += recolectorEventos + "\n";
                        panelAwaitAsync.Visible = false;
                    }
                    ind++;
                }
                sincronizaFacturas();
                panelAwaitAsync.Visible = false;

                DataTable res = new DataTable();
                gifSearchInvc.Visible = true;
                sincronizaFacturas();

                txtFacturasProcesadas.Text += "Sincronizando las facturas actuales...\n";
                txtFacturasProcesadas.SelectionStart = txtFacturasProcesadas.TextLength;
                /*
                await Task.Factory.StartNew(async () =>
                {
                    res = await functions.obtenerFacturas(listaChoferes.SelectedItem.ToString(), conEpicor);
                }).Unwrap();
                */
                if (res.Rows.Count > 0)
                {
                    dgvFacturas.DataSource = res;
                    txtFacturasProcesadas.Text += "Facturas pendientes obtenidas correctamente. \n";
                    txtFacturasProcesadas.SelectionStart = txtFacturasProcesadas.TextLength;
                }
                else
                {
                    txtFacturasProcesadas.Text += "No se encontró ninguna factura para el chofer seleccionado.\n";
                    txtFacturasProcesadas.SelectionStart = txtFacturasProcesadas.TextLength;
                }

                //ToastNotification.DefaultToastGlowColor = eToastGlowColor.Red;
                ToastNotification.DefaultToastPosition = eToastPosition.MiddleCenter;
                ToastNotification.Show(this, "Facturas sincronizadas correctamente", 2000);

                dgvFacturas.Visible = true;
                
                gifSearchInvc.Visible = false;


                //
                //obtenerFacturas();
                panelFacturasProcesadas.Expanded = true;
                btnCorte.Enabled = true;
                btnCierreTurno.Enabled = true;
            }
            catch (Exception x)
            {
                txtFacturasProcesadas.Text += "Ocurrió un problema al tratar de procesar la(s) factura(s) seleccionada(s) =>  " + x.Message + "\n";
                txtFacturasProcesadas.SelectionStart = txtFacturasProcesadas.TextLength;
            }
            
        }

        private void btnCorte_Click(object sender, EventArgs e)
        {
            CierreTarimas modal = new CierreTarimas();
            modal.folioGral = folioProceso;
            modal.epiUser = epiUser;
            modal.ShowDialog();
        }

        private void btnCierreTurno_Click(object sender, EventArgs e)
        {
            util.SQLstatement(String.Format("UPDATE tb_Sesiones SET Abierto = 0 WHERE Id = {0}", idSesion[0]), TISERVER);
            Application.Exit();
        }

        private async void btnReporteTarimas_Click(object sender, EventArgs e)
        {
            try
            {
                if (idSesion[1].Equals("S/P"))
                    obtFolioProceso();
                dtResumenTarimas = util.getRecords(String.Format(ConfigurationManager.AppSettings["consultarTurnoAnterior"], idSesion[1]), null, conEpicor);
                file = new FileManager();


                await file.exportTable(dtResumenTarimas, idSesion[1], epiUser.ToUpper());
            }
            catch (System.IO.IOException fileException)
            {
                MessageBox.Show(fileException.Message);
            }
        }
        #endregion

        private void sincronizaFacturas()
        {
            invoiceProcList.Clear();

            DataTable prueba = util.getRecords("SELECT FactNum FROM tb_FactProc",null,TISERVER);
            for (int i = 0; i < prueba.Rows.Count; i++)
            {
                invoiceProcList.Add(Convert.ToInt32(prueba.Rows[i].ItemArray[0].ToString()));
            }
        }

        private void getDrivers()
        {
            string instruccion = ConfigurationManager.AppSettings["obtChoferes"].ToString();
            int index = 0;
            DataTable dt = util.getRecords(instruccion, null, conEpicor);

            foreach (DataRow row in dt.Rows)
            {
                //listaChoferes.Items.Add(row[0].ToString() + " - " + row[1].ToString());
                index++;
            }
        }

        private void obtFolioProceso()
        {
            if (idSesion[1].Equals("S/P"))
            {
                DataTable obtID = util.getRecords("SELECT MAX(Id),FolioActivo FROM tb_Sesiones WHERE FolioActivo LIKE '%DLM%' GROUP BY Id,FolioActivo;", null, TISERVER);
                int nFilas = obtID.Rows.Count;
                //MessageBox.Show(nFilas.ToString());
                
                if (nFilas == 0) // Si no hay ningún folio DLM se asigna DLM-1
                {
                    util.SQLstatement(String.Format("UPDATE tb_Sesiones SET FolioActivo = 'DLM-1', Abierto = 1 WHERE Id = {0}", idSesion[0]), TISERVER);
                    folioVIR = "DLM-1VIR1";
                    folioBES = "DLM-1BES1";
                    folioT1  = "DLM-1T1BUEES1";
                    folioT2  = "DLM-1T2BUEES1";
                    folioT3  = "DLM-1T3BUEES1";
                    folioDEF = "DLM-1DEF1";
                    folioEDA = "DLM-1EDA1";
                    folioGAR = "DLM-1GAR1";
                    folioProceso = "DLM-1";
                    idSesion[1] = "DLM-1";

                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioVIR, folioProceso, epiUser, "GETDATE()", "VIRTUAL"), TISERVER);

                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioBES, folioProceso, epiUser, "GETDATE()", "GENERAL"), TISERVER);
                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioT1, folioProceso, epiUser, "GETDATE()", "TORRE1"), TISERVER);
                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioT2, folioProceso, epiUser, "GETDATE()", "TORRE2"), TISERVER);
                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioT3, folioProceso, epiUser, "GETDATE()", "TORRE3"), TISERVER);

                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioDEF, folioProceso, epiUser, "GETDATE()", "DEFECTUOSO"), TISERVER);
                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioEDA, folioProceso, epiUser, "GETDATE()", "EDAÑADO"), TISERVER);
                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioGAR, folioProceso, epiUser, "GETDATE()", "GARANTIA"), TISERVER);
                }
                else
                {
                    // Si se obtuvieron filas, se obtiene el mayor folio de turno y se hace el incremento para asignarlo al turno actual
                    string next = "DLM-" + ((Convert.ToInt32(obtID.Rows[obtID.Rows.Count - 1].ItemArray[1].ToString().Substring(4)) + 1).ToString());
                    folioVIR = next + "VIR1";
                    folioBES = next + "BUEES1";
                    folioT1  = next + "T1BUEES1";
                    folioT2  = next + "T2BUEES1";
                    folioT3  = next + "T3BUEES1";
                    folioDEF = next + "DEF1";
                    folioEDA = next + "EDA1";
                    folioGAR = next + "GAR1";
                    idSesion[1] = next;


                    util.SQLstatement(String.Format("UPDATE tb_Sesiones SET FolioActivo = '{0}', Abierto = 1 WHERE Id = {1}", next, idSesion[0]), TISERVER);
                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioVIR, next, epiUser, "GETDATE()", "VIRTUAL"), TISERVER);

                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioBES, next, epiUser, "GETDATE()", "GENERAL"), TISERVER);
                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioT1, next, epiUser, "GETDATE()", "TORRE1"), TISERVER);
                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioT2, next, epiUser, "GETDATE()", "TORRE2"), TISERVER);
                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioT3, next, epiUser, "GETDATE()", "TORRE3"), TISERVER);

                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioDEF, next, epiUser, "GETDATE()", "DEFECTUOSO"), TISERVER);
                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioEDA, next, epiUser, "GETDATE()", "EDAÑADO"), TISERVER);
                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioGAR, next, epiUser, "GETDATE()", "GARANTIA"), TISERVER);
                    folioProceso = next;
                }
            }
            else
            {
                if (idSesion[2].Equals("0")) // Si el turno está cerrado, se hace el incremento para 
                {
                    string nextID = "DLM-" + (Convert.ToInt32(idSesion[1].Substring(4)) + 1).ToString();
                    util.SQLstatement(String.Format("INSERT INTO dbo.tb_Sesiones(Usuario,Terminal,FolioActivo,FechaInicio,Abierto) VALUES('{0}','{1}','{2}',{3},{4});", epiUser.ToUpper(), epiWorkstation, nextID, "GETDATE()", "1"), TISERVER);
                    DataTable nuevoTurno = util.getRecords("SELECT Id,FolioActivo,Abierto FROM tb_Sesiones WHERE FolioActivo = " + nextID, null, TISERVER);

                    idSesion[0] = nuevoTurno.Rows[0].ItemArray[0].ToString();
                    idSesion[1] = nuevoTurno.Rows[0].ItemArray[1].ToString();
                    idSesion[2] = nuevoTurno.Rows[0].ItemArray[2].ToString();

                    folioVIR = nextID + "VIR1";
                    folioBES = nextID + "BUEES1";
                    folioT1  = nextID + "T1BUEES1";
                    folioT1  = nextID + "T2BUEES1";
                    folioT1  = nextID + "T3BUEES1";
                    folioDEF = nextID + "DEF1";
                    folioEDA = nextID + "EDA1";
                    folioGAR = nextID + "GAR1";


                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioVIR, nextID, epiUser, "GETDATE()", "VIRTUAL"), TISERVER);

                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioBES, nextID, epiUser, "GETDATE()", "GENERAL"), TISERVER);
                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioT1, nextID, epiUser, "GETDATE()", "TORRE1"), TISERVER);
                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioT2, nextID, epiUser, "GETDATE()", "TORRE2"), TISERVER);
                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioT3, nextID, epiUser, "GETDATE()", "TORRE3"), TISERVER);

                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioDEF, nextID, epiUser, "GETDATE()", "DEFECTUOSO"), TISERVER);
                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioEDA, nextID, epiUser, "GETDATE()", "EDAÑADO"), TISERVER);
                    util.SQLstatement(String.Format("INSERT INTO tb_Tarimas(Folio,FolioRMA,UsuarioCaptura,FechaCaptura,Ubicacion) VALUES('{0}','{1}','{2}',{3},'{4}')", folioGAR, nextID, epiUser, "GETDATE()", "GARANTIA"), TISERVER);
                    folioProceso = nextID;
                }
                else
                {
                    //Si el turno se encuentra abierto solo se obtienen los folios actuales de las tarimas
                    folioProceso = idSesion[1];
                    obtFoliosTarimas();
                }
            }
            //MessageBox.Show("Turno " + idSesion[1]);
        }

        private void obtFoliosTarimas()
        {
            DataTable folios = util.getRecords(String.Format("SELECT Folio FROM tb_Tarimas WHERE FolioRMA = '{0}';",folioProceso), null, TISERVER);
            folioVIR = folios.Rows[0].ItemArray[0].ToString();
            folioBES = folios.Rows[1].ItemArray[0].ToString();
            folioT1  = folios.Rows[2].ItemArray[0].ToString();
            folioT2  = folios.Rows[3].ItemArray[0].ToString();
            folioT3  = folios.Rows[4].ItemArray[0].ToString();
            folioDEF = folios.Rows[5].ItemArray[0].ToString();
            folioEDA = folios.Rows[6].ItemArray[0].ToString();
            folioGAR = folios.Rows[7].ItemArray[0].ToString();

            //MessageBox.Show(folioVIR + "\n" + folioBES + "\n" + folioDEF + "\n" + folioEDA + "\n" + folioGAR);
        }

        private void disableButtons()
        {
            if (btnCorte.Enabled == true)
                btnCorte.Enabled = false;
            if (btnCierreTurno.Enabled == true)
                btnCierreTurno.Enabled = false;
            if (btnRMA.Enabled == true)
                btnRMA.Enabled = false;
        }

        private async Task generarRMA(List<string> lFactProcesada , DataTable dt, int factura, string legal, int cliente, string folioRelacion, string folioT)
        {
            try
            {
                recolectorEventos = String.Empty;
                string sqlQuery = String.Empty;
                List<string> iRMALine = new List<string>();
                int RMA = 0, ind2 = 0, existencia = 0;

                DataTable dtRMAData = util.getRecords(String.Format(ConfigurationManager.AppSettings["consultarRMA"], cliente, factura), null, conEpicor);
                int parseRMANum = (dtRMAData.Rows.Count == 0) ? 0 : parseRMANum = Convert.ToInt32(dtRMAData.Rows[0].ItemArray[0].ToString());

                EpiFunctions epiAdapter = new EpiFunctions(epiUser, epiPass);

                if (parseRMANum == 0)
                {
                    await epiAdapter.RMAheader(cliente, factura, legal, folioRelacion, folioT);
                    recolectorEventos += epiAdapter.recolector + "\n";
                    //Una vez creado el encabezado lo devuelvo para la revisión del encabezado
                    DataTable obtainRMAHead = util.getRecords(String.Format(ConfigurationManager.AppSettings["consultarRMA"], cliente, factura), null, conEpicor);
                    RMA = (obtainRMAHead.Rows.Count == 0) ? 0 : Convert.ToInt32(obtainRMAHead.Rows[0].ItemArray[0].ToString());

                    file.writeContentToFile(String.Format(epiAdapter.recolector, RMA));
                    sqlQuery = String.Format("INSERT INTO tb_FactProc(FactNum,NumeroLegal,Cliente,Lineas,Relacion,Status,UsuarioCaptura,FechaCaptura) VALUES({0},'{1}','{2}',{3},'{4}',{5},'{6}',{7});", lFactProcesada[0], lFactProcesada[1], lFactProcesada[2], lFactProcesada[3], lFactProcesada[4], "2", epiUser.ToUpper(), "GETDATE()");
                    util.SQLstatement(sqlQuery, TISERVER, null);
                }
                else
                {
                    recolectorEventos += "Se encontró la RMA " + parseRMANum + " abierta, se agregaran las líneas en su detallado \n";
                    file.writeContentToFile("Se encontró la RMA " + parseRMANum + " abierta, se agregaran las líneas en su detallado.");
                    RMA = parseRMANum;
                    // Si ya existe RMA abierta, se obtiene el número de líneas cargadas actualmente
                    DataTable numRMALines = util.getRecords(String.Format("SELECT COUNT(*) FROM Erp.RMADtl WHERE RMANum = {0};", parseRMANum), null, conEpicor);
                    existencia = (numRMALines.Rows.Count == 0) ? 0 : Convert.ToInt32(numRMALines.Rows[0].ItemArray[0].ToString());
                }
                //string partExist;

                foreach (DataRow fila in dt.Rows)
                {
                    int lineaFactura = Convert.ToInt32(dt.Rows[ind2].ItemArray[1].ToString());
                    string parte = dt.Rows[ind2].ItemArray[2].ToString();
                    string desc = dt.Rows[ind2].ItemArray[3].ToString();
                    int Pack = Convert.ToInt32(dt.Rows[ind2].ItemArray[4]);
                    int PackLine = Convert.ToInt32(dt.Rows[ind2].ItemArray[5]);
                    string razon = dt.Rows[ind2].ItemArray[6].ToString();
                    int numOrden = Convert.ToInt32(dt.Rows[ind2].ItemArray[7]);
                    int lineaOrden = Convert.ToInt32(dt.Rows[ind2].ItemArray[8]);
                    int relOrden = Convert.ToInt32(dt.Rows[ind2].ItemArray[9]);
                    double cant = Convert.ToDouble(dt.Rows[ind2].ItemArray[10]);
                    string UOM = dt.Rows[ind2].ItemArray[11].ToString();
                    
                    string ubicacion = dt.Rows[ind2].ItemArray[12].ToString();
                    string comentarios = "Ruta y Unidad " + dt.Rows[ind2].ItemArray[13].ToString() + ", Área Responsable: " + await areaResponsable(razon);
                    string zona = dt.Rows[ind2].ItemArray[14].ToString();
                    string primbin = dt.Rows[ind2].ItemArray[15].ToString();

                    string almacen = ConfigurationManager.AppSettings["Warehouse"].ToString();
                    int customer = cliente;

                    /*
                     * ==============================================================
                     * Criterios para no agregar una línea al detalle de la RMA:
                     * -> Si la cantidad a devolver es 0.
                     * -> Si el número de parte y línea de la factura son los mismos.
                     * ==============================================================
                    */

                    if (!cant.Equals(0))
                    {
                        // Se valida si existe la parte, de ser así devuelve una lista con: RMANum, RMALine, PartNum, InvoiceLine
                        iRMALine = epiAdapter.RMALineExist(RMA, parte, lineaFactura);

                        int lineaRMA = existencia + 1;
                        string tarimaDest = await definirTarimaDestino(ubicacion, zona);
                        epiAdapter.RMANewLine(RMA, lineaRMA, legal, factura, lineaFactura, numOrden, lineaOrden, relOrden, parte, desc, razon, cant, UOM, customer, comentarios, almacen, ubicacion, tarimaDest, primbin);
                        
                        try
                        {
                            util.SQLstatement(String.Format("INSERT INTO tb_FactDtl(FactNum,FactLine,PartNum,LineDesc,PackNum,PackLine,ReturnReason,OrderNum,OrderLine,OrderRel,ReturnQty,QtyUOM,PartClass,Note,ZoneID,PrimBin) VALUES({0},{1},'{2}','{3}',{4},{5},'{6}',{7},{8},{9},{10},'{11}','{12}','{13}','{14}','{15}');", factura, lineaFactura, parte, desc, Pack, PackLine, razon, numOrden, lineaOrden, relOrden, cant, UOM, ubicacion, comentarios, zona, primbin), TISERVER, null);
                        }
                        catch (System.Data.SqlClient.SqlException x)
                        {
                            MessageBox.Show(x.Message);
                        }
                        catch (Exception y)
                        {
                            MessageBox.Show(y.Message);
                        }

                        recolectorEventos += epiAdapter.recolector + "\n";
                        file.writeContentToFile(epiAdapter.recolector);
                        ReasonsList.Add(razon.Substring(0, 5)); // Se almacena el motivo de devolucion de la línea actual
                        existencia++;

                        /*
                        // 28/09/2017 (No valida) - Se agrega una restricción mas para permitir varias veces la misma parte solo si se clasifica a una ubicación diferente
                        if (!iRMALine[3].Equals(lineaFactura))
                        {
                            if (!iRMALine[4].Equals(ubicacion))
                            {
                                int lineaRMA = existencia + 1;
                                string tarimaDest = await definirTarimaDestino(ubicacion, zona);
                                epiAdapter.RMANewLine(RMA, lineaRMA, legal, factura, lineaFactura, numOrden, lineaOrden, relOrden, parte, desc, razon, cant, UOM, customer, comentarios, almacen, ubicacion, tarimaDest, primbin);
                                recolectorEventos += epiAdapter.recolector + "\n";

                                file.writeContentToFile(epiAdapter.recolector);
                                ReasonsList.Add(razon.Substring(0, 5)); // Se almacena el motivo de devolucion de la línea actual
                                existencia++;
                            }
                        }
                        else
                        {
                            recolectorEventos += "Ya existe la parte " + iRMALine[2] + " con número de línea " + iRMALine[3] + "\n";
                            file.writeContentToFile("Ya existe la parte " + iRMALine[2] + " con número de línea " + iRMALine[3]);
                        }
                        // 28/09/2017 - Fin de Customización
                        */
                    }
                    else
                    {
                        recolectorEventos += "La cantidad de la parte " + parte + " debe ser mayor a cero, no se agregará a la RMA.\n";
                        file.writeContentToFile("La cantidad de la parte " + parte + " debe ser mayor a cero, no se agregará a la RMA.");
                    }
                    ind2++;
                }

                int currentRMANum = epiAdapter.getRMANum(cliente, factura); //Se obtiene la RMA creada y se pasa al armado de la disposición
                epiAdapter.armaRMADisp(currentRMANum, ReasonsList); // Disposición de líneas de la RMA
                file.writeContentToFile(epiAdapter.recolector);

                epiAdapter.changeDocType(epiAdapter.CMreturn); // Cambio de tipo de documento en Nota de Crédito
                file.writeContentToFile(epiAdapter.recolector);

                //Terminada la RMA se actualiza el status de la factura en la BD Devoluciones
                sqlQuery = String.Format("UPDATE tb_FactProc SET Status = 1 WHERE FactNum = {0};", lFactProcesada[0]);
                util.SQLstatement(sqlQuery,TISERVER,null);
                ReasonsList.Clear();
                recolectorEventos += "Factura " + factura + " procesada en la RMA: " + RMA + "\n";
                if (!epiAdapter.PartTranException.Equals(""))
                    recolectorEventos += epiAdapter.PartTranException + "\n";

                recolectorEventos += "Terminó la generación de RMA para la factura " + factura;
                file.writeContentToFile("Terminó la generación de RMA para la factura " + factura);
            }
            catch (System.IndexOutOfRangeException RMANotFound)
            {
                recolectorEventos += "Se capturó la siguiente excepción al procesa la factura " + factura + ": " + RMANotFound.Message + "\n";
                file.writeContentToFile("Se capturó la siguiente excepción: " + RMANotFound.Message);
                string sql = String.Format("UPDATE tb_FactProc SET Status = 3 WHERE FactNum = {0};", lFactProcesada[0]);
                util.SQLstatement(sql, TISERVER, null);
                ReasonsList.Clear();
            }
            catch (Exception isBug)
            {
                recolectorEventos += "Se capturó la siguiente excepción al procesa la factura " + factura + ": " + isBug.Message;
                file.writeContentToFile("Excepción capturada " + isBug.Message);
                string sql = String.Format("UPDATE tb_FactProc SET Status = 3 WHERE FactNum = {0};", lFactProcesada[0]);
                util.SQLstatement(sql, TISERVER, null);
                ReasonsList.Clear();
            }
        }

        private async Task<string> areaResponsable(string motivoDev)
        {
            string dev = String.Empty;
            if (motivoDev.Contains("MDA"))
                dev = "Almacen";
            if (motivoDev.Contains("MVD") || motivoDev.Contains("MDV"))
                dev = "Ventas";
            if (motivoDev.Contains("MTD") || motivoDev.Contains("MDT"))
                dev = "Tráfico";
            return dev;
        }

        private async Task<string> definirTarimaDestino(string ubicacion, string zoneID)
        {
            string auxiliar = String.Empty;
            if (ubicacion.Contains("Virtual"))
                auxiliar = folioVIR;
            if (ubicacion.Contains("BueE"))
            {
                if (zoneID.Contains("T1"))
                    auxiliar = folioT1;
                else if (zoneID.Contains("T2"))
                    auxiliar = folioT2;
                else if (zoneID.Contains("T3"))
                    auxiliar = folioT3;
                else
                    auxiliar = folioBES;
            }
            if (ubicacion.Contains("Def"))
                auxiliar = folioDEF;
            if (ubicacion.Contains("EmpDaño"))
                auxiliar = folioEDA;
            if (ubicacion.Contains("Garantia"))
                auxiliar = folioGAR;
            return auxiliar;
        }
        
        private void loadSettings()
        {
            txOpcEpiConn.Text = ConfigurationManager.AppSettings["epiEnvironment"].ToString();
            txOpcEpiDB.Text = ConfigurationManager.AppSettings["connEpicor"].ToString();
            txOpcEpiCompany.Text = ConfigurationManager.AppSettings["epiCompany"].ToString();
            txOpcSslite.Text = ConfigurationManager.AppSettings["SQLiteConn"].ToString();
            txQueryDevoluciones.Text = ConfigurationManager.AppSettings["obtInvoices"].ToString();
            txQueryLineas.Text = ConfigurationManager.AppSettings["obtInvoiceDetail"].ToString();
            txQueryRMA.Text = ConfigurationManager.AppSettings["consultarRMA"].ToString();
        }

        private void listaChoferes_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnObtRelacion.Enabled = true;
            btnRMA.Enabled = false;
            dgvFacturas.Visible = false;
            /*
            if (dgvFacturas.Columns.Count > 0)
            {
                dgvFacturas.Columns.Remove("   Devolver");
                dgvFacturas.DataSource = null;
            }
            */

            dgvDetFactura.Visible = false;
            //txtChofer.Text = listaChoferes.SelectedItem.ToString();
            //Console.Write(listaChoferes.SelectedItem.ToString());
        }

        private void fillImageLists()
        {
            System.Drawing.Bitmap folder1 = ControlDevoluciones.Properties.Resources.close_folder;
            System.Drawing.Bitmap folder2 = ControlDevoluciones.Properties.Resources.open_folder;

            System.Drawing.Bitmap cash1 = ControlDevoluciones.Properties.Resources.casher;
            System.Drawing.Bitmap cash2 = ControlDevoluciones.Properties.Resources.casher;

            System.Drawing.Bitmap item1 = ControlDevoluciones.Properties.Resources.invoice_document;
            System.Drawing.Bitmap item2 = ControlDevoluciones.Properties.Resources.invoice_document;

            imgInvoiceTree.Images.Add(folder1);
            imgInvoiceTree.Images.Add(folder2);
            imgInvoiceTree.Images.Add(cash1);
            imgInvoiceTree.Images.Add(cash2);
            imgInvoiceTree.Images.Add(item1);
            imgInvoiceTree.Images.Add(item2);
        }

        private void dgvFacturas_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                int index = Convert.ToInt32(dgvFacturas.SelectedCells[0].RowIndex.ToString()); //Obtengo el indice de la fila
                lblDetallado.Visible = true;
                dgvDetFactura.Visible = true;
                btnRMA.Enabled = true;
            }
            catch (System.NullReferenceException isNull)
            {
                MessageBox.Show("Excepción por referencia nula ... \n" + isNull.Message, "NullReferenceException Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception isError)
            {
                MessageBox.Show("Excepción capturada ... \n" + isError.Message, "Exception Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvFacturas_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvFacturas.IsCurrentCellDirty)
                dgvFacturas.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void labelItem2_Click(object sender, EventArgs e)
        {

        }

        private void btnActualizarPendientes_Click(object sender, EventArgs e)
        {
            try
            {
                if (treeFacturasPendientes.GetNodeCount(false) > 0)
                    treeFacturasPendientes.Nodes.Clear();

                DataTable dt = util.getRecords(String.Format(ConfigurationManager.AppSettings["obtEsperaFacturas"].ToString(), idSesion[1]), null, conEpicor);
                int i = 0, iCobranza = 0, iFactura = 0;
                string rCobranza = String.Empty, rFactura = String.Empty;
                treeFacturasPendientes.ImageList = imgInvoiceTree;
                treeFacturasPendientes.Nodes.Add("key", "Facturas Pendientes", 0);
                TreeNode pattern = treeFacturasPendientes.Nodes[0];
                TreeNode cobranza = null;
                TreeNode factura = null;

                foreach (DataRow row in dt.Rows)
                {
                    if (rCobranza.Equals(""))
                    {
                        rCobranza = dt.Rows[i].ItemArray[0].ToString();
                        rFactura = dt.Rows[i].ItemArray[1].ToString();
                        factura = new TreeNode(rFactura, 4, 5);
                        cobranza = new TreeNode(rCobranza, 2, 3);
                        cobranza.Nodes.Add(factura);
                        pattern.Nodes.Add(cobranza);
                    }
                    else
                    {
                        if (dt.Rows[i].ItemArray[0].ToString().Equals(rCobranza))
                        {
                            if (!dt.Rows[i].ItemArray[1].ToString().Equals(rFactura))
                            {
                                TreeNode cobranzaActual = pattern.Nodes[iCobranza];
                                rFactura = dt.Rows[i].ItemArray[1].ToString();
                                factura = new TreeNode(dt.Rows[i].ItemArray[1].ToString(), 4, 5);
                                cobranzaActual.Nodes.Add(factura);
                            }
                        }
                        else
                        {
                            rCobranza = dt.Rows[i].ItemArray[0].ToString();
                            rFactura = dt.Rows[i].ItemArray[1].ToString();
                            cobranza = new TreeNode(rCobranza, 2, 3);
                            factura = new TreeNode(rFactura, 4, 5);
                            cobranza.Nodes.Add(factura);
                            pattern.Nodes.Add(cobranza);
                            iCobranza++;
                        }
                    }
                    i++;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            treeFacturasPendientes.ExpandAll();
        }

        private void btnActualizarProcesadas_Click(object sender, EventArgs e)
        {
            try
            {
                if (treeFacturasProcesadas.GetNodeCount(false) > 0)
                    treeFacturasProcesadas.Nodes.Clear();

                DataTable dt = util.getRecords(String.Format(ConfigurationManager.AppSettings["obtAvanceFacturas"].ToString(), idSesion[1]), null, conEpicor);
                int i = 0, iCobranza = 0, iFactura = 0;
                string rCobranza = String.Empty, rFactura = String.Empty;
                treeFacturasProcesadas.ImageList = imgInvoiceTree;
                treeFacturasProcesadas.Nodes.Add("key", "Facturas procesadas", 0);
                TreeNode pattern = treeFacturasProcesadas.Nodes[0];
                TreeNode cobranza = null;
                TreeNode factura = null;

                foreach (DataRow row in dt.Rows)
                {
                    if (rCobranza.Equals(""))
                    {
                        rCobranza = dt.Rows[i].ItemArray[0].ToString();
                        rFactura = dt.Rows[i].ItemArray[1].ToString();
                        factura = new TreeNode(rFactura, 4, 5);
                        cobranza = new TreeNode(rCobranza, 2, 3);
                        cobranza.Nodes.Add(factura);
                        pattern.Nodes.Add(cobranza);
                    }
                    else
                    {
                        if (dt.Rows[i].ItemArray[0].ToString().Equals(rCobranza))
                        {
                            if (!dt.Rows[i].ItemArray[1].ToString().Equals(rFactura))
                            {
                                TreeNode cobranzaActual = pattern.Nodes[iCobranza];
                                rFactura = dt.Rows[i].ItemArray[1].ToString();
                                factura = new TreeNode(dt.Rows[i].ItemArray[1].ToString(), 4, 5);
                                cobranzaActual.Nodes.Add(factura);
                            }
                        }
                        else
                        {
                            rCobranza = dt.Rows[i].ItemArray[0].ToString();
                            rFactura = dt.Rows[i].ItemArray[1].ToString();
                            cobranza = new TreeNode(rCobranza, 2, 3);
                            factura = new TreeNode(rFactura, 4, 5);
                            cobranza.Nodes.Add(factura);
                            pattern.Nodes.Add(cobranza);
                            iCobranza++;
                        }
                    }
                    i++;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            treeFacturasProcesadas.ExpandAll();
        }
        
        private async void treeFacturasProcesadas_AfterSelect(object sender, TreeViewEventArgs e)
        {
            DataTable x = new DataTable();
            bool flag1 = false, flag2 = false;
            loaderRMA.Visible = true;
            switchFactura.Visible = false;
            switchOpenRMA.Visible = false;
            switchNotaCredito.Visible = false;

            await Task.Factory.StartNew(async () =>
            {
                x = await functions.datosRMA(e.Node.Text, conEpicor);
            }).Unwrap();

            if (x.Rows.Count > 0)
            {
                if (x.Rows.Count > 1)
                {
                    int index = 0; //Indice para recorrer DataTable y mostrar los RMANum duplicados
                    foreach (DataRow row in x.Rows)
                    {
                        ListarRMADuplicada.Text += x.Rows[index].ItemArray[3].ToString() + "\n";
                        index++;
                    }

                    switchFactura.Value = true;
                }
                else
                {
                    ListarRMADuplicada.Text = String.Empty;
                    switchFactura.Value = false;
                }
                    
                textCliente.Text = x.Rows[0].ItemArray[0].ToString() + " - " + x.Rows[0].ItemArray[1].ToString(); // CustNum
                textFactura.Text = x.Rows[0].ItemArray[2].ToString(); // InvoiceNum
                textRMA.Text = x.Rows[0].ItemArray[3].ToString(); // RMANum
                textNotaCredito.Text = x.Rows[0].ItemArray[4].ToString(); // CreditMemo
                textOrdenVenta.Text = x.Rows[0].ItemArray[5].ToString(); // OrderNum
                DateTime var = Convert.ToDateTime(x.Rows[0].ItemArray[6]);
                dateTimeRMA.Text = var.ToShortDateString(); // RMADate
                
                if (x.Rows[0].ItemArray[7].ToString().Equals("1"))
                    switchOpenRMA.Value = false;
                else
                    switchOpenRMA.Value = true;


                if (x.Rows[0].ItemArray[8].ToString().Equals("1"))
                    switchNotaCredito.Value = false;
                else
                    switchNotaCredito.Value = true;


            }

            x.Clear();

            await Task.Factory.StartNew(async () =>
            {
                x = await functions.detalladoRMA(e.Node.Text, conEpicor);
            }).Unwrap();
            loaderRMA.Visible = false;

            dgvDetalleRMA.DataSource = x;
            switchFactura.Visible = true;
            switchOpenRMA.Visible = true;
            switchNotaCredito.Visible = true;
        }

        private async void treeFacturasPendientes_AfterSelect(object sender, TreeViewEventArgs e)
        {
            DataTable queryResult = new DataTable();
            
            await Task.Factory.StartNew(async () =>
            {
                queryResult = await functions.datosRMA(e.Node.Text, conEpicor);
            }).Unwrap();

            if (queryResult.Rows.Count == 0)
            {
                ToastNotification.DefaultToastGlowColor = eToastGlowColor.Green;
                ToastNotification.DefaultToastPosition = eToastPosition.MiddleCenter;
                ToastNotification.Show(this, "No existe ninguna RMA en Epicor para esta factura !!", 2000);

                switchOpenRMA_c.Visible = false;
                switchCreditMemo_c.Visible = false;

                txRMA_c.Text = "";
                dateRMADate_c.Text = ""; // RMADate
                txInvc_c.Text = "";
                txLegalNumber_c.Text = "";
                txCustomer_c.Text = "";
                txCreditMemo_c.Text = "";
                txUser_c.Text = "";
            }
            else
            {
                switchOpenRMA_c.Visible = true;
                switchCreditMemo_c.Visible = true;

                txRMA_c.Text = queryResult.Rows[0].ItemArray[3].ToString();
                DateTime var = Convert.ToDateTime(queryResult.Rows[0].ItemArray[6]);
                dateRMADate_c.Text = var.ToShortDateString(); // RMADate
                txInvc_c.Text = queryResult.Rows[0].ItemArray[2].ToString();
                txLegalNumber_c.Text = queryResult.Rows[0].ItemArray[9].ToString();
                txCustomer_c.Text = queryResult.Rows[0].ItemArray[0].ToString();
                txCreditMemo_c.Text = queryResult.Rows[0].ItemArray[4].ToString();
                txUser_c.Text = queryResult.Rows[0].ItemArray[10].ToString();

                if (queryResult.Rows[0].ItemArray[7].ToString().Equals("1"))
                    switchOpenRMA_c.Value = false;
                else
                    switchOpenRMA_c.Value = true;

                if (queryResult.Rows[0].ItemArray[8].ToString().Equals("1"))
                    switchCreditMemo_c.Value = false;
                else
                    switchCreditMemo_c.Value = true;
            }
            queryResult.Clear();

            await Task.Factory.StartNew(async () =>
            {
                queryResult = await functions.detalladoRMA(e.Node.Text, conEpicor);
            }).Unwrap();

            if (queryResult.Rows.Count > 0)
            {
                dgvDetalleRMA_c.DataSource = queryResult;
            }
            else
            {
                dgvDetalleRMA_c.DataSource = "";
            }
        }

        private void tabAccionesAd_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable query = util.getRecords(String.Format(ConfigurationManager.AppSettings["turnosUsuario"], epiUser), null, TISERVER);

                if (query.Rows.Count > 0)
                {
                    int i = 0;
                    foreach (DataRow fila in query.Rows)
                    {
                        listBoxHistorialTurnos.Items.Add(query.Rows[i].ItemArray[0].ToString());
                        i++;
                    }
                }

                else
                    listBoxHistorialTurnos.DataSource = "No hay datos que mostrar.";
            }
            catch (Exception r)
            {
                Console.WriteLine(r.Message);
            }
        }

        private void listBoxHistorialTurnos_ItemClick(object sender, EventArgs e)
        {
            DataTable exec = util.getRecords(String.Format(ConfigurationManager.AppSettings["consultarTurnoAnterior"], listBoxHistorialTurnos.SelectedItem.ToString()), null, conEpicor);

            if (exec.Rows.Count > 0)
                dgvFacturasTurnosAnteriores.DataSource = exec;
        }

        private async void btnReporteDelTurno_Click(object sender, EventArgs e)
        {
            try
            {
                if (idSesion[1].Equals("S/P"))
                    obtFolioProceso();
                dtResumenTarimas = util.getRecords(String.Format(ConfigurationManager.AppSettings["consultarTurnoAnterior"], listBoxHistorialTurnos.SelectedItem.ToString()), null, conEpicor);
                file = new FileManager();


                await file.exportTable(dtResumenTarimas, listBoxHistorialTurnos.SelectedItem.ToString(), epiUser.ToUpper());
            }
            catch (System.IO.IOException fileException)
            {
                MessageBox.Show(fileException.Message);
            }
        }

        private async void dgvFacturas_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                string factura_actual = dgvFacturas.CurrentCell.Value.ToString();
                int linea = dgvFacturas.CurrentCell.RowIndex;
                DataTable dt = new DataTable();

                if (dgvFacturas.CurrentCell.ColumnIndex == 1)
                {
                    ToastNotification.DefaultToastGlowColor = eToastGlowColor.Blue;
                    ToastNotification.DefaultToastPosition = eToastPosition.MiddleCenter;
                    ToastNotification.Show(this, "Consultando detallado de líneas a devolver", 2000);

                    await Task.Factory.StartNew(async () =>
                    {
                        dt = await functions.getInvoiceDtl(linea, conEpicor, factura_actual);
                    }).Unwrap();

                    dgvDetFactura.DataSource = dt;

                    ToastNotification.DefaultToastGlowColor = eToastGlowColor.Blue;
                    ToastNotification.DefaultToastPosition = eToastPosition.MiddleCenter;
                    ToastNotification.Show(this, "Detalle consultado correctamente", 1000);
                }
            }
            catch (System.NullReferenceException) { }
        }

        private void sideNavItem1_Click(object sender, EventArgs e)
        {

        }

        private async Task fillAdvTree()
        {
            try
            {
                DataTable sql = util.getRecords("SELECT x.ResponsableRelacion,c.Name,x.Evento_Key FROM TISERVER.DevolucionesTEST.dbo.Choferes c CROSS APPLY(SELECT d.ResponsableRelacion, Evento_Key FROM dbo.MS_DevChfrs_tst d WHERE d.ResponsableRelacion = c.Id)x GROUP BY x.ResponsableRelacion, c.Name, x.Evento_Key ORDER BY x.ResponsableRelacion;", null, conEpicor);
                
                advTreeDrivers.ImageList = imgListTreeDrivers;
                DevComponents.AdvTree.Node nodeDriver;
                DevComponents.AdvTree.Node nodeEvent;
                DevComponents.AdvTree.Node nodeAux;
                int ind = 0, iDriver = 0, iEvent = 0;
                string tmpUser = String.Empty;
                string tmpEvent = String.Empty;

                foreach (DataRow row in sql.Rows)
                {
                    if (tmpUser.Equals(""))
                    {
                        tmpUser = sql.Rows[ind].ItemArray[0].ToString();
                        tmpEvent = sql.Rows[ind].ItemArray[2].ToString();

                        nodeDriver = new DevComponents.AdvTree.Node(sql.Rows[ind].ItemArray[0].ToString() + " - " + sql.Rows[ind].ItemArray[1].ToString());
                        nodeEvent = new DevComponents.AdvTree.Node(sql.Rows[ind].ItemArray[2].ToString());
                        nodeDriver.ImageIndex = 1;
                        nodeEvent.ImageIndex = 2;
                        nodeDriver.ImageExpandedIndex = 0;
                        nodeDriver.Selectable = false;
                        nodeDriver.Enabled = false;
                        nodeDriver.Nodes.Add(nodeEvent);
                        advTreeDrivers.Nodes.Add(nodeDriver);
                        ind++;
                    }
                    else
                    {
                        if (!sql.Rows[ind].ItemArray[0].ToString().Contains(tmpUser)) //El chofer no se encuentra cargado en la lista
                        {
                            tmpUser = sql.Rows[ind].ItemArray[0].ToString();
                            tmpEvent = sql.Rows[ind].ItemArray[2].ToString();
                            nodeDriver = new DevComponents.AdvTree.Node(sql.Rows[ind].ItemArray[0].ToString() + " - " + sql.Rows[ind].ItemArray[1].ToString());
                            nodeEvent = new DevComponents.AdvTree.Node(sql.Rows[ind].ItemArray[2].ToString());
                            nodeDriver.ImageIndex = 1;
                            nodeEvent.ImageIndex = 2;
                            nodeDriver.ImageExpandedIndex = 0;
                            nodeDriver.Selectable = false;
                            nodeDriver.Enabled = false;
                            nodeDriver.Nodes.Add(nodeEvent);
                            advTreeDrivers.Nodes.Add(nodeDriver);
                            iDriver++;
                        }
                        else // El chofer ya se encuentra agregado en la vista
                        {
                            nodeAux = advTreeDrivers.Nodes[iDriver];

                            if (!sql.Rows[ind].ItemArray[2].ToString().Contains(tmpEvent)) //Si el evento_key no está repetido
                            {
                                tmpEvent = sql.Rows[ind].ItemArray[2].ToString();
                                nodeEvent = new DevComponents.AdvTree.Node(tmpEvent);
                                nodeEvent.ImageIndex = 2;
                                nodeAux.Nodes.Add(nodeEvent);
                            }
                        }
                        ind++;
                    }
                }
            }
            catch (System.Data.SqlClient.SqlException w)
            {
                MessageBox.Show(w.Message);
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private async void advTreeDrivers_AfterNodeSelect(object sender, DevComponents.AdvTree.AdvTreeNodeEventArgs e)
        {
            try
            {
                if (e.Node.Enabled == true)
                {
                    // Otención de registros
                    DataTable res = new DataTable();
                    gifSearchInvc.Visible = true;
                    
                    await Task.Factory.StartNew(async () =>
                    {
                        res = await functions.obtenerFacturas(e.Node.Text, conEpicor);
                    }).Unwrap();

                    if (res.Rows.Count > 0)
                        dgvFacturas.DataSource = res;

                    dgvFacturas.Visible = true;
                    gifSearchInvc.Visible = false;

                    DataTable d = new DataTable();
                    await Task.Factory.StartNew(async () =>
                    {
                        d = await getRowsByEvent(e.Node.Text);
                    }).Unwrap();

                    dgvDetFactura.DataSource = d;
                    dgvDetFactura.Visible = true;
                }
            }
            catch (System.NullReferenceException) { }
        }

        private async Task<DataTable> getRowsByEvent(string Event)
        {
            DataTable DBrecords = util.getRecords(String.Format("SELECT * FROM dbo.MS_DevChfrs_tst Where Evento_Key = '{0}'", Event), null, conEpicor);
            DataRow[] returnedRows;

            returnedRows = DBrecords.Select(String.Format("Evento_Key = '{0}'", Event));
            for (int x = 0; x < returnedRows.Length; x++)
            {
                MessageBox.Show("Factura: " + returnedRows[x][5]);
            }

            DataTable filterResult = returnedRows.CopyToDataTable();

            return filterResult;
        }
    }
}
