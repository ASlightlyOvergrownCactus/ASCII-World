using Menu.Remix.MixedUI;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using System.Security.Permissions;
using System.Security;
using MonoMod.RuntimeDetour;
using On.Menu;
using RWCustom;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ASCII_World;

[BepInEx.BepInPlugin(GUID: MOD_ID, Name: MOD_NAME, Version: VERSION)]
public class Plugin : BepInEx.BaseUnityPlugin
{
    public const string MOD_ID = "cactus.ascii";
    public const string MOD_NAME = "ASCII";
    public const string VERSION = "1.0";
    public const string AUTHORS = "ASlightlyOvergrownCactus";
    
    static bool loaded = false;
    private static bool isMenu = false;

    private static bool menuLoaded = false;
    
    public static Shader ASCIIShader;
    public static Shader BloomShader;
    public static Dictionary<string, Texture2D> asciiTextures = new Dictionary<string, Texture2D>();
    public static FSprite asciiSprite;
    public static FSprite bloomSprite;

    public void OnEnable()
    {
        try
        {
            On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
            On.RoomCamera.DrawUpdate += RoomCameraOnDrawUpdate;
            On.Menu.MenuContainer.GrafUpdate += MenuContainerOnGrafUpdate;
            On.RainWorld.Update += RainWorldOnUpdate;
            On.FScreen.ctor += FScreenOnctor;
            On.FScreen.ReinitRenderTexture += FScreenOnReinitRenderTexture;
            On.Options.OnLoadFinished += OptionsOnOnLoadFinished;
            var getScreenSize = new Hook(
                typeof(Options).GetProperty(nameof(Options.ScreenSize)).GetGetMethod(), HookScreenSizeDelegate);
        }
        catch (Exception e)
        {
            Logger.LogDebug(e);
            Debug.LogException(e);
            throw new Exception("Exception from ASCIIWorld: " + e);
        }
    }

    // This hook corrects the options menu screenSize to be closest to the default options presets
    private static Vector2 HookScreenSizeDelegate(Func<Options, Vector2> orig, Options self)
    {
        if (self.windowed)
            return orig(self);
        Vector2 resolution = new IntVector2(Screen.resolutions[Screen.resolutions.Length - 1].width,
            Screen.resolutions[Screen.resolutions.Length - 1].height).ToVector2();
        float ratioCalc = resolution.x / resolution.y;

        if (ratioCalc >= 1.775f)
            return Options.screenResolutions[1]; // 1366x768 or 16:9
        if (ratioCalc >= 1.77f)
            return Options.screenResolutions[2]; // 1360x768 or 16:9 but fucked up
        if (ratioCalc >= 1.63f)
            return Options.screenResolutions[3]; // 1280x768 or 5:3
        if (ratioCalc >= 1.5f)
            return Options.screenResolutions[4]; // 1229x768 or 16:10
        return Options.screenResolutions[0]; // 1024x768 or 4:3
    }

    // Sharpens the image to always be max screen resolution when fullscreen. This is so characters render properly.
    private void UpdateResolution(FScreen fScreen)
    {
        if (Custom.rainWorld.options.fullScreen) 
        {
            IntVector2 resolution = new IntVector2(Screen.resolutions[Screen.resolutions.Length - 1].width,
                Screen.resolutions[Screen.resolutions.Length - 1].height);
            Screen.SetResolution(resolution.x, resolution.y, true);
            fScreen.renderTexture.Release();
            fScreen.renderTexture.DiscardContents();
            fScreen.renderTexture = new RenderTexture(resolution.x * fScreen.renderScale, resolution.y * fScreen.renderScale, 0);
        }
        else
        {
            Screen.SetResolution(fScreen.pixelWidth, fScreen.pixelHeight, false);
            fScreen.renderTexture.Release();
            fScreen.renderTexture.DiscardContents();
            fScreen.renderTexture = new RenderTexture((int)Custom.rainWorld.options.ScreenSize.x * fScreen.renderScale,
                (int)Custom.rainWorld.options.ScreenSize.y * fScreen.renderScale, 0);
        }
    }
    
    private void OptionsOnOnLoadFinished(On.Options.orig_OnLoadFinished orig, Options self)
    {
        orig(self);
        UpdateResolution(Futile.screen);
    }

    private void FScreenOnReinitRenderTexture(On.FScreen.orig_ReinitRenderTexture orig, FScreen self, int displaywidth)
    {
        orig(self, displaywidth);
        if (Custom.rainWorld.options != null)
            UpdateResolution(self);
    }

    private void FScreenOnctor(On.FScreen.orig_ctor orig, FScreen self, FutileParams futileparams)
    {
        orig(self, futileparams);
        if (Custom.rainWorld.options != null)
            UpdateResolution(self);
    }

    private void RainWorldOnUpdate(On.RainWorld.orig_Update orig, RainWorld self)
    {
        orig(self);
        Futile.instance._cameraImage.texture.filterMode = FilterMode.Point; // Fix filter mode from bilinear to point
    }

    private void MenuContainerOnGrafUpdate(MenuContainer.orig_GrafUpdate orig, Menu.MenuContainer self, float timestacker)
    {
        orig(self, timestacker);

        string asciiTex = ASCIIOptions.ASCIITex.Value;
        Shader.SetGlobalTexture("_ASCIIWorldTex", asciiTextures[asciiTex]);
        Shader.SetGlobalInt("_ASCIIKernelSize", ASCIIOptions.KernelSize.Value);

        string asciiSize = asciiTex.Split('_').Last();
        var sizes = asciiSize.Split('x');
        
        int xSize = Convert.ToInt32(sizes[0]);
        int ySize = Convert.ToInt32(sizes[1]);
        int zSize = Convert.ToInt32(sizes[2]);
        int wSize = Convert.ToInt32(sizes[3]);
        
        Shader.SetGlobalVector("_ASCIIWorldSize", new Vector4(xSize, ySize, zSize, wSize));
        asciiSprite.color = new Color(ASCIIOptions.Contrast.Value / 100f, ASCIIOptions.Offset.Value / 100f, 0.0f);
        bloomSprite.color = new Color(ASCIIOptions.Bloom.Value / 100f, 0.0f, 0.0f, 1.0f);
        self.Container.AddChildAtIndex(asciiSprite, 0);
        self.Container.AddChildAtIndex(bloomSprite, 1);
    }

    private void RoomCameraOnDrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timestacker, float timespeed)
    {
        orig(self, timestacker, timespeed);

        self.ReturnFContainer("HUD").AddChildAtIndex(asciiSprite, 0);
        self.ReturnFContainer("HUD").AddChildAtIndex(bloomSprite, 1);
    }

    private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        if (loaded) return;
        loaded = true;

        MachineConnector.SetRegisteredOI("cactus.ascii", ASCIIOptions.Instance);
            
        var bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assets/asciiworld")); // Load asset bundle from assets folder
            
        // Ascii textures loading - Naming scheme is name followed by (widthOfChar)x(heightOfChar)x(widthOfColorRes)x(heightOfColorRes). 
        // This is because all characters should be squares, i.e. same width and height
        
        // Acerola characters are from https://github.com/GarrettGunnell/Post-Processing/tree/main/Assets/ASCII/LUTs
        asciiTextures["AcerolaASCII_8x8x8x8"] = bundle.LoadAsset<Texture2D>("Assets/ASCIIPROJECT/Textures/1x0 8x8 3.png");
        ASCIIOptions.asciiTextures.Add(new ListItem("AcerolaASCII_8x8x8x8"));
        
        asciiTextures["ASCIIKarma16_8x8x8x8"] = bundle.LoadAsset<Texture2D>("Assets/ASCIIPROJECT/Textures/newASCII2.png");
        ASCIIOptions.asciiTextures.Add(new ListItem("ASCIIKarma16_8x8x8x8"));
        asciiTextures["ASCIIKarma32_8x8x8x8"] = bundle.LoadAsset<Texture2D>("Assets/ASCIIPROJECT/Textures/ASCIIKarma32.png");
        ASCIIOptions.asciiTextures.Add(new ListItem("ASCIIKarma32_8x8x8x8"));
        asciiTextures["Lego_8x8x8x8"] = bundle.LoadAsset<Texture2D>("Assets/ASCIIPROJECT/Textures/Lego.png");
        ASCIIOptions.asciiTextures.Add(new ListItem("Lego_8x8x8x8"));
        asciiTextures["Lego_4x4x4x4"] = bundle.LoadAsset<Texture2D>("Assets/ASCIIPROJECT/Textures/Lego1x4.png");
        ASCIIOptions.asciiTextures.Add(new ListItem("Lego_4x4x4x4"));
        asciiTextures["Stonehead_9x9x9x9"] = bundle.LoadAsset<Texture2D>("Assets/ASCIIPROJECT/Textures/Stonehead1x9.png");
        ASCIIOptions.asciiTextures.Add(new ListItem("Stonehead_9x9x9x9"));
        asciiTextures["DensePipes_18x18x18x18"] = bundle.LoadAsset<Texture2D>("Assets/ASCIIPROJECT/Textures/DensePipes1x18.png");
        ASCIIOptions.asciiTextures.Add(new ListItem("DensePipes_18x18x18x18"));
        
        // Misaki Gothic: https://littlelimit.net/misaki.htm
        asciiTextures["MisakiGothic_8x8x8x8"] = bundle.LoadAsset<Texture2D>("Assets/ASCIIPROJECT/Textures/misaki_gothic 2 8x8.png");
        ASCIIOptions.asciiTextures.Add(new ListItem("MisakiGothic_8x8x8x8"));
        
        // Fonts used below are under Creative Commons and created by VileR, link: https://int10h.org/
        // Go check them out!
        
        asciiTextures["VileR_IBM_BIOS_8x8x8x8"] = bundle.LoadAsset<Texture2D>("Assets/ASCIIPROJECT/Textures/Ac437_IBM_BIOS.png");
        ASCIIOptions.asciiTextures.Add(new ListItem("VileR_IBM_BIOS_8x8x8x8"));
        asciiTextures["VileR_Kaypro2K_8x8x8x8"] = bundle.LoadAsset<Texture2D>("Assets/ASCIIPROJECT/Textures/Ac437_Kaypro2K_G.png");
        ASCIIOptions.asciiTextures.Add(new ListItem("VileR_Kaypro2K_8x8x8x8"));
        asciiTextures["VileR_TiPro_9x12x9x12"] = bundle.LoadAsset<Texture2D>("Assets/ASCIIPROJECT/Textures/Ac437_Ti_Pro.png");
        ASCIIOptions.asciiTextures.Add(new ListItem("VileR_TiPro_9x12x9x12"));
        asciiTextures["VileR_IBM_Model3x_8x16x8x8"] = bundle.LoadAsset<Texture2D>("Assets/ASCIIPROJECT/Textures/Ac437_IBM_Model3x_Alt1.png");
        ASCIIOptions.asciiTextures.Add(new ListItem("VileR_IBM_Model3x_8x16x8x8"));
            
        // Shader Stuff
        ASCIIShader = bundle.LoadAsset<Shader>("Assets/ASCIIPROJECT/ASCII.shader"); // Loads shaders from asset bundle.
        self.Shaders.Add("ASCIIShader", FShader.CreateShader("ASCIIShader", ASCIIShader));
        BloomShader = bundle.LoadAsset<Shader>("Assets/ASCIIPROJECT/ASCIIBloom.shader");
        self.Shaders.Add("ASCIIBloom", FShader.CreateShader("ASCIIBloom", BloomShader));
        
        Shader.SetGlobalTexture("_ASCIIWorldTex", asciiTextures["ASCIIKarma32_8x8x8x8"]);
        
        asciiSprite = new FSprite("pixel") { scaleX = 1366f, scaleY = 768f, anchorX = 0f, anchorY = 0f };
        asciiSprite.SetPosition(Vector2.zero);
        asciiSprite.shader = self.Shaders["ASCIIShader"];
        
        bloomSprite = new FSprite("pixel") { scaleX = 1366f, scaleY = 768f, anchorX = 0f, anchorY = 0f };
        bloomSprite.SetPosition(Vector2.zero);
        bloomSprite.shader = self.Shaders["ASCIIBloom"];
    }
}