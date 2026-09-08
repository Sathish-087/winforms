// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace System.Windows.Forms.UITests.Dpi;

public class CheckedListBoxDpiTests : ControlTestBase
{
    private const OBJECT_IDENTIFIER VerticalScrollBarObjectId = (OBJECT_IDENTIFIER)(-5);
    private const uint StateSystemInvisible = 0x00008000;

    public CheckedListBoxDpiTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [WinFormsTheory]
    [InlineData(3 * ScaleHelper.OneHundredPercentLogicalDpi)]
    public void CheckedListBox_DpiChangedAfterParent_VScrollVisibleAndTopIndexPreserved(int newDpi)
    {
        // Run tests only on Windows 10 versions that support thread dpi awareness.
        if (!PlatformDetection.IsWindows10Version1803OrGreater)
        {
            return;
        }

        DPI_AWARENESS_CONTEXT originalAwarenessContext =
            PInvoke.SetThreadDpiAwarenessContextInternal(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        typeof(ScaleHelper).TestAccessor.Dynamic.InitializeStatics();
        try
        {
            using Form form = new()
            {
                AutoScaleMode = AutoScaleMode.Font
            };

            using CheckedListBox checkedListBox = new()
            {
                IntegralHeight = false,
                Size = new Size(120, 94)
            };

            for (int i = 0; i < 100; i++)
            {
                checkedListBox.Items.Add($"Item {i}");
            }

            checkedListBox.SelectedIndex = 55;
            checkedListBox.SetItemChecked(5, value: true);
            checkedListBox.SetItemChecked(60, value: true);

            form.Controls.Add(checkedListBox);
            form.Show();

            checkedListBox.TopIndex = 50;

            Assert.True(HasScrollableContent(checkedListBox));
            Assert.True(IsVerticalScrollBarVisible(checkedListBox));
            Assert.Equal(50, checkedListBox.TopIndex);

            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, checkedListBox, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_AFTERPARENT, checkedListBox, newDpi);

            Assert.True(HasScrollableContent(checkedListBox));
            Assert.True(IsVerticalScrollBarVisible(checkedListBox));
            Assert.Equal(50, checkedListBox.TopIndex);
            Assert.Equal(55, checkedListBox.SelectedIndex);
            Assert.True(checkedListBox.GetItemChecked(5));
            Assert.True(checkedListBox.GetItemChecked(60));
        }
        finally
        {
            // Reset back to original awareness context.
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }

    private static bool HasScrollableContent(CheckedListBox listBox)
        => listBox.Items.Count * listBox.ItemHeight > listBox.ClientSize.Height;

    private static unsafe bool IsVerticalScrollBarVisible(CheckedListBox listBox)
    {
        SCROLLBARINFO scrollBarInfo = new()
        {
            cbSize = (uint)sizeof(SCROLLBARINFO)
        };

        return PInvoke.GetScrollBarInfo((HWND)listBox.Handle, VerticalScrollBarObjectId, ref scrollBarInfo)
            && (scrollBarInfo.rgstate[0] & StateSystemInvisible) == 0;
    }
}
