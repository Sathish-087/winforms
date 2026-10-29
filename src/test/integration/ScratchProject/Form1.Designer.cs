// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ScratchProject;

partial class Form1
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
        treeView1 = new System.Windows.Forms.TreeView();
        listView1 = new ListView();
        dateTimePicker1 = new DateTimePicker();
        textBox1 = new RichTextBox();
        System.Windows.Forms.DataGridViewCellStyle headerStyle = new System.Windows.Forms.DataGridViewCellStyle();
        this.dataGridView1 = new System.Windows.Forms.DataGridView();
        ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
        this.SuspendLayout();
        //
        // headerStyle
        //
        headerStyle.Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Bold);
        //
        // dataGridView1
        //
        this.dataGridView1.ColumnHeadersDefaultCellStyle = headerStyle;
        dataGridView1.RowHeadersDefaultCellStyle.BackColor = Color.Red;
        dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.dataGridView1.Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Regular);
        this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dataGridView1.Columns.Add("Id", "Id");
        this.dataGridView1.Columns.Add("Name", "Name");
        dataGridView1.RowHeadersVisible = true;
        dataGridView1.RowHeadersWidth = 80;

        dataGridView1.Columns.Add("Id", "Id");
        dataGridView1.Columns.Add("Name", "Name");

        for (int i = 0; i < 5; i++)
        {
            int rowIndex = dataGridView1.Rows.Add(i, $"Name {i}");
            dataGridView1.Rows[rowIndex].HeaderCell.Value = $"Row {i}";
        }

        dataGridView1.RowHeadersDefaultCellStyle.BackColor = Color.Red;
        dataGridView1.RowHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Italic);
        //
        // treeView1
        //
        this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
        treeView1.Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Regular);
        treeView1.Location = new System.Drawing.Point(20, 160);
        treeView1.Size = new System.Drawing.Size(360, 100);
        treeView1.LabelEdit = true;

        for (int i = 1; i <= 10; i++)
        {
            TreeNode node = new TreeNode($"Node {i}");

            // Custom font for every even node
            if (i % 2 == 0)
            {
                node.NodeFont = new Font("Segoe UI", 9, FontStyle.Bold);
            }

            treeView1.Nodes.Add(node);
        }

        //
        // listView1
        //
        this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.listView1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
        this.listView1.View = System.Windows.Forms.View.Details;
        this.listView1.FullRowSelect = true;
        this.listView1.Columns.Add("Name", 200);

        ListViewItem item1 = new ListViewItem("Normal Item");
        ListViewItem item2 = new ListViewItem("Bold Item");
        item1.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        this.listView1.Items.AddRange(
        [
            item1,
            item2
        ]);

        //
        // dateTimePicker1
        //
        this.dateTimePicker1.Font = new System.Drawing.Font("Segoe UI", 12F, FontStyle.Bold);
        this.dateTimePicker1.Location = new System.Drawing.Point(30, 30);
        this.dateTimePicker1.Size = new System.Drawing.Size(250, 29);

        //
        // richTextBox1
        //
        this.textBox1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.textBox1.Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Regular);
        textBox1.Text = @"Normal Text Bold Text Italic Text";
        textBox1.Select(12, 9);
        textBox1.SelectionFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        textBox1.Select(22, 10);
        textBox1.SelectionFont = new Font("Segoe UI", 9F, FontStyle.Italic);
        textBox1.Select(0, 0);

        //
        // Form1
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.ClientSize = new System.Drawing.Size(800, 450);
        //this.Controls.Add(this.dataGridView1);
        //this.Controls.Add(this.treeView1);
        this.Controls.Add(this.listView1);
        //this.Controls.Add(this.dateTimePicker1);
        //this.Controls.Add(this.textBox1);
        this.Name = "Form1";
        this.Text = "PerMonitorV2 DPI Repro";
        ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.DataGridView dataGridView1;
    private TreeView treeView1;
    private ListView listView1;
    private DateTimePicker dateTimePicker1;
    private RichTextBox textBox1;
}
