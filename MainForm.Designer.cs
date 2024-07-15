using System.Windows.Forms;

namespace FastHorse
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.button1 = new System.Windows.Forms.Button();
            this.OpenFolder = new System.Windows.Forms.Button();
            this.DataBaseTool = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.labelProgress = new System.Windows.Forms.Label();
            this.ScriptFolder = new System.Windows.Forms.Label();
            this.AboutButton = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.White;
            this.button1.Location = new System.Drawing.Point(1060, 13);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(93, 54);
            this.button1.TabIndex = 0;
            this.button1.Text = "运行";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // OpenFolder
            // 
            this.OpenFolder.Location = new System.Drawing.Point(13, 13);
            this.OpenFolder.Name = "OpenFolder";
            this.OpenFolder.Size = new System.Drawing.Size(127, 54);
            this.OpenFolder.TabIndex = 2;
            this.OpenFolder.Text = "打开文件夹";
            this.OpenFolder.UseVisualStyleBackColor = true;
            this.OpenFolder.Click += new System.EventHandler(this.OpenFolder_Click);
            // 
            // DataBaseTool
            // 
            this.DataBaseTool.Location = new System.Drawing.Point(210, 13);
            this.DataBaseTool.Name = "DataBaseTool";
            this.DataBaseTool.Size = new System.Drawing.Size(107, 54);
            this.DataBaseTool.TabIndex = 3;
            this.DataBaseTool.Text = "数据库";
            this.DataBaseTool.UseVisualStyleBackColor = true;
            this.DataBaseTool.Click += new System.EventHandler(this.DataBaseTool_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(8, 220);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(1145, 27);
            this.progressBar1.TabIndex = 4;
            this.progressBar1.Visible = false;
            this.progressBar1.Click += new System.EventHandler(this.progressBar1_Click);
            // 
            // labelProgress
            // 
            this.labelProgress.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.labelProgress.Location = new System.Drawing.Point(13, 138);
            this.labelProgress.Name = "labelProgress";
            this.labelProgress.Size = new System.Drawing.Size(1138, 79);
            this.labelProgress.TabIndex = 5;
            this.labelProgress.Click += new System.EventHandler(this.labelProgress_Click);
            // 
            // ScriptFolder
            // 
            this.ScriptFolder.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ScriptFolder.Location = new System.Drawing.Point(13, 70);
            this.ScriptFolder.Name = "ScriptFolder";
            this.ScriptFolder.Size = new System.Drawing.Size(1139, 51);
            this.ScriptFolder.TabIndex = 6;
            this.ScriptFolder.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.ScriptFolder.Click += new System.EventHandler(this.ScriptFolder_Click);
            // 
            // AboutButton
            // 
            this.AboutButton.Location = new System.Drawing.Point(387, 13);
            this.AboutButton.Name = "AboutButton";
            this.AboutButton.Size = new System.Drawing.Size(107, 54);
            this.AboutButton.TabIndex = 7;
            this.AboutButton.Text = "关于";
            this.AboutButton.UseVisualStyleBackColor = true;
            this.AboutButton.Click += new System.EventHandler(this.AboutButton_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.OpenFolder);
            this.panel1.Controls.Add(this.AboutButton);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.ScriptFolder);
            this.panel1.Controls.Add(this.DataBaseTool);
            this.panel1.Controls.Add(this.labelProgress);
            this.panel1.Controls.Add(this.progressBar1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1168, 266);
            this.panel1.TabIndex = 8;
            this.panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 19F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1168, 264);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "Fast Horse For MSSQL";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button OpenFolder;
        private System.Windows.Forms.Button DataBaseTool;
        private ProgressBar progressBar1;
        private Label labelProgress;
        private Label ScriptFolder;
        private Button AboutButton;
        private Panel panel1;
    }
}

