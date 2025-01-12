﻿namespace EgsInternal.ZkooHostApp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Diagnostics;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Egs;
    using Egs.DotNetUtility;
    using Egs.Views;
    using Egs.ZkooTutorial;

    public partial class ZkooHostAppWithTutorialApplication : Application
    {
        EgsHostAppBaseComponents hostAppComponents { get; set; }
        ZkooTutorialModel zkooTutorialModel { get; set; }
        MainNavigationWindow navigator { get; set; }

        public ZkooHostAppWithTutorialApplication()
            : base()
        {
            // TODO: Check the correct way to catch all exceptions and show it "We're sorry" dialog.  But the next way is still meaningless now, and sometimes this code can cause a problem that application cannot exit.
            //AppDomain.CurrentDomain.UnhandledException += (sender, e) => { ShutdownApplicationByException((Exception)e.ExceptionObject); };

            try
            {
                Egs.BindableResources.Current.CultureChanged += delegate
                {
                    ApplicationCommonSettings.HostApplicationName = Egs.EgsDeviceControlCore.Properties.Resources.CommonStrings_Zkoo;
                    Egs.ZkooTutorial.BindableResources.Current.ChangeCulture(Egs.EgsDeviceControlCore.Properties.Resources.Culture.Name);
                };

                Egs.BindableResources.Current.ChangeCulture(ApplicationCommonSettings.DefaultCultureInfoName);

                if (DuplicatedProcessStartBlocking.TryGetMutexOnTheBeginningOfApplicationConstructor() == false)
                {
                    var msg = string.Format(System.Globalization.CultureInfo.InvariantCulture, Egs.EgsDeviceControlCore.Properties.Resources.CommonStrings_Application0IsAlreadyRunning, ApplicationCommonSettings.HostApplicationName);
                    MessageBox.Show(msg, ApplicationCommonSettings.HostApplicationName);
                    if (Application.Current != null) { Application.Current.Shutdown(); }
                    return;
                }

                hostAppComponents = new EgsHostAppBaseComponents();
                hostAppComponents.InitializeOnceAtStartup();
                hostAppComponents.HasResetSettings += delegate
                {
                    // You can modify the application default settings here.
                    hostAppComponents.Device.Settings.FaceDetectionMethod.Value = Egs.PropertyTypes.FaceDetectionMethods.DefaultProcessOnEgsHostApplication;
                    hostAppComponents.IsToStartTutorialWhenHostApplicationStart = true;
                };
                if (SettingsSerialization.LoadSettingsJsonFile(hostAppComponents) == false) { hostAppComponents.Reset(); }

                hostAppComponents.CameraViewWindow.Closed += delegate { hostAppComponents.Dispose(); };

                hostAppComponents.Disposing += delegate
                {
                    // NOTE: Save settings before Dispose().  Target event is not Disposed but Disposing.
                    if (hostAppComponents.CanSaveSettingsJsonFileSafely) { SettingsSerialization.SaveSettingsJsonFile(hostAppComponents); }

                    if (navigator != null)
                    {
                        // detach static event
                        Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
                    }
                    // NOTE: IMPORTANT!
                    if (navigator != null) { navigator.Close(); navigator = null; }

                    zkooTutorialModel = null;
                };

                base.Exit += delegate
                {
                    if (hostAppComponents != null) { hostAppComponents.Dispose(); hostAppComponents = null; }
                    DuplicatedProcessStartBlocking.ReleaseMutex();
                };

                hostAppComponents.CheckIfDeviceFirmwareIsLatestOrNotAndExitApplicationIfFailed();
                // NOTE: If users exit the application by the button on Camera View while "Firmware Update" dialog, exception occurs.
                // MUSTDO: We will fix this.
                if (hostAppComponents.SettingsWindow == null) { return; }


                // NOTE: Codes about "Tutorial" application.
                // TODO: Tutorial application should be independent from this host application, so I wrote it as an event handler.
                var hasZkooTutorialLaunched = false;
                var zkooTutorialLaunchAction = new Action(() =>
                {
                    if (hasZkooTutorialLaunched == false)
                    {
                        zkooTutorialModel = new ZkooTutorialModel(hostAppComponents);
                        navigator = new MainNavigationWindow();
                        // attach static event
                        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
                        // NOTE: TODO: When exceptions occur in Initialize, we have to distinguish whether from Settings files or from source code.
                        zkooTutorialModel.TutorialAppHeaderMenu.InitializeOnceAtStartup(navigator, zkooTutorialModel);
                        zkooTutorialModel.InitializeOnceAtStartup(hostAppComponents);
                        navigator.InitializeOnceAtStartup(zkooTutorialModel);
                        hasZkooTutorialLaunched = true;
                    }
                    navigator.StartTutorial();
                });

                hostAppComponents.SettingsWindow.SettingsUserControl.TutorialAppSettingsGroupBoxVisibility = Visibility.Visible;
                hostAppComponents.StartTutorialCommand.PerformEventHandler += delegate
                {
                    zkooTutorialLaunchAction.Invoke();
                };

                hostAppComponents.IsStartingDeviceFirmwareUpdate += delegate { if (navigator != null) { navigator.ExitTutorial(); } };
                hostAppComponents.IsStartingHostApplicationUpdate += delegate { if (navigator != null) { navigator.ExitTutorial(); } };


                if (ApplicationCommonSettings.IsInternalRelease || ApplicationCommonSettings.IsDebuggingInternal)
                {
                    var exvisionInternalSettingsTabItem = new System.Windows.Controls.TabItem() { Header = "Exvision" };
                    exvisionInternalSettingsTabItem.Content = new Egs.Views.ExvisionSettingsUserControl();
                    hostAppComponents.SettingsWindow.SettingsUserControl.SettingsTabControl.Items.Add(exvisionInternalSettingsTabItem);
                }


                if (hostAppComponents.IsToStartTutorialWhenHostApplicationStart)
                {
                    zkooTutorialLaunchAction.Invoke();
                }
            }
            catch (Exception ex)
            {
                ShutdownApplicationByException(ex);
            }
        }

        void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            if (navigator != null) { navigator.OnDisplaySettingsChanged(sender, e); }
            if (hostAppComponents != null && hostAppComponents.CameraViewWindowModel != null)
            {
                hostAppComponents.CameraViewWindowModel.ResetLocationAndSizeIfNotInsideAnyScreen();
            }
        }

        void ShutdownApplicationByException(Exception ex)
        {
            if (ex is EgsHostApplicationIsClosingException)
            {
                // NOTE: Assuming that this is the correct way to shutdown the application.
                MessageBox.Show(Egs.EgsDeviceControlCore.Properties.Resources.CommonStrings_ApplicationWillExit, ApplicationCommonSettings.HostApplicationName, MessageBoxButton.OK);
                if (hostAppComponents != null) { hostAppComponents.Dispose(); hostAppComponents = null; }
            }
            else
            {
                // NOTE: This is not handled exceptions.  At first it saves safer settings, and then it shows "we're sorry" window.
                try
                {
                    // NOTE: But in some cases, application is already shutdown, so this code itself can occur exceptions
                    var window = new NotHandledExceptionReportWindow();
                    window.Initialize(ex);
                    window.ShowDialog();
                }
                catch (Exception ex2)
                {
                    if (ApplicationCommonSettings.IsDebugging) { Debugger.Break(); }
                    MessageBox.Show(ex2.Message);
                }
            }
            DuplicatedProcessStartBlocking.ReleaseMutex();
        }
    }
}
