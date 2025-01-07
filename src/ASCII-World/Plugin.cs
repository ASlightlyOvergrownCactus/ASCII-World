using Menu.Remix.MixedUI;
using UnityEngine;
using System;
using BepInEx;
using System.Security.Permissions;
using System.Security;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ASCII_World;

[BepInEx.BepInPlugin(GUID: MOD_ID, Name: MOD_NAME, Version: VERSION)]
public class Plugin : BepInEx.BaseUnityPlugin
{
    public const string MOD_ID = "cactus.ascii";
    public const string MOD_NAME = "ASCII";
    public const string VERSION = "1.0";
    public const string AUTHORS = "ASlightlyOverGrownCactus";
    
    static bool loaded = false;
    public static Shader ASCIIShader;
    public static Shader ASCIIStencil;
    public static Shader test;
    public static Texture2D[] asciiTextures = new Texture2D[3];
    public static MaterialPropertyBlock asciiTexBlock;
    public static FSprite asciiSprite;

    public void OnEnable()
    {
        Debug.Log("ermmmmmwtf");
        try
        {
            On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
            On.RainWorld.Update += RainWorldOnUpdate;
            On.Menu.Menu.ctor += MenuOnctor;
            Logger.LogDebug("onenableweirdness");
        }
        catch (Exception e)
        {
            Logger.LogDebug(e);
            Debug.LogException(e);
            throw new Exception("Exception from ASCIIWorld: " + e);
        }
    }
    
    private void MenuOnctor(On.Menu.Menu.orig_ctor orig, Menu.Menu self, ProcessManager manager, ProcessManager.ProcessID id)
    {
        orig(self, manager, id);
        Logger.LogDebug("Haiiii");
        asciiSprite = new FSprite("pixel") { scaleX = 1366f, scaleY = 768f };
        self.container.AddChild(asciiSprite);
        asciiSprite.SetPosition(0, 0);
        asciiSprite._isVisible = true;
    }

    private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        Debug.LogWarning("loading ascii ermmm");
        if (loaded) return;
        loaded = true;

        MachineConnector.SetRegisteredOI("cactus.ascii", ASCIIOptions.Instance);
            
        var bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assets/asciiworld")); // Load asset bundle from assets folder

        asciiTexBlock = new();
            
        // Ascii textures loading
        asciiTextures[0] = bundle.LoadAsset<Texture2D>("Assets/ASCIIPROJECT/Textures/newASCII2.png");
        ASCIIOptions.asciiTextures.Add(new ListItem("KarmaASCII"));
        asciiTextures[1] = bundle.LoadAsset<Texture2D>("Assets/ASCIIPROJECT/Textures/1x0 8x8 3.png");
        ASCIIOptions.asciiTextures.Add(new ListItem("AcerolaASCII"));
        asciiTextures[2] = bundle.LoadAsset<Texture2D>("Assets/ASCIIPROJECT/Textures/hootisicons.png");
        ASCIIOptions.asciiTextures.Add(new ListItem("HootisASCII"));
            
        // Shaders
        ASCIIShader = bundle.LoadAsset<Shader>("Assets/ASCIIPROJECT/ASCII.shader"); // Loads shader from asset bundle.
        ASCIIStencil = bundle.LoadAsset<Shader>("Assets/ASCIIPROJECT/BasicStencil.shader");
        test = bundle.LoadAsset<Shader>("Assets/shaders 1.9.03/TestShader.shader");

        asciiSprite.shader = self.Shaders["Hologram"]; //FShader.CreateShader("test", test);
    }
    
    private void RainWorldOnUpdate(On.RainWorld.orig_Update orig, RainWorld self)
    {
        orig(self);
    }
}

public class ASCIIScreen : MonoBehaviour
{
    private static bool menuLoaded = false;

    public Material asciiMat;
    [Range(0, 3)] public float contrast = 1.3f;
    [Range(0, 1)] public float offset = 0.15f;
    private bool useShader = false;
    
    private void Awake()
    {
        asciiMat = new Material(Plugin.ASCIIShader);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // TODO: Add in contrast and offset setting
        asciiMat.SetTexture("_ASCIIKarmaTex", Plugin.asciiTexBlock.GetTexture("_ASCIIKarmaTex"));
        Graphics.Blit(src, dest, asciiMat);
    }

    private void Update()
    {
        if (menuLoaded)
        {
            Plugin.asciiTexBlock.SetTexture("_ASCIIKarmaTex", Plugin.asciiTextures[2]);
            Plugin.asciiTexBlock.SetFloat("_TexWidth", 32);

            contrast = ASCIIOptions.contrastF / 100f;
            offset = ASCIIOptions.offsetF / 100f;
            useShader = ASCIIOptions.asciiBoolF;
        }
        else
        {
            Plugin.asciiTexBlock.SetTexture("_ASCIIKarmaTex", Plugin.asciiTextures[2]);
            Plugin.asciiTexBlock.SetFloat("_TexWidth", 32);

            contrast = ASCIIOptions.contrast.Value / 100f;
            offset = ASCIIOptions.offset.Value / 100f;
            useShader = ASCIIOptions.asciiBool.Value;
        }
    }
    
    public Texture2D GetASCII(string ascii)
    {
        Texture2D asciiTex = ascii switch
        {
            "KarmaASCII" => Plugin.asciiTextures[0],
            _ => Plugin.asciiTextures[1],
        };
        return asciiTex;
    }

    public float GetASCIIWidth(string ascii)
    {
        float asciiWidth = ascii switch
        {
            "KarmaASCII" => 16,
            _ => 8,
        };
        return asciiWidth;
    }
}