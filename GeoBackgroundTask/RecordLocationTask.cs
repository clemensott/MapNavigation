using GeoCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Geolocation;
using Windows.Storage;

namespace GeoBackgroundTask
{
    public sealed class RecordLocationTask : IBackgroundTask
    {
        private static StorageFile currentValuesFile;

        private BackgroundTaskDeferral deferral;
        private Mutex endMutex, updateMutex;
        private bool ended;

        private Geolocator geolocator;
        private List<GeoMeasure> measures;
        private double currentDist;
        private double currentSpeed;

        private object updateValueLockObj;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            return;
            deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += OnCanceled;
            taskInstance.Task.Completed += TaskCompleted;

            endMutex = new Mutex(false, GeoUtils.EndMutixName);
            updateMutex = new Mutex(true, GeoUtils.UpdateMutexName);
            updateMutex.WaitOne();

            ApplicationTriggerDetails triggerDetails = taskInstance.TriggerDetails as ApplicationTriggerDetails;

            GeolocationAccessStatus accessStatus = await Geolocator.RequestAccessAsync();

            System.Diagnostics.Debug.WriteLine("geoAccessStatus: " + accessStatus);

            if (accessStatus != GeolocationAccessStatus.Allowed)
            {
                Complete();
                return;
            }

            updateValueLockObj = new object();
            measures = new List<GeoMeasure>();
            geolocator = new Geolocator()
            {
                DesiredAccuracy = PositionAccuracy.High,
                ReportInterval = 1000
            };

            geolocator.StatusChanged += Geolocator_StatusChanged;
            //geolocator.PositionChanged += Geolocator_PositionChanged;

            UpdateMeasures(await GetCurrentValuesFile());
        }

        private void Geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Geolocator_StatusChanged: " + args.Status);

            lock (updateValueLockObj) Monitor.Pulse(updateValueLockObj);
        }

        private void Geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("PositionChanged1: " + measures.Count);

            AddPosition(args.Position);
        }

        private void AddPosition(Geoposition geoposition)
        {
            GeoMeasure measure = geoposition.Coordinate.ToGeoMeasure();

            if (measures.Count == 0)
            {
                currentDist = 0;
            }
            else
            {
                currentDist += GeoUtils.DistanceBetweenPlaces(measures[measures.Count - 1].Position, measure.Position);
            }

            if (measure.Speed.HasValue) currentSpeed = measure.Speed.Value;
            else if (measures.Count > 0)
            {
                GeoMeasure last = measures[measures.Count - 1];
                double dist = GeoUtils.DistanceBetweenPlaces(last.Position, measure.Position);

                currentSpeed = dist / (measure.Timestamp - last.Timestamp).TotalSeconds;
            }

            System.Diagnostics.Debug.WriteLine("PositionChanged2");

            System.Diagnostics.Debug.WriteLine("Dist: " + currentDist);
            System.Diagnostics.Debug.WriteLine("Speed: " + currentSpeed);

            System.Diagnostics.Debug.WriteLine("PositionChanged3");
            lock (updateValueLockObj)
            {
                measures.Add(measure);
                Monitor.Pulse(updateValueLockObj);
            }
            System.Diagnostics.Debug.WriteLine("PositionChanged4");
        }

        private void UpdateMeasures(StorageFile currentValuesFile)
        {
            lock (updateValueLockObj)
            {
                while (geolocator.LocationStatus != PositionStatus.Ready) Monitor.Wait(updateValueLockObj);
            }

            int lastCount = 0;
            Task endedTask = WaitForEnd();

            while (!endedTask.IsCompleted)
            {
                System.Diagnostics.Debug.WriteLine("UpdateMeasures1");
                Task<Geoposition> geoTask = geolocator.GetGeopositionAsync().AsTask();
                geoTask.Wait();

                System.Diagnostics.Debug.WriteLine("UpdateMeasures1.5");
                AddPosition(geoTask.Result);
                //lock (updateValueLockObj)
                //{
                //    while (lastCount == measures.Count) Monitor.Wait(updateValueLockObj);
                //}

                //lastCount = measures.Count;

                System.Diagnostics.Debug.WriteLine("UpdateMeasures2");
                GeoMeasure last = measures[measures.Count - 1];
                TimeSpan duration = measures.Count > 0 ? last.Timestamp - measures[0].Timestamp : TimeSpan.Zero;
                object[] lines = new object[]
                {
                    duration.Ticks,
                    currentDist,
                    currentSpeed,
                    last.Accuracy,
                    last.AltitudeAccuracy,
                    last.Heading,
                    last.Speed,
                    last.Timestamp,
                    last.Position.Altitude,
                    last.Position.Latitude,
                    last.Position.Longitude,
                };
                System.Diagnostics.Debug.WriteLine("UpdateMeasures3");

                FileIO.WriteLinesAsync(currentValuesFile, lines.Select(l => l?.ToString() ?? "")).AsTask().Wait();
                System.Diagnostics.Debug.WriteLine("UpdateMeasures4");

                updateMutex.ReleaseMutex();
                System.Diagnostics.Debug.WriteLine("UpdateMeasures5");
                updateMutex.WaitOne();
                System.Diagnostics.Debug.WriteLine("UpdateMeasures6");
            }

            Complete();
        }

        private async Task<StorageFile> GetCurrentValuesFile()
        {
            if (currentValuesFile == null)
            {
                currentValuesFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("currentValues", CreationCollisionOption.OpenIfExists);
            }

            return currentValuesFile;
        }

        private Task WaitForEnd()
        {
            return Task.Run(() =>
            {
                endMutex.WaitOne();
                ended = true;
            });
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            Complete();
        }

        private void TaskCompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            Complete();
        }

        private void Complete()
        {
            System.Diagnostics.Debug.WriteLine("Complete");
            updateMutex.ReleaseMutex();
            deferral.Complete();
        }
    }
}
