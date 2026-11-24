// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using Moq;

namespace System.Windows.Forms.Tests;

#if NET11_0_OR_GREATER
public class KioskModeManagerTests
{
    [WinFormsFact]
    public void KioskModeManager_Ctor_Default()
    {
        using SubKioskModeManager manager = new();

        Assert.False(manager.AlwaysOn);
        Assert.Null(manager.Container);
        Assert.Null(manager.ContainerControl);
        Assert.False(manager.DesignMode);
        Assert.True(manager.EscapeExitsFullScreen);
        Assert.False(manager.FullScreen);
        Assert.Equal(0, manager.MousePointerAutoHideDelay);
        Assert.Null(manager.Site);
        Assert.Equal(Keys.F11, manager.ToggleFullScreenKeys);
        Assert.False(manager.TopMostInFullScreen);
    }

    [WinFormsFact]
    public void KioskModeManager_Ctor_IContainer()
    {
        using Container container = new();
        using KioskModeManager manager = new(container);

        Assert.Same(container, manager.Container);
        Assert.NotNull(manager.Site);
    }

    [WinFormsFact]
    public void KioskModeManager_Ctor_NullContainer_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("container", () => new KioskModeManager(null));
    }

    [WinFormsFact]
    public void KioskModeManager_ContainerControl_Set_GetReturnsExpected()
    {
        using Form form = new();
        using KioskModeManager manager = new()
        {
            ContainerControl = form
        };

        Assert.Same(form, manager.ContainerControl);

        manager.ContainerControl = form;
        Assert.Same(form, manager.ContainerControl);

        manager.ContainerControl = null;
        Assert.Null(manager.ContainerControl);
    }

    [WinFormsFact]
    public void KioskModeManager_ContainerControl_SetWithHandler_CallsContainerControlChanged()
    {
        using Form form = new();
        using KioskModeManager manager = new();
        int callCount = 0;
        EventHandler handler = (sender, e) =>
        {
            Assert.Same(manager, sender);
            Assert.Same(EventArgs.Empty, e);
            callCount++;
        };

        manager.ContainerControlChanged += handler;
        manager.ContainerControl = form;
        Assert.Equal(1, callCount);

        manager.ContainerControl = form;
        Assert.Equal(1, callCount);

        manager.ContainerControl = null;
        Assert.Equal(2, callCount);

        manager.ContainerControlChanged -= handler;
        manager.ContainerControl = form;
        Assert.Equal(2, callCount);
    }

    [WinFormsTheory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3000)]
    public void KioskModeManager_MousePointerAutoHideDelay_Set_GetReturnsExpected(int value)
    {
        using KioskModeManager manager = new()
        {
            MousePointerAutoHideDelay = value
        };

        Assert.Equal(value, manager.MousePointerAutoHideDelay);

        manager.MousePointerAutoHideDelay = value;
        Assert.Equal(value, manager.MousePointerAutoHideDelay);
    }

    [WinFormsFact]
    public void KioskModeManager_MousePointerAutoHideDelay_SetNegative_ThrowsArgumentOutOfRangeException()
    {
        using KioskModeManager manager = new();

        Assert.Throws<ArgumentOutOfRangeException>("value", () => manager.MousePointerAutoHideDelay = -1);
    }

    [WinFormsFact]
    public void KioskModeManager_FullScreen_WithMousePointerAutoHideDelay_StartsTimer()
    {
        using Form form = new();
        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            MousePointerAutoHideDelay = 3000,
            FullScreen = true
        };

        Timer timer = manager.TestAccessor.Dynamic._mousePointerAutoHideTimer;

        Assert.True(timer.Enabled);
        Assert.Equal(3000, timer.Interval);
    }

    [WinFormsFact]
    public void KioskModeManager_MousePointerAutoHideDelay_SetWhileFullScreen_UpdatesTimer()
    {
        using Form form = new();
        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            MousePointerAutoHideDelay = 1000,
            FullScreen = true
        };

        manager.MousePointerAutoHideDelay = 2500;
        Timer timer = manager.TestAccessor.Dynamic._mousePointerAutoHideTimer;

        Assert.True(timer.Enabled);
        Assert.Equal(2500, timer.Interval);
    }

    [WinFormsFact]
    public void KioskModeManager_MousePointerAutoHideDelay_SetZero_ShowsPointerAndStopsTimer()
    {
        using Form form = new();
        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            MousePointerAutoHideDelay = 1000,
            FullScreen = true
        };
        manager.TestAccessor.Dynamic.OnMousePointerAutoHideTimerTick(null, EventArgs.Empty);

        manager.MousePointerAutoHideDelay = 0;
        Timer timer = manager.TestAccessor.Dynamic._mousePointerAutoHideTimer;

        Assert.False(manager.TestAccessor.Dynamic._isCursorHidden);
        Assert.False(timer.Enabled);
    }

    [WinFormsTheory]
    [BoolData]
    public void KioskModeManager_MousePointerAutoHideTimerTick_WhenInactive_DoesNotHidePointer(
        bool fullScreen)
    {
        using Form form = new();
        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            MousePointerAutoHideDelay = fullScreen ? 0 : 1000,
            FullScreen = fullScreen
        };

        manager.TestAccessor.Dynamic.OnMousePointerAutoHideTimerTick(null, EventArgs.Empty);

        Assert.False(manager.TestAccessor.Dynamic._isCursorHidden);
    }

    [WinFormsTheory]
    [InlineData(Keys.F11)]
    [InlineData(Keys.None)]
    [InlineData(Keys.Alt | Keys.Enter)]
    [InlineData(Keys.Control | Keys.Shift | Keys.F)]
    public void KioskModeManager_ToggleFullScreenKeys_Set_GetReturnsExpected(Keys value)
    {
        using KioskModeManager manager = new()
        {
            ToggleFullScreenKeys = value
        };

        Assert.Equal(value, manager.ToggleFullScreenKeys);

        manager.ToggleFullScreenKeys = value;
        Assert.Equal(value, manager.ToggleFullScreenKeys);
    }

    [WinFormsTheory]
    [BoolData]
    public void KioskModeManager_AlwaysOn_Set_GetReturnsExpected(bool value)
    {
        using KioskModeManager manager = new()
        {
            AlwaysOn = value
        };

        Assert.Equal(value, manager.AlwaysOn);

        manager.AlwaysOn = value;
        Assert.Equal(value, manager.AlwaysOn);

        manager.AlwaysOn = !value;
        Assert.Equal(!value, manager.AlwaysOn);
    }

    [WinFormsTheory]
    [InlineData(Keys.Alt | Keys.Enter)]
    [InlineData(Keys.Control | Keys.Shift | Keys.F)]
    public void KioskModeManager_ProcessKeyboardActivity_MatchingKeyCombination_TogglesFullScreen(Keys keys)
    {
        using Form form = new();
        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            ToggleFullScreenKeys = keys
        };

        manager.TestAccessor.Dynamic.ProcessKeyboardActivity(keys, false);
        Assert.True(manager.FullScreen);

        manager.TestAccessor.Dynamic.ProcessKeyboardActivity(keys & Keys.KeyCode, false);
        Assert.True(manager.FullScreen);

        manager.TestAccessor.Dynamic.ProcessKeyboardActivity(keys, false);
        Assert.False(manager.FullScreen);
    }

    [WinFormsFact]
    public void KioskModeManager_ProcessKeyboardActivity_KeysNone_DoesNotToggleFullScreen()
    {
        using Form form = new();
        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            ToggleFullScreenKeys = Keys.None
        };

        manager.TestAccessor.Dynamic.ProcessKeyboardActivity(Keys.None, false);

        Assert.False(manager.FullScreen);
    }

    [WinFormsFact]
    public void KioskModeManager_ToggleFullScreen_Form_RestoresExpected()
    {
        using Form form = new()
        {
            Bounds = new Rectangle(10, 20, 300, 200),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            WindowState = FormWindowState.Normal,
            TopMost = false
        };

        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            TopMostInFullScreen = true
        };
        Rectangle screenBounds = Screen.FromRectangle(form.Bounds).Bounds;

        manager.ToggleFullScreen();

        Assert.True(manager.FullScreen);
        Assert.Equal(FormBorderStyle.None, form.FormBorderStyle);
        Assert.True(form.TopMost);
        Assert.Equal(FormWindowState.Normal, form.WindowState);
        Assert.Equal(screenBounds, form.Bounds);

        manager.ToggleFullScreen();

        Assert.False(manager.FullScreen);
        Assert.Equal(FormBorderStyle.FixedDialog, form.FormBorderStyle);
        Assert.False(form.TopMost);
        Assert.Equal(FormWindowState.Normal, form.WindowState);
        Assert.Equal(new Rectangle(10, 20, 300, 200), form.Bounds);
    }

    [WinFormsFact]
    public void KioskModeManager_FullScreen_CoversCompleteScreenBounds()
    {
        using Form form = new()
        {
            Bounds = new Rectangle(10, 20, 300, 200)
        };
        using KioskModeManager manager = new()
        {
            ContainerControl = form
        };
        Rectangle screenBounds = Screen.FromRectangle(form.Bounds).Bounds;

        manager.FullScreen = true;

        Assert.Equal(FormWindowState.Normal, form.WindowState);
        Assert.Equal(screenBounds, form.Bounds);
    }

    [WinFormsFact]
    public void KioskModeManager_FullScreen_MinimizedForm_UsesRestoreMonitor()
    {
        using Form form = new()
        {
            Bounds = new Rectangle(10, 20, 300, 200),
            StartPosition = FormStartPosition.Manual
        };
        form.Show();
        form.WindowState = FormWindowState.Minimized;
        Rectangle restoreBounds = form.RestoreBounds;
        Rectangle screenBounds = Screen.FromRectangle(
            restoreBounds).Bounds;
        using KioskModeManager manager = new()
        {
            ContainerControl = form
        };

        manager.FullScreen = true;

        Assert.Equal(screenBounds, form.Bounds);
    }

    [WinFormsFact]
    public void KioskModeManager_FullScreen_RefreshBounds_ReappliesScreenBounds()
    {
        using Form form = new()
        {
            Bounds = new Rectangle(10, 20, 300, 200)
        };
        Rectangle screenBounds = Screen.FromRectangle(
            form.Bounds).Bounds;
        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            FullScreen = true
        };
        form.Bounds = new Rectangle(
            screenBounds.X + 10,
            screenBounds.Y + 10,
            300,
            200);

        manager.TestAccessor.Dynamic.RefreshFullScreenBounds();

        Assert.Equal(screenBounds, form.Bounds);
    }

    [WinFormsFact]
    public void KioskModeManager_FullScreen_StatusStripDropDown_RemainsAttached()
    {
        using Form form = new()
        {
            Bounds = new Rectangle(10, 20, 300, 200)
        };
        using StatusStrip statusStrip = new();
        using ToolStripDropDownButton dropDownButton = new("Options");
        dropDownButton.DropDownItems.Add("First");
        statusStrip.Items.Add(dropDownButton);
        form.Controls.Add(statusStrip);
        using KioskModeManager manager = new()
        {
            ContainerControl = form
        };

        form.Show();
        manager.FullScreen = true;
        dropDownButton.ShowDropDown();

        Rectangle itemScreenBounds = statusStrip.RectangleToScreen(dropDownButton.Bounds);
        Assert.InRange(
            Math.Abs(dropDownButton.DropDown.Bounds.Bottom - itemScreenBounds.Top),
            0,
            1);
    }

    [WinFormsFact]
    public void KioskModeManager_ToggleFullScreen_UserControlContainer_UsesParentForm()
    {
        using Form form = new();
        using UserControl userControl = new();
        form.Controls.Add(userControl);
        using KioskModeManager manager = new()
        {
            ContainerControl = userControl
        };

        manager.ToggleFullScreen();

        Assert.True(manager.FullScreen);
        Assert.Equal(FormBorderStyle.None, form.FormBorderStyle);
    }

    [WinFormsFact]
    public void KioskModeManager_ToggleFullScreen_UserControlContainerParentedAfterSet_UsesParentForm()
    {
        using Form form = new();
        using UserControl userControl = new();
        using KioskModeManager manager = new()
        {
            ContainerControl = userControl
        };

        form.Controls.Add(userControl);
        manager.ToggleFullScreen();

        Assert.True(manager.FullScreen);
        Assert.Equal(FormBorderStyle.None, form.FormBorderStyle);
    }

    [WinFormsFact]
    public void KioskModeManager_FullScreen_SetBeforeUserControlIsParented_EntersWhenParented()
    {
        using Form form = new();
        using UserControl userControl = new();
        using KioskModeManager manager = new()
        {
            ContainerControl = userControl,
            FullScreen = true
        };

        form.Controls.Add(userControl);

        Assert.True(manager.FullScreen);
        Assert.Equal(FormBorderStyle.None, form.FormBorderStyle);
    }

    [WinFormsFact]
    public void KioskModeManager_FullScreen_UserControlReparented_TransfersToNewForm()
    {
        using Form firstForm = new()
        {
            FormBorderStyle = FormBorderStyle.FixedDialog
        };
        using Form secondForm = new();
        using UserControl userControl = new();
        firstForm.Controls.Add(userControl);
        using KioskModeManager manager = new()
        {
            ContainerControl = userControl,
            FullScreen = true
        };

        secondForm.Controls.Add(userControl);

        Assert.True(manager.FullScreen);
        Assert.Equal(FormBorderStyle.FixedDialog, firstForm.FormBorderStyle);
        Assert.Equal(FormBorderStyle.None, secondForm.FormBorderStyle);
    }

    [WinFormsFact]
    public void KioskModeManager_FullScreen_AncestorReparented_TransfersToNewForm()
    {
        using Form firstForm = new()
        {
            FormBorderStyle = FormBorderStyle.FixedDialog
        };
        using Form secondForm = new();
        using Panel panel = new();
        using UserControl userControl = new();
        panel.Controls.Add(userControl);
        firstForm.Controls.Add(panel);
        using KioskModeManager manager = new()
        {
            ContainerControl = userControl,
            FullScreen = true
        };

        secondForm.Controls.Add(panel);

        Assert.True(manager.FullScreen);
        Assert.Equal(FormBorderStyle.FixedDialog, firstForm.FormBorderStyle);
        Assert.Equal(FormBorderStyle.None, secondForm.FormBorderStyle);
    }

    [WinFormsFact]
    public void KioskModeManager_FullScreen_SetBeforeAncestorIsParented_EntersWhenParented()
    {
        using Form form = new();
        using Panel panel = new();
        using UserControl userControl = new();
        panel.Controls.Add(userControl);
        using KioskModeManager manager = new()
        {
            ContainerControl = userControl,
            FullScreen = true
        };

        form.Controls.Add(panel);

        Assert.True(manager.FullScreen);
        Assert.Equal(FormBorderStyle.None, form.FormBorderStyle);
    }

    [WinFormsFact]
    public void KioskModeManager_ContainerControl_SetWhileFullScreen_RestoresPreviousForm()
    {
        using Form form = new()
        {
            Bounds = new Rectangle(10, 20, 300, 200),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            WindowState = FormWindowState.Normal
        };

        using KioskModeManager manager = new()
        {
            ContainerControl = form
        };

        manager.ToggleFullScreen();
        Assert.True(manager.FullScreen);

        manager.ContainerControl = null;

        Assert.False(manager.FullScreen);
        Assert.Equal(FormBorderStyle.FixedDialog, form.FormBorderStyle);
        Assert.Equal(FormWindowState.Normal, form.WindowState);
        Assert.Equal(new Rectangle(10, 20, 300, 200), form.Bounds);
    }

    [WinFormsFact]
    public void KioskModeManager_ContainerControl_Set_DoesNotChangeFormKeyPreview()
    {
        using Form form = new()
        {
            KeyPreview = false
        };

        using KioskModeManager manager = new()
        {
            ContainerControl = form
        };

        Assert.False(form.KeyPreview);
    }

    [WinFormsFact]
    public void KioskModeManager_FullScreen_SetTrueThenFalse_EntersAndExitsFullScreen()
    {
        using Form form = new()
        {
            Bounds = new Rectangle(10, 20, 300, 200),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            WindowState = FormWindowState.Normal
        };

        using KioskModeManager manager = new()
        {
            ContainerControl = form
        };

        manager.FullScreen = true;

        Assert.True(manager.FullScreen);
        Assert.Equal(FormBorderStyle.None, form.FormBorderStyle);

        manager.FullScreen = false;

        Assert.False(manager.FullScreen);
        Assert.Equal(FormBorderStyle.FixedDialog, form.FormBorderStyle);
        Assert.Equal(FormWindowState.Normal, form.WindowState);
        Assert.Equal(new Rectangle(10, 20, 300, 200), form.Bounds);
    }

    [WinFormsFact]
    public void KioskModeManager_FullScreen_TopMostDisabled_OverridesAndRestoresFormTopMost()
    {
        using Form form = new()
        {
            TopMost = true
        };
        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            TopMostInFullScreen = false
        };

        manager.FullScreen = true;

        Assert.False(form.TopMost);

        manager.FullScreen = false;

        Assert.True(form.TopMost);
    }

    [WinFormsFact]
    public void KioskModeManager_FullScreen_SetWithHandler_CallsFullScreenChanged()
    {
        using Form form = new();
        using KioskModeManager manager = new()
        {
            ContainerControl = form
        };

        int callCount = 0;
        EventHandler handler = (sender, e) =>
        {
            Assert.Same(manager, sender);
            Assert.Same(EventArgs.Empty, e);
            callCount++;
        };

        manager.FullScreenChanged += handler;

        manager.FullScreen = true;
        Assert.Equal(1, callCount);

        manager.FullScreen = true;
        Assert.Equal(1, callCount);

        manager.FullScreen = false;
        Assert.Equal(2, callCount);

        manager.FullScreenChanged -= handler;
        manager.FullScreen = true;
        Assert.Equal(2, callCount);
    }

    [WinFormsTheory]
    [NewAndDefaultData<EventArgs>]
    public void KioskModeManager_OnContainerControlChanged_Invoke_CallsContainerControlChanged(EventArgs eventArgs)
    {
        using SubKioskModeManager manager = new();
        int callCount = 0;
        EventHandler handler = (sender, e) =>
        {
            Assert.Same(manager, sender);
            Assert.Same(eventArgs, e);
            callCount++;
        };

        manager.ContainerControlChanged += handler;
        manager.OnContainerControlChanged(eventArgs);
        Assert.Equal(1, callCount);

        manager.ContainerControlChanged -= handler;
        manager.OnContainerControlChanged(eventArgs);
        Assert.Equal(1, callCount);
    }

    [WinFormsTheory]
    [NewAndDefaultData<EventArgs>]
    public void KioskModeManager_OnFullScreenChanged_Invoke_CallsFullScreenChanged(EventArgs eventArgs)
    {
        using SubKioskModeManager manager = new();
        int callCount = 0;
        EventHandler handler = (sender, e) =>
        {
            Assert.Same(manager, sender);
            Assert.Same(eventArgs, e);
            callCount++;
        };

        manager.FullScreenChanged += handler;
        manager.OnFullScreenChanged(eventArgs);
        Assert.Equal(1, callCount);

        manager.FullScreenChanged -= handler;
        manager.OnFullScreenChanged(eventArgs);
        Assert.Equal(1, callCount);
    }

    [WinFormsFact]
    public void KioskModeManager_Site_Set_AssignsRootComponentAsContainerControl()
    {
        using Form form = new();
        Mock<IDesignerHost> host = new();
        host.Setup(h => h.RootComponent).Returns(form);

        Mock<ISite> site = new();
        site.Setup(s => s.GetService(typeof(IDesignerHost))).Returns(host.Object);

        using KioskModeManager manager = new();
        manager.Site = site.Object;

        Assert.Same(form, manager.ContainerControl);
    }

    [WinFormsFact]
    public void KioskModeManager_Site_Set_DoesNotOverwriteExplicitContainerControl()
    {
        using Form explicitForm = new();
        using Form rootForm = new();
        Mock<IDesignerHost> host = new();
        host.Setup(h => h.RootComponent).Returns(rootForm);

        Mock<ISite> site = new();
        site.Setup(s => s.GetService(typeof(IDesignerHost))).Returns(host.Object);

        using KioskModeManager manager = new()
        {
            ContainerControl = explicitForm
        };

        manager.Site = site.Object;

        Assert.Same(explicitForm, manager.ContainerControl);
    }

    [WinFormsFact]
    public void KioskModeManager_Site_SetWithExplicitNull_DoesNotAssignRootComponent()
    {
        using Form form = new();
        Mock<IDesignerHost> host = new();
        host.Setup(h => h.RootComponent).Returns(form);

        Mock<ISite> site = new();
        site.Setup(s => s.GetService(typeof(IDesignerHost))).Returns(host.Object);

        using KioskModeManager manager = new()
        {
            ContainerControl = null
        };

        manager.Site = site.Object;

        Assert.Null(manager.ContainerControl);
    }

    [WinFormsFact]
    public void KioskModeManager_Site_SetWithoutExplicitContainerControl_UpdatesResolvedRootComponent()
    {
        using Form firstForm = new();
        using Form secondForm = new();

        Mock<IDesignerHost> firstHost = new();
        firstHost.Setup(h => h.RootComponent).Returns(firstForm);
        Mock<IDesignerHost> secondHost = new();
        secondHost.Setup(h => h.RootComponent).Returns(secondForm);

        Mock<ISite> firstSite = new();
        firstSite.Setup(s => s.GetService(typeof(IDesignerHost))).Returns(firstHost.Object);
        Mock<ISite> secondSite = new();
        secondSite.Setup(s => s.GetService(typeof(IDesignerHost))).Returns(secondHost.Object);

        using KioskModeManager manager = new();
        manager.Site = firstSite.Object;
        Assert.Same(firstForm, manager.ContainerControl);

        manager.Site = secondSite.Object;
        Assert.Same(secondForm, manager.ContainerControl);
    }

    [WinFormsFact]
    public void KioskModeManager_BeginInit_FullScreenSet_DoesNotEnterUntilEndInit()
    {
        using Form form = new()
        {
            Bounds = new Rectangle(10, 20, 300, 200),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            WindowState = FormWindowState.Normal
        };

        using KioskModeManager manager = new();
        ISupportInitialize supportInitialize = manager;
        Rectangle screenBounds = Screen.FromRectangle(form.Bounds).Bounds;

        supportInitialize.BeginInit();
        manager.ContainerControl = form;
        manager.FullScreen = true;

        Assert.Equal(FormBorderStyle.FixedDialog, form.FormBorderStyle);
        Assert.Equal(FormWindowState.Normal, form.WindowState);

        supportInitialize.EndInit();

        Assert.True(manager.FullScreen);
        Assert.Equal(FormBorderStyle.None, form.FormBorderStyle);
        Assert.Equal(FormWindowState.Normal, form.WindowState);
        Assert.Equal(screenBounds, form.Bounds);
    }

    [WinFormsFact]
    public void KioskModeManager_DisposeWhileFullScreen_RestoresExpected()
    {
        using Form form = new()
        {
            Bounds = new Rectangle(10, 20, 300, 200),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            WindowState = FormWindowState.Normal,
            TopMost = false
        };

        KioskModeManager manager = new()
        {
            ContainerControl = form,
            TopMostInFullScreen = true,
            FullScreen = true
        };
        manager.Dispose();

        Assert.Equal(FormBorderStyle.FixedDialog, form.FormBorderStyle);
        Assert.Equal(FormWindowState.Normal, form.WindowState);
        Assert.Equal(new Rectangle(10, 20, 300, 200), form.Bounds);
        Assert.False(form.TopMost);
    }

    [WinFormsFact]
    public void KioskModeManager_ProcessMessage_KeyDownThenKeyUp_TogglesOnce()
    {
        using Form form = new();
        using KioskModeManager manager = new()
        {
            ContainerControl = form
        };

        Message keyDownMessage = Message.Create(form.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.F11, 0);
        Message keyUpMessage = Message.Create(form.Handle, (int)PInvokeCore.WM_KEYUP, (nint)Keys.F11, 0);

        manager.TestAccessor.Dynamic.ProcessMessage(keyDownMessage);
        Assert.True(manager.FullScreen);

        manager.TestAccessor.Dynamic.ProcessMessage(keyUpMessage);
        Assert.True(manager.FullScreen);
    }

    [WinFormsFact]
    public void KioskModeManager_ProcessMessage_RepeatedKeyDown_DoesNotToggleAgain()
    {
        using Form form = new();
        using KioskModeManager manager = new()
        {
            ContainerControl = form
        };

        Message keyDownMessage = Message.Create(form.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.F11, 0);
        Message repeatedKeyDownMessage = Message.Create(
            form.Handle,
            (int)PInvokeCore.WM_KEYDOWN,
            (nint)Keys.F11,
            1 << 30);

        manager.TestAccessor.Dynamic.ProcessMessage(keyDownMessage);
        manager.TestAccessor.Dynamic.ProcessMessage(repeatedKeyDownMessage);

        Assert.True(manager.FullScreen);
    }

    [WinFormsFact]
    public void KioskModeManager_ProcessMessage_EscapeHandledByInputControl_DoesNotExitFullScreen()
    {
        using SubForm form = new();
        using EscapeInputControl control = new();
        form.Controls.Add(control);
        form.Show();
        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            FullScreen = true
        };
        int escapeHandled = 0;
        control.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                escapeHandled++;
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        };

        Message filterMessage = Message.Create(control.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 0);
        manager.TestAccessor.Dynamic.ProcessMessage(filterMessage);

        Assert.True(manager.FullScreen);
        Message message = Message.Create(control.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 0);
        Assert.Equal(
            PreProcessControlState.MessageNeeded,
            Control.PreProcessControlMessageInternal(control, ref message));
        control.DispatchMessage(ref message);

        Assert.Equal(1, escapeHandled);
        message = Message.Create(control.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 1 << 30);
        manager.TestAccessor.Dynamic.ProcessMessage(message);
        Assert.Equal(PreProcessControlState.MessageNeeded, Control.PreProcessControlMessageInternal(control, ref message));
        control.DispatchMessage(ref message);
        Assert.Equal(2, escapeHandled);
        Assert.True(manager.FullScreen);
    }

    [WinFormsFact]
    public void KioskModeManager_ProcessDialogKey_EscapeToggleKeyTogglesAfterKeyRouting()
    {
        using SubForm form = new();
        TestButtonControl cancelButton = new();
        form.CancelButton = cancelButton;
        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            ToggleFullScreenKeys = Keys.Escape
        };

        Message message = Message.Create(form.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 0);
        manager.TestAccessor.Dynamic.ProcessMessage(message);
        Assert.False(manager.FullScreen);

        Assert.NotEqual(PreProcessControlState.MessageProcessed, Control.PreProcessControlMessageInternal(form, ref message));
        PInvokeCore.SendMessage(form, PInvokeCore.WM_KEYDOWN, (WPARAM)(uint)Keys.Escape, (LPARAM)0);
        Assert.True(manager.FullScreen);

        message = Message.Create(form.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 1 << 30);
        manager.TestAccessor.Dynamic.ProcessMessage(message);
        Assert.NotEqual(PreProcessControlState.MessageProcessed, Control.PreProcessControlMessageInternal(form, ref message));
        PInvokeCore.SendMessage(form, PInvokeCore.WM_KEYDOWN, (WPARAM)(uint)Keys.Escape, (LPARAM)(1 << 30));
        Assert.True(manager.FullScreen);
        Assert.Equal(0, cancelButton.ClickCount);

        message = Message.Create(form.Handle, (int)PInvokeCore.WM_KEYUP, (nint)Keys.Escape, 0);
        manager.TestAccessor.Dynamic.ProcessMessage(message);
        message = Message.Create(form.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 0);
        manager.TestAccessor.Dynamic.ProcessMessage(message);

        Assert.NotEqual(PreProcessControlState.MessageProcessed, Control.PreProcessControlMessageInternal(form, ref message));
        PInvokeCore.SendMessage(form, PInvokeCore.WM_KEYDOWN, (WPARAM)(uint)Keys.Escape, (LPARAM)0);
        Assert.False(manager.FullScreen);
    }

    [WinFormsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void KioskModeManager_ProcessMessage_EscapeDuringDataGridViewEdit_CancelsEditWithoutExitingFullScreen(
        bool withCancelButton)
    {
        using SubForm form = new();
        using DataGridView grid = new();
        form.Controls.Add(grid);
        form.CreateControl();
        form.Show();
        grid.Columns.Add("Name", "Name");
        grid.Rows.Add("Alice");
        grid.CurrentCell = grid[0, 0];
        Assert.True(grid.BeginEdit(true));
        Assert.IsType<DataGridViewTextBoxEditingControl>(grid.EditingControl);
        TestButtonControl cancelButton = new();
        if (withCancelButton)
        {
            form.CancelButton = cancelButton;
        }

        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            FullScreen = true
        };
        grid.EditingControl.Text = "Changed";
        Message message = Message.Create(grid.EditingControl.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 1);
        manager.TestAccessor.Dynamic.ProcessMessage(message);

        PreProcessControlState state = Control.PreProcessControlMessageInternal(grid.EditingControl, ref message);
        if (state != PreProcessControlState.MessageProcessed)
        {
            PInvokeCore.SendMessage(grid.EditingControl, PInvokeCore.WM_KEYDOWN, (WPARAM)(uint)Keys.Escape, (LPARAM)1);
        }

        Assert.False(grid.IsCurrentCellInEditMode);
        Assert.Equal("Alice", grid[0, 0].Value);
        Assert.True(manager.FullScreen);
        Assert.Equal(0, cancelButton.ClickCount);
    }

    [WinFormsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void KioskModeManager_ProcessMessage_DataGridViewEscapeDisabled_PreservesDialogRouting(bool withCancelButton)
    {
        using SubForm form = new();
        using DataGridView grid = new();
        form.Controls.Add(grid);
        form.Show();
        grid.Columns.Add("Name", "Name");
        grid.Rows.Add("Alice");
        grid.CurrentCell = grid[0, 0];
        Assert.True(grid.BeginEdit(true));
        TestButtonControl cancelButton = new();
        if (withCancelButton)
        {
            form.CancelButton = cancelButton;
        }

        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            EscapeExitsFullScreen = false,
            FullScreen = true
        };
        Control editor = grid.EditingControl;
        editor.Text = "Changed";
        try
        {
            Message message = Message.Create(editor.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 1);
            manager.TestAccessor.Dynamic.ProcessMessage(message);
            if (Control.PreProcessControlMessageInternal(editor, ref message) != PreProcessControlState.MessageProcessed)
            {
                PInvokeCore.SendMessage(editor, PInvokeCore.WM_KEYDOWN, (WPARAM)(uint)Keys.Escape, (LPARAM)1);
            }

            Assert.True(manager.FullScreen);
            Assert.Equal(withCancelButton ? 1 : 0, cancelButton.ClickCount);
            Assert.Equal(withCancelButton, grid.IsCurrentCellInEditMode);
        }
        finally
        {
            grid.CancelEdit();
            grid.EndEdit();
        }
    }

    [WinFormsFact]
    public void KioskModeManager_ProcessMessage_EscapeClosesComboBoxDropDownWithoutExitingFullScreen()
    {
        using SubForm form = new();
        using EscapeComboBox comboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        comboBox.Items.Add("One");
        comboBox.SelectedIndex = 0;
        form.Controls.Add(comboBox);
        form.Show();
        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            FullScreen = true
        };
        comboBox.Focus();
        comboBox.DroppedDown = true;
        Message message = Message.Create(comboBox.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 1);
        manager.TestAccessor.Dynamic.ProcessMessage(message);

        PreProcessControlState state = Control.PreProcessControlMessageInternal(comboBox, ref message);
        comboBox.DispatchMessage(ref message);

        Assert.Equal(PreProcessControlState.MessageNeeded, state);
        Assert.False(comboBox.DroppedDown);
        Assert.True(manager.FullScreen);
    }

    [WinFormsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void KioskModeManager_ProcessMessage_RepeatedEscapeAfterComboBoxCloses_DoesNotExitFullScreen(bool toggleEscape)
    {
        using SubForm form = new();
        using EscapeComboBox comboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        comboBox.Items.Add("One");
        comboBox.SelectedIndex = 0;
        form.Controls.Add(comboBox);
        form.Show();
        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            FullScreen = true,
            ToggleFullScreenKeys = toggleEscape ? Keys.Escape : Keys.F11
        };
        comboBox.Focus();
        comboBox.DroppedDown = true;

        Message message = Message.Create(comboBox.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 1);
        manager.TestAccessor.Dynamic.ProcessMessage(message);
        Assert.Equal(PreProcessControlState.MessageNeeded, Control.PreProcessControlMessageInternal(comboBox, ref message));
        comboBox.DispatchMessage(ref message);
        Assert.False(comboBox.DroppedDown);

        message = Message.Create(comboBox.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 1 << 30);
        manager.TestAccessor.Dynamic.ProcessMessage(message);
        if (Control.PreProcessControlMessageInternal(comboBox, ref message) != PreProcessControlState.MessageProcessed)
        {
            comboBox.DispatchMessage(ref message);
        }

        Assert.True(manager.FullScreen);

        message = Message.Create(comboBox.Handle, (int)PInvokeCore.WM_KEYUP, (nint)Keys.Escape, 0);
        manager.TestAccessor.Dynamic.ProcessMessage(message);
        message = Message.Create(comboBox.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 1);
        manager.TestAccessor.Dynamic.ProcessMessage(message);
        PreProcessControlState state = Control.PreProcessControlMessageInternal(comboBox, ref message);
        if (state != PreProcessControlState.MessageProcessed)
        {
            comboBox.DispatchMessage(ref message);
        }

        Assert.False(manager.TestAccessor.Dynamic._escapeKeyDeferred);
        Assert.False(manager.FullScreen);
    }

    [WinFormsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void KioskModeManager_ProcessMessage_DataGridViewOverrideHandlesEscape(bool withCancelButton)
    {
        using SubForm form = new();
        using EscapeDataGridView grid = new();
        form.Controls.Add(grid);
        form.Show();
        grid.Columns.Add("Name", "Name");
        grid.Rows.Add("Alice");
        grid.CurrentCell = grid[0, 0];
        Assert.True(grid.BeginEdit(true));
        if (withCancelButton)
        {
            form.CancelButton = new TestButtonControl();
        }

        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            FullScreen = true,
            ToggleFullScreenKeys = Keys.Escape
        };
        Control editor = grid.EditingControl;
        editor.Text = "Changed";
        try
        {
            Message message = Message.Create(editor.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 1);
            manager.TestAccessor.Dynamic.ProcessMessage(message);
            if (Control.PreProcessControlMessageInternal(editor, ref message) != PreProcessControlState.MessageProcessed)
            {
                PInvokeCore.SendMessage(editor, PInvokeCore.WM_KEYDOWN, (WPARAM)(uint)Keys.Escape, (LPARAM)1);
            }

            Assert.True(grid.EscapeHandled);
            Assert.True(grid.IsCurrentCellInEditMode);
            Assert.Equal("Changed", editor.Text);
            Assert.True(manager.FullScreen);
        }
        finally
        {
            grid.CancelEdit();
            grid.EndEdit();
        }
    }

    [WinFormsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void KioskModeManager_ProcessMessage_FormKeyPreviewHandlesEscape(bool handleEscape)
    {
        using SubForm form = new() { KeyPreview = true };
        using TextBox textBox = new();
        form.Controls.Add(textBox);
        form.Show();
        using KioskModeManager manager = new() { ContainerControl = form, FullScreen = true };
        int received = 0;
        form.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                received++;
                e.Handled = handleEscape;
                e.SuppressKeyPress = handleEscape;
            }
        };

        Message message = Message.Create(textBox.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 1);
        manager.TestAccessor.Dynamic.ProcessMessage(message);
        if (Control.PreProcessControlMessageInternal(textBox, ref message) != PreProcessControlState.MessageProcessed)
        {
            PInvokeCore.SendMessage(textBox, PInvokeCore.WM_KEYDOWN, (WPARAM)(uint)Keys.Escape, (LPARAM)1);
        }

        Assert.Equal(1, received);
        Assert.Equal(handleEscape, manager.FullScreen);
        if (handleEscape)
        {
            message = Message.Create(textBox.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 1 << 30);
            manager.TestAccessor.Dynamic.ProcessMessage(message);
            if (Control.PreProcessControlMessageInternal(textBox, ref message) != PreProcessControlState.MessageProcessed)
            {
                PInvokeCore.SendMessage(textBox, PInvokeCore.WM_KEYDOWN, (WPARAM)(uint)Keys.Escape, (LPARAM)(1 << 30));
            }

            Assert.Equal(2, received);
            Assert.True(manager.FullScreen);
        }
    }

    [WinFormsFact]
    public void KioskModeManager_ProcessMessage_OverrideHandlesEscapeAfterBase()
    {
        using SubForm form = new() { KeyPreview = true };
        using EscapeTextBox textBox = new();
        form.Controls.Add(textBox);
        form.Show();
        using KioskModeManager manager = new() { ContainerControl = form, FullScreen = true };
        Message message = Message.Create(textBox.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 1);
        manager.TestAccessor.Dynamic.ProcessMessage(message);
        if (Control.PreProcessControlMessageInternal(textBox, ref message) != PreProcessControlState.MessageProcessed)
        {
            PInvokeCore.SendMessage(textBox, PInvokeCore.WM_KEYDOWN, (WPARAM)(uint)Keys.Escape, (LPARAM)1);
        }

        Assert.True(textBox.EscapeHandled);
        Assert.True(manager.FullScreen);
    }

    [WinFormsFact]
    public void KioskModeManager_ProcessMessage_ProcessDialogKeyOverrideHandlesEscapeAfterBase()
    {
        using EscapeDialogKeyForm form = new();
        form.Show();
        using KioskModeManager manager = new() { ContainerControl = form, FullScreen = true };

        Message message = Message.Create(form.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 1);
        manager.TestAccessor.Dynamic.ProcessMessage(message);
        PreProcessControlState state = Control.PreProcessControlMessageInternal(form, ref message);

        Assert.Equal(PreProcessControlState.MessageProcessed, state);
        Assert.True(form.EscapeHandled);
        Assert.True(manager.FullScreen);
    }

    [WinFormsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public unsafe void KioskModeManager_ProcessMessage_DeferredEscape_PreservesUnrelatedCharacterMessage(bool comboBox)
    {
        using SubForm form = new() { KeyPreview = true };
        using Control control = comboBox ? new ComboBox() : new TextBox();
        using TextBox secondControl = new();
        form.Controls.Add(control);
        form.Controls.Add(secondControl);
        form.Show();
        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            FullScreen = true,
            ToggleFullScreenKeys = Keys.Escape
        };
        nint target = comboBox ? ((ComboBox)control).TestAccessor.Dynamic._childEdit.Handle : control.Handle;
        nint secondTarget = secondControl.Handle;
        Message message = Message.Create(target, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 1);
        manager.TestAccessor.Dynamic.ProcessMessage(message);
        Assert.NotEqual(PreProcessControlState.MessageProcessed, Control.PreProcessControlMessageInternal(control, ref message));
        MSG nativeMessage = message.ToMSG();
        Assert.True(PInvoke.TranslateMessage(nativeMessage));
        Assert.True(PInvokeCore.PostMessage((HWND)target, PInvokeCore.WM_CHAR, (WPARAM)'x', default));
        PInvokeCore.SendMessage((HWND)target, PInvokeCore.WM_KEYDOWN, (WPARAM)(uint)Keys.Escape, (LPARAM)1);
        message = Message.Create(secondTarget, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 1);
        manager.TestAccessor.Dynamic.ProcessMessage(message);
        Assert.NotEqual(
            PreProcessControlState.MessageProcessed,
            Control.PreProcessControlMessageInternal(secondControl, ref message));
        nativeMessage = message.ToMSG();
        Assert.True(PInvoke.TranslateMessage(nativeMessage));
        PInvokeCore.SendMessage((HWND)secondTarget, PInvokeCore.WM_KEYDOWN, (WPARAM)(uint)Keys.Escape, (LPARAM)1);
        MSG pending = default;
        bool hasUnrelatedCharacter = false;
        bool firstEscapeCharacterSuppressed = false;
        bool secondEscapeCharacterSuppressed = false;
        while (PInvokeCore.PeekMessage(
            &pending, default, PInvokeCore.WM_CHAR, PInvokeCore.WM_CHAR, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE))
        {
            if (pending.hwnd == (HWND)target && pending.wParam == (WPARAM)(uint)Keys.Escape)
            {
                Message characterMessage = Message.Create(
                    (nint)pending.hwnd,
                    (int)PInvokeCore.WM_CHAR,
                    (nint)pending.wParam,
                    (nint)pending.lParam);
                firstEscapeCharacterSuppressed = manager.TestAccessor.Dynamic.ProcessMessage(characterMessage);
            }
            else if (pending.hwnd == (HWND)secondTarget && pending.wParam == (WPARAM)(uint)Keys.Escape)
            {
                Message characterMessage = Message.Create(
                    (nint)pending.hwnd,
                    (int)PInvokeCore.WM_CHAR,
                    (nint)pending.wParam,
                    (nint)pending.lParam);
                secondEscapeCharacterSuppressed = manager.TestAccessor.Dynamic.ProcessMessage(characterMessage);
            }
            else
            {
                Message characterMessage = Message.Create(
                    (nint)pending.hwnd,
                    (int)PInvokeCore.WM_CHAR,
                    (nint)pending.wParam,
                    (nint)pending.lParam);
                hasUnrelatedCharacter |= pending.hwnd == (HWND)target
                    && pending.wParam == (WPARAM)'x'
                    && !manager.TestAccessor.Dynamic.ProcessMessage(characterMessage);
            }
        }

        Assert.True(manager.FullScreen);
        Assert.True(hasUnrelatedCharacter);
        Assert.True(firstEscapeCharacterSuppressed, "The first Escape character should be suppressed.");
        Assert.True(secondEscapeCharacterSuppressed, "The second Escape character should be suppressed.");
    }

    [WinFormsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void KioskModeManager_ProcessMessage_ComboBoxEditKeyPreview(bool handleEscape)
    {
        using SubForm form = new() { KeyPreview = true };
        using ComboBox comboBox = new() { DropDownStyle = ComboBoxStyle.DropDown };
        form.Controls.Add(comboBox);
        form.Show();
        using KioskModeManager manager = new() { ContainerControl = form, FullScreen = true };
        bool received = false;
        form.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                received = true;
                e.Handled = handleEscape;
            }
        };

        nint editorHandle = comboBox.TestAccessor.Dynamic._childEdit.Handle;
        Message message = Message.Create(editorHandle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 1);
        manager.TestAccessor.Dynamic.ProcessMessage(message);
        if (Control.PreProcessControlMessageInternal(comboBox, ref message) != PreProcessControlState.MessageProcessed)
        {
            PInvokeCore.SendMessage((HWND)editorHandle, PInvokeCore.WM_KEYDOWN, (WPARAM)(uint)Keys.Escape, (LPARAM)1);
        }

        Assert.True(received);
        Assert.Equal(handleEscape, manager.FullScreen);
    }

    [WinFormsFact]
    public void KioskModeManager_ProcessMessage_UnhandledEscape_ExitsWithoutClickingCancelButton()
    {
        using SubForm form = new();
        using TextBox textBox = new();
        form.Controls.Add(textBox);
        TestButtonControl cancelButton = new();
        form.CancelButton = cancelButton;
        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            FullScreen = true
        };
        Message message = Message.Create(textBox.Handle, (int)PInvokeCore.WM_KEYDOWN, (nint)Keys.Escape, 1);
        manager.TestAccessor.Dynamic.ProcessMessage(message);

        PreProcessControlState state = Control.PreProcessControlMessageInternal(textBox, ref message);

        Assert.Equal(PreProcessControlState.MessageProcessed, state);
        Assert.False(manager.FullScreen);
        Assert.Equal(0, cancelButton.ClickCount);
    }

    [WinFormsFact]
    public void KioskModeManager_ProcessDialogKey_EscapeExitsBeforeClickingCancelButton()
    {
        using SubForm form = new();
        TestButtonControl cancelButton = new();
        form.CancelButton = cancelButton;
        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            FullScreen = true
        };

        bool processed = form.ProcessDialogKey(Keys.Escape);

        Assert.True(processed);
        Assert.False(manager.FullScreen);
        Assert.Equal(0, cancelButton.ClickCount);
    }

    [WinFormsFact]
    public void KioskModeManager_ProcessDialogKey_EscapeExitsDisabled_ClicksCancelButton()
    {
        using SubForm form = new();
        TestButtonControl cancelButton = new();
        form.CancelButton = cancelButton;
        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            EscapeExitsFullScreen = false,
            FullScreen = true
        };

        bool processed = form.ProcessDialogKey(Keys.Escape);

        Assert.True(processed);
        Assert.True(manager.FullScreen);
        Assert.Equal(1, cancelButton.ClickCount);
    }

    [WinFormsFact]
    public void KioskModeManager_ContainerControl_Reparented_UnregistersFromPreviousForm()
    {
        using SubForm firstForm = new();
        using SubForm secondForm = new();
        using UserControl container = new();
        firstForm.Controls.Add(container);
        using KioskModeManager manager = new()
        {
            ContainerControl = container,
            ToggleFullScreenKeys = Keys.Escape
        };

        secondForm.Controls.Add(container);

        Assert.False(firstForm.ProcessDialogKey(Keys.Escape));
        Assert.True(secondForm.ProcessDialogKey(Keys.Escape));
        Assert.True(manager.FullScreen);
    }

    [WinFormsFact]
    public void KioskModeManager_Dispose_UnregistersFromForm()
    {
        using SubForm form = new();
        KioskModeManager manager = new()
        {
            ContainerControl = form,
            ToggleFullScreenKeys = Keys.Escape
        };
        manager.Dispose();

        Assert.False(form.ProcessDialogKey(Keys.Escape));
        Assert.False(manager.FullScreen);
    }

    [WinFormsFact]
    public void KioskModeManager_ProcessMessage_MouseMove_ShowsHiddenMousePointer()
    {
        using Form form = new();
        using KioskModeManager manager = new()
        {
            ContainerControl = form,
            MousePointerAutoHideDelay = 1,
            FullScreen = true
        };

        manager.TestAccessor.Dynamic.OnMousePointerAutoHideTimerTick(null, EventArgs.Empty);
        Assert.True(manager.TestAccessor.Dynamic._isCursorHidden);

        Message message = Message.Create(form.Handle, (int)PInvokeCore.WM_MOUSEMOVE, 0, 0);
        manager.TestAccessor.Dynamic.ProcessMessage(message);
        Timer timer = manager.TestAccessor.Dynamic._mousePointerAutoHideTimer;

        Assert.False(manager.TestAccessor.Dynamic._isCursorHidden);
        Assert.True(timer.Enabled);
        Assert.Equal(1, timer.Interval);
    }

    private class SubKioskModeManager : KioskModeManager
    {
        public new bool DesignMode => base.DesignMode;

        public new void OnContainerControlChanged(EventArgs e)
            => base.OnContainerControlChanged(e);

        public new void OnFullScreenChanged(EventArgs e)
            => base.OnFullScreenChanged(e);
    }

    private sealed class SubForm : Form
    {
        public new bool ProcessDialogKey(Keys keyData)
            => base.ProcessDialogKey(keyData);
    }

    private sealed class EscapeInputControl : Control
    {
        protected override bool IsInputKey(Keys keyData)
            => (keyData & Keys.KeyCode) == Keys.Escape || base.IsInputKey(keyData);

        public void DispatchMessage(ref Message message)
            => base.WndProc(ref message);
    }

    private sealed class EscapeComboBox : ComboBox
    {
        public void DispatchMessage(ref Message message)
            => base.WndProc(ref message);
    }

    private sealed class EscapeTextBox : TextBox
    {
        public bool EscapeHandled { get; private set; }

        protected internal override bool ProcessKeyMessage(ref Message message)
        {
            bool handled = base.ProcessKeyMessage(ref message);
            if (message.Msg == (int)PInvokeCore.WM_KEYDOWN && (Keys)message.WParam == Keys.Escape)
            {
                EscapeHandled = true;
                return true;
            }

            return handled;
        }
    }

    private sealed class EscapeDialogKeyForm : Form
    {
        public bool EscapeHandled { get; private set; }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            bool handled = base.ProcessDialogKey(keyData);
            if (!handled && (keyData & Keys.KeyCode) == Keys.Escape)
            {
                EscapeHandled = true;
                return true;
            }

            return handled;
        }
    }

    private sealed class EscapeDataGridView : DataGridView
    {
        public bool EscapeHandled { get; private set; }

        protected override bool ProcessDataGridViewKey(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                EscapeHandled = true;
                return true;
            }

            return base.ProcessDataGridViewKey(e);
        }
    }

    private sealed class TestButtonControl : IButtonControl
    {
        public DialogResult DialogResult { get; set; }

        public int ClickCount { get; private set; }

        public void NotifyDefault(bool value)
        {
        }

        public void PerformClick()
            => ClickCount++;
    }
}
#endif
