// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;

namespace System.Windows.Forms.UITests;

public class TabControlTests : ControlTestBase
{
    public TabControlTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [WinFormsFact]
    public async Task TabControl_TabPage_IsHoveredWithMouse_IsTrue_WhenMouseIsOn_FirstTabAsync()
    {
        await RunTestAsync(async (form, tabControl) =>
        {
            // TODO: find the way to determin the tab's dimensions and use those instead of (10, 10)
            bool result = await IsHoveredWithMouseAsync(form, tabControl, new(10, 10));

            Assert.True(result);
        });
    }

    [WinFormsFact]
    public async Task TabControl_TabPage_IsHoveredWithMouse_IsTrue_WhenMouseIsOn_SecondTabAsync()
    {
        await RunTestAsync(async (form, tabControl) =>
        {
            // TODO: find the way to determin the tab's dimensions and use those instead of (60, 10)
            bool result = await IsHoveredWithMouseAsync(form, tabControl, new(60, 10));

            Assert.True(result);
        });
    }

    [WinFormsFact]
    public async Task TabControl_TabPage_IsHoveredWithMouse_IsFalse_WhenMouseIs_OutsideControlAsync()
    {
        await RunTestAsync(async (form, tabControl) =>
        {
            // TODO: find the way to determin the tab's dimensions and use those instead of (300, 300)
            bool result = await IsHoveredWithMouseAsync(form, tabControl, new(300, 300));

            Assert.False(result);
        });
    }

    [WinFormsFact]
    public async Task TabControl_TabPage_IsHoveredWithMouse_IsFalse_WhenMouseIs_OutsideMainScreenAsync()
    {
        await RunTestAsync(async (form, tabControl) =>
        {
            TestOutputHelper.WriteLine($"Primary screen: {Screen.PrimaryScreen?.WorkingArea}");

            // We can't physically move the mouse outside the desktop area,
            // so the mouse cursor will be stuck at the bottom right corner of the desktop.
            bool result = await IsHoveredWithMouseAsync(form, tabControl, new(1000, 1000), assertCorrectLocation: false);

            Assert.False(result);
        });
    }

    private async Task<bool> IsHoveredWithMouseAsync(Form form, TabControl tabControl, Point point, bool assertCorrectLocation = true)
    {
        await MoveMouseAsync(form, tabControl.PointToScreen(point), assertCorrectLocation);

        bool result = ((IKeyboardToolTip)tabControl).IsHoveredWithMouse();
        bool resultOfPage1 = ((IKeyboardToolTip)tabControl.TabPages[0]).IsHoveredWithMouse();
        bool resultOfPage2 = ((IKeyboardToolTip)tabControl.TabPages[1]).IsHoveredWithMouse();
        return result && resultOfPage1 && resultOfPage2;
    }

    private async Task RunTestAsync(Func<Form, TabControl, Task> runTest)
    {
        await RunSingleControlTestAsync(
            testDriverAsync: runTest,
            createControl: () =>
            {
                TabControl tabControl = new()
                {
                    Location = new Point(0, 0)
                };

                TabPage tabPage1 = new();
                TabPage tabPage2 = new();
                tabControl.TabPages.Add(tabPage1);
                tabControl.TabPages.Add(tabPage2);

                return tabControl;
            });
    }

    [WinFormsFact]
    public async Task TabControl_ControlsDoNotReorderWhenSelectedIndexChanges()
    {
        // Validates the following bug fix: https://github.com/dotnet/winforms/issues/7837
        await RunSingleControlTestAsync(
            testDriverAsync: async (form, tabControl) =>
            {
                var originalPages = tabControl.TabPages.Cast<TabPage>().ToArray();

                tabControl.SelectedIndex--;
                await Task.Yield();

                // The changes we made to SelectedIndex will result in a WM_WINDOWPOSCHANGED message which will
                // fire TabPage.WmWindowPosChanged, which will call TabControl.UpdateChildControlIndex.

                // This test ensures that the latter method short-circuits and does not call Controls.SetChildIndex
                // which would inappropriately reorder the Controls collection, resulting in TabPages and Controls
                // becoming out of sync.

                for (int i = 0; i < originalPages.Length; i++)
                {
                    Assert.Equal(tabControl.Controls[i], tabControl.TabPages[i]);
                }

                // The following test proves that it *matters* that the Controls and TabPages collection remain in sync.

                // Remove the first page by position:
                tabControl.TabPages.RemoveAt(0);

                // We should be left with pages[1] and pages[2]. This will not be the case if the Controls and TabPages
                // collections are out of sync.
                Assert.Equal(originalPages[1], tabControl.TabPages[0]);
                Assert.Equal(originalPages[2], tabControl.TabPages[1]);
            },
            createControl: () =>
            {
                SubclassedTabControl tabControl = new()
                {
                    Location = new Point(0, 0)
                };

                TabPage tabPage1 = new() { Text = "Page 1" };
                TabPage tabPage2 = new() { Text = "Page 2" };
                TabPage tabPage3 = new() { Text = "Page 3" };

                tabControl.TabPages.Add(tabPage1);
                tabControl.TabPages.Add(tabPage2);
                tabControl.TabPages.Add(tabPage3);

                // Start with the Selected Page at the end:
                tabControl.SelectedIndex = tabControl.TabPages.Count - 1;

                return tabControl;
            });
    }

    // Bug https://github.com/dotnet/winforms/issues/7837 occured only when TabControl was subclassed.
    private class SubclassedTabControl : TabControl { }

    [WinFormsFact]
    public async Task TabControl_Properties_RoundTripAsync()
    {
        await RunSingleControlTestAsync(
            testDriverAsync: (form, tabControl) =>
            {
                tabControl.Alignment = TabAlignment.Left;
                tabControl.Appearance = TabAppearance.Normal;
                tabControl.SizeMode = TabSizeMode.Normal;
                tabControl.Multiline = true;
                tabControl.Padding = new Point(8, 4);
                tabControl.HotTrack = true;
                tabControl.ShowToolTips = true;

                Assert.Equal(TabAlignment.Left, tabControl.Alignment);
                Assert.Equal(TabAppearance.Normal, tabControl.Appearance);
                Assert.Equal(TabSizeMode.Normal, tabControl.SizeMode);
                Assert.True(tabControl.Multiline);
                Assert.Equal(new Point(8, 4), tabControl.Padding);
                Assert.True(tabControl.HotTrack);
                Assert.True(tabControl.ShowToolTips);

                return Task.CompletedTask;
            },
            createControl: () =>
            {
                TabControl tabControl = new()
                {
                    Location = new Point(0, 0)
                };

                TabPage tabPage1 = new();
                TabPage tabPage2 = new();
                tabControl.TabPages.Add(tabPage1);
                tabControl.TabPages.Add(tabPage2);

                return tabControl;
            });
    }

    [WinFormsFact]
    public async Task TabControl_SelectedIndex_ChangesRaiseEventAsync()
    {
        await RunSingleControlTestAsync(
            testDriverAsync: (form, tabControl) =>
            {
                int selectedIndexChangedCount = 0;
                int selectedCount = 0;
                TabControlEventArgs? lastSelectedArgs = null;

                tabControl.SelectedIndexChanged += (sender, e) => selectedIndexChangedCount++;
                tabControl.Selected += (sender, e) =>
                {
                    selectedCount++;
                    lastSelectedArgs = e;
                };

                Assert.Equal(0, tabControl.SelectedIndex);
                Assert.Same(tabControl.TabPages[0], tabControl.SelectedTab);

                tabControl.SelectedIndex = 1;
                Assert.Equal(1, tabControl.SelectedIndex);
                Assert.Same(tabControl.TabPages[1], tabControl.SelectedTab);

                tabControl.SelectedTab = tabControl.TabPages[0];
                Assert.Equal(0, tabControl.SelectedIndex);

                Assert.True(selectedIndexChangedCount >= 2);
                Assert.True(selectedCount >= 2);
                Assert.NotNull(lastSelectedArgs);
                Assert.Same(tabControl.TabPages[0], lastSelectedArgs!.TabPage);

                return Task.CompletedTask;
            },
            createControl: () =>
            {
                TabControl tabControl = new()
                {
                    Location = new Point(0, 0)
                };

                tabControl.TabPages.Add(new TabPage { Text = "First" });
                tabControl.TabPages.Add(new TabPage { Text = "Second" });
                return tabControl;
            });
    }

    [WinFormsFact]
    public async Task TabControl_TabPages_AddAndRemove_UpdatesCountAndControlsAsync()
    {
        await RunSingleControlTestAsync(
            testDriverAsync: (form, tabControl) =>
            {
                Assert.Equal(2, tabControl.TabPages.Count);
                Assert.Equal(2, tabControl.Controls.Count);

                TabPage newPage = new() { Text = "Third" };
                tabControl.TabPages.Add(newPage);

                Assert.Equal(3, tabControl.TabPages.Count);
                Assert.Equal(3, tabControl.Controls.Count);
                Assert.True(tabControl.TabPages.Contains(newPage));

                tabControl.TabPages.Remove(newPage);
                Assert.Equal(2, tabControl.TabPages.Count);
                Assert.False(tabControl.TabPages.Contains(newPage));

                return Task.CompletedTask;
            },
            createControl: () =>
            {
                TabControl tabControl = new()
                {
                    Location = new Point(0, 0)
                };

                tabControl.TabPages.Add(new TabPage { Text = "A" });
                tabControl.TabPages.Add(new TabPage { Text = "B" });
                return tabControl;
            });
    }

    [WinFormsFact]
    public async Task TabControl_TabPage_Text_RoundTripsAsync()
    {
        await RunSingleControlTestAsync(
            testDriverAsync: (form, tabControl) =>
            {
                tabControl.TabPages[0].Text = "Hello";
                tabControl.TabPages[1].Text = "&World";

                Assert.Equal("Hello", tabControl.TabPages[0].Text);
                Assert.Equal("&World", tabControl.TabPages[1].Text);
                Assert.False(tabControl.TabPages[0].UseVisualStyleBackColor);

                return Task.CompletedTask;
            },
            createControl: () =>
            {
                TabControl tabControl = new()
                {
                    Location = new Point(0, 0)
                };

                tabControl.TabPages.Add(new TabPage());
                tabControl.TabPages.Add(new TabPage());
                return tabControl;
            });
    }

    [WinFormsFact]
    public async Task TabControl_SelectedIndex_InvalidValue_ThrowsArgumentOutOfRangeExceptionAsync()
    {
        await RunSingleControlTestAsync(
            testDriverAsync: (form, tabControl) =>
            {
                // SelectedIndex only allows values >= -1; anything lower throws.
                Assert.Throws<ArgumentOutOfRangeException>("value", () => tabControl.SelectedIndex = -5);
                Assert.Equal(0, tabControl.SelectedIndex);

                tabControl.SelectedIndex = -1;
                Assert.Equal(-1, tabControl.SelectedIndex);

                tabControl.SelectedIndex = 0;
                Assert.Equal(0, tabControl.SelectedIndex);

                return Task.CompletedTask;
            },
            createControl: () =>
            {
                TabControl tabControl = new()
                {
                    Location = new Point(0, 0)
                };

                tabControl.TabPages.Add(new TabPage());
                return tabControl;
            });
    }

    [WinFormsFact]
    public async Task TabControl_RowCount_ReflectsMultilineAndTabsAsync()
    {
        await RunSingleControlTestAsync(
            testDriverAsync: (form, tabControl) =>
            {
                tabControl.Multiline = true;
                tabControl.Alignment = TabAlignment.Top;

                Assert.True(tabControl.RowCount >= 1);

                return Task.CompletedTask;
            },
            createControl: () =>
            {
                TabControl tabControl = new()
                {
                    Location = new Point(0, 0)
                };

                tabControl.TabPages.Add(new TabPage());
                tabControl.TabPages.Add(new TabPage());
                return tabControl;
            });
    }

    [WinFormsFact]
    public async Task TabControl_GetTabRect_ReturnsNonEmptyForPagesAsync()
    {
        await RunSingleControlTestAsync(
            testDriverAsync: (form, tabControl) =>
            {
                Rectangle rect0 = tabControl.GetTabRect(0);
                Rectangle rect1 = tabControl.GetTabRect(1);

                Assert.True(rect0.Width > 0);
                Assert.True(rect0.Height > 0);
                Assert.True(rect1.Width > 0);
                Assert.True(rect1.Height > 0);
                Assert.NotEqual(rect0, rect1);

                Assert.Throws<ArgumentOutOfRangeException>(() => tabControl.GetTabRect(99));

                return Task.CompletedTask;
            },
            createControl: () =>
            {
                TabControl tabControl = new()
                {
                    Location = new Point(0, 0)
                };

                tabControl.TabPages.Add(new TabPage());
                tabControl.TabPages.Add(new TabPage());
                return tabControl;
            });
    }
}
