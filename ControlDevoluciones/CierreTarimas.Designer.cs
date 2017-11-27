namespace ControlDevoluciones
{
    partial class CierreTarimas
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CierreTarimas));
            this.txtFolio = new DevComponents.DotNetBar.Controls.TextBoxX();
            this.dgvTarimas = new DevComponents.DotNetBar.Controls.DataGridViewX();
            this.ChkPaletColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.btnCorteTarima = new DevComponents.DotNetBar.ButtonX();
            this.chkTodo = new DevComponents.DotNetBar.Controls.CheckBoxX();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTarimas)).BeginInit();
            this.SuspendLayout();
            // 
            // txtFolio
            // 
            this.txtFolio.BackColor = System.Drawing.Color.White;
            // 
            // 
            // 
            this.txtFolio.Border.Class = "TextBoxBorder";
            this.txtFolio.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtFolio.DisabledBackColor = System.Drawing.Color.White;
            this.txtFolio.Enabled = false;
            this.txtFolio.ForeColor = System.Drawing.Color.Black;
            this.txtFolio.Location = new System.Drawing.Point(4, 13);
            this.txtFolio.Name = "txtFolio";
            this.txtFolio.PreventEnterBeep = true;
            this.txtFolio.Size = new System.Drawing.Size(72, 22);
            this.txtFolio.TabIndex = 1;
            this.txtFolio.Visible = false;
            // 
            // dgvTarimas
            // 
            this.dgvTarimas.AllowUserToAddRows = false;
            this.dgvTarimas.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvTarimas.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTarimas.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ChkPaletColumn});
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvTarimas.DefaultCellStyle = dataGridViewCellStyle1;
            this.dgvTarimas.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(170)))), ((int)(((byte)(170)))));
            this.dgvTarimas.Location = new System.Drawing.Point(0, 41);
            this.dgvTarimas.Name = "dgvTarimas";
            this.dgvTarimas.RowHeadersVisible = false;
            this.dgvTarimas.Size = new System.Drawing.Size(226, 152);
            this.dgvTarimas.TabIndex = 2;
            // 
            // ChkPaletColumn
            // 
            this.ChkPaletColumn.Frozen = true;
            this.ChkPaletColumn.HeaderText = "";
            this.ChkPaletColumn.Name = "ChkPaletColumn";
            this.ChkPaletColumn.Width = 20;
            // 
            // btnCorteTarima
            // 
            this.btnCorteTarima.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnCorteTarima.ColorTable = DevComponents.DotNetBar.eButtonColor.Office2007WithBackground;
            this.btnCorteTarima.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCorteTarima.ImageFixedSize = new System.Drawing.Size(64, 0);
            this.btnCorteTarima.Location = new System.Drawing.Point(155, 12);
            this.btnCorteTarima.Name = "btnCorteTarima";
            this.btnCorteTarima.Size = new System.Drawing.Size(67, 25);
            this.btnCorteTarima.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnCorteTarima.TabIndex = 58;
            this.btnCorteTarima.Text = "Aceptar";
            this.btnCorteTarima.Click += new System.EventHandler(this.btnCorteTarima_Click);
            // 
            // chkTodo
            // 
            this.chkTodo.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.chkTodo.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.chkTodo.Location = new System.Drawing.Point(4, 42);
            this.chkTodo.Name = "chkTodo";
            this.chkTodo.Size = new System.Drawing.Size(15, 18);
            this.chkTodo.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.chkTodo.TabIndex = 59;
            this.chkTodo.CheckedChanged += new System.EventHandler(this.chkTodo_CheckedChanged);
            // 
            // CierreTarimas
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(227, 195);
            this.Controls.Add(this.chkTodo);
            this.Controls.Add(this.btnCorteTarima);
            this.Controls.Add(this.dgvTarimas);
            this.Controls.Add(this.txtFolio);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.Black;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CierreTarimas";
            this.Text = "Selección de Tarimas";
            this.Load += new System.EventHandler(this.CierreTarimas_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvTarimas)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        public DevComponents.DotNetBar.Controls.TextBoxX txtFolio;
        private DevComponents.DotNetBar.Controls.DataGridViewX dgvTarimas;
        private DevComponents.DotNetBar.ButtonX btnCorteTarima;
        private DevComponents.DotNetBar.Controls.CheckBoxX chkTodo;
        private System.Windows.Forms.DataGridViewCheckBoxColumn ChkPaletColumn;
    }
}