// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using System.Windows.Forms.PropertyGridInternal;
using Windows.Win32.UI.HiDpi;

namespace System.Windows.Forms.UITests.Dpi;

public class PropertyGridDpiTests : ControlTestBase
{
    public PropertyGridDpiTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [WinFormsTheory]
    // Use a DPI that differs from common ambient values (96/192) so BEFOREPARENT does not early-out.
    [InlineData(3.5 * ScaleHelper.OneHundredPercentLogicalDpi)]
    public void PropertyGrid_DpiChanged_ScalesToolbarAndGridViewMetrics(int newDpi)
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
            form.ClientSize = new Size(400, 500);

            using PropertyGrid propertyGrid = new()
            {
                Dock = DockStyle.Fill,
                HelpVisible = true,
                ToolbarVisible = true,
                SelectedObject = form,
                // Explicit font keeps the local-font DPI path.
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };

            form.Controls.Add(propertyGrid);
            form.Show();

            ToolStrip toolStrip = propertyGrid.TestAccessor.Dynamic._toolStrip;
            PropertyGridView gridView = propertyGrid.TestAccessor.Dynamic._gridView;

            Size initialImageScalingSize = toolStrip.ImageScalingSize;
            int initialToolStripHeight = toolStrip.Height;
            int initialOutlineIconSize = gridView.OutlineIconSize;
            int initialValuePaintWidth = gridView.ValuePaintWidth;

            // PropertyGrid children are excluded from parent AutoScale (_doNotScaleChildren).
            // Send BEFOREPARENT to nested controls first while their inherited font is still at the
            // old DPI; if PropertyGrid scales its font first, children early-out when fontDpi
            // already matches the new DeviceDpi and skip RescaleConstantsForDpi.
            // - toolStrip: ToolStrip.ResetScaling updates ImageScalingSize (PropertyGrid does not assign it)
            // - gridView: PropertyGridView.RescaleConstants updates outline/paint metrics
            // - propertyGrid: OriginalDeviceDpi != DeviceDpi so OnLayoutInternal rebuilds toolbar height
            // Form WM_DPICHANGED resizes the docked grid and triggers OnLayoutInternal via OnResize.
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, toolStrip, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, gridView, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, propertyGrid, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_AFTERPARENT, propertyGrid, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, newDpi);

            Assert.NotEqual(initialImageScalingSize, toolStrip.ImageScalingSize);
            Assert.NotEqual(initialToolStripHeight, toolStrip.Height);
            Assert.NotEqual(initialOutlineIconSize, gridView.OutlineIconSize);
            Assert.NotEqual(initialValuePaintWidth, gridView.ValuePaintWidth);

            form.Close();
        }
        finally
        {
            // Reset back to original awareness context.
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }

    [WinFormsTheory]
    // Use a DPI that differs from common ambient values (96/192) so BEFOREPARENT does not early-out.
    [InlineData(3.5 * ScaleHelper.OneHundredPercentLogicalDpi)]
    public void PropertyGrid_HelpPane_DpiChanged_ScalesBorderSize(int newDpi)
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
            form.ClientSize = new Size(400, 500);

            using PropertyGrid propertyGrid = new()
            {
                Dock = DockStyle.Fill,
                HelpVisible = true,
                SelectedObject = form,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };

            form.Controls.Add(propertyGrid);
            form.Show();

            Control helpPane = propertyGrid.TestAccessor.Dynamic._helpPane;
            int initialBorderSize = helpPane.TestAccessor.Dynamic._borderSize;

            // HelpPane private metrics need BEFOREPARENT; form WM_DPICHANGED completes parent scale.
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, helpPane, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, propertyGrid, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, newDpi);

            int scaledBorderSize = helpPane.TestAccessor.Dynamic._borderSize;
            Assert.NotEqual(initialBorderSize, scaledBorderSize);

            form.Close();
        }
        finally
        {
            // Reset back to original awareness context.
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }
}
