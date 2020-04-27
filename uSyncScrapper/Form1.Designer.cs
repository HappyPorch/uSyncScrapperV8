namespace uSyncScrapper
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.buttonBrowseFolder = new System.Windows.Forms.Button();
            this.buttonScrap = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.checkBoxIncludePropertiesWithoutDescription = new System.Windows.Forms.CheckBox();
            this.textBoxResults = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // buttonBrowseFolder
            // 
            this.buttonBrowseFolder.Location = new System.Drawing.Point(486, 12);
            this.buttonBrowseFolder.Name = "buttonBrowseFolder";
            this.buttonBrowseFolder.Size = new System.Drawing.Size(113, 37);
            this.buttonBrowseFolder.TabIndex = 0;
            this.buttonBrowseFolder.Text = "Browse folder";
            this.buttonBrowseFolder.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.buttonBrowseFolder.UseVisualStyleBackColor = true;
            this.buttonBrowseFolder.Click += new System.EventHandler(this.buttonBrowseFolder_Click);
            // 
            // buttonScrap
            // 
            this.buttonScrap.Location = new System.Drawing.Point(525, 256);
            this.buttonScrap.Name = "buttonScrap";
            this.buttonScrap.Size = new System.Drawing.Size(75, 38);
            this.buttonScrap.TabIndex = 1;
            this.buttonScrap.Text = "Scrap";
            this.buttonScrap.UseVisualStyleBackColor = true;
            this.buttonScrap.Click += new System.EventHandler(this.buttonScrap_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(12, 12);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(468, 22);
            this.textBox1.TabIndex = 2;
            this.textBox1.Text = "E:\\Projects\\princesa-plaza\\website\\PrincesaPlaza.Web";
            // 
            // checkBoxIncludePropertiesWithoutDescription
            // 
            this.checkBoxIncludePropertiesWithoutDescription.AutoSize = true;
            this.checkBoxIncludePropertiesWithoutDescription.Checked = true;
            this.checkBoxIncludePropertiesWithoutDescription.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxIncludePropertiesWithoutDescription.Location = new System.Drawing.Point(12, 44);
            this.checkBoxIncludePropertiesWithoutDescription.Name = "checkBoxIncludePropertiesWithoutDescription";
            this.checkBoxIncludePropertiesWithoutDescription.Size = new System.Drawing.Size(264, 21);
            this.checkBoxIncludePropertiesWithoutDescription.TabIndex = 3;
            this.checkBoxIncludePropertiesWithoutDescription.Text = "Include properties without description";
            this.checkBoxIncludePropertiesWithoutDescription.UseVisualStyleBackColor = true;
            // 
            // textBoxResults
            // 
            this.textBoxResults.Location = new System.Drawing.Point(12, 81);
            this.textBoxResults.Multiline = true;
            this.textBoxResults.Name = "textBoxResults";
            this.textBoxResults.ReadOnly = true;
            this.textBoxResults.Size = new System.Drawing.Size(587, 167);
            this.textBoxResults.TabIndex = 4;
            this.textBoxResults.Text = "Assumes all pages have a *page suffix. Assumes all compositions have a *compositi" +
    "on suffix. Module\'s descriptions are fetched from the element\'s description as w" +
    "ell as Editor Notes properties.";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(609, 304);
            this.Controls.Add(this.textBoxResults);
            this.Controls.Add(this.checkBoxIncludePropertiesWithoutDescription);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.buttonScrap);
            this.Controls.Add(this.buttonBrowseFolder);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "uSync Scrapper";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Button buttonBrowseFolder;
        private System.Windows.Forms.Button buttonScrap;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.CheckBox checkBoxIncludePropertiesWithoutDescription;
        private System.Windows.Forms.TextBox textBoxResults;
    }
}

