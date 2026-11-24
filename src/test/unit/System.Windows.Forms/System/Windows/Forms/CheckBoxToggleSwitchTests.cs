// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Drawing;

namespace System.Windows.Forms.Tests;

// Behavioral tests for the modern toggle-switch CheckBox (Appearance.ToggleSwitch + VisualStylesMode.Net11).
// These cover public behavior and the shared metrics without depending on pixel-level rendering details.
public class CheckBoxToggleSwitchTests
{
    private const int LeftMouseButtonKeyState = 0x0001;

    internal static bool HasCompleteCaption(
        ButtonBase control,
        Bitmap rendered,
        Rectangle textArea,
        out string diagnostic)
    {
        using Bitmap reference = new(450, 100);
        using (Graphics graphics = Graphics.FromImage(reference))
        {
            graphics.Clear(Color.White);
            TextRenderer.DrawText(
                graphics,
                control.Text,
                control.Font,
                new Rectangle(Point.Empty, reference.Size),
                Color.Black,
                Rendering.CheckBox.AnimatedToggleSwitchRenderer.GetTextFormatFlags(control));
        }

        static Rectangle GetBlackPixelBounds(Bitmap bitmap, Rectangle bounds)
        {
            int left = bounds.Right;
            int top = bounds.Bottom;
            int right = bounds.Left;
            int bottom = bounds.Top;
            for (int y = bounds.Top; y < bounds.Bottom; y++)
            {
                for (int x = bounds.Left; x < bounds.Right; x++)
                {
                    if (bitmap.GetPixel(x, y).ToArgb() == Color.Black.ToArgb())
                    {
                        left = Math.Min(left, x);
                        top = Math.Min(top, y);
                        right = Math.Max(right, x + 1);
                        bottom = Math.Max(bottom, y + 1);
                    }
                }
            }

            return left == bounds.Right ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right, bottom);
        }

        Rectangle actualBounds = GetBlackPixelBounds(rendered, Rectangle.Intersect(textArea, control.ClientRectangle));
        Rectangle expectedBounds = GetBlackPixelBounds(reference, new Rectangle(Point.Empty, reference.Size));
        if (actualBounds.IsEmpty || actualBounds.Size != expectedBounds.Size)
        {
            diagnostic = $"Actual glyph bounds {actualBounds}; expected {expectedBounds}.";
            return false;
        }

        // Compare solid glyph pixels; antialiased edge colors vary between rendering targets.
        for (int y = 0; y < expectedBounds.Height; y++)
        {
            for (int x = 0; x < expectedBounds.Width; x++)
            {
                bool actualPixel = rendered.GetPixel(actualBounds.X + x, actualBounds.Y + y).ToArgb()
                    == Color.Black.ToArgb();
                bool expectedPixel = reference.GetPixel(expectedBounds.X + x, expectedBounds.Y + y).ToArgb()
                    == Color.Black.ToArgb();
                if (actualPixel != expectedPixel)
                {
                    diagnostic = $"Glyph masks differ at ({x}, {y}) within {expectedBounds.Size}.";
                    return false;
                }
            }
        }

        diagnostic = $"Complete glyph bounds matched: {actualBounds.Size}.";
        return true;
    }

    [WinFormsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToggleSwitch_ImageBeforeText_WithSpace_RendersFullCaption(bool radioButton)
    {
        using SystemVisualSettingsTestScope settingsScope = new(
            clientAreaAnimationEnabled: false,
            highContrastEnabled: false);
        using Bitmap image = new(16, 11);
        using ButtonBase control = radioButton
            ? new RadioButton { Appearance = Appearance.ToggleSwitch }
            : new CheckBox { Appearance = Appearance.ToggleSwitch };
        control.VisualStylesMode = VisualStylesMode.Net11;
        control.BackColor = Color.White;
        control.ForeColor = Color.Black;
        control.Size = new Size(450, 80);
        control.Image = image;
        control.Text = "Toggle caption";
        control.TextImageRelation = TextImageRelation.ImageBeforeText;
        using Bitmap rendered = new(control.Width, control.Height);

        control.DrawToBitmap(rendered, control.ClientRectangle);

        Rendering.CheckBox.ToggleSwitchMetrics metrics = Rendering.CheckBox.ToggleSwitchMetrics.Create(control);
        Rectangle textArea = Rendering.CheckBox.AnimatedToggleSwitchRenderer.GetTextBounds(
            control,
            ContentAlignment.MiddleLeft,
            metrics);
        Assert.True(
            HasCompleteCaption(control, rendered, textArea, out string diagnostic),
            diagnostic);
    }

    public static IEnumerable<object[]> RenderingMatrixData()
    {
        foreach (FlatStyle flatStyle in Enum.GetValues<FlatStyle>())
        {
            foreach (Appearance appearance in Enum.GetValues<Appearance>())
            {
                yield return [flatStyle, appearance, VisualStylesMode.Inherit];
                yield return [flatStyle, appearance, VisualStylesMode.Classic];
                yield return [flatStyle, appearance, VisualStylesMode.Disabled];
                yield return [flatStyle, appearance, VisualStylesMode.Net11];
                yield return [flatStyle, appearance, VisualStylesMode.Latest];
            }
        }
    }

    [WinFormsTheory]
    [MemberData(nameof(RenderingMatrixData))]
    public void CheckBox_RenderingMatrix_DoesNotThrow(
        FlatStyle flatStyle,
        Appearance appearance,
        VisualStylesMode visualStylesMode)
    {
        using SystemVisualSettingsTestScope settingsScope = new(
            clientAreaAnimationEnabled: false,
            highContrastEnabled: false);
        using CheckBox checkBox = new()
        {
            Appearance = appearance,
            FlatStyle = flatStyle,
            Size = new Size(140, 36),
            Text = "Matrix",
            VisualStylesMode = visualStylesMode
        };
        checkBox.CreateControl();
        using Bitmap bitmap = new(
            checkBox.Width,
            checkBox.Height);

        _ = checkBox.DownChangeRectangle;
        _ = checkBox.OverChangeRectangle;
        checkBox.DrawToBitmap(
            bitmap,
            new Rectangle(Point.Empty, checkBox.Size));
        PInvokeCore.SendMessage(
            checkBox,
            PInvokeCore.WM_LBUTTONDOWN,
            (WPARAM)LeftMouseButtonKeyState,
            LPARAM.MAKELPARAM(5, 5));
        PInvokeCore.SendMessage(
            checkBox,
            PInvokeCore.WM_LBUTTONUP,
            0,
            LPARAM.MAKELPARAM(5, 5));
    }

    [WinFormsTheory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    public void CheckBox_ToggleSwitch_FontSize_DerivesPreferredGeometry(float fontSize)
    {
        using Font font = new(Control.DefaultFont.FontFamily, fontSize);
        using CheckBox checkBox = new()
        {
            VisualStylesMode = VisualStylesMode.Net11,
            Appearance = Appearance.ToggleSwitch,
            Font = font,
            Text = "Toggle",
            Padding = new Padding(2)
        };

        Rendering.CheckBox.ToggleSwitchMetrics metrics = Rendering.CheckBox.ToggleSwitchMetrics.Create(checkBox);
        int expectedHeight = Math.Max(
            checkBox.LogicalToDeviceUnits(13),
            (int)(font.Height * 0.9f));

        Assert.Equal(expectedHeight, metrics.SwitchHeight);
        Assert.Equal(2 * expectedHeight, metrics.SwitchWidth);
        Assert.True(metrics.HoverThumbDiameter > metrics.ThumbDiameter);
        Assert.Equal(metrics.GetPreferredSize(checkBox), checkBox.GetPreferredSize(Size.Empty));
    }

    [WinFormsFact]
    public void CheckBox_ToggleSwitch_TightHeight_ZeroTextImageInset_IncreasesTextBounds()
    {
        using Bitmap image = new(16, 11);
        using CheckBox checkBox = new()
        {
            Appearance = Appearance.ToggleSwitch,
            Image = image,
            ImageAlign = ContentAlignment.MiddleCenter,
            Size = new Size(450, Control.DefaultFont.Height),
            Text = "Toggle caption",
            TextImageRelation = TextImageRelation.ImageBeforeText,
            VisualStylesMode = VisualStylesMode.Net11
        };

        Rendering.CheckBox.ToggleSwitchMetrics metrics = Rendering.CheckBox.ToggleSwitchMetrics.Create(checkBox);
        Rectangle textArea = Rendering.CheckBox.AnimatedToggleSwitchRenderer.GetTextBounds(
            checkBox,
            checkBox.RtlTranslatedCheckAlign,
            metrics);
        ButtonInternal.ButtonBaseAdapter.LayoutOptions layoutOptions =
            ButtonInternal.ButtonBaseAdapter.CommonLayout(
                textArea,
                Padding.Empty,
                isDefault: false,
                checkBox.Font,
                checkBox.Text,
                checkBox.Enabled,
                checkBox.TextAlign,
                checkBox.RightToLeft);
        layoutOptions.BorderSize = 0;
        layoutOptions.PaddingSize = 0;
        layoutOptions.MaxFocus = false;
        layoutOptions.GrowBorderBy1PxWhenDefault = false;
        layoutOptions.ImageSize = image.Size;
        layoutOptions.ImageAlign = checkBox.ImageAlign;
        layoutOptions.TextImageRelation = checkBox.TextImageRelation;
        layoutOptions.GdiTextFormatFlags =
            Rendering.CheckBox.AnimatedToggleSwitchRenderer.GetTextFormatFlags(checkBox);
        layoutOptions.DotNetOneButtonCompat = false;
        layoutOptions.ClipImagesToClient = true;

        layoutOptions.TextImageInset = 2;
        int defaultTextHeight = layoutOptions.Layout().TextBounds.Height;
        layoutOptions.TextImageInset = 0;
        int zeroInsetTextHeight = layoutOptions.Layout().TextBounds.Height;

        Assert.True(zeroInsetTextHeight > defaultTextHeight);
    }

    [WinFormsFact]
    public void CheckBox_ToggleSwitch_LargeFont_ReservesTenPercentHoverGrowth()
    {
        using Font font = new(Control.DefaultFont.FontFamily, 72f);
        using CheckBox checkBox = new()
        {
            Appearance = Appearance.ToggleSwitch,
            Font = font,
            VisualStylesMode = VisualStylesMode.Net11
        };

        Rendering.CheckBox.ToggleSwitchMetrics metrics = Rendering.CheckBox.ToggleSwitchMetrics.Create(checkBox);
        float growth = metrics.HoverThumbDiameter / (float)metrics.ThumbDiameter;

        Assert.InRange(growth, 1.08f, 1.12f);
    }

    [WinFormsFact]
    public void CheckBox_ToggleSwitch_PreferredSize_UseMnemonicFalse_MeasuresLiteralAmpersand()
    {
        using CheckBox literalAmpersand = new()
        {
            Appearance = Appearance.ToggleSwitch,
            FlatStyle = FlatStyle.Standard,
            Text = "A&B",
            UseMnemonic = false,
            VisualStylesMode = VisualStylesMode.Net11
        };
        using CheckBox escapedMnemonic = new()
        {
            Appearance = Appearance.ToggleSwitch,
            FlatStyle = FlatStyle.Standard,
            Text = "A&&B",
            UseMnemonic = true,
            VisualStylesMode = VisualStylesMode.Net11
        };

        Assert.Equal(
            escapedMnemonic.GetPreferredSize(Size.Empty),
            literalAmpersand.GetPreferredSize(Size.Empty));
    }

    [WinFormsFact]
    public void CheckBox_Appearance_ToggleSwitch_RoundTrips()
    {
        using CheckBox checkBox = new() { Appearance = Appearance.ToggleSwitch };

        Assert.Equal(Appearance.ToggleSwitch, checkBox.Appearance);
    }

    [WinFormsTheory]
    [InlineData(VisualStylesMode.Net11)]
    [InlineData(VisualStylesMode.Latest)]
    public void CheckBox_ToggleSwitch_WithModernVisualStyles_HasPositivePreferredSize(VisualStylesMode visualStylesMode)
    {
        using CheckBox checkBox = new()
        {
            VisualStylesMode = visualStylesMode,
            Appearance = Appearance.ToggleSwitch,
            Text = "Toggle"
        };

        Size preferred = checkBox.GetPreferredSize(Size.Empty);

        Assert.True(preferred.Width > 0);
        Assert.True(preferred.Height > 0);
    }

    [WinFormsTheory]
    [InlineData(VisualStylesMode.Classic)]
    [InlineData(VisualStylesMode.Disabled)]
    public void CheckBox_ToggleSwitch_WithoutModernVisualStyles_UsesClassicPreferredSize(VisualStylesMode visualStylesMode)
    {
        using CheckBox classicCheckBox = new()
        {
            VisualStylesMode = visualStylesMode,
            Appearance = Appearance.Normal,
            Text = "Toggle"
        };

        using CheckBox toggleSwitchCheckBox = new()
        {
            VisualStylesMode = visualStylesMode,
            Appearance = Appearance.ToggleSwitch,
            Text = "Toggle"
        };

        Assert.Equal(
            classicCheckBox.GetPreferredSize(Size.Empty),
            toggleSwitchCheckBox.GetPreferredSize(Size.Empty));
    }

    [WinFormsFact]
    public void CheckBox_ToggleSwitch_CheckedRoundTrips()
    {
        using CheckBox checkBox = new()
        {
            VisualStylesMode = VisualStylesMode.Net11,
            Appearance = Appearance.ToggleSwitch
        };

        Assert.False(checkBox.Checked);

        checkBox.Checked = true;
        Assert.True(checkBox.Checked);
        Assert.Equal(CheckState.Checked, checkBox.CheckState);

        checkBox.Checked = false;
        Assert.False(checkBox.Checked);
        Assert.Equal(CheckState.Unchecked, checkBox.CheckState);
    }

    [WinFormsFact]
    public void CheckBox_ToggleSwitch_CheckStateChanged_Raised()
    {
        using CheckBox checkBox = new()
        {
            VisualStylesMode = VisualStylesMode.Net11,
            Appearance = Appearance.ToggleSwitch
        };

        int callCount = 0;
        checkBox.CheckStateChanged += (s, e) => callCount++;

        checkBox.CheckState = CheckState.Checked;
        Assert.Equal(1, callCount);

        // Setting the same value does not raise the event.
        checkBox.CheckState = CheckState.Checked;
        Assert.Equal(1, callCount);
    }

    [WinFormsFact]
    public void CheckBox_ToggleSwitch_ThreeState_PreservesClassicCheckStateBehavior()
    {
        using CheckBox checkBox = new()
        {
            VisualStylesMode = VisualStylesMode.Net11,
            Appearance = Appearance.ToggleSwitch,
            ThreeState = true
        };

        checkBox.CheckState = CheckState.Indeterminate;

        Assert.Equal(CheckState.Indeterminate, checkBox.CheckState);
        Assert.True(checkBox.GetPreferredSize(Size.Empty).Width > 0);
    }

    [WinFormsFact]
    public void CheckBox_VisualStylesMode_DefaultIsAmbient()
    {
        using CheckBox checkBox = new();

        Assert.Equal(VisualStylesMode.Inherit, checkBox.VisualStylesMode);
    }

    [WinFormsTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void CheckBox_ToggleSwitch_TransparentBackColor_DoesNotThrow(bool useBackgroundImage)
    {
        using SystemVisualSettingsTestScope settingsScope = new(
            clientAreaAnimationEnabled: false,
            highContrastEnabled: false);
        using Bitmap backgroundImage = new(10, 10);
        using Panel parent = new() { BackColor = Color.LightBlue, Size = new Size(200, 100) };
        using CheckBox checkBox = new()
        {
            Parent = parent,
            Appearance = Appearance.ToggleSwitch,
            VisualStylesMode = VisualStylesMode.Net11,
            BackColor = Color.Transparent,
            BackgroundImage = useBackgroundImage ? backgroundImage : null,
            Size = new Size(140, 36),
            Text = "Matrix"
        };
        parent.CreateControl();
        using Bitmap bitmap = new(checkBox.Width, checkBox.Height);

        checkBox.DrawToBitmap(bitmap, new Rectangle(Point.Empty, checkBox.Size));
    }
}
