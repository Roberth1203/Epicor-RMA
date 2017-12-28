using System;
using System.Data;
using System.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities;

namespace ControlDevoluciones
{
    public class Orchestador
    {
        public string catcher;
        public DataTable dtFacturas = new DataTable();
        SQLUtilities sql = new SQLUtilities();

        public async Task<DataTable> LinesToDetail(string evento, string EpiConnection)
        {
            DataTable dtEventRows = new DataTable();
            DataTable dtFilterRows = new DataTable();
            try
            {
                dtEventRows = sql.getRecords(String.Format("SELECT r.Facturas,''  AS Linea,r.IdProducto,''  AS Descripcion,''  AS Empaque,''  AS LineaE,r.motivodevolucion,''  AS Orden,''  AS LineaO,''  AS Relacion,r.DistrDev,r.unidad,r.DistrClsf,r.Observaciones,r.ZoneID,r.PrimBin FROM ERP10DB.dbo.MS_DevChfrs_tst r WHERE Evento_Key = '{0}' ORDER BY r.IdProducto, r.Facturas;",evento),null,EpiConnection);
                return dtEventRows;
            }
            catch (Exception) { return dtEventRows; }
        }

        public async Task<DataTable> obtenerFacturas(string EventKey, string EpiConnection)
        {
            try
            {
                List<string> FacturasCargadas = new List<string>();
                if (dtFacturas.Rows.Count > 0)
                    dtFacturas.Clear();

                catcher = String.Empty;
                int x = 0,y = 0;
                char[] separadores = { ':', ',' };
                bool flag = false;
                string query = ConfigurationManager.AppSettings["obtInvoices"].ToString();
                DataTable result = sql.getRecords(String.Format(query, EventKey), null, EpiConnection);
                dtFacturas = result.Clone(); // Se clona el formato del DataTable result a dtFacturas que es donde se irán agregando las filas al final de las validaciones

                do
                {
                    int iLegales = 0;
                    if (dtFacturas.Rows.Count == 0) //Primera iteración del ciclo
                    {
                        if (result.Rows[x].ItemArray[5].ToString().Equals("")) //Si el campo DistrDev está vacío la línea se omite
                            x++;
                        else
                        {
                            string[] zFacturas = result.Rows[x].ItemArray[5].ToString().Trim().Split(separadores);
                            string[] zNLegales = result.Rows[x].ItemArray[1].ToString().Trim().Split(separadores);
                            if (zFacturas[4].Equals("")) // Si la posición 4 está vacía entonces solo aparece una vez en la factura y se importa la linea completa
                            {
                                dtFacturas.ImportRow(result.Rows[x]);
                                dtFacturas.Rows[y][0] = zFacturas[0];
                                dtFacturas.Rows[y][1] = zNLegales[iLegales];
                                FacturasCargadas.Add(zFacturas[0]);
                                x++;

                                Console.WriteLine("Fila " + y + " -> " + dtFacturas.Rows[y].ItemArray[0].ToString() + " " + dtFacturas.Rows[y].ItemArray[1].ToString() + " " + dtFacturas.Rows[y].ItemArray[2].ToString() + " " + dtFacturas.Rows[y].ItemArray[3].ToString() + " " + dtFacturas.Rows[y].ItemArray[4].ToString() + " " + dtFacturas.Rows[y].ItemArray[5].ToString());
                            }
                            else //Si en la primer iteración hay más de una línea a devolver
                            {
                                int i = 0;
                                do
                                {
                                    if (dtFacturas.Rows.Count == 0)
                                    {
                                        dtFacturas.ImportRow(result.Rows[x]);
                                        FacturasCargadas.Add(zFacturas[i].Trim());
                                        dtFacturas.Rows[y][0] = zFacturas[i].Trim();
                                        dtFacturas.Rows[y][1] = zNLegales[iLegales].Trim();
                                        iLegales++;

                                        i += 4;

                                        Console.WriteLine("Fila " + y + " -> " + dtFacturas.Rows[y].ItemArray[0].ToString() + " " + dtFacturas.Rows[y].ItemArray[1].ToString() + " " + dtFacturas.Rows[y].ItemArray[2].ToString() + " " + dtFacturas.Rows[y].ItemArray[3].ToString() + " " + dtFacturas.Rows[y].ItemArray[4].ToString() + " " + dtFacturas.Rows[y].ItemArray[5].ToString());
                                    }
                                    else
                                    {
                                        int facturaCargada = -1;
                                        facturaCargada = FacturasCargadas.FindIndex(delegate (string current)
                                        {
                                            return current.Contains(zFacturas[i].Trim());
                                        });

                                        if (facturaCargada >= 0) //Si se encuentra la factura
                                        {
                                            dtFacturas.Rows[facturaCargada][3] = Convert.ToInt32(dtFacturas.Rows[y].ItemArray[3]) + 1;
                                            //x++;
                                            i += 4;
                                            Console.WriteLine("Fila " + y + " -> " + dtFacturas.Rows[facturaCargada].ItemArray[0].ToString() + " " + dtFacturas.Rows[facturaCargada].ItemArray[1].ToString() + " " + dtFacturas.Rows[facturaCargada].ItemArray[2].ToString() + " " + dtFacturas.Rows[facturaCargada].ItemArray[3].ToString() + " " + dtFacturas.Rows[facturaCargada].ItemArray[4].ToString() + " " + dtFacturas.Rows[facturaCargada].ItemArray[5].ToString());
                                        }
                                        /*
                                        if (dtFacturas.Rows[y].ItemArray[0].ToString().Contains(zFacturas[i].Trim())) // Validar si la factura de la ultima fila en dtFacturas es igual a la factura en result
                                        {
                                            dtFacturas.Rows[y][3] = Convert.ToInt32(dtFacturas.Rows[y].ItemArray[3]) + 1;
                                            i += 4;

                                            Console.WriteLine("Fila " + y + " -> " + dtFacturas.Rows[y].ItemArray[0].ToString() + " " + dtFacturas.Rows[y].ItemArray[1].ToString() + " " + dtFacturas.Rows[y].ItemArray[2].ToString() + " " + dtFacturas.Rows[y].ItemArray[3].ToString() + " " + dtFacturas.Rows[y].ItemArray[4].ToString() + " " + dtFacturas.Rows[y].ItemArray[5].ToString());
                                        }*/
                                        else // Si las facturas no coinciden, se agrega una nueva línea para esa factura
                                        {
                                            dtFacturas.ImportRow(result.Rows[x]);
                                            y++;
                                            FacturasCargadas.Add(zFacturas[i].Trim());
                                            dtFacturas.Rows[y][0] = zFacturas[i].Trim();
                                            dtFacturas.Rows[y][1] = zNLegales[iLegales].Trim();
                                            iLegales++;

                                            i += 4;

                                            Console.WriteLine("Fila " + y + " -> " + dtFacturas.Rows[y].ItemArray[0].ToString() + " " + dtFacturas.Rows[y].ItemArray[1].ToString() + " " + dtFacturas.Rows[y].ItemArray[2].ToString() + " " + dtFacturas.Rows[y].ItemArray[3].ToString() + " " + dtFacturas.Rows[y].ItemArray[4].ToString() + " " + dtFacturas.Rows[y].ItemArray[5].ToString());
                                        }
                                    }
                                }
                                while (i < (zFacturas.Length - 1));

                                x++;
                            }
                        }
                    }
                    else // Segunda iteración en adelante
                    {
                        if (result.Rows[x].ItemArray[5].ToString().Equals("")) // Si DistrDev es vacío la línea completa se omite
                            x++;
                        else
                        {
                            string[] xFacturas = result.Rows[x].ItemArray[5].ToString().Trim().Split(separadores);
                            string[] xNLegales = result.Rows[x].ItemArray[1].ToString().Trim().Split(separadores);

                            if (xFacturas[4].ToString().Equals("")) // Si la POS 4 es vacía entonces solo hay una línea a devolver
                            {
                                int facturaCargada = -1;
                                facturaCargada = FacturasCargadas.FindIndex(delegate (string current)
                                {
                                    return current.Contains(xFacturas[0]);
                                });

                                if (facturaCargada >= 0) //Si se encuentra la factura
                                {
                                    dtFacturas.Rows[facturaCargada][3] = Convert.ToInt32(dtFacturas.Rows[y].ItemArray[3]) + 1;
                                    x++;
                                    Console.WriteLine("Fila " + y + " -> " + dtFacturas.Rows[facturaCargada].ItemArray[0].ToString() + " " + dtFacturas.Rows[facturaCargada].ItemArray[1].ToString() + " " + dtFacturas.Rows[facturaCargada].ItemArray[2].ToString() + " " + dtFacturas.Rows[facturaCargada].ItemArray[3].ToString() + " " + dtFacturas.Rows[facturaCargada].ItemArray[4].ToString() + " " + dtFacturas.Rows[facturaCargada].ItemArray[5].ToString());
                                }
                                /*
                                if (dtFacturas.Rows[y].ItemArray[0].ToString().Contains(xFacturas[0])) // Si la ultima fila de dtFacturas tiene la misma factura que la línea actual en result
                                {
                                    dtFacturas.Rows[y][3] = Convert.ToInt32(dtFacturas.Rows[y].ItemArray[3]) + 1;
                                    x++;

                                    Console.WriteLine("Fila " + y + " -> " + dtFacturas.Rows[y].ItemArray[0].ToString() + " " + dtFacturas.Rows[y].ItemArray[1].ToString() + " " + dtFacturas.Rows[y].ItemArray[2].ToString() + " " + dtFacturas.Rows[y].ItemArray[3].ToString() + " " + dtFacturas.Rows[y].ItemArray[4].ToString() + " " + dtFacturas.Rows[y].ItemArray[5].ToString());
                                }*/
                                else
                                {
                                    dtFacturas.ImportRow(result.Rows[x]);
                                    y++;
                                    FacturasCargadas.Add(xFacturas[0]);
                                    dtFacturas.Rows[y][0] = xFacturas[0];
                                    dtFacturas.Rows[y][1] = xNLegales[iLegales];
                                    iLegales++;
                                    x++;

                                    Console.WriteLine("Fila " + y + " -> " + dtFacturas.Rows[y].ItemArray[0].ToString() + " " + dtFacturas.Rows[y].ItemArray[1].ToString() + " " + dtFacturas.Rows[y].ItemArray[2].ToString() + " " + dtFacturas.Rows[y].ItemArray[3].ToString() + " " + dtFacturas.Rows[y].ItemArray[4].ToString() + " " + dtFacturas.Rows[y].ItemArray[5].ToString());
                                }
                            }
                            else // Si hay más de una línea a devolver
                            {
                                int i = 0;
                                do
                                {
                                    int facturaCargada = -1;
                                    facturaCargada = FacturasCargadas.FindIndex(delegate (string current)
                                    {
                                        return current.Contains(xFacturas[i].Trim());
                                    });

                                    if (facturaCargada >= 0) //Si se encuentra la factura
                                    {
                                        dtFacturas.Rows[facturaCargada][3] = Convert.ToInt32(dtFacturas.Rows[y].ItemArray[3]) + 1;
                                        //x++;
                                        i += 4;
                                        Console.WriteLine("Fila " + y + " -> " + dtFacturas.Rows[facturaCargada].ItemArray[0].ToString() + " " + dtFacturas.Rows[facturaCargada].ItemArray[1].ToString() + " " + dtFacturas.Rows[facturaCargada].ItemArray[2].ToString() + " " + dtFacturas.Rows[facturaCargada].ItemArray[3].ToString() + " " + dtFacturas.Rows[facturaCargada].ItemArray[4].ToString() + " " + dtFacturas.Rows[facturaCargada].ItemArray[5].ToString());
                                    }
                                    /*
                                    if (dtFacturas.Rows[y].ItemArray[0].ToString().Contains(xFacturas[i].Trim())) // Validar si la factura de la ultima fila en dtFacturas es igual a la factura en result
                                    {
                                        dtFacturas.Rows[y][3] = Convert.ToInt32(dtFacturas.Rows[y].ItemArray[3]) + 1;
                                        i += 4;

                                        Console.WriteLine("Fila " + y + " -> " + dtFacturas.Rows[y].ItemArray[0].ToString() + " " + dtFacturas.Rows[y].ItemArray[1].ToString() + " " + dtFacturas.Rows[y].ItemArray[2].ToString() + " " + dtFacturas.Rows[y].ItemArray[3].ToString() + " " + dtFacturas.Rows[y].ItemArray[4].ToString() + " " + dtFacturas.Rows[y].ItemArray[5].ToString());
                                    }*/
                                    else // Si las facturas no coinciden, se agrega una nueva línea para esa factura
                                    {
                                        dtFacturas.ImportRow(result.Rows[x]);
                                        y++;
                                        FacturasCargadas.Add(xFacturas[i].Trim());
                                        dtFacturas.Rows[y][0] = xFacturas[i].Trim();
                                        dtFacturas.Rows[y][1] = xNLegales[iLegales].Trim();
                                        iLegales++;

                                        i += 4;

                                        Console.WriteLine("Fila " + y + " -> " + dtFacturas.Rows[y].ItemArray[0].ToString() + " " + dtFacturas.Rows[y].ItemArray[1].ToString() + " " + dtFacturas.Rows[y].ItemArray[2].ToString() + " " + dtFacturas.Rows[y].ItemArray[3].ToString() + " " + dtFacturas.Rows[y].ItemArray[4].ToString() + " " + dtFacturas.Rows[y].ItemArray[5].ToString());
                                    }
                                }
                                while (i < (xFacturas.Length - 1));

                                x++;
                            }
                        }
                    }
                }
                while (x < result.Rows.Count);

                dtFacturas.Columns.Remove("DistrDev");
                return dtFacturas;
            }
            catch (Exception)
            {
                return dtFacturas;
            }
        }

        //public async Task<DataTable> getInvoiceDtl(Int32 lineaFactura, string EpiConnection, string currentInvoice)
        public async Task<DataTable> getInvoiceDtl(DataTable InvoiceLines, string EpiConnection, string currentInvoice)
        {
            catcher = String.Empty;
            Boolean firstExist = true;
            DataTable dtLineasDev = new DataTable();
            DataTable dtLineasFac = new DataTable();
            try
            {
                int x = 0, totalRows = 0;
                char[] separadores = {':',','};
                //int invoiceHead = Convert.ToInt32(dtFacturas.Rows[lineaFactura].ItemArray[0]);
                //string script = String.Format(ConfigurationManager.AppSettings["obtInvoiceDetail"].ToString(), invoiceHead);
                //dtLineasDev = sql.getRecords(script, null, EpiConnection);
                dtLineasDev = InvoiceLines;
                totalRows = dtLineasDev.Rows.Count;
                
                do
                {
                    int indiceDev = 0, indiceFac = 0;
                    string[] detDevolucion = dtLineasDev.Rows[x].ItemArray[10].ToString().Split(separadores); // DistrDev
                    string[] detClasificar = dtLineasDev.Rows[x].ItemArray[12].ToString().Split(separadores); // DistrClsf
                    if (x == 0) // Primer iteración
                    {
                        if (detDevolucion[4].Trim().Equals("")) // Si solo hay una línea a devolver
                        {
                            if (detDevolucion[0].Trim().Equals(currentInvoice)) //Reviso si el numero de factura corresponde al seleccionado
                            {
                                if (detClasificar[3].Trim().Equals("")) // Si solo hay una clasificación destino
                                {
                                    dtLineasDev.Rows[x][0] = detDevolucion[0].Trim(); // InvoiceNum
                                    dtLineasDev.Rows[x][1] = detDevolucion[1]; // InvoiceLine
                                    dtLineasDev.Rows[x][10] = detClasificar[2]; // ReturnQty
                                    dtLineasDev.Rows[x][12] = await asignarClasificacion(detClasificar[1], EpiConnection); // BinNum(Clasificacion)
                                    dtLineasFac = await complementoFactura(detDevolucion[0].Trim(), detDevolucion[1], EpiConnection);

                                    if (dtLineasFac.Rows.Count > 0)
                                    {
                                        dtLineasDev.Rows[x][3] = dtLineasFac.Rows[0].ItemArray[1]; // LineDesc
                                        dtLineasDev.Rows[x][4] = dtLineasFac.Rows[0].ItemArray[2]; // PackNum
                                        dtLineasDev.Rows[x][5] = dtLineasFac.Rows[0].ItemArray[3]; // PackLine
                                        dtLineasDev.Rows[x][7] = dtLineasFac.Rows[0].ItemArray[4]; // OrderNum
                                        dtLineasDev.Rows[x][8] = dtLineasFac.Rows[0].ItemArray[5]; // OrderLine
                                        dtLineasDev.Rows[x][9] = dtLineasFac.Rows[0].ItemArray[6]; // OrderRelease
                                    }
                                    x++;
                                }
                                else // Si la parte se devuelve a más de una clasificación
                                {
                                    int contClasificaciones = 0;
                                    bool isLastRow = ((x + 1) == totalRows) ? true : false;

                                    do
                                    {
                                        if (indiceFac == 0) // Primer iteración
                                        {
                                            dtLineasDev.Rows[x][0] = detDevolucion[indiceDev].Trim(); // InvoiceNum
                                            dtLineasDev.Rows[x][1] = detDevolucion[indiceDev + 1]; // InvoiceLine
                                            dtLineasDev.Rows[x][10] = detClasificar[indiceFac + 2]; // ReturnQty
                                            dtLineasDev.Rows[x][12] = await asignarClasificacion(detClasificar[indiceFac + 1], EpiConnection); // BinNum(Clasificacion)
                                            dtLineasFac = await complementoFactura(detDevolucion[indiceDev].Trim(), detDevolucion[indiceDev + 1], EpiConnection);

                                            if (dtLineasFac.Rows.Count > 0)
                                            {
                                                dtLineasDev.Rows[x][3] = dtLineasFac.Rows[0].ItemArray[1]; // LineDesc
                                                dtLineasDev.Rows[x][4] = dtLineasFac.Rows[0].ItemArray[2]; // PackNum
                                                dtLineasDev.Rows[x][5] = dtLineasFac.Rows[0].ItemArray[3]; // PackLine
                                                dtLineasDev.Rows[x][7] = dtLineasFac.Rows[0].ItemArray[4]; // OrderNum
                                                dtLineasDev.Rows[x][8] = dtLineasFac.Rows[0].ItemArray[5]; // OrderLine
                                                dtLineasDev.Rows[x][9] = dtLineasFac.Rows[0].ItemArray[6]; // OrderRelease
                                            }

                                            indiceFac += 3;
                                        }
                                        else // Segunda iteración en adelante
                                        {
                                            // Para devolver la misma parte a otra clasificación, de crea una fila auxiliar a la cual cargar los datos para posteriormente agregarla a dtLineasDev
                                            DataRow filaDuplex = dtLineasDev.NewRow();

                                            filaDuplex[0] = detDevolucion[indiceDev].Trim(' '); // InvoiceNum
                                            filaDuplex[1] = detDevolucion[indiceDev + 1]; // InvoiceLine
                                            filaDuplex[2] = dtLineasDev.Rows[x].ItemArray[2].ToString(); // PartNum
                                            filaDuplex[6] = dtLineasDev.Rows[x].ItemArray[6].ToString(); // ReturnReasonCode
                                            filaDuplex[10] = detClasificar[indiceFac + 2]; // ReturnQty
                                            filaDuplex[11] = dtLineasDev.Rows[x].ItemArray[11].ToString(); // ReturnQtyUOM
                                            filaDuplex[12] = await asignarClasificacion(detClasificar[indiceFac + 1], EpiConnection); // BinNum(Clasificacion)
                                            filaDuplex[13] = dtLineasDev.Rows[x].ItemArray[13].ToString(); // Note
                                            filaDuplex[14] = dtLineasDev.Rows[x].ItemArray[14].ToString(); // ZoneID
                                            filaDuplex[15] = dtLineasDev.Rows[x].ItemArray[15].ToString(); // PrimBin

                                            dtLineasFac = await complementoFactura(detDevolucion[0].Trim(), detDevolucion[1], EpiConnection);
                                            if (dtLineasFac.Rows.Count > 0)
                                            {
                                                filaDuplex[3] = dtLineasFac.Rows[0].ItemArray[1];
                                                filaDuplex[4] = dtLineasFac.Rows[0].ItemArray[2];
                                                filaDuplex[5] = dtLineasFac.Rows[0].ItemArray[3];
                                                filaDuplex[7] = dtLineasFac.Rows[0].ItemArray[4];
                                                filaDuplex[8] = dtLineasFac.Rows[0].ItemArray[5];
                                                filaDuplex[9] = dtLineasFac.Rows[0].ItemArray[6];
                                            }
                                            dtLineasDev.Rows.Add(filaDuplex);
                                            //totalRows = dtLineasDev.Rows.Count; // Se actualiza el total de filas del DataTable a devolver

                                            contClasificaciones++;
                                            indiceFac += 3;
                                        }
                                    }
                                    while (indiceFac < (detClasificar.Length - 1)); // El ciclo termina hasta que el indice de clasificaciones sea igual al tamaño del array

                                    if (isLastRow == true)
                                        x += contClasificaciones; // Se incrementa el número de filas
                                    else
                                        x++;
                                }
                            }
                            else // Si la factura es diferente a la seleccionada
                            {
                                x++;
                            }
                        }
                        else // Si hay varias líneas a devolver
                        {
                            if (detClasificar[3].Trim().Equals("")) // Si todas las lineas se devuelven a la misma clasificacion
                            {
                                int conteoLineas = 0;
                                bool isLastRow = ((x + 1) == totalRows) ? true : false;
                                do
                                {
                                    if (indiceDev == 0) // Primer iteración
                                    {
                                        if (detDevolucion[indiceDev].Trim().Equals(currentInvoice)) // Reviso que la factura coincida con la factura seleecionada
                                        {
                                            dtLineasDev.Rows[x][0] = detDevolucion[indiceDev].Trim(); // InvoiceNum
                                            dtLineasDev.Rows[x][1] = detDevolucion[indiceDev + 1]; // InvoiceLine
                                            dtLineasDev.Rows[x][10] = detDevolucion[indiceDev + 3]; // ReturnQty

                                            dtLineasDev.Rows[x][12] = await asignarClasificacion(detClasificar[indiceFac + 1], EpiConnection); // BinNum(Clasificacion)
                                            dtLineasFac = await complementoFactura(detDevolucion[indiceDev].Trim(), detDevolucion[indiceDev + 1], EpiConnection);

                                            if (dtLineasFac.Rows.Count > 0)
                                            {
                                                dtLineasDev.Rows[x][3] = dtLineasFac.Rows[0].ItemArray[1]; // LineDesc
                                                dtLineasDev.Rows[x][4] = dtLineasFac.Rows[0].ItemArray[2]; // PackNum
                                                dtLineasDev.Rows[x][5] = dtLineasFac.Rows[0].ItemArray[3]; // PackLine
                                                dtLineasDev.Rows[x][7] = dtLineasFac.Rows[0].ItemArray[4]; // OrderNum
                                                dtLineasDev.Rows[x][8] = dtLineasFac.Rows[0].ItemArray[5]; // OrderLine
                                                dtLineasDev.Rows[x][9] = dtLineasFac.Rows[0].ItemArray[6]; // OrderRelease
                                            }
                                            indiceDev += 4;
                                        }
                                        else // Si la factura es dieferente a la seleccionada
                                        {
                                            firstExist = false;
                                            indiceDev += 4;
                                        }
                                    }
                                    else // Segunda iteración en adelante
                                    {
                                        if (detDevolucion[indiceDev].Trim().Equals(currentInvoice)) // Reviso que la factura coincida con la factura seleccionada
                                        {
                                            if (firstExist == false)
                                            {
                                                dtLineasDev.Rows[x][0] = detDevolucion[indiceDev].Trim(); // InvoiceNum
                                                dtLineasDev.Rows[x][1] = detDevolucion[indiceDev + 1]; // InvoiceLine
                                                dtLineasDev.Rows[x][10] = detDevolucion[indiceDev + 3]; // ReturnQty

                                                dtLineasDev.Rows[x][12] = await asignarClasificacion(detClasificar[indiceFac + 1], EpiConnection); // BinNum(Clasificacion)
                                                dtLineasFac = await complementoFactura(detDevolucion[indiceDev].Trim(), detDevolucion[indiceDev + 1], EpiConnection);

                                                if (dtLineasFac.Rows.Count > 0)
                                                {
                                                    dtLineasDev.Rows[x][3] = dtLineasFac.Rows[0].ItemArray[1]; // LineDesc
                                                    dtLineasDev.Rows[x][4] = dtLineasFac.Rows[0].ItemArray[2]; // PackNum
                                                    dtLineasDev.Rows[x][5] = dtLineasFac.Rows[0].ItemArray[3]; // PackLine
                                                    dtLineasDev.Rows[x][7] = dtLineasFac.Rows[0].ItemArray[4]; // OrderNum
                                                    dtLineasDev.Rows[x][8] = dtLineasFac.Rows[0].ItemArray[5]; // OrderLine
                                                    dtLineasDev.Rows[x][9] = dtLineasFac.Rows[0].ItemArray[6]; // OrderRelease
                                                }
                                                indiceDev += 4;
                                                firstExist = true;
                                            }
                                            else
                                            {
                                                // Para devolver la misma parte a otra clasificación, de crea una fila auxiliar a la cual cargar los datos para posteriormente agregarla a dtLineasDev
                                                DataRow filaDuplex = dtLineasDev.NewRow();

                                                filaDuplex[0] = detDevolucion[indiceDev].Trim(); // InvoiceNum
                                                filaDuplex[1] = detDevolucion[indiceDev + 1]; // InvoiceLine
                                                filaDuplex[2] = dtLineasDev.Rows[x].ItemArray[2].ToString(); // PartNum
                                                filaDuplex[6] = dtLineasDev.Rows[x].ItemArray[6].ToString(); // ReturnReasonCode
                                                filaDuplex[10] = detDevolucion[indiceDev + 3]; // ReturnQty
                                                filaDuplex[11] = dtLineasDev.Rows[x].ItemArray[11].ToString(); // ReturnQtyUOM
                                                filaDuplex[12] = await asignarClasificacion(detClasificar[indiceFac + 1], EpiConnection); // BinNum(Clasificacion)
                                                filaDuplex[13] = dtLineasDev.Rows[x].ItemArray[13].ToString(); // Note
                                                filaDuplex[14] = dtLineasDev.Rows[x].ItemArray[14].ToString(); // ZoneID
                                                filaDuplex[15] = dtLineasDev.Rows[x].ItemArray[15].ToString(); // PrimBin

                                                dtLineasFac = await complementoFactura(detDevolucion[0].Trim(), detDevolucion[1], EpiConnection);
                                                if (dtLineasFac.Rows.Count > 0)
                                                {
                                                    filaDuplex[3] = dtLineasFac.Rows[0].ItemArray[1];
                                                    filaDuplex[4] = dtLineasFac.Rows[0].ItemArray[2];
                                                    filaDuplex[5] = dtLineasFac.Rows[0].ItemArray[3];
                                                    filaDuplex[7] = dtLineasFac.Rows[0].ItemArray[4];
                                                    filaDuplex[8] = dtLineasFac.Rows[0].ItemArray[5];
                                                    filaDuplex[9] = dtLineasFac.Rows[0].ItemArray[6];
                                                }
                                                dtLineasDev.Rows.Add(filaDuplex);
                                                //totalRows = dtLineasDev.Rows.Count; // Se actualiza el total de filas del DataTable a devolver

                                                conteoLineas++;
                                                indiceDev += 4;
                                            }
                                        }
                                        else // Si la factura no coincide
                                        {
                                            indiceDev += 4;
                                        }
                                    }
                                }
                                while (indiceDev < (detDevolucion.Length - 1));

                                if (isLastRow == true)
                                    x += conteoLineas; // Se incrementa el número de filas
                                else
                                    x++;
                            }
                            else // Si hay más de una clasificación para devolver las lineas
                            {
                                int contador = 0;
                                bool isLastRow = ((x + 1) == totalRows) ? true : false;
                                do
                                {
                                    if (indiceDev == 0) // Primer iteración
                                    {
                                        if (detDevolucion[indiceDev].Trim().Equals(currentInvoice))
                                        {
                                            dtLineasDev.Rows[x][0] = detDevolucion[indiceDev].Trim(); // InvoiceNum
                                            dtLineasDev.Rows[x][1] = detDevolucion[indiceDev + 1]; // InvoiceLine
                                            dtLineasDev.Rows[x][10] = detClasificar[indiceFac + 2]; // ReturnQty

                                            dtLineasDev.Rows[x][12] = await asignarClasificacion(detClasificar[indiceFac + 1], EpiConnection); // BinNum(Clasificacion)
                                            dtLineasFac = await complementoFactura(detDevolucion[indiceDev].Trim(), detDevolucion[indiceDev + 1], EpiConnection);

                                            if (dtLineasFac.Rows.Count > 0)
                                            {
                                                dtLineasDev.Rows[x][3] = dtLineasFac.Rows[0].ItemArray[1]; // LineDesc
                                                dtLineasDev.Rows[x][4] = dtLineasFac.Rows[0].ItemArray[2]; // PackNum
                                                dtLineasDev.Rows[x][5] = dtLineasFac.Rows[0].ItemArray[3]; // PackLine
                                                dtLineasDev.Rows[x][7] = dtLineasFac.Rows[0].ItemArray[4]; // OrderNum
                                                dtLineasDev.Rows[x][8] = dtLineasFac.Rows[0].ItemArray[5]; // OrderLine
                                                dtLineasDev.Rows[x][9] = dtLineasFac.Rows[0].ItemArray[6]; // OrderRelease
                                            }

                                            // Ambos indices se incrementan debido a que la condición es (varias lineas vs varias clasificaciones)
                                            indiceDev += 4;
                                            indiceFac += 3;
                                        }
                                        else
                                        {
                                            // Ambos indices se incrementan debido a que la condición es (varias lineas vs varias clasificaciones)
                                            firstExist = false;
                                            indiceDev += 4;
                                            indiceFac += 3;
                                        }
                                    }
                                    else // Segunda iteración en adelante
                                    {
                                        if (detDevolucion[indiceDev].Trim().Equals(currentInvoice))
                                        {
                                            if (firstExist == false)
                                            {
                                                dtLineasDev.Rows[x][0] = detDevolucion[indiceDev].Trim(); // InvoiceNum
                                                dtLineasDev.Rows[x][1] = detDevolucion[indiceDev + 1]; // InvoiceLine
                                                dtLineasDev.Rows[x][10] = detClasificar[indiceFac + 2]; // ReturnQty

                                                dtLineasDev.Rows[x][12] = await asignarClasificacion(detClasificar[indiceFac + 1], EpiConnection); // BinNum(Clasificacion)
                                                dtLineasFac = await complementoFactura(detDevolucion[indiceDev].Trim(), detDevolucion[indiceDev + 1], EpiConnection);

                                                if (dtLineasFac.Rows.Count > 0)
                                                {
                                                    dtLineasDev.Rows[x][3] = dtLineasFac.Rows[0].ItemArray[1]; // LineDesc
                                                    dtLineasDev.Rows[x][4] = dtLineasFac.Rows[0].ItemArray[2]; // PackNum
                                                    dtLineasDev.Rows[x][5] = dtLineasFac.Rows[0].ItemArray[3]; // PackLine
                                                    dtLineasDev.Rows[x][7] = dtLineasFac.Rows[0].ItemArray[4]; // OrderNum
                                                    dtLineasDev.Rows[x][8] = dtLineasFac.Rows[0].ItemArray[5]; // OrderLine
                                                    dtLineasDev.Rows[x][9] = dtLineasFac.Rows[0].ItemArray[6]; // OrderRelease
                                                }

                                                // Ambos indices se incrementan debido a que la condición es (varias lineas vs varias clasificaciones)
                                                indiceDev += 4;
                                                indiceFac += 3;
                                                firstExist = true;
                                            }
                                            else
                                            {
                                                // Para devolver la misma parte a otra clasificación, de crea una fila auxiliar a la cual cargar los datos para posteriormente agregarla a dtLineasDev
                                                DataRow filaDuplex = dtLineasDev.NewRow();

                                                filaDuplex[0] = detDevolucion[indiceDev].Trim(); // InvoiceNum
                                                filaDuplex[1] = detDevolucion[indiceDev + 1]; // InvoiceLine
                                                filaDuplex[2] = dtLineasDev.Rows[x].ItemArray[2].ToString(); // PartNum
                                                filaDuplex[6] = dtLineasDev.Rows[x].ItemArray[6].ToString(); // ReturnReasonCode
                                                filaDuplex[10] = detClasificar[indiceFac + 3]; // ReturnQty
                                                filaDuplex[11] = dtLineasDev.Rows[x].ItemArray[11].ToString(); // ReturnQtyUOM
                                                filaDuplex[12] = await asignarClasificacion(detClasificar[indiceFac + 1], EpiConnection); // BinNum(Clasificacion)
                                                filaDuplex[13] = dtLineasDev.Rows[x].ItemArray[13].ToString(); // Note
                                                filaDuplex[14] = dtLineasDev.Rows[x].ItemArray[14].ToString(); // ZoneID
                                                filaDuplex[15] = dtLineasDev.Rows[x].ItemArray[15].ToString(); // PrimBin

                                                dtLineasFac = await complementoFactura(detDevolucion[0].Trim(), detDevolucion[1], EpiConnection);
                                                if (dtLineasFac.Rows.Count > 0)
                                                {
                                                    filaDuplex[3] = dtLineasFac.Rows[0].ItemArray[1];
                                                    filaDuplex[4] = dtLineasFac.Rows[0].ItemArray[2];
                                                    filaDuplex[5] = dtLineasFac.Rows[0].ItemArray[3];
                                                    filaDuplex[7] = dtLineasFac.Rows[0].ItemArray[4];
                                                    filaDuplex[8] = dtLineasFac.Rows[0].ItemArray[5];
                                                    filaDuplex[9] = dtLineasFac.Rows[0].ItemArray[6];
                                                }
                                                dtLineasDev.Rows.Add(filaDuplex);
                                                //totalRows = dtLineasDev.Rows.Count; // Se actualiza el total de filas del DataTable a devolver

                                                contador++;
                                                indiceDev += 4;
                                                indiceFac += 3;
                                            }
                                        }
                                        else
                                        {
                                            indiceDev += 4;
                                            indiceFac += 3;
                                        }
                                    }
                                }
                                while (indiceDev < (detDevolucion.Length - 1));

                                if (isLastRow == true)
                                    x += contador; // Se incrementa el número de filas
                                else
                                    x++;
                            }
                        }
                    }
                    else // Segunda iteración en adelante
                    {
                        if (detDevolucion[4].Trim().Equals("")) // Si solo hay una línea a devolver
                        {
                            if (detDevolucion[0].Trim().Equals(currentInvoice))
                            {
                                if (detClasificar[3].Trim().Equals("")) // Si solo hay una clasificación destino
                                {
                                    dtLineasDev.Rows[x][0] = detDevolucion[0].Trim(); // InvoiceNum
                                    dtLineasDev.Rows[x][1] = detDevolucion[1]; // InvoiceLine
                                    dtLineasDev.Rows[x][10] = detClasificar[2]; // ReturnQty
                                    dtLineasDev.Rows[x][12] = await asignarClasificacion(detClasificar[1], EpiConnection); // BinNum(Clasificacion)
                                    dtLineasFac = await complementoFactura(detDevolucion[0].Trim(), detDevolucion[1], EpiConnection);

                                    if (dtLineasFac.Rows.Count > 0)
                                    {
                                        dtLineasDev.Rows[x][3] = dtLineasFac.Rows[0].ItemArray[1]; // LineDesc
                                        dtLineasDev.Rows[x][4] = dtLineasFac.Rows[0].ItemArray[2]; // PackNum
                                        dtLineasDev.Rows[x][5] = dtLineasFac.Rows[0].ItemArray[3]; // PackLine
                                        dtLineasDev.Rows[x][7] = dtLineasFac.Rows[0].ItemArray[4]; // OrderNum
                                        dtLineasDev.Rows[x][8] = dtLineasFac.Rows[0].ItemArray[5]; // OrderLine
                                        dtLineasDev.Rows[x][9] = dtLineasFac.Rows[0].ItemArray[6]; // OrderRelease
                                    }
                                    x++;

                                }
                                else // Si la parte se devuelve a más de una clasificación
                                {
                                    int contClasificaciones = 0;
                                    bool isLastRow = ((x + 1) == totalRows) ? true : false;

                                    do
                                    {
                                        if (indiceFac == 0) // Primer iteración
                                        {
                                            dtLineasDev.Rows[x][0] = detDevolucion[indiceDev].Trim(); // InvoiceNum
                                            dtLineasDev.Rows[x][1] = detDevolucion[indiceDev + 1]; // InvoiceLine
                                            dtLineasDev.Rows[x][10] = detClasificar[indiceFac + 2]; // ReturnQty
                                            dtLineasDev.Rows[x][12] = await asignarClasificacion(detClasificar[indiceFac + 1], EpiConnection); // BinNum(Clasificacion)
                                            dtLineasFac = await complementoFactura(detDevolucion[indiceDev].Trim(), detDevolucion[indiceDev + 1], EpiConnection);

                                            if (dtLineasFac.Rows.Count > 0)
                                            {
                                                dtLineasDev.Rows[x][3] = dtLineasFac.Rows[0].ItemArray[1]; // LineDesc
                                                dtLineasDev.Rows[x][4] = dtLineasFac.Rows[0].ItemArray[2]; // PackNum
                                                dtLineasDev.Rows[x][5] = dtLineasFac.Rows[0].ItemArray[3]; // PackLine
                                                dtLineasDev.Rows[x][7] = dtLineasFac.Rows[0].ItemArray[4]; // OrderNum
                                                dtLineasDev.Rows[x][8] = dtLineasFac.Rows[0].ItemArray[5]; // OrderLine
                                                dtLineasDev.Rows[x][9] = dtLineasFac.Rows[0].ItemArray[6]; // OrderRelease
                                            }

                                            indiceFac += 3;
                                        }
                                        else // Segunda iteración en adelante
                                        {
                                            // Para devolver la misma parte a otra clasificación, de crea una fila auxiliar a la cual cargar los datos para posteriormente agregarla a dtLineasDev
                                            DataRow filaDuplex = dtLineasDev.NewRow();

                                            filaDuplex[0] = detDevolucion[indiceDev].Trim(' '); // InvoiceNum
                                            filaDuplex[1] = detDevolucion[indiceDev + 1]; // InvoiceLine
                                            filaDuplex[2] = dtLineasDev.Rows[x].ItemArray[2].ToString(); // PartNum
                                            filaDuplex[6] = dtLineasDev.Rows[x].ItemArray[6].ToString(); // ReturnReasonCode
                                            filaDuplex[10] = detClasificar[indiceFac + 2]; // ReturnQty
                                            filaDuplex[11] = dtLineasDev.Rows[x].ItemArray[11].ToString(); // ReturnQtyUOM
                                            filaDuplex[12] = await asignarClasificacion(detClasificar[indiceFac + 1], EpiConnection); // BinNum(Clasificacion)
                                            filaDuplex[13] = dtLineasDev.Rows[x].ItemArray[13].ToString(); // Note
                                            filaDuplex[14] = dtLineasDev.Rows[x].ItemArray[14].ToString(); // ZoneID
                                            filaDuplex[15] = dtLineasDev.Rows[x].ItemArray[15].ToString(); // PrimBin

                                            dtLineasFac = await complementoFactura(detDevolucion[0].Trim(), detDevolucion[1], EpiConnection);
                                            if (dtLineasFac.Rows.Count > 0)
                                            {
                                                filaDuplex[3] = dtLineasFac.Rows[0].ItemArray[1];
                                                filaDuplex[4] = dtLineasFac.Rows[0].ItemArray[2];
                                                filaDuplex[5] = dtLineasFac.Rows[0].ItemArray[3];
                                                filaDuplex[7] = dtLineasFac.Rows[0].ItemArray[4];
                                                filaDuplex[8] = dtLineasFac.Rows[0].ItemArray[5];
                                                filaDuplex[9] = dtLineasFac.Rows[0].ItemArray[6];
                                            }
                                            dtLineasDev.Rows.Add(filaDuplex);
                                            //totalRows = dtLineasDev.Rows.Count; // Se actualiza el total de filas del DataTable a devolver

                                            contClasificaciones++;
                                            indiceFac += 3;
                                        }
                                    }
                                    while (indiceFac < (detClasificar.Length - 1)); // El ciclo termina hasta que el indice de clasificaciones sea igual al tamaño del array

                                    if (isLastRow == true)
                                        x += contClasificaciones; // Se incrementa el número de filas
                                    else
                                        x++;
                                }
                            }
                        }
                        else // Si hay varias líneas a devolver
                        {
                            if (detClasificar[3].Trim().Equals("")) // Si todas las lineas se devuelven a la misma clasificacion
                            {
                                int conteoLineas = 0;
                                bool isLastRow = ((x + 1) == totalRows) ? true : false;
                                do
                                {
                                    if (indiceDev == 0) // Primer iteración
                                    {
                                        if (detDevolucion[indiceDev].Trim().Equals(currentInvoice))
                                        {
                                            dtLineasDev.Rows[x][0] = detDevolucion[indiceDev].Trim(); // InvoiceNum
                                            dtLineasDev.Rows[x][1] = detDevolucion[indiceDev + 1]; // InvoiceLine
                                            dtLineasDev.Rows[x][10] = detDevolucion[indiceDev + 3]; // ReturnQty

                                            dtLineasDev.Rows[x][12] = await asignarClasificacion(detClasificar[indiceFac + 1], EpiConnection); // BinNum(Clasificacion)
                                            dtLineasFac = await complementoFactura(detDevolucion[indiceDev].Trim(), detDevolucion[indiceDev + 1], EpiConnection);

                                            if (dtLineasFac.Rows.Count > 0)
                                            {
                                                dtLineasDev.Rows[x][3] = dtLineasFac.Rows[0].ItemArray[1]; // LineDesc
                                                dtLineasDev.Rows[x][4] = dtLineasFac.Rows[0].ItemArray[2]; // PackNum
                                                dtLineasDev.Rows[x][5] = dtLineasFac.Rows[0].ItemArray[3]; // PackLine
                                                dtLineasDev.Rows[x][7] = dtLineasFac.Rows[0].ItemArray[4]; // OrderNum
                                                dtLineasDev.Rows[x][8] = dtLineasFac.Rows[0].ItemArray[5]; // OrderLine
                                                dtLineasDev.Rows[x][9] = dtLineasFac.Rows[0].ItemArray[6]; // OrderRelease
                                            }

                                            indiceDev += 4;
                                        }
                                        else
                                        {
                                            firstExist = false;
                                            indiceDev += 4;
                                        }
                                    }
                                    else // Segunda iteración en adelante
                                    {
                                        if (detDevolucion[indiceDev].Trim().Equals(currentInvoice))
                                        {
                                            if (firstExist == false)
                                            {
                                                dtLineasDev.Rows[x][0] = detDevolucion[indiceDev].Trim(); // InvoiceNum
                                                dtLineasDev.Rows[x][1] = detDevolucion[indiceDev + 1]; // InvoiceLine
                                                dtLineasDev.Rows[x][10] = detDevolucion[indiceDev + 3]; // ReturnQty

                                                dtLineasDev.Rows[x][12] = await asignarClasificacion(detClasificar[indiceFac + 1], EpiConnection); // BinNum(Clasificacion)
                                                dtLineasFac = await complementoFactura(detDevolucion[indiceDev].Trim(), detDevolucion[indiceDev + 1], EpiConnection);

                                                if (dtLineasFac.Rows.Count > 0)
                                                {
                                                    dtLineasDev.Rows[x][3] = dtLineasFac.Rows[0].ItemArray[1]; // LineDesc
                                                    dtLineasDev.Rows[x][4] = dtLineasFac.Rows[0].ItemArray[2]; // PackNum
                                                    dtLineasDev.Rows[x][5] = dtLineasFac.Rows[0].ItemArray[3]; // PackLine
                                                    dtLineasDev.Rows[x][7] = dtLineasFac.Rows[0].ItemArray[4]; // OrderNum
                                                    dtLineasDev.Rows[x][8] = dtLineasFac.Rows[0].ItemArray[5]; // OrderLine
                                                    dtLineasDev.Rows[x][9] = dtLineasFac.Rows[0].ItemArray[6]; // OrderRelease
                                                }

                                                indiceDev += 4;
                                                firstExist = true;
                                            }
                                            else
                                            {
                                                // Para devolver la misma parte a otra clasificación, de crea una fila auxiliar a la cual cargar los datos para posteriormente agregarla a dtLineasDev
                                                DataRow filaDuplex = dtLineasDev.NewRow();

                                                filaDuplex[0] = detDevolucion[indiceDev].Trim(); // InvoiceNum
                                                filaDuplex[1] = detDevolucion[indiceDev + 1]; // InvoiceLine
                                                filaDuplex[2] = dtLineasDev.Rows[x].ItemArray[2].ToString(); // PartNum
                                                filaDuplex[6] = dtLineasDev.Rows[x].ItemArray[6].ToString(); // ReturnReasonCode
                                                filaDuplex[10] = detDevolucion[indiceDev + 3]; // ReturnQty
                                                filaDuplex[11] = dtLineasDev.Rows[x].ItemArray[11].ToString(); // ReturnQtyUOM
                                                filaDuplex[12] = await asignarClasificacion(detClasificar[indiceFac + 1], EpiConnection); // BinNum(Clasificacion)
                                                filaDuplex[13] = dtLineasDev.Rows[x].ItemArray[13].ToString(); // Note
                                                filaDuplex[14] = dtLineasDev.Rows[x].ItemArray[14].ToString(); // ZoneID
                                                filaDuplex[15] = dtLineasDev.Rows[x].ItemArray[15].ToString(); // PrimBin

                                                dtLineasFac = await complementoFactura(detDevolucion[0].Trim(), detDevolucion[1], EpiConnection);
                                                if (dtLineasFac.Rows.Count > 0)
                                                {
                                                    filaDuplex[3] = dtLineasFac.Rows[0].ItemArray[1];
                                                    filaDuplex[4] = dtLineasFac.Rows[0].ItemArray[2];
                                                    filaDuplex[5] = dtLineasFac.Rows[0].ItemArray[3];
                                                    filaDuplex[7] = dtLineasFac.Rows[0].ItemArray[4];
                                                    filaDuplex[8] = dtLineasFac.Rows[0].ItemArray[5];
                                                    filaDuplex[9] = dtLineasFac.Rows[0].ItemArray[6];
                                                }
                                                dtLineasDev.Rows.Add(filaDuplex);
                                                //totalRows = dtLineasDev.Rows.Count; // Se actualiza el total de filas del DataTable a devolver

                                                conteoLineas++;
                                                indiceDev += 4;
                                            }
                                        }
                                        else
                                        {
                                            indiceDev += 4;
                                        }
                                    }
                                }
                                while (indiceDev < (detDevolucion.Length - 1));

                                if (isLastRow == true)
                                    x += conteoLineas; // Se incrementa el número de filas
                                else
                                    x++;
                            }
                            else // Si hay más de una clasificación para devolver las lineas
                            {
                                int contador = 0;
                                bool isLastRow = ((x + 1) == totalRows) ? true : false;
                                do
                                {
                                    if (indiceDev == 0) // Primer iteración
                                    {
                                        if (detDevolucion[indiceDev].Trim().Equals(currentInvoice))
                                        {
                                            dtLineasDev.Rows[x][0] = detDevolucion[indiceDev].Trim(); // InvoiceNum
                                            dtLineasDev.Rows[x][1] = detDevolucion[indiceDev + 1]; // InvoiceLine
                                            dtLineasDev.Rows[x][10] = detClasificar[indiceFac + 3]; // ReturnQty

                                            dtLineasDev.Rows[x][12] = await asignarClasificacion(detClasificar[indiceFac + 1], EpiConnection); // BinNum(Clasificacion)
                                            dtLineasFac = await complementoFactura(detDevolucion[indiceDev].Trim(), detDevolucion[indiceDev + 1], EpiConnection);

                                            if (dtLineasFac.Rows.Count > 0)
                                            {
                                                dtLineasDev.Rows[x][3] = dtLineasFac.Rows[0].ItemArray[1]; // LineDesc
                                                dtLineasDev.Rows[x][4] = dtLineasFac.Rows[0].ItemArray[2]; // PackNum
                                                dtLineasDev.Rows[x][5] = dtLineasFac.Rows[0].ItemArray[3]; // PackLine
                                                dtLineasDev.Rows[x][7] = dtLineasFac.Rows[0].ItemArray[4]; // OrderNum
                                                dtLineasDev.Rows[x][8] = dtLineasFac.Rows[0].ItemArray[5]; // OrderLine
                                                dtLineasDev.Rows[x][9] = dtLineasFac.Rows[0].ItemArray[6]; // OrderRelease
                                            }

                                            // Ambos indices se incrementan debido a que la condición es (varias lineas vs varias clasificaciones)
                                            indiceDev += 4;
                                            indiceFac += 3;
                                        }
                                        else
                                        {
                                            // Ambos indices se incrementan debido a que la condición es (varias lineas vs varias clasificaciones)
                                            firstExist = false;
                                            indiceDev += 4;
                                            indiceFac += 3;
                                        }
                                    }
                                    else // Segunda iteración en adelante
                                    {
                                        if (detDevolucion[indiceDev].Trim().Equals(currentInvoice))
                                        {
                                            if (firstExist == false)
                                            {
                                                dtLineasDev.Rows[x][0] = detDevolucion[indiceDev].Trim(); // InvoiceNum
                                                dtLineasDev.Rows[x][1] = detDevolucion[indiceDev + 1]; // InvoiceLine
                                                dtLineasDev.Rows[x][10] = detClasificar[indiceFac + 3]; // ReturnQty

                                                dtLineasDev.Rows[x][12] = await asignarClasificacion(detClasificar[indiceFac + 1], EpiConnection); // BinNum(Clasificacion)
                                                dtLineasFac = await complementoFactura(detDevolucion[indiceDev].Trim(), detDevolucion[indiceDev + 1], EpiConnection);

                                                if (dtLineasFac.Rows.Count > 0)
                                                {
                                                    dtLineasDev.Rows[x][3] = dtLineasFac.Rows[0].ItemArray[1]; // LineDesc
                                                    dtLineasDev.Rows[x][4] = dtLineasFac.Rows[0].ItemArray[2]; // PackNum
                                                    dtLineasDev.Rows[x][5] = dtLineasFac.Rows[0].ItemArray[3]; // PackLine
                                                    dtLineasDev.Rows[x][7] = dtLineasFac.Rows[0].ItemArray[4]; // OrderNum
                                                    dtLineasDev.Rows[x][8] = dtLineasFac.Rows[0].ItemArray[5]; // OrderLine
                                                    dtLineasDev.Rows[x][9] = dtLineasFac.Rows[0].ItemArray[6]; // OrderRelease
                                                }

                                                // Ambos indices se incrementan debido a que la condición es (varias lineas vs varias clasificaciones)
                                                indiceDev += 4;
                                                indiceFac += 3;
                                                firstExist = true;
                                            }
                                            else
                                            {
                                                // Para devolver la misma parte a otra clasificación, de crea una fila auxiliar a la cual cargar los datos para posteriormente agregarla a dtLineasDev
                                                DataRow filaDuplex = dtLineasDev.NewRow();

                                                filaDuplex[0] = detDevolucion[indiceDev].Trim(); // InvoiceNum
                                                filaDuplex[1] = detDevolucion[indiceDev + 1]; // InvoiceLine
                                                filaDuplex[2] = dtLineasDev.Rows[x].ItemArray[2].ToString(); // PartNum
                                                filaDuplex[6] = dtLineasDev.Rows[x].ItemArray[6].ToString(); // ReturnReasonCode
                                                filaDuplex[10] = detClasificar[indiceFac + 2]; // ReturnQty
                                                filaDuplex[11] = dtLineasDev.Rows[x].ItemArray[11].ToString(); // ReturnQtyUOM
                                                filaDuplex[12] = await asignarClasificacion(detClasificar[indiceFac + 1], EpiConnection); // BinNum(Clasificacion)
                                                filaDuplex[13] = dtLineasDev.Rows[x].ItemArray[13].ToString(); // Note
                                                filaDuplex[14] = dtLineasDev.Rows[x].ItemArray[14].ToString(); // ZoneID
                                                filaDuplex[15] = dtLineasDev.Rows[x].ItemArray[15].ToString(); // PrimBin

                                                dtLineasFac = await complementoFactura(detDevolucion[0].Trim(), detDevolucion[1], EpiConnection);
                                                if (dtLineasFac.Rows.Count > 0)
                                                {
                                                    filaDuplex[3] = dtLineasFac.Rows[0].ItemArray[1];
                                                    filaDuplex[4] = dtLineasFac.Rows[0].ItemArray[2];
                                                    filaDuplex[5] = dtLineasFac.Rows[0].ItemArray[3];
                                                    filaDuplex[7] = dtLineasFac.Rows[0].ItemArray[4];
                                                    filaDuplex[8] = dtLineasFac.Rows[0].ItemArray[5];
                                                    filaDuplex[9] = dtLineasFac.Rows[0].ItemArray[6];
                                                }
                                                dtLineasDev.Rows.Add(filaDuplex);
                                                //totalRows = dtLineasDev.Rows.Count; // Se actualiza el total de filas del DataTable a devolver

                                                contador++;
                                                indiceDev += 4;
                                                indiceFac += 3;
                                            }
                                        }
                                       else
                                        {
                                            indiceDev += 4;
                                            indiceFac += 3;
                                        }
                                    }
                                }
                                while (indiceDev < (detDevolucion.Length - 1));

                                if (isLastRow == true)
                                    x += contador; // Se incrementa el número de filas
                                else
                                    x++;
                            }
                        }
                    }
                }
                while (x < totalRows);
                
                return dtLineasDev;
            }
            catch (System.IndexOutOfRangeException index)
            {
                catcher = index.Message;
                return dtLineasDev;
            }
            catch (System.Data.SqlClient.SqlException sqlError)
            {
                catcher = sqlError.Message;
                return dtLineasDev;
                //MessageBox.Show("Excepción capturada ... \n" + sqlError, "SQLException", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception scriptError)
            {
                catcher = scriptError.Message;
                return dtLineasDev;
                //MessageBox.Show("Excepción capturada ... \n" + scriptError, "Error al mostrar líneas de la factura", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task<string> asignarClasificacion(string IdClsf, string connection)
        {
            string Clsf = String.Empty;
            try
            {
                DataTable results = sql.getRecords(String.Format(ConfigurationManager.AppSettings["obtClasificacion"].ToString(), IdClsf), null, connection);
                if (results.Rows.Count > 0)
                    Clsf = results.Rows[0].ItemArray[0].ToString();

                return Clsf;
            }
            catch (System.Data.SqlClient.SqlException x)
            {
                catcher = x.Message;
                return Clsf;
            }
            catch(Exception y)
            {
                catcher = y.Message;
                return Clsf;
            }
        }

        private async Task<DataTable> complementoFactura(string InvoiceNum, string InvoiceLine, string connection)
        {
            DataTable InvcDtl = new DataTable();
            try
            {
                InvcDtl = sql.getRecords(String.Format(ConfigurationManager.AppSettings["obtFactDtl"].ToString(), InvoiceNum, InvoiceLine), null, connection);
                return InvcDtl;
            }
            catch (Exception e)
            {
                catcher = e.Message;
                return InvcDtl;
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
        
        public async Task<DataTable> datosRMA(string invoice, string Connection)
        {
            DataTable x = new DataTable();
            try
            {
                x = sql.getRecords(String.Format(ConfigurationManager.AppSettings["obtRMAHeader"],invoice), null, Connection);
                return x;
            }
            catch (Exception)
            {
                return x;
            }
        }

        public async Task<DataTable> detalladoRMA(string invoice, string connection)
        {
            DataTable d = new DataTable();
            try
            {
                d = sql.getRecords(String.Format(ConfigurationManager.AppSettings["obtRMADtl"], invoice), null, connection);
                return d;
            }
            catch (Exception x)
            {
                return d;
            }
        }
    }
}
