using System;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UntamedAndroidSubsystem.Views;
using WinUIEx;

namespace UntamedAndroidSubsystem;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(titleBar);
        Title = "AppDisplayName".GetLocalized()!;
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        NavView.SelectedItem = NavView.MenuItems[0];
        NavigateToPage(typeof(DevicesPage));
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        var pageToNavigate = (args.InvokedItemContainer as NavigationViewItem)!.Tag switch
        {
            "Device" => typeof(DevicesPage),
            "Settings" => typeof(SettingsPage),
            _ => typeof(DevicesPage),
        };
        NavigateToPage(pageToNavigate);
    }

    private void NavigateToPage(Type pageType)
    {
        if (ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }
}
