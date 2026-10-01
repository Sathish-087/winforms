// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Drawing.Design;

namespace System.Windows.Forms.Design;

/// <summary>
///  Provides an editor for choosing a folder from the filesystem.
/// </summary>
[CLSCompliant(false)]
public partial class FolderNameEditor : UITypeEditor
{
    private FolderBrowser? _folderBrowser;

    public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
    {
        if (_folderBrowser is null)
        {
            _folderBrowser = new FolderBrowser();
            InitializeDialog(_folderBrowser);
        }

        using FolderBrowserDialog folderBrowserDialog = CreateFolderBrowserDialog(_folderBrowser, value);

        if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
        {
            return value;
        }

        _folderBrowser.SetDirectoryPath(folderBrowserDialog.SelectedPath);
        return folderBrowserDialog.SelectedPath;
    }

    private protected static FolderBrowserDialog CreateFolderBrowserDialog(FolderBrowser folderBrowser, object? value)
    {
        FolderBrowserDialog folderBrowserDialog = new()
        {
            Description = folderBrowser.Description,
            RootFolder = (Environment.SpecialFolder)folderBrowser.StartLocation
        };

        if (value is string stringValue)
        {
            folderBrowserDialog.SelectedPath = stringValue;
        }

        return folderBrowserDialog;
    }

    /// <inheritdoc />
    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) => UITypeEditorEditStyle.Modal;

    /// <summary>
    ///  Initializes the folder browser dialog when it is created. This gives you an opportunity
    ///  to configure the dialog as you please. The default implementation provides a generic folder browser.
    /// </summary>
    protected virtual void InitializeDialog(FolderBrowser folderBrowser)
    {
    }
}
