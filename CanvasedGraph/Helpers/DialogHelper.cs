using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using CanvasedGraph.UI.Controls;

namespace CanvasedGraph.Helper;
public static class DialogHelper
{
    public static async Task ShowErrorDialogAsync(string title, XamlRoot root)
    {
        ContentDialog errorDialog = new ContentDialog();

        errorDialog.XamlRoot = root;
        errorDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
        errorDialog.Title = title;
        errorDialog.CloseButtonText = "Ок";
        errorDialog.DefaultButton = ContentDialogButton.Close;

        await errorDialog.ShowAsync();
    }
    public static async Task<string> ShowSingleInputDialogAsync(XamlRoot root, string title = "Заголовок", string placeholder = "Введите чо то", string primaryText = "Ок", string closeText = "Отмена")
    {
        var content = new SingleInputDialog();
        var dialog = new ContentDialog();

        dialog.XamlRoot = root;
        dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
        dialog.Title = title;
        dialog.PrimaryButtonText = primaryText;
        dialog.CloseButtonText = closeText;
        dialog.DefaultButton = ContentDialogButton.Primary;
        content.Placeholder = placeholder;
        dialog.Content = content;

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            return content.Input ?? null;
        }
        return null;
    }
}