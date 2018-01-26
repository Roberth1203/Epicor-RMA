using System;
using System.Data.SqlClient;
using System.Configuration;
using System.Windows.Forms;
using System.Data;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography.X509Certificates; //Certificado
using System.Net.Security; // Seguridad
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace Utilities
{
    public class SQLUtilities
    {
        public string catchError = String.Empty;
        public string connect = "Data Source=rmalocal.db;Version=3;New=True;Compress=True;";

        public static SqlConnection SQLConnectionStart(string conexion)
        {
            try
            {
                string connectionStr = conexion;
                SqlConnection conn = new SqlConnection(connectionStr);
                conn.Open();
                return conn;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace, ex.Message);
                return null;
            }
        }

        public void closeConnection(SqlConnection conn)
        {
            try
            {
                conn.Close();
                SqlConnection.ClearPool(conn);
            }
            catch(Exception ex)
            {
                catchError = ex.Message + " => \n" + ex.StackTrace;
            }
        }

        public DataTable getRecords(string query,SqlConnection connection,string getConn)
        {
            DataTable dt = new DataTable();
            try
            {
                if (connection == null)
                {
                    connection = SQLConnectionStart(getConn);
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand(query, connection); // Ejecución de la consulta SELECT
                    adapter.Fill(dt); // Los registros obtenidos se cargan al DataTable a retornar
                    closeConnection(connection);
                }
            }
            catch (System.Data.SqlClient.SqlException sql)
            {
                catchError = sql.Message + " => \n" + sql.StackTrace;
                mailNotification(catchError);
            }
            catch (Exception w)
            {
                catchError = w.Message + " => \n" + w.StackTrace;
                mailNotification(catchError);
            }
            return dt;
        }

        public void SQLstatement(string sentence, string getConnString, SqlConnection conn = null)
        {
            try
            {
                if (conn == null)
                {
                    conn = SQLConnectionStart(getConnString);
                    SqlCommand query = new SqlCommand(sentence, conn);
                    query.ExecuteNonQuery();

                    closeConnection(conn);
                }
            }
            catch (Exception q)
            {
                catchError = q.Message + " => \n" + q.StackTrace;
                mailNotification(catchError);
            }
        }

        public void mailNotification(string exception)
        {
            MailMessage Mensaje = new MailMessage(); // Instancia para preparar el cuerpo del correo

            // Parámetros y cuerpo del correo 
            Mensaje.To.Add(new MailAddress("rarroyo.1878@gmail.com"));
            //Mensaje.To.Add(new MailAddress("robertogarcia003@hotmail.com"));
            Mensaje.From = new MailAddress("asesores@gicaor.com");
            Mensaje.Subject = "Excepción capturada en DevolucionesMAC";
            Mensaje.Body = "Buen día !! \n Ha ocurrido una excepción en la aplicación, a continuación se presenta la descripción completa \n\n" + exception;


            SmtpClient server = new SmtpClient("mail.gicaor.com", 366); // Especifico el servidor de salida
            server.Credentials = new System.Net.NetworkCredential("asesores@gicaor.com", "GICrfv456");
            server.EnableSsl = true;

            ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            };
            server.Send(Mensaje);
        }

            public static SQLiteConnection SQLiteConnect()
            {
                try
                {
                    SQLiteConnection connect = new SQLiteConnection(string.Format("Data Source={0};Version=3;",Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dbDevoluciones.db")));
                    connect.Open();
                    Console.Write("Conexión establecida");
                    return connect;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Excepción Capturada ... \n" + ex.Message, "SQLiteException Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }

            }

            public static void closeSQLite(SQLiteConnection connection)
            {
                try
                {
                    if (connection != null)
                    {
                        connection.Close();
                        SQLiteConnection.ClearPool(connection);
                    }
                }
                catch (Exception error)
                {
                    MessageBox.Show("Excepción Capturada ... \n" + error.Message, "SQLiteException Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            public void SQLiteStatement(string instruccion)
            {
                string query = instruccion;
                SQLiteConnection conn = SQLiteConnect();
                SQLiteCommand comando = new SQLiteCommand(query, conn);
                comando.ExecuteNonQuery();
            }

            public DataTable SQLiteData(string instruccion)
            {
                DataTable temp = new DataTable();
                SQLiteConnection conn = new SQLiteConnection(connect);
                try
                {
                    conn = SQLiteConnect();
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter();
                    adapter.SelectCommand = new SQLiteCommand(instruccion, conn);
                    adapter.Fill(temp);
                    conn.Close();

                }
                catch (Exception dataError)
                {
                    MessageBox.Show("Excepción Capturada ... \n" + dataError.Message, "SQLiteException Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                return temp;
            }
    }
}
