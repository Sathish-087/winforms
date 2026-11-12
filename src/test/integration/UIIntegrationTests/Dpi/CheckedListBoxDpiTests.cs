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

            DpiMessageHelper.TriggerDpiMessage(
                PInvokeCore.WM_DPICHANGED_BEFOREPARENT, checkedListBox, ScaleHelper.OneHundredPercentLogicalDpi);
            DpiMessageHelper.TriggerDpiMessage(
                PInvokeCore.WM_DPICHANGED, form, ScaleHelper.OneHundredPercentLogicalDpi);
            DpiMessageHelper.TriggerDpiMessage(
                PInvokeCore.WM_DPICHANGED_AFTERPARENT, checkedListBox, ScaleHelper.OneHundredPercentLogicalDpi);

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

    [WinFormsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ListBox_DpiChangedWithoutSelection_PreservesTopIndex(bool checkedListBox)
    {
        if (!PlatformDetection.IsWindows10Version1803OrGreater)
        {
            return;
        }

        DPI_AWARENESS_CONTEXT originalAwarenessContext =
            PInvoke.SetThreadDpiAwarenessContextInternal(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        typeof(ScaleHelper).TestAccessor.Dynamic.InitializeStatics();
        try
        {
            using Form form = new();
            using ListBox listBox = checkedListBox ? new CheckedListBox() : new ListBox();
            listBox.IntegralHeight = false;
            listBox.Size = new Size(120, 94);
            for (int i = 0; i < 100; i++)
            {
                listBox.Items.Add($"Item {i}");
            }

            form.Controls.Add(listBox);
            form.Show();
            listBox.TopIndex = 50;
            Assert.Equal(50, listBox.TopIndex);
            Assert.Equal(-1, listBox.SelectedIndex);

            int newDpi = 3 * ScaleHelper.OneHundredPercentLogicalDpi;
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, listBox, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_AFTERPARENT, listBox, newDpi);

            Assert.Equal(50, listBox.TopIndex);
            Assert.Equal(-1, listBox.SelectedIndex);
        }
        finally
        {
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }

    [WinFormsFact]
    public void CheckedListBox_DpiChangedAfterParent_SelectionHandlerSeesAndCanChangeCheckStates()
    {
        if (!PlatformDetection.IsWindows10Version1803OrGreater)
        {
            return;
        }

        DPI_AWARENESS_CONTEXT originalAwarenessContext =
            PInvoke.SetThreadDpiAwarenessContextInternal(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        typeof(ScaleHelper).TestAccessor.Dynamic.InitializeStatics();
        try
        {
            using Form form = new();
            using CheckedListBox checkedListBox = new();
            checkedListBox.Items.AddRange(["one", "two", "three"]);
            checkedListBox.SelectedIndex = 1;
            checkedListBox.SetItemCheckState(0, CheckState.Checked);
            checkedListBox.SetItemCheckState(1, CheckState.Indeterminate);
            form.Controls.Add(checkedListBox);
            form.Show();

            bool selectionChanged = false;
            bool observedIncorrectState = false;
            checkedListBox.SelectedIndexChanged += (_, _) =>
            {
                selectionChanged = true;
                observedIncorrectState |= checkedListBox.GetItemCheckState(0) != CheckState.Checked
                    || checkedListBox.GetItemCheckState(1) != CheckState.Indeterminate;
                checkedListBox.SetItemChecked(2, value: true);
            };

            int newDpi = 3 * ScaleHelper.OneHundredPercentLogicalDpi;
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, checkedListBox, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_AFTERPARENT, checkedListBox, newDpi);

            Assert.False(observedIncorrectState);
            Assert.True(selectionChanged);
            Assert.True(checkedListBox.GetItemChecked(2));
            Assert.Equal(1, checkedListBox.SelectedIndex);
            Assert.Equal(CheckState.Checked, checkedListBox.GetItemCheckState(0));
            Assert.Equal(CheckState.Indeterminate, checkedListBox.GetItemCheckState(1));
        }
        finally
        {
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
