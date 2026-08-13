// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using Windows.Win32.UI.HiDpi;

namespace System.Windows.Forms.UITests.Dpi;

public class CheckBoxDpiTests : ControlTestBase
{
    public CheckBoxDpiTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [WinFormsTheory]
    // Use a DPI that differs from common ambient values (96/192) so BEFOREPARENT does not early-out.
    [InlineData(3.5 * ScaleHelper.OneHundredPercentLogicalDpi)]
    public void CheckBox_DpiChanged_ScalesFlatSystemStyleConstants(int newDpi)
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

            using CheckBox checkBox = new()
            {
                Text = "CheckBox",
                AutoSize = true,
                Location = new Point(10, 10),
                // Explicit font keeps the local-font DPI path and invalidates preferred-size cache.
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };

            form.Controls.Add(checkBox);
            form.Show();

            int initialPaddingWidth = checkBox.TestAccessor.Dynamic._flatSystemStylePaddingWidth;
            int initialMinimumHeight = checkBox.TestAccessor.Dynamic._flatSystemStyleMinimumHeight;
            Size initialPreferredSize = checkBox.PreferredSize;

            // Mirror SplitContainerTests: child BEFOREPARENT then top-level form WM_DPICHANGED.
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, checkBox, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, newDpi);

            Assert.NotEqual(initialPaddingWidth, (int)checkBox.TestAccessor.Dynamic._flatSystemStylePaddingWidth);
            Assert.NotEqual(initialMinimumHeight, (int)checkBox.TestAccessor.Dynamic._flatSystemStyleMinimumHeight);
            Assert.NotEqual(initialPreferredSize, checkBox.PreferredSize);

            form.Close();
        }
        finally
        {
            // Reset back to original awareness context.
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }
}
