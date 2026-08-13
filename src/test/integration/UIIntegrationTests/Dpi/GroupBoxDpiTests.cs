// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using Windows.Win32.UI.HiDpi;

namespace System.Windows.Forms.UITests.Dpi;

public class GroupBoxDpiTests : ControlTestBase
{
    public GroupBoxDpiTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [WinFormsTheory]
    // Use a DPI that differs from common ambient values (96/192) so BEFOREPARENT does not early-out.
    [InlineData(3.5 * ScaleHelper.OneHundredPercentLogicalDpi)]
    public void GroupBox_DpiChanged_ScalesSizeAndFont(int newDpi)
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

            using GroupBox groupBox = new()
            {
                Text = "Group",
                Location = new Point(10, 10),
                Size = new Size(220, 120),
                // Explicit font keeps the local-font DPI path.
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };
            using Label child = new()
            {
                Text = "Child",
                Location = new Point(12, 28),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };
            groupBox.Controls.Add(child);
            form.Controls.Add(groupBox);
            form.Show();

            Size initialSize = groupBox.Size;
            float initialFontSize = groupBox.Font.Size;
            Point initialChildLocation = child.Location;

            // Mirror SplitContainerTests: child BEFOREPARENT then top-level form WM_DPICHANGED.
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, groupBox, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, child, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, newDpi);

            Assert.NotEqual(initialSize, groupBox.Size);
            Assert.NotEqual(initialFontSize, groupBox.Font.Size);
            Assert.NotEqual(initialChildLocation, child.Location);

            form.Close();
        }
        finally
        {
            // Reset back to original awareness context.
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }
}
