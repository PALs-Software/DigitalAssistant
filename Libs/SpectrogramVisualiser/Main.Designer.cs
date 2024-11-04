namespace SpectrogramVisualiser
{
    partial class Main
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            panel1 = new Panel();
            panel2 = new Panel();
            ViewAudioFileBtn = new Button();
            StopPlayAudioBtn = new Button();
            PoolingSizeLbl = new Label();
            PoolingSizeCtr = new NumericUpDown();
            WindowStrideLbl = new Label();
            WindowStrideCtr = new NumericUpDown();
            WindowSizeLbl = new Label();
            WindowSizeCtr = new NumericUpDown();
            FftSizeLbl = new Label();
            FftSizeCtr = new NumericUpDown();
            Timer = new System.Windows.Forms.Timer(components);
            OpenFileDialog = new OpenFileDialog();
            PictureBoxPanel = new Panel();
            ProgressBar = new ProgressBar();
            PictureBox = new PictureBox();
            UseHannWindowLbl = new Label();
            UseHannWindow = new CheckBox();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)PoolingSizeCtr).BeginInit();
            ((System.ComponentModel.ISupportInitialize)WindowStrideCtr).BeginInit();
            ((System.ComponentModel.ISupportInitialize)WindowSizeCtr).BeginInit();
            ((System.ComponentModel.ISupportInitialize)FftSizeCtr).BeginInit();
            PictureBoxPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)PictureBox).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(UseHannWindow);
            panel1.Controls.Add(UseHannWindowLbl);
            panel1.Controls.Add(panel2);
            panel1.Controls.Add(PoolingSizeLbl);
            panel1.Controls.Add(PoolingSizeCtr);
            panel1.Controls.Add(WindowStrideLbl);
            panel1.Controls.Add(WindowStrideCtr);
            panel1.Controls.Add(WindowSizeLbl);
            panel1.Controls.Add(WindowSizeCtr);
            panel1.Controls.Add(FftSizeLbl);
            panel1.Controls.Add(FftSizeCtr);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(812, 67);
            panel1.TabIndex = 1;
            // 
            // panel2
            // 
            panel2.Controls.Add(ViewAudioFileBtn);
            panel2.Controls.Add(StopPlayAudioBtn);
            panel2.Dock = DockStyle.Right;
            panel2.Location = new Point(588, 0);
            panel2.Name = "panel2";
            panel2.Size = new Size(224, 67);
            panel2.TabIndex = 9;
            // 
            // ViewAudioFileBtn
            // 
            ViewAudioFileBtn.Location = new Point(18, 8);
            ViewAudioFileBtn.Name = "ViewAudioFileBtn";
            ViewAudioFileBtn.Size = new Size(94, 53);
            ViewAudioFileBtn.TabIndex = 10;
            ViewAudioFileBtn.Text = "View Audio File";
            ViewAudioFileBtn.UseVisualStyleBackColor = true;
            ViewAudioFileBtn.Click += ViewAudioFileBtn_Click;
            // 
            // StopPlayAudioBtn
            // 
            StopPlayAudioBtn.Location = new Point(118, 8);
            StopPlayAudioBtn.Name = "StopPlayAudioBtn";
            StopPlayAudioBtn.Size = new Size(94, 53);
            StopPlayAudioBtn.TabIndex = 9;
            StopPlayAudioBtn.Text = "Stop";
            StopPlayAudioBtn.UseVisualStyleBackColor = true;
            StopPlayAudioBtn.Click += StopPlayAudioBtn_Click;
            // 
            // PoolingSizeLbl
            // 
            PoolingSizeLbl.AutoSize = true;
            PoolingSizeLbl.Location = new Point(323, 7);
            PoolingSizeLbl.Name = "PoolingSizeLbl";
            PoolingSizeLbl.Size = new Size(90, 20);
            PoolingSizeLbl.TabIndex = 7;
            PoolingSizeLbl.Text = "Pooling Size";
            // 
            // PoolingSizeCtr
            // 
            PoolingSizeCtr.Location = new Point(326, 30);
            PoolingSizeCtr.Maximum = new decimal(new int[] { 99999, 0, 0, 0 });
            PoolingSizeCtr.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            PoolingSizeCtr.Name = "PoolingSizeCtr";
            PoolingSizeCtr.Size = new Size(86, 27);
            PoolingSizeCtr.TabIndex = 6;
            PoolingSizeCtr.Value = new decimal(new int[] { 6, 0, 0, 0 });
            PoolingSizeCtr.ValueChanged += PoolingSizeCtr_ValueChanged;
            // 
            // WindowStrideLbl
            // 
            WindowStrideLbl.AutoSize = true;
            WindowStrideLbl.Location = new Point(210, 7);
            WindowStrideLbl.Name = "WindowStrideLbl";
            WindowStrideLbl.Size = new Size(107, 20);
            WindowStrideLbl.TabIndex = 5;
            WindowStrideLbl.Text = "Window Stride";
            // 
            // WindowStrideCtr
            // 
            WindowStrideCtr.Location = new Point(213, 30);
            WindowStrideCtr.Maximum = new decimal(new int[] { 99999, 0, 0, 0 });
            WindowStrideCtr.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            WindowStrideCtr.Name = "WindowStrideCtr";
            WindowStrideCtr.Size = new Size(86, 27);
            WindowStrideCtr.TabIndex = 4;
            WindowStrideCtr.Value = new decimal(new int[] { 160, 0, 0, 0 });
            WindowStrideCtr.ValueChanged += WindowStride_ValueChanged;
            // 
            // WindowSizeLbl
            // 
            WindowSizeLbl.AutoSize = true;
            WindowSizeLbl.Location = new Point(109, 7);
            WindowSizeLbl.Name = "WindowSizeLbl";
            WindowSizeLbl.Size = new Size(95, 20);
            WindowSizeLbl.TabIndex = 3;
            WindowSizeLbl.Text = "Window Size";
            // 
            // WindowSizeCtr
            // 
            WindowSizeCtr.Location = new Point(112, 30);
            WindowSizeCtr.Maximum = new decimal(new int[] { 99999, 0, 0, 0 });
            WindowSizeCtr.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            WindowSizeCtr.Name = "WindowSizeCtr";
            WindowSizeCtr.Size = new Size(86, 27);
            WindowSizeCtr.TabIndex = 2;
            WindowSizeCtr.Value = new decimal(new int[] { 320, 0, 0, 0 });
            WindowSizeCtr.ValueChanged += WindowSize_ValueChanged;
            // 
            // FftSizeLbl
            // 
            FftSizeLbl.AutoSize = true;
            FftSizeLbl.Location = new Point(9, 7);
            FftSizeLbl.Name = "FftSizeLbl";
            FftSizeLbl.Size = new Size(62, 20);
            FftSizeLbl.TabIndex = 1;
            FftSizeLbl.Text = "FFT Size";
            // 
            // FftSizeCtr
            // 
            FftSizeCtr.Enabled = false;
            FftSizeCtr.Location = new Point(12, 30);
            FftSizeCtr.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            FftSizeCtr.Name = "FftSizeCtr";
            FftSizeCtr.Size = new Size(86, 27);
            FftSizeCtr.TabIndex = 0;
            FftSizeCtr.Value = new decimal(new int[] { 512, 0, 0, 0 });
            // 
            // Timer
            // 
            Timer.Enabled = true;
            Timer.Interval = 50;
            Timer.Tick += Timer_Tick;
            // 
            // OpenFileDialog
            // 
            OpenFileDialog.FileName = "audio";
            OpenFileDialog.Filter = "Wav Files |*.wav";
            // 
            // PictureBoxPanel
            // 
            PictureBoxPanel.Controls.Add(ProgressBar);
            PictureBoxPanel.Controls.Add(PictureBox);
            PictureBoxPanel.Dock = DockStyle.Fill;
            PictureBoxPanel.Location = new Point(0, 67);
            PictureBoxPanel.Name = "PictureBoxPanel";
            PictureBoxPanel.Size = new Size(812, 383);
            PictureBoxPanel.TabIndex = 10;
            // 
            // ProgressBar
            // 
            ProgressBar.Dock = DockStyle.Top;
            ProgressBar.Location = new Point(0, 0);
            ProgressBar.Name = "ProgressBar";
            ProgressBar.Size = new Size(812, 29);
            ProgressBar.TabIndex = 10;
            // 
            // PictureBox
            // 
            PictureBox.Dock = DockStyle.Fill;
            PictureBox.Location = new Point(0, 0);
            PictureBox.Name = "PictureBox";
            PictureBox.Size = new Size(812, 383);
            PictureBox.TabIndex = 3;
            PictureBox.TabStop = false;
            // 
            // UseHannWindowLbl
            // 
            UseHannWindowLbl.AutoSize = true;
            UseHannWindowLbl.Location = new Point(430, 7);
            UseHannWindowLbl.Name = "UseHannWindowLbl";
            UseHannWindowLbl.Size = new Size(131, 20);
            UseHannWindowLbl.TabIndex = 10;
            UseHannWindowLbl.Text = "Use Hann Window";
            // 
            // UseHannWindow
            // 
            UseHannWindow.AutoSize = true;
            UseHannWindow.Checked = true;
            UseHannWindow.CheckState = CheckState.Checked;
            UseHannWindow.Location = new Point(484, 35);
            UseHannWindow.Name = "UseHannWindow";
            UseHannWindow.Size = new Size(18, 17);
            UseHannWindow.TabIndex = 11;
            UseHannWindow.UseVisualStyleBackColor = true;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(812, 450);
            Controls.Add(PictureBoxPanel);
            Controls.Add(panel1);
            Name = "Main";
            Text = "Spectrogram Visualiser";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)PoolingSizeCtr).EndInit();
            ((System.ComponentModel.ISupportInitialize)WindowStrideCtr).EndInit();
            ((System.ComponentModel.ISupportInitialize)WindowSizeCtr).EndInit();
            ((System.ComponentModel.ISupportInitialize)FftSizeCtr).EndInit();
            PictureBoxPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)PictureBox).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Label FftSizeLbl;
        private NumericUpDown FftSizeCtr;
        private Label WindowSizeLbl;
        private NumericUpDown WindowSizeCtr;
        private Label WindowStrideLbl;
        private NumericUpDown WindowStrideCtr;
        private Label PoolingSizeLbl;
        private NumericUpDown PoolingSizeCtr;
        private System.Windows.Forms.Timer Timer;
        private Panel panel2;
        private Button StopPlayAudioBtn;
        private OpenFileDialog OpenFileDialog;
        private Button ViewAudioFileBtn;
        private Panel PictureBoxPanel;
        private PictureBox PictureBox;
        private ProgressBar ProgressBar;
        private Label UseHannWindowLbl;
        private CheckBox UseHannWindow;
    }
}
