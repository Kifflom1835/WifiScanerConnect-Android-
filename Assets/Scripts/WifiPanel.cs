using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WIFIConnection;

public class WifiPanel : MonoBehaviour
{
    public static Action<string> OnNetworkBtnClick; 

    [SerializeField] private Text message;
    [SerializeField] private Text currentSsidText;
    [SerializeField] private NetworkBtn networkBtn;
    [SerializeField] private Transform scrollPanel;

    List<NetworkBtn> networkBtns = new List<NetworkBtn>();

    WiFiManager manager;
    string password;
    string fiter;
    string currentSsid;
    // Start is called before the first frame update
    void Start()
    {
        manager = new WiFiManager();
        OnNetworkBtnClick += SetCurrentNetwork;
    }

    private void OnDestroy()
    {
        OnNetworkBtnClick -= SetCurrentNetwork;
    }

    public void Btn_ScanJetWifi()
    {
        SetCurrentNetwork("");
        ClearMessage();
        ScanResaultType scanResault = manager.TryGetWifiWithFilter(fiter, out List<string> ssids);

        switch (scanResault)
        {
            case ScanResaultType.Network_with_filter_not_found:
                ShowMessage("��� ����� � �������� jet");
                break;
            case ScanResaultType.WiFi_is_not_enabled:
                ShowMessage("WiFi  �� �������");
                break;
            case ScanResaultType.WiFi_manager_is_not_available:

                break;
            case ScanResaultType.Success:
                ShowNetworks(ssids);
                break;
        }
    }
    public void Btn_ConnectToWiFi()
    {
        ClearMessage();
        if(string.IsNullOrEmpty(currentSsid))
        {
            ShowMessage("WiFi ���� �� �������");
            return;
        }
        ConnectType connectType = manager.ConnectToWiFi(currentSsid, password);

        switch (connectType)
        {
            case ConnectType.Success:
                ShowMessage($"Success connect to {currentSsid}");
                break;
            case ConnectType.Failure:
                ShowMessage($"Failure connect to {currentSsid}");
                break;
        }
    }

    public void Btn_FindCurrentConnection()
    {
        manager.GetCurrentConnection(out string ssid);
        ShowMessage($"������� ����: {ssid}");
    }

    public void Btn_OpenWifiDialog()
    {
        manager.OpenWiFiSettings();
    }

    void ShowMessage(string _message)
    {
        message.text = _message;
        Debug.Log(_message);
    }
    void ShowNetworks(List<string> networks)
    {
        ClearNetworks();
        foreach(string network in networks)
        {
            NetworkBtn btn = Instantiate(networkBtn, scrollPanel);
            btn.Init(network);
            btn.Btn.onClick.AddListener(() => { OnNetworkBtnClick?.Invoke(network); });
            networkBtns.Add(btn);
        }
    }

    void ClearMessage()
    {
        message.text = "";
    }

    void ClearNetworks()
    {
        foreach(NetworkBtn btn in networkBtns)
        {
            Destroy(btn.gameObject);
        }
        networkBtns.Clear();
    }
    void SetCurrentNetwork(string ssid)
    {
        currentSsid = ssid;
        currentSsidText.text = currentSsid;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
