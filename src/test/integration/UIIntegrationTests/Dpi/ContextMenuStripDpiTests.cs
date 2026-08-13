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

    [WinFormsTheory]
    // Use a DPI that differs from common ambient values (96/192) so BEFOREPARENT does not early-out.
    [InlineData(3.5 * ScaleHelper.OneHundredPercentLogicalDpi)]
    public void ContextMenuStrip_TopLevel_DpiChanged_ScalesFontAndItemMetrics(int newDpi)
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

            // ContextMenuStrip is top-level: the OS only delivers WM_DPICHANGED (not BEFOREPARENT),
            // and WinForms intentionally does not scale non-Form top-levels on WM_DPICHANGED.
            // Product scaling for CMS is HandleHighDpi after RecreateHandle when DeviceDpi differs
            // from GetDpiForWindow (see ContextMenuStrip.SetVisibleCore). Synthetic WM_DPICHANGED
            // does not change the HWND DPI, so Close/Show cannot force that path here.
            // Exercise the same Control DPI path used by ToolStripItemDpiTests: inject BEFOREPARENT
            // so DeviceDpi, Font, and ToolStrip private metrics rescale.
            contextMenu.Show(form, new Point(10, 10));

            float initialFontSize = contextMenu.Font.Size;
            Size initialSize = contextMenu.Size;
            Size initialItemSize = item.Size;
            Size initialImageScalingSize = contextMenu.ImageScalingSize;

            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, contextMenu, newDpi);

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
            // not via top-level WM_DPICHANGED. ToolStripDropDown.ScaleControl calls
            // base.ScaleControl (scales Min/Max once) then scales Min/Max again, so the
            // effective factor is applied twice.
            float factor = (float)newDpi / dropDown.DeviceDpi;
            dropDown.Scale(new SizeF(factor, factor));

            Size expectedMinSize = new(
                (int)Math.Round((int)Math.Round(initialMinSize.Width * factor) * factor),
                (int)Math.Round((int)Math.Round(initialMinSize.Height * factor) * factor));
            Size expectedMaxSize = new(
                (int)Math.Round((int)Math.Round(initialMaxSize.Width * factor) * factor),
                (int)Math.Round((int)Math.Round(initialMaxSize.Height * factor) * factor));

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
