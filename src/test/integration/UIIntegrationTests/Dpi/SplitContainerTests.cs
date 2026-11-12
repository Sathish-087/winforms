// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using System.Windows.Forms.Layout;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace System.Windows.Forms.UITests.Dpi;

public class SplitContainerTests : ControlTestBase
{
    public SplitContainerTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [WinFormsFact]
    public void SplitContainer_Constructor()
    {
        using SplitContainer sc = new();

        Assert.NotNull(sc);
        Assert.NotNull(sc.Panel1);
        Assert.Equal(sc, sc.Panel1.Owner);
        Assert.NotNull(sc.Panel2);
        Assert.Equal(sc, sc.Panel2.Owner);
        Assert.False(sc.SplitterRectangle.IsEmpty);
    }

    [WinFormsTheory]
    [InlineData(3.5 * 96)]
    public void SplitContainer_Properties_HorizontalSplitter_Scaling(int newDpi)
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
            using SplitContainer splitContainer = new()
            {
                FixedPanel = FixedPanel.Panel1,
                Location = new Point(0, 0),
                Margin = new Padding(0),
                Name = "splitContainer2",
                Orientation = Orientation.Horizontal,
                Size = new Size(812, 619),
                SplitterDistance = 90,
                SplitterWidth = 2
            };

            form.AutoScaleMode = AutoScaleMode.Dpi;
            form.Controls.Add(splitContainer);
            form.Show();

            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED_BEFOREPARENT, splitContainer, newDpi);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, newDpi);

            Assert.NotEqual(90, splitContainer.SplitterDistance);
            Assert.NotEqual(2, splitContainer.SplitterWidth);
            Assert.Equal(splitContainer.SplitterDistance, splitContainer.Panel1.Height);
            form.Close();
        }
        finally
        {
            // Reset back to original awareness context.
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }

    [WinFormsFact]
    public void ListBox_TemporaryParentGrowthDuringDpiTransition_PreservesBottomAnchor()
    {
        using AnchorLayoutV2Scope scope = new(enable: false);
        bool originalPerMonitorAware = typeof(ScaleHelper).TestAccessor.Dynamic.s_processPerMonitorAware;

        try
        {
            typeof(ScaleHelper).TestAccessor.Dynamic.s_processPerMonitorAware = true;

            using Form form = new()
            {
                AutoScaleMode = AutoScaleMode.None,
                ClientSize = new Size(400, 800)
            };
            using SplitContainer splitContainer = new()
            {
                Size = form.ClientSize,
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                FixedPanel = FixedPanel.Panel2,
                SplitterDistance = 600
            };
            using ListBox listBox = new()
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(10, 10),
                Size = new Size(60, 20)
            };

            listBox.Items.AddRange(["A", "B", "C", "D", "E", "F", "G", "H", "I", "J"]);
            splitContainer.Panel1.Controls.Add(listBox);
            form.Controls.Add(splitContainer);
            form.Show();

            Assert.True(listBox.IsHandleCreated);
            Assert.False(DefaultLayout.UseAnchorLayoutV2(listBox));

            DefaultLayout.AnchorInfo anchorInfo = Assert.IsType<DefaultLayout.AnchorInfo>(
                DefaultLayout.GetAnchorInfo(listBox));

            Rectangle oldBounds = listBox.Bounds;
            int oldPanelHeight = splitContainer.Panel1.DisplayRectangle.Height;
            Size oldClientSize = form.ClientSize;
            int baselineBottom = listBox.Bottom - oldPanelHeight;

            Assert.True(baselineBottom < 0);
            anchorInfo.Bottom = baselineBottom;

            form.TestAccessor.Dynamic._isScaledByDpiChangedEvent = true;
            splitContainer.Panel1.SuspendLayout();

            try
            {
                form.ClientSize = new Size(oldClientSize.Width, oldClientSize.Height + 3);

                Assert.Equal(oldPanelHeight + 3, splitContainer.Panel1.DisplayRectangle.Height);
                Assert.Equal(oldBounds, listBox.Bounds);

                int proposedBottom = listBox.Bottom - splitContainer.Panel1.DisplayRectangle.Height;
                Assert.True(proposedBottom < baselineBottom);

                typeof(DefaultLayout).TestAccessor.Dynamic.UpdateAnchorInfo(listBox);

                DefaultLayout.AnchorInfo updatedAnchorInfo = Assert.IsType<DefaultLayout.AnchorInfo>(
                    DefaultLayout.GetAnchorInfo(listBox));

                Assert.Equal(baselineBottom, updatedAnchorInfo.Bottom);
            }
            finally
            {
                try
                {
                    form.ClientSize = oldClientSize;
                    splitContainer.Panel1.ResumeLayout(performLayout: true);
                }
                finally
                {
                    form.TestAccessor.Dynamic._isScaledByDpiChangedEvent = false;
                }
            }

            Assert.Equal(oldBounds, listBox.Bounds);
        }
        finally
        {
            typeof(ScaleHelper).TestAccessor.Dynamic.s_processPerMonitorAware = originalPerMonitorAware;
        }
    }

    [WinFormsTheory]
    [InlineData(AutoScaleMode.Font)]
    [InlineData(AutoScaleMode.Dpi)]
    public void ListBox_RepeatedDpiTransitions_PreservesHeight(AutoScaleMode autoScaleMode)
    {
        if (!PlatformDetection.IsWindows10Version1803OrGreater)
        {
            Assert.Skip("Per-monitor DPI awareness is unavailable.");
        }

        Screen[] screens = Screen.AllScreens.OrderBy(screen => screen.Bounds.X).ToArray();
        if (screens.Length < 2)
        {
            Assert.Skip("Two monitors are required.");
        }

        if (!(bool)typeof(ScaleHelper).TestAccessor.Dynamic.s_processPerMonitorAware)
        {
            Assert.Skip("The test process must be per-monitor DPI aware.");
        }

        DPI_AWARENESS_CONTEXT originalAwarenessContext = PInvoke.SetThreadDpiAwarenessContextInternal(
            DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

        try
        {
            using AnchorLayoutV2Scope scope = new(enable: false);
            using Form form = new()
            {
                AutoScaleDimensions = autoScaleMode == AutoScaleMode.Dpi
                    ? new SizeF(96, 96)
                    : new SizeF(7, 15),
                AutoScaleMode = autoScaleMode,
                ClientSize = new Size(198, 368),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(screens[0].WorkingArea.X + 100, screens[0].WorkingArea.Y + 100)
            };
            using SplitContainer splitContainer = new()
            {
                Size = form.ClientSize,
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 217
            };
            using ListBox listBox = new()
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                FormattingEnabled = true,
                Location = new Point(52, 12),
                Size = new Size(60, 169)
            };

            listBox.Items.AddRange(
            [
                "A", "B", "C", "D", "E", "F", "G", "H", "I", "J",
                "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T",
                "U", "V", "W", "X", "Y", "Z", "0", "1", "2", "3"
            ]);

            splitContainer.Panel1.Controls.Add(listBox);
            form.Controls.Add(splitContainer);
            form.Show();

            if (!WaitForNativeDpi(form, 96))
            {
                Assert.Skip("The left monitor must use 96 DPI.");
            }

            Assert.Equal(96, form.DeviceDpi);
            int originalHeight = listBox.Height;

            for (int i = 0; i < 3; i++)
            {
                MoveToScreen(form, screens[1]);

                if (!WaitForNativeDpi(form, 144))
                {
                    Assert.Skip("The right monitor must use 144 DPI.");
                }

                Assert.Equal(144, form.DeviceDpi);

                MoveToScreen(form, screens[0]);
                Assert.True(WaitForNativeDpi(form, 96));
                Assert.Equal(96, form.DeviceDpi);

                Application.DoEvents();
                form.PerformLayout();
                Assert.Equal(originalHeight, listBox.Height);
            }
        }
        finally
        {
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }

    private static void MoveToScreen(Form form, Screen screen)
    {
        PInvoke.SetWindowPos(
            form,
            HWND.Null,
            screen.WorkingArea.X + 100,
            screen.WorkingArea.Y + 100,
            0,
            0,
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER
                | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE
                | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
    }

    private static bool WaitForNativeDpi(Form form, int expectedDpi)
    {
        Stopwatch timeout = Stopwatch.StartNew();

        while (timeout.Elapsed < TimeSpan.FromSeconds(5))
        {
            Application.DoEvents();

            if ((int)PInvoke.GetDpiForWindow(form) == expectedDpi)
            {
                return true;
            }
        }

        return false;
    }
}
