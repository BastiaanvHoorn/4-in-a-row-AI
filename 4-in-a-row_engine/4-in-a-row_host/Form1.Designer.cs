namespace _4_in_a_row_
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
            this.numeric_Alice = new System.Windows.Forms.NumericUpDown();
            this.numeric_Bob = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.button_Alice = new System.Windows.Forms.Button();
            this.button_Bob = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numeric_Alice)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numeric_Bob)).BeginInit();
            this.SuspendLayout();
            // 
            // numeric_Alice
            // 
            this.numeric_Alice.Location = new System.Drawing.Point(12, 28);
            this.numeric_Alice.Maximum = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.numeric_Alice.Name = "numeric_Alice";
            this.numeric_Alice.Size = new System.Drawing.Size(120, 20);
            this.numeric_Alice.TabIndex = 0;
            this.numeric_Alice.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // numeric_Bob
            // 
            this.numeric_Bob.Location = new System.Drawing.Point(369, 28);
            this.numeric_Bob.Maximum = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.numeric_Bob.Name = "numeric_Bob";
            this.numeric_Bob.Size = new System.Drawing.Size(120, 20);
            this.numeric_Bob.TabIndex = 1;
            this.numeric_Bob.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(30, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Alice";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(454, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(26, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Bob";
            // 
            // button_Alice
            // 
            this.button_Alice.Location = new System.Drawing.Point(12, 54);
            this.button_Alice.Name = "button_Alice";
            this.button_Alice.Size = new System.Drawing.Size(120, 23);
            this.button_Alice.TabIndex = 4;
            this.button_Alice.Text = "button1";
            this.button_Alice.UseVisualStyleBackColor = true;
            this.button_Alice.Click += new System.EventHandler(this.button_Alice_Click);
            // 
            // button_Bob
            // 
            this.button_Bob.Location = new System.Drawing.Point(369, 54);
            this.button_Bob.Name = "button_Bob";
            this.button_Bob.Size = new System.Drawing.Size(120, 23);
            this.button_Bob.TabIndex = 5;
            this.button_Bob.Text = "button2";
            this.button_Bob.UseVisualStyleBackColor = true;
            this.button_Bob.Click += new System.EventHandler(this.button_Bob_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(501, 564);
            this.Controls.Add(this.button_Bob);
            this.Controls.Add(this.button_Alice);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numeric_Bob);
            this.Controls.Add(this.numeric_Alice);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numeric_Alice)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numeric_Bob)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NumericUpDown numeric_Alice;
        private System.Windows.Forms.NumericUpDown numeric_Bob;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button_Alice;
        private System.Windows.Forms.Button button_Bob;
    }
}

