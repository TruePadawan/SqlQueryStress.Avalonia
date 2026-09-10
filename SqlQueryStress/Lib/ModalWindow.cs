using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;

namespace SqlQueryStress.Lib;

/// <summary>
/// Represents a modal window that can be closed with a gesture
/// </summary>
public class ModalWindow : Window
{
    protected ModalWindow(KeyGesture? closingGesture = null)
    {
        KeyBindings.Add(new KeyBinding
        {
            Gesture = closingGesture ?? new KeyGesture(Key.Escape),
            Command = new RelayCommand(Close)
        });
    }
}