namespace LamaCleaner_TorchSharp.Common
{
    public class Config
    {
        public int LdmSteps { get; set; }
        public LDMSampler LdmSampler { get; set; } = LDMSampler.plms;
        public bool ZitsWireframe { get; set; } = true;
        public HDStrategy HdStrategy { get; set; }
        public int HdStrategyCropMargin { get; set; }
        public int HdStrategyCropTriggerSize { get; set; }
        public int HdStrategyResizeLimit { get; set; }
        public string Prompt { get; set; } = "";
        public string NegativePrompt { get; set; } = "";
        public bool UseCroper { get; set; } = false;
        public int CroperX { get; set; }
        public int CroperY { get; set; }
        public int CroperHeight { get; set; }
        public int CroperWidth { get; set; }
        public float SdScale { get; set; } = 1.0f;
        public int SdMaskBlur { get; set; } = 0;
        public float SdStrength { get; set; } = 0.75f;
        public int SdSteps { get; set; } = 50;
        public float SdGuidanceScale { get; set; } = 7.5f;
        public SDSampler SdSampler { get; set; } = SDSampler.uni_pc;
        public int SdSeed { get; set; } = 42;
        public bool SdMatchHistograms { get; set; } = false;
        public string Cv2Flag { get; set; } = "INPAINT_NS";
        public int Cv2Radius { get; set; } = 4;
        public int P2pSteps { get; set; } = 50;
        public float P2pImageGuidanceScale { get; set; } = 1.5f;
        public float P2pGuidanceScale { get; set; } = 7.5f;
        public float ControlnetConditioningScale { get; set; } = 0.4f;
    }
}
