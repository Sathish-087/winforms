// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using Windows.Win32.UI.HiDpi;

namespace System.Windows.Forms.UITests.Dpi;

public class ContextMenuStripDpiTests : ControlTestBase
{
    public ContextMenuStripDpiTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [WinFormsFact]
    public async Task ContextMenuStrip_TopLevel_MovingToDifferentDpiMonitor_ScalesMetrics()
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
            form.ClientSize = new Size(300, 200);

            using ContextMenuStrip contextMenu = new()
            {
                // Explicit font keeps the local-font DPI path.
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };
            using ToolStripMenuItem item = new("Open");
            contextMenu.Items.Add(item);
            form.ContextMenuStrip = contextMenu;

            form.Show();

            // ContextMenuStrip is top-level and receives WM_DPICHANGED, which WinForms does not
            // use to scale non-Form top-level controls directly. ContextMenuStrip.SetVisibleCore
            // recreates the handle after the popup is reopened when its window DPI changes.
            contextMenu.Show(form, new Point(10, 10));

            float initialFontSize = contextMenu.Font.Size;
            Size initialSize = contextMenu.Size;
            Size initialItemSize = item.Size;
            Size initialImageScalingSize = contextMenu.ImageScalingSize;
            int initialDpi = (int)PInvoke.GetDpiForWindow(contextMenu);

            contextMenu.Close();

            int? targetDpi = await MoveToDifferentDpiMonitorAsync(form, initialDpi);
            if (targetDpi is null)
            {
                return;
            }

            contextMenu.Show(form, new Point(10, 10));

            Assert.Equal(targetDpi.Value, contextMenu.DeviceDpi);
            Assert.NotEqual(initialFontSize, contextMenu.Font.Size);
            Assert.NotEqual(initialImageScalingSize, contextMenu.ImageScalingSize);
            Assert.NotEqual(initialItemSize, item.Size);
            Assert.NotEqual(initialSize, contextMenu.Size);

            contextMenu.Close();
            form.Close();
        }
        finally
        {
            // Reset back to original awareness context.
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }

    private async Task<int?> MoveToDifferentDpiMonitorAsync(Form form, int initialDpi)
    {
        string initialDeviceName = Screen.FromControl(form).DeviceName;

        foreach (Screen screen in Screen.AllScreens)
        {
            if (screen.DeviceName == initialDeviceName)
            {
                continue;
            }

            form.Location = screen.WorkingArea.Location;
            await WaitForIdleAsync();

            int targetDpi = (int)PInvoke.GetDpiForWindow(form);
            if (targetDpi != initialDpi)
            {
                return targetDpi;
            }
        }

        return null;
    }

    [WinFormsTheory]
    // Use a DPI that differs from common ambient values (96/192) so the scale factor is not 1.
    [InlineData(3.5 * ScaleHelper.OneHundredPercentLogicalDpi)]
    public void ToolStripDropDown_ScaleControl_ScalesMinimumAndMaximumSize(int newDpi)
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

            using ToolStripDropDown dropDown = new()
            {
                MinimumSize = new Size(100, 40),
                MaximumSize = new Size(400, 200),
            };
            using ToolStripButton item = new("Item");
            dropDown.Items.Add(item);

            form.Show();
            dropDown.Show(form, new Point(20, 20));

            Size initialMinSize = dropDown.MinimumSize;
            Size initialMaxSize = dropDown.MaximumSize;

            // MinimumSize/MaximumSize are scaled via the public Scale → ScaleControl path,
            // not via top-level WM_DPICHANGED.
            float factor = (float)newDpi / dropDown.DeviceDpi;
            dropDown.Scale(new SizeF(factor, factor));

            Size expectedMinSize = new(
                (int)Math.Round(initialMinSize.Width * factor),
                (int)Math.Round(initialMinSize.Height * factor));
            Size expectedMaxSize = new(
                (int)Math.Round(initialMaxSize.Width * factor),
                (int)Math.Round(initialMaxSize.Height * factor));

            Assert.NotEqual(initialMinSize, dropDown.MinimumSize);
            Assert.NotEqual(initialMaxSize, dropDown.MaximumSize);
            Assert.Equal(expectedMinSize, dropDown.MinimumSize);
            Assert.Equal(expectedMaxSize, dropDown.MaximumSize);

            dropDown.Close();
            form.Close();
        }
        finally
        {
            // Reset back to original awareness context.
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }
}
