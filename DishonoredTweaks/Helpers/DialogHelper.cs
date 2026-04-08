using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using MaterialDesignThemes.Wpf;

namespace DishonoredTweaks.Helpers
{
    public static class DialogHelper
    {
        private static bool _isDialogOpen;
        private static readonly object DialogLock = new();
        public static string ConfirmationYesText { get; set; } = "Yes";
        public static string ConfirmationNoText { get; set; } = "No";
        public static string OkText { get; set; } = "OK";

        public static async Task<bool> ShowConfirmationAsync(string title, string message)
        {
            lock (DialogLock)
            {
                if (_isDialogOpen)
                {
                    return false;
                }

                _isDialogOpen = true;
            }

            try
            {
                object? result = await DialogHost.Show(new ConfirmationDialog(title, message), "RootDialog");
                return result is bool value && value;
            }
            finally
            {
                lock (DialogLock)
                {
                    _isDialogOpen = false;
                }
            }
        }

        public static async Task ShowMessageAsync(string title, string message, MessageType messageType = MessageType.Information)
        {
            lock (DialogLock)
            {
                if (_isDialogOpen)
                {
                    return;
                }

                _isDialogOpen = true;
            }

            try
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await DialogHost.Show(new MessageDialog(title, message, messageType), "RootDialog");
                });
            }
            finally
            {
                lock (DialogLock)
                {
                    _isDialogOpen = false;
                }
            }
        }

        public static async void ShowMessage(string title, string message, MessageType messageType = MessageType.Information)
        {
            await ShowMessageAsync(title, message, messageType);
        }

        public enum MessageType
        {
            Information,
            Warning,
            Error,
            Success
        }
    }

    public sealed class ConfirmationDialog : System.Windows.Controls.UserControl
    {
        public ConfirmationDialog(string title, string message)
        {
            System.Windows.Controls.Border border = new()
            {
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Background = System.Windows.Media.Brushes.White,
                Padding = new Thickness(20),
                MaxWidth = 560,
                MinWidth = 320
            };

            System.Windows.Controls.StackPanel stackPanel = new();

            System.Windows.Controls.TextBlock titleText = new()
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 16)
            };

            System.Windows.Controls.TextBlock messageText = new()
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 16),
                MaxWidth = 520
            };

            System.Windows.Controls.StackPanel buttonPanel = new()
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            System.Windows.Controls.Button yesButton = new()
            {
                Content = DialogHelper.ConfirmationYesText,
                Margin = new Thickness(0, 0, 8, 0),
                Style = (Style)System.Windows.Application.Current.FindResource("MaterialDesignRaisedButton")
            };
            yesButton.Click += (_, _) => DialogHost.CloseDialogCommand.Execute(true, yesButton);

            System.Windows.Controls.Button noButton = new()
            {
                Content = DialogHelper.ConfirmationNoText,
                Style = (Style)System.Windows.Application.Current.FindResource("MaterialDesignRaisedButton")
            };
            noButton.Click += (_, _) => DialogHost.CloseDialogCommand.Execute(false, noButton);

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);

            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(messageText);
            stackPanel.Children.Add(buttonPanel);

            border.Child = stackPanel;
            Content = border;
        }
    }

    public sealed class MessageDialog : System.Windows.Controls.UserControl
    {
        public MessageDialog(string title, string message, DialogHelper.MessageType _)
        {
            System.Windows.Controls.Border border = new()
            {
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Background = System.Windows.Media.Brushes.White,
                Padding = new Thickness(20),
                MaxWidth = 560,
                MinWidth = 320
            };

            System.Windows.Controls.StackPanel stackPanel = new();

            System.Windows.Controls.TextBlock titleText = new()
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 16)
            };

            System.Windows.Controls.TextBlock messageText = new()
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 16),
                MaxWidth = 520
            };
            ParseAndAddInlines(messageText, message);

            System.Windows.Controls.Button okButton = new()
            {
                Content = DialogHelper.OkText,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Style = (Style)System.Windows.Application.Current.FindResource("MaterialDesignRaisedButton")
            };
            okButton.Click += (_, _) => DialogHost.CloseDialogCommand.Execute(null, okButton);

            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(messageText);
            stackPanel.Children.Add(okButton);

            border.Child = stackPanel;
            Content = border;
        }

        private static void ParseAndAddInlines(System.Windows.Controls.TextBlock textBlock, string message)
        {
            MatchCollection matches = Regex.Matches(message, "(https?://[^\\s]+)");
            if (matches.Count == 0)
            {
                textBlock.Text = message;
                return;
            }

            int lastIndex = 0;
            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                {
                    textBlock.Inlines.Add(new Run(message.Substring(lastIndex, match.Index - lastIndex)));
                }

                string url = match.Value;
                Hyperlink hyperlink = new(new Run(url))
                {
                    NavigateUri = new Uri(url)
                };
                hyperlink.RequestNavigate += (_, args) =>
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = args.Uri.AbsoluteUri,
                        UseShellExecute = true
                    });
                    args.Handled = true;
                };
                textBlock.Inlines.Add(hyperlink);

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < message.Length)
            {
                textBlock.Inlines.Add(new Run(message.Substring(lastIndex)));
            }
        }
    }
}
