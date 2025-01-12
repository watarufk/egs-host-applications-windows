﻿namespace WpfApplication2
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Linq;
    using System.Windows;
    using System.Diagnostics;
    using Egs;
    using Egs.Views;
    using Egs.DotNetUtility;
    using Egs.PropertyTypes;

    public partial class App : Application
    {
        public EgsDevice Device { get; private set; }
        public OnePersonBothHandsViewModel OnePersonBothHandsViewModel { get; private set; }
        public IList<CursorForm> CursorViews { get; private set; }
        public CameraViewUserControlModel CameraViewUserControlModel { get; private set; }
        public CameraViewWindowModel CameraViewWindowModel { get; private set; }
        public CameraViewWindow CameraViewWindow { get; private set; }

        public App()
            : base()
        {
            Egs.BindableResources.Current.CultureChanged += delegate
            {
                ApplicationCommonSettings.HostApplicationName = Egs.EgsDeviceControlCore.Properties.Resources.CommonStrings_GestureCamera;
                //ApplicationCommonSettings.HostApplicationName = "WpfApplication2";
            };

            // You can change the application CultureInfo to some cultures.
            // The next line lets it use OS culture
            Egs.BindableResources.Current.ChangeCulture("");
            //Egs.BindableResources.Current.ChangeCulture(ApplicationCommonSettings.DefaultCultureInfoName);
            //Egs.BindableResources.Current.ChangeCulture("en");
            //Egs.BindableResources.Current.ChangeCulture("ja");
            //Egs.BindableResources.Current.ChangeCulture("zh-Hans");

            if (DuplicatedProcessStartBlocking.TryGetMutexOnTheBeginningOfApplicationConstructor() == false)
            {
                var msg = string.Format(System.Globalization.CultureInfo.InvariantCulture, Egs.EgsDeviceControlCore.Properties.Resources.CommonStrings_Application0IsAlreadyRunning, ApplicationCommonSettings.HostApplicationName);
                MessageBox.Show(msg, ApplicationCommonSettings.HostApplicationName);
                Application.Current.Shutdown();
                return;
            }


            // Sorry, EgsHostSettings and EgsDevice.GetDefaultEgsDevice(EgsDeviceSettings) are no longer available.
            // Device settings are saved on Host PC,
            // and the host application transfers the device settings to connected device when it starts running or a device is connected.
            // You can use EgsDevice object directly, without EgsHostAppBaseComponents or EgsHostOnUserControl.
            Device = EgsDevice.GetDefaultEgsDevice();
            Device.Settings.FaceDetectionMethod.Value = FaceDetectionMethods.DefaultProcessOnEgsDevice;
            Device.Settings.IsToDetectFaces.Value = true;
            Device.Settings.IsToDetectHands.Value = true;


            // OnePersonBothHandsViewModel can receive information from EgsDevice on EgsGestureHidReport.ReportUpdated event etc.
            // It interprets the information from EgsDevice for CursorForm and so on.
            OnePersonBothHandsViewModel = new OnePersonBothHandsViewModel();
            // A lot of classes in "Egs" namespace have "InitializeOnceAtStartup..." methods.
            // Please call the initialization before they are used as arguments of some other objects, or before some event handlers are attached.
            OnePersonBothHandsViewModel.InitializeOnceAtStartup(Device);
            // CursorForm shows Gesture Cursor.  It is available on Windows 7, 8, 8.1 (contains start screen) and 10.
            CursorViews = new CursorForm[Device.TrackableHandsCountMaximum];
            CursorViews[0] = new CursorForm();
            CursorViews[1] = new CursorForm();
            CursorViews[0].InitializeOnceAtStartup(OnePersonBothHandsViewModel.RightHand, ImageInformationSet.CreateDefaultRightCursorImageInformationSetList());
            CursorViews[1].InitializeOnceAtStartup(OnePersonBothHandsViewModel.LeftHand, ImageInformationSet.CreateDefaultLeftCursorImageInformationSetList());
            Device.EgsGestureHidReport.ReportUpdated += EgsGestureHidReport_ReportUpdated;

            // Sorry, specification is changed.  You need to make an object of CameraViewUserControlModel.
            CameraViewUserControlModel = new CameraViewUserControlModel();
            CameraViewWindowModel = new CameraViewWindowModel();
            CameraViewWindow = new CameraViewWindow();

            CameraViewUserControlModel.IsToDrawImageSet = true;
            Device.Settings.IsToDrawBordersOnCameraViewImageByDevice.Value = false;

            CameraViewUserControlModel.InitializeOnceAtStartup(Device);
            CameraViewWindowModel.InitializeOnceAtStartup(Device);
            CameraViewWindow.InitializeOnceAtStartup(CameraViewWindowModel, CameraViewUserControlModel);

            CameraViewWindow.MainMenuItemsPanel.Columns = 2;
            // Minimize button needs EgsHostAppBaseComponents, because there is no way to "Un"-minimize it except for tray icon in system notification area, currently.
            // AppTrayIconAndMenuItemsComponent manages the tray icon, and change the visibility of Settings Window and Camera View.
            CameraViewWindow.MinimizeButton.Visibility = Visibility.Collapsed;
            // Settings button needs EgsHostAppBaseComponents, because only EgsHostAppBaseComponents has all information about host app,
            // and SettingsWindow can change the settings of EgsHostAppBaseComponents.
            CameraViewWindow.SettingsButton.Visibility = Visibility.Collapsed;
            // EgsHostAppBaseComponents also attachs an event handler which calls EgsHostAppBaseComponents.Dispose() and closes the application.
            CameraViewWindowModel.ExitCommand.PerformEventHandler += delegate
            {
                // In this example program, the next line means the application exit.
                CameraViewWindow.Close();
            };



            var button = new System.Windows.Controls.Button() { Content = "Show Dialog" };
            button.Click += delegate { MessageBox.Show("Hello!"); };
            CameraViewWindow.MainMenuItemsPanel.Children.Insert(0, button);
            CameraViewWindow.KeyDown += (sender, e) =>
            {
                switch (e.Key)
                {
                    case System.Windows.Input.Key.Escape:
                        // This application does not use EgsHostAppBaseComponents, so this just closes the window.
                        CameraViewWindow.Close();
                        break;
                }
            };

            // But this event handler attach close cursors and device.
            CameraViewWindow.Closed += delegate
            {
                Device.EgsGestureHidReport.ReportUpdated -= EgsGestureHidReport_ReportUpdated;

                // When you get EgsDevice by EgsDevice.GetDefaultEgsDevice, you need to call EgsDevice.CloseDefaultEgsDevice(). 
                Device.Settings.IsToDetectFaces.Value = false;
                Device.Settings.IsToDetectHands.Value = false;
                EgsDevice.CloseDefaultEgsDevice();

                // And then, you need to close cursorViews.
                foreach (var cursorView in CursorViews) { cursorView.Close(); }
            };

            this.Exit += delegate
            {
                DuplicatedProcessStartBlocking.ReleaseMutex();
            };
        }

        bool isDrawingCursors = false;
        void EgsGestureHidReport_ReportUpdated(object sender, EventArgs e)
        {
            OnePersonBothHandsViewModel.RightHand.UpdateByEgsGestureHidReportHand(Device.EgsGestureHidReport.Hands[0]);
            OnePersonBothHandsViewModel.LeftHand.UpdateByEgsGestureHidReportHand(Device.EgsGestureHidReport.Hands[1]);

            // If Task is not used, delay in moving cursors can accumulate.
            System.Threading.Tasks.Task.Run(() =>
            {
                if (isDrawingCursors) { return; }
                isDrawingCursors = true;
                for (int i = 0; i < Device.TrackableHandsCount; i++)
                {
                    CursorViews[i].UpdatePosition();
                }
                isDrawingCursors = false;
            });
        }
    }
}
