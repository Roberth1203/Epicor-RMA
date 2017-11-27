using System;
using System.Data;
using System.Windows.Forms;
using System.Configuration;
using System.ServiceModel.Channels;
using Epicor.ServiceModel.StandardBindings;
using Ice.Proxy.BO;
using Ice.Lib;
using Erp.Proxy.BO;
using Erp.BO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities;

namespace EpicorAdapters
{
    public class EpiFunctions
    {
        public string recolector;
        public string PartTranException;
        public int CMreturn;
        Credenciales cred;
        protected string fileSys;
        protected string getCompany;

        public EpiFunctions(string environment, string company, string user, string pass)
        {
            cred = new Credenciales();
            fileSys = String.Format(ConfigurationManager.AppSettings["epiEnvironment"].ToString(),environment);
            getCompany = company;
            cred.username = user;
            cred.password = pass;
        }
        
        public EpiFunctions(string user, string pass, string company)
        {
            cred = new Credenciales();
            fileSys = ConfigurationManager.AppSettings["epiConfig"].ToString();
            cred.username = user;
            cred.password = pass;
        }

        public EpiFunctions(string user, string pass)
        {
            cred = new Credenciales();
            getCompany = ConfigurationManager.AppSettings["epiCompany"].ToString();
            fileSys = ConfigurationManager.AppSettings["epiConfig"].ToString();
            cred.username = user;
            cred.password = pass;

            setCompany(getCompany);
        }

        public EpiFunctions(string company)
        {
            getCompany = company;
            fileSys = ConfigurationManager.AppSettings["epiConfig"].ToString();
            cred.username = "manager";
            cred.password = "!15LiveTI";
        }

        public void setCompany(string currentCompany)
        {
            try
            {
                string appServerUrl = string.Empty;

                EnvironmentInformation.ConfigurationFileName = fileSys;
                appServerUrl = AppSettingsHandler.AppServerUrl;
                CustomBinding wcfBinding = NetTcp.UsernameWindowsChannel();
                Uri CustSvcUri = new Uri(String.Format("{0}/Ice/BO/{1}.svc", appServerUrl, "UserFile"));

                using (Ice.Proxy.BO.UserFileImpl US = new Ice.Proxy.BO.UserFileImpl(wcfBinding, CustSvcUri))
                {
                    US.ClientCredentials.UserName.UserName = cred.username;
                    US.ClientCredentials.UserName.Password = cred.password;
                    US.SaveSettings(cred.username, true, currentCompany, true, false, true, true, true, true, true, true, true,
                                               false, false, -2, 0, 1456, 886, 2, "MAINMENU", "", "", 0, -1, 0, "", false);
                    US.Close();
                    US.Dispose();
                }
            }
            catch (System.UnauthorizedAccessException loginError)
            {
                MessageBox.Show("Error producido " + loginError.Message,"Error de Inicio de Sesión",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }

        public async Task RMAheader(int CustNum,int InvoiceNum,string legalNumber,string FolioRelacion,string FolioTurno)
        {
            try
            {
                string appServerUrl = string.Empty;
                recolector = String.Empty;

                EnvironmentInformation.ConfigurationFileName = fileSys;
                appServerUrl = AppSettingsHandler.AppServerUrl;
                CustomBinding wcfBinding = new CustomBinding();
                wcfBinding = NetTcp.UsernameWindowsChannel();
                Uri CustSvcUri = new Uri(string.Format("{0}/Erp/BO/{1}.svc", appServerUrl, "RMAProc"));
                using (RMAProcImpl OB = new RMAProcImpl(wcfBinding, CustSvcUri))
                {
                    OB.ClientCredentials.UserName.UserName = cred.username;
                    OB.ClientCredentials.UserName.Password = cred.password;
                    RMAProcDataSet dt = new RMAProcDataSet();
                    OB.GetNewRMAHead(dt);

                    dt.Tables["RMAHead"].Rows[0]["CustNum"] = CustNum;
                    dt.Tables["RMAHead"].Rows[0]["BTCustNum"] = CustNum;
                    dt.Tables["RMAHead"].Rows[0]["InvoiceNum"] = InvoiceNum;
                    dt.Tables["RMAHead"].Rows[0]["LegalNumber"] = legalNumber;
                    dt.Tables["RMAHead"].Rows[0]["FolioRelacion_c"] = FolioRelacion;
                    dt.Tables["RMAHead"].Rows[0]["FolioPalet_c"] = FolioTurno;

                    OB.Update(dt);
                }
                recolector = "Se ha creado la RMA {0} para la factura " + InvoiceNum;
            }
            catch (Ice.Common.BusinessObjectException epicor)
            {
                recolector = "Se encontro un error al crear la RMA. " + epicor.Message;
            }
            catch (Exception ex)
            {
                recolector = "Ocurrió un error al generar el encabezado de la RMA " + ex.Message;
            }
        }

        public void RMANewLine(int RMANum, int RMAline, string RMAlegalnumber,int RMAInvoiceNum,int RMAInvoiceLine, int RMAOrderNum, int RMAOrderLine, int RMAOrderRelNum, string RMAPartNum, string RMALineDesc,string RMAReason,double RMAReturnQty, string RMAReturnUOM, int RMACustNum, string RMANote, string WarehouseCode, string BinNum, string folioTarimaAsignada, string primbin)
        {
            try
            {
                string appServerUrl = string.Empty;
                string CMmsg = String.Empty;
                int CMNum, CMLine;

                recolector = String.Empty;
                EnvironmentInformation.ConfigurationFileName = fileSys;
                appServerUrl = AppSettingsHandler.AppServerUrl;
                CustomBinding wcfBinding = new CustomBinding();
                wcfBinding = NetTcp.UsernameWindowsChannel();
                Uri CustSvcUri = new Uri(string.Format("{0}/Erp/BO/{1}.svc", appServerUrl, "RMAProc"));
                using (RMAProcImpl BO = new RMAProcImpl(wcfBinding, CustSvcUri))
                {
                    BO.ClientCredentials.UserName.UserName = cred.username;
                    BO.ClientCredentials.UserName.Password = cred.password;
                    RMAProcDataSet dataset = new RMAProcDataSet(); //DataSet para la generación de la línea
                    RMAProcDataSet dsRecepcion = new RMAProcDataSet(); //DataSet para la recepción de marcancía
                    RMAProcDataSet dsCreditMemo = new RMAProcDataSet(); //DataSet para la Nota de crédito
                    RMAProcDataSet temporal = new RMAProcDataSet();
                    RMAProcDataSet ds = new RMAProcDataSet();

                    //Saltar la comprobación de constraints en tabblas de Epicor
                    dataset.EnforceConstraints = false;
                    dsRecepcion.EnforceConstraints = false;
                    dsCreditMemo.EnforceConstraints = false;
                    ds.EnforceConstraints = false;

                    //Se cargan los datos de la línea a agregar
                    
                    BO.GetNewRMADtl(dataset, RMANum);
                        dataset.Tables["RMADtl"].Rows[0]["LegalNumber"] = RMAlegalnumber;
                        dataset.Tables["RMADtl"].Rows[0]["InvoiceNum"] = RMAInvoiceNum;
                        dataset.Tables["RMADtl"].Rows[0]["InvoiceLine"] = RMAInvoiceLine;
                        dataset.Tables["RMADtl"].Rows[0]["OrderNum"] = RMAOrderNum;
                        dataset.Tables["RMADtl"].Rows[0]["OrderLine"] = RMAOrderLine;
                        dataset.Tables["RMADtl"].Rows[0]["OrderRelNum"] = RMAOrderRelNum;
                        dataset.Tables["RMADtl"].Rows[0]["PartNum"] = RMAPartNum;
                        dataset.Tables["RMADtl"].Rows[0]["LineDesc"] = RMALineDesc;
                        dataset.Tables["RMAdtl"].Rows[0]["ReturnReasonCode"] = RMAReason.Substring(0, 5);
                        dataset.Tables["RMADtl"].Rows[0]["ReasonDescription"] = RMAReason;
                        dataset.Tables["RMADtl"].Rows[0]["ReturnQty"] = Convert.ToDecimal(RMAReturnQty);
                        dataset.Tables["RMADtl"].Rows[0]["ReturnQtyUOM"] = RMAReturnUOM;
                        dataset.Tables["RMADtl"].Rows[0]["CustNum"] = RMACustNum;
                        dataset.Tables["RMADtl"].Rows[0]["Note"] = RMANote;
                        dataset.Tables["RMADtl"].Rows[0]["FolioTarima_c"] = folioTarimaAsignada;
                        dataset.Tables["RMADtl"].Rows[0]["PrimBin_c"] = primbin;
                    BO.Update(dataset);
                    //se cargan los datos de la recepción
                    BO.GetNewRMARcpt(dsRecepcion, RMANum, RMAline);
                        dsRecepcion.Tables["RMARcpt"].Rows[0]["RcvDate"] = DateTime.Now.Date;
                        dsRecepcion.Tables["RMARcpt"].Rows[0]["ReceivedQty"] = RMAReturnQty;
                        dsRecepcion.Tables["RMARcpt"].Rows[0]["ReceivedQtyUOM"] = RMAReturnUOM;
                        dsRecepcion.Tables["RMARcpt"].Rows[0]["WareHouseCode"] = WarehouseCode; // Almacen destino para generar la disposición
                        dsRecepcion.Tables["RMARcpt"].Rows[0]["BinNum"] = BinNum; // Ubicación sobre el almacen destino
                    BO.Update(dsRecepcion);

                    //Se agrega una línea a la nota de crédito
                    dsCreditMemo = BO.GetByID(RMANum);
                    BO.RMACreditAdd(RMANum, RMAline, false, out CMNum, out CMLine, out CMmsg, dsCreditMemo);
                    CMreturn = CMNum;
                    //cambiarDocType(memo);
                }

                recolector = "Se agregó la parte " + RMAPartNum + " correctamente";
            }
            catch (Ice.Common.BusinessObjectException epicor)
            {
                recolector = "Se encontro un error al cargar la línea a la RMA. " + epicor.Message;
            }
            catch (Exception isError)
            {
                recolector = "Se encontró un error al crear la línea: " + isError.Message;
            }
        }

        public void cambiarDocType(int CreditMemo)
        {
            try
            {
                string appServerUrl = string.Empty;
                recolector = String.Empty;

                EnvironmentInformation.ConfigurationFileName = fileSys;
                appServerUrl = AppSettingsHandler.AppServerUrl;
                CustomBinding wcfBinding = new CustomBinding();
                wcfBinding = NetTcp.UsernameWindowsChannel();
                Uri CustSvcUri = new Uri(string.Format("{0}/Erp/BO/{1}.svc", appServerUrl, "ARInvoice"));
                using (ARInvoiceImpl OB = new ARInvoiceImpl(wcfBinding, CustSvcUri))
                {
                    OB.ClientCredentials.UserName.UserName = cred.username;
                    OB.ClientCredentials.UserName.Password = cred.password;
                    ARInvoiceDataSet dsInvoice = new ARInvoiceDataSet();
                    dsInvoice.EnforceConstraints = false;

                    dsInvoice = OB.GetByID(CreditMemo);
                    OB.OnChangeTranDocTypeID("NCXDEV", dsInvoice);
                    OB.Update(dsInvoice);
                }
            }
            catch (Ice.Common.BusinessObjectException epicor)
            {
                recolector = "Se encontro un error al cargar la línea a la RMA. " + epicor.Message;
            }
            catch (Exception isError)
            {
                recolector = "Se encontró un error al crear la línea: " + isError.Message;
            }
        }

        public void changeDocType(int cm)
        {
            try
            {
                recolector = String.Empty;
                SQLUtilities sql = new SQLUtilities();
                sql.SQLstatement(String.Format("UPDATE Erp.InvcHead SET TranDocTypeID = '{0}',PMUID = {1} WHERE Company = 'DLMAC' AND InvoiceNum = {2}", "NCXDEV", 12, cm), ConfigurationManager.AppSettings["connEpicor"].ToString());
                recolector = "El tipo de documento en la Nota de crédito " + cm + " se cambió a NCXDEV";
            }
            catch (System.Data.SqlClient.SqlException sqlError)
            {
                recolector = "Ocurrió un error al cambiar el tipo de documento en la Nota de Crédito, permanecerá con su valor por defecto" + sqlError.Message;
            }
            catch (Exception error)
            {
                recolector = "Error al cambiar TranDocType " + error.Message;
            }
        }

        public int getRMANum(int Customer, int Invoice)
        {
            try
            {
                string appServerUrl = string.Empty;
                bool morePages = false;
                
                EnvironmentInformation.ConfigurationFileName = fileSys;
                appServerUrl = AppSettingsHandler.AppServerUrl;
                CustomBinding wcfBinding = new CustomBinding();
                wcfBinding = NetTcp.UsernameWindowsChannel();
                Uri CustSvcUri = new Uri(string.Format("{0}/Erp/BO/{1}.svc", appServerUrl, "RMAProc"));
                using (RMAProcImpl BO = new RMAProcImpl(wcfBinding, CustSvcUri))
                {
                    RMAHeadListDataSet d = new RMAHeadListDataSet();
                    BO.ClientCredentials.UserName.UserName = cred.username;
                    BO.ClientCredentials.UserName.Password = cred.password;
                    d = BO.GetList("CustNum = " + Customer + " AND InvoiceNum = " + Invoice + " AND OpenRMA = 1", 0, 1, out morePages);
                    
                    if (Convert.ToInt32(d.Tables["RMAHeadList"].Rows[0]["RMANum"].ToString()) > 0)
                    {
                        int aux = Convert.ToInt32(d.Tables["RMAHeadList"].Rows[0]["RMANum"].ToString());
                        return aux;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
            catch (System.IndexOutOfRangeException)
            {
                return 0;
            }
        }

        public List<string> RMALineExist(int RMANum, string PartNum, int InvoiceNum)
        {
            try
            {
                string appServerUrl = string.Empty;
                //bool pages = false;
                List<string> listaDatos = new List<string>();

                EnvironmentInformation.ConfigurationFileName = fileSys;
                appServerUrl = AppSettingsHandler.AppServerUrl;
                CustomBinding wcfBinding = new CustomBinding();
                wcfBinding = NetTcp.UsernameWindowsChannel();
                Uri CustSvcUri = new Uri(string.Format("{0}/Erp/BO/{1}.svc", appServerUrl, "RMAProc"));
                using (RMAProcImpl BO = new RMAProcImpl(wcfBinding, CustSvcUri))
                {
                    BO.ClientCredentials.UserName.UserName = cred.username;
                    BO.ClientCredentials.UserName.Password = cred.password;
                    DataSet getLine = BO.GetByID(RMANum);

                    if (getLine.Tables["RMADtl"].Rows.Count > 0)
                    {
                        int index = 0;
                        foreach (DataRow row in getLine.Tables["RMADtl"].Rows)
                        {
                            if (getLine.Tables["RMADtl"].Rows[index]["PartNum"].ToString().Equals(PartNum))
                            {
                                listaDatos.Add(getLine.Tables["RMADtl"].Rows[index]["RMANum"].ToString());
                                listaDatos.Add(getLine.Tables["RMADtl"].Rows[index]["RMALine"].ToString());
                                listaDatos.Add(getLine.Tables["RMADtl"].Rows[index]["PartNum"].ToString());
                                listaDatos.Add(getLine.Tables["RMADtl"].Rows[index]["invoiceLine"].ToString());
                                break;
                            }
                            index++;
                        }
                    }
                    return listaDatos;
                }
            }
            catch(System.IndexOutOfRangeException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        public void armaRMADisp(int rma, List<string> listaLineas)
        {
            string appServerUrl = string.Empty;
            int CurrentRMA = rma;
            int index = 0, nLinea = 1;

            EnvironmentInformation.ConfigurationFileName = fileSys;
            appServerUrl = AppSettingsHandler.AppServerUrl;
            CustomBinding wcfBinding = new CustomBinding();
            wcfBinding = NetTcp.UsernameWindowsChannel();
            Uri CustSvcUri = new Uri(string.Format("{0}/Erp/BO/{1}.svc", appServerUrl, "RMAProc"));
            using (RMAProcImpl BO = new RMAProcImpl(wcfBinding, CustSvcUri))
            {
                BO.ClientCredentials.UserName.UserName = cred.username;
                BO.ClientCredentials.UserName.Password = cred.password;
                //RMAProcDataSet ds = new RMAProcDataSet();
                DataSet datosRcpt = BO.GetByID(rma);

                if (listaLineas.Count > 0)
                {
                    foreach (string row in listaLineas)
                    {
                        string disposedQty = datosRcpt.Tables["RMARcpt"].Rows[index]["ReceivedQty"].ToString();
                        string dispQtyUOM = datosRcpt.Tables["RMARcpt"].Rows[index]["ReceivedQtyUOM"].ToString();
                        CreateRMADisp(CurrentRMA, listaLineas[index], nLinea, disposedQty, dispQtyUOM);
                        index++;
                        nLinea++;
                    }
                }
            }
        }

        public void CreateRMADisp(int CurrentRMA,string motivoDevolucion,int linea, string dispQty, string dispQtyUOM)
        {
            try
            {
                string appServerUrl = String.Empty;
                recolector = String.Empty;
                string message,tgWarehouse,tgBinNum,tgPartNum;
                bool UserInput;

                EnvironmentInformation.ConfigurationFileName = fileSys;
                appServerUrl = AppSettingsHandler.AppServerUrl;
                CustomBinding wcfBinding = new CustomBinding();
                wcfBinding = NetTcp.UsernameWindowsChannel();
                Uri CustSvcUri = new Uri(string.Format("{0}/Erp/BO/{1}.svc", appServerUrl, "RMADisp"));
                using (RMADispImpl BO = new RMADispImpl(wcfBinding, CustSvcUri))
                {
                    BO.ClientCredentials.UserName.UserName = cred.username;
                    BO.ClientCredentials.UserName.Password = cred.password;
                    DataSet exist = BO.GetByID(CurrentRMA, linea, 1);
                    if(exist.Tables["RMADisp"].Rows.Count < 1)
                    {
                        
                        RMADispDataSet disposicion = new RMADispDataSet();
                        disposicion.EnforceConstraints = false;
                        
                        BO.GetNewRMADisp(disposicion, CurrentRMA, linea, 1);
                        BO.ChangeDispType(disposicion, "INS-STK", out message);
                        BO.GetLegalNumGenOpts(disposicion, out UserInput);

                        disposicion.Tables["RMADisp"].Rows[0]["Company"] = "DLMAC";
                        disposicion.Tables["RMADisp"].Rows[0]["DispQty"] = Convert.ToDecimal(dispQty);
                        disposicion.Tables["RMADisp"].Rows[0]["DispQtyUOM"] = dispQtyUOM;
                        //disposicion.Tables["RMADisp"].Rows[0]["ReasonCode"] = motivoDevolucion;
                        disposicion.Tables["RMADisp"].Rows[0]["RequestMove"] = 0;
                        disposicion.Tables["RMADisp"].Rows[0]["InspectedBy"] = "2"; // Se define el ID del inspector, en este caso es Marco antonio Suarez Mendoza
                        BO.Update(disposicion);
                        

                        RMADispDataSet dispData = BO.GetByID(CurrentRMA, linea, 1);

                        //disposicion.Tables["RMADisp"].Rows[0]["WarehouseCode"] = dispData.Tables["RMADisp"].Rows[0]["ToWareHouseCode"].ToString();
                        //disposicion.Tables["RMADisp"].Rows[0]["BinNum"] = dispData.Tables["RMADisp"].Rows[0]["ToBinNum"].ToString();
                        tgPartNum = dispData.Tables["RMADisp"].Rows[0]["PartNum"].ToString();
                        tgWarehouse = dispData.Tables["RMADisp"].Rows[0]["ToWareHouseCode"].ToString();
                        tgBinNum = dispData.Tables["RMADisp"].Rows[0]["ToBinNum"].ToString();

                        disposicion.Tables["RMADisp"].Rows[0]["WarehouseCode"] = dispData.Tables["RMADisp"].Rows[0]["WareHouseCode"].ToString();
                        disposicion.Tables["RMADisp"].Rows[0]["BinNum"] = dispData.Tables["RMADisp"].Rows[0]["BinNum"].ToString();
                        //BO.Update(disposicion);
                        if (disposicion.Tables["RMADisp"].Rows[0]["BinNum"].ToString().Equals("BueEstado"))
                        {
                            transferToBinSource(disposicion.Tables["RMADisp"].Rows[0]["WarehouseCode"].ToString(), disposicion.Tables["RMADisp"].Rows[0]["BinNum"].ToString(), tgWarehouse, tgBinNum, tgPartNum, Convert.ToDouble(dispQty), dispQtyUOM);
                            recolector = "Las líneas en BESTADO han sido transferidas a su ubicación primaria";
                        }
                    }
                    else
                        recolector = "Ya existe una disposición sobre la línea " + linea;
                }
            }
            catch (System.IndexOutOfRangeException ex)
            {
                recolector = "Ocurrió un problema al momento de hacer la disposición " + ex.Message;
            }
        }

        public void transferToBinSource(string sourceWarehouse, string sourceBinNum, string targetWarehouse, string targetBinNum, string partNum, double TransferQty, string qtyUOM)
        {
            FileManager fileIO = new FileManager();

            try
            {
                string appServerUrl = string.Empty;
                recolector = String.Empty;
                string legalMessage, parTranPK;
                PartTranException = String.Empty;

                EnvironmentInformation.ConfigurationFileName = fileSys;
                appServerUrl = AppSettingsHandler.AppServerUrl;
                CustomBinding wcfBinding = new CustomBinding();
                wcfBinding = NetTcp.UsernameWindowsChannel();
                Uri CustSvcUri = new Uri(string.Format("{0}/Erp/BO/{1}.svc", appServerUrl, "InvTransfer"));
                using (InvTransferImpl BO = new InvTransferImpl(wcfBinding, CustSvcUri))
                {
                    BO.ClientCredentials.UserName.UserName = cred.username;
                    BO.ClientCredentials.UserName.Password = cred.password;

                    InvTransferDataSet dsTransfer = BO.GetTransferRecord(partNum, qtyUOM);
                    dsTransfer.Tables["InvTrans"].Rows[0]["Company"] = "DLMAC";
                    dsTransfer.Tables["InvTrans"].Rows[0]["PartNum"] = partNum;
                    dsTransfer.Tables["InvTrans"].Rows[0]["FromWarehouseCode"] = sourceWarehouse;
                    dsTransfer.Tables["InvTrans"].Rows[0]["FromBinNum"] = sourceBinNum;
                    dsTransfer.Tables["InvTrans"].Rows[0]["ToWarehouseCode"] = targetWarehouse;
                    dsTransfer.Tables["InvTrans"].Rows[0]["ToBinNum"] = targetBinNum;
                    dsTransfer.Tables["InvTrans"].Rows[0]["TransferQty"] = TransferQty;
                    dsTransfer.Tables["InvTrans"].Rows[0]["TransferQtyUOM"] = qtyUOM;
                    dsTransfer.Tables["InvTrans"].Rows[0]["TranReference"] = "Traspaso de RMA por Buen Estado";
                    BO.CommitTransfer(dsTransfer, out legalMessage, out parTranPK);
                    BO.Dispose();
                }
                fileIO.writeContentToFile(" Transferencia a la ubicación " + targetBinNum + " de la parte " + partNum + " realizada correctamente.");
                
            }
            catch (Ice.Common.EpicorServerException serverSideError)
            {
                fileIO.writeContentToFile(String.Format("Error al realizar el traspaso de la parte {0} \n {1} ", partNum, serverSideError.Message));
                PartTranException += String.Format("Error al enviar la parte {0} a la ubicación primaria \n",partNum);
            }
            catch (Exception generalException)
            {
                fileIO.writeContentToFile(String.Format("Error al realizar el traspaso de la parte {0} \n {1} ", partNum, generalException.Message));
                PartTranException += String.Format("Error al enviar la parte {0} a la ubicación primaria \n", partNum);
            }
        }
    }
}