using UnityEngine;
namespace WTT_CAG.Client.AMPArms;

public class HelmetSlotTuner
{
    public static readonly string[] AMPArmsTpl =
    [
        "6917bfb870f5e5d6aea8165d", // Ops-Core AMP Arms (Black)
        "6a0e13a9b1f1333de13052c8", // Ops-Core AMP Helmet Rail Mount Kit (Tan)
        "6a0e13c5113b24a5bc3052cb", // Ops-Core AMP Helmet Rail Mount Kit (Foliage Green)
        "6a0e13e4d4c66b01c53052ce" // Ops-Core AMP Helmet Rail Mount Kit (Ranger Green)
    ];

    public static readonly string[] RaclinkTpl =
    [
        "5a16b9fffcdbcb0176308b34"
    ];

    public static readonly string[] UnityAdapterTpl =
    [
        "693c59557af373b906645b54"
    ];

    public static void Register()
    {
        var order = 800;

        RegisterHelmet("6917bd1327906df8ce6fcfff", "Ground Branch",
            ampPos: new Vector3(0f, 0.05f, 0.02f),
            ampEul: new Vector3(-10f, 0f, 0f),
            armsPos: new Vector3(0f, 0.03552864f, 0.00638967f), 
            armsEul: new Vector3(-65.9155f, 0f, 0f),
            raclinkPos: new Vector3(0f, -0.01877934f, -0.004694836f), 
            raclinkEul: new Vector3(-90.67606f, 0f, 0f),
            order: order);
        
        order -= 10;

        RegisterHelmet("6998e428d9c1fe43aaf7a03d", "FAST AOR2",
            ampPos: new Vector3(0f, 0.007136149f, 0.02438967f),
            ampEul: new Vector3(-112.7887f, 0f, 0f),
            armsPos: new Vector3(0f, 0.03552864f, 0.00638967f),
            armsEul: new Vector3(-65.9155f, 0f, 0f),
            raclinkPos: new Vector3(0f, -0.01877934f, -0.004694836f), 
            raclinkEul: new Vector3(-68.70423f, -29.57746f, 0f),
            order: order);
        
        order -= 10;

        RegisterHelmet("6998e48a9d7c140756f7a049", "FAST AOR1",
            ampPos: new Vector3(0f, 0.002441313f, 0.02908451f),
            ampEul: new Vector3(-121.2394f, 0f, 0f),
            armsPos: new Vector3(0f, 0.03552864f, 0.00638967f), 
            armsEul: new Vector3(-52.39437f, 0f, 0f),
            raclinkPos: new Vector3(0f, -0.01877934f, -0.004694836f),
            raclinkEul: new Vector3(-90.67606f, 0f, 0f),
            order: order);
        
        order -= 10;

        RegisterHelmet("5e00c1ad86f774747333222c", "EXFIL Black",
            ampPos: new Vector3(0f, 0.007136149f, 0.015f),
            ampEul: new Vector3(-112.7887f, 0f, 0f),
            unityAmpsPos: Vector3.zero, 
            unityAmpsEul: Vector3.zero,
            hasUnityArms: true,
            order: order);
        
        order -= 10;

        RegisterHelmet("5e01ef6886f77445f643baa4", 
            "EXFIL Coyote",
            ampPos: new Vector3(0f, 0.04f, -0.0507277f),
            ampEul: new Vector3(-109.4084f, 0f, 0f),
            unityAmpsPos: new Vector3(0f, 0.03286385f, 0.004694836f), 
            unityAmpsEul: new Vector3(-65.9155f, 0f, 0f),
            hasUnityArms: true,
            order: order);
        
        order -= 10;

        RegisterHelmet("6998e4f8f156ad13a1f7a055", 
            "FAST Cadpat",
            ampPos: new Vector3(0f, 0.007136149f, 0.015f), 
            ampEul: new Vector3(-112.7887f, 0f, 0f),
            armsPos: new Vector3(0f, 0.03552864f, 0.00638967f), 
            armsEul: new Vector3(-65.9155f, 0f, 0f),
            raclinkPos: new Vector3(0f, -0.01877934f, -0.004694836f), 
            raclinkEul: new Vector3(-90.67606f, 0f, 0f),
            order: order);
        
        order -= 10;

        RegisterHelmet("5a154d5cfcdbcb001a3b00da", "FAST MT Black",
            ampPos: new Vector3(0f, 0.007136149f, 0.015f), 
            ampEul: new Vector3(-112.7887f, 0f, 0f),
            raclinkPos: new Vector3(0f, -0.01877934f, -0.004694836f),
            raclinkEul: new Vector3(-90.67606f, 0f, 0f),
            order: order);
        
        order -= 10;

        RegisterHelmet("5b432d215acfc4771e1c6624", "LShZ",
            ampPos: new Vector3(0f, 0.002441313f, 0.02438967f), 
            ampEul: new Vector3(-104.338f, 0f, 0f),
            armsPos: new Vector3(0f, 0.03755869f, 0.004694836f), 
            armsEul: new Vector3(-70.98592f, 0f, 0f),
            raclinkPos: new Vector3(0f, -0.01877934f, -0.0657277f), 
            raclinkEul: new Vector3(-90.67606f, 0f, 0f),
            order: order);
        
        order -= 10;

        RegisterHelmet("5ac8d6885acfc400180ae7b0", "FAST MT Tan",
            ampPos: new Vector3(0f, 0.007136149f, 0.015f), 
            ampEul: new Vector3(-112.7887f, 0f, 0f),
            raclinkPos: new Vector3(0f, -0.01877934f, -0.004694836f), 
            raclinkEul: new Vector3(-90.67606f, 0f, 0f),
            order: order);
        
        order -= 10;

        RegisterHelmet("5ea05cf85ad9772e6624305d", "TAK-KEK",
            ampPos: new Vector3(0f, 0.007136149f, 0.02438967f), 
            ampEul: new Vector3(-119.5493f, 0f, 0f),
            armsPos: new Vector3(0f, 0.03755869f, 0.009389671f), 
            armsEul: new Vector3(-62.53521f, 0f, 0f),
            raclinkPos: new Vector3(0f, -0.01877934f, -0.004694836f), 
            raclinkEul: new Vector3(-90.67606f, 0f, 0f),
            order: order);
        
        order -= 10;
    }

    internal static void RegisterHelmet(
        string templateId, 
        string label,
        Vector3 ampPos, 
        Vector3 ampEul,
        Vector3 armsPos = default, 
        Vector3 armsEul = default,
        Vector3 raclinkPos = default, 
        Vector3 raclinkEul = default,
        Vector3 raclinkArmsPos = default, 
        Vector3 raclinkArmsEul = default,
        Vector3 unityAmpsPos = default, 
        Vector3 unityAmpsEul = default,
        bool hasUnityArms = false,
        int order = 0)
    {
        var cfg = new HelmetSlotConfigSection
        {
            HelmetTpl = templateId,
            HelmetLabel = label,
            Order = order,
            HasUnityArms = hasUnityArms,
            AMPSlotPosition = ampPos,
            AMPSlotEuler = ampEul,
            AMPArmsPosition = armsPos,
            AMPArmsEuler = armsEul,
            RaclinkSlotPosition = raclinkPos,
            RaclinkSlotEuler = raclinkEul,
            RaclinkArmsPosition = raclinkArmsPos,
            RaclinkArmsEuler = raclinkArmsEul,
            UnityAmpsPosition = unityAmpsPos,
            UnityAmpsEuler = unityAmpsEul,
        };
        Plugin.RegisterHelmet(cfg);
    }
}
