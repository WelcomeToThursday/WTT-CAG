using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using WTTServerCommonLib.Helpers;

namespace WTTClothingAndGear.Helpers
{
    [Injectable]
    public class CagQuestHelper(
        DatabaseService databaseService,
        ISptLogger<CagQuestHelper> logger,
        QuestHelper questHelper)
    {
        public void AddArmorToEquipmentInclusive(
            Dictionary<MongoId, Quest> quests,
            string questId,
            string[] armorIds
        )
        {
            if (
                armorIds == null
                || armorIds.Length == 0
                || !quests.TryGetValue(questId, out var quest)
                || quest.Conditions.AvailableForFinish == null
            )
            {
                return;
            }

            foreach (var condition in quest.Conditions.AvailableForFinish)
            {
                if (condition is not
                    {
                        ConditionType: "CounterCreator",
                        Counter.Conditions: not null
                    })
                {
                    continue;
                }

                foreach (var counterCondition in condition.Counter.Conditions)
                {
                    if (counterCondition is not
                        {
                            ConditionType: "Equipment",
                            EquipmentInclusive: not null
                        })
                    {
                        continue;
                    }

                    var newCombination = armorIds.ToList();
                    var combinations = counterCondition.EquipmentInclusive.ToList();
                    combinations.Add(newCombination);

                    counterCondition.EquipmentInclusive = combinations;
                }
            }
        }
        // Define weapon IDs
        // helmets
        private const string Helmet6B27 = "69e73666cbadfd79bdbe98ce";
        private const string Helmet6B27Flora = "69e7344dc0d143efe0be98c2";
        private const string Helmet6B71M = "69eb87e55af80595742360ab";
        private const string HelmetAirframeBlack = "6a0b240373d8e8a532c4d55a";
        private const string HelmetAirframeTan = "6a09bd2d7ec30ec140b4a85e";
        private const string HelmetAm95 = "680be1c97c53613b0016d45f";
        private const string HelmetB826 = "68e53709d997b0cebccfc880";
        private const string HelmetBatlskinCobra = "69d44c0cd9330f9cffeb31f3";
        private const string HelmetBk3 = "689a138a035e1c4298c32357";
        private const string HelmetBtsh6 = "6a1cb540948a4782f605dfd0";
        private const string HelmetCaimanTl = "69d422d93ea0e62623eb31e0";
        private const string HelmetCaimanTlOlive = "69d4257f0655878dcbeb31ed";
        private const string HelmetF1000H = "69c866ea66512917657e7242";
        private const string HelmetFastSf = "697e3ac6cd228eab70411c78";
        private const string HelmetFastSfMulticam = "69d2fa81c19b81119bb03961";
        private const string HelmetFastSfTan = "697e3ffb791420d6a7411c88";
        private const string HelmetFastXpBlack = "69557794e5a97ebffec5cca7";
        private const string HelmetFastXpMulticam = "6955714f0a371ee4bed8e29e";
        private const string HelmetFastXpTan = "6955a5d5094d2caaafc5ccac";
        private const string HelmetGalletTc500 = "69d58f2731285d616cf815b5";
        private const string HelmetIbh = "69d59e8ce166d1833ff815cb";
        private const string HelmetIhps = "6952dd3e0a19ef4b6ac90335";
        private const string HelmetKiverRsp = "6a3596f269cf78088785c432";
        private const string HelmetLshzLowcut = "6a25f8ba887e6d4ba05ac1d2";
        private const string HelmetRifletech = "6a304d8f2e80b27b3547da3d";
        private const string HelmetRifletechMulticam = "6a306685334612b9d847da50";
        private const string HelmetTc2000 = "695cfbcebab602cd85621736";
        private const string HelmetTc2000Mesh = "695d06494fa4e6a2cc621749";
        private const string HelmetTc2000Tan = "695d01c4146725a805621742";
        private const string HelmetTor2Black = "69d6dc1fabe158a296906d46";
        private const string HelmetUntarFast = "6a71ce2a1f7262198d264238";
        private const string HelmetViperp2Black = "69e92bd85a9f81b04fa1e934";
        private const string HelmetViperp2Tan = "69e9629ebde4a54efca1e93f";

        // face covers
        private const string FacecoverCompassHalfshield = "6954a787a8530d7fbd513bd8";
        private const string MaskAvonFm12 = "6a090656da8c0b1a1fb424eb";
        private const string MaskM50 = "697df1cc71b3b5b6c5341071";
        private const string MaskM50Olive = "697df3bad7387ce0f2341072";
        private const string MaskMsaMillenium = "689cefd7c6d829d1c30a8404";
        private const string MaskPmg = "689b7d32e920d74a02e5a63c";
        private const string MaskPmk4 = "68a60e568c72b73e32842085";

        private const string MaskPmk4A = "68a8dac9242e2efa4a35ecc0";

        // armored rigs
        private const string Rig6B46 = "69537b77189ba5c1d5e64f4b";
        private const string RigArsA18Mm14 = "6ab6bd7c8800fe21d523911b";
        private const string RigAvsArsArma = "6a90fe58bb0f96bbb75866f3";
        private const string RigC2Fulcrum = "6ab95488e937d74e02b76447";
        private const string RigCgpc3Black = "6a1a35f3d10a03f857de3059";
        private const string RigCgpc3Small = "6a19eb984e57885c89e456e0";
        private const string RigCryeCpc = "6ab97214f1a1c7257ab7644d";
        private const string RigEffPc = "6ab976999f1f897a47b7645e";
        private const string RigGen4Taps = "6962818af5c4204e1844033a";
        private const string RigHaleyThorax = "6a6f393774fe82b686c4a83a";
        private const string RigJpcBlackDiv = "6a1f343a0dd59223dd9ff57d";
        private const string RigJpcCoyote = "69786ec0c5d74f55c1c91ceb";
        private const string RigJpcMcTropic = "6a1b1aafbeb04b2149da1b1a";
        private const string RigLv120Ge = "6959f5cefa310a108862b798";
        private const string RigMsvGenPurpose = "6abeb364c8ca1bf0608b07f3";
        private const string RigMsvGunner = "6abea72d4d2b98fa898b07e1";
        private const string RigParaclete = "6aba9b43d60b630f910ba49e";
        private const string RigPerun6 = "69af8c67b5dd41b53dc2a14f";
        private const string RigPico1M = "6a63f04f998ec5836410dcad";
        private const string RigPicoDsx = "6abfd772f6734e1e3b6a2b22";
        private const string RigPigeonFrame = "6abaabce62687ca9970ba4b5";
        private const string RigRampage = "6a1445f45e02d6066dff6c1c";
        private const string RigSlickster = "6a18c320614dfcaac696e174";
        private const string RigThorMcvs = "695751925e767177d0975afe";
        private const string RigThorMcvsMulticam = "69577b2faab33d9b959d4f9e";
        private const string RigTv110Omon = "69749476189c0d08d9a28f1d";
        private const string RigTv119Boss = "6a4d4f9bc90d6800f79ee73a";
        private const string RigVelocityScarab = "6ac00b0abb1eb0d1db6a2b33";
        private const string RigWartechTv124 = "6ab6c1e9e2eab0117bd2839e";
        private const string RigWasDcs = "6ac01bb7a6d378ee556a2b37";

        // body armor
        private const string ArmorAc1 = "6a0b8cf5e3248a7d679569a4";
        private const string ArmorGoplitS = "6a544d901e790d853ee3634e";
        private const string ArmorRhinoDpm = "697df0ab5e5a47675734105c";
        private const string ArmorRhinoMtp = "697df164761aa50814341066";
        private const string ArmorTv119Multicam = "69ecb89b1bc77f05e3033c12";
        private const string ArmorTv119Olive = "6a4d4d3b4b248ca65d9ee731";
        // BSG base stuff
        private const string ArmorUntarStd = "5ab8e4ed86f7742d8e50c7fa";

        public void ModifyQuests()
        {
            var quests = databaseService.GetTemplates().Quests;
            // ====================== ALL NEW ITEMS ======================

            var allArmors = new[]
            {
                ArmorAc1,ArmorGoplitS,ArmorRhinoDpm,ArmorRhinoMtp,ArmorTv119Multicam,ArmorTv119Olive,Rig6B46,RigArsA18Mm14,RigAvsArsArma,RigC2Fulcrum,RigCgpc3Black,RigCgpc3Small,RigCryeCpc,RigEffPc,RigGen4Taps,RigHaleyThorax,RigJpcBlackDiv,RigJpcCoyote,RigJpcMcTropic,RigLv120Ge,RigMsvGenPurpose,RigMsvGunner,RigParaclete,RigPerun6,RigPico1M,RigPicoDsx,RigPigeonFrame,RigRampage,RigSlickster,RigThorMcvs,RigThorMcvsMulticam,RigTv110Omon,RigTv119Boss,RigVelocityScarab,RigWartechTv124, RigWasDcs
            };
            var allHelmets = new[]
            { HelmetUntarFast, HelmetIhps, HelmetFastXpTan, HelmetFastXpMulticam, HelmetFastXpBlack, HelmetBtsh6, HelmetB826, HelmetAm95, HelmetTc2000, HelmetTc2000Tan, HelmetTc2000Mesh, HelmetFastSf, HelmetFastSfTan, HelmetFastSfMulticam, HelmetF1000H, HelmetCaimanTl, HelmetCaimanTlOlive, HelmetBatlskinCobra, HelmetGalletTc500, HelmetIbh, HelmetTor2Black, Helmet6B27, Helmet6B27Flora, HelmetViperp2Black, HelmetViperp2Tan, Helmet6B71M, HelmetAirframeTan, HelmetAirframeBlack, HelmetLshzLowcut, HelmetRifletech, HelmetRifletechMulticam, HelmetKiverRsp, HelmetBk3
            };
            var allArmoredFaceCovers = new[]
            {
                FacecoverCompassHalfshield
            };
            var alLGasMasks = new[]
            {
                MaskPmg, MaskMsaMillenium, MaskPmk4, MaskPmk4A, MaskM50, MaskM50Olive, MaskAvonFm12
            };

            // ====================== JAEGER QUESTS ======================
            // Survivalist Path Unprotected but Dangerous (5d25aed386f77442734d25d2)
            questHelper.AddArmorToEquipmentExclusive(quests, "5d25aed386f77442734d25d2", allArmors);

            // Swift One (60e729cf5698ee7b05057439)
            questHelper.AddArmorToEquipmentExclusive(quests, "60e729cf5698ee7b05057439", allArmors);
            questHelper.AddArmorToEquipmentExclusive(quests, "60e729cf5698ee7b05057439", allHelmets);
            questHelper.AddArmorToEquipmentExclusive(quests, "60e729cf5698ee7b05057439", allArmoredFaceCovers);

            // ====================== PEACEKEEPER QUESTS ======================
            var allUntar = new[]
            {
                HelmetUntarFast, ArmorUntarStd 
            };
            //  Peacekeeping Mission (5c0d4c12d09282029f539173)
             AddArmorToEquipmentInclusive(quests, "5c0d4c12d09282029f539173", allUntar);
            //  Humanitarian Supplies (5a27b87686f77460de0252a8)
            AddArmorToEquipmentInclusive(quests, "5a27b87686f77460de0252a8", allUntar);

            // ====================== THERAPIST QUESTS ======================

            // Decontamination Services (5c0d1c4cd0928202a02a6f5c)
            questHelper.AddArmorToEquipmentExclusive(quests, "5c0d1c4cd0928202a02a6f5c", alLGasMasks);
        }
    }
}