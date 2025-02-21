namespace Chromatic
{
    partial class Form4
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form4));
            this.btn_freeze = new System.Windows.Forms.Button();
            this.btn_grab = new System.Windows.Forms.Button();
            this.lbl_imagelegth = new System.Windows.Forms.Label();
            this.lbl_GainValue = new System.Windows.Forms.Label();
            this.lbl_currExposureValue = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.btn_saveImageHighth = new System.Windows.Forms.Button();
            this.btn_setGainValue = new System.Windows.Forms.Button();
            this.btn_setExposureValue = new System.Windows.Forms.Button();
            this.txt_imageHeighth = new System.Windows.Forms.TextBox();
            this.txt_GainValue = new System.Windows.Forms.TextBox();
            this.txt_ExposureValue = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.Main_Timer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // btn_freeze
            // 
            this.btn_freeze.Font = new System.Drawing.Font("楷体", 42F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_freeze.ForeColor = System.Drawing.Color.Red;
            this.btn_freeze.Location = new System.Drawing.Point(821, 397);
            this.btn_freeze.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btn_freeze.Name = "btn_freeze";
            this.btn_freeze.Size = new System.Drawing.Size(219, 102);
            this.btn_freeze.TabIndex = 50;
            this.btn_freeze.Text = "停止";
            this.btn_freeze.UseVisualStyleBackColor = true;
            this.btn_freeze.Click += new System.EventHandler(this.btn_freeze_Click);
            // 
            // btn_grab
            // 
            this.btn_grab.Font = new System.Drawing.Font("楷体", 42F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_grab.ForeColor = System.Drawing.Color.ForestGreen;
            this.btn_grab.Location = new System.Drawing.Point(821, 191);
            this.btn_grab.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btn_grab.Name = "btn_grab";
            this.btn_grab.Size = new System.Drawing.Size(219, 102);
            this.btn_grab.TabIndex = 49;
            this.btn_grab.Text = "开始";
            this.btn_grab.UseVisualStyleBackColor = true;
            this.btn_grab.Click += new System.EventHandler(this.btn_grab_Click);
            // 
            // lbl_imagelegth
            // 
            this.lbl_imagelegth.AutoSize = true;
            this.lbl_imagelegth.Font = new System.Drawing.Font("Times New Roman", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_imagelegth.Location = new System.Drawing.Point(210, 597);
            this.lbl_imagelegth.Name = "lbl_imagelegth";
            this.lbl_imagelegth.Size = new System.Drawing.Size(30, 34);
            this.lbl_imagelegth.TabIndex = 48;
            this.lbl_imagelegth.Text = "0";
            // 
            // lbl_GainValue
            // 
            this.lbl_GainValue.AutoSize = true;
            this.lbl_GainValue.Font = new System.Drawing.Font("Times New Roman", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_GainValue.Location = new System.Drawing.Point(210, 397);
            this.lbl_GainValue.Name = "lbl_GainValue";
            this.lbl_GainValue.Size = new System.Drawing.Size(30, 34);
            this.lbl_GainValue.TabIndex = 47;
            this.lbl_GainValue.Text = "0";
            // 
            // lbl_currExposureValue
            // 
            this.lbl_currExposureValue.AutoSize = true;
            this.lbl_currExposureValue.Font = new System.Drawing.Font("Times New Roman", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_currExposureValue.Location = new System.Drawing.Point(210, 187);
            this.lbl_currExposureValue.Name = "lbl_currExposureValue";
            this.lbl_currExposureValue.Size = new System.Drawing.Size(30, 34);
            this.lbl_currExposureValue.TabIndex = 46;
            this.lbl_currExposureValue.Text = "0";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("宋体", 16.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label6.ForeColor = System.Drawing.SystemColors.GrayText;
            this.label6.Location = new System.Drawing.Point(80, 597);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(124, 28);
            this.label6.TabIndex = 45;
            this.label6.Text = "当前高度";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("宋体", 16.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label5.ForeColor = System.Drawing.SystemColors.GrayText;
            this.label5.Location = new System.Drawing.Point(80, 397);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(124, 28);
            this.label5.TabIndex = 44;
            this.label5.Text = "当前增益";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("宋体", 16.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label4.ForeColor = System.Drawing.SystemColors.GrayText;
            this.label4.Location = new System.Drawing.Point(80, 191);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(124, 28);
            this.label4.TabIndex = 43;
            this.label4.Text = "当前曝光";
            // 
            // btn_saveImageHighth
            // 
            this.btn_saveImageHighth.Font = new System.Drawing.Font("楷体", 22.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_saveImageHighth.Location = new System.Drawing.Point(482, 519);
            this.btn_saveImageHighth.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btn_saveImageHighth.Name = "btn_saveImageHighth";
            this.btn_saveImageHighth.Size = new System.Drawing.Size(127, 50);
            this.btn_saveImageHighth.TabIndex = 42;
            this.btn_saveImageHighth.Text = "设置";
            this.btn_saveImageHighth.UseVisualStyleBackColor = true;
            this.btn_saveImageHighth.Click += new System.EventHandler(this.btn_saveImageHighth_Click);
            // 
            // btn_setGainValue
            // 
            this.btn_setGainValue.Font = new System.Drawing.Font("楷体", 22.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_setGainValue.Location = new System.Drawing.Point(482, 315);
            this.btn_setGainValue.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btn_setGainValue.Name = "btn_setGainValue";
            this.btn_setGainValue.Size = new System.Drawing.Size(127, 50);
            this.btn_setGainValue.TabIndex = 41;
            this.btn_setGainValue.Text = "设置";
            this.btn_setGainValue.UseVisualStyleBackColor = true;
            this.btn_setGainValue.Click += new System.EventHandler(this.btn_setGainValue_Click);
            // 
            // btn_setExposureValue
            // 
            this.btn_setExposureValue.Font = new System.Drawing.Font("楷体", 22.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_setExposureValue.Location = new System.Drawing.Point(482, 106);
            this.btn_setExposureValue.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btn_setExposureValue.Name = "btn_setExposureValue";
            this.btn_setExposureValue.Size = new System.Drawing.Size(127, 50);
            this.btn_setExposureValue.TabIndex = 40;
            this.btn_setExposureValue.Text = "设置";
            this.btn_setExposureValue.UseVisualStyleBackColor = true;
            this.btn_setExposureValue.Click += new System.EventHandler(this.btn_setExposureValue_Click);
            // 
            // txt_imageHeighth
            // 
            this.txt_imageHeighth.Font = new System.Drawing.Font("Times New Roman", 22.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_imageHeighth.Location = new System.Drawing.Point(286, 519);
            this.txt_imageHeighth.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txt_imageHeighth.Name = "txt_imageHeighth";
            this.txt_imageHeighth.Size = new System.Drawing.Size(169, 50);
            this.txt_imageHeighth.TabIndex = 39;
            // 
            // txt_GainValue
            // 
            this.txt_GainValue.Font = new System.Drawing.Font("Times New Roman", 22.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_GainValue.Location = new System.Drawing.Point(286, 315);
            this.txt_GainValue.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txt_GainValue.Name = "txt_GainValue";
            this.txt_GainValue.Size = new System.Drawing.Size(169, 50);
            this.txt_GainValue.TabIndex = 38;
            // 
            // txt_ExposureValue
            // 
            this.txt_ExposureValue.Font = new System.Drawing.Font("Times New Roman", 22.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_ExposureValue.Location = new System.Drawing.Point(286, 105);
            this.txt_ExposureValue.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txt_ExposureValue.Name = "txt_ExposureValue";
            this.txt_ExposureValue.Size = new System.Drawing.Size(169, 50);
            this.txt_ExposureValue.TabIndex = 37;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("楷体", 22.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(73, 527);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(207, 37);
            this.label3.TabIndex = 36;
            this.label3.Text = "图像高度：";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("楷体", 22.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.Location = new System.Drawing.Point(73, 324);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(131, 37);
            this.label2.TabIndex = 35;
            this.label2.Text = "增益：";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("楷体", 22.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(73, 113);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(207, 37);
            this.label1.TabIndex = 34;
            this.label1.Text = "曝光时间：";
            // 
            // Main_Timer
            // 
            this.Main_Timer.Interval = 1000;
            this.Main_Timer.Tick += new System.EventHandler(this.Main_Timer_Tick);
            // 
            // Form4
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1113, 736);
            this.Controls.Add(this.btn_freeze);
            this.Controls.Add(this.btn_grab);
            this.Controls.Add(this.lbl_imagelegth);
            this.Controls.Add(this.lbl_GainValue);
            this.Controls.Add(this.lbl_currExposureValue);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btn_saveImageHighth);
            this.Controls.Add(this.btn_setGainValue);
            this.Controls.Add(this.btn_setExposureValue);
            this.Controls.Add(this.txt_imageHeighth);
            this.Controls.Add(this.txt_GainValue);
            this.Controls.Add(this.txt_ExposureValue);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form4";
            this.Text = "DALSA_SDK";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_freeze;
        private System.Windows.Forms.Button btn_grab;
        private System.Windows.Forms.Label lbl_imagelegth;
        private System.Windows.Forms.Label lbl_GainValue;
        private System.Windows.Forms.Label lbl_currExposureValue;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btn_saveImageHighth;
        private System.Windows.Forms.Button btn_setGainValue;
        private System.Windows.Forms.Button btn_setExposureValue;
        private System.Windows.Forms.TextBox txt_imageHeighth;
        private System.Windows.Forms.TextBox txt_GainValue;
        public System.Windows.Forms.TextBox txt_ExposureValue;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Timer Main_Timer;
    }
}