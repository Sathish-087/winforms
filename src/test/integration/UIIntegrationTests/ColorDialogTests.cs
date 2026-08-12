// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;

namespace System.Windows.Forms.UITests;

public class ColorDialogTests : ControlTestBase
{
    public ColorDialogTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [WinFormsFact]
    public void ColorDialog_ShowDialog_Cancel()
    {
        using DialogHostForm dialogOwnerForm = new();
        using ColorDialog dialog = new()
        {
            Color = Color.Red,
        };

        Assert.Equal(DialogResult.Cancel, dialog.ShowDialog(dialogOwnerForm));
        Assert.Equal(Color.Red, dialog.Color);
    }

    [WinFormsFact]
    public void ColorDialog_ShowDialog_OK()
    {
        using AcceptDialogForm dialogOwnerForm = new();
        using ColorDialog dialog = new()
        {
            Color = Color.Blue,
        };

        Assert.Equal(DialogResult.OK, dialog.ShowDialog(dialogOwnerForm));
        Assert.Equal(Color.Blue, dialog.Color);
    }

    private class AcceptDialogForm : DialogHostForm
    {
        protected override void OnDialogIdle(HWND dialogHandle)
        {
            Accept(dialogHandle);
        }
    }
}
