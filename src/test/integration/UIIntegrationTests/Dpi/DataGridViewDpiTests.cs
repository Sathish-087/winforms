// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using Windows.Win32.UI.HiDpi;

namespace System.Windows.Forms.UITests.Dpi;

public class DataGridViewDpiTests : ControlTestBase
{
    public DataGridViewDpiTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [WinFormsTheory]
    // Use a DPI that differs from common ambient values (96/192) so BEFOREPARENT does not early-out.
    [InlineData(3.5 * ScaleHelper.OneHundredPercentLogicalDpi)]
    public void DataGridView_DpiChanged_ScalesSizeAndFont(int newDpi)
    {
        // Run tests only on Windows 10 versions that support thread dpi awareness.
        if (!PlatformDetection.IsWindows10Version1803OrGreater)
        {
            return;
        }

        DPI_AWARENESS_CONTEXT originalAwarenessContext = PInvoke.SetThreadDpiAwarenessContextInternal(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        typeof(ScaleHelper).TestAccessor.Dynamic.InitializeStatics();
        try
        {
            using Form form = new();
            form.AutoScaleMode = AutoScaleMode.Dpi;

            using DataGridView dataGridView = new()
            {
                Location = new Point(10, 10),
                Size = new Size(320, 200),
                AutoGenerateColumns = false,
                // Explicit font keeps the local-font DPI path.
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };
            dataGridView.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", Width = 120 });
            dataGridView.Rows.Add("One");
            dataGridView.Rows.Add("Two");

            form.Controls.Add(dataGridView);
            form.Show();

            Size initialSize = dataGridView.Size;
            float initialFontSize = dataGridView.Font.Size;

            // Mirror SplitContainerTests: child BEFOREPARENT then top-level form WM_DPICHANGED.
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, dataGridView, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, newDpi);

            // DataGridView scales control bounds/font on the child DPI path.
            // Header defaults are LogicalToDeviceUnits at construction and are not reasserted here.
            Assert.NotEqual(initialSize, dataGridView.Size);
            Assert.NotEqual(initialFontSize, dataGridView.Font.Size);

            form.Close();
        }
        finally
        {
            // Reset back to original awareness context.
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }
}
