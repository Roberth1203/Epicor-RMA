using System;
using System.IO;
using System.Configuration;
using System.Windows.Forms;
using System.Diagnostics;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Data;
using System.Threading.Tasks;
using System.Drawing;
using DevComponents.DotNetBar;
//using iTextSharp.text;
//using iTextSharp.text.pdf;

namespace Utilities
{
    public class FileManager
    {
        string filePath = String.Empty;
        public void createLog(string numRelacion)
        {
            // Especificación de la carpeta Raiz
            string folderName = ConfigurationManager.AppSettings["mainFolder"].ToString();

            // Se especifica una subcarpeta con la fecha del día
            string date = DateTime.Now.ToString("yyyy-MM-dd");
            string pathString = System.IO.Path.Combine(folderName, date);

            // Se crea el nuevo directorio especificado
            System.IO.Directory.CreateDirectory(pathString);

            // Se crea un nombre de archivo para escribir el log y se agrega el nombre a la ruta
            string fileName = numRelacion + ".txt";
            pathString = System.IO.Path.Combine(pathString, fileName);

            if (!System.IO.File.Exists(pathString))
            {
                //System.IO.FileStream fs = System.IO.File.Create(pathString);
                System.IO.StreamWriter file = new System.IO.StreamWriter(pathString);
                file.Close();
                writeFileHeader(pathString, numRelacion);

                // Guardando la ruta del archivo
                ConfigurationManager.AppSettings.Set("rutaDelArchivo", pathString);

            }
            else
            {
                Console.WriteLine("File \"{0}\" already exists.", fileName);
                ConfigurationManager.AppSettings.Set("rutaDelArchivo", pathString);
                return;
            }
        }

        private void writeFileHeader(string path, string cobranza)
        {
            using (StreamWriter sw = File.AppendText(path))
            {
                sw.WriteLine("________________________________________________________________________________");
                sw.WriteLine("                 Resumen de la relación de cobranza " + cobranza);
                sw.WriteLine("________________________________________________________________________________");
            }
        }

        public void writeContentToFile(string content)
        {
            using (StreamWriter sw = File.AppendText(ConfigurationManager.AppSettings["rutaDelArchivo"].ToString()))
                sw.WriteLine(content);
        }

        public void printPDF(string sourcePathTXT)
        {
            try
            {
                string destinationPathPDF = String.Empty;
                string sourcePath = sourcePathTXT;

                iTextSharp.text.Document document = new iTextSharp.text.Document();
                string filename = Path.GetFileNameWithoutExtension(sourcePath);
                System.IO.StreamReader myFile = new System.IO.StreamReader(sourcePath);
                string myString = myFile.ReadToEnd();
                myFile.Close();
                if (!Directory.Exists("C:\\DevolucionesLOG\\pdf"))
                    Directory.CreateDirectory("C:\\DevolucionesLOG\\pdf");
                if (File.Exists("C:\\DevolucionesLOG\\pdf" + "\\" + filename + ".pdf"))
                    File.Delete("C:\\DevolucionesLOG\\pdf" + "\\" + filename + ".pdf");

                iTextSharp.text.pdf.PdfWriter.GetInstance(document, new FileStream("C:\\DevolucionesLOG\\pdf" + "\\" + filename + ".pdf", FileMode.CreateNew));

                // Se añade la imagen de la empresa al documento
                iTextSharp.text.Image imagenMAC = iTextSharp.text.Image.GetInstance(ConfigurationManager.AppSettings["imagenPDF"].ToString());
                imagenMAC.BorderWidth = 0;
                imagenMAC.Alignment = Element.ALIGN_CENTER;
                float percentage = 0.0f;
                percentage = 150 / imagenMAC.Width;
                imagenMAC.ScalePercent(percentage * 100);



                document.Open();
                document.Add(imagenMAC);
                document.Add(new iTextSharp.text.Paragraph(myString));
                document.Close();

                destinationPathPDF = "C:\\DevolucionesLOG\\pdf" + "\\" + filename + ".pdf";
                openPDF(destinationPathPDF);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void openPDF(string sourcePathPDF)
        {
            string pdfPath = Path.Combine(Application.StartupPath, sourcePathPDF);
            Process.Start(pdfPath);
        }

        public async Task exportTable(DataTable dt, string fileName, string currentUser)
        {
            DataTable dt2 = new DataTable();
            dt2 = dt;
            Document document = new Document();
            string ubicacionDestino = String.Empty;
            string file = String.Format("C:\\DevolucionesLOG\\pdf\\Reporte de turno {0}.pdf", fileName);

            if (File.Exists(file))
                File.Delete(file);

            PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(file, FileMode.CreateNew));
            document.Open();
            iTextSharp.text.Font h1 = iTextSharp.text.FontFactory.GetFont(FontFactory.TIMES_BOLD, 8);
            iTextSharp.text.Font h5 = iTextSharp.text.FontFactory.GetFont(FontFactory.HELVETICA, 8);

            PdfPTable table = new PdfPTable(dt.Columns.Count);
            PdfPTable tableT1 = new PdfPTable(dt.Columns.Count);
            PdfPTable tableT2 = new PdfPTable(dt.Columns.Count);
            PdfPTable tableT3 = new PdfPTable(dt.Columns.Count);
            PdfPTable tableDEF = new PdfPTable(dt.Columns.Count);
            PdfPTable tableVIR = new PdfPTable(dt.Columns.Count);
            PdfPTable tableEDA = new PdfPTable(dt.Columns.Count);
            PdfPTable tableGAR = new PdfPTable(dt.Columns.Count);

            PdfPCell thead;

            int[] widths = new int[] { 8, 10, 13, 8, 53, 6, 6, 8, 13 };

            // Tarima General Buen Estado
            table.SetWidths(widths);
            table.WidthPercentage = 100;
            PdfPCell header = new PdfPCell(new Phrase("Tarima GENERAL BUEN ESTADO"));
            header.Colspan = dt.Columns.Count;
            header.HorizontalAlignment = Element.ALIGN_CENTER;
            header.BackgroundColor = new iTextSharp.text.BaseColor(84, 153, 199);
            table.AddCell(header);

            // Tarima Torre 1
            tableT1.SetWidths(widths);
            tableT1.WidthPercentage = 100;
            PdfPCell headerT1 = new PdfPCell(new Phrase("Tarima BUEN ESTADO TORRE 1"));
            headerT1.Colspan = dt.Columns.Count;
            headerT1.HorizontalAlignment = Element.ALIGN_CENTER;
            headerT1.BackgroundColor = new iTextSharp.text.BaseColor(84, 153, 199);
            tableT1.AddCell(headerT1);

            // Tarima Torre 2
            tableT2.SetWidths(widths);
            tableT2.WidthPercentage = 100;
            PdfPCell headerT2 = new PdfPCell(new Phrase("Tarima BUEN ESTADO TORRE 2"));
            headerT2.Colspan = dt.Columns.Count;
            headerT2.HorizontalAlignment = Element.ALIGN_CENTER;
            headerT2.BackgroundColor = new iTextSharp.text.BaseColor(84, 153, 199);
            tableT2.AddCell(headerT2);

            // Tarima Torre 3
            tableT3.SetWidths(widths);
            tableT3.WidthPercentage = 100;
            PdfPCell headerT3 = new PdfPCell(new Phrase("Tarima BUEN ESTADO TORRE 3"));
            headerT3.Colspan = dt.Columns.Count;
            headerT3.HorizontalAlignment = Element.ALIGN_CENTER;
            headerT3.BackgroundColor = new iTextSharp.text.BaseColor(84, 153, 199);
            tableT3.AddCell(headerT3);

            // Tarima Defectuoso
            tableDEF.SetWidths(widths);
            tableDEF.WidthPercentage = 100;
            PdfPCell headerDEF = new PdfPCell(new Phrase("Tarima DEFECTUOSO"));
            headerDEF.Colspan = dt.Columns.Count;
            headerDEF.HorizontalAlignment = Element.ALIGN_CENTER;
            headerDEF.BackgroundColor = new iTextSharp.text.BaseColor(84, 153, 199);
            tableDEF.AddCell(headerDEF);
            tableGAR.SetWidths(widths);
            tableGAR.WidthPercentage = 100;

            // Tarima Virtual
            tableVIR.SetWidths(widths);
            tableVIR.WidthPercentage = 100;
            PdfPCell headerVIR = new PdfPCell(new Phrase("Tarima VIRTUAL"));
            headerVIR.Colspan = dt.Columns.Count;
            headerVIR.HorizontalAlignment = Element.ALIGN_CENTER;
            headerVIR.BackgroundColor = new iTextSharp.text.BaseColor(84, 153, 199);
            tableVIR.AddCell(headerVIR);

            // Tarima Empaque Dañado
            tableEDA.SetWidths(widths);
            tableEDA.WidthPercentage = 100;
            PdfPCell headerEDA = new PdfPCell(new Phrase("Tarima EMPAQUE DAÑADO"));
            headerEDA.Colspan = dt.Columns.Count;
            headerEDA.HorizontalAlignment = Element.ALIGN_CENTER;
            headerEDA.BackgroundColor = new iTextSharp.text.BaseColor(84, 153, 199);
            tableEDA.AddCell(headerEDA);

            // Tarima Garantía
            tableGAR.SetWidths(widths);
            tableGAR.WidthPercentage = 100;
            PdfPCell headerGAR = new PdfPCell(new Phrase("Tarima GARANTÍA"));
            headerGAR.Colspan = dt.Columns.Count;
            headerGAR.HorizontalAlignment = Element.ALIGN_CENTER;
            headerGAR.BackgroundColor = new iTextSharp.text.BaseColor(84, 153, 199);
            tableGAR.AddCell(headerGAR);

            foreach (DataColumn c in dt.Columns)
            {
                thead = new PdfPCell(new Phrase(c.ColumnName, h1));
                thead.Colspan = 1;
                thead.BackgroundColor = new iTextSharp.text.BaseColor(169, 204, 227);

                table.AddCell(thead);
                tableT1.AddCell(thead);
                tableT2.AddCell(thead);
                tableT3.AddCell(thead);
                tableDEF.AddCell(thead);
                tableVIR.AddCell(thead);
                tableEDA.AddCell(thead);
                tableGAR.AddCell(thead);
            }

            if (dt.Rows.Count > 0)
            {
                foreach (DataRow r in dt.Rows)
                {
                    if (r[2].ToString().Contains("T1BUEES")) //Validar a que tarima se envío la parte
                    {
                        tableT1.AddCell(new Phrase(r[0].ToString(), h5));
                        tableT1.AddCell(new Phrase(r[1].ToString(), h5));
                        tableT1.AddCell(new Phrase(r[2].ToString(), h5));
                        tableT1.AddCell(new Phrase(r[3].ToString(), h5));
                        tableT1.AddCell(new Phrase(r[4].ToString(), h5));
                        tableT1.AddCell(new Phrase(r[5].ToString().Substring(0, 4), h5));
                        tableT1.AddCell(new Phrase(r[6].ToString(), h5));
                        tableT1.AddCell(new Phrase(r[7].ToString(), h5));
                        tableT1.AddCell(new Phrase(r[8].ToString(), h5));
                    }
                    else if (r[2].ToString().Contains("T2BUEES")) //Validar a que tarima se envío la parte
                    {
                        tableT2.AddCell(new Phrase(r[0].ToString(), h5));
                        tableT2.AddCell(new Phrase(r[1].ToString(), h5));
                        tableT2.AddCell(new Phrase(r[2].ToString(), h5));
                        tableT2.AddCell(new Phrase(r[3].ToString(), h5));
                        tableT2.AddCell(new Phrase(r[4].ToString(), h5));
                        tableT2.AddCell(new Phrase(r[5].ToString().Substring(0, 4), h5));
                        tableT2.AddCell(new Phrase(r[6].ToString(), h5));
                        tableT2.AddCell(new Phrase(r[7].ToString(), h5));
                        tableT2.AddCell(new Phrase(r[8].ToString(), h5));
                    }
                    else if (r[2].ToString().Contains("T3BUEES")) //Validar a que tarima se envío la parte
                    {
                        tableT3.AddCell(new Phrase(r[0].ToString(), h5));
                        tableT3.AddCell(new Phrase(r[1].ToString(), h5));
                        tableT3.AddCell(new Phrase(r[2].ToString(), h5));
                        tableT3.AddCell(new Phrase(r[3].ToString(), h5));
                        tableT3.AddCell(new Phrase(r[4].ToString(), h5));
                        tableT3.AddCell(new Phrase(r[5].ToString().Substring(0, 4), h5));
                        tableT3.AddCell(new Phrase(r[6].ToString(), h5));
                        tableT3.AddCell(new Phrase(r[7].ToString(), h5));
                        tableT3.AddCell(new Phrase(r[8].ToString(), h5));
                    }
                    else if (r[2].ToString().Contains("DEF")) //Validar a que tarima se envío la parte
                    {
                        tableDEF.AddCell(new Phrase(r[0].ToString(), h5));
                        tableDEF.AddCell(new Phrase(r[1].ToString(), h5));
                        tableDEF.AddCell(new Phrase(r[2].ToString(), h5));
                        tableDEF.AddCell(new Phrase(r[3].ToString(), h5));
                        tableDEF.AddCell(new Phrase(r[4].ToString(), h5));
                        tableDEF.AddCell(new Phrase(r[5].ToString().Substring(0, 4), h5));
                        tableDEF.AddCell(new Phrase(r[6].ToString(), h5));
                        tableDEF.AddCell(new Phrase(r[7].ToString(), h5));
                        tableDEF.AddCell(new Phrase(r[8].ToString(), h5));
                    }
                    else if (r[2].ToString().Contains("GAR")) //Validar a que tarima se envío la parte
                    {
                        tableGAR.AddCell(new Phrase(r[0].ToString(), h5));
                        tableGAR.AddCell(new Phrase(r[1].ToString(), h5));
                        tableGAR.AddCell(new Phrase(r[2].ToString(), h5));
                        tableGAR.AddCell(new Phrase(r[3].ToString(), h5));
                        tableGAR.AddCell(new Phrase(r[4].ToString(), h5));
                        tableGAR.AddCell(new Phrase(r[5].ToString().Substring(0, 4), h5));
                        tableGAR.AddCell(new Phrase(r[6].ToString(), h5));
                        tableGAR.AddCell(new Phrase(r[7].ToString(), h5));
                        tableGAR.AddCell(new Phrase(r[8].ToString(), h5));
                    }
                    else if (r[2].ToString().Contains("EDA")) //Validar a que tarima se envío la parte
                    {
                        tableEDA.AddCell(new Phrase(r[0].ToString(), h5));
                        tableEDA.AddCell(new Phrase(r[1].ToString(), h5));
                        tableEDA.AddCell(new Phrase(r[2].ToString(), h5));
                        tableEDA.AddCell(new Phrase(r[3].ToString(), h5));
                        tableEDA.AddCell(new Phrase(r[4].ToString(), h5));
                        tableEDA.AddCell(new Phrase(r[5].ToString().Substring(0, 4), h5));
                        tableEDA.AddCell(new Phrase(r[6].ToString(), h5));
                        tableEDA.AddCell(new Phrase(r[7].ToString(), h5));
                        tableEDA.AddCell(new Phrase(r[8].ToString(), h5));
                    }
                    else if (r[2].ToString().Contains("VIR")) //Validar a que tarima se envío la parte
                    {
                        tableVIR.AddCell(new Phrase(r[0].ToString(), h5));
                        tableVIR.AddCell(new Phrase(r[1].ToString(), h5));
                        tableVIR.AddCell(new Phrase(r[2].ToString(), h5));
                        tableVIR.AddCell(new Phrase(r[3].ToString(), h5));
                        tableVIR.AddCell(new Phrase(r[4].ToString(), h5));
                        tableVIR.AddCell(new Phrase(r[5].ToString().Substring(0, 4), h5));
                        tableVIR.AddCell(new Phrase(r[6].ToString(), h5));
                        tableVIR.AddCell(new Phrase(r[7].ToString(), h5));
                        tableVIR.AddCell(new Phrase(r[8].ToString(), h5));
                    }
                    else
                    {
                        table.AddCell(new Phrase(r[0].ToString(), h5));
                        table.AddCell(new Phrase(r[1].ToString(), h5));
                        table.AddCell(new Phrase(r[2].ToString(), h5));
                        table.AddCell(new Phrase(r[3].ToString(), h5));
                        table.AddCell(new Phrase(r[4].ToString(), h5));
                        table.AddCell(new Phrase(r[5].ToString().Substring(0, 4), h5));
                        table.AddCell(new Phrase(r[6].ToString(), h5));
                        table.AddCell(new Phrase(r[7].ToString(), h5));
                        table.AddCell(new Phrase(r[8].ToString(), h5));
                    }
                }
            }

            document.Add(new iTextSharp.text.Paragraph(String.Format("Turno Actual: {0}", fileName)));
            document.Add(new iTextSharp.text.Paragraph("Fecha de impresión: " + DateTime.Now));
            document.Add(new iTextSharp.text.Paragraph(String.Format("Usuario Captura: {0}", currentUser)));

            iTextSharp.text.Image imagenMAC = iTextSharp.text.Image.GetInstance(ConfigurationManager.AppSettings["imagenPDF"].ToString());
            imagenMAC.BorderWidth = 0;
            imagenMAC.Alignment = Element.ALIGN_RIGHT;
            float percentage = 0.0f;
            percentage = 150 / imagenMAC.Width;
            imagenMAC.ScalePercent(percentage * 100);
            imagenMAC.SetAbsolutePosition(400, 750);
            document.Add(imagenMAC);

            if (table.Rows.Count > 2)
            {
                document.Add(new iTextSharp.text.Paragraph("_"));
                document.Add(table);
            }

            if (tableT1.Rows.Count > 2)
            {
                document.Add(new iTextSharp.text.Paragraph("_"));
                document.Add(tableT1);
            }

            if (tableT2.Rows.Count > 2)
            {
                document.Add(new iTextSharp.text.Paragraph("_"));
                document.Add(tableT2);
            }

            if (tableT3.Rows.Count > 2)
            {
                document.Add(new iTextSharp.text.Paragraph("_"));
                document.Add(tableT3);
            }

            if (tableDEF.Rows.Count > 2)
            {
                document.Add(new iTextSharp.text.Paragraph("_"));
                document.Add(tableDEF);
            }

            if (tableGAR.Rows.Count > 2)
            {
                document.Add(new iTextSharp.text.Paragraph("_"));
                document.Add(tableGAR);
            }

            if (tableEDA.Rows.Count > 2)
            {
                document.Add(new iTextSharp.text.Paragraph("_"));
                document.Add(tableEDA);
            }

            if (tableVIR.Rows.Count > 2)
            {
                document.Add(new iTextSharp.text.Paragraph("_"));
                document.Add(tableVIR);
            }

            document.Close();
            Process.Start(file);
            rptRMAvsCreditMemo(dt2, fileName, currentUser);
        }

        private void rptRMAvsCreditMemo(DataTable dt, string turno, string cu)
        {
            Document document = new Document();
            string Ubicacion = String.Empty;
            string file = String.Format("C:\\DevolucionesLOG\\pdf\\Listado Facturas Procesadas en turno {0}.pdf",turno);

            if (File.Exists(file))
                File.Delete(file);

            PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(file, FileMode.CreateNew));
            document.Open();
            iTextSharp.text.Font h1 = iTextSharp.text.FontFactory.GetFont(FontFactory.TIMES_BOLD, 8);
            iTextSharp.text.Font h5 = iTextSharp.text.FontFactory.GetFont(FontFactory.HELVETICA, 8);

            PdfPTable table = new PdfPTable(dt.Columns.Count);
            PdfPCell thead;

            int[] widths = new int[] { 8, 10, 13, 8, 53, 6, 6, 8, 13 };

            table.SetWidths(widths);
            table.WidthPercentage = 100;
            PdfPCell header = new PdfPCell(new Phrase("Tabla de RMA´s Generadas"));
            header.Colspan = dt.Columns.Count;
            header.HorizontalAlignment = Element.ALIGN_CENTER;
            header.BackgroundColor = new iTextSharp.text.BaseColor(84, 153, 199);
            table.AddCell(header);

            foreach (DataColumn c in dt.Columns)
            {
                thead = new PdfPCell(new Phrase(c.ColumnName, h1));
                thead.Colspan = 1;
                thead.BackgroundColor = new iTextSharp.text.BaseColor(169, 204, 227);

                table.AddCell(thead);
            }

            if (dt.Rows.Count > 0)
            {
                foreach (DataRow r in dt.Rows)
                {
                    table.AddCell(new Phrase(r[0].ToString(), h5));
                    table.AddCell(new Phrase(r[1].ToString(), h5));
                    table.AddCell(new Phrase(r[2].ToString(), h5));
                    table.AddCell(new Phrase(r[3].ToString(), h5));
                    table.AddCell(new Phrase(r[4].ToString(), h5));
                    table.AddCell(new Phrase(r[5].ToString().Substring(0, 4), h5));
                    table.AddCell(new Phrase(r[6].ToString(), h5));
                    table.AddCell(new Phrase(r[7].ToString(), h5));
                    table.AddCell(new Phrase(r[8].ToString(), h5));
                }
            }

            document.Add(new iTextSharp.text.Paragraph(String.Format("Turno Actual: {0}", turno)));
            document.Add(new iTextSharp.text.Paragraph("Fecha de impresión: " + DateTime.Now));
            document.Add(new iTextSharp.text.Paragraph(String.Format("Usuario Captura: {0}", cu)));

            iTextSharp.text.Image imagenMAC = iTextSharp.text.Image.GetInstance(ConfigurationManager.AppSettings["imagenPDF"].ToString());
            imagenMAC.BorderWidth = 0;
            imagenMAC.Alignment = Element.ALIGN_RIGHT;
            float percentage = 0.0f;
            percentage = 150 / imagenMAC.Width;
            imagenMAC.ScalePercent(percentage * 100);
            imagenMAC.SetAbsolutePosition(400, 750);
            document.Add(imagenMAC);

            if (table.Rows.Count > 2)
            {
                document.Add(new iTextSharp.text.Paragraph("_"));
                document.Add(table);
            }

            document.Close();
            Process.Start(file);
        }
    }
}
