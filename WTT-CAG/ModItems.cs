using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;

namespace WTTClothingAndGear;

public static class ModItems
{
    private static readonly MongoId Helmet6B27 = "69e73666cbadfd79bdbe98ce";
    private static readonly MongoId Helmet6B27Flora = "69e7344dc0d143efe0be98c2";
    private static readonly MongoId Helmet6B71M = "69eb87e55af80595742360ab";
    private static readonly MongoId HelmetAirframeBlack = "6a0b240373d8e8a532c4d55a";
    private static readonly MongoId HelmetAirframeTan = "6a09bd2d7ec30ec140b4a85e";
    private static readonly MongoId HelmetAm95 = "680be1c97c53613b0016d45f";
    private static readonly MongoId HelmetB826 = "68e53709d997b0cebccfc880";
    private static readonly MongoId HelmetBatlskinCobra = "69d44c0cd9330f9cffeb31f3";
    private static readonly MongoId HelmetBk3 = "689a138a035e1c4298c32357";
    private static readonly MongoId HelmetBtsh6 = "6a1cb540948a4782f605dfd0";
    private static readonly MongoId HelmetCaimanTl = "69d422d93ea0e62623eb31e0";
    private static readonly MongoId HelmetCaimanTlOlive = "69d4257f0655878dcbeb31ed";
    private static readonly MongoId HelmetF1000H = "69c866ea66512917657e7242";
    private static readonly MongoId HelmetFastSf = "697e3ac6cd228eab70411c78";
    private static readonly MongoId HelmetFastSfMulticam = "69d2fa81c19b81119bb03961";
    private static readonly MongoId HelmetFastSfTan = "697e3ffb791420d6a7411c88";
    private static readonly MongoId HelmetFastXpBlack = "69557794e5a97ebffec5cca7";
    private static readonly MongoId HelmetFastXpMulticam = "6955714f0a371ee4bed8e29e";
    private static readonly MongoId HelmetFastXpTan = "6955a5d5094d2caaafc5ccac";
    private static readonly MongoId HelmetGalletTc500 = "69d58f2731285d616cf815b5";
    private static readonly MongoId HelmetIbh = "69d59e8ce166d1833ff815cb";
    private static readonly MongoId HelmetIhps = "6952dd3e0a19ef4b6ac90335";
    private static readonly MongoId HelmetKiverRsp = "6a3596f269cf78088785c432";
    private static readonly MongoId HelmetLshzLowcut = "6a25f8ba887e6d4ba05ac1d2";
    private static readonly MongoId HelmetRifletech = "6a304d8f2e80b27b3547da3d";
    private static readonly MongoId HelmetRifletechMulticam = "6a306685334612b9d847da50";
    private static readonly MongoId HelmetTc2000 = "695cfbcebab602cd85621736";
    private static readonly MongoId HelmetTc2000Mesh = "695d06494fa4e6a2cc621749";
    private static readonly MongoId HelmetTc2000Tan = "695d01c4146725a805621742";
    private static readonly MongoId HelmetTor2Black = "69d6dc1fabe158a296906d46";
    private static readonly MongoId HelmetUntarFast = "6a71ce2a1f7262198d264238";
    private static readonly MongoId HelmetViperp2Black = "69e92bd85a9f81b04fa1e934";
    private static readonly MongoId HelmetViperp2Tan = "69e9629ebde4a54efca1e93f";
    private static readonly MongoId FacecoverCompassHalfshield = "6954a787a8530d7fbd513bd8";
    private static readonly MongoId MaskAvonFm12 = "6a090656da8c0b1a1fb424eb";
    private static readonly MongoId MaskM50 = "697df1cc71b3b5b6c5341071";
    private static readonly MongoId MaskM50Olive = "697df3bad7387ce0f2341072";
    private static readonly MongoId MaskMsaMillenium = "689cefd7c6d829d1c30a8404";
    private static readonly MongoId MaskPmg = "689b7d32e920d74a02e5a63c";
    private static readonly MongoId MaskPmk4 = "68a60e568c72b73e32842085";
    private static readonly MongoId MaskPmk4A = "68a8dac9242e2efa4a35ecc0";
    private static readonly MongoId Rig6B46 = "69537b77189ba5c1d5e64f4b";
    private static readonly MongoId RigArsA18Mm14 = "6ab6bd7c8800fe21d523911b";
    private static readonly MongoId RigAvsArsArma = "6a90fe58bb0f96bbb75866f3";
    private static readonly MongoId RigC2Fulcrum = "6ab95488e937d74e02b76447";
    private static readonly MongoId RigCgpc3Black = "6a1a35f3d10a03f857de3059";
    private static readonly MongoId RigCgpc3Small = "6a19eb984e57885c89e456e0";
    private static readonly MongoId RigCryeCpc = "6ab97214f1a1c7257ab7644d";
    private static readonly MongoId RigEffPc = "6ab976999f1f897a47b7645e";
    private static readonly MongoId RigGen4Taps = "6962818af5c4204e1844033a";
    private static readonly MongoId RigHaleyThorax = "6a6f393774fe82b686c4a83a";
    private static readonly MongoId RigJpcBlackDiv = "6a1f343a0dd59223dd9ff57d";
    private static readonly MongoId RigJpcCoyote = "69786ec0c5d74f55c1c91ceb";
    private static readonly MongoId RigJpcMcTropic = "6a1b1aafbeb04b2149da1b1a";
    private static readonly MongoId RigLv120Ge = "6959f5cefa310a108862b798";
    private static readonly MongoId RigLv120Olive = "6a7791cc2c3b30e6d1a0d6de";
    private static readonly MongoId RigLv119Tiger = "6ac039ca5884f6d1266a2b62";
    private static readonly MongoId RigMsvGenPurpose = "6abeb364c8ca1bf0608b07f3";
    private static readonly MongoId RigMsvGunner = "6abea72d4d2b98fa898b07e1";
    private static readonly MongoId RigParaclete = "6aba9b43d60b630f910ba49e";
    private static readonly MongoId RigPerun6 = "69af8c67b5dd41b53dc2a14f";
    private static readonly MongoId RigPico1M = "6a63f04f998ec5836410dcad";
    private static readonly MongoId RigPicoDsx = "6abfd772f6734e1e3b6a2b22";
    private static readonly MongoId RigPigeonFrame = "6abaabce62687ca9970ba4b5";
    private static readonly MongoId RigRampage = "6a1445f45e02d6066dff6c1c";
    private static readonly MongoId RigSlickster = "6a18c320614dfcaac696e174";
    private static readonly MongoId RigShawM81 = "6ac08023371f61ec7b6a2b82";
    private static readonly MongoId RigThorMcvs = "695751925e767177d0975afe";
    private static readonly MongoId RigThorMcvsMulticam = "69577b2faab33d9b959d4f9e";
    private static readonly MongoId RigTv110Omon = "69749476189c0d08d9a28f1d";
    private static readonly MongoId RigTv119Boss = "6a4d4f9bc90d6800f79ee73a";
    private static readonly MongoId RigTbasV5 = "6ac04ca8ef6feb35406a2b6c";
    private static readonly MongoId RigVelocityScarab = "6ac00b0abb1eb0d1db6a2b33";
    private static readonly MongoId RigWartechTv124 = "6ab6c1e9e2eab0117bd2839e";
    private static readonly MongoId RigWasDcs = "6ac01bb7a6d378ee556a2b37";
    private static readonly MongoId ArmorAc1 = "6a0b8cf5e3248a7d679569a4";
    private static readonly MongoId ArmorGoplitS = "6a544d901e790d853ee3634e";
    private static readonly MongoId ArmorRhinoDpm = "697df0ab5e5a47675734105c";
    private static readonly MongoId ArmorRhinoMtp = "697df164761aa50814341066";
    private static readonly MongoId ArmorTv119Multicam = "69ecb89b1bc77f05e3033c12";
    private static readonly MongoId ArmorTv119Olive = "6a4d4d3b4b248ca65d9ee731";

    internal static readonly List<string> ARMORS =
    [
        ArmorAc1,
        ArmorGoplitS,
        ArmorRhinoDpm,
        ArmorRhinoMtp,
        ArmorTv119Multicam,
        ArmorTv119Olive,
        Rig6B46,
        RigArsA18Mm14,
        RigAvsArsArma,
        RigC2Fulcrum,
        RigCgpc3Black,
        RigCgpc3Small,
        RigCryeCpc,
        RigEffPc,
        RigGen4Taps,
        RigHaleyThorax,
        RigJpcBlackDiv,
        RigJpcCoyote,
        RigJpcMcTropic,
        RigLv119Tiger,
        RigLv120Ge,
        RigLv120Olive,
        RigMsvGenPurpose,
        RigMsvGunner,
        RigParaclete,
        RigPerun6,
        RigPico1M,
        RigPicoDsx,
        RigPigeonFrame,
        RigRampage,
        RigSlickster,
        RigShawM81,
        RigThorMcvs,
        RigThorMcvsMulticam,
        RigTv110Omon,
        RigTv119Boss,
        RigTbasV5,
        RigVelocityScarab,
        RigWartechTv124,
        RigWasDcs
    ];

    internal static readonly List<string> HELMETS =
    [
        HelmetUntarFast,
        HelmetIhps,
        HelmetFastXpTan,
        HelmetFastXpMulticam,
        HelmetFastXpBlack,
        HelmetBtsh6,
        HelmetB826,
        HelmetAm95,
        HelmetTc2000,
        HelmetTc2000Tan,
        HelmetTc2000Mesh,
        HelmetFastSf,
        HelmetFastSfTan,
        HelmetFastSfMulticam,
        HelmetF1000H,
        HelmetCaimanTl,
        HelmetCaimanTlOlive,
        HelmetBatlskinCobra,
        HelmetGalletTc500,
        HelmetIbh,
        HelmetTor2Black,
        Helmet6B27,
        Helmet6B27Flora,
        HelmetViperp2Black,
        HelmetViperp2Tan,
        Helmet6B71M,
        HelmetAirframeTan,
        HelmetAirframeBlack,
        HelmetLshzLowcut,
        HelmetRifletech,
        HelmetRifletechMulticam,
        HelmetKiverRsp,
        HelmetBk3
    ];

    internal static readonly List<string> ARMORED_FACECOVERS =
    [
        FacecoverCompassHalfshield
    ];

    internal static readonly List<string> GAS_MASKS =
    [
        MaskPmg,
        MaskMsaMillenium,
        MaskPmk4,
        MaskPmk4A,
        MaskM50,
        MaskM50Olive,
        MaskAvonFm12
    ];

    internal static readonly List<string> UNTAR =
    [
        HelmetUntarFast,
        ItemTpl.ARMOR_MFUNTAR_BODY
    ];

    internal static List<string> With(this List<string> l, List<List<string>> lists)
    {
        var combined = new List<string>(l);

        foreach (var list in lists)
        {
            combined.AddRange(list);
        }

        return combined;
    }
}
