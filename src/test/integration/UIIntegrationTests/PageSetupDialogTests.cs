// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Printing;

namespace System.Windows.Forms.UITests;

public class PageSetupDialogTests : ControlTestBase
{
    public PageSetupDialogTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [WinFormsFact]
    public void PageSetupDialog_ShowDialog_Cancel()
    {
        using PrintDocument document = new();
        using DialogHostForm dialogOwnerForm = new();
        using PageSetupDialog dialog = new()
        {
            Document = document,
        };

        Assert.Equal(DialogResult.Cancel, dialog.ShowDialog(dialogOwnerForm));
    }

    [WinFormsFact]
    public void PageSetupDialog_ShowDialog_OK()
    {
        using PrintDocument document = new();
        using AcceptDialogForm dialogOwnerForm = new();
        using PageSetupDialog dialog = new()
        {
            Document = document,
        };

        Assert.Equal(DialogResult.OK, dialog.ShowDialog(dialogOwnerForm));
    }

    private class AcceptDialogForm : DialogHostForm
    {
        protected override void OnDialogIdle(HWND dialogHandle)
        {
            Accept(dialogHandle);
        }
    }
}
