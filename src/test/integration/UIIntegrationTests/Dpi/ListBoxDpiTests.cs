// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace System.Windows.Forms.UITests.Dpi;

public class ListBoxDpiTests : ControlTestBase
{
    public ListBoxDpiTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [WinFormsTheory]
    [InlineData(SelectionMode.MultiExtended, true, 15)]
    [InlineData(SelectionMode.MultiExtended, false, 15)]
    [InlineData(SelectionMode.MultiSimple, true, 15)]
    [InlineData(SelectionMode.MultiSimple, false, 15)]
    [InlineData(SelectionMode.MultiExtended, true, 25)]
    public void ListBox_DpiChanged_MultipleSelection_PreservesCaretAndSelection(
        SelectionMode selectionMode,
        bool hasSelection,
        int caretIndex)
    {
        if (!PlatformDetection.IsWindows10Version1803OrGreater)
        {
            return;
        }

        DPI_AWARENESS_CONTEXT originalAwarenessContext =
            PInvoke.SetThreadDpiAwarenessContextInternal(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

        try
        {
            using Form form = new() { AutoScaleMode = AutoScaleMode.Font };
            using ListBox listBox = new()
            {
                IntegralHeight = false,
                SelectionMode = selectionMode,
                Size = new Size(120, 94)
            };

            for (int i = 0; i < 30; i++)
            {
                listBox.Items.Add($"Item {i}");
            }

            form.Controls.Add(listBox);
            form.Show();
            int[] selectedIndices = hasSelection ? [4, 8] : [];
            foreach (int index in selectedIndices)
            {
                listBox.SetSelected(index, true);
            }

            PInvokeCore.SendMessage(listBox, PInvoke.LB_SETANCHORINDEX, (WPARAM)3);
            PInvokeCore.SendMessage(listBox, PInvoke.LB_SETCARETINDEX, (WPARAM)caretIndex);
            listBox.TopIndex = 10;

            Assert.Equal(3, (int)PInvokeCore.SendMessage(listBox, PInvoke.LB_GETANCHORINDEX));
            Assert.Equal(caretIndex, (int)PInvokeCore.SendMessage(listBox, PInvoke.LB_GETCARETINDEX));
            Assert.Equal(selectedIndices, listBox.SelectedIndices.Cast<int>());
            Assert.Equal(10, listBox.TopIndex);

            int originalDpi = form.DeviceDpi;
            int newDpi = originalDpi == 3 * ScaleHelper.OneHundredPercentLogicalDpi / 2
                ? ScaleHelper.OneHundredPercentLogicalDpi
                : 3 * ScaleHelper.OneHundredPercentLogicalDpi / 2;
            foreach (int dpi in new[] { newDpi, originalDpi })
            {
                DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, listBox, dpi);
                DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, dpi);
                DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_AFTERPARENT, listBox, dpi);

                Assert.Equal(selectedIndices, listBox.SelectedIndices.Cast<int>());
                Assert.Equal(10, listBox.TopIndex);
                Assert.Equal(3, (int)PInvokeCore.SendMessage(listBox, PInvoke.LB_GETANCHORINDEX));
                Assert.Equal(caretIndex, (int)PInvokeCore.SendMessage(listBox, PInvoke.LB_GETCARETINDEX));
            }
        }
        finally
        {
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }

    [WinFormsFact]
    public void ListBox_DpiChanged_EmptyMultipleSelection_RemainsEmpty()
    {
        if (!PlatformDetection.IsWindows10Version1803OrGreater)
        {
            return;
        }

        DPI_AWARENESS_CONTEXT originalAwarenessContext =
            PInvoke.SetThreadDpiAwarenessContextInternal(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

        try
        {
            using Form form = new();
            using ListBox listBox = new() { SelectionMode = SelectionMode.MultiExtended };
            form.Controls.Add(listBox);
            form.Show();

            int newDpi = form.DeviceDpi == 144 ? 96 : 144;
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, listBox, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_AFTERPARENT, listBox, newDpi);

            Assert.True(listBox.IsHandleCreated);
            Assert.Empty(listBox.Items);
            Assert.Empty(listBox.SelectedIndices);
        }
        finally
        {
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }

    [WinFormsFact]
    public void ListBox_DpiChanged_SingleSelectionWithoutSelection_PreservesCaret()
    {
        if (!PlatformDetection.IsWindows10Version1803OrGreater)
        {
            return;
        }

        DPI_AWARENESS_CONTEXT originalAwarenessContext =
            PInvoke.SetThreadDpiAwarenessContextInternal(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

        try
        {
            using Form form = new() { AutoScaleMode = AutoScaleMode.Font };
            using ListBox listBox = new()
            {
                IntegralHeight = false,
                SelectionMode = SelectionMode.One,
                Size = new Size(120, 94)
            };
            int handleCreatedCount = 0;
            int handleDestroyedCount = 0;
            listBox.HandleCreated += (_, _) => handleCreatedCount++;
            listBox.HandleDestroyed += (_, _) => handleDestroyedCount++;

            for (int i = 0; i < 30; i++)
            {
                listBox.Items.Add($"Item {i}");
            }

            form.Controls.Add(listBox);
            form.Show();
            int initialHandleCreatedCount = handleCreatedCount;
            int initialHandleDestroyedCount = handleDestroyedCount;
            listBox.SelectedIndex = -1;

            PInvokeCore.SendMessage(listBox, PInvoke.LB_SETCARETINDEX, (WPARAM)15);
            listBox.TopIndex = 10;

            Assert.Equal(-1, listBox.SelectedIndex);
            Assert.Equal(15, (int)PInvokeCore.SendMessage(listBox, PInvoke.LB_GETCARETINDEX));
            Assert.Equal(10, listBox.TopIndex);

            int newDpi = form.DeviceDpi == 144 ? 96 : 144;
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, listBox, newDpi);

            Assert.Equal(initialHandleCreatedCount, handleCreatedCount);
            Assert.Equal(initialHandleDestroyedCount, handleDestroyedCount);
            Assert.Equal(-1, listBox.SelectedIndex);
            Assert.Equal(15, (int)PInvokeCore.SendMessage(listBox, PInvoke.LB_GETCARETINDEX));
            Assert.Equal(10, listBox.TopIndex);
        }
        finally
        {
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }

    [WinFormsTheory]
    [InlineData(false, RightToLeft.No)]
    [InlineData(true, RightToLeft.No)]
    [InlineData(false, RightToLeft.Yes)]
    [InlineData(true, RightToLeft.Yes)]
    public void ListBox_DpiChanged_PreservesHorizontalScrollPosition(bool checkedListBox, RightToLeft rightToLeft)
    {
        if (!PlatformDetection.IsWindows10Version1803OrGreater)
        {
            return;
        }

        DPI_AWARENESS_CONTEXT originalAwarenessContext =
            PInvoke.SetThreadDpiAwarenessContextInternal(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

        try
        {
            using Form form = new() { AutoScaleMode = AutoScaleMode.Font };
            using ListBox listBox = checkedListBox ? new CheckedListBox() : new ListBox();
            listBox.Size = new Size(120, 94);
            listBox.IntegralHeight = false;
            listBox.HorizontalScrollbar = true;
            listBox.HorizontalExtent = 1000;
            listBox.RightToLeft = rightToLeft;
            listBox.Items.Add("A list item wide enough to scroll horizontally.");

            form.Controls.Add(listBox);
            form.Show();
            int handlesCreated = 0;
            listBox.HandleCreated += (_, _) => handlesCreated++;

            const int position = 80;
            PInvokeCore.SendMessage(
                listBox,
                PInvokeCore.WM_HSCROLL,
                WPARAM.MAKEWPARAM((int)SCROLLBAR_COMMAND.SB_THUMBPOSITION, position));
            Assert.Equal(position, GetHorizontalScrollPosition(listBox));

            int newDpi = form.DeviceDpi == 144 ? 96 : 144;
            foreach (int dpi in new[] { newDpi, form.DeviceDpi })
            {
                DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, listBox, dpi);
                Assert.Equal(position, GetHorizontalScrollPosition(listBox));

                DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, dpi);
                Assert.Equal(position, GetHorizontalScrollPosition(listBox));

                DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_AFTERPARENT, listBox, dpi);
                Assert.Equal(position, GetHorizontalScrollPosition(listBox));
            }

            Assert.Equal(0, handlesCreated);
        }
        finally
        {
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }

    private static unsafe int GetHorizontalScrollPosition(ListBox listBox)
    {
        SCROLLINFO scrollInfo = new()
        {
            cbSize = (uint)sizeof(SCROLLINFO),
            fMask = SCROLLINFO_MASK.SIF_POS
        };

        Assert.True(PInvoke.GetScrollInfo(listBox, SCROLLBAR_CONSTANTS.SB_HORZ, ref scrollInfo));
        return scrollInfo.nPos;
    }
}
