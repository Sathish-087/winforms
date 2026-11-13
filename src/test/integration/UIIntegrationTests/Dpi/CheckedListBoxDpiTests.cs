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

    [WinFormsFact]
    public void CheckedListBox_FontChanged_HandlerSeesUpdatedNativeItemHeight()
    {
        using CheckedListBox checkedListBox = new();
        _ = checkedListBox.Handle;
        int nativeItemHeightDuringFontChanged = 0;
        checkedListBox.FontChanged += (_, _) =>
            nativeItemHeightDuringFontChanged = (int)PInvokeCore.SendMessage(
                checkedListBox,
                PInvoke.LB_GETITEMHEIGHT,
                default,
                (LPARAM)0);

        checkedListBox.Font = new Font(checkedListBox.Font.FontFamily, checkedListBox.Font.Size + 2);

        Assert.Equal(checkedListBox.ItemHeight, nativeItemHeightDuringFontChanged);
    }

    [WinFormsTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CheckedListBox_DpiChangedAfterParent_VScrollCrossesThresholdAndStateIsPreserved(
        bool explicitFont,
        bool integralHeight)
    {
        // Run tests only on Windows 10 versions that support thread dpi awareness.
        if (!PlatformDetection.IsWindows10Version1803OrGreater)
        {
            return;
        }

        dynamic scaleHelper = typeof(ScaleHelper).TestAccessor.Dynamic;
        bool originalProcessPerMonitorAware = scaleHelper.s_processPerMonitorAware;
        int originalInitialSystemDpi = ScaleHelper.InitialSystemDpi;
        DPI_AWARENESS_CONTEXT originalAwarenessContext =
            PInvoke.SetThreadDpiAwarenessContextInternal(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

        try
        {
            scaleHelper.InitializeStatics();

            using Form form = new()
            {
                AutoScaleMode = AutoScaleMode.None
            };
            form.Show();

            if (form.DeviceDpi != ScaleHelper.OneHundredPercentLogicalDpi)
            {
                DpiMessageHelper.TriggerDpiMessage(
                    PInvokeCore.WM_DPICHANGED,
                    form,
                    ScaleHelper.OneHundredPercentLogicalDpi);
            }

            Assert.Equal(ScaleHelper.OneHundredPercentLogicalDpi, form.DeviceDpi);

            using CheckedListBox checkedListBox = new()
            {
                IntegralHeight = integralHeight,
                Size = new Size(120, 94)
            };

            if (explicitFont)
            {
                checkedListBox.Font = new Font(checkedListBox.Font.FontFamily, checkedListBox.Font.Size + 2);
            }

            int itemCount = Math.Max(1, checkedListBox.ClientSize.Height / checkedListBox.ItemHeight);
            for (int i = 0; i < itemCount; i++)
            {
                checkedListBox.Items.Add($"Item {i}");
            }

            checkedListBox.SelectedIndex = itemCount - 1;
            checkedListBox.SetItemChecked(0, value: true);
            checkedListBox.SetItemChecked(itemCount - 1, value: true);

            form.Controls.Add(checkedListBox);

            checkedListBox.TopIndex = Math.Max(0, itemCount - 2);
            IntPtr originalHandle = checkedListBox.Handle;
            int itemCheckEvents = 0;
            int selectedIndexChangedEvents = 0;
            checkedListBox.ItemCheck += (_, _) => itemCheckEvents++;
            checkedListBox.SelectedIndexChanged += (_, _) => selectedIndexChangedEvents++;

            Assert.False(HasScrollableContent(checkedListBox));
            Assert.False(IsVerticalScrollBarVisible(checkedListBox));
            int originalTopIndex = checkedListBox.TopIndex;

            int newDpi = 3 * ScaleHelper.OneHundredPercentLogicalDpi / 2;
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, checkedListBox, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_AFTERPARENT, checkedListBox, newDpi);

            Assert.True(HasScrollableContent(checkedListBox));
            Assert.True(IsVerticalScrollBarVisible(checkedListBox));
            Assert.Equal(originalHandle, checkedListBox.Handle);
            Assert.Equal(originalTopIndex, checkedListBox.TopIndex);
            Assert.Equal(itemCount - 1, checkedListBox.SelectedIndex);
            Assert.True(checkedListBox.GetItemChecked(0));
            Assert.True(checkedListBox.GetItemChecked(itemCount - 1));
            Assert.Equal(0, itemCheckEvents);
            Assert.Equal(0, selectedIndexChangedEvents);
        }
        finally
        {
            scaleHelper.s_processPerMonitorAware = originalProcessPerMonitorAware;
            scaleHelper.InitialSystemDpi = originalInitialSystemDpi;
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }

    [WinFormsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void CheckedListBox_DpiChanged_RoundTripPreservesScrolledState(bool integralHeight)
    {
        if (!PlatformDetection.IsWindows10Version1803OrGreater)
        {
            return;
        }

        dynamic scaleHelper = typeof(ScaleHelper).TestAccessor.Dynamic;
        bool originalProcessPerMonitorAware = scaleHelper.s_processPerMonitorAware;
        int originalInitialSystemDpi = ScaleHelper.InitialSystemDpi;
        DPI_AWARENESS_CONTEXT originalAwarenessContext =
            PInvoke.SetThreadDpiAwarenessContextInternal(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

        try
        {
            scaleHelper.InitializeStatics();

            using Form form = new()
            {
                AutoScaleMode = AutoScaleMode.None
            };
            form.Show();

            if (form.DeviceDpi != ScaleHelper.OneHundredPercentLogicalDpi)
            {
                DpiMessageHelper.TriggerDpiMessage(
                    PInvokeCore.WM_DPICHANGED,
                    form,
                    ScaleHelper.OneHundredPercentLogicalDpi);
            }

            using CheckedListBox checkedListBox = new()
            {
                IntegralHeight = integralHeight,
                Size = new Size(120, 94)
            };

            for (int i = 0; i < 30; i++)
            {
                checkedListBox.Items.Add($"Item {i}");
            }

            checkedListBox.SelectedIndex = 15;
            checkedListBox.SetItemCheckState(2, CheckState.Checked);
            checkedListBox.SetItemCheckState(7, CheckState.Indeterminate);
            checkedListBox.SetItemCheckState(15, CheckState.Checked);
            form.Controls.Add(checkedListBox);

            int originalTopIndex = 10;
            checkedListBox.TopIndex = originalTopIndex;
            IntPtr originalHandle = checkedListBox.Handle;
            int originalSelectedIndex = checkedListBox.SelectedIndex;
            CheckState[] originalCheckStates =
            [
                checkedListBox.GetItemCheckState(2),
                checkedListBox.GetItemCheckState(7),
                checkedListBox.GetItemCheckState(15)
            ];

            Assert.True(HasScrollableContent(checkedListBox));
            Assert.True(IsVerticalScrollBarVisible(checkedListBox));
            Assert.Equal(originalTopIndex, checkedListBox.TopIndex);

            int newDpi = 3 * ScaleHelper.OneHundredPercentLogicalDpi / 2;
            foreach (int dpi in new[] { newDpi, ScaleHelper.OneHundredPercentLogicalDpi })
            {
                DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, checkedListBox, dpi);
                DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, dpi);
                DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_AFTERPARENT, checkedListBox, dpi);

                Assert.Equal(originalHandle, checkedListBox.Handle);
                Assert.Equal(originalTopIndex, checkedListBox.TopIndex);
                Assert.Equal(originalSelectedIndex, checkedListBox.SelectedIndex);
                Assert.Equal(originalCheckStates[0], checkedListBox.GetItemCheckState(2));
                Assert.Equal(originalCheckStates[1], checkedListBox.GetItemCheckState(7));
                Assert.Equal(originalCheckStates[2], checkedListBox.GetItemCheckState(15));
            }
        }
        finally
        {
            scaleHelper.s_processPerMonitorAware = originalProcessPerMonitorAware;
            scaleHelper.InitialSystemDpi = originalInitialSystemDpi;
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
