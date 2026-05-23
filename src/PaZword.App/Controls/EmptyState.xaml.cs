using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PaZword.App.Controls;

/// <summary>
/// Reusable empty-state surface: brand-circle Fluent glyph + headline + body + optional CTA.
/// Sprint 2 ships the Fluent-glyph form; sprint 9 swaps the glyph for a custom illustration
/// without changing the dependency-property surface.
/// </summary>
/// <remarks>
/// AOT note: <see cref="DependencyProperty.Register"/> calls use the typed property
/// metadata; no reflection-based binding paths are involved (all XAML callers use <c>{x:Bind}</c>).
/// </remarks>
public sealed partial class EmptyState : UserControl
{
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(string), typeof(EmptyState), new PropertyMetadata("\uE7C3"));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(EmptyState), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty BodyProperty = DependencyProperty.Register(
        nameof(Body), typeof(string), typeof(EmptyState),
        new PropertyMetadata(string.Empty, OnBodyChanged));

    public static readonly DependencyProperty CtaTextProperty = DependencyProperty.Register(
        nameof(CtaText), typeof(string), typeof(EmptyState),
        new PropertyMetadata(string.Empty, OnCtaTextChanged));

    public static readonly DependencyProperty CtaCommandProperty = DependencyProperty.Register(
        nameof(CtaCommand), typeof(ICommand), typeof(EmptyState), new PropertyMetadata(null));

    public static readonly DependencyProperty IsCtaEnabledProperty = DependencyProperty.Register(
        nameof(IsCtaEnabled), typeof(bool), typeof(EmptyState), new PropertyMetadata(true));

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Body
    {
        get => (string)GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    public string CtaText
    {
        get => (string)GetValue(CtaTextProperty);
        set => SetValue(CtaTextProperty, value);
    }

    public ICommand? CtaCommand
    {
        get => (ICommand?)GetValue(CtaCommandProperty);
        set => SetValue(CtaCommandProperty, value);
    }

    public bool IsCtaEnabled
    {
        get => (bool)GetValue(IsCtaEnabledProperty);
        set => SetValue(IsCtaEnabledProperty, value);
    }

    public Visibility BodyVisibility => string.IsNullOrEmpty(Body) ? Visibility.Collapsed : Visibility.Visible;
    public Visibility CtaVisibility => string.IsNullOrEmpty(CtaText) ? Visibility.Collapsed : Visibility.Visible;

    public EmptyState()
    {
        InitializeComponent();
    }

    private static void OnBodyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (EmptyState)d;
        ctrl.Bindings.Update();
    }

    private static void OnCtaTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (EmptyState)d;
        ctrl.Bindings.Update();
    }
}
