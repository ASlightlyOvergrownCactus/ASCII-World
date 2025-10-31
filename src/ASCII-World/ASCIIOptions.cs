using System.Collections.Generic;
using JetBrains.Annotations;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using UnityEngine;
using MenuObject = Menu.MenuObject;

namespace ASCII_World;

public class ASCIIOptions : OptionInterface
{
    public static readonly ASCIIOptions Instance = new();
    
    public static Configurable<string> ASCIITex;
    public static string ASCIITexF;
    public static Configurable<float> Offset;
    public static float offsetF;
    public static Configurable<float> Contrast;
    public static float contrastF;
    public static Configurable<float> Bloom;
    public static float bloomF;
    public static Configurable<int> KernelSize;
    public static float kernelSizeF;
    
    public static List<ListItem> asciiTextures = new();

    [CanBeNull] public static UIelement[] uIelements;

    public ASCIIOptions()
    {
        ASCIITex = config.Bind<string>("ASCIIWorld_texture", "MisakiGothic_8x8x8x8");
        Contrast = config.Bind<float>("ASCIIWorld_contrast", 100.0f, new ConfigAcceptableRange<float>(0f, 200f));
        Offset = config.Bind<float>("ASCIIWorld_offset", 0.0f, new ConfigAcceptableRange<float>(0f, 100f));
        Bloom = config.Bind<float>("ASCIIWorld_bloom", 50.0f, new ConfigAcceptableRange<float>(0f, 100f));
        KernelSize = config.Bind<int>("ASCIIWorld_kernel", 3, new ConfigAcceptableRange<int>(1, 9));
    }

    public override void Initialize()
    {
        OpTab opTab = new(this, "Options");
        Tabs = new[]
        {
            opTab
        };
        
        const int rightSidePos = 360;
        const int sliderBarLength = 135;
        const int leftSidePos = 60;
        #nullable enable

        uIelements = new UIelement[]
        {
            new OpLabel(200, 575, Translate("ASCII Shader Options"), true) {alignment=FLabelAlignment.Center},
            
            // Make the options on the left side
            new OpLabel(leftSidePos, 540, Translate("Luminosity Contrast")),
            new OpFloatSlider(Contrast, new Vector2(leftSidePos, 510), sliderBarLength),
            
            new OpLabel(leftSidePos, 480, Translate("Luminosity Offset")),
            new OpFloatSlider(Offset, new Vector2(leftSidePos, 450), sliderBarLength),
            
            new OpLabel(leftSidePos, 420, Translate("Bloom Amount")),
            new OpFloatSlider(Bloom, new Vector2(leftSidePos, 390), sliderBarLength),
            
            new OpLabel(leftSidePos, 360, Translate("Bloom Kernel Size")),
            new OpSliderTick(KernelSize, new Vector2(leftSidePos, 330), sliderBarLength),
            
            new OpLabel(leftSidePos, 300, Translate("ASCII Character Set, CharacterSize and Color Res Size in Pixels")),
            new OpComboBox(ASCIITex, new Vector2(leftSidePos, 270), 300, asciiTextures),
        };
        opTab.AddItems(uIelements);
    }

    public override void Update()
    {
        if (uIelements != null)
        {
            ASCIITexF = ((OpComboBox)uIelements[10])._GetDisplayValue();
            kernelSizeF = ((OpSliderTick)uIelements[8]).GetValueInt();
            bloomF = ((OpFloatSlider)uIelements[6]).GetValueFloat();
            offsetF = ((OpFloatSlider)uIelements[4]).GetValueFloat();
            contrastF = ((OpFloatSlider)uIelements[2]).GetValueFloat();
        }
    }
}