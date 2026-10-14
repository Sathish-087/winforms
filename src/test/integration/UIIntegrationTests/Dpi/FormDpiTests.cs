// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Windows.Win32.UI.HiDpi;

namespace System.Windows.Forms.UITests.Dpi;

public class FormDpiTests : ControlTestBase
{
    public FormDpiTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [WinFormsTheory]
    [InlineData(3.5 * ScaleHelper.OneHundredPercentLogicalDpi)]
    public void Form_DpiChanged_Bounds(int newDpi)
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
            form.AutoScaleMode = AutoScaleMode.Dpi;
            form.Show();

            Drawing.Rectangle initialBounds = form.Bounds;
            float initialFontSize = form.Font.Size;
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, newDpi);

            // Lab machines giving strange values that I could not explain. for ex: on local machine,
            // I get 1050*1050 for factor 3.5. This is not same on lab machines ( ex, we get 1044). For now,
            // just verifying they are scaled.
            Assert.NotEqual(initialBounds.Width, form.Bounds.Width);
            Assert.NotEqual(initialBounds.Height, form.Bounds.Height);
            Assert.NotEqual(initialFontSize, form.Font.Size);
            form.Close();
        }
        finally
        {
            // Reset back to original awareness context.
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }

    [WinFormsTheory]
    [InlineData(3.5 * ScaleHelper.OneHundredPercentLogicalDpi)]
    public void Form_DpiChanged_MinMaxSizeNotChanged_Default(int newDpi)
    {
        // Run tests only on Windows 10 versions that support thread dpi awareness.
        if (!PlatformDetection.IsWindows10Version1803OrGreater)
        {
            return;
        }

        DPI_AWARENESS_CONTEXT originalAwarenessContext = PInvoke.SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        typeof(ScaleHelper).TestAccessor.Dynamic.InitializeStatics();
        try
        {
            var minSize = new Drawing.Size(100, 100);
            var maxSize = new Drawing.Size(500, 500);
            using Form form = new();
            form.MinimumSize = minSize;
            form.MaximumSize = maxSize;
            form.AutoScaleMode = AutoScaleMode.Dpi;
            form.Show();
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, newDpi);
            var factor = (float)newDpi / ScaleHelper.OneHundredPercentLogicalDpi;
            Assert.Equal(minSize * factor, form.MinimumSize);
            Assert.Equal(maxSize * factor, form.MaximumSize);
            form.Close();
        }
        finally
        {
            // Reset back to original awareness context.
            PInvoke.SetThreadDpiAwarenessContext(originalAwarenessContext);
        }
    }

    [WinFormsTheory]
    [InlineData(3.5 * ScaleHelper.OneHundredPercentLogicalDpi)]
    public void Form_DpiChanged_MinMaxSizeChanged_WithRuntimeSetting(int newDpi)
    {
        // Run tests only on Windows 10 versions that support thread dpi awareness.
        if (!PlatformDetection.IsWindows10Version1803OrGreater)
        {
            return;
        }

        DPI_AWARENESS_CONTEXT originalAwarenessContext = PInvoke.SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        typeof(ScaleHelper).TestAccessor.Dynamic.InitializeStatics();
        try
        {
            var minSize = new Drawing.Size(100, 100);
            var maxSize = new Drawing.Size(500, 500);
            using Form form = new();
            form.MinimumSize = minSize;
            form.MaximumSize = maxSize;
            form.AutoScaleMode = AutoScaleMode.Dpi;
            form.Show();

            // Explicitly opt-in to resize min and max sizes with Dpi changed event.
            using ScaleTopLevelFormMinMaxSizeForDpiScope scope = new(enable: true);
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, newDpi);
            Assert.NotEqual(form.MinimumSize, minSize);
            Assert.NotEqual(form.MaximumSize, maxSize);
            form.Close();
        }
        finally
        {
            // Reset back to original awareness context.
            PInvoke.SetThreadDpiAwarenessContext(originalAwarenessContext);
        }
    }

    [WinFormsTheory]
    [InlineData(3.5 * ScaleHelper.OneHundredPercentLogicalDpi)]
    public void Form_DpiChanged_NonLinear_DesiredSize(int newDpi)
    {
        // Run tests only on Windows 10 versions that support thread dpi awareness.
        if (!PlatformDetection.IsWindows10Version1803OrGreater)
        {
            return;
        }

        DPI_AWARENESS_CONTEXT originalAwarenessContext = PInvoke.SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        typeof(ScaleHelper).TestAccessor.Dynamic.InitializeStatics();
        try
        {
            using Form form = new();
            form.AutoScaleMode = AutoScaleMode.Font;
            form.Show();

            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, newDpi);
            Drawing.Size nonLinearSize = form.Size;

            // Rollback to 96 Dpi, and change AutoScaleMode to check with linear size
            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, ScaleHelper.OneHundredPercentLogicalDpi);
            form.AutoScaleMode = AutoScaleMode.Dpi;

            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, newDpi);
            Assert.NotEqual(form.Size, nonLinearSize);
            form.Close();
        }
        finally
        {
            // Reset back to original awareness context.
            PInvoke.SetThreadDpiAwarenessContext(originalAwarenessContext);
        }
    }

    [WinFormsTheory]
    [InlineData(3.5 * ScaleHelper.OneHundredPercentLogicalDpi)]
    public void Form_DpiChanged_FormCacheSize(int newDpi)
    {
        // Run tests only on Windows 10 versions that support thread dpi awareness.
        if (!PlatformDetection.IsWindows10Version1803OrGreater)
        {
            return;
        }

        DPI_AWARENESS_CONTEXT originalAwarenessContext = PInvoke.SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        typeof(ScaleHelper).TestAccessor.Dynamic.InitializeStatics();
        try
        {
            using Form form = new();
            form.AutoScaleMode = AutoScaleMode.Font;
            form.Show();

            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, newDpi);

            dynamic fomrTestAccessor = form.TestAccessor.Dynamic;
            Assert.NotNull(fomrTestAccessor._dpiFormSizes);
            Assert.Equal(2, fomrTestAccessor._dpiFormSizes.Count);
            form.Close();
        }
        finally
        {
            // Reset back to original awareness context.
            PInvoke.SetThreadDpiAwarenessContext(originalAwarenessContext);
        }
    }

    [WinFormsTheory]
    [InlineData(3.5 * ScaleHelper.OneHundredPercentLogicalDpi)]
    public void Form_DpiChanged_AutoScaleMode_Dpi_FormDoesNotCacheSize(int newDpi)
    {
        // Run tests only on Windows 10 versions that support thread dpi awareness.
        if (!PlatformDetection.IsWindows10Version1803OrGreater)
        {
            return;
        }

        DPI_AWARENESS_CONTEXT originalAwarenessContext = PInvoke.SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        typeof(ScaleHelper).TestAccessor.Dynamic.InitializeStatics();
        try
        {
            using Form form = new();
            form.AutoScaleMode = AutoScaleMode.Dpi;
            form.Show();

            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, newDpi);
            Assert.Null(form.TestAccessor.Dynamic._dpiFormSizes);
            form.Close();
        }
        finally
        {
            // Reset back to original awareness context.
            PInvoke.SetThreadDpiAwarenessContext(originalAwarenessContext);
        }
    }

    [WinFormsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Form_DpiChanged_ReentrantDpiChange_DoesNotDoubleScaleChildFonts(bool anchorLayoutV2)
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
            using AnchorLayoutV2Scope scope = new(enable: anchorLayoutV2);
            using ReentrantDpiForm form = new();
            form.Show();

            float initialFontSize = form.Font.Size;
            form.TriggerNestedDpiChange();

            DpiMessageHelper.TriggerDpiMessage(
                PInvokeCore.WM_DPICHANGED,
                form,
                ScaleHelper.OneHundredPercentLogicalDpi * 3 / 2);

            // The nested message supersedes the original transition. Windows subsequently delivers the
            // notification for the target monitor once the dialog is positioned.
            Assert.Equal(ScaleHelper.OneHundredPercentLogicalDpi, form.DeviceDpi);
            Assert.Equal(initialFontSize, form.Font.Size, precision: 3);

            DpiMessageHelper.TriggerDpiMessage(
                PInvokeCore.WM_DPICHANGED,
                form,
                ScaleHelper.OneHundredPercentLogicalDpi * 3 / 2);

            Assert.Equal(ScaleHelper.OneHundredPercentLogicalDpi * 3 / 2, form.DeviceDpi);
            Assert.Equal(initialFontSize * 1.5F, form.Font.Size, precision: 3);
            Assert.Equal(form.Font, form.NameTextBox.Font);
            Assert.Equal(form.Font, form.OkButton.Font);

            DpiMessageHelper.TriggerDpiMessage(PInvokeCore.WM_DPICHANGED, form, ScaleHelper.OneHundredPercentLogicalDpi);
            Assert.Equal(ScaleHelper.OneHundredPercentLogicalDpi, form.DeviceDpi);
            Assert.Equal(initialFontSize, form.Font.Size, precision: 3);
            form.Close();
        }
        finally
        {
            // Reset back to original awareness context.
            PInvoke.SetThreadDpiAwarenessContextInternal(originalAwarenessContext);
        }
    }

    [WinFormsFact]
    public void Form_SizeNotCached_SystemAwareMode()
    {
        // Run tests only on Windows 10 versions that support thread dpi awareness.
        if (!PlatformDetection.IsWindows10Version1803OrGreater)
        {
            return;
        }

        DPI_AWARENESS_CONTEXT originalAwarenessContext = PInvoke.SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_SYSTEM_AWARE);
        typeof(ScaleHelper).TestAccessor.Dynamic.InitializeStatics();
        try
        {
            using Form form = new();
            form.AutoScaleMode = AutoScaleMode.Font;
            form.Show();

            Assert.Null(form.TestAccessor.Dynamic._dpiFormSizes);
            form.Close();
        }
        finally
        {
            // Reset back to original awareness context.
            PInvoke.SetThreadDpiAwarenessContext(originalAwarenessContext);
        }
    }

    /// <summary>
    ///  Simulates the nested DPI notification that Windows can deliver while positioning a modal dialog.
    /// </summary>
    private sealed class ReentrantDpiForm : Form
    {
        private bool _triggerNestedDpiChange;

        internal ReentrantDpiForm()
        {
            AutoScaleDimensions = new Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Drawing.Size(400, 140);

            NameTextBox = new TextBox
            {
                Location = new Drawing.Point(15, 70),
                Size = new Drawing.Size(250, 23)
            };

            OkButton = new Button
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Drawing.Point(310, 100),
                Size = new Drawing.Size(75, 27),
                Text = "OK"
            };

            Controls.Add(NameTextBox);
            Controls.Add(OkButton);
        }

        internal TextBox NameTextBox { get; }

        internal Button OkButton { get; }

        internal void TriggerNestedDpiChange() => _triggerNestedDpiChange = true;

        protected override void WndProc(ref Message m)
        {
            if (_triggerNestedDpiChange && m.Msg == PInvokeCore.WM_WINDOWPOSCHANGED)
            {
                _triggerNestedDpiChange = false;
                DpiMessageHelper.TriggerDpiMessage(
                    PInvokeCore.WM_DPICHANGED,
                    this,
                    ScaleHelper.OneHundredPercentLogicalDpi);
            }

            base.WndProc(ref m);
        }
    }
}
