using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using System.Configuration;
using Utilities;
using EpicorAdapters;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using MetroFramework.Forms;

namespace ControlDevoluciones
{
    public partial class PantallaPrincipal : Form
    {
        FileManager file;
        SQLUtilities util = new SQLUtilities();
        Config conf = new Config();
        DataTable dtFacturasPendientes = new DataTable();
        DataTable facturas = new DataTable();
        DataTable dtDetFactura = new DataTable();
        DataTable dtResumenTarimas = new DataTable();
        string folderBase = ConfigurationManager.AppSettings["mainFolder"].ToString();
        string conMultistop = ConfigurationManager.AppSettings["connMultistop"].ToString();
        string conEpicor = ConfigurationManager.AppSettings["connEpicor"].ToString();
        string TISERVER = ConfigurationManager.AppSettings["connRMADB"].ToString();
        public List<String> ReasonsList = new List<string>();
        public List<int> invoiceProcList = new List<int>(); //Lista de facturas ya procesadas
        public List<string> idSesion = new List<string>();
        public List<String> listaFacturasAgg = new List<string>();
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

        private void PantallaPrincipal_Load(object sender, EventArgs e)
        {
            getDrivers();
            listarCarpetas();
            loadSettings();
            sincronizaFacturas();
        }

        private void sincronizaFacturas()
        {
            invoiceProcList.Clear();

            DataTable prueba = util.getRecords("SELECT FactNum FROM tb_FactProc",null,TISERVER);
            for (int i = 0; i < prueba.Rows.Count; i++)
            {
                invoiceProcList.Add(Convert.ToInt32(prueba.Rows[i].ItemArray[0].ToString()));
            }
        }

        private void btnObtRelacion_Click(object sender, EventArgs e)
        {
            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
                btnObtRelacion.Enabled = false;
                dgvFacturas.Visible = false;
                progressBarDev.Visible = true;
                //tabpreviewRMA.Visible = false;
            }
        }

        private void getDrivers()
        {
            string instruccion = ConfigurationManager.AppSettings["obtChoferes"].ToString();
            int index = 0;
            DataTable dt = util.getRecords(instruccion, null, conEpicor);

            foreach (DataRow row in dt.Rows)
            {
                listaChoferes.Items.Add(row[0].ToString() + " - " + row[1].ToString());
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

        private async void btnRMA_Click(object sender, EventArgs e)
        {
            LoaderForm loader = new LoaderForm();
            loader.ShowDialog(); // Presentación de form Loader (Petición a epicor)

            obtFolioProceso(); // Obtener folio de turno y folios de tarimas
            disableButtons();
            await procesarRMA(dgvFacturas, dgvDetFactura, folioProceso); // Generación de RMA por factura en segundo plano
            sincronizaFacturas();
            obtenerFacturas();
            panelFacturasProcesadas.Expanded = true;
            btnCorte.Enabled = true;
            btnCierreTurno.Enabled = true;
        }

        private async Task procesarRMA(DataGridView dgvFacturas, DataGridView dgvDetFactura, string folioTurno)
        {
            file = new FileManager();
            int ind = 0;

            try
            {
                if (!panelFacturasProcesadas.Expanded == false)
                    panelFacturasProcesadas.Expanded = false;

                panelAwaitAsync.Visible = true;

                foreach (DataGridViewRow row in dgvFacturas.Rows)
                {
                    if (Convert.ToBoolean(dgvFacturas.Rows[ind].Cells[0].Value) == true)
                    {
                        getInvoiceDtl(dgvFacturas.Rows[ind].Cells[1].Value.ToString());
                        /*
                        List<string> listFactura = new List<string>();
                        
                        listFactura.Add(dgvFacturas.Rows[ind].Cells[1].Value.ToString()); // Factura
                        listFactura.Add(dgvFacturas.Rows[ind].Cells[2].Value.ToString()); // Número Legal
                        listFactura.Add(dgvFacturas.Rows[ind].Cells[3].Value.ToString()); // Cliente
                        listFactura.Add(dgvFacturas.Rows[ind].Cells[4].Value.ToString()); // Lineas
                        listFactura.Add(dgvFacturas.Rows[ind].Cells[5].Value.ToString()); // Relacion de Cobranza

                        getInvoiceDtl(dgvFacturas.Rows[ind].Cells[1].Value.ToString());

                        string folioRelacion = dgvFacturas.Rows[ind].Cells[5].Value.ToString(); //se obtiene el número de relación para crear el archivo log
                        file.createLog(folioRelacion);
                        file.writeContentToFile("\n");
                        file.writeContentToFile("\nFactura en proceso actual " + dgvFacturas.Rows[ind].Cells[1].Value.ToString());

                        await Task.Factory.StartNew(async () => 
                        {
                            await generarRMA(listFactura, dtDetFactura, Convert.ToInt32(dgvFacturas.Rows[ind].Cells[1].Value), dgvFacturas.Rows[ind].Cells[2].Value.ToString(), Convert.ToInt32(Regex.Replace(dgvFacturas.Rows[ind].Cells[3].Value.ToString(), @"[^\d]", "", RegexOptions.None, TimeSpan.FromSeconds(1.5))),folioRelacion,folioTurno);
                        }).Unwrap();
                        */
                    }
                    ind++;
                }
                //MessageBox.Show(" Finalizó la creación de RMA en Epicor !!", "Proceso completo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //MetroFramework.MetroMessageBox.Show(this, " Finalizó la creación de RMA en Epicor !!", "Proceso completo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                panelAwaitAsync.Visible = false;
            }
            catch(Exception e)
            {
                MessageBox.Show("Terminó la generación de RMA con algunos errores \n Consulte el log de la relación de cobranza para más información", "Proceso completo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                MetroFramework.MetroMessageBox.Show(this, "Terminó la generación de RMA con algunos errores \n Consulte el log de la relación de cobranza para más información", "Proceso completo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                file.writeContentToFile(e.Message);
            }
        }

        private async Task generarRMA(List<string> lFactProcesada , DataTable dt, int factura, string legal, int cliente, string folioRelacion, string folioT)
        {
            try
            {
                string sqlQuery = String.Empty;
                List<string> iRMALine = new List<string>();
                int RMA = 0, ind2 = 0, existencia = 0;

                DataTable dtRMAData = util.getRecords(String.Format(ConfigurationManager.AppSettings["consultarRMA"], cliente, factura), null, conEpicor);
                int parseRMANum = (dtRMAData.Rows.Count == 0) ? 0 : parseRMANum = Convert.ToInt32(dtRMAData.Rows[0].ItemArray[0].ToString());

                EpiFunctions epiAdapter = new EpiFunctions(epiUser, epiPass);

                if (parseRMANum == 0)
                {
                    await epiAdapter.RMAheader(cliente, factura, legal, folioRelacion, folioT);

                    //Una vez creado el encabezado lo devuelvo para la revisión del encabezado
                    DataTable obtainRMAHead = util.getRecords(String.Format(ConfigurationManager.AppSettings["consultarRMA"], cliente, factura), null, conEpicor);
                    RMA = (obtainRMAHead.Rows.Count == 0) ? 0 : Convert.ToInt32(obtainRMAHead.Rows[0].ItemArray[0].ToString());

                    file.writeContentToFile(String.Format(epiAdapter.recolector, RMA));
                    sqlQuery = String.Format("INSERT INTO tb_FactProc(FactNum,NumeroLegal,Cliente,Lineas,Relacion,Status,UsuarioCaptura,FechaCaptura) VALUES({0},'{1}','{2}',{3},'{4}',{5},'{6}',{7});", lFactProcesada[0], lFactProcesada[1], lFactProcesada[2], lFactProcesada[3], lFactProcesada[4], "2", epiUser.ToUpper(), "GETDATE()");
                    util.SQLstatement(sqlQuery, TISERVER, null);
                }
                else
                {
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
                    int numOrden = Convert.ToInt32(dt.Rows[ind2].ItemArray[7]);
                    int lineaOrden = Convert.ToInt32(dt.Rows[ind2].ItemArray[8]);
                    int relOrden = Convert.ToInt32(dt.Rows[ind2].ItemArray[9]);
                    string parte = dt.Rows[ind2].ItemArray[2].ToString();
                    string desc = dt.Rows[ind2].ItemArray[3].ToString();
                    string razon = dt.Rows[ind2].ItemArray[6].ToString();
                    double cant = Convert.ToDouble(dt.Rows[ind2].ItemArray[10]);
                    string UOM = dt.Rows[ind2].ItemArray[11].ToString();
                    int customer = cliente;
                    string almacen = ConfigurationManager.AppSettings["Warehouse"].ToString();
                    string ubicacion = dt.Rows[ind2].ItemArray[13].ToString();
                    string comentarios = "Ruta y Unidad " + dt.Rows[ind2].ItemArray[14].ToString() + ", Área Responsable: " + await areaResponsable(razon);
                    string zona = dt.Rows[ind2].ItemArray[15].ToString();
                    string primbin = dt.Rows[ind2].ItemArray[16].ToString();

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

                        if (iRMALine.Count.Equals(0))
                        {
                            int lineaRMA = existencia + 1;
                            string tarimaDest = await definirTarimaDestino(ubicacion,zona);
                            epiAdapter.RMANewLine(RMA, lineaRMA, legal, factura, lineaFactura, numOrden, lineaOrden, relOrden, parte, desc, razon, cant, UOM, customer, comentarios, almacen, ubicacion, tarimaDest,primbin);
                            file.writeContentToFile(epiAdapter.recolector);
                            ReasonsList.Add(razon.Substring(0, 5)); // Se almacena el motivo de devolucion de la línea actual
                            existencia++;
                        }
                        else
                        {
                            if (!iRMALine[3].Equals(lineaFactura))
                            {
                                int lineaRMA = existencia + 1;
                                string tarimaDest = await definirTarimaDestino(ubicacion,zona);
                                epiAdapter.RMANewLine(RMA, lineaRMA, legal, factura, lineaFactura, numOrden, lineaOrden, relOrden, parte, desc, razon, cant, UOM, customer, comentarios, almacen, ubicacion, tarimaDest,primbin);
                                file.writeContentToFile(epiAdapter.recolector);
                                ReasonsList.Add(razon.Substring(0, 5)); // Se almacena el motivo de devolucion de la línea actual
                                existencia++;
                            }
                            else
                                file.writeContentToFile("Ya existe la parte " + iRMALine[2] + " con número de línea " + iRMALine[3]);
                            ;
                        }
                    }
                    else
                        file.writeContentToFile("La cantidad de la parte " + parte + " debe ser mayor a cero, no se agregará a la RMA.");
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
                txtFacturasProcesadas.Text += "Factura " + factura + " -> RMA: " + RMA + "\n";
                if (!epiAdapter.PartTranException.Equals(""))
                    txtFacturasProcesadas.Text += epiAdapter.PartTranException;
                file.writeContentToFile("Terminó la generación de RMA para la factura " + factura);
            }
            catch (System.IndexOutOfRangeException RMANotFound)
            {
                file.writeContentToFile("Se capturó la siguiente excepción: " + RMANotFound.Message);
                string sql = String.Format("UPDATE tb_FactProc SET Status = 3 WHERE FactNum = {0};", lFactProcesada[0]);
                util.SQLstatement(sql, TISERVER, null);
                ReasonsList.Clear();
                txtFacturasProcesadas.Text += "Error al procesar Factura " + factura + " \n";
            }
            catch (Exception isBug)
            {
                file.writeContentToFile("Excepción capturada " + isBug.Message);
                string sql = String.Format("UPDATE tb_FactProc SET Status = 3 WHERE FactNum = {0};", lFactProcesada[0]);
                util.SQLstatement(sql, TISERVER, null);
                ReasonsList.Clear();
                txtFacturasProcesadas.Text += "Error al procesar Factura " + factura + " \n";
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
            if (ubicacion.Contains("VIR") || ubicacion.Contains("Virtual"))
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
            if (ubicacion.Contains("DEF") || ubicacion.Contains("Def"))
                auxiliar = folioDEF;
            if (ubicacion.Contains("EDA") || ubicacion.Contains("EmpDaño"))
                auxiliar = folioEDA;
            if (ubicacion.Contains("GAR") || ubicacion.Contains("Garantia"))
                auxiliar = folioGAR;
            return auxiliar;
        }

        private void obtenerFacturas()
        {
            try
            {
                int index = 0, index_dt = 0, sumador, noAdmitir = -1;
                listaFacturasAgg.Clear();
                string queryBuild = String.Format(ConfigurationManager.AppSettings["obtInvoices"].ToString(), listaChoferes.SelectedItem.ToString().Substring(0, 5));
                DataTable tabla = util.getRecords(queryBuild, null, conEpicor);
                facturas = tabla.Clone();
                sincronizaFacturas(); //Actualiza facturas en BD Local
                
                /*Se valida que las facturas de la consulta no se hayan procesado antes, de ser asi no se mostrarán al usuario.
                foreach (DataRow row in tabla.Rows)
                {
                    if (tabla.Rows[index].ItemArray[0].ToString().Contains(","))
                    {
                        char delimitador = ',';
                        string[] array = tabla.Rows[index].ItemArray[0].ToString().Split(delimitador);

                        if (!invoiceProcList.Contains(Convert.ToInt32(array[0])))
                            facturas.ImportRow(row);
                    }
                    else
                    {
                        if (!invoiceProcList.Contains(Convert.ToInt32(tabla.Rows[index].ItemArray[0].ToString())))
                            facturas.ImportRow(row);
                    }
                    index++;
                }
                */

                foreach (DataRow row in tabla.Rows)
                {
                    if (index != noAdmitir)
                    {
                        if (index == 0)
                        {
                            facturas.ImportRow(row);
                            index_dt = facturas.Rows.Count;
                        }
                        else
                        {
                            if (tabla.Rows[index].ItemArray[0].ToString().Contains(facturas.Rows[facturas.Rows.Count - 1].ItemArray[0].ToString())) // ¿La siguiente fila contiene el número de factura de la fila actual?
                            {
                                if (tabla.Rows[index].ItemArray[0].ToString().Contains(",")) // ¿Fila actual contiene más de un número de factura?
                                {
                                    char separador = ',';
                                    string[] arrayInvoice = tabla.Rows[index].ItemArray[0].ToString().Split(separador);
                                    string[] arrayLegalNum = tabla.Rows[index].ItemArray[1].ToString().Split(separador);
                                    listaFacturasAgg.Add(tabla.Rows[index].ItemArray[0].ToString());
                                    //MessageBox.Show("Número de facturas: " + arrayInvoice.Length.ToString() + "\n Número de LN: " + arrayLegalNum.Length.ToString());

                                    for (int i = 0; i < arrayInvoice.Length; i++)
                                    {
                                        if ((i + 1) >= arrayInvoice.Length)
                                        {
                                            if (!arrayInvoice[i].Equals(facturas.Rows[facturas.Rows.Count - 1].ItemArray[0].ToString())) // ¿Fact. en el array es diferente a la ultima en DataTable?
                                            {
                                                facturas.ImportRow(row);
                                                facturas.Rows[facturas.Rows.Count - 1][0] = arrayInvoice[i]; // InvoiceNum
                                                facturas.Rows[facturas.Rows.Count - 1][1] = arrayLegalNum[i]; // LegalNumber
                                                index_dt++;
                                            }
                                            // Si el valor de fact. actual es igual al ultimo en DataTable se omite
                                        }
                                        else
                                        {
                                            /* Problemática:
                                             * Si el indice del arreglo es 0 debo validar primero si la factura de la posición actual es diferente a la factura del DataTable que voy a presentar
                                             */ 
                                            if (i == 0)
                                            {
                                                facturas.ImportRow(row);
                                                facturas.Rows[facturas.Rows.Count - 1][0] = arrayInvoice[i]; // InvoiceNum
                                                facturas.Rows[facturas.Rows.Count - 1][1] = arrayLegalNum[i]; // LegalNumber
                                                index_dt++;
                                            }
                                            else
                                            {
                                                if (!arrayInvoice[i].Equals(facturas.Rows[facturas.Rows.Count - 1].ItemArray[0].ToString()))
                                                {
                                                    facturas.ImportRow(row);
                                                    facturas.Rows[facturas.Rows.Count - 1][0] = arrayInvoice[i]; // InvoiceNum
                                                    facturas.Rows[facturas.Rows.Count - 1][1] = arrayLegalNum[i].Substring(1); // LegalNumber
                                                    index_dt++;
                                                }
                                                // Si el valor de fact. actual es igual al ultimo en DataTable se omite
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //facturas.ImportRow(row);
                                    sumador = Convert.ToInt32(facturas.Rows[index_dt].ItemArray[3]) + Convert.ToInt32(tabla.Rows[index].ItemArray[3]);
                                    facturas.Rows[facturas.Rows.Count - 1][3] = sumador;
                                    noAdmitir = index + 1;
                                }
                            }
                            else
                            {
                                if (tabla.Rows[index].ItemArray[0].ToString().Contains(",")) // ¿Fila actual contiene más de un número de factura?
                                {
                                    char separador = ',';
                                    string[] arrayInvoice = tabla.Rows[index].ItemArray[0].ToString().Split(separador);
                                    string[] arrayLegalNum = tabla.Rows[index].ItemArray[1].ToString().Split(separador);
                                    listaFacturasAgg.Add(tabla.Rows[index].ItemArray[0].ToString());
                                    //MessageBox.Show("Número de facturas: " + arrayInvoice.Length.ToString() + "\n Número de LN: " + arrayLegalNum.Length.ToString());

                                    for (int i = 0; i < arrayInvoice.Length; i++)
                                    {
                                        if ((i + 1) >= arrayInvoice.Length)
                                        {
                                            if (!arrayInvoice[i].Equals(facturas.Rows[facturas.Rows.Count - 1].ItemArray[0].ToString())) // ¿Fact. en el array es diferente a la ultima en DataTable?
                                            {
                                                facturas.ImportRow(row);
                                                facturas.Rows[facturas.Rows.Count - 1][0] = arrayInvoice[i]; // InvoiceNum
                                                facturas.Rows[facturas.Rows.Count - 1][1] = arrayLegalNum[i]; // LegalNumber
                                                index_dt++;
                                            }
                                            // Si el valor de fact. actual es igual al ultimo en DataTable se omite
                                        }
                                        else
                                        {
                                            if (i == 0)
                                            {
                                                facturas.ImportRow(row);
                                                facturas.Rows[facturas.Rows.Count - 1][0] = arrayInvoice[i]; // InvoiceNum
                                                facturas.Rows[facturas.Rows.Count - 1][1] = arrayLegalNum[i]; // LegalNumber
                                                index_dt++;
                                            }
                                            else
                                            {
                                                if (!arrayInvoice[i].Equals(facturas.Rows[facturas.Rows.Count - 1].ItemArray[0].ToString()))
                                                {
                                                    facturas.ImportRow(row);
                                                    facturas.Rows[facturas.Rows.Count - 1][0] = arrayInvoice[i]; // InvoiceNum
                                                    facturas.Rows[facturas.Rows.Count - 1][1] = arrayLegalNum[i].Substring(1); // LegalNumber
                                                    index_dt++;
                                                }
                                                // Si el valor de fact. actual es igual al ultimo en DataTable se omite
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    facturas.ImportRow(row);
                                    index_dt = facturas.Rows.Count;
                                }
                            }
                        }
                    }
                    index++;
                }
                dgvFacturas.DataSource = facturas;
                dgvFacturas.Visible = true;
                chkDevolverTodo.Visible = true;
                
            }
            catch (Exception es)
            {
                MessageBox.Show("Exception Found: " + es.Message, "SQLException Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                dgvFacturas.DataSource = facturas;
                dgvFacturas.Visible = true;
                chkDevolverTodo.Visible = true;
            }
        }

        private void getInvoiceDtl(string invoiceHed)
        {
            try
            {
                /*
                string script = String.Format(ConfigurationManager.AppSettings["obtInvoiceDetail"].ToString(), invoiceHed);
                dtDetFactura = util.getRecords(script, null, conEpicor);

                dgvDetFactura.DataSource = dtDetFactura;
                //lblNumLineas.Text = dgvDetFactura.Rows.Count.ToString();
                */
                int indFactLine = 0, indReturnLine = 0, aux = 0,inc = 0;
                String partes = "'";
                DataTable dtLineasDev = util.getRecords(String.Format("SELECT r.Factura,r.IdProducto,x.ShortChar01 AS Clasificacion,r.motivodevolucion,r.CantClsf,r.unidad,r.Observaciones,r.ZoneID,r.PrimBin FROM ERP10DB.dbo.MS_DevChfrs_tst r CROSS APPLY(SELECT c.Key1, c.Character01, c.ShortChar01 FROM ERP10DB.Ice.UD37 c WHERE c.Company = 'DLMAC' AND c.Key5 = 17 AND c.Key1 = r.Clasificacion) x WHERE Factura LIKE '%{0}%' ORDER BY IdProducto;", invoiceHed), null, conEpicor);
                dtLineasDev.Columns.Add("InvoiceLine", typeof(Int32));
                dtLineasDev.Columns.Add("PackNum", typeof(Int32));
                dtLineasDev.Columns.Add("PackLine", typeof(Int32));
                dtLineasDev.Columns.Add("OrderNum", typeof(Int32));
                dtLineasDev.Columns.Add("OrderLine", typeof(Int32));
                dtLineasDev.Columns.Add("OrderRelNum", typeof(Int32));

                foreach (DataRow row in dtLineasDev.Rows)
                {
                    if (indReturnLine == (dtLineasDev.Rows.Count - 1))
                        partes += dtLineasDev.Rows[indReturnLine].ItemArray[1].ToString() + "'";
                    else
                        partes += dtLineasDev.Rows[indReturnLine].ItemArray[1].ToString() + "','";
                    indReturnLine++;
                }
                indReturnLine = 0;

                DataTable dtLineasFactura = util.getRecords(String.Format("SELECT d.PartNum,d.SellingShipQty,d.InvoiceLine,d.PackNum,d.PackLine,d.OrderNum,d.OrderLine,d.OrderRelNum FROM Erp.InvcDtl d WHERE d.InvoiceNum = '{0}' AND d.PartNum IN ({1}) ORDER BY d.PartNum;", invoiceHed, partes), null, conEpicor);

                foreach (DataRow fila in dtLineasFactura.Rows)
                {
                    if (indFactLine == dtLineasFactura.Rows.Count)
                        break;
                    if (Convert.ToInt32(dtLineasFactura.Rows[indFactLine].ItemArray[1]) == Convert.ToInt32(dtLineasDev.Rows[indReturnLine].ItemArray[4])) // ¿Cant. Facturada = Cant. Devuelta?
                    {
                        dtLineasDev.Rows[indReturnLine][9]  = dtLineasFactura.Rows[indFactLine].ItemArray[2].ToString(); // InvoiceLine
                        dtLineasDev.Rows[indReturnLine][10] = dtLineasFactura.Rows[indFactLine].ItemArray[3].ToString(); // PackNum
                        dtLineasDev.Rows[indReturnLine][11] = dtLineasFactura.Rows[indFactLine].ItemArray[4].ToString(); // PackLine
                        dtLineasDev.Rows[indReturnLine][12] = dtLineasFactura.Rows[indFactLine].ItemArray[5].ToString(); // OrderNum
                        dtLineasDev.Rows[indReturnLine][13] = dtLineasFactura.Rows[indFactLine].ItemArray[6].ToString(); // OrderLine
                        dtLineasDev.Rows[indReturnLine][14] = dtLineasFactura.Rows[indFactLine].ItemArray[7].ToString(); // OrderRelNum

                        indReturnLine++;
                        indFactLine++;
                    }
                    else
                    {
                        if(Convert.ToInt32(dtLineasFactura.Rows[indFactLine].ItemArray[1]) > Convert.ToInt32(dtLineasDev.Rows[indReturnLine].ItemArray[4])) // ¿Cant. Facturada mayor a devuelta?
                        {
                            aux = Convert.ToInt32(dtLineasFactura.Rows[indFactLine].ItemArray[1]);
                            do
                            {
                                if (dtLineasDev.Rows[indReturnLine].ItemArray[1].ToString().Equals(dtLineasDev.Rows[indReturnLine + 1].ItemArray[1].ToString())) // ¿Es la misma parte?
                                {
                                    if (dtLineasDev.Rows[indReturnLine].ItemArray[2].ToString().Equals(dtLineasDev.Rows[indReturnLine + 1].ItemArray[2].ToString())) // ¿Tienen la misma clasificación?
                                    {
                                        inc = Convert.ToInt32(dtLineasDev.Rows[indReturnLine].ItemArray[4]) + Convert.ToInt32(dtLineasDev.Rows[indReturnLine + 1].ItemArray[4]);
                                        aux -= inc;
                                        indReturnLine++;
                                    }
                                    else
                                    {
                                        dtLineasDev.Rows[indReturnLine][9] = dtLineasFactura.Rows[indFactLine].ItemArray[2].ToString(); // InvoiceLine
                                        dtLineasDev.Rows[indReturnLine][10] = dtLineasFactura.Rows[indFactLine].ItemArray[3].ToString(); // PackNum
                                        dtLineasDev.Rows[indReturnLine][11] = dtLineasFactura.Rows[indFactLine].ItemArray[4].ToString(); // PackLine
                                        dtLineasDev.Rows[indReturnLine][12] = dtLineasFactura.Rows[indFactLine].ItemArray[5].ToString(); // OrderNum
                                        dtLineasDev.Rows[indReturnLine][13] = dtLineasFactura.Rows[indFactLine].ItemArray[6].ToString(); // OrderLine
                                        dtLineasDev.Rows[indReturnLine][14] = dtLineasFactura.Rows[indFactLine].ItemArray[7].ToString(); // OrderRelNum

                                        inc = Convert.ToInt32(dtLineasDev.Rows[indReturnLine].ItemArray[4]);
                                        aux -= inc;

                                        indReturnLine++;
                                    }
                                }
                                else
                                {
                                    dtLineasDev.Rows[indReturnLine][9] = dtLineasFactura.Rows[indFactLine].ItemArray[2].ToString(); // InvoiceLine
                                    dtLineasDev.Rows[indReturnLine][10] = dtLineasFactura.Rows[indFactLine].ItemArray[3].ToString(); // PackNum
                                    dtLineasDev.Rows[indReturnLine][11] = dtLineasFactura.Rows[indFactLine].ItemArray[4].ToString(); // PackLine
                                    dtLineasDev.Rows[indReturnLine][12] = dtLineasFactura.Rows[indFactLine].ItemArray[5].ToString(); // OrderNum
                                    dtLineasDev.Rows[indReturnLine][13] = dtLineasFactura.Rows[indFactLine].ItemArray[6].ToString(); // OrderLine
                                    dtLineasDev.Rows[indReturnLine][14] = dtLineasFactura.Rows[indFactLine].ItemArray[7].ToString(); // OrderRelNum
                                    aux = 0;
                                    indReturnLine++;
                                }
                            }
                            while (aux > 0);

                            MessageBox.Show("Cantidad sumada => " + inc + "\n Cantidad Facturada => " + aux);
                            inc = 0;
                            aux = 0;
                            indFactLine++;
                        }
                        else
                        {
                            aux = Convert.ToInt32(dtLineasDev.Rows[indReturnLine].ItemArray[4]);
                            do
                            {
                                if (dtLineasFactura.Rows[indFactLine].ItemArray[0].ToString().Equals(dtLineasDev.Rows[indReturnLine].ItemArray[1].ToString()))
                                {
                                    inc = Convert.ToInt32(dtLineasFactura.Rows[indFactLine].ItemArray[1]);
                                    aux -= inc;
                                    if (aux >= 0)
                                    {
                                        if (dtLineasDev.Rows[indReturnLine].ItemArray[9].ToString().Equals(""))
                                        {
                                            dtLineasDev.Rows[indReturnLine][4] = dtLineasFactura.Rows[indFactLine].ItemArray[1].ToString(); // ReturnQty
                                            dtLineasDev.Rows[indReturnLine][9] = dtLineasFactura.Rows[indFactLine].ItemArray[2].ToString(); // InvoiceLine
                                            dtLineasDev.Rows[indReturnLine][10] = dtLineasFactura.Rows[indFactLine].ItemArray[3].ToString(); // PackNum
                                            dtLineasDev.Rows[indReturnLine][11] = dtLineasFactura.Rows[indFactLine].ItemArray[4].ToString(); // PackLine
                                            dtLineasDev.Rows[indReturnLine][12] = dtLineasFactura.Rows[indFactLine].ItemArray[5].ToString(); // OrderNum
                                            dtLineasDev.Rows[indReturnLine][13] = dtLineasFactura.Rows[indFactLine].ItemArray[6].ToString(); // OrderLine
                                            dtLineasDev.Rows[indReturnLine][14] = dtLineasFactura.Rows[indFactLine].ItemArray[7].ToString(); // OrderRelNum
                                            indFactLine++;
                                        }
                                        else
                                        {
                                            dtLineasDev.Rows.Add(); // Se duplica la fila para la siguiente iteración
                                            dtLineasDev.Rows[dtLineasDev.Rows.Count - 1][0] = dtLineasDev.Rows[indReturnLine].ItemArray[0].ToString(); // InvoiceNum
                                            dtLineasDev.Rows[dtLineasDev.Rows.Count - 1][1] = dtLineasDev.Rows[indReturnLine].ItemArray[1].ToString(); // PartNum
                                            dtLineasDev.Rows[dtLineasDev.Rows.Count - 1][2] = dtLineasDev.Rows[indReturnLine].ItemArray[2].ToString(); // Clasificación
                                            dtLineasDev.Rows[dtLineasDev.Rows.Count - 1][3] = dtLineasDev.Rows[indReturnLine].ItemArray[3].ToString(); // ReturnReasonCode
                                            dtLineasDev.Rows[dtLineasDev.Rows.Count - 1][5] = dtLineasDev.Rows[indReturnLine].ItemArray[5].ToString(); // UOM
                                            dtLineasDev.Rows[dtLineasDev.Rows.Count - 1][6] = dtLineasDev.Rows[indReturnLine].ItemArray[6].ToString(); // Note
                                            dtLineasDev.Rows[dtLineasDev.Rows.Count - 1][7] = dtLineasDev.Rows[indReturnLine].ItemArray[7].ToString(); // ZoneID
                                            dtLineasDev.Rows[dtLineasDev.Rows.Count - 1][8] = dtLineasDev.Rows[indReturnLine].ItemArray[8].ToString(); // PrimBin

                                            dtLineasDev.Rows[dtLineasDev.Rows.Count - 1][4] = dtLineasFactura.Rows[indFactLine].ItemArray[1].ToString(); // ReturnQty
                                            dtLineasDev.Rows[dtLineasDev.Rows.Count - 1][9] = dtLineasFactura.Rows[indFactLine].ItemArray[2].ToString(); // InvoiceLine
                                            dtLineasDev.Rows[dtLineasDev.Rows.Count - 1][10] = dtLineasFactura.Rows[indFactLine].ItemArray[3].ToString(); // PackNum
                                            dtLineasDev.Rows[dtLineasDev.Rows.Count - 1][11] = dtLineasFactura.Rows[indFactLine].ItemArray[4].ToString(); // PackLine
                                            dtLineasDev.Rows[dtLineasDev.Rows.Count - 1][12] = dtLineasFactura.Rows[indFactLine].ItemArray[5].ToString(); // OrderNum
                                            dtLineasDev.Rows[dtLineasDev.Rows.Count - 1][13] = dtLineasFactura.Rows[indFactLine].ItemArray[6].ToString(); // OrderLine
                                            dtLineasDev.Rows[dtLineasDev.Rows.Count - 1][14] = dtLineasFactura.Rows[indFactLine].ItemArray[7].ToString(); // OrderRelNum
                                            indFactLine++;
                                        }
                                    }
                                }
                            }
                            while (aux > 0);

                            indReturnLine++;
                        }   
                    }
                }
                dgvDetFactura.DataSource = dtLineasDev;
            }
            catch (System.Data.SqlClient.SqlException sqlError)
            {
                MessageBox.Show("Excepción capturada ... \n" + sqlError, "SQLException", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception scriptError)
            {
                MessageBox.Show("Excepción capturada ... \n" + scriptError, "Error al mostrar líneas de la factura", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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

        /*=========================================================================================================*/
        /*========================          Funciones async Backgrounworker         ===============================*/
        /*=========================================================================================================*/

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            for (int i = 1; i <= 100; i++)
            {
                progressBarDev.ProgressTextVisible = true;
                System.Threading.Thread.Sleep(5);
                backgroundWorker1.ReportProgress(i);

                if (backgroundWorker1.CancellationPending)
                    return;
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBarDev.Value = e.ProgressPercentage;
            progressBarDev.Text = e.ProgressPercentage + "%";
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBarDev.Value = 0;
            progressBarDev.Visible = false;
            progressBarDev.ProgressTextVisible = false;
            obtenerFacturas();
        }

        /*=========================================================================================================*/
        /*========================          Fin funciones async Backgrounworker     ===============================*/
        /*=========================================================================================================*/

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
            chkDevolverTodo.Visible = false;
            //txtChofer.Text = listaChoferes.SelectedItem.ToString();
            //Console.Write(listaChoferes.SelectedItem.ToString());
        }

        private void labelItem1_Click(object sender, EventArgs e)
        {

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

        private void superTabControlPanel1_Click(object sender, EventArgs e)
        {

        }

        private void PantallaPrincipal_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void listarCarpetas()
        {
            DirectoryInfo oDirectorio = new DirectoryInfo(@"C:\DevolucionesLOG");

            foreach (DirectoryInfo subdirectorios in oDirectorio.GetDirectories())
            {
                listCarpetas.Items.Add(subdirectorios.FullName);
            }

        }

        private void listarArchivos(string ruta)
        {
            DirectoryInfo oDirectorio = new DirectoryInfo(@ruta);

            //obtengo ls ficheros contenidos en la ruta
            foreach (FileInfo file in oDirectorio.GetFiles())
            {
                listadoArchivos.Items.Add(file.Name);
            }
        }

        private void listCarpetas_ItemClick(object sender, EventArgs e)
        {
            lbRuta.Visible = false;
            lbRuta.Text = listCarpetas.SelectedItem.ToString();
            listadoArchivos.Items.Clear();
            listarArchivos(listCarpetas.SelectedItem.ToString());
            btnGetPDF.Enabled = false;
            btnOpenLog.Enabled = false;
        }

        private void listadoArchivos_ItemClick(object sender, EventArgs e)
        {
            btnGetPDF.Enabled = true;
            btnOpenLog.Enabled = true;
        }

        private void btnRefreshLog_Click(object sender, EventArgs e)
        {
            listCarpetas.Items.Clear();
            listarCarpetas();
        }

        private void backgroundWorker_Process_DoWork(object sender, DoWorkEventArgs e)
        {
            DialogResult result = DialogResult.No;
            DoOnUIThread(delegate ()
            {
                modalForm f = new modalForm();
                //f.ShowDialog();
                result = f.ShowDialog();
            });
        }

        private void DoOnUIThread(MethodInvoker d)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(d);
            }
            else
            {
                d();
            }
        }

        private void backgroundWorker_Process_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //DialogResult fin = DialogResult.No;
            DoOnUIThread(delegate ()
            {
                modalForm x = new modalForm();
                x.Close();
            });
        }

        private void btnGetPDF_Click(object sender, EventArgs e)
        {
            string rutaArchivo = lbRuta.Text + "\\" + listadoArchivos.SelectedItem.ToString();
            //string pdfPath = Path.Combine(Application.StartupPath, rutaArchivo);
            //Process.Start(pdfPath);

            file = new FileManager();
            file.printPDF(rutaArchivo);
        }

        private void chkDevolverTodo_CheckedChanged(object sender, EventArgs e)
        {
            int pos = 0;

            if (chkDevolverTodo.Checked)
            {
                foreach (DataGridViewRow row in dgvFacturas.Rows)
                {
                    dgvFacturas.Rows[pos].Cells[0].Value = true;
                    btnRMA.Enabled = true;
                    pos++;
                }
            }
            else
            {
                foreach (DataGridViewRow row in dgvFacturas.Rows)
                {
                    dgvFacturas.Rows[pos].Cells[0].Value = false;
                    btnRMA.Enabled = false;
                    pos++;
                }
            }
        }

        private void btnOpenLog_Click(object sender, EventArgs e)
        {
            string rutaArchivo = lbRuta.Text + "\\" + listadoArchivos.SelectedItem.ToString();
            string pdfPath = Path.Combine(Application.StartupPath, rutaArchivo);
            Process.Start(pdfPath);
        }

        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            conf.UpdateSettings("folioTurno", txFolioGral.Text);
        }

        private void btnCorte_Click(object sender, EventArgs e)
        {
            CierreTarimas modal = new CierreTarimas();
            modal.folioGral = folioProceso;
            modal.epiUser = epiUser;
            modal.ShowDialog();
        }

        public void actualizarTarimas(string folio)
        {
            if (folio.Contains("VIR"))
                folioVIR = folio + "***";
            if (folio.Contains("BES"))
                folioBES = folio + "***";
            if (folio.Contains("DEF"))
                folioDEF = folio + "***";
            if (folio.Contains("EDA"))
                folioEDA = folio + "***";
            if (folio.Contains("GAR"))
                folioGAR = folio + "***";

            MessageBox.Show(folioVIR + "\n" + folioBES + "\n" + folioDEF + "\n" + folioEDA + "\n" + folioGAR);
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
                dtResumenTarimas = util.getRecords(String.Format("SELECT d.RMANum AS RMA,x.InvoiceNum AS NotaCred,d.FolioTarima_c AS Tarima,d.PartNum AS Parte,d.LineDesc AS Descripcion,d.ReturnQty AS Cant,d.ReturnQtyUOM AS UOM,d.InvoiceNum AS Factura,d.PrimBin_c AS Ubicacion FROM RMADtl d CROSS APPLY (SELECT i.InvoiceNum, i.XRefInvoiceNum FROM Erp.InvcHead i WHERE i.RMANum = d.RMANum AND i.InvoiceRef = d.InvoiceNum) x WHERE FolioTarima_c LIKE '%{0}%' GROUP BY d.RMANum,x.InvoiceNum,d.FolioTarima_c,d.PartNum,d.LineDesc,d.ReturnQty,d.ReturnQtyUOM,d.InvoiceNum,d.PrimBin_c ORDER BY d.RMANum;", idSesion[1]), null, conEpicor);
                file = new FileManager();


                await file.exportTable(dtResumenTarimas, idSesion[1], epiUser.ToUpper());
            }
            catch (System.IO.IOException fileException)
            {
                MessageBox.Show(fileException.Message);
            }
        }

        private void PantallaPrincipal_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        private void btnObtParts_Click(object sender, EventArgs e)
        {
            DataTable d = util.getRecords(String.Format("SELECT RMANum,PartNum,FolioTarima_c,PrimBin_c,ReturnQty,ReturnQtyUOM FROM RMADtl WHERE RMANum = {0} AND PartNum = '{1}' ORDER BY RMANum;", txRMANumToTransfer.Text,txPartNumToTransfer.Text), null, conEpicor);
            dgvPartesTraspaso.DataSource = d;
        }

        private void btnTraspaso_Click(object sender, EventArgs e)
        {
            int i = 0;
            DateTime fecha = DateTime.Now.AddDays(-4);
            EpiFunctions epicor = new EpiFunctions(epiUser, epiPass);
            foreach (DataGridViewRow row in dgvPartesTraspaso.Rows)
            {
                epicor.transferToBinSource("IC", "BueEstado", "DLMAC", dgvPartesTraspaso.Rows[i].Cells[3].Value.ToString(), dgvPartesTraspaso.Rows[i].Cells[1].Value.ToString(), Convert.ToDouble(dgvPartesTraspaso.Rows[i].Cells[4].Value), dgvPartesTraspaso.Rows[i].Cells[5].Value.ToString());
                i++;
            }
            if (!epicor.recolector.Equals(""))
                MetroFramework.MetroMessageBox.Show(this,epicor.recolector,"EpicorException Found",MessageBoxButtons.OK,MessageBoxIcon.Hand);
        }
    }
}
