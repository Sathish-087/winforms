// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using System.Windows.Forms.Layout;
using Windows.Win32.UI.HiDpi;

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
    public void SplitContainer_ListBoxAnchoredAll_TemporaryPanelGrowth_PreservesBottomAnchorInfo()
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
            int oldPanelDisplayHeight = splitContainer.Panel1.DisplayRectangle.Height;
            Size oldClientSize = form.ClientSize;
            int baselineBottom = listBox.Bottom - oldPanelDisplayHeight;
            Assert.True(baselineBottom < 0);

            // Establish the baseline after native IntegralHeight adjustment, preserving the other anchors.
            anchorInfo.Bottom = baselineBottom;

            splitContainer.Panel1.SuspendLayout();
            try
            {
                form.ClientSize = new Size(oldClientSize.Width, oldClientSize.Height + 3);
                Assert.Equal(oldPanelDisplayHeight + 3, splitContainer.Panel1.DisplayRectangle.Height);
                Assert.Equal(oldBounds, listBox.Bounds);
                int proposedBottom = listBox.Bottom - splitContainer.Panel1.DisplayRectangle.Height;
                Assert.True(proposedBottom < baselineBottom);

                // Reproduce anchor rebaking during temporary parent growth without injecting cached bounds.
                typeof(DefaultLayout).TestAccessor.Dynamic.UpdateAnchorInfo(listBox);
                DefaultLayout.AnchorInfo anchorInfoAfterRebake = Assert.IsType<DefaultLayout.AnchorInfo>(
                    DefaultLayout.GetAnchorInfo(listBox));
                Assert.Equal(baselineBottom, anchorInfoAfterRebake.Bottom);
            }
            finally
            {
                form.ClientSize = oldClientSize;
                splitContainer.Panel1.ResumeLayout(performLayout: true);
            }

            Assert.Equal(oldBounds, listBox.Bounds);
        }
        finally
        {
            typeof(ScaleHelper).TestAccessor.Dynamic.s_processPerMonitorAware = originalPerMonitorAware;
        }
    }
}
