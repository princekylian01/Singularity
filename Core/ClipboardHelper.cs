using System;
using System.Windows;

namespace Singularity.Core
{
    public static class ClipboardHelper
    {
        public static string GetUrlFromClipboard()
        {
            if (Clipboard.ContainsText())
            {
                string text = Clipboard.GetText().Trim();
                return Uri.IsWellFormedUriString(text, UriKind.Absolute) ? text : null;
            }
            return null;
        }
    }
}