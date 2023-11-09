using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WIFIConnection
{
    public enum ScanResaultType
    {
        WiFi_manager_is_not_available,
        WiFi_is_not_enabled,
        Network_with_filter_not_found,
        Success
    }

    public enum ConnectType
    {
        Success,
        Failure
    }

    public class WiFiManager
    {
        private AndroidJavaObject wifiManager;
        private AndroidJavaObject connectivityManager;
        private AndroidJavaObject currentActivity;


        public WiFiManager()
        {
            Input.location.Start();
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaClass wifiManagerClass = new AndroidJavaClass("android.net.wifi.WifiManager");
            wifiManager = currentActivity.Call<AndroidJavaObject>("getSystemService", "wifi");

            // Get the ConnectivityManager service
            AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
            connectivityManager = context.Call<AndroidJavaObject>("getSystemService", "connectivity");

            // Get the ConnectivityManager instance
            // connectivityManager = connectivityService.Call<AndroidJavaObject>("getSystemService", "connectivity");

        }

        public void OpenWiFiSettings()
        {
            /*AndroidJavaClass settingsPanel = new AndroidJavaClass("android.provider.Settings.Panel");
            settingsPanel.CallStatic<string>("ACTION_INTERNET_CONNECTIVITY");*/

            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent");
            intent.Call<AndroidJavaObject>("setAction", "android.settings.WIFI_SETTINGS");

            currentActivity.Call("startActivity", intent);
        }

        /// <summary>
        /// Try find networks containted "filter" in name
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="ssids"></param>
        /// <returns></returns>
        public ScanResaultType TryGetWifiWithFilter(string filter, out List<string> ssids)
        {
            ScanResaultType scanResaultType = TryScanWiFiNetworks(out List<string> allSsids);
            if (scanResaultType == ScanResaultType.Success)
            {
                ssids = new List<string>();
                foreach (string ssid in allSsids)
                {
                    if (ssid.Contains(filter))
                        ssids.Add(ssid);
                }
                if (ssids.Count > 0)
                {
                    return ScanResaultType.Success;
                }
                else
                {
                    return ScanResaultType.Network_with_filter_not_found;
                }
            }
            else
            {
                ssids = null;
                return scanResaultType;
            }
        }

        /// <summary>
        /// Call For Scan Wi-Fi
        /// </summary>
        public ScanResaultType TryScanWiFiNetworks(out List<string> ssids)
        {
            if (wifiManager == null)
            {
                Debug.LogError("WiFi manager is not available");
                ssids = null;
                return ScanResaultType.WiFi_manager_is_not_available;
            }

            if (!wifiManager.Call<bool>("isWifiEnabled"))
            {
                Debug.LogWarning("Wi-Fi is not enabled");
                ssids = null;
                OpenWiFiSettings();
                return ScanResaultType.WiFi_is_not_enabled;
            }

            AndroidJavaObject wifiScanResultList = wifiManager.Call<AndroidJavaObject>("getScanResults");
            int numResults = wifiScanResultList.Call<int>("size");

            List<string> scannedNetworks = new List<string>();

            for (int i = 0; i < numResults; i++)
            {
                AndroidJavaObject wifiScanResult = wifiScanResultList.Call<AndroidJavaObject>("get", i);
                string ssid = wifiScanResult.Get<string>("SSID");
                //string ssid = WifiSsid.Get<string>("toString");
                scannedNetworks.Add(ssid);
            }

            foreach (string network in scannedNetworks)
            {
                Debug.Log("Найденная сеть: " + network);
            }
            ssids = scannedNetworks;

            return ScanResaultType.Success;
        }


        /// <summary>
        /// Returned current wifi network
        /// </summary>
        /// <param name="ssid"></param>
        /// <returns></returns>
        public ScanResaultType GetCurrentConnection(out string ssid)
        {
            if (wifiManager == null)
            {
                Debug.LogError("WiFi manager is not available");
                ssid = null;
                return ScanResaultType.WiFi_manager_is_not_available;
            }

            if (!wifiManager.Call<bool>("isWifiEnabled"))
            {
                Debug.LogWarning("Wi-Fi is not enabled");
                ssid = null;
                OpenWiFiSettings();
                return ScanResaultType.WiFi_is_not_enabled;
            }

            ssid = "";
            AndroidJavaObject connectionInfo = wifiManager.Call<AndroidJavaObject>("getConnectionInfo");
            if (connectionInfo != null)
            {
                ssid = connectionInfo.Call<string>("getSSID").Replace("\"", "");
            }


            return ScanResaultType.Success;
        }

        /// <summary>
        /// Remove auto reconect for current wifi network
        /// </summary>
        public void RemoveSuggestionCurrentWifi()
        {
            GetCurrentConnection(out string ssid);

            if (ssid == "<unknown ssid>" || string.IsNullOrEmpty(ssid))
                return;

            AndroidJavaObject wifiNetworkSuggestion1 = new AndroidJavaObject("android.net.wifi.WifiNetworkSuggestion$Builder")
                .Call<AndroidJavaObject>("setSsid", ssid)
                .Call<AndroidJavaObject>("setIsAppInteractionRequired", true)
                .Call<AndroidJavaObject>("build");

            AndroidJavaObject wifiNetworkSuggestionList = new AndroidJavaObject("java.util.ArrayList");
            wifiNetworkSuggestionList.Call<bool>("add", wifiNetworkSuggestion1);

            int status = wifiManager.Call<int>("removeNetworkSuggestions", wifiNetworkSuggestionList);//, wifiManager.GetStatic<int>("ACTION_REMOVE_SUGGESTION_DISCONNECT"));
        }

        /// <summary>
        /// API level < 29
        /// </summary>
        /// <param name="ssid"></param>
        /// <param name="password"></param>
        public void ConnectToWiFiOld(string ssid, string password)
        {
            if (wifiManager == null)
            {
                Debug.LogError("WiFi manager is not available");
                return;
            }

            if (!wifiManager.Call<bool>("isWifiEnabled"))
            {
                Debug.LogWarning("Wi-Fi is not enabled");
                return;
            }

            AndroidJavaObject wifiConfig = new AndroidJavaObject("android.net.wifi.WifiConfiguration");
            wifiConfig.Set<string>("SSID", "\"" + ssid + "\"");
            wifiConfig.Set<string>("preSharedKey", "\"" + password + "\"");

            wifiManager.Call<bool>("disconnect");
            int networkId = wifiManager.Call<int>("addNetwork", wifiConfig);

            wifiManager.Call<bool>("enableNetwork", networkId, true);
            wifiManager.Call<bool>("reconnect");
            Debug.Log("Connecting to Wi-Fi network: " + ssid);

        }

        /// <summary>
        /// API level > 29 Suggestion API connect. Add auto reconnect to wifi network
        /// </summary>
        /// <param name="ssid"></param>
        /// <param name="password"></param>
        public ConnectType AddSuggestionWiFi(string ssid, string password)
        {

            RemoveSuggestionCurrentWifi();

            AndroidJavaObject wifiNetworkSuggestion1 = new AndroidJavaObject("android.net.wifi.WifiNetworkSuggestion$Builder")
                .Call<AndroidJavaObject>("setSsid", ssid)
                .Call<AndroidJavaObject>("setPriority", 1)
                .Call<AndroidJavaObject>("setIsAppInteractionRequired", true)
                .Call<AndroidJavaObject>("build");
            AndroidJavaObject wifiNetworkSuggestion2 = new AndroidJavaObject("android.net.wifi.WifiNetworkSuggestion$Builder")
                .Call<AndroidJavaObject>("setSsid", ssid)
                .Call<AndroidJavaObject>("setPriority", 1)
                .Call<AndroidJavaObject>("setWpa2Passphrase", password)
                .Call<AndroidJavaObject>("setIsAppInteractionRequired", true)
                .Call<AndroidJavaObject>("build");
            AndroidJavaObject wifiNetworkSuggestion3 = new AndroidJavaObject("android.net.wifi.WifiNetworkSuggestion$Builder")
                .Call<AndroidJavaObject>("setSsid", ssid)
                .Call<AndroidJavaObject>("setPriority", 1)
                .Call<AndroidJavaObject>("setWpa3Passphrase", password)
                .Call<AndroidJavaObject>("setIsAppInteractionRequired", true)
                .Call<AndroidJavaObject>("build");

            AndroidJavaObject wifiNetworkSuggestionList = new AndroidJavaObject("java.util.ArrayList");
            wifiNetworkSuggestionList.Call<bool>("add", wifiNetworkSuggestion1);
            wifiNetworkSuggestionList.Call<bool>("add", wifiNetworkSuggestion2);
            wifiNetworkSuggestionList.Call<bool>("add", wifiNetworkSuggestion3);

            int statusRemove = wifiManager.Call<int>("removeNetworkSuggestions", wifiNetworkSuggestionList);
            int status = wifiManager.Call<int>("addNetworkSuggestions", wifiNetworkSuggestionList);

            if (status != wifiManager.GetStatic<int>("STATUS_NETWORK_SUGGESTIONS_SUCCESS"))
            {
                return ConnectType.Failure;
            }
            else
            {
                return ConnectType.Success;
            }

        }

        /// <summary>
        /// API level > 29 Request API connect
        /// </summary>
        /// <param name="ssid"></param>
        /// <param name="password"></param>
        public ConnectType ConnectToWiFi(string ssid, string password)
        {
            // Disconnect from the current network
            // Get the class reference for android.os.Build.VERSION
            IntPtr versionClass = AndroidJNI.FindClass("android/os/Build$VERSION");

            // Get the SDK_INT field ID
            IntPtr sdkIntField = AndroidJNI.GetStaticFieldID(versionClass, "SDK_INT", "I");

            // Get the value of Android API level
            int apiLevel = AndroidJNI.GetStaticIntField(versionClass, sdkIntField);

            // Clean up the references
            AndroidJNI.DeleteLocalRef(versionClass);
            if (apiLevel >= 29)
            {
                AndroidJavaObject currentNetwork = connectivityManager.Call<AndroidJavaObject>("getActiveNetwork");
                bool status = connectivityManager.Call<bool>("bindProcessToNetwork", currentNetwork);
            }
            else
            {
                bool status = wifiManager.Call<bool>("disconnect");
            }

            AddSuggestionWiFi(ssid, password);

            AndroidJavaObject specifier = CreateWifiConfig(ssid, password);
            AndroidJavaObject networkCapabilities = new AndroidJavaObject("android.net.NetworkCapabilities");
            AndroidJavaObject request = new AndroidJavaObject("android.net.NetworkRequest$Builder")
                .Call<AndroidJavaObject>("addTransportType", networkCapabilities.GetStatic<int>("TRANSPORT_WIFI"))
                //.Call<AndroidJavaObject>("removeCapability", networkCapabilities.GetStatic<int>("NET_CAPABILITY_INTERNET"))
                .Call<AndroidJavaObject>("setNetworkSpecifier", specifier)
                .Call<AndroidJavaObject>("build");



            // AndroidJavaObject networkCallback = new AndroidJavaObject("android.net.ConnectivityManager$NetworkCallback");

            // Register a network callback to listen for network changes
            AndroidJavaClass networkCallbackClass = new AndroidJavaClass("android.net.ConnectivityManager$NetworkCallback");
            AndroidJavaProxy networkCallbackInstance = new NetworkCallbackProxy();
            AndroidJavaObject networkCallbackProxy = new AndroidJavaObject("android.net.ConnectivityManager$NetworkCallback", networkCallbackInstance);


            connectivityManager.Call("requestNetwork", request, networkCallbackProxy);

            //RemoveSuggestionCurrentWifi();

            return ConnectType.Success;


        }

        private AndroidJavaObject CreateWifiConfig(string ssid, string password)
        {
            AndroidJavaObject wifiConfig = new AndroidJavaObject("android.net.wifi.WifiNetworkSpecifier$Builder")
                .Call<AndroidJavaObject>("setSsid", ssid)
                .Call<AndroidJavaObject>("setWpa2Passphrase", password)
                .Call<AndroidJavaObject>("build");

            return wifiConfig;
        }
    }

    public class NetworkCallbackProxy : AndroidJavaProxy
    {
        public NetworkCallbackProxy() : base("android.net.ConnectivityManager$NetworkCallback") { }

        public void onAvailable(AndroidJavaObject network)
        {
            AndroidJavaObject[] objs = new AndroidJavaObject[] { network };
            Invoke("onAvailable", objs);

            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
            AndroidJavaObject connectivityManager = context.Call<AndroidJavaObject>("getSystemService", "connectivity");

            //connectivityManager.Call<bool>("setProcessDefaultNetwork", network);

            connectivityManager.Call<bool>("bindProcessToNetwork", network);
        }
    }
}