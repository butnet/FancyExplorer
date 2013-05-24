using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FancyExplorer
{
    public partial class PreferencesWindow : Window
    {
        KeyGestureConverter converter;

        public PreferencesWindow()
        {
            InitializeComponent();

            converter = new KeyGestureConverter();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            shortcutTextBox.PreviewKeyDown += shortcutTextBox_PreviewKeyDown;
            shortcutTextBox.GotFocus += shortcutTextBox_GotFocus;
            shortcutTextBox.Text = FancyExplorer.Properties.Settings.Default.shortcutKeyGesture;

            windowHeightSlider.Minimum = 0;
            windowHeightSlider.Maximum = 1;
            windowHeightSlider.ValueChanged += windowHeightSlider_ValueChanged;

            animFPSSlider.Minimum = 0;
            animFPSSlider.Maximum = 500;
            animFPSSlider.TickPlacement = TickPlacement.None;
            animFPSSlider.TickFrequency = 1;
            animFPSSlider.ValueChanged += animFPSSlider_ValueChanged;

            animDurationSlider.Minimum = 0;
            animDurationSlider.Maximum = 5;
            animDurationSlider.ValueChanged += animDurationSlider_ValueChanged;

            animDurationSlider.Value = FancyExplorer.Properties.Settings.Default.animDuration;
            animDurationLabel.Content = String.Format("{0:0.00}", FancyExplorer.Properties.Settings.Default.animDuration) + "s";

            animFPSSlider.Value = FancyExplorer.Properties.Settings.Default.animFramerate;
            animFPSLabel.Content = ((int)FancyExplorer.Properties.Settings.Default.animFramerate).ToString() + "fps";

            windowHeightSlider.Value = FancyExplorer.Properties.Settings.Default.windowHeightPercent;
            windowHeightLabel.Content = ((int)(FancyExplorer.Properties.Settings.Default.windowHeightPercent * 100)).ToString() + "%";
        }

        void animDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            animDurationLabel.Content = String.Format("{0:0.00}", animDurationSlider.Value) + "s";
            FancyExplorer.Properties.Settings.Default.animDuration = animDurationSlider.Value;
            FancyExplorer.Properties.Settings.Default.Save();
        }

        void animFPSSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            animFPSLabel.Content = ((int)animFPSSlider.Value).ToString() + "fps";
            FancyExplorer.Properties.Settings.Default.animFramerate = (int)animFPSSlider.Value;
            FancyExplorer.Properties.Settings.Default.Save();
        }

        void windowHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            windowHeightLabel.Content = ((int)(windowHeightSlider.Value * 100)).ToString() + "%";
            FancyExplorer.Properties.Settings.Default.windowHeightPercent = windowHeightSlider.Value;
            FancyExplorer.Properties.Settings.Default.Save();
        }

        private void saveShortcut(KeyGesture shortcut)
        {
            FancyExplorer.Properties.Settings.Default.shortcutKeyGesture = converter.ConvertToInvariantString(shortcut);
            FancyExplorer.Properties.Settings.Default.Save();
        }

        private void shortcutTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            shortcutTextBox.Text = "";
        }

        private void shortcutTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
            
            if (key == Key.LeftShift || key == Key.RightShift
                || key == Key.LeftCtrl || key == Key.RightCtrl
                || key == Key.LeftAlt || key == Key.RightAlt
                || key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            KeyGesture shortcut = new KeyGesture(key, Keyboard.Modifiers);

            saveShortcut(shortcut);
            shortcutTextBox.Text = converter.ConvertToInvariantString(shortcut);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.MainWindow.Close();
            Application.Current.MainWindow = new MainWindow();
            Application.Current.MainWindow.Show();
        }
    }
}
