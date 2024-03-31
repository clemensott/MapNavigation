using GeoCommon;
using StdOttStandard.Linq;
using StdOttUwp.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Geolocation;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x407 dokumentiert.

namespace MapNavigation
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ViewModel viewModel;
        private ComplexMapBinding mapBinding;

        private Mutex endMutex, updateMutex;
        private bool record;

        private Geolocator geolocator;
        private Geopoint holdingLocation;

        public MainPage()
        {
            this.InitializeComponent();

            //endMutex = new Mutex(true, GeoUtils.EndMutixName);
            //updateMutex = new Mutex(false, GeoUtils.UpdateMutexName);

            //IsValueToTwoValueConverter rotationCon = (IsValueToTwoValueConverter)Resources["rotationCon"];
            //rotationCon.EqualsValue = MapInteractionMode.Auto;
            //rotationCon.NotEqualsValue = MapInteractionMode.Disabled;


            IsValueToTwoValueConverter destCon = (IsValueToTwoValueConverter)Resources["destCon"];
            destCon.CompareValue = DirectionLineDestType.Custom;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = viewModel = await ViewModel.GetInstanceAsync();
            mapBinding = new ComplexMapBinding(map, viewModel);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            mapBinding.Dispose();
        }

        private async void StartGeolocating()
        {
            if (geolocator == null)
            {
                GeolocationAccessStatus accessStatus = await Geolocator.RequestAccessAsync();

                switch (accessStatus)
                {
                    case GeolocationAccessStatus.Allowed:
                        // If DesiredAccuracy or DesiredAccuracyInMeters are not set (or value is 0), DesiredAccuracy.Default is used.
                        geolocator = new Geolocator
                        {
                        };

                        break;

                    default:
                        await new MessageDialog("GeolocationAccessStatus: " + accessStatus).ShowAsync();
                        return;
                }
            }

            viewModel.FocusGeoposition = true;

            if (gidGeoIndicator.Visibility == Visibility.Visible) return;
            gidGeoIndicator.Visibility = Visibility.Visible;

            while (true)
            {
                Geoposition pos = await geolocator.GetGeopositionAsync();

                viewModel.LatestPosition = pos.Coordinate.Point.Position;

                if (viewModel.FocusGeoposition)
                {
                    viewModel.CurrentPosition = pos.Coordinate.Point.Position;

                    await map.TrySetViewAsync(pos.Coordinate.Point);
                }
                else break;
            }

            gidGeoIndicator.Visibility = Visibility.Collapsed;
        }

        private void AbbRemoveLastPoint_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.AddingMode && viewModel.CurrentRoute != null)
            {
                BasicGeoposition[] path = viewModel.CurrentRoute.Path;
                viewModel.CurrentRoute.Path = path.Take(path.Length - 1).ToArray();
            }
        }

        private void AbbPreviousDestPoint_Click(object sender, RoutedEventArgs e)
        {
            BasicGeoposition[] points = viewModel.CurrentRoute?.Path;

            if (points == null || points.Length == 0) return;

            try
            {
                viewModel.DirectionLineDestPoint = viewModel.CurrentRoute.Path.Previous(viewModel.DirectionLineDestPoint).previous;
            }
            catch
            {
                viewModel.DirectionLineDestPoint = points[0];
            }
        }

        private void AbbNextDestPoint_Click(object sender, RoutedEventArgs e)
        {
            BasicGeoposition[] points = viewModel.CurrentRoute?.Path;

            if (points == null || points.Length == 0) return;

            try
            {
                viewModel.DirectionLineDestPoint = viewModel.CurrentRoute.Path.Next(viewModel.DirectionLineDestPoint).next;
            }
            catch
            {
                viewModel.DirectionLineDestPoint = points[0];
            }
        }

        private void AbbDirectionLineSrc_Click(object sender, RoutedEventArgs e)
        {
            switch (viewModel.DirectionLineSrc)
            {
                case DirectionLineSourceType.CurrentPosition:
                    viewModel.DirectionLineSrc = DirectionLineSourceType.LatestPosition;
                    break;

                case DirectionLineSourceType.LatestPosition:
                    viewModel.DirectionLineSrc = DirectionLineSourceType.Disable;
                    break;

                default:
                    viewModel.DirectionLineSrc = DirectionLineSourceType.CurrentPosition;
                    break;
            }
        }

        private void AbbDirectionLineDest_Click(object sender, RoutedEventArgs e)
        {
            switch (viewModel.DirectionLineDest)
            {
                case DirectionLineDestType.Nearest:
                    viewModel.DirectionLineDest = DirectionLineDestType.Custom;
                    break;

                default:
                    viewModel.DirectionLineDest = DirectionLineDestType.Nearest;
                    break;

            }
        }

        private void AtbCurrentPosition_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.FocusGeoposition) viewModel.FocusGeoposition = false;
            else StartGeolocating();
        }

        private async void AbbResetRotation_Click(object sender, RoutedEventArgs e)
        {
            await map.TryRotateToAsync(0);
        }

        private void AbbSettings_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage), viewModel);
        }

        private void Map_MapTapped(MapControl sender, MapInputEventArgs args)
        {
            if (viewModel.AddingMode && viewModel.CurrentRoute != null)
            {
                viewModel.CurrentRoute.Path = viewModel.CurrentRoute.Path.ConcatParams(args.Location.Position).ToArray();
            }
        }

        private void Map_MapHolding(MapControl sender, MapInputEventArgs args)
        {
            holdingLocation = args.Location;
            mfyMap.ShowAt(sender, args.Position);
        }

        private void MfiAddEndPoint_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CurrentRoute.Path = viewModel.CurrentRoute.Path.ConcatParams(holdingLocation.Position).ToArray();
        }

        private void MfiAddBeginPoint_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CurrentRoute.Path = viewModel.CurrentRoute.Path.Insert(0, holdingLocation.Position).ToArray();
        }

        private void MfiInsertAfterPoint_Click(object sender, RoutedEventArgs e)
        {
            int currentIndex = 0, insertIndex = 0;
            double lastDist, minDist = double.MaxValue;
            BasicGeoposition pos = holdingLocation.Position;

            foreach (BasicGeoposition curPos in viewModel.CurrentRoute.Path)
            {
                double dist = GeoUtils.DistanceBetweenPlaces(curPos, pos);

                if (dist < minDist)
                {
                    minDist = dist;
                    insertIndex = currentIndex + 1;
                }

                lastDist = dist;
                currentIndex++;
            }

            viewModel.CurrentRoute.Path = viewModel.CurrentRoute.Path.Insert(insertIndex, pos).ToArray();
        }

        private void MfiInsertBeforePoint_Click(object sender, RoutedEventArgs e)
        {
            int currentIndex = 0, insertIndex = 0;
            double lastDist, minDist = double.MaxValue;
            BasicGeoposition pos = holdingLocation.Position;

            foreach (BasicGeoposition curPos in viewModel.CurrentRoute.Path)
            {
                double dist = GeoUtils.DistanceBetweenPlaces(curPos, pos);

                if (dist < minDist)
                {
                    minDist = dist;
                    insertIndex = currentIndex;
                }

                lastDist = dist;
                currentIndex++;
            }

            viewModel.CurrentRoute.Path = viewModel.CurrentRoute.Path.Insert(insertIndex, pos).ToArray();
        }

        private void MfiRemovePoint_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.CurrentRoute.Path.Length == 0) return;

            BasicGeoposition pos = holdingLocation.Position;

            pos = viewModel.CurrentRoute.Path.MinElement(p => GeoUtils.DistanceBetweenPlaces(p, pos));

            viewModel.CurrentRoute.Path = viewModel.CurrentRoute.Path.Where(p => !Equals(p, pos)).ToArray();
        }

        private async Task RegistorBackgroundTask()
        {
            endMutex.WaitOne();

            var appTrigger = new ApplicationTrigger();

            var requestStatus = await BackgroundExecutionManager.RequestAccessAsync();

            System.Diagnostics.Debug.WriteLine("requestStatus: " + requestStatus);

            //if (requestStatus != BackgroundAccessStatus.AlwaysAllowed)
            //{
            //    return;
            //}

            string entryPoint = "GeoBackgroundTask.RecordLocationTask";
            entryPoint = typeof(GeoBackgroundTask.RecordLocationTask).ToString();
            string taskName = "Example application trigger";

            IBackgroundTaskRegistration task;
            if (!BackgroundTaskRegistration.AllTasks.Select(p => p.Value).TryFirst(t => t.Name == taskName, out task))
            {
                System.Diagnostics.Debug.WriteLine("BackgroundTask does not exist");
                var builder = new BackgroundTaskBuilder()
                {
                    TaskEntryPoint = entryPoint,
                    Name = taskName
                };

                builder.SetTrigger(appTrigger);

                task = builder.Register();

            }

            ApplicationTriggerResult result = await appTrigger.RequestAsync();

            System.Diagnostics.Debug.WriteLine("RequestAsync: " + result);

            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("currentValues", CreationCollisionOption.OpenIfExists);
            await Task.Run(() => ReadUpdates(file));

            endMutex.ReleaseMutex();
        }

        private void ReadUpdates(StorageFile file)
        {
            record = true;


            while (record)
            {
                Task.Delay(100).Wait();

                System.Diagnostics.Debug.WriteLine("ReadUpdates1");
                updateMutex.WaitOne();
                System.Diagnostics.Debug.WriteLine("ReadUpdates2");

                Task<IList<string>> linesTask = FileIO.ReadLinesAsync(file).AsTask();
                linesTask.Wait();
                string[] lines = linesTask.Result.ToArray();
                System.Diagnostics.Debug.WriteLine("ReadUpdates3");

                updateMutex.ReleaseMutex();
                System.Diagnostics.Debug.WriteLine("ReadUpdates4");

                long ticks;
                double currentDist, currentSpeed;

                if (lines.Length < 3 || !long.TryParse(lines[0], out ticks) || !double.TryParse(lines[1], out currentDist) || !double.TryParse(lines[2], out currentSpeed)) continue;
                System.Diagnostics.Debug.WriteLine("ReadUpdates5");

                TimeSpan duration = new TimeSpan(ticks);

                Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    tblDist.Text = currentDist.ToString();
                    tblDuration.Text = duration.TotalMinutes.ToString();
                    tblAvgSpeed.Text = (currentDist / duration.TotalSeconds).ToString();
                    tblCurrentSpeed.Text = currentDist.ToString();
                    tblRaw.Text = string.Join("\r\n", lines);
                }).AsTask().Wait();
                System.Diagnostics.Debug.WriteLine("ReadUpdates6");
            }

        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await RegistorBackgroundTask();
            }
            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine(exc);
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            record = false;
        }
    }
}
