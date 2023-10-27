using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Devices.Lights;
using Windows.Devices.Enumeration;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI;
using System.Numerics;
using Windows.Devices.Lights.Effects;
using Windows.Foundation.Metadata;
using System.Reflection;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CSharp_Uwp_LampArray
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        class MetaLampInfo
        {
            public string _mId;
            public string _mName;
            public int _mLampCount;
            public List<int> _mIndexes;
            public LampArrayCustomEffect _mEffect;
        };

        private Dictionary<string, MetaLampInfo> _mConnectedDevices =
            new Dictionary<string, MetaLampInfo>();

        private Color _mColorClear;
        private Color _mColorRed;
        private Color _mColorWhite;

        private DeviceWatcher _mDeviceWatcher;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            #region Check if DL is available

            if (ApiInformation.IsPropertyPresent("Windows.Devices.Lights.LampArray", "IsAvailable"))
            {
                try
                {
                    Type lampArrayType = typeof(LampArray);
                    if (lampArrayType != null)
                    {
                        // Get the property info
                        PropertyInfo isAvailableProperty = lampArrayType.GetProperty("IsAvailable");

                        if (isAvailableProperty == null)
                        {
                            _mLblAvailable.Text = "NO";
                        }
                        else
                        {
                            // Invoke the property (assuming it's a bool property)
                            bool isAvailable = (bool)isAvailableProperty.GetValue(null, null);

                            // Now you can use the value of the property
                            Console.WriteLine("IsAvailable: " + isAvailable);
                            if (isAvailable)
                            {
                                _mLblAvailable.Text = "YES";
                            }
                            else
                            {
                                _mLblAvailable.Text = "NO";
                            }
                        }
                    }
                }
                catch
                {
                    _mLblAvailable.Text = "EXCEPTION, NO";
                }
            }
            else
            {
                _mLblAvailable.Text = "NO";
            }

            #endregion

            #region Get Device Selector

            string deviceSelector = LampArray.GetDeviceSelector();

            #endregion Get Device Selector


            #region Create Device Watcher

            _mDeviceWatcher = DeviceInformation.CreateWatcher(deviceSelector);

            #endregion Create Device Watcher


            #region Assign Device Watcher Added Event

            _mDeviceWatcher.Added += Watcher_Added;

            #endregion Assign Device Watcher Added Event


            #region Assign Device Watcher Removed Event

            _mDeviceWatcher.Removed += Watcher_Removed;

            #endregion Assign Device Watcher Removed Event


            LogInfo("Detect connected devices...\n");


            #region Start Device Watcher

            _mDeviceWatcher.Start();
            LogInfo("WinRT Started DeviceWatcher!\n");

            #endregion Start Device Watcher


            // wait to detect connected devices
            await Task.Delay(100);
            LogInfo("Waited for devices.\n");

            SetAllDevicesToColors();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private async void LogInfo(string msg, params object[] values)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                string message = "Info: " + string.Format(msg, values);
                _mTxtDebug.Text += message;
            });

        }
        private async void LogError(string msg, params object[] values)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                string message = "Error: " + string.Format(msg, values);
                _mTxtDebug.Text += message;
            });
        }

        private void Watcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            MetaLampInfo meta = new MetaLampInfo();
            string id = args.Id;
            meta._mId = id;
            meta._mName = args.Name;
            _mConnectedDevices[id] = meta;
            LogInfo("Added device: name={0} id={1}\n", meta._mName, meta._mId);
        }

        private void Watcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            string id = args.Id;
            if (_mConnectedDevices.ContainsKey(id))
            {
                MetaLampInfo meta = _mConnectedDevices[id];
                _mConnectedDevices.Remove(id);
                LogInfo("Removed device: {0}\n", meta._mName);
            }
        }

        private async void SetAllDevicesToColors()
        {

            #region Create colors

            _mColorClear = Color.FromArgb(0, 0, 0, 0);
            _mColorRed = Color.FromArgb(255, 255, 0, 0);
            _mColorWhite = Color.FromArgb(255, 255, 255, 255);

            #endregion Create colors


            // loop through all the connected lighting devices
            int lampArrayIndex = 0;
            foreach (KeyValuePair<string, MetaLampInfo> kvp in _mConnectedDevices)
            {
                string id = kvp.Key;

                MetaLampInfo meta = kvp.Value;

                LogInfo("Device {0} id={1} name={2}...\n", lampArrayIndex, id, meta._mName);


                #region Get DeviceInformation

                DeviceInformation deviceInformation = await DeviceInformation.CreateFromIdAsync(id);
                if (null == deviceInformation)
                {
                    continue;
                }

                #endregion Get DeviceInformation


                #region Get LampArray

                LampArray lampArray = await LampArray.FromIdAsync(id);

                if (null == lampArray)
                {
                    continue;
                }

                #endregion Get LampArray


                #region Get lamp count

                meta._mLampCount = lampArray.LampCount;

                #endregion Get lamp count


                #region Get min update interval

                TimeSpan minUpdateInterval = lampArray.MinUpdateInterval;

                #endregion Get min update interval


                #region Get connected

                bool isConnected = lampArray.IsConnected;

                #endregion Get connected


                #region Get enabled

                bool isEnabled = lampArray.IsEnabled;

                #endregion Get enabled


                LogInfo("Added device: name={0} isEnabled={1} isConnected={2} lampCount={3} id={4}\n",
                    meta._mName,
                    isEnabled ? "true" : "false",
                    isConnected ? "true" : "false",
                    meta._mLampCount,
                    id);


                #region Get lamp info positions

                for (int lamp = 0; lamp < meta._mLampCount; ++lamp)
                {
                    LampInfo lampInfo = lampArray.GetLampInfo(lamp);
                    Vector3 vector3 = lampInfo.Position;
                    //LogInfo("Lamp {0}: position x={1} y={2} z={3}\n", lamp, vector3.X, vector3.Y, vector3.Z);
                }

                #endregion Get lamp info positions


                #region Prepare lamp indices

                meta._mIndexes = new List<int>(Enumerable.Range(0, meta._mLampCount));

                #endregion Prepare lamp indices


                #region Show color on the device

                lampArray.SetColor(_mColorClear);

                lampArray.SetSingleColorForIndices(_mColorWhite, meta._mIndexes.ToArray<int>());

                #endregion Show color on the device


                #region Create Playlist

                LampArrayEffectPlaylist lampArrayEffectPlaylist = new LampArrayEffectPlaylist();

                #endregion Create Playlist


                #region Set Playlist start mode

                lampArrayEffectPlaylist.EffectStartMode = LampArrayEffectStartMode.Simultaneous;

                #endregion Set Playlist start mode


                #region Create Custom Effect

                meta._mEffect = new LampArrayCustomEffect(lampArray, meta._mIndexes.ToArray());

                #endregion Create Custom Effect


                #region Set Effect Duration

                meta._mEffect.Duration = TimeSpan.MaxValue;

                #endregion Set Effect Duration


                #region Set Effect Update Interval

                //meta._mEffect.UpdateInterval = minUpdateInterval;
                meta._mEffect.UpdateInterval = TimeSpan.FromSeconds(1); // slower for debugging

                #endregion Set Effect Update Interval


                #region Assign UpdateRequested event

                meta._mEffect.UpdateRequested += CustomEffect_FrameRequested;

                #endregion Assign UpdateRequested event


                #region Append to playlist

                lampArrayEffectPlaylist.Append(meta._mEffect);

                #endregion Append to playlist


                #region Start playlist

                lampArrayEffectPlaylist.Start();

                #endregion Start playlist


                ++lampArrayIndex;
            }
        }

        private void CustomEffect_FrameRequested(LampArrayCustomEffect sender, LampArrayUpdateRequestedEventArgs args)
        {
            int lampArrayIndex = 0;
            foreach (KeyValuePair<string, MetaLampInfo> kvp in _mConnectedDevices)
            {
                MetaLampInfo meta = kvp.Value;

                if (meta._mEffect == sender)
                {
                    args.SetColor(_mColorRed);

                    for (int lamp = 0; lamp < meta._mLampCount; ++lamp)
                    {
                        args.SetColorForIndex(lamp, _mColorRed);
                        if (lamp == 0) // log just the first element per device
                        {
                            LogInfo("Device {0}: set red color: name={1} lamp={2} of {3}\n", lampArrayIndex, meta._mName, lamp, meta._mLampCount);
                        }
                    }
                }

                ++lampArrayIndex;
            }
        }
    }
}
