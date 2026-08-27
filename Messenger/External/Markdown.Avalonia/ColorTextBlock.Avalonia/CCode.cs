using Avalonia;
using Avalonia.Media;
using ColorTextBlock.Avalonia.Fonts;
using System.Collections.Generic;

namespace ColorTextBlock.Avalonia {
    /// <summary>
    /// Monospace decoration
    /// </summary>
    public class CCode : CSpan {
        /// <summary>
        /// Monospace font family used for code display.
        /// </summary>
        /// <see cref="MonospaceFontFamily"/>
        public static readonly StyledProperty<FontFamily> MonospaceFontFamilyProperty =
            AvaloniaProperty.Register<CCode, FontFamily>(
                nameof(MonospaceFontFamily),
                defaultValue: FontFamilyCollector.TryGetMonospace() ?? FontFamily.Default,
                inherits: true);

        public CCode() {
            var obsvr = this.GetBindingObservable(MonospaceFontFamilyProperty);
            Bind(FontFamilyProperty, obsvr);
        }

        public CCode(IEnumerable<CInline> inlines) : base(inlines) {
            var obsvr = this.GetBindingObservable(MonospaceFontFamilyProperty);
            Bind(FontFamilyProperty, obsvr);
        }

        /// <summary>
        /// Monospace font family used for code display.
        /// </summary>
        public FontFamily MonospaceFontFamily {
            get { return GetValue(MonospaceFontFamilyProperty); }
            set { SetValue(MonospaceFontFamilyProperty, value); }
        }
    }
}
