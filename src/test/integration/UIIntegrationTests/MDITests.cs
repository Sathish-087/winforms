// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using Windows.Win32.UI.HiDpi;

namespace System.Windows.Forms.UITests;

public class MDITests : ControlTestBase
{
    public MDITests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [WinFormsFact]
    public async Task MDIForm_ResizeWhenMdiChildrenMinimizedAnchorBottom_DefaultAsync()
    {
        await RunTestAsync(form =>
        {
            using Form childForm = new()
            {
                MdiParent = form,
                WindowState = FormWindowState.Minimized
            };

            childForm.Show();

            int childFormMinimizedYPositionFromBottom = form.ClientSize.Height - childForm.Top;
            form.Height += 100;

            Assert.Equal(childFormMinimizedYPositionFromBottom, form.ClientSize.Height - childForm.Top);

            return Task.CompletedTask;
        });
    }

    [WinFormsFact]
    public async Task MDIForm_ResizeWhenMdiChildrenMinimizedAnchorBottom_FalseAsync()
    {
        await RunTestAsync(form =>
        {
            using Form childForm = new()
            {
                MdiParent = form,
                WindowState = FormWindowState.Minimized
            };
            form.MdiChildrenMinimizedAnchorBottom = false;

            childForm.Show();

            int childFormMinimizedTop = childForm.Top;
            form.Height += 100;

            Assert.Equal(childFormMinimizedTop, childForm.Top);

            return Task.CompletedTask;
        });
    }

    [WinFormsFact]
    public async Task MDIForm_ClosingChild_DoesNotSuppressScalingOfUnrelatedContainerAsync()
    {
        await RunTestAsync(form =>
        {
            using Form unrelatedForm = new()
            {
                AutoScaleDimensions = new SizeF(7F, 15F),
                AutoScaleMode = AutoScaleMode.Font,
                ClientSize = new Size(400, 300),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(50, 50)
            };
            using UserControl container = new()
            {
                AutoScaleDimensions = new SizeF(7F, 15F),
                AutoScaleMode = AutoScaleMode.Font,
                Bounds = new Rectangle(40, 40, 200, 120)
            };
            using Button button = new()
            {
                Bounds = new Rectangle(10, 10, 80, 30)
            };
            container.Controls.Add(button);
            unrelatedForm.Controls.Add(container);
            unrelatedForm.Show();

            using Form childForm = new()
            {
                MdiParent = form
            };
            childForm.Show();
            childForm.Close();

            Rectangle containerBoundsBeforeFontChange = container.Bounds;
            using Font largerFont = new(unrelatedForm.Font.FontFamily, unrelatedForm.Font.Size * 1.5F);
            unrelatedForm.Font = largerFont;

            Assert.NotEqual(containerBoundsBeforeFontChange, container.Bounds);
            Assert.NotEqual(new Rectangle(10, 10, 80, 30), button.Bounds);

            return Task.CompletedTask;
        });
    }

    [WinFormsTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task MDIForm_HiddenContainer_FontChangedOnOrAfterHandleCreation_ScalesBoundsAsync(
        bool explicitFont,
        bool changeFontInHandleCreated)
    {
        await RunTestAsync(form =>
        {
            using Font initialFont = new(form.Font.FontFamily, 9F);
            using Font parentFont = new(initialFont.FontFamily, 12F);
            using Font laterFont = new(initialFont.FontFamily, 18F);
            using Form childForm = new()
            {
                MdiParent = form,
                AutoScaleMode = AutoScaleMode.Font,
                Font = initialFont
            };
            using UserControl container = new()
            {
                AutoScaleMode = AutoScaleMode.Font,
                Font = explicitFont ? initialFont : null,
                Bounds = new Rectangle(10, 10, 200, 120),
                Visible = false
            };
            using Button button = new()
            {
                Bounds = new Rectangle(10, 10, 80, 30)
            };
            container.Controls.Add(button);
            container.AutoScaleDimensions = container.CurrentAutoScaleDimensions;
            childForm.Controls.Add(container);
            childForm.AutoScaleDimensions = childForm.CurrentAutoScaleDimensions;
            childForm.Show();
            childForm.Font = parentFont;
            Assert.False(container.IsHandleCreated);

            Size containerSize = Size.Empty;
            Size buttonSize = Size.Empty;
            SizeF dimensions = SizeF.Empty;
            if (changeFontInHandleCreated)
            {
                container.HandleCreated += (_, _) => ChangeFont();
            }

            container.Visible = true;
            Assert.True(container.IsHandleCreated);
            if (!changeFontInHandleCreated)
            {
                ChangeFont();
            }

            SizeF factor = new(
                container.CurrentAutoScaleDimensions.Width / dimensions.Width,
                container.CurrentAutoScaleDimensions.Height / dimensions.Height);
            Assert.Equal(
                Size.Round(new SizeF(containerSize.Width * factor.Width, containerSize.Height * factor.Height)),
                container.Size);
            Assert.Equal(
                Size.Round(new SizeF(buttonSize.Width * factor.Width, buttonSize.Height * factor.Height)),
                button.Size);

            return Task.CompletedTask;

            void ChangeFont()
            {
                containerSize = container.Size;
                buttonSize = button.Size;
                dimensions = container.CurrentAutoScaleDimensions;
                container.Font = laterFont;
            }
        });
    }

    [WinFormsTheory]
    [InlineData(false, AutoScaleMode.None, false)]
    [InlineData(false, AutoScaleMode.None, true)]
    [InlineData(false, AutoScaleMode.Inherit, false)]
    [InlineData(false, AutoScaleMode.Inherit, true)]
    [InlineData(false, AutoScaleMode.Dpi, false)]
    [InlineData(false, AutoScaleMode.Dpi, true)]
    [InlineData(false, AutoScaleMode.Font, false)]
    [InlineData(false, AutoScaleMode.Font, true)]
    [InlineData(true, AutoScaleMode.None, false)]
    [InlineData(true, AutoScaleMode.None, true)]
    [InlineData(true, AutoScaleMode.Inherit, false)]
    [InlineData(true, AutoScaleMode.Inherit, true)]
    [InlineData(true, AutoScaleMode.Dpi, false)]
    [InlineData(true, AutoScaleMode.Dpi, true)]
    [InlineData(true, AutoScaleMode.Font, false)]
    [InlineData(true, AutoScaleMode.Font, true)]
    public async Task MDIForm_FontChanged_ScalesContainerModesCorrectlyAsync(
        bool anchorLayoutV2,
        AutoScaleMode autoScaleMode,
        bool createHandle)
    {
        await RunTestAsync(form =>
        {
            using Font initialFont = new(form.Font.FontFamily, 9F);
            using Font parentFont = new(initialFont.FontFamily, 13.5F);
            using Form parentForm = new()
            {
                AutoScaleDimensions = new SizeF(7F, 15F),
                AutoScaleMode = AutoScaleMode.Font,
                ClientSize = new Size(400, 300),
                Font = initialFont
            };
            using UserControl container = new()
            {
                AutoScaleMode = autoScaleMode,
                Bounds = new Rectangle(10, 10, 143, 100),
                Visible = false
            };
            using Button button = new()
            {
                Bounds = new Rectangle(10, 10, 40, 20)
            };
            using AnchorLayoutV2Scope scope = new(enable: anchorLayoutV2);

            container.Controls.Add(button);
            parentForm.Controls.Add(container);
            container.AutoScaleDimensions = container.CurrentAutoScaleDimensions;
            parentForm.Show();

            if (createHandle)
            {
                container.Visible = true;
            }

            Assert.Equal(createHandle, container.IsHandleCreated);
            Size containerSizeBeforeFontChange = container.Size;

            parentForm.Font = parentFont;

            bool shouldScale = autoScaleMode == AutoScaleMode.Inherit
                || (autoScaleMode == AutoScaleMode.Font && createHandle);

            Assert.True(
                shouldScale == (container.Size != containerSizeBeforeFontChange),
                $"AutoScaleMode={autoScaleMode}, createHandle={createHandle}, before={containerSizeBeforeFontChange}, after={container.Size}");

            return Task.CompletedTask;
        });
    }

    [WinFormsFact]
    public async Task MDIForm_HiddenInheritedContainer_DoesNotDoubleScaleOnHandleCreationAsync()
    {
        DPI_AWARENESS_CONTEXT originalAwarenessContext = PInvoke.SetThreadDpiAwarenessContextInternal(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        try
        {
            await RunTestAsync(form =>
            {
                using Font initialFont = new(form.Font.FontFamily, 9F);
                using Font parentFont = new(initialFont.FontFamily, 13.5F);
                using Form parentForm = new()
                {
                    AutoScaleMode = AutoScaleMode.Font,
                    AutoScaleDimensions = new SizeF(7F, 15F),
                    ClientSize = new Size(400, 300),
                    Font = initialFont
                };
                using UserControl container = new()
                {
                    AutoScaleMode = AutoScaleMode.Inherit,
                    Bounds = new Rectangle(10, 10, 143, 100),
                    Visible = false
                };
                using Button button = new()
                {
                    Bounds = new Rectangle(10, 10, 40, 20)
                };
                container.Controls.Add(button);
                parentForm.Controls.Add(container);
                parentForm.Show();
                parentForm.Font = parentFont;

                Assert.False(container.IsHandleCreated);
                Assert.True(container.BoundsScaledByParent);
                Size containerSizeBeforeHandleCreation = container.Size;
                Size buttonSizeBeforeHandleCreation = button.Size;

                // Simulate the scaling callback raised during handle creation.
                container.TestAccessor.Dynamic.SetState(0x00040000, true); // Control.States.CreatingHandle
                try
                {
                    container.Scale(SizeF.Empty, new SizeF(1.5F, 1.5F), container, causedByFontChanged: true);
                }
                finally
                {
                    container.TestAccessor.Dynamic.SetState(0x00040000, false); // Control.States.CreatingHandle
                }

                Assert.Equal(containerSizeBeforeHandleCreation, container.Size);
                Assert.Equal(buttonSizeBeforeHandleCreation, button.Size);

                return Task.CompletedTask;
            });
        }
        finally
        {
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }

    [WinFormsFact]
    public async Task MDIForm_ReparentingHandlelessContainer_ClearsParentScalingMarkerAsync()
    {
        await RunTestAsync(form =>
        {
            using Font initialFont = new(form.Font.FontFamily, 9F);
            using Font parentFont = new(initialFont.FontFamily, 13.5F);
            using Form parentForm = new()
            {
                AutoScaleMode = AutoScaleMode.Font,
                AutoScaleDimensions = new SizeF(7F, 15F),
                ClientSize = new Size(400, 300),
                Font = initialFont
            };
            using UserControl container = new()
            {
                AutoScaleMode = AutoScaleMode.Inherit,
                Bounds = new Rectangle(10, 10, 143, 100),
                Visible = false
            };

            parentForm.Controls.Add(container);
            parentForm.Show();
            parentForm.Font = parentFont;

            Assert.True(container.BoundsScaledByParent);

            parentForm.Controls.Remove(container);

            Assert.False(container.BoundsScaledByParent);

            return Task.CompletedTask;
        });
    }

    [WinFormsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void MDIForm_ReopenedChild_DoesNotDoubleScaleHandlelessContainer(bool anchorLayoutV2)
    {
        if (!PlatformDetection.IsWindows10Version1803OrGreater)
        {
            return;
        }

        DPI_AWARENESS_CONTEXT originalAwarenessContext = PInvoke.SetThreadDpiAwarenessContextInternal(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        try
        {
            typeof(ScaleHelper).TestAccessor.Dynamic.InitializeStatics();
            using AnchorLayoutV2Scope scope = new(enable: anchorLayoutV2);
            using Form form = new()
            {
                IsMdiContainer = true,
                AutoScaleMode = AutoScaleMode.Font,
                ClientSize = new Size(640, 480)
            };
            form.Show();
            bool foundDifferentDpi = false;
            foreach (Screen screen in Screen.AllScreens)
            {
                form.Location = new Point(screen.WorkingArea.Left + 50, screen.WorkingArea.Top + 50);
                if ((int)PInvoke.GetDpiForWindow(form) != ScaleHelper.InitialSystemDpi)
                {
                    foundDifferentDpi = true;
                    break;
                }
            }

            Assert.SkipWhen(!foundDifferentDpi, "Requires monitors with different DPI settings.");
            Assert.Equal((int)PInvoke.GetDpiForWindow(form), form.DeviceDpi);

            using Form firstChild = CreateDpiChildForm();
            firstChild.MdiParent = form;
            firstChild.Show();
            AssertChildScaled(firstChild);
            firstChild.Close();

            using Form reopenedChild = CreateDpiChildForm();
            reopenedChild.MdiParent = form;
            Assert.False(reopenedChild.Controls["customControl"]!.IsHandleCreated);
            Assert.NotEqual(form.DeviceDpi, reopenedChild.Controls["customControl"]!.DeviceDpi);
            reopenedChild.Show();
            AssertChildScaled(reopenedChild);

            void AssertChildScaled(Form child)
            {
                Control customControl = child.Controls["customControl"]!;
                Control pictureBox = child.Controls["pictureBox"]!;
                SizeF factor = new(
                    child.CurrentAutoScaleDimensions.Width / 7F,
                    child.CurrentAutoScaleDimensions.Height / 15F);
                Size expectedSize = Size.Round(new SizeF(100 * factor.Width, 60 * factor.Height));

                Assert.Equal(form.DeviceDpi, (int)PInvoke.GetDpiForWindow(child));
                Assert.Equal(form.DeviceDpi, customControl.DeviceDpi);
                Assert.NotEqual(new Size(100, 60), pictureBox.Size);
                Assert.Equal(expectedSize, pictureBox.Size);
                Assert.Equal(expectedSize, customControl.Size);
                Assert.Equal(pictureBox.Right, customControl.Right);
                Assert.Equal(
                    Size.Round(new SizeF(40 * factor.Width, 20 * factor.Height)),
                    customControl.Controls[0].Size);
            }
        }
        finally
        {
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
            typeof(ScaleHelper).TestAccessor.Dynamic.InitializeStatics();
        }
    }

    private async Task RunTestAsync(Func<Form, Task> runTest)
    {
        await RunFormWithoutControlAsync(
            testDriverAsync: runTest,
            createForm: () =>
            {
                return new()
                {
                    IsMdiContainer = true,
                    ClientSize = new Size(640, 480),
                };
            });
    }

    private static Form CreateDpiChildForm()
    {
        Form childForm = new()
        {
            AutoScaleDimensions = new SizeF(7F, 15F),
            AutoScaleMode = AutoScaleMode.Font,
            ClientSize = new Size(400, 300)
        };
        UserControl customControl = new()
        {
            AutoScaleDimensions = new SizeF(7F, 15F),
            AutoScaleMode = AutoScaleMode.Font,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Bounds = new Rectangle(280, 40, 100, 60),
            Name = "customControl"
        };
        PictureBox pictureBox = new()
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Bounds = new Rectangle(280, 120, 100, 60),
            Name = "pictureBox"
        };
        customControl.Controls.Add(new Button
        {
            Bounds = new Rectangle(10, 10, 40, 20)
        });
        childForm.Controls.Add(pictureBox);
        childForm.Controls.Add(customControl);
        return childForm;
    }
}
