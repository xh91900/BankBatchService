namespace BankBatchService
{
    partial class BankBatchWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BankBatchWindow));
            this.groupBox = new System.Windows.Forms.GroupBox();
            this.richTextBox = new System.Windows.Forms.RichTextBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.更新系统配置ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.打印系统配置ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.当日扣款文件发送情况ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.清屏ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.打印发送信息ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.打印接收信息ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox
            // 
            this.groupBox.Controls.Add(this.richTextBox);
            this.groupBox.Location = new System.Drawing.Point(4, 30);
            this.groupBox.Margin = new System.Windows.Forms.Padding(3, 3, 3, 4);
            this.groupBox.Name = "groupBox";
            this.groupBox.Size = new System.Drawing.Size(470, 450);
            this.groupBox.TabIndex = 0;
            this.groupBox.TabStop = false;
            this.groupBox.Text = "银行批量数据归集服务";
            // 
            // richTextBox
            // 
            this.richTextBox.BackColor = System.Drawing.SystemColors.InfoText;
            this.richTextBox.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.richTextBox.ForeColor = System.Drawing.Color.Green;
            this.richTextBox.Location = new System.Drawing.Point(4, 14);
            this.richTextBox.Name = "richTextBox";
            this.richTextBox.Size = new System.Drawing.Size(470, 430);
            this.richTextBox.TabIndex = 0;
            this.richTextBox.Text = "";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.更新系统配置ToolStripMenuItem,
            this.打印系统配置ToolStripMenuItem,
            this.当日扣款文件发送情况ToolStripMenuItem,
            this.清屏ToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(484, 25);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // 更新系统配置ToolStripMenuItem
            // 
            this.更新系统配置ToolStripMenuItem.Name = "更新系统配置ToolStripMenuItem";
            this.更新系统配置ToolStripMenuItem.Size = new System.Drawing.Size(92, 21);
            this.更新系统配置ToolStripMenuItem.Text = "更新系统配置";
            this.更新系统配置ToolStripMenuItem.Click += new System.EventHandler(this.更新系统配置ToolStripMenuItem_Click);
            // 
            // 打印系统配置ToolStripMenuItem
            // 
            this.打印系统配置ToolStripMenuItem.Name = "打印系统配置ToolStripMenuItem";
            this.打印系统配置ToolStripMenuItem.Size = new System.Drawing.Size(92, 21);
            this.打印系统配置ToolStripMenuItem.Text = "打印系统配置";
            this.打印系统配置ToolStripMenuItem.Click += new System.EventHandler(this.打印系统配置ToolStripMenuItem_Click);
            // 
            // 当日扣款文件发送情况ToolStripMenuItem
            // 
            this.当日扣款文件发送情况ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.打印发送信息ToolStripMenuItem,
            this.打印接收信息ToolStripMenuItem});
            this.当日扣款文件发送情况ToolStripMenuItem.Name = "当日扣款文件发送情况ToolStripMenuItem";
            this.当日扣款文件发送情况ToolStripMenuItem.Size = new System.Drawing.Size(169, 21);
            this.当日扣款文件发送情况ToolStripMenuItem.Text = "当日扣款文件发送/接收情况";
            // 
            // 清屏ToolStripMenuItem
            // 
            this.清屏ToolStripMenuItem.Name = "清屏ToolStripMenuItem";
            this.清屏ToolStripMenuItem.Size = new System.Drawing.Size(44, 21);
            this.清屏ToolStripMenuItem.Text = "清屏";
            this.清屏ToolStripMenuItem.Click += new System.EventHandler(this.清屏ToolStripMenuItem_Click);
            // 
            // 打印发送信息ToolStripMenuItem
            // 
            this.打印发送信息ToolStripMenuItem.Name = "打印发送信息ToolStripMenuItem";
            this.打印发送信息ToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.打印发送信息ToolStripMenuItem.Text = "打印发送信息";
            this.打印发送信息ToolStripMenuItem.Click += new System.EventHandler(this.打印发送信息ToolStripMenuItem_Click);
            // 
            // 打印接收信息ToolStripMenuItem
            // 
            this.打印接收信息ToolStripMenuItem.Name = "打印接收信息ToolStripMenuItem";
            this.打印接收信息ToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.打印接收信息ToolStripMenuItem.Text = "打印接收信息";
            this.打印接收信息ToolStripMenuItem.Click += new System.EventHandler(this.打印接收信息ToolStripMenuItem_Click);
            // 
            // BankBatchWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(484, 491);
            this.Controls.Add(this.groupBox);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.Name = "BankBatchWindow";
            this.Text = "BankBatchWindow";
            this.groupBox.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox;
        private System.Windows.Forms.RichTextBox richTextBox;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 更新系统配置ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 打印系统配置ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 当日扣款文件发送情况ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 清屏ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 打印发送信息ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 打印接收信息ToolStripMenuItem;
    }
}