using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace ExplorerTabUtility.Localization;

public sealed class LocExtension : MarkupExtension
{
    public LocExtension(string key) => Key = key;
    public string Key { get; }

    public override object ProvideValue(IServiceProvider serviceProvider) => new Binding($"[{Key}]")
    {
        Source = LocalizationManager.Instance,
        Mode = BindingMode.OneWay
    }.ProvideValue(serviceProvider);
}
