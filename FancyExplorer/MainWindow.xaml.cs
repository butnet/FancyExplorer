/*
    Copyright 2013 Wil Hall

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Windows.Media.Animation;
using System.Windows.Controls.Primitives;
using Microsoft.WindowsAPICodePack.Controls.WindowsPresentationFoundation;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;
using System.Timers;
using Microsoft.WindowsAPICodePack.Controls;

namespace FancyExplorer
{
    public partial class MainWindow : Window
    {
        //Global HotKey
        private HwndSource hWndSource;
        private short atom;

        //Animation
        private Storyboard WindowAnimIn;
        private Storyboard WindowAnimOut;
        
        //Misc
        Taskbar taskbar = new Taskbar();
        private Dictionary<TabItem, ExplorerBrowser> browsers = new Dictionary<TabItem, ExplorerBrowser>();

        double windowOpenTop;
        double windowClosedTop;
        bool windowOpen = false;
        bool windowReady = false;
        bool tabCreateEnabled = true;

        public MainWindow()
        {
            InitializeComponent();

            this.AddHandler(CloseableTabItem.CloseTabEvent, new RoutedEventHandler(this.CloseTab));
        }

        private void CloseApplication(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OpenPreferences(object sender, RoutedEventArgs e)
        {
            new PreferencesWindow().Show();
        }

        //New tab from string path
        private void NewTab(String targetPath)
        {
            ShellObject navTarget = null;

            try
            {
                navTarget = ShellObject.FromParsingName(targetPath);
            } catch (Exception e) {
                //Default to use folder if path is invalid
                navTarget = ShellObject.FromParsingName(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            }
            NewTab(navTarget);
        }

        //New tab from ShellObject
        private void NewTab(ShellObject navTarget)
        {
            //Add a delay for creating new tabs
            System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Interval = 500;
            aTimer.Elapsed += new ElapsedEventHandler(delegate(object source, ElapsedEventArgs e) {
                tabCreateEnabled = true;
            });
            aTimer.Enabled = true;
            
            if (!tabCreateEnabled)
            {
                tabControl.SelectedIndex = tabControl.Items.Count - 1;
            }
            else {
                tabCreateEnabled = false;

                ExplorerBrowser expBrowser = new ExplorerBrowser();
                expBrowser.HorizontalAlignment = HorizontalAlignment.Left;
                expBrowser.VerticalAlignment = VerticalAlignment.Top;
                expBrowser.Height = (this.Height - 57) > 0 ? (this.Height - 57) : this.Height;
                expBrowser.Width = this.Width;
                expBrowser.NavigationTarget = navTarget;
                expBrowser.AllowDrop = true;
                expBrowser.ViewMode = ExplorerBrowserViewMode.Icon;

                StackPanel expPanel = new StackPanel();
                expPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
                expPanel.VerticalAlignment = VerticalAlignment.Stretch;
                expPanel.Margin = new Thickness(-5, 0, 0, 0);
                expPanel.Background = new SolidColorBrush(Color.FromRgb(245, 246, 247));

                WrapPanel expControls = new WrapPanel();
                expControls.Height = 34;
                expControls.Width = this.Width;
                
                Button backButton = new Button()
                {
                    Width = 29,
                    Height = 29,
                    Margin = new Thickness(32, 0, 12, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Template = (ControlTemplate)FindResource("GlassButton"),
                    Content = new Image()
                    {
                        Width = 29, //back: 20
                        Height = 29, //back: 20
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Source = new BitmapImage(new Uri("pack://application:,,,/images/back2.ico"))
                    }
                };

                Button forwardButton = new Button()
                {
                    Width = 29,
                    Height = 29,
                    Margin = new Thickness(12, 0, 12, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Template = (ControlTemplate)FindResource("GlassButton"),
                    Content = new Image()
                    {
                        Width = 29, //forward: 20
                        Height = 29, //forward: 20
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Source = new BitmapImage(new Uri("pack://application:,,,/images/forward2.ico"))
                    }
                };

                TextBox addressBar = new TextBox() {
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = this.Width - 250,
                    Margin = new Thickness(29, 3, 0, 0),
                    AcceptsReturn = false
                };

                forwardButton.PreviewMouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
                {
                    expBrowser.NavigationLogIndex += 1;
                };

                backButton.PreviewMouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
                {
                    expBrowser.NavigationLogIndex -= 1;
                };

                addressBar.KeyDown += delegate(object sender, KeyEventArgs e)
                {
                    if (e.Key == Key.Enter || e.Key == Key.Return)
                    {
                        tryNavigate(expBrowser, addressBar);
                    }
                };

                addressBar.LostKeyboardFocus += delegate(object sender, KeyboardFocusChangedEventArgs e) {
                    try
                    {
                        expBrowser.NavigationTarget = ShellObject.FromParsingName(addressBar.Text);
                    }
                    catch (Exception _e)
                    {
                        addressBar.Text = expBrowser.NavigationTarget.ParsingName;
                    }
                };

                expControls.Children.Add(backButton);
                expControls.Children.Add(forwardButton);
                expControls.Children.Add(addressBar);

                expPanel.Children.Add(expControls);
                expPanel.Children.Add(expBrowser);

                CloseableTabItem tabItem = new CloseableTabItem()
                {
                    MinWidth = 100,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Content = expPanel,
                    Header = "Test1",
                };

                tabControl.Items.Add(tabItem);
                tabControl.SelectedIndex = tabControl.Items.Count - 1;

                expBrowser.Loaded += delegate(object sender, RoutedEventArgs e)
                {
                    tabItem.Header = expBrowser.NavigationTarget.Name;
                };

                expBrowser.ExplorerBrowserControl.NavigationComplete +=
                delegate(object sender, Microsoft.WindowsAPICodePack.Controls.NavigationCompleteEventArgs e)
                {
                    tabItem.Header = e.NewLocation.Name;
                    addressBar.Text = e.NewLocation.ParsingName;

                    if (expBrowser.ExplorerBrowserControl.NavigationLog.CanNavigateBackward) {
                        backButton.IsEnabled = true;
                    } else {
                        backButton.IsEnabled = false;
                    }

                    if (expBrowser.ExplorerBrowserControl.NavigationLog.CanNavigateForward) {
                        forwardButton.IsEnabled = true;
                    } else {
                        forwardButton.IsEnabled = false;
                    }
                };

                browsers.Add(tabItem, expBrowser);
            }
        }

        private void tryNavigate(ExplorerBrowser browser, TextBox address)
        {
            try
            {
                browser.NavigationTarget = ShellObject.FromParsingName(address.Text);
            }
            catch (Exception _e)
            {
                address.Text = browser.NavigationTarget.ParsingName;
            }
        }

        private void CloseTab(object source, RoutedEventArgs args)
        {
            TabItem tabItem = args.Source as TabItem;
            if (tabItem != null)
            {
                TabControl tabControl = tabItem.Parent as TabControl;
                if (tabControl != null)
                {
                    tabControl.SelectedIndex = tabControl.Items.Count - 1;
                    tabControl.Items.Remove(tabItem);
                    browsers.Remove(tabItem);
                }
            }
        }

        private void toggleVisibility()
        {
            if (windowReady)
            {
                if (windowOpen)
                {
                    windowOut();
                }
                else
                {
                    windowIn();
                }
            }
        }

        private void windowIn()
        {
            windowOpen = true;
            WindowAnimIn.Begin();
            this.Focus();
            this.Activate();
        }

        private void windowOut()
        {
            windowOpen = false;
            WindowAnimOut.Begin();
            this.Focus();
            this.Activate();
        }

        private IntPtr GlobalKeyPress(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //Toggle visibility when global hotkey is pressed
            switch (msg)
            {
                case Win32.WM_HOTKEY:
                    toggleVisibility();
                    break;
            }

            return IntPtr.Zero;
        }

        private Point GetMousePositionWindowsForms()
        {
            //Gets global mouse coordinates
            System.Drawing.Point point = System.Windows.Forms.Control.MousePosition;
            return new Point(point.X, point.Y);
        }

        private void loadTabs()
        {
            //If we have saved paths
            if (FancyExplorer.Properties.Settings.Default.autosavedTabs.Length > 0)
            {
                String[] paths = FancyExplorer.Properties.Settings.Default.autosavedTabs.Split(',');

                //Loop through them and create tabs
                foreach (String path in paths)
                {
                    tabCreateEnabled = true;
                    NewTab(path);
                }
            }
            else
            {
                tabCreateEnabled = true;
                NewTab("");
            }
        }

        private void saveTabs()
        {
            List<String> paths = new List<String>();
            foreach (KeyValuePair<TabItem, ExplorerBrowser> pair in browsers)
            {
                //If the navigation location has changed
                if (pair.Value.NavigationLog.Count > 0 && pair.Value.NavigationLogIndex != -1)
                {
                    paths.Add(pair.Value.NavigationLog.ElementAt(pair.Value.NavigationLogIndex).ParsingName);
                }
                //Otherwise, use initial navigation location
                else
                {
                    paths.Add(pair.Value.NavigationTarget.ParsingName);
                }
            }

            FancyExplorer.Properties.Settings.Default.autosavedTabs = String.Join(",", paths.Select(x => x));
            FancyExplorer.Properties.Settings.Default.Save();
        }

        private void FancyExplorer_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;

            this.Width = System.Windows.SystemParameters.WorkArea.Width;
            this.Height = System.Windows.SystemParameters.WorkArea.Height * FancyExplorer.Properties.Settings.Default.windowHeightPercent;

            windowOpenTop = System.Windows.SystemParameters.PrimaryScreenHeight - this.Height;
            windowClosedTop = System.Windows.SystemParameters.PrimaryScreenHeight;

            this.Top = windowClosedTop;
            this.Left = 0;

            if (!taskbar.AutoHide)
            {
                if (taskbar.Position == TaskbarPosition.Left)
                {
                    this.Left += taskbar.Size.Width;
                }
                else if (taskbar.Position == TaskbarPosition.Bottom)
                {
                    windowOpenTop -= taskbar.Size.Height;
                }
            }

            tabControl.Height = this.Height;
            tabControl.Width = this.Width;

            loadTabs();

            tabControl.SelectionChanged += delegate(object selSender, SelectionChangedEventArgs selE)
            {
                if (tabControl.SelectedIndex == 0)
                {
                    NewTab("");
                }
            };

            try
            {
                Timeline.DesiredFrameRateProperty.OverrideMetadata(
                    typeof(Timeline),
                    new FrameworkPropertyMetadata { DefaultValue = FancyExplorer.Properties.Settings.Default.animFramerate }
                );
            }
            catch (Exception ex) { }

            Duration duration = new Duration(TimeSpan.FromSeconds(FancyExplorer.Properties.Settings.Default.animDuration));

            //Window open animation
            DoubleAnimation topIn = new DoubleAnimation() {
                Duration = duration,
                To = windowOpenTop
            };
            DoubleAnimation opacityIn = new DoubleAnimation() {
                Duration = duration,
                To = 1
            };
            PropertyPath pathIn = new PropertyPath("(Window.Top)");
            PropertyPath pathInOpacity = new PropertyPath("(Window.Opacity)");
            Storyboard.SetTarget(topIn, this);
            Storyboard.SetTarget(opacityIn, this);
            Storyboard.SetTargetProperty(topIn, pathIn);
            Storyboard.SetTargetProperty(opacityIn, pathInOpacity);
            WindowAnimIn = new Storyboard() {
                Duration = duration
            };
            WindowAnimIn.Children.Add(topIn);
            WindowAnimIn.Children.Add(opacityIn);

            //Window close animation
            DoubleAnimation topOut = new DoubleAnimation() {
                Duration = duration,
                To = windowClosedTop
            };
            DoubleAnimation opacityOut = new DoubleAnimation() {
                Duration = duration,
                To = 0
            };
            PropertyPath pathOut = new PropertyPath("(Window.Top)");
            PropertyPath pathOutOpacity = new PropertyPath("(Window.Opacity)");
            topOut.To = windowClosedTop;
            opacityOut.To = 0;
            Storyboard.SetTarget(topOut, this);
            Storyboard.SetTarget(opacityOut, this);
            Storyboard.SetTargetProperty(topOut, pathOut);
            Storyboard.SetTargetProperty(opacityOut, pathOutOpacity);
            WindowAnimOut = new Storyboard()
            {
                Duration = duration
            };
            WindowAnimOut.Children.Add(topOut);
            WindowAnimOut.Children.Add(opacityOut);
            
            //Make the animations resources
            Resources.Add("WindowAnimIn", WindowAnimIn);
            Resources.Add("WindowAnimOut", WindowAnimOut);

            //Run the window close animation to properly seat the window
            WindowAnimOut.Begin();

            //Configure global hotkey
            WindowInteropHelper wih = new WindowInteropHelper(this);
            hWndSource = HwndSource.FromHwnd(wih.Handle);
            hWndSource.AddHook(GlobalKeyPress);
            atom = Win32.GlobalAddAtom("GlobalSlideKey");

            if (atom == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            KeyGestureConverter converter = new KeyGestureConverter();
            KeyGesture hotkey = (KeyGesture)converter.ConvertFromInvariantString(FancyExplorer.Properties.Settings.Default.shortcutKeyGesture);

            uint modifiers = 0;

            if ((hotkey.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                modifiers = modifiers | Win32.MOD_ALT;
            }
            if ((hotkey.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                modifiers = modifiers | Win32.MOD_CONTROL;
            }
            if ((hotkey.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                modifiers = modifiers | Win32.MOD_SHIFT;
            }
            if ((hotkey.Modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
            {
                modifiers = modifiers | Win32.MOD_WIN;
            }

            if (!Win32.RegisterHotKey(wih.Handle, atom, modifiers, (uint)KeyInterop.VirtualKeyFromKey(hotkey.Key)))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            //Setup complete
            windowReady = true;
        }

        private void FancyExplorer_Deactivated(object sender, EventArgs e)
        {
            //Only close the window on deactivate if our mouse is not above it
            Point mousePos = GetMousePositionWindowsForms();
            Point windowPos = new Point(Application.Current.MainWindow.Left, Application.Current.MainWindow.Top);

            if (mousePos.X >= windowPos.X && mousePos.Y <= windowPos.Y)
            {
                windowOut();
            }
        }

        private void FancyExplorer_Closed(object sender, EventArgs e)
        {
            //Save Tabs
            saveTabs();

            //Properly Dispose of Taskbar Icon
            tbarIcon.Dispose();

            //Try to unregister global hotkey
            if (atom != 0)
            {
                Win32.UnregisterHotKey(hWndSource.Handle, atom);
            }
        }
    }
}
