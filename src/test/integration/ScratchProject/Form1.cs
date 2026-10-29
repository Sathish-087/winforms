// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;

namespace ScratchProject;

// As we can't currently design in VS in the runtime solution, mark as "Default" so this opens in code view by default.
[DesignerCategory("Default")]
public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
        DpiChanged += Issue12350_DpiChanged;
        Load += Issue12350_Load;
    }

    private void Issue12350_Load(object? sender, EventArgs e)
    {
        // Debug.WriteLine($"Data Grid Font: {dataGridView1.Font.Size}");
        // if (dataGridView1.ColumnHeadersDefaultCellStyle.Font is not null)
        // {
        //    Debug.WriteLine($"Header Font: {dataGridView1.ColumnHeadersDefaultCellStyle.Font.Size}");
        // }

        // if (dataGridView1.RowHeadersDefaultCellStyle.Font is not null)
        // {
        //    Debug.WriteLine($"Header Font: {dataGridView1.RowHeadersDefaultCellStyle.Font.Size}");
        // }

        //Debug.WriteLine($"Tree Font: {treeView1.Font.Size}, Font Style: {treeView1.Font.Style}");
        //if (treeView1.Nodes.Count > 0)
        //{
        //    foreach (TreeNode node in treeView1.Nodes)
        //    {
        //        Debug.WriteLine($"Tree Node Font: {node.NodeFont?.Size}, Font Style: {node.NodeFont?.Style}");
        //    }
        //}

        Debug.WriteLine($"ListView Font: {listView1.Font.Size}, Font Style: {listView1.Font.Style}");
        if (listView1.Items.Count > 0)
        {
            foreach (ListViewItem node in listView1.Items)
            {
                Debug.WriteLine($"Tree Node Font: {node.Font?.Size}, Font Style: {node.Font?.Style}");
            }
        }

        // Debug.WriteLine($"DateTimePicker Font: {dateTimePicker1.Font.Size}, FontStyle: {dateTimePicker1.Font.Style}");

        // Debug.WriteLine($"RichTextBox Font: {textBox1.Font.Size}, FontStyle: {textBox1.Font.Style}");
        // Debug.WriteLine($"RichTextBox Selection Font: {textBox1.SelectionFont?.Size}, FontStyle: {textBox1.SelectionFont?.Style}");
    }

    private void Issue12350_DpiChanged(object? sender, DpiChangedEventArgs e)
    {
        Debug.WriteLine($"New DPI: {e.DeviceDpiNew}, Old DPI: {e.DeviceDpiOld}");
        // Debug.WriteLine($"Data Grid Font: {dataGridView1.Font.Size}");
        // if (dataGridView1.ColumnHeadersDefaultCellStyle.Font is not null)
        // {
        //    Debug.WriteLine($"Header Font: {dataGridView1.ColumnHeadersDefaultCellStyle.Font.Size}");
        // }

        // if (dataGridView1.RowHeadersDefaultCellStyle.Font is not null)
        // {
        //    Debug.WriteLine($"Header Font: {dataGridView1.RowHeadersDefaultCellStyle.Font.Size}");
        // }

        //Debug.WriteLine($"Tree Font: {treeView1.Font.Size}, Font Style: {treeView1.Font.Style}");
        //if (treeView1.Nodes.Count > 0)
        //{
        //    foreach (TreeNode node in treeView1.Nodes)
        //    {
        //        Debug.WriteLine($"Tree Node Font: {node.NodeFont?.Size}, Font Style: {node.NodeFont?.Style}");
        //    }
        //}

        Debug.WriteLine($"ListView Font: {listView1.Font.Size}, Font Style: {listView1.Font.Style}");
        if (listView1.Items.Count > 0)
        {
            foreach (ListViewItem node in listView1.Items)
            {
                Debug.WriteLine($"Tree Node Font: {node.Font?.Size}, Font Style: {node.Font?.Style}");
            }
        }

        // Debug.WriteLine($"DateTimePicker Font: {dateTimePicker1.Font.Size}, FontStyle: {dateTimePicker1.Font.Style}");

        // Debug.WriteLine($"RichTextBox Font: {textBox1.Font.Size}, FontStyle: {textBox1.Font.Style}");
        // Debug.WriteLine($"RichTextBox Selection Font: {textBox1.SelectionFont?.Size}, FontStyle: {textBox1.SelectionFont?.Style}");
    }
}
