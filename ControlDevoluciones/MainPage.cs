using DevComponents.DotNetBar;
using EpicorAdapters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utilities;

namespace ControlDevoluciones
{
    public partial class PantallaPrincipal : Form
    {
        FileManager file;
        LoadInitData data;
        SQLUtilities util = new SQLUtilities();
        Orchestador functions = new Orchestador();
        Config conf = new Config();
        DataTable facturas = new DataTable();
        DataTable dtEventRows = new DataTable();
        DataTable dtResumenTarimas = new DataTable();
        DataTable dt2 = new DataTable(); //Utilizado para obtener el detallado original de un evento con lineas reasignables
        DataTable dtCustomEvent = new DataTable(); // Se utiliza para procesar las facturas de un evento modificado por el usuario
        string folderBase = ConfigurationManager.AppSettings["mainFolder"].ToString();
        string conMultistop = ConfigurationManager.AppSettings["connMultistop"].ToString();
        string conEpicor = ConfigurationManager.AppSettings["connEpicor"].ToString();
        string TISERVER = ConfigurationManager.AppSettings["connRMADB"].ToString();
        string recolectorEventos, varEventoID;
        Double valorPrevioCambio = 0, asigPrevioCambio = 0;
        public List<string> partesRMA = new List<string>();
        public List<String> ReasonsList = new List<string>();
        public List<int> invoiceProcList = new List<int>(); //Lista de facturas ya procesadas
        List<int> selectedRows = new List<int>(); //Lista de filas para facturas a procesar
        public List<string> idSesion = new List<string>();
        List<int> listadoIndFacturasAlt = new List<int>();
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
            this.Text = Application.ProductName + " " + Application.ProductVersion;
        }

        private async void tmrLoader_Tick(object sender, EventArgs e)
        {
            if (panelClsfDev.Expanded == true)
                panelClsfDev.Expanded = false;
            data = new LoadInitData();
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
        private async void advTreeDrivers_AfterNodeSelect(object sender, DevComponents.AdvTree.AdvTreeNodeEventArgs e)
        {
            try
            {
                if (e.Node.Enabled == true)
                {
                    //Limpieza del panel de asignación
                    limpiarPanelAsignacion();

                    // Separacion del nodo
                    char[] separadores = {':',' '};
                    char[] sepFacturas = { ',' };
                    string[] NodeItems = e.Node.Text.Split(separadores);
                    
                    // Otención de registros
                    dtEventRows.Clear();
                    varEventoID = NodeItems[7];
                    lbEventoActual.Text = NodeItems[7];
                    
                    gifSearchInvc.Visible = true;

                    await Task.Factory.StartNew(async () =>
                    {
                        dtEventRows = await functions.obtenerFacturas(varEventoID, conEpicor);
                    }).Unwrap();

                    if (!functions.catcher.Equals(""))
                        MessageBox.Show(functions.catcher, "Problema al consultar las facturas", MessageBoxButtons.OK, MessageBoxIcon.Information);


                    // Carga de facturas con lineas asignables a la lista para dispersión
                    if (functions.lineasAsignables == true)
                    {
                        panelClsfDev.Expanded = true;
                        gifObtPartesAsignables.Visible = true;
                        await Task.Factory.StartNew(async () =>
                        {
                            await cargarPartesAsignables();
                        }).Unwrap();

                        dgvNvoDetalleEventoKey.DataSource = dt2;
                        gifObtPartesAsignables.Visible = false;
                    }

                    if (dtEventRows.Rows.Count > 0)
                        dgvFacturas.DataSource = dtEventRows;

                    dgvFacturas.Visible = true;
                    gifSearchInvc.Visible = false;

                    btnEventProcess.Enabled = true;
                }
            }
            catch (System.NullReferenceException) { }
        }
        
        private async void btnEventProcess_Click(object sender, EventArgs e)
        {
            try
            {
                int i = 0;
                file = new FileManager();
                DataTable EventRecords = new DataTable(); // Contiene todos los registros del evento actual
                DataTable tmp = new DataTable(); // Contiene los registros filtrados por el numero de factura
                DataTable dtPrint = new DataTable(); // Contiene los registros preparados para procesar la factura
                LoaderForm loading = new LoaderForm();
                loading.Show();

                // Se asigna el turno al usuario y se impide el cierre de tarimas o turno en el proceso de RMA's
                obtFolioProceso();
                disableButtons();
                advTreeDrivers.Enabled = false;

                await Task.Factory.StartNew(async () =>
                {
                    EventRecords = await functions.LinesToDetail(varEventoID, conEpicor);
                }).Unwrap();
                
                foreach (DataRow row in dtEventRows.Rows)
                {
                    DataRow[] rows = EventRecords.Select(String.Format("DistrDev LIKE '%{0}%'",dtEventRows.Rows[i].ItemArray[0].ToString()));
                    tmp = rows.CopyToDataTable();

                    /* ******************************************* */
                    /* Obtencion de datos de la factura a procesar */
                    /* ******************************************* */
                    await Task.Factory.StartNew(async () =>
                    {
                        dtPrint = await functions.getInvoiceDtl(tmp, conEpicor, dtEventRows.Rows[i].ItemArray[0].ToString());
                    }).Unwrap();

                    if (functions.catcher.Equals(""))
                    {
                        txtFacturasProcesadas.Text += "Se otuvieron las líneas a devolver.\n";
                        txtFacturasProcesadas.SelectionStart = txtFacturasProcesadas.TextLength;
                        dgvDetFactura.DataSource = dtPrint;
                        dgvDetFactura.Visible = true;
                    }
                    else
                    {
                        txtFacturasProcesadas.Text += "Ocurrió un problema al obtener las líneas de la factura. " + functions.catcher + "\n";
                        txtFacturasProcesadas.SelectionStart = txtFacturasProcesadas.TextLength;
                    }

                    /* ******************************************* */
                    /* Creación del log de la relacion de cobranza */
                    /* ******************************************* */
                    string folioRelacion = dtEventRows.Rows[i].ItemArray[4].ToString();
                    file.createLog(folioRelacion);
                    file.writeContentToFile("\n");
                    file.writeContentToFile(String.Format("\n[ {1} ] - Comienza la generación de RMA para la factura: {0} ", dtEventRows.Rows[i].ItemArray[0].ToString(), System.DateTime.Now));
                    txtFacturasProcesadas.Text += String.Format("Comienza la generación de RMA para la factura: {0}\n", dtEventRows.Rows[i].ItemArray[0].ToString());
                    panelAwaitAsync.Visible = true;
                    char[] separadores = { ',', ' ' };

                    await Task.Factory.StartNew(async () =>
                    {
                        await generarRMA(dtPrint, Convert.ToInt32(dtEventRows.Rows[i][0]), dtEventRows.Rows[i][1].ToString().Trim(separadores), Convert.ToInt32(Regex.Replace(dtEventRows.Rows[i][2].ToString(), @"[^\d]", "", RegexOptions.None, TimeSpan.FromSeconds(1.5))), dtEventRows.Rows[i][4].ToString(), idSesion[1], Convert.ToInt32(dtEventRows.Rows[i][3]), varEventoID);
                    }).Unwrap();

                    txtFacturasProcesadas.Text += recolectorEventos + "\n";
                    panelAwaitAsync.Visible = false;

                    i++;
                }

                loading.Close();

                /* ********************************************* */
                /* Actualización de choferes despues del proceso */
                /* ********************************************* */
                advTreeDrivers.Enabled = true;
                MessageBox.Show("Terminó la generación de RMA's", "Proceso terminado", MessageBoxButtons.OK,MessageBoxIcon.Information);
                advTreeDrivers.Nodes.Clear();
                dgvFacturas.Visible = false;
                dgvDetFactura.Visible = false;
                data = new LoadInitData();
                data.Show();
                await Task.Factory.StartNew(async () =>
                {
                    await fillAdvTree();
                }).Unwrap();
                data.Close();

                // Habilitado de botones para cerrar tarimas y turno
                if (btnCierreTurno.Enabled == false)
                {
                    btnCierreTurno.Enabled = true;
                    btnCorte.Enabled = true;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.StackTrace, exc.Message);
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
                MessageBox.Show(fileException.StackTrace, fileException.Message);
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
            if (util.catchError != "")
                MessageBox.Show(util.catchError);
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
            if (btnEventProcess.Enabled == true)
                btnEventProcess.Enabled = false;
        }

        private async Task generarRMA(DataTable dt, int factura, string legal, int cliente, string folioRelacion, string folioT, int lineasFactura, string evento)
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
                    await epiAdapter.RMAheader(cliente, factura, legal, folioRelacion, folioT, evento);
                    recolectorEventos += epiAdapter.recolector + "\n";
                    //Una vez creado el encabezado lo devuelvo para la revisión del encabezado
                    DataTable obtainRMAHead = util.getRecords(String.Format(ConfigurationManager.AppSettings["consultarRMA"], cliente, factura), null, conEpicor);
                    RMA = (obtainRMAHead.Rows.Count == 0) ? 0 : Convert.ToInt32(obtainRMAHead.Rows[0].ItemArray[0].ToString());

                    file.writeContentToFile(String.Format("[ {1} ] - " + epiAdapter.recolector, RMA, System.DateTime.Now));
                    sqlQuery = String.Format("INSERT INTO tb_FactProc(FactNum,NumeroLegal,Cliente,Lineas,Relacion,Status,UsuarioCaptura,FechaCaptura) VALUES({0},'{1}','{2}',{3},'{4}',{5},'{6}',GETDATE());", factura, legal, cliente, lineasFactura, folioRelacion, "2", epiUser.ToUpper());
                    util.SQLstatement(sqlQuery, TISERVER, null);
                }
                else
                {
                    recolectorEventos += "Se encontró la RMA " + parseRMANum + " abierta, se agregaran las líneas en su detallado \n";
                    file.writeContentToFile(String.Format("[ {0} ] - Se encontró la RMA " + parseRMANum + " abierta, se agregaran las líneas en su detallado.", System.DateTime.Now));
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
                            util.SQLstatement(String.Format("INSERT INTO tb_FactDtl(FactNum,FactLine,PartNum,LineDesc,PackNum,PackLine,ReturnReason,OrderNum,OrderLine,OrderRel,ReturnQty,QtyUOM,PartClass,Note,ZoneID,PrimBin,RMANum,RMALine,EventoKey, FechaCaptura) VALUES({0},{1},'{2}','{3}',{4},{5},'{6}',{7},{8},{9},{10},'{11}','{12}','{13}','{14}','{15}',{16},{17},'{18}',GETDATE());", factura, lineaFactura, parte, desc, Pack, PackLine, razon, numOrden, lineaOrden, relOrden, cant, UOM, ubicacion, comentarios, zona, primbin, RMA, lineaRMA, varEventoID), TISERVER, null);
                        }
                        catch (System.Data.SqlClient.SqlException x)
                        {
                            MessageBox.Show(x.StackTrace, x.Message);
                        }
                        catch (Exception y)
                        {
                            MessageBox.Show(y.StackTrace, y.Message);
                        }

                        recolectorEventos += epiAdapter.recolector + "\n";
                        file.writeContentToFile(String.Format("[ {0} ] - " + epiAdapter.recolector, System.DateTime.Now));
                        ReasonsList.Add(razon.Substring(0, 5)); // Se almacena el motivo de devolucion de la línea actual
                        existencia++;
                        
                    }
                    else
                    {
                        recolectorEventos += "La cantidad de la parte " + parte + " debe ser mayor a cero, no se agregará a la RMA.\n";
                        file.writeContentToFile(String.Format("[ {0} ] - La cantidad de la parte " + parte + " debe ser mayor a cero, no se agregará a la RMA.", System.DateTime.Now));
                    }
                    ind2++;
                }

                int currentRMANum = epiAdapter.getRMANum(cliente, factura); //Se obtiene la RMA creada y se pasa al armado de la disposición
                epiAdapter.armaRMADisp(currentRMANum, ReasonsList); // Disposición de líneas de la RMA
                file.writeContentToFile(String.Format("[ {0} ] - " + epiAdapter.recolector, System.DateTime.Now));

                epiAdapter.changeDocType(epiAdapter.CMreturn); // Cambio de tipo de documento en Nota de Crédito
                file.writeContentToFile(String.Format("[ {0} ] - " + epiAdapter.recolector, System.DateTime.Now));

                //Terminada la RMA se actualiza el status de la factura en la BD Devoluciones
                Console.WriteLine("Valor factura: " + factura + "\nValor RMA: " + RMA);
                sqlQuery = String.Format("UPDATE tb_FactProc SET Status = 1 WHERE FactNum = {0};", factura);
                util.SQLstatement(sqlQuery, TISERVER, null);
                ReasonsList.Clear();
                recolectorEventos += "Factura " + factura + " procesada en la RMA: " + RMA + "\n";
                if (!epiAdapter.PartTranException.Equals(""))
                    recolectorEventos += epiAdapter.PartTranException + "\n";

                recolectorEventos += "Terminó la generación de RMA para la factura " + factura;
                file.writeContentToFile(String.Format("[ {0} ] - Terminó la generación de RMA para la factura " + factura, System.DateTime.Now));
            }
            catch (System.IndexOutOfRangeException RMANotFound)
            {
                recolectorEventos += "Se capturó la siguiente excepción al procesa la factura " + factura + ": " + RMANotFound.Message + "\n";
                file.writeContentToFile(String.Format("[ {0} ] - Se capturó la siguiente excepción: " + RMANotFound.Message, System.DateTime.Now));
                string sql = String.Format("UPDATE tb_FactProc SET Status = 3 WHERE FactNum = {0};", factura);
                util.SQLstatement(sql, TISERVER, null);
                ReasonsList.Clear();
            }
            catch (Exception isBug)
            {
                recolectorEventos += "Se capturó la siguiente excepción al procesa la factura " + factura + ": " + isBug.Message;
                file.writeContentToFile(String.Format("[ {0} ] - Excepción capturada " + isBug.Message, System.DateTime.Now));
                string sql = String.Format("UPDATE tb_FactProc SET Status = 3 WHERE FactNum = {0};", factura);
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
            btnEventProcess.Enabled = false;
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
                btnEventProcess.Enabled = true;
            }
            catch (System.NullReferenceException isNull)
            {
                MessageBox.Show(isNull.StackTrace, isNull.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception isError)
            {
                MessageBox.Show(isError.StackTrace, isError.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show(ex.StackTrace, ex.Message);
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
                MessageBox.Show(ex.StackTrace, ex.Message);
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
                MessageBox.Show(fileException.StackTrace, fileException.Message);
            }
        }
        
        private void sideNavItem1_Click(object sender, EventArgs e)
        {

        }


        #region Funciones modificación de devolucion a facturas alternas

        private async void listPartesReasignables_ItemClick(object sender, EventArgs e)
        {
            string adv = listPartesReasignables.SelectedItem.ToString();
            if (dgvDetalleLineasAsignables.Rows.Count > 0)
                confirmAsignaciones(txPartNumDev.Text);

            DataTable d = new DataTable();
            txReturnQtyDev.Text = "0";
            txAvailableQtyDev.Text = "0";
            txPartNumDev.Text = "";
            listPartesReasignables.Enabled = false;
            panelAwaitForDetail.Visible = true;
            txPartNumDev.Text = adv;

            await Task.Factory.StartNew(async () =>
            {
                d = await consultarDetallado(adv, varEventoID);
            }).Unwrap();
            
            dgvDetalleLineasAsignables.DataSource = d;

            //Asigncación del campo DistrDev si Definido es igual a 1.
            foreach (DataGridViewRow row in dgvDetalleLineasAsignables.Rows)
            {
                for (int i = 0; i < dgvDetalleLineasAsignables.Columns.Count; i++)
                {
                    if (i != 1)
                        row.Cells[i].ReadOnly = true;
                }

                DataTable w = new DataTable();
                if (!row.Cells[18].Value.ToString().Equals(""))
                {
                    await Task.Factory.StartNew(async () =>
                    {
                        w = await functions.facturadoEnAsignados(row.Cells[2].Value.ToString(), row.Cells[4].Value.ToString(), row.Cells[3].Value.ToString(), conEpicor);
                    }).Unwrap();
                    row.Cells[0].Value = w.Rows[0].ItemArray[0];
                    row.Cells[1].Value = row.Cells[12].Value;
                }
                else
                {
                    row.Cells[1].Value = 0;
                    row.Cells[0].Value = row.Cells[12].Value;
                }
            }

            //Ocultar filas a usuario disponibles para proceso del evento
            DataGridViewBand b1  = dgvDetalleLineasAsignables.Columns[6]; //Empaque
            DataGridViewBand b2 = dgvDetalleLineasAsignables.Columns[7]; //LineaE
            DataGridViewBand b3 = dgvDetalleLineasAsignables.Columns[8]; //Motivo
            DataGridViewBand b4 = dgvDetalleLineasAsignables.Columns[9]; //Orden
            DataGridViewBand b5 = dgvDetalleLineasAsignables.Columns[10]; //LineaO
            DataGridViewBand b6 = dgvDetalleLineasAsignables.Columns[11]; //Relacion
            DataGridViewBand b7 = dgvDetalleLineasAsignables.Columns[12]; //DistrDev
            DataGridViewBand b8 = dgvDetalleLineasAsignables.Columns[14]; //DistrClfs
            DataGridViewBand b9 = dgvDetalleLineasAsignables.Columns[15]; //Observaciones
            DataGridViewBand b10 = dgvDetalleLineasAsignables.Columns[16]; //ZoneID
            DataGridViewBand b11 = dgvDetalleLineasAsignables.Columns[17]; //PrimBin
            DataGridViewBand b12 = dgvDetalleLineasAsignables.Columns[17]; //Definido

            b1.Visible = false;
            b2.Visible = false;
            b3.Visible = false;
            b4.Visible = false;
            b5.Visible = false;
            b6.Visible = false;
            b7.Visible = false;
            b8.Visible = false;
            b9.Visible = false;
            b10.Visible = false;
            b11.Visible = false;
            b12.Visible = false;

            txReturnQtyDev.Text = calcDevuelto().ToString();
            txAvailableQtyDev.Text = "0";
            panelAwaitForDetail.Visible = false;
            listPartesReasignables.Enabled = true;
            btCargarLineas.Enabled = true;
        }

        // Se evaluan todas las partes del evento que se pueden asignar a otras facturas
        private async Task cargarPartesAsignables()
        {
            DataTable dtPartesEvento = new DataTable();
            List<string> procesados = new List<string>();
            List<string> reasignables = new List<string>();
            int facturaCargada = -1;
            char[] trim = { ':',',' };
            listPartesReasignables.Items.Clear(); // Si hubiera datos anteriores se limpia el listBox   

            dtPartesEvento = await functions.LinesToDetail(varEventoID, conEpicor); // Se obtienen todos los registros del evento actual

            DataRow[] filasVariasFacturas = dtPartesEvento.Select("Facturas LIKE '%,%'"); // se filtran los registros que contengan ',' en la columna Facturas
            
            for (int a = 0; a < filasVariasFacturas.Length; a++)
            {
                int x = 0;
                procesados.Clear();
                string[] facts = filasVariasFacturas[a].ItemArray[0].ToString().Split(trim); //Separación del campo Facturas
                foreach (string i in facts)
                {
                    facturaCargada = procesados.FindIndex(delegate (string current)
                    {
                        return current.Contains(facts[x]);
                    });

                    if (facturaCargada == -1)
                    {
                        if (!filasVariasFacturas[a].ItemArray[10].ToString().Contains(facts[x]))
                        {
                            listPartesReasignables.Items.Add(filasVariasFacturas[a].ItemArray[2].ToString());
                            reasignables.Add(filasVariasFacturas[a].ItemArray[2].ToString());
                            procesados.Add(facts[x]);
                        }
                    }
                    facturaCargada = -1;
                    
                    x++;
                }
            }

            // Generación de detallado de las partes no reasignables
            int z = 0,size = reasignables.Count;
            string partesOmitidas = String.Empty;
            foreach (string item in reasignables)
            {
                if (z == 0) //Primer iteracion
                    if ((z + 1) == size)
                        partesOmitidas += "'" + item + "'";
                    else
                        partesOmitidas += "'" + item + "',";
                else //Despues de primer iteracion
                    if ((z + 1) == size)
                    partesOmitidas += "'" + item + "'";
                else
                    partesOmitidas += "'" + item + "',";
                z++;
            }
            Console.WriteLine("Partes a omitir: " + partesOmitidas);
            DataTable dt1 = util.getRecords(String.Format(ConfigurationManager.AppSettings["obtNoAsignables"], varEventoID, partesOmitidas),null,conEpicor);
            dt2 = dt1.Clone(); //Clonado de estructura para presentar los datos
            DataTable dt3 = new DataTable();

            List<string> l1 = new List<string>(); //Lista PartNum
            List<string> l2 = new List<string>(); //Lista DistrDev
            int y = 0, ind1 = 0, ind2 = 0;
            foreach (DataRow row in dt1.Rows)
            {
                l1.Add(dt1.Rows[y].ItemArray[2].ToString());
                l2.Add(dt1.Rows[y].ItemArray[10].ToString());
                y++;
            }

            if (l1.Count > 0)
            {
                do
                {
                    string part = l1[ind1]; //asignación de parte a procesar
                    string[] fact = l2[ind1].ToString().Split(trim); //separacion de DitrDev
                    if (fact.Length > 5)
                        Console.WriteLine("--> " + fact[0]);
                    do
                    {
                        if (!fact[ind2].Trim().Equals(""))
                        {
                            DataTable bum = util.getRecords(String.Format("SELECT r.Facturas,''  AS Linea,r.IdProducto,''  AS Descripcion,''  AS Empaque,''  AS LineaE,r.motivodevolucion,''  AS Orden,''  AS LineaO,''  AS Relacion,r.DistrDev,r.unidad,r.DistrClsf,r.Observaciones,r.ZoneID,r.PrimBin FROM ERP10DB.dbo.MS_DevChfrs_tst r WHERE Evento_Key = '{0}' AND IdProducto = '{1}' ORDER BY r.IdProducto, r.Facturas;", varEventoID, part), null, conEpicor);
                            await Task.Factory.StartNew(async () =>
                            {
                                dt3 = await functions.getInvoiceDtl(bum, conEpicor, fact[ind2].Trim());
                            }).Unwrap();

                            foreach (DataRow tmp in dt3.Rows)
                                dt2.ImportRow(tmp);
                        }
                        ind2 += 4;
                    }
                    while (ind2 < fact.Length);
                    ind2 = 0; //Reinicio del indice para recorrer el array fact
                    ind1++;
                }
                while (ind1 < l1.Count);
            }
        }

        private async Task<DataTable> consultarDetallado(string partNum, string Evento)
        {
            DataTable Part = new DataTable();
            try
            {
                int ind = 0, facturaCargada = -1;
                List<string> lFacturasConsultadas = new List<string>();
                List<String> listAlternasCargadas = new List<string>(); // Lista para almacenar las facturas que se han cargado al grid de asignación
                string wAlternos = String.Empty;
                DataTable PartDtl = new DataTable();
                DataTable dtFinal = new DataTable();
                DataTable InvoiceDtl = new DataTable();
                DataTable prim = new DataTable();

                char[] separadores = { ':', ',' };
                PartDtl = util.getRecords(String.Format("SELECT r.Facturas,''  AS Linea,r.IdProducto,''  AS Descripcion,''  AS Empaque,''  AS LineaE,r.motivodevolucion,''  AS Orden,''  AS LineaO,''  AS Relacion,r.DistrDev,r.unidad,r.DistrClsf,r.Observaciones,r.ZoneID,r.PrimBin,'' AS Definido FROM ERP10DB.dbo.MS_DevChfrs_tst r WHERE Evento_Key = '{0}' AND IdProducto = '{1}' ORDER BY r.IdProducto, r.Facturas;", Evento, partNum), null, conEpicor);
                dtFinal = PartDtl.Clone();
                string[] arr = PartDtl.Rows[ind].ItemArray[10].ToString().Trim().Split(separadores);
                string[] facturasAlternas = PartDtl.Rows[ind].ItemArray[0].ToString().Trim().Split(separadores);

                // Obtención del detallado original de la parte
                do
                {
                    if (lFacturasConsultadas.Count == 0)
                        lFacturasConsultadas.Add(arr[ind].Trim());
                    else
                    {
                        facturaCargada = lFacturasConsultadas.FindIndex(delegate (string current)
                        {
                            return current.Contains(arr[ind].Trim());
                        });
                    }

                    if (facturaCargada == -1)
                    {
                        InvoiceDtl = util.getRecords(String.Format("SELECT r.Facturas,''  AS Linea,r.IdProducto,''  AS Descripcion,''  AS Empaque,''  AS LineaE,r.motivodevolucion,''  AS Orden,''  AS LineaO,''  AS Relacion,r.DistrDev,r.unidad,r.DistrClsf,r.Observaciones,r.ZoneID,r.PrimBin,'' AS Definido FROM ERP10DB.dbo.MS_DevChfrs_tst r WHERE Evento_Key = '{0}' AND IdProducto = '{1}' ORDER BY r.IdProducto, r.Facturas;", Evento, partNum), null, conEpicor);
                        string factura = arr[ind].Trim();
                        if (arr[ind].Equals(""))
                            break;
                        else
                        {
                            await Task.Factory.StartNew(async () =>
                            {
                                prim = await functions.getInvoiceDtl(InvoiceDtl, conEpicor, factura);
                            }).Unwrap();
                            foreach (DataRow r in prim.Rows)
                            {
                                dtFinal.ImportRow(r);
                                dtFinal.Rows[dtFinal.Rows.Count - 1][16] = 1;
                            }
                        }
                        lFacturasConsultadas.Add(arr[ind].Trim());
                    }
                    ind += 4;
                }
                while (ind < arr.Length);

                // Obtención de detallado de facturas alternas
                int pos = 0;
                foreach (string s in facturasAlternas)
                {
                    if (!PartDtl.Rows[0].ItemArray[10].ToString().Contains(facturasAlternas[pos]))
                    {
                        Boolean existe = false;
                        buscarEnLista(listAlternasCargadas, facturasAlternas[pos], out existe);

                        if (existe == false)
                        {
                            // Adicion de la factura en la lista de alternas cargadas
                            listAlternasCargadas.Add(facturasAlternas[pos]);
                            // Adicion de fila al table final
                            DataTable AltInvcDtl = new DataTable();
                            int indice = 0;
                            await Task.Factory.StartNew(async () =>
                            {
                                AltInvcDtl = await functions.obtParteFacturaAlterna(facturasAlternas[pos].Trim(), partNum, conEpicor);
                            }).Unwrap();

                            foreach (DataRow row in AltInvcDtl.Rows)
                            {
                                dtFinal.Rows.Add();
                                int w = dtFinal.Rows.Count - 1;
                                listadoIndFacturasAlt.Add(w);
                                dtFinal.Rows[w][0] = AltInvcDtl.Rows[indice][0]; //InvoiceNum
                                dtFinal.Rows[w][1] = AltInvcDtl.Rows[indice][1]; //InvoiceLine
                                dtFinal.Rows[w][2] = partNum; //PartNum
                                dtFinal.Rows[w][3] = AltInvcDtl.Rows[indice][3]; //LineDesc
                                dtFinal.Rows[w][4] = AltInvcDtl.Rows[indice][4]; //PackNum
                                dtFinal.Rows[w][5] = AltInvcDtl.Rows[indice][5]; //PackLine
                                dtFinal.Rows[w][6] = dtFinal.Rows[w - 1][6]; //Return Reason
                                dtFinal.Rows[w][7] = AltInvcDtl.Rows[indice][6]; //OrderNum
                                dtFinal.Rows[w][8] = AltInvcDtl.Rows[indice][7]; //OrderLine
                                dtFinal.Rows[w][9] = AltInvcDtl.Rows[indice][8]; //OrderRelNum
                                dtFinal.Rows[w][10] = AltInvcDtl.Rows[indice][9]; //PartQty
                                //dtFinal.Rows[w][10] = AltInvcDtl.Rows[indice][]; //
                                dtFinal.Rows[w][11] = dtFinal.Rows[w - 1][11]; //UOM
                                dtFinal.Rows[w][12] = dtFinal.Rows[w - 1][12]; //Clasification
                                dtFinal.Rows[w][13] = dtFinal.Rows[w - 1][13]; //Note
                                dtFinal.Rows[w][14] = dtFinal.Rows[w - 1][14]; //ZoneID
                                dtFinal.Rows[w][15] = dtFinal.Rows[w - 1][15]; //PrimBin

                                indice++;
                            }
                        


                            if (facturasAlternas.Length == 1)
                                wAlternos += facturasAlternas[pos];
                            else if (facturasAlternas.Length > 1)
                            {
                                if (wAlternos.Equals(""))
                                {
                                    if ((pos + 1) == facturasAlternas.Length)
                                        wAlternos += facturasAlternas[pos];
                                    else
                                        wAlternos += facturasAlternas[pos] + ",";
                                }
                                else
                                {
                                    if ((pos + 1) == facturasAlternas.Length)
                                        wAlternos += facturasAlternas[pos];
                                    else
                                        wAlternos += facturasAlternas[pos] + ",";
                                }
                            }
                        }
                        
                    }
                    pos++;
                }
                Console.WriteLine("Facturas alternas a consultar: " + wAlternos);
                return dtFinal;

            }
            catch (System.IndexOutOfRangeException log)
            {
                MessageBox.Show(log.StackTrace, log.Message, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return Part;
            }
            catch (Exception e)
            {
                return Part;
            }
        }

        private void limpiarPanelAsignacion()
        {
            listPartesReasignables.Items.Clear();
            dgvDetalleLineasAsignables.DataSource = null;
            dgvNvoDetalleEventoKey.DataSource = null;
        }

        private async void btnActualizarChfrs_Click(object sender, EventArgs e)
        {
            advTreeDrivers.Nodes.Clear();
            // Se cierra el panel de reasignción (si está abierto)
            if (panelClsfDev.Expanded == true)
                panelClsfDev.Expanded = false;

            data = new LoadInitData();
            tmrLoader.Stop();
            data.Show();
            await Task.Factory.StartNew(async () =>
            {
                await fillAdvTree();
            }).Unwrap();
            data.Close();
            tmrLoader.Enabled = false;
        }

        private void cargarAsignaciones(string parte)
        {
            string currentItem = parte;
            foreach (DataGridViewRow row in dgvDetalleLineasAsignables.Rows)
            {
                if (Convert.ToDouble(row.Cells[1].Value) > 0)
                {
                    dt2.Rows.Add(dgvDetalleLineasAsignables.Rows[row.Index].Cells[2].Value/* Facturas */,
                                 dgvDetalleLineasAsignables.Rows[row.Index].Cells[3].Value/* Linea*/,
                                 dgvDetalleLineasAsignables.Rows[row.Index].Cells[4].Value/* IdProducto*/,
                                 dgvDetalleLineasAsignables.Rows[row.Index].Cells[5].Value/* Descripcion*/,
                                 dgvDetalleLineasAsignables.Rows[row.Index].Cells[6].Value/* Empaque */,
                                 dgvDetalleLineasAsignables.Rows[row.Index].Cells[7].Value/* LineaE */,
                                 dgvDetalleLineasAsignables.Rows[row.Index].Cells[8].Value/* Motivo */,
                                 dgvDetalleLineasAsignables.Rows[row.Index].Cells[9].Value/* Orden */,
                                 dgvDetalleLineasAsignables.Rows[row.Index].Cells[10].Value/* LineaO */,
                                 dgvDetalleLineasAsignables.Rows[row.Index].Cells[11].Value/* Relacion */,
                                 dgvDetalleLineasAsignables.Rows[row.Index].Cells[1].Value/* Cantidad */,
                                 dgvDetalleLineasAsignables.Rows[row.Index].Cells[13].Value/* unidad */,
                                 dgvDetalleLineasAsignables.Rows[row.Index].Cells[14].Value/* DitrClsf */,
                                 dgvDetalleLineasAsignables.Rows[row.Index].Cells[15].Value/* Observaciones */,
                                 dgvDetalleLineasAsignables.Rows[row.Index].Cells[16].Value/* ZoneID */,
                                 dgvDetalleLineasAsignables.Rows[row.Index].Cells[17].Value/* PrimBin */);
                }
            }
            dgvNvoDetalleEventoKey.DataSource = dt2;
            listPartesReasignables.Items.Remove(currentItem);
            txPartNumDev.Text = "";
            txReturnQtyDev.Text = "";
            txAvailableQtyDev.Text = "";
            dgvDetalleLineasAsignables.DataSource = null;
            btCargarLineas.Enabled = false;

            if (listPartesReasignables.Items.Count == 0)
                btProcesarAsignacionesEvt.Enabled = true;
        }

        private Boolean soloNumeros(string dato)
        {
            Regex Val = new Regex(@"^[+]?\d+(\.\d+)?$");
            if (Val.IsMatch(dato))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        private void dgvDetalleLineasAsignables_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                Boolean bandera = false;
                if (e.ColumnIndex == 1)
                {
                    Double AllReturnQty = Convert.ToDouble(txReturnQtyDev.Text);
                    Double AvailableQty = Convert.ToDouble(txAvailableQtyDev.Text);

                    Boolean flag = soloNumeros(dgvDetalleLineasAsignables.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString());
                    if (flag == false)
                    {
                        MessageBox.Show("Debe ingresar solo números en el campo cantidad.\nSe revertirá el cambio.", "Error");
                        dgvDetalleLineasAsignables.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = valorPrevioCambio;
                    }

                    bandera = devueltoFacturado(e.RowIndex, e.ColumnIndex);
                    if (bandera == false)

                    txAvailableQtyDev.Text = calcDisponible(e.RowIndex, e.ColumnIndex).ToString();
                }
            }
            catch (System.NullReferenceException)
            {
                MessageBox.Show("La celda no puede quedar vacía.\nSe revertirá el cambio.","Error",MessageBoxButtons.OK,MessageBoxIcon.Asterisk);
            }
        }

        private void dgvDetalleLineasAsignables_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            valorPrevioCambio = Convert.ToDouble(dgvDetalleLineasAsignables.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
            asigPrevioCambio = Convert.ToDouble(txAvailableQtyDev.Text);
        }

        private void btCargarLineas_Click(object sender, EventArgs e)
        {
            confirmAsignaciones(listPartesReasignables.SelectedItem.ToString());
        }

        private Double calcDevuelto()
        {
            Double result = 0;

            foreach (DataGridViewRow row in dgvDetalleLineasAsignables.Rows)
                if (Convert.ToDouble(row.Cells[1].Value) > 0)
                    result += Convert.ToDouble(row.Cells[1].Value);

            return result;
        }

        private Double calcDisponible(Int32 rowIndex, Int32 colIndex)
        {
            Double total = Convert.ToDouble(txReturnQtyDev.Text);
            Double res = 0;

            foreach (DataGridViewRow row in dgvDetalleLineasAsignables.Rows)
            {
                if (Convert.ToDouble(row.Cells[1].Value) > 0)
                    res += Convert.ToDouble(row.Cells[1].Value);
            }

            if (total > res)
                btCargarLineas.Enabled = false;
            else if (total < res)
            {
                MessageBox.Show("La cantidad que está asignando supera la cantidad devuelta, se revertirá el cambio.", "Asignación mayor a lo facturado", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                dgvDetalleLineasAsignables.Rows[rowIndex].Cells[colIndex].Value = valorPrevioCambio;
                txAvailableQtyDev.Text = asigPrevioCambio.ToString();
                btCargarLineas.Enabled = false;
            }
            else if (total == res)
                btCargarLineas.Enabled = true;

            res = total - res; 
            return res;
        }

        private Boolean devueltoFacturado(Int32 rowIndex, Int32 colIndex)
        {
            Boolean f = false;
            if (!dgvDetalleLineasAsignables.Rows[rowIndex].Cells[0].Value.ToString().Equals(""))
            {
                Double total = Convert.ToDouble(txReturnQtyDev.Text);
                Double Invoiced = Convert.ToDouble(dgvDetalleLineasAsignables.Rows[rowIndex].Cells[0].Value);
                Double qty = Convert.ToDouble(dgvDetalleLineasAsignables.Rows[rowIndex].Cells[colIndex].Value);

                if (qty > Invoiced) //Revisión contra lo facturado
                {
                    MessageBox.Show("La cantidad asignada supera el máximo facturado, no es aplicable la cantidad.", "Asignación mayor a facturado", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    dgvDetalleLineasAsignables.Rows[rowIndex].Cells[colIndex].Value = valorPrevioCambio;
                    txAvailableQtyDev.Text = asigPrevioCambio.ToString();
                }
                else // Validación contra asignación y facturado
                {
                    Double sumaDev = calcDevuelto();

                    if (total < sumaDev)
                    {
                        MessageBox.Show("Con la cantidad asignada el total sería mayor a lo devuelto, revirtiendo cambio.", "Total asignado menor a devuelto", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                        dgvDetalleLineasAsignables.Rows[rowIndex].Cells[colIndex].Value = valorPrevioCambio;
                        txAvailableQtyDev.Text = asigPrevioCambio.ToString();
                    }
                }
            }

            return f;
        }
        
        private void confirmAsignaciones(string partNum)
        {
            Form formConfirm = new Form();
            formConfirm.MaximizeBox = false;
            formConfirm.MinimizeBox = false;
            formConfirm.FormBorderStyle = FormBorderStyle.FixedDialog;
            formConfirm.StartPosition = FormStartPosition.CenterScreen;
            formConfirm.Size = new Size(514, 118);

            Label mensaje = new Label();
            Button btOK = new Button();
            Button btCancel = new Button();

            mensaje.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            mensaje.Text = String.Format("¿Las asignaciones hechas a la parte {0} son correctas?", partNum);
            mensaje.Location = new Point(3, 9);
            mensaje.Size = new Size(491, 20);

            btOK.Text = "Si, continuar";
            btOK.Location = new Point(299, 44);
            btOK.Size = new Size(77, 23);
            btOK.DialogResult = DialogResult.OK;

            btCancel.Text = "Seguir editando";
            btCancel.Location = new Point(391, 44);
            btCancel.Size = new Size(89, 23);
            btCancel.DialogResult = DialogResult.Cancel;

            formConfirm.Controls.Add(mensaje);
            formConfirm.Controls.Add(btOK);
            formConfirm.Controls.Add(btCancel);

            // Ejecución
            formConfirm.ShowDialog();
            if (formConfirm.DialogResult == DialogResult.OK)
            {
                cargarAsignaciones(partNum);
                formConfirm.Dispose();
            }
            else
                formConfirm.Dispose();
        }

        private async void dialogoConfirmacionEvt()
        {
            // Variables
            DataTable dtl = await clsfFacturasNuevoEvento();

            // Fomulario
            Form form1 = new Form();
            form1.Size = new Size(956, 344);
            form1.FormBorderStyle = FormBorderStyle.FixedDialog;
            form1.StartPosition = FormStartPosition.CenterScreen;

            // Componentes
            Label lb1 = new Label();
            Label lb2 = new Label();
            Label lb3 = new Label();
            Button button1 = new Button();
            Button button2 = new Button();
            form1.AcceptButton = button1;
            form1.CancelButton = button2;
            form1.MaximizeBox = false;
            form1.MinimizeBox = false;

            Double r = 0;
            foreach(DataRow rowe in dt2.Rows)
                r += Convert.ToDouble(rowe.ItemArray[10]);

            lb1.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            lb1.Text = "¿Confirma los cambios realizados?";
            lb1.Size = new Size(333, 24);
            lb1.Location = new Point(23, 9);
            lb2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            lb2.Text = String.Format("Devolución Inicial: {0} unidades.", r);
            lb2.Size = new Size(231, 16);
            lb2.Location = new Point(9, 41);
            lb3.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            lb3.Text = String.Format("Devolución con reasignaciones: {0} unidades.", r);
            lb3.Size = new Size(350, 16);
            lb3.Location = new Point(499, 41);
            DataGridView dgvBefore = new DataGridView();
            DataGridView dgvAfter = new DataGridView();

            dgvBefore.Location = new Point(12, 60);
            dgvBefore.Size = new Size(442, 204);
            dgvBefore.DataSource = dtEventRows;
            dgvBefore.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvBefore   .RowHeadersVisible = false;
            dgvBefore.AllowUserToAddRows = false;
            dgvBefore.AllowUserToDeleteRows = false;
            dgvBefore.ReadOnly = true;

            dgvAfter.Location = new Point(485, 60);
            dgvAfter.Size = new Size(442, 204);
            dgvAfter.DataSource = dtl;
            dgvAfter.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvAfter.RowHeadersVisible = false;
            dgvAfter.AllowUserToAddRows = false;
            dgvAfter.AllowUserToDeleteRows = false;
            dgvAfter.ReadOnly = true;

            button1.Text = "Confirmar";
            button1.Location = new Point(757, 279);
            button1.DialogResult = DialogResult.OK;

            button2.Text = "Cancelar";
            button2.Location = new Point(852, 279);
            button2.DialogResult = DialogResult.Cancel;



            // Adición de controles
            form1.Controls.Add(lb1);
            form1.Controls.Add(lb2);
            form1.Controls.Add(lb3);
            form1.Controls.Add(button1);
            form1.Controls.Add(button2);
            form1.Controls.Add(dgvBefore);
            form1.Controls.Add(dgvAfter);
            
            form1.ShowDialog();

            
            if (form1.DialogResult == DialogResult.OK)
            {
                procesarEventoModificado();
                form1.Dispose();
            }
            else
            {
                panelClsfDev.Expanded = false;
                limpiarPanelAsignacion();
                form1.Dispose();
            }
        }
        
        private void btProcesarAsignacionesEvt_Click(object sender, EventArgs e)
        {
            dialogoConfirmacionEvt();
        }

        private async Task<DataTable> clsfFacturasNuevoEvento()
        {
            Boolean existe = false;
            int i = 0;
            List<String> list = new List<string>();
            DataTable LegalNumber = new DataTable();
            dtCustomEvent = dtEventRows.Clone();
            

            foreach (DataRow row in dt2.Rows)
            {
                buscarEnLista(list, row.ItemArray[0].ToString(), out existe); // ¿Ya fue agregada la factura?

                if (existe == false)
                {
                    dtCustomEvent.Rows.Add();
                    await Task.Factory.StartNew(async () =>
                    {
                        LegalNumber = await functions.obtenerNumeroLegal(row.ItemArray[0].ToString(), conEpicor);
                    }).Unwrap();
                    dtCustomEvent.Rows[i][0] = row.ItemArray[0]; // Facturas
                    dtCustomEvent.Rows[i][1] = LegalNumber.Rows[0].ItemArray[0]; // Número Legal
                    dtCustomEvent.Rows[i][2] = dtEventRows.Rows[0].ItemArray[2]; // Cliente
                    dtCustomEvent.Rows[i][3] = 1; // Lineas
                    dtCustomEvent.Rows[i][4] = dtEventRows.Rows[0].ItemArray[4]; // Relación de Cobranza
                    list.Add(row.ItemArray[0].ToString());
                    i++;
                }
                else
                {
                    int x = 0;
                    foreach (DataRow r in dtCustomEvent.Rows)
                    {
                        if (dtCustomEvent.Rows[x].ItemArray[0].ToString().Equals(row.ItemArray[0]))
                            dtCustomEvent.Rows[x][3] = Convert.ToInt32(r.ItemArray[3]) + 1;
                        x++;
                    }
                }
            }
            
            return dtCustomEvent;
        }

        private async void procesarEventoModificado()
        {
            try
            {
                int i = 0;
                file = new FileManager();
                DataTable EventRecords = new DataTable(); // Contiene todos los registros del evento actual
                DataTable dtPrint = new DataTable(); // Contiene los registros filtrados para procesar la factura
                LoaderForm loading = new LoaderForm();

                obtFolioProceso();
                disableButtons();

                // Recorrido del nuevo DataTable del evento modificado
                foreach (DataRow rowCustomEvt in dtCustomEvent.Rows)
                {
                    /* ******************************************* */
                    /* Creación del log de la relacion de cobranza */
                    /* ******************************************* */
                    lblFacturaEnProceso.Text = String.Format("Procesando factura {0}", dtCustomEvent.Rows[i][0]);
                    panelProcesoEvtCustom.Visible = true;
                    string folioRelacion = dtCustomEvent.Rows[i].ItemArray[4].ToString();
                    file.createLog(folioRelacion);
                    file.writeContentToFile("\n");
                    file.writeContentToFile(String.Format("\n[ {1} ] - Comienza la generación de RMA para la factura: {0} ", dtCustomEvent.Rows[i].ItemArray[0].ToString(), System.DateTime.Now));
                    txtFacturasProcesadas.Text += String.Format("Comienza la generación de RMA para la factura: {0}\n", dtCustomEvent.Rows[i].ItemArray[0].ToString());
                    char[] separadores = { ',', ' ' };
                    DataRow[] filasPorFactura = dt2.Select(String.Format("Facturas = {0}", dtCustomEvent.Rows[i].ItemArray[0].ToString()));
                    dtPrint = filasPorFactura.CopyToDataTable();

                    await Task.Factory.StartNew(async () =>
                    {
                        await generarRMA(dtPrint, Convert.ToInt32(dtCustomEvent.Rows[i][0]), dtCustomEvent.Rows[i][1].ToString().Trim(separadores), Convert.ToInt32(Regex.Replace(dtCustomEvent.Rows[i][2].ToString(), @"[^\d]", "", RegexOptions.None, TimeSpan.FromSeconds(1.5))), dtCustomEvent.Rows[i][4].ToString(), idSesion[1], Convert.ToInt32(dtCustomEvent.Rows[i][3]), varEventoID);
                    }).Unwrap();
                    
                    txtFacturasProcesadas.Text += recolectorEventos + "\n";
                    panelProcesoEvtCustom.Visible = false;
                    i++;
                }

                /* ********************************************* */
                /* Actualización de choferes despues del proceso */
                /* ********************************************* */
                MessageBox.Show("Terminó la generación de RMA's", "Proceso terminado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                advTreeDrivers.Nodes.Clear();
                dgvFacturas.Visible = false;
                dgvDetFactura.Visible = false;
                data = new LoadInitData();
                data.Show();
                await Task.Factory.StartNew(async () =>
                {
                    await fillAdvTree();
                }).Unwrap();
                data.Close();

                // Habilitado de botones para cerrar tarimas y turno
                if (btnCierreTurno.Enabled == false)
                {
                    btnCierreTurno.Enabled = true;
                    btnCorte.Enabled = true;
                }

                // Cierre del panel de asignación y limpieza de datos
                limpiarPanelAsignacion();
                panelClsfDev.Expanded = false;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.StackTrace, e.Message, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        private void btnNoAsignar_Click(object sender, EventArgs e)
        {
            limpiarPanelAsignacion();
            panelClsfDev.Expanded = false;
        }

        private void buscarEnLista(List<string> lista, string elemento, out Boolean existe)
        {
            int facturaCargada = lista.FindIndex(delegate (string current)
            {
                return current.Contains(elemento);
            });

            existe = (facturaCargada > -1) ? true : false;
        }

        #endregion
        private async Task fillAdvTree()
        {
            try
            {
                //DataTable sql = util.getRecords("SELECT x.ResponsableRelacion,c.Name,x.Evento_Key FROM TISERVER.DevolucionesTEST.dbo.Choferes c CROSS APPLY(SELECT d.ResponsableRelacion, Evento_Key FROM dbo.MS_DevChfrs_tst d WHERE d.ResponsableRelacion = c.Id)x GROUP BY x.ResponsableRelacion, c.Name, x.Evento_Key ORDER BY x.ResponsableRelacion;", null, conEpicor);
                DataTable sql = util.getRecords(ConfigurationManager.AppSettings["syncChoferes"], null, conEpicor);
                if (!util.catchError.Equals(""))
                    MessageBox.Show(util.catchError);


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
                        nodeEvent = new DevComponents.AdvTree.Node("Cobranza: " + sql.Rows[ind].ItemArray[3].ToString() + "   ID_Evento: " + sql.Rows[ind].ItemArray[2].ToString());
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
                            nodeEvent = new DevComponents.AdvTree.Node("Cobranza: " + sql.Rows[ind].ItemArray[3].ToString() + "   ID_Evento: " + sql.Rows[ind].ItemArray[2].ToString());
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
                                tmpEvent = "Cobranza: " + sql.Rows[ind].ItemArray[3].ToString() + "   ID_Evento: " + sql.Rows[ind].ItemArray[2].ToString();
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
                MessageBox.Show(w.StackTrace, w.Message);
            }
            catch(Exception e)
            {
                MessageBox.Show(e.StackTrace, e.Message);
            }
        }
        
    }
}
