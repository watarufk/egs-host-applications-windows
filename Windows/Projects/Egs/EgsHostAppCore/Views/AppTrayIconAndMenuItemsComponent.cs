﻿namespace Egs.Views
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Windows.Forms;

    public partial class AppTrayIconAndMenuItemsComponent : Component
    {
        // NOTE: It is important to derive this class from "Component".

        Icon deviceIsConnectedIcon { get; set; }
        Icon deviceIsNotConnectedIcon { get; set; }

        // Hide NotifyIcon object.
        //internal NotifyIcon NotifyIconInTray { get { return notifyIconInTray; } }

        Padding MenuItemLabelPadding { get; set; }
        public Label EgsHostApplicationNameMenuItemLabel { get; private set; }
        public ToolStripControlHost EgsHostApplicationNameMenuItem { get; private set; }
        ToolStripSeparator ToolStripSeparator03 { get; set; }
        public Label IsConnectedMenuItemLabel { get; private set; }
        public ToolStripControlHost IsConnectedMenuItem { get; private set; }
        ToolStripSeparator ToolStripSeparator02 { get; set; }
        public ToolStripMenuItem CameraViewMenuItem { get; private set; }
        public ToolStripMenuItem SettingsMenuItem { get; private set; }
        ToolStripSeparator ToolStripSeparator01 { get; set; }
        public ToolStripMenuItem ExitMenuItem { get; private set; }

        EgsHostAppBaseComponents ownerEgsHostAppBaseComponents { get; set; }

        public AppTrayIconAndMenuItemsComponent() : this(null) { }
        public AppTrayIconAndMenuItemsComponent(IContainer container)
        {
            if (container != null) { container.Add(this); }

            InitializeComponent();
            InitializeMenuItems();
            BindableResources.Current.CultureChanged += delegate { OnBindableResourcesCurrentCultureChanged(); };
        }

        public void OnBindableResourcesCurrentCultureChanged()
        {
            EgsHostApplicationNameMenuItemLabel.Text = ApplicationCommonSettings.HostApplicationName;
            IsConnectedMenuItemLabel.Text = ownerEgsHostAppBaseComponents.Device.DeviceStatusString;
            CameraViewMenuItem.Text = Egs.EgsDeviceControlCore.Properties.Resources.CommonStrings_CameraView;
            SettingsMenuItem.Text = Egs.EgsDeviceControlCore.Properties.Resources.CommonStrings_Settings;
            ExitMenuItem.Text = Egs.EgsDeviceControlCore.Properties.Resources.CommonStrings_Exit;
            notifyIconInTray.Text = ApplicationCommonSettings.HostApplicationName;
        }

        void InitializeMenuItems()
        {
            MenuItemLabelPadding = new Padding(1);
            EgsHostApplicationNameMenuItemLabel = new Label();
            EgsHostApplicationNameMenuItem = new ToolStripControlHost(EgsHostApplicationNameMenuItemLabel) { Margin = MenuItemLabelPadding };
            ToolStripSeparator03 = new ToolStripSeparator();
            IsConnectedMenuItemLabel = new Label();
            IsConnectedMenuItem = new ToolStripControlHost(IsConnectedMenuItemLabel) { Margin = MenuItemLabelPadding };
            ToolStripSeparator02 = new ToolStripSeparator();
            CameraViewMenuItem = new ToolStripMenuItem();
            SettingsMenuItem = new ToolStripMenuItem();
            ToolStripSeparator01 = new ToolStripSeparator();
            ExitMenuItem = new ToolStripMenuItem();
            contextMenuStripFromNotifyIconInTray.Items.AddRange(new ToolStripItem[]
            {
                EgsHostApplicationNameMenuItem,
                ToolStripSeparator03,
                IsConnectedMenuItem,
                ToolStripSeparator02,
                CameraViewMenuItem,
                SettingsMenuItem,
                ToolStripSeparator01,
                ExitMenuItem
            });
        }

        public void MenuItemsRemoveAt(int index)
        {
            contextMenuStripFromNotifyIconInTray.Items.RemoveAt(index);
        }

        public void MenuItemsInsert(int index, ToolStripItem value)
        {
            contextMenuStripFromNotifyIconInTray.Items.Insert(index, value);
        }

        internal void InitializeOnceAtStartup(EgsHostAppBaseComponents egsHostAppBaseComponents)
        {
            Trace.Assert(egsHostAppBaseComponents != null);
            ownerEgsHostAppBaseComponents = egsHostAppBaseComponents;

            // TODO: low priority.  Decide the specification about multiple device connections.
            deviceIsConnectedIcon = new Icon("Resources/HandIcon_DeviceIsConnected.ico", new Size(16, 16));
            deviceIsNotConnectedIcon = new Icon("Resources/HandIcon_DeviceIsDisconnected.ico", new Size(16, 16));

            OnBindableResourcesCurrentCultureChanged();

            ownerEgsHostAppBaseComponents.Device.IsConnectedChanged += (sender, e) =>
            {
                // NOTE: Before I derived this class from System.Windows.Forms.Form, and it caused various problems.  Now I derive this class from System.ComponentModel.Component, it solved the problems.
                try
                {
                    // MUSTDO: FIX: In some cases, NullReferenceException occurs, after I changed the update way of IsConnected.
                    if (deviceIsConnectedIcon != null && deviceIsNotConnectedIcon != null) { notifyIconInTray.Icon = ownerEgsHostAppBaseComponents.Device.IsConnected ? deviceIsConnectedIcon : deviceIsNotConnectedIcon; }
                    IsConnectedMenuItemLabel.Text = ownerEgsHostAppBaseComponents.Device.DeviceStatusString;
                }
                catch (Exception ex)
                {
                    if (ApplicationCommonSettings.IsDebugging) { Debugger.Break(); }
                    Debug.WriteLine(ex.Message);
                }
            };
            ownerEgsHostAppBaseComponents.CameraViewWindowModel.WindowStateChanged += CameraViewWindowModel_WindowStateChanged;
            ownerEgsHostAppBaseComponents.SettingsWindow.IsVisibleChanged += SettingsWindow_IsVisibleChanged;

            notifyIconInTray.MouseDoubleClick += (sender, e) =>
            {
                ownerEgsHostAppBaseComponents.CameraViewWindowModel.ToggleWindowStateControlMethodOnAutoOrOff();
            };

            CameraViewMenuItem.Click += (sender, e) =>
            {
                ownerEgsHostAppBaseComponents.CameraViewWindowModel.ToggleWindowStateControlMethodOnAutoOrOff();
            };
            SettingsMenuItem.Click += (sender, e) =>
            {
                ownerEgsHostAppBaseComponents.SettingsWindow.ToggleVisibility();
            };
            ExitMenuItem.Click += (sender, e) =>
            {
                ownerEgsHostAppBaseComponents.Dispose();
            };

            notifyIconInTray.Visible = true;
            notifyIconInTray.Icon = ownerEgsHostAppBaseComponents.Device.IsConnected ? deviceIsConnectedIcon : deviceIsNotConnectedIcon;
            IsConnectedMenuItemLabel.Text = ownerEgsHostAppBaseComponents.Device.DeviceStatusString;
            CameraViewMenuItem.Checked = ownerEgsHostAppBaseComponents.CameraViewWindowModel.IsNormalOrElseMinimized;
            SettingsMenuItem.Checked = ownerEgsHostAppBaseComponents.SettingsWindow.IsVisible;
        }

        void CameraViewWindowModel_WindowStateChanged(object sender, EventArgs e)
        {
            CameraViewMenuItem.Checked = ownerEgsHostAppBaseComponents.CameraViewWindowModel.IsNormalOrElseMinimized;
        }

        void SettingsWindow_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            SettingsMenuItem.Checked = ownerEgsHostAppBaseComponents.SettingsWindow.IsVisible;
        }
    }
}
