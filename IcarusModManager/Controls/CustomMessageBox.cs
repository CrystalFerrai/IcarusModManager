// Copyright 2022 Crystal Ferrai
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using IcarusModManager.Utils;
using System;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IcarusModManager.Controls
{
    /// <summary>
    /// A control for spawning custom message boxes. Mimics the layout and API of a standard message box but
    /// allows custom theming to match the look of an application.
    /// </summary>
    [TemplatePart(Name = PART_Message, Type = typeof(TextBox))]
    [TemplatePart(Name = PART_Icon, Type = typeof(Image))]
    [TemplatePart(Name = PART_LeftButton, Type = typeof(Button))]
    [TemplatePart(Name = PART_CenterButton, Type = typeof(Button))]
    [TemplatePart(Name = PART_RightButton, Type = typeof(Button))]
    public class CustomMessageBox : Control
    {
        // Template part names
        private const string? PART_Message = "PART_Message";
        private const string? PART_Icon = "PART_Icon";
        private const string? PART_LeftButton = "PART_LeftButton";
        private const string? PART_CenterButton = "PART_CenterButton";
        private const string? PART_RightButton = "PART_RightButton";

        // Template parts
        private TextBox? mMessagePart;
        private Image? mIconPart;
        private Button? mLeftButtonPart;
        private Button? mCenterButtonPart;
        private Button? mRightButtonPart;

        /// <summary>
        /// Gets or sets the color of the background area behind the message box buttons
        /// </summary>
        public Brush ButtonTrayBackground
        {
            get { return (Brush)GetValue(ButtonTrayBackgroundProperty); }
            set { SetValue(ButtonTrayBackgroundProperty, value); }
        }
        public static readonly DependencyProperty ButtonTrayBackgroundProperty = DependencyProperty.Register("ButtonTrayBackground", typeof(Brush), typeof(CustomMessageBox),
            new FrameworkPropertyMetadata(SystemColors.ControlLightBrush));

        /// <summary>
        /// Gets or sets the minimum width at which to show a message box window
        /// </summary>
        public double MinWindowWidth
        {
            get { return (double)GetValue(MinWindowWidthProperty); }
            set { SetValue(MinWindowWidthProperty, value); }
        }
        public static readonly DependencyProperty MinWindowWidthProperty = DependencyProperty.Register("MinWindowWidth", typeof(double), typeof(CustomMessageBox),
            new FrameworkPropertyMetadata(250.0));

        /// <summary>
        /// Gets or sets the minimum height at which to show a message box window
        /// </summary>
        public double MinWindowHeight
        {
            get { return (double)GetValue(MinWindowHeightProperty); }
            set { SetValue(MinWindowHeightProperty, value); }
        }
        public static readonly DependencyProperty MinWindowHeightProperty = DependencyProperty.Register("MinWindowHeight", typeof(double), typeof(CustomMessageBox),
            new FrameworkPropertyMetadata(100.0));

        /// <summary>
        /// Gets or sets the maximum width at which to show a message box window
        /// </summary>
        public double MaxWindowWidth
        {
            get { return (double)GetValue(MaxWindowWidthProperty); }
            set { SetValue(MaxWindowWidthProperty, value); }
        }
        public static readonly DependencyProperty MaxWindowWidthProperty = DependencyProperty.Register("MaxWindowWidth", typeof(double), typeof(CustomMessageBox),
            new FrameworkPropertyMetadata(600.0));

        /// <summary>
        /// Gets or sets the maximum height at which to show a message box window
        /// </summary>
        public double MaxWindowHeight
        {
            get { return (double)GetValue(MaxWindowHeightProperty); }
            set { SetValue(MaxWindowHeightProperty, value); }
        }
        public static readonly DependencyProperty MaxWindowHeightProperty = DependencyProperty.Register("MaxWindowHeight", typeof(double), typeof(CustomMessageBox),
            new FrameworkPropertyMetadata(600.0));

        /// <summary>
        /// Initializes static members of the CustomMessageBox class
        /// </summary>
        static CustomMessageBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CustomMessageBox), new FrameworkPropertyMetadata(typeof(CustomMessageBox)));
            IsTabStopProperty.OverrideMetadata(typeof(CustomMessageBox), new FrameworkPropertyMetadata(false));
        }

        /// <summary>
        /// Create a new instance of the CustomMessageBox class
        /// </summary>
        /// <remarks>
        /// Not intended to be used outside of the Show static method.
        /// </remarks>
        private CustomMessageBox()
        {
        }

        /// <summary>
        /// Creates and shows a message box to the user
        /// </summary>
        /// <param name="owner">The window that owns the message box</param>
        /// <param name="message">The message to display to the user</param>
        /// <param name="caption">The caption for the message box</param>
        /// <returns>A value indicating which button the user pressed on the message box</returns>
        public static MessageBoxResult Show(DependencyObject owner, string message, string caption)
        {
            return Show(owner, message, caption, MessageBoxButton.OK, MessageBoxImage.None, false);
        }

        /// <summary>
        /// Creates and shows a message box to the user
        /// </summary>
        /// <param name="owner">The window that owns the message box</param>
        /// <param name="message">The message to display to the user</param>
        /// <param name="caption">The caption for the message box</param>
        /// <param name="buttons">The buttons to include in the message box</param>
        /// <returns>A value indicating which button the user pressed on the message box</returns>
        public static MessageBoxResult Show(DependencyObject owner, string message, string caption, MessageBoxButton buttons)
        {
            return Show(owner, message, caption, buttons, MessageBoxImage.None, false);
        }
        
        /// <summary>
        /// Creates and shows a message box to the user
        /// </summary>
        /// <param name="owner">The window that owns the message box</param>
        /// <param name="message">The message to display to the user</param>
        /// <param name="caption">The caption for the message box</param>
        /// <param name="icon">The icon to include in the message box</param>
        /// <param name="playSound">Whether to play the system sound associated with the spcified icon when opening the message box</param>
        /// <returns>A value indicating which button the user pressed on the message box</returns>
        public static MessageBoxResult Show(DependencyObject owner, string message, string caption, MessageBoxImage icon, bool playSound = true)
        {
            return Show(owner, message, caption, MessageBoxButton.OK, icon, playSound);
        }
        
        /// <summary>
        /// Creates and shows a message box to the user
        /// </summary>
        /// <param name="message">The message to display to the user</param>
        /// <param name="caption">The caption for the message box</param>
        /// <returns>A value indicating which button the user pressed on the message box</returns>
        public static MessageBoxResult Show(string message, string caption)
        {
            return Show(message, caption, MessageBoxButton.OK, MessageBoxImage.None, false);
        }

        /// <summary>
        /// Creates and shows a message box to the user
        /// </summary>
        /// <param name="message">The message to display to the user</param>
        /// <param name="caption">The caption for the message box</param>
        /// <param name="buttons">The buttons to include in the message box</param>
        /// <returns>A value indicating which button the user pressed on the message box</returns>
        public static MessageBoxResult Show(string message, string caption, MessageBoxButton buttons)
        {
            return Show(message, caption, buttons, MessageBoxImage.None, false);
        }
        
        /// <summary>
        /// Creates and shows a message box to the user
        /// </summary>
        /// <param name="message">The message to display to the user</param>
        /// <param name="caption">The caption for the message box</param>
        /// <param name="icon">The icon to include in the message box</param>
        /// <param name="playSound">Whether to play the system sound associated with the spcified icon when opening the message box</param>
        /// <returns>A value indicating which button the user pressed on the message box</returns>
        public static MessageBoxResult Show(string message, string caption, MessageBoxImage icon, bool playSound = true)
        {
            return Show(message, caption, MessageBoxButton.OK, icon, playSound);
        }

        /// <summary>
        /// Creates and shows a message box to the user
        /// </summary>
        /// <param name="message">The message to display to the user</param>
        /// <param name="caption">The caption for the message box</param>
        /// <param name="buttons">The buttons to include in the message box</param>
        /// <param name="icon">The icon to include in the message box</param>
        /// <param name="playSound">Whether to play the system sound associated with the spcified icon when opening the message box</param>
        /// <returns>A value indicating which button the user pressed on the message box</returns>
        public static MessageBoxResult Show(string message, string caption, MessageBoxButton buttons, MessageBoxImage icon, bool playSound = true)
        {
            DependencyObject? mainWindow = null;
            if (Application.Current != null)
            {
                mainWindow = Application.Current.MainWindow;
            }
            return Show(mainWindow, message, caption, buttons, icon, playSound);
        }

        /// <summary>
        /// Creates and shows a message box to the user
        /// </summary>
        /// <param name="owner">The window that owns the message box</param>
        /// <param name="message">The message to display to the user</param>
        /// <param name="caption">The caption for the message box</param>
        /// <param name="buttons">The buttons to include in the message box</param>
        /// <param name="icon">The icon to include in the message box</param>
        /// <param name="playSound">Whether to play the system sound associated with the spcified icon when opening the message box</param>
        /// <returns>A value indicating which button the user pressed on the message box</returns>
        public static MessageBoxResult Show(DependencyObject? owner, string message, string caption, MessageBoxButton buttons, MessageBoxImage icon, bool playSound = true)
        {
            Window? ownerWindow = null;
            if (owner != null)
            {
                PresentationSource source = PresentationSource.FromDependencyObject(owner);
                if (source != null)
                {
                    ownerWindow = source.RootVisual as Window;
                }
                if (ownerWindow == null) throw new ArgumentException("The specified owner does not appear to be a window (or contained within one).");
            }

            CustomMessageBox control = new CustomMessageBox();
            return control.Show(ownerWindow, message, caption, buttons, icon, playSound);
        }

        /// <summary>
        /// Helper method for showing a message box
        /// </summary>
        /// <param name="owner">The owning window</param>
        /// <param name="message">The message to display</param>
        /// <param name="caption">The dialog caption</param>
        /// <param name="buttons">The buttons to display</param>
        /// <param name="icon">The icon to display</param>
        /// <param name="playSound">Whether to play a sound</param>
        /// <returns></returns>
        private MessageBoxResult Show(Window? owner, string message, string caption, MessageBoxButton buttons, MessageBoxImage icon, bool playSound)
        {
            Window window = new Window()
            {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.WidthAndHeight,
                MinWidth = MinWindowWidth,
                MinHeight = MinWindowHeight,
                MaxWidth = MaxWindowWidth,
                MaxHeight = MaxWindowHeight,
                Content = this
            };
            
            ApplyTemplate();

            // Verify template
            if (mMessagePart == null) throw new ApplicationException("MessageBox control template missing required part: " + PART_Message);
            if (mIconPart == null) throw new ApplicationException("MessageBox control template missing required part: " + PART_Icon);
            if (mLeftButtonPart == null) throw new ApplicationException("MessageBox control template missing required part: " + PART_LeftButton);
            if (mCenterButtonPart == null) throw new ApplicationException("MessageBox control template missing required part: " + PART_CenterButton);
            if (mRightButtonPart == null) throw new ApplicationException("MessageBox control template missing required part: " + PART_RightButton);

            // Set the message
            mMessagePart.Text = message;
            
            // Set the caption
            window.Title = caption;

            // Set the buttons
            MessageBoxResult result = MessageBoxResult.None;
            Action<MessageBoxResult> setResult = res =>
                {
                    result = res;
                    window.Close();
                };
            switch (buttons)
            {
                case MessageBoxButton.OK:
                    mLeftButtonPart.Content = "OK";
                    mLeftButtonPart.Command = new DelegateCommand(() => setResult(MessageBoxResult.OK));
                    mLeftButtonPart.IsDefault = true;
                    mLeftButtonPart.Visibility = Visibility.Visible;
                    mLeftButtonPart.IsTabStop = true;
                    break;
                case MessageBoxButton.OKCancel:
                    mLeftButtonPart.Content = "OK";
                    mLeftButtonPart.Command = new DelegateCommand(() => setResult(MessageBoxResult.OK));
                    mLeftButtonPart.IsDefault = true;
                    mLeftButtonPart.Visibility = Visibility.Visible;
                    mLeftButtonPart.IsTabStop = true;

                    mRightButtonPart.Content = "Cancel";
                    mRightButtonPart.Command = new DelegateCommand(() => setResult(MessageBoxResult.Cancel));
                    mRightButtonPart.IsCancel = true;
                    mRightButtonPart.Visibility = Visibility.Visible;
                    mRightButtonPart.IsTabStop = true;
                    break;
                case MessageBoxButton.YesNo:
                    mLeftButtonPart.Content = "_Yes";
                    mLeftButtonPart.Command = new DelegateCommand(() => setResult(MessageBoxResult.Yes));
                    mLeftButtonPart.IsDefault = true;
                    mLeftButtonPart.Visibility = Visibility.Visible;
                    mLeftButtonPart.IsTabStop = true;

                    mRightButtonPart.Content = "_No";
                    mRightButtonPart.Command = new DelegateCommand(() => setResult(MessageBoxResult.No));
                    mRightButtonPart.IsCancel = true;
                    mRightButtonPart.Visibility = Visibility.Visible;
                    mRightButtonPart.IsTabStop = true;
                    break;
                case MessageBoxButton.YesNoCancel:
                    mLeftButtonPart.Content = "_Yes";
                    mLeftButtonPart.Command = new DelegateCommand(() => setResult(MessageBoxResult.Yes));
                    mLeftButtonPart.IsDefault = true;
                    mLeftButtonPart.Visibility = Visibility.Visible;
                    mLeftButtonPart.IsTabStop = true;

                    mCenterButtonPart.Content = "_No";
                    mCenterButtonPart.Command = new DelegateCommand(() => setResult(MessageBoxResult.No));
                    mCenterButtonPart.Visibility = Visibility.Visible;
                    mCenterButtonPart.IsTabStop = true;

                    mRightButtonPart.Content = "Cancel";
                    mRightButtonPart.Command = new DelegateCommand(() => setResult(MessageBoxResult.Cancel));
                    mRightButtonPart.IsCancel = true;
                    mRightButtonPart.Visibility = Visibility.Visible;
                    mRightButtonPart.IsTabStop = true;
                    break;
                default:
                    throw new ArgumentException("Invalid message box buttons specified", "buttons");
            }
            FocusManager.SetFocusedElement(window, mLeftButtonPart);

            // Set the icon and sound
            if (icon != MessageBoxImage.None)
            {
                switch (icon)
                {
                    case MessageBoxImage.Question:
                        mIconPart.Source = MessageBoxIcons.Question;
                        break;
                    case MessageBoxImage.Information: // Also Asterisk
                        mIconPart.Source = MessageBoxIcons.Information;
                        break;
                    case MessageBoxImage.Warning: // Also Exclamation
                        mIconPart.Source = MessageBoxIcons.Warning;
                        break;
                    case MessageBoxImage.Error: // Also Hand, Stop
                        mIconPart.Source = MessageBoxIcons.Error;
                        break;
                    default:
                        throw new ArgumentException("Invalid message box icon specified", "icon");
                }
                mIconPart.Visibility = Visibility.Visible;
            }

            if (playSound && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SystemSound? sound = null;
                if (icon != MessageBoxImage.None)
                {
                    switch (icon)
                    {
                        case MessageBoxImage.Question:
                            sound = SystemSounds.Question;
                            break;
                        case MessageBoxImage.Information: // Also Asterisk
                            sound = SystemSounds.Asterisk;
                            break;
                        case MessageBoxImage.Warning: // Also Exclamation
                            sound = SystemSounds.Exclamation;
                            break;
                        case MessageBoxImage.Error: // Also Hand, Stop
                            sound = SystemSounds.Hand;
                            break;
                        default:
                            throw new ArgumentException("Invalid message box icon specified", "icon");
                    }
                    mIconPart.Visibility = Visibility.Visible;
                }
                if (sound != null) sound.Play();
            }

            window.ShowDialog();
            return result;
        }

        /// <summary>
        /// Called when a control template has been applied to the control
        /// </summary>
        public override void OnApplyTemplate()
        {
            mMessagePart = GetTemplateChild(PART_Message) as TextBox;
            mIconPart = GetTemplateChild(PART_Icon) as Image;
            mLeftButtonPart = GetTemplateChild(PART_LeftButton) as Button;
            mCenterButtonPart = GetTemplateChild(PART_CenterButton) as Button;
            mRightButtonPart = GetTemplateChild(PART_RightButton) as Button;
            base.OnApplyTemplate();
        }

        /// <summary>
        /// Image sources for message box icons
        /// </summary>
        private static class MessageBoxIcons
        {
            /// <summary>
            /// Gets the image for the question icon
            /// </summary>
            public static ImageSource Question { get; private set; }

            /// <summary>
            /// Gets the image for the information icon
            /// </summary>
            public static ImageSource Information { get; private set; }

            /// <summary>
            /// Gets the image for the warning icon
            /// </summary>
            public static ImageSource Warning { get; private set; }

            /// <summary>
            /// Gets the image for the error icon
            /// </summary>
            public static ImageSource Error { get; private set; }

            /// <summary>
            /// Initializes static members of the MessageBoxIcons class
            /// </summary>
            static MessageBoxIcons()
            {
                Question = LoadIcon(32514);
                Information = LoadIcon(32516);
                Warning = LoadIcon(32515);
                Error = LoadIcon(32513);
            }

            /// <summary>
            /// Returns the system icon with the specified ID
            /// </summary>
            /// <param name="id">The id of the icon to load</param>
            /// <remarks>
            /// See https://msdn.microsoft.com/en-us/library/windows/desktop/ms648072.aspx for icon IDs
            /// </remarks>
            private static ImageSource LoadIcon(int id)
            {
                return Imaging.CreateBitmapSourceFromHIcon(LoadIcon(IntPtr.Zero, id), new Int32Rect(), BitmapSizeOptions.FromEmptyOptions());
            }

            /// <summary>
            /// Native method to load a system icon
            /// </summary>
            /// <param name="hInstance">Should be null (IntPtr.Zero)</param>
            /// <param name="iconId">The ID of the icon to load</param>
            /// <returns>A native handle to the loaded icon</returns>
            [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
            private static extern IntPtr LoadIcon(IntPtr hInstance, int iconId);
        }
    }
}
