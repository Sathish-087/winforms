// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Forms.UITests;

public class FolderBrowserDialogTests : ControlTestBase
{
    public FolderBrowserDialogTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    // Regression test for https://github.com/dotnet/winforms/issues/7981
    [WinFormsTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void FolderBrowserDialog_ShowDialog(bool autoUpgradeEnabled)
    {
        using DialogHostForm dialogOwnerForm = new();
        using FolderBrowserDialog dialog = new()
        {
            AutoUpgradeEnabled = autoUpgradeEnabled,
        };

        Assert.Equal(DialogResult.Cancel, dialog.ShowDialog(dialogOwnerForm));
    }

    [WinFormsTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void FolderBrowserDialog_ShowDialog_OK(bool autoUpgradeEnabled)
    {
        string selectedPath = Path.GetTempPath();
        using AcceptDialogForm dialogOwnerForm = new();
        using FolderBrowserDialog dialog = new()
        {
            AutoUpgradeEnabled = autoUpgradeEnabled,
            SelectedPath = selectedPath,
        };

        Assert.Equal(DialogResult.OK, dialog.ShowDialog(dialogOwnerForm));
        Assert.Equal(selectedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            dialog.SelectedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    private class AcceptDialogForm : DialogHostForm
    {
        protected override void OnDialogIdle(HWND dialogHandle)
        {
            Accept(dialogHandle);
        }
    }
}
