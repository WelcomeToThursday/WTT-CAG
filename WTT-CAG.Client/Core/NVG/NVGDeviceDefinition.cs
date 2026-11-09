using EFT.InventoryLogic;
using UnityEngine;

namespace WTT_CAG.Client.Core;

public class NVGDeviceDefinition
{
    public string PrefabNameContains;
    public string ChildNvgName;
    public string LeftPodPath;
    public string RightPodPath;
    public NightVisionComponent.EMask? SinglePodMask = NightVisionComponent.EMask.Anvis;
    public Quaternion DefaultLeftOn;
    public Quaternion DefaultLeftOff;
    public Quaternion DefaultRightOn;
    public Quaternion DefaultRightOff;
}
