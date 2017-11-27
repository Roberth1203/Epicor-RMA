using System;
using System.Threading;
using System.Windows.Forms;

namespace ControlDevoluciones
{
    public partial class SplashAnimation : Form
    {
        public SplashAnimation()
        {
            InitializeComponent();
        }

        void SplashAnimation_Load(object sender, EventArgs e)
        {
            initApplication();
        }
        
        /*Carga de la aplicación*/
        public void initApplication()
        {
            Thread.Sleep(100);
            this.Invoke((MethodInvoker)(() => setMessage("Buscando actualizaciones...")));
            Thread.Sleep(100);
            this.Invoke((MethodInvoker)(() => setMessage("Conectando a la base de datos...")));
            Thread.Sleep(100);
            this.Invoke((MethodInvoker)(() => setMessage("Verificando Licencias...")));
            Thread.Sleep(100);
            this.Invoke((MethodInvoker)(() => setMessage("Obteniendo listado de choferes...")));
            Thread.Sleep(100);
            this.Invoke((MethodInvoker)(() => setMessage("Iniciando aplicación...")));
            Thread.Sleep(100);
            if (this.InvokeRequired == false) this.Invoke(new Action(finishProcess));
        }

        public void setMessage(string msg)
        {
            lblMensaje.Text = msg;
        }

        public void finishProcess()
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            //this.Close();
            using (PantallaPrincipal splah = new PantallaPrincipal())
            {
                this.Show();
            }
        }

        void closeButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //initApplication();
        }
    }
}
