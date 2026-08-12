// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Forms.UITests;

public class SaveFileDialogTests : ControlTestBase
{
    public SaveFileDialogTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [WinFormsFact]
    public void SaveFileDialog_ShowDialog_Cancel()
    {
        using DialogHostForm dialogOwnerForm = new();
        using SaveFileDialog dialog = new();
        dialog.InitialDirectory = Path.GetTempPath();
        Assert.Equal(DialogResult.Cancel, dialog.ShowDialog(dialogOwnerForm));
    }

    [WinFormsFact]
    public void SaveFileDialog_ShowDialog_OK()
    {
        string filePath = Path.Join(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
        using AcceptDialogForm dialogOwnerForm = new();
        using SaveFileDialog dialog = new()
        {
            InitialDirectory = Path.GetTempPath(),
            FileName = filePath,
            OverwritePrompt = false,
        };

        Assert.Equal(DialogResult.OK, dialog.ShowDialog(dialogOwnerForm));
        Assert.Equal(filePath, dialog.FileName);
    }

    private class AcceptDialogForm : DialogHostForm
    {
        protected override void OnDialogIdle(HWND dialogHandle)
        {
            Accept(dialogHandle);
        }
    }
}
