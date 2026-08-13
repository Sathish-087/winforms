// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using Windows.Win32.UI.HiDpi;

namespace System.Windows.Forms.UITests.Dpi;

public class ListBoxDpiTests : ControlTestBase
{
    public ListBoxDpiTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [WinFormsTheory]
    // Use a DPI that differs from common ambient values (96/192) so BEFOREPARENT does not early-out.
    [InlineData(3.5 * ScaleHelper.OneHundredPercentLogicalDpi)]
    public void ListBox_DpiChanged_ScalesItemPaddingConstants(int newDpi)
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

            using ListBox listBox = new()
            {
                Location = new Point(10, 10),
                Size = new Size(160, 120),
                // Explicit font keeps the local-font DPI path.
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };
            listBox.Items.AddRange(["One", "Two", "Three"]);

            form.Controls.Add(listBox);
            form.Show();

            int initialStartPosition = listBox.TestAccessor.Dynamic._listItemStartPosition;
            int initialBordersHeight = listBox.TestAccessor.Dynamic._listItemBordersHeight;
            int initialPaddingBuffer = listBox.TestAccessor.Dynamic._listItemPaddingBuffer;
            Size initialSize = listBox.Size;
            float initialFontSize = listBox.Font.Size;

            // Mirror SplitContainerTests: child BEFOREPARENT then top-level form WM_DPICHANGED.
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, listBox, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, newDpi);

            Assert.NotEqual(initialStartPosition, (int)listBox.TestAccessor.Dynamic._listItemStartPosition);
            Assert.NotEqual(initialBordersHeight, (int)listBox.TestAccessor.Dynamic._listItemBordersHeight);
            Assert.NotEqual(initialPaddingBuffer, (int)listBox.TestAccessor.Dynamic._listItemPaddingBuffer);
            Assert.NotEqual(initialSize, listBox.Size);
            Assert.NotEqual(initialFontSize, listBox.Font.Size);

            form.Close();
        }
        finally
        {
            // Reset back to original awareness context.
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }
}
