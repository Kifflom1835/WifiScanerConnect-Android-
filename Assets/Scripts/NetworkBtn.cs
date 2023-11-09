using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetworkBtn : MonoBehaviour
{
    [SerializeField] private Text name;

    private Button btn;
    public Button Btn =>btn;

    public void Init(string ssid)
    {
        btn = GetComponent<Button>();
        name.text = ssid;
    }
    
}
