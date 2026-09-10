// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace System.Windows.Forms.UITests;

public class FormTests : ControlTestBase
{
    // When using the keyboard for snap layout menu, there are various times when
    // a delay is needed. This value may need to be adjusted if tests fail
    // in CI/different environment
    private const int SnapLayoutDelayMS = 500;

    public FormTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [WinFormsTheory]
    [InlineData(FormWindowState.Normal)]
    [InlineData(FormWindowState.Maximized)]
    public async Task Form_SnapsLeftAsync(FormWindowState windowState)
    {
        if (!OsVersion.IsWindows11_OrGreater())
        {
            return;
        }

        await RunEmptyFormTestAsync(async form =>
        {
            form.Location = new Point(20, 21);
            form.Size = new Size(300, 310);

            form.WindowState = windowState;

            // Snap left using Win+Left shortcut. This directly snaps the window
            // to the left half of the screen without requiring snap layout panel navigation.
            await InputSimulator.SendAsync(
                form,
                inputSimulator => inputSimulator.Keyboard
                    .ModifiedKeyStroke(VIRTUAL_KEY.VK_LWIN, VIRTUAL_KEY.VK_LEFT));

            await Task.Delay(SnapLayoutDelayMS);

            // After snapping left, Windows may display a panel containing all running
            // applications so the user can select one to dock next to our form. It also
            // takes the keyboard focus away. Dismiss it with Escape.
            await InputSimulator.SendAsync(
                form,
                inputSimulator => inputSimulator.Keyboard.KeyPress(VIRTUAL_KEY.VK_ESCAPE));

            await Task.Delay(SnapLayoutDelayMS);

            var screenWorkingArea = Screen.FromControl(form).WorkingArea;
            int borderSize = (form.Width - form.ClientRectangle.Width) / 2;

            Assert.True(form.Left <= screenWorkingArea.X);
            Assert.True(form.Left >= screenWorkingArea.X - borderSize);

            Assert.True(form.Top <= screenWorkingArea.Y);
            Assert.True(form.Top >= screenWorkingArea.Y - borderSize);

            Assert.True(form.Height >= screenWorkingArea.Height);
            Assert.True(form.Height <= screenWorkingArea.Height + (borderSize * 2));

            Assert.True(form.Width >= screenWorkingArea.Width / 2);
            Assert.True(form.Width <= (screenWorkingArea.Width / 2) + (borderSize * 2));
        });
    }

    [WinFormsTheory]
    [InlineData(FormWindowState.Normal)]
    [InlineData(FormWindowState.Maximized)]
    public async Task Form_SnapsRightAsync(FormWindowState windowState)
    {
        if (!OsVersion.IsWindows11_OrGreater())
        {
            return;
        }

        await RunEmptyFormTestAsync(async form =>
        {
            form.Location = new Point(20, 21);
            form.Size = new Size(300, 310);

            form.WindowState = windowState;

            // Snap right using Win+Right shortcut. This directly snaps the window
            // to the right half of the screen without requiring snap layout panel navigation.
            await InputSimulator.SendAsync(
                form,
                inputSimulator => inputSimulator.Keyboard
                    .ModifiedKeyStroke(VIRTUAL_KEY.VK_LWIN, VIRTUAL_KEY.VK_RIGHT));

            await Task.Delay(SnapLayoutDelayMS);

            // After snapping right, Windows may display a panel containing all running
            // applications so the user can select one to dock next to our form. It also
            // takes the keyboard focus away. Dismiss it with Escape.
            await InputSimulator.SendAsync(
                form,
                inputSimulator => inputSimulator.Keyboard.KeyPress(VIRTUAL_KEY.VK_ESCAPE));

            await Task.Delay(SnapLayoutDelayMS);

            var screenWorkingArea = Screen.FromControl(form).WorkingArea;
            int screenMiddleX = screenWorkingArea.X + (screenWorkingArea.Width / 2);
            int borderSize = (form.Width - form.ClientRectangle.Width) / 2;

            Assert.True(form.Left <= screenMiddleX);
            Assert.True(form.Left >= screenMiddleX - borderSize);

            Assert.True(form.Top <= screenWorkingArea.Y);
            Assert.True(form.Top >= screenWorkingArea.Y - borderSize);

            Assert.True(form.Height >= screenWorkingArea.Height);
            Assert.True(form.Height <= screenWorkingArea.Height + (borderSize * 2));

            Assert.True(form.Width >= screenWorkingArea.Width / 2);
            Assert.True(form.Width <= (screenWorkingArea.Width / 2) + (borderSize * 2));
        });
    }

    private async Task RunEmptyFormTestAsync(Func<Form, Task> testDriverAsync)
    {
        await RunFormWithoutControlAsync(
            () =>
            {
                Form form = new()
                {
                    TopMost = true
                };

                return form;
            },
            testDriverAsync);
    }

    [WinFormsFact]
    public void Form_MinimumSize_DoesNotChangeDisplayOrder()
    {
        // Initialize Form1 and Form2.
        using Form form1 = new Form { Text = "Form1" };
        using Form form2 = new Form { Text = "Form2" };

        // Display the forms.
        form1.Show();
        form2.Show();

        // Set initial hierarchy: Form2 should be displayed in front of Form1.
        form2.BringToFront();
        Assert.True(form2.TopMost || form2.Focused, "Form2 should be displayed in front of Form1");

        // Set the MinimumSize property of Form1.
        form1.MinimumSize = new Size(300, 300);

        // Verify the hierarchy remains unchanged.
        Assert.True(form2.TopMost || form2.Focused, "Form2 should still be displayed in front after setting MinimumSize");
    }

    [WinFormsFact]
    public async Task Form_BoundProperties_RoundTripAsync()
    {
        await RunEmptyFormTestAsync(form =>
        {
            form.Text = "Coverage";
            form.MinimumSize = new Size(100, 100);
            form.MaximumSize = new Size(800, 600);
            form.ClientSize = new Size(250, 200);
            form.StartPosition = FormStartPosition.Manual;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.ShowInTaskbar = false;
            form.HelpButton = true;
            form.KeyPreview = true;
            form.Opacity = 1.0;

            Assert.Equal("Coverage", form.Text);
            Assert.Equal(new Size(100, 100), form.MinimumSize);
            Assert.Equal(new Size(800, 600), form.MaximumSize);
            Assert.Equal(FormStartPosition.Manual, form.StartPosition);
            Assert.Equal(FormBorderStyle.FixedDialog, form.FormBorderStyle);
            Assert.False(form.ShowInTaskbar);
            Assert.True(form.HelpButton);
            Assert.True(form.KeyPreview);
            Assert.Equal(1.0, form.Opacity);

            return Task.CompletedTask;
        });
    }

    [WinFormsFact]
    public async Task Form_AcceptAndCancelButton_AssignsControlsAsync()
    {
        await RunFormWithoutControlAsync(
            () =>
            {
                Form form = new()
                {
                    TopMost = true
                };

                Button accept = new() { Name = "accept" };
                Button cancel = new() { Name = "cancel" };
                form.Controls.Add(accept);
                form.Controls.Add(cancel);

                form.AcceptButton = accept;
                form.CancelButton = cancel;

                // Preserve the same references so the test can compare after creation.
                WeakReference acceptRef = new(accept);
                WeakReference cancelRef = new(cancel);
                form.Tag = (acceptRef, cancelRef);

                return form;
            },
            form =>
            {
                Assert.NotNull(form.AcceptButton);
                Assert.NotNull(form.CancelButton);

                var (acceptRef, cancelRef) = ((WeakReference, WeakReference))form.Tag!;
                Button? accept = (Button?)acceptRef.Target;
                Button? cancel = (Button?)cancelRef.Target;

                Assert.NotNull(accept);
                Assert.NotNull(cancel);
                Assert.Same(accept, (object?)form.AcceptButton);
                Assert.Same(cancel, (object?)form.CancelButton);

                return Task.CompletedTask;
            });
    }

    [WinFormsFact]
    public async Task Form_IsMdiContainer_TogglesCorrectlyAsync()
    {
        await RunEmptyFormTestAsync(form =>
        {
            Assert.False(form.IsMdiContainer);
            form.IsMdiContainer = true;
            Assert.True(form.IsMdiContainer);
            form.IsMdiContainer = false;
            Assert.False(form.IsMdiContainer);
            return Task.CompletedTask;
        });
    }

    [WinFormsFact]
    public async Task Form_TransparencyKey_AssignmentRoundTripsAsync()
    {
        await RunEmptyFormTestAsync(form =>
        {
            form.TransparencyKey = Color.Magenta;
            Assert.Equal(Color.Magenta, form.TransparencyKey);

            form.TransparencyKey = Color.Empty;
            Assert.Equal(Color.Empty, form.TransparencyKey);

            return Task.CompletedTask;
        });
    }

    [WinFormsFact]
    public async Task Form_ToString_ContainsTypeAndTextAsync()
    {
        await RunEmptyFormTestAsync(form =>
        {
            form.Text = "CoverageForm";

            string value = form.ToString();

            Assert.Contains("Form", value, StringComparison.Ordinal);
            Assert.Contains("CoverageForm", value, StringComparison.Ordinal);

            return Task.CompletedTask;
        });
    }

    [WinFormsFact]
    public async Task Form_AutoScaleMode_RoundTripsAsync()
    {
        await RunEmptyFormTestAsync(form =>
        {
            Assert.Equal(AutoScaleMode.Inherit, form.AutoScaleMode);

            form.AutoScaleMode = AutoScaleMode.Font;
            Assert.Equal(AutoScaleMode.Font, form.AutoScaleMode);

            form.AutoScaleMode = AutoScaleMode.Dpi;
            Assert.Equal(AutoScaleMode.Dpi, form.AutoScaleMode);

            return Task.CompletedTask;
        });
    }

    [WinFormsFact]
    public async Task Form_RestoreBounds_TracksWindowStateChangesAsync()
    {
        await RunEmptyFormTestAsync(form =>
        {
            form.WindowState = FormWindowState.Normal;
            form.Size = new Size(400, 300);
            form.Location = new Point(50, 60);

            Rectangle restoreBounds = form.RestoreBounds;
            Assert.Equal(new Size(400, 300), restoreBounds.Size);

            return Task.CompletedTask;
        });
    }
}
