// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;

namespace System.Windows.Forms.UITests;

public class FontDialogTests : ControlTestBase
{
    public FontDialogTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [WinFormsFact]
    public void FontDialog_ShowDialog_Cancel()
    {
        using Font font = new(FontFamily.GenericSansSerif, 10f);
        using DialogHostForm dialogOwnerForm = new();
        using FontDialog dialog = new()
        {
            Font = font,
        };

        Assert.Equal(DialogResult.Cancel, dialog.ShowDialog(dialogOwnerForm));
    }

    [WinFormsFact]
    public void FontDialog_ShowDialog_OK()
    {
        using Font font = new(FontFamily.GenericSansSerif, 12f);
        using AcceptDialogForm dialogOwnerForm = new();
        using FontDialog dialog = new()
        {
            Font = font,
        };

        Assert.Equal(DialogResult.OK, dialog.ShowDialog(dialogOwnerForm));
        Assert.NotNull(dialog.Font);
        Assert.Equal(12f, dialog.Font.SizeInPoints);
    }

    private class AcceptDialogForm : DialogHostForm
    {
        protected override void OnDialogIdle(HWND dialogHandle)
        {
            Accept(dialogHandle);
        }
    }
}
