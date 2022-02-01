using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D9;
using SharpDX.Mathematics;
using SharpDX.XInput;
using WeScriptWrapper;
using WeScript.SDK.UI;
using WeScript.SDK.UI.Components;
using System.Diagnostics;

namespace PropNight
{
    class Program
    {
        public static IntPtr processHandle = IntPtr.Zero;
        public static IntPtr wndHnd = IntPtr.Zero;
        public static IntPtr GWorldPtr = IntPtr.Zero;
        public static IntPtr GNamesPtr = IntPtr.Zero;
        public static IntPtr FNamePool = IntPtr.Zero;
        public static IntPtr ULocalPlayerControler = IntPtr.Zero;
        public static IntPtr GameBase = IntPtr.Zero;
        public static IntPtr GameSize = IntPtr.Zero;
        public static IntPtr FindWindow = IntPtr.Zero;
        public static IntPtr ULevel = IntPtr.Zero;
        public static IntPtr AActors = IntPtr.Zero;
        public static IntPtr AActor = IntPtr.Zero;
        public static IntPtr USceneComponent = IntPtr.Zero;
        public static IntPtr actor_pawn = IntPtr.Zero;
        public static IntPtr Playerstate = IntPtr.Zero;
        public static IntPtr AKSTeamState = IntPtr.Zero;
        public static IntPtr UplayerState = IntPtr.Zero; 
        public static IntPtr UGameInstance = IntPtr.Zero;
        public static IntPtr localPlayerArray = IntPtr.Zero;
        public static IntPtr ULocalPlayer = IntPtr.Zero;
        public static IntPtr Upawn = IntPtr.Zero; 
        public static IntPtr APlayerCameraManager = IntPtr.Zero;

        public static bool gameProcessExists = false;
        public static bool isWow64Process = false;
        public static bool isGameOnTop = false;
        public static bool isOverlayOnTop = false;
        public static bool IsDowned = false;

        public static uint PROCESS_ALL_ACCESS = 0x1FFFFF;
        public static uint calcPid = 0x1FFFFF;
        public static uint Health = 0;
        public static uint EnemyID = 0;
        public static uint SurvID = 0;
        public static uint ActorCnt = 0; 
        public static uint AActorID = 0;

        public static int dist = 0;

        public static Vector2 wndMargins = new Vector2(0, 0);
        public static Vector2 wndSize = new Vector2(0, 0);     
        public static Vector2 GameCenterPos = new Vector2(0, 0);
        public static Vector2 GameCenterPos2 = new Vector2(0, 0);

        public static Vector3 FMinimalViewInfo_Location = new Vector3(0, 0, 0);
        public static Vector3 FMinimalViewInfo_Rotation = new Vector3(0, 0, 0);
        public static Vector3 tempVec = new Vector3(0, 0, 0);

        public static string survivorname = "SURVIVOR";

        public static float FMinimalViewInfo_FOV = 0;                

        
        public static Dictionary<UInt32, string> CachedID = new Dictionary<UInt32, string>();

        public static WeScript.SDK.UI.Menu RootMenu { get; private set; }
        public static WeScript.SDK.UI.Menu VisualsMenu { get; private set; }
        public static Menu AimbotMenu { get; private set; }
        class Components
        {
            public static readonly MenuKeyBind MainAssemblyToggle = new MenuKeyBind("mainassemblytoggle", "Toggle the whole assembly effect by pressing key:", VirtualKeyCode.Delete, KeybindType.Toggle, true);
            public static class VisualsComponent
            {
                public static readonly MenuBool DrawTheVisuals = new MenuBool("drawthevisuals", "Enable all of the Visuals", true);
                public static readonly MenuColor EnemyColor = new MenuColor("enecolor", "Enemy Color", new SharpDX.Color(0, 255, 0, 60));
                public static readonly MenuColor SurvColor = new MenuColor("survcolor", "Survivor Color", new SharpDX.Color(0, 255, 0, 60));
                public static readonly MenuBool DrawBox = new MenuBool("box", "DrawBox", true);
                public static readonly MenuSlider DrawBoxThic = new MenuSlider("boxthickness", "Draw Box Thickness", 0, 0, 10);
                public static readonly MenuBool DrawBoxBorder = new MenuBool("drawboxborder", "Draw Border around Box and Text?", true);                
            }
        }
        public static void InitializeMenu()
        {
            VisualsMenu = new WeScript.SDK.UI.Menu("visualsmenu", "Visuals Menu")
            {
                Components.VisualsComponent.DrawTheVisuals,
                Components.VisualsComponent.EnemyColor,
                Components.VisualsComponent.SurvColor,
                Components.VisualsComponent.DrawBox,
                Components.VisualsComponent.DrawBoxThic.SetToolTip("Setting thickness to 0 will let the assembly auto-adjust itself depending on model distance"),
                Components.VisualsComponent.DrawBoxBorder.SetToolTip("Drawing borders may take extra performance (FPS) on low-end computers"),
            };
            RootMenu = new WeScript.SDK.UI.Menu("Rogue", "WeScript.app Rogue Company --Poptart--", true)
            {
                Components.MainAssemblyToggle.SetToolTip("The magical boolean which completely disables/enables the assembly!"),
                VisualsMenu,
                AimbotMenu,
            };
            RootMenu.Attach();
        }        
        private static string GetNameFromFName(uint key)
        {
            if (GNamesPtr == IntPtr.Zero)
                return "NULL";

            var chunkOffset = (uint)((int)(key) >> 16);
            var nameOffset = (ushort)key;
            ulong namePoolChunk = Memory.ZwReadUInt64(processHandle, (IntPtr)(GNamesPtr.ToInt64() + ((chunkOffset + 2) * 8)));
            ulong entryOffset = namePoolChunk + (ulong)(2 * nameOffset);
            short nameEntry = Memory.ZwReadInt16(processHandle, (IntPtr)entryOffset);
            int nameLength = nameEntry >> 6;
            string result = Memory.ZwReadString(processHandle, (IntPtr)entryOffset + 2, false);
            return result;
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Scavengers Assembly by Poptart");
            InitializeMenu();
            if (!Memory.InitDriver(DriverName.nsiproxy))
            {
                Console.WriteLine("[ERROR] Failed to initialize driver for some reason...");
            }
            Renderer.OnRenderer += OnRenderer;
            Memory.OnTick += OnTick;
        }
        public static double dims = 0.01905f;
        private static double GetDistance3D(Vector3 myPos, Vector3 enemyPos)
        {
            Vector3 vector = new Vector3(myPos.X - enemyPos.X, myPos.Y - enemyPos.Y, myPos.Z - enemyPos.Z);
            return Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z) * dims;
        }
        private static void OnTick(int counter, EventArgs args)
        {
            
            if (processHandle == IntPtr.Zero)
            {
                wndHnd = Memory.FindWindowName("Propnight  ");
                if (wndHnd != IntPtr.Zero)
                {
                    
                    calcPid = Memory.GetPIDFromHWND(wndHnd);
                    if (calcPid > 0)
                    {
                        processHandle = Memory.ZwOpenProcess(PROCESS_ALL_ACCESS, calcPid);
                        if (processHandle != IntPtr.Zero)
                        {
                            isWow64Process = Memory.IsProcess64Bit(processHandle);
                        }
                        else
                        {
                            Console.WriteLine("failed to get handle");
                        }
                    }
                }
            }
            else //else we have a handle, lets check if we should close it, or use it
            {
                wndHnd = Memory.FindWindowName("Propnight  "); //why the devs added spaces after the name?!
                if (wndHnd != IntPtr.Zero) //window still exists, so handle should be valid? let's keep using it
                {
                    //the lines of code below execute every 33ms outside of the renderer thread, heavy code can be put here if it's not render dependant
                    gameProcessExists = true;
                    wndMargins = Renderer.GetWindowMargins(wndHnd);
                    wndSize = Renderer.GetWindowSize(wndHnd);
                    isGameOnTop = Renderer.IsGameOnTop(wndHnd);
                    isOverlayOnTop = Overlay.IsOnTop();
                    if (GameBase == IntPtr.Zero)
                    {
                        GameBase = Memory.ZwGetModule(processHandle, null, isWow64Process);
                        Console.WriteLine($"GameBase: {GameBase.ToString("X")}");
                        Console.WriteLine("Got GAMEBASE of Propnight!");
                    }
                    else
                    {
                        if (GameSize == IntPtr.Zero)
                        {
                            GameSize = Memory.ZwGetModuleSize(processHandle, null, isWow64Process);
                        }
                        else
                        {
                            if (GWorldPtr == IntPtr.Zero)
                            {
                                GWorldPtr = Memory.ZwReadPointer(processHandle, GameBase + 0x4D42F18, isWow64Process);
                            }

                            if (GNamesPtr == IntPtr.Zero)
                            {
                                GNamesPtr = GameBase + 0x4BF5140;
                            }
                        }
                    }
                }
                else
                {
                    Memory.CloseHandle(processHandle);
                    processHandle = IntPtr.Zero;
                    gameProcessExists = false;
                    GameBase = IntPtr.Zero;
                    GameSize = IntPtr.Zero;
                    GWorldPtr = IntPtr.Zero;
                    GNamesPtr = IntPtr.Zero;
                }
            }
        }        
        private static void OnRenderer(int fps, EventArgs args)
        {
            if (!gameProcessExists) return;
            if ((!isGameOnTop) && (!isOverlayOnTop)) return;
            if (!Components.MainAssemblyToggle.Enabled) return;                      
            GameCenterPos = new Vector2(wndSize.X / 2 + wndMargins.X, wndSize.Y / 2 + wndMargins.Y);
            GameCenterPos2 = new Vector2(wndSize.X / 2 + wndMargins.X, wndSize.Y / 2 + wndMargins.Y + 750.0f);

            if (GWorldPtr != IntPtr.Zero)
            {

                Functions.Ppc();
                ULevel = Memory.ZwReadPointer(processHandle, GWorldPtr + 0x30, isWow64Process);
                if (GWorldPtr != IntPtr.Zero)
                {
                    AActors = Memory.ZwReadPointer(processHandle, (IntPtr)ULevel.ToInt64() + 0x98, isWow64Process);
                    ActorCnt = Memory.ZwReadUInt32(processHandle, (IntPtr)ULevel.ToInt64() + 0xA0);

                    if ((AActors != IntPtr.Zero) && (ActorCnt > 0))
                    {
                        for (uint i = 0; i <= ActorCnt; i++)
                        {
                            AActor = Memory.ZwReadPointer(processHandle, (IntPtr)(AActors.ToInt64() + i * 8),
                                isWow64Process);
                            if (AActor != IntPtr.Zero)
                            {
                                                                    
                                USceneComponent = Memory.ZwReadPointer(processHandle,
                                    (IntPtr)AActor.ToInt64() + 0x130, isWow64Process);
                                if (USceneComponent != IntPtr.Zero)
                                {
                                    tempVec = Memory.ZwReadVector3(processHandle,
                                        (IntPtr)USceneComponent.ToInt64() + 0x11C);

                                    AActorID = Memory.ZwReadUInt32(processHandle,
                                        (IntPtr)AActor.ToInt64() + 0x18);
                                    if (!CachedID.ContainsKey(AActorID))
                                    {
                                        var retname = GetNameFromFName(AActorID);
                                        CachedID.Add(AActorID, retname);
                                    }

                                    if ((AActorID > 0))
                                    {
                                        var retname = CachedID[AActorID];
                                        retname = GetNameFromFName(AActorID);
                                        
                                        if (retname.Contains("Surv1_C") || retname.Contains("Surv2_C") || retname.Contains("Surv3_C") || retname.Contains("Surv4_C") || retname.Contains("Surv5_C")) SurvID = AActorID;
                                        if (retname.Contains("Surv1_C")) survivorname = "Issac";
                                        if (retname.Contains("Surv2_C")) survivorname = "Taiga";
                                        if (retname.Contains("Surv3_C")) survivorname = "Chris";
                                        if (retname.Contains("Surv4_C")) survivorname = "Mable";
                                        if (retname.Contains("Surv5_C")) survivorname = "Kate";

                                        if (retname.Contains("Banshee_C") || retname.Contains("Surv2_C") || retname.Contains("Granny_C") || retname.Contains("Rabbit_C") || retname.Contains("Keymaster_C")) EnemyID = AActorID;
                                        if (retname.Contains("Banshee_C")) survivorname = "Banshee";
                                        if (retname.Contains("Granny_C")) survivorname = "Granny";
                                        if (retname.Contains("Keymaster_C")) survivorname = "Impostor";
                                        if (retname.Contains("Vampire_C")) survivorname = "Vampire";
                                        if (retname.Contains("Rabbit_C")) survivorname = "Rabbit";

                                        dist = (int)(GetDistance3D(FMinimalViewInfo_Location, tempVec));                      
                                    }
                                    if (Components.VisualsComponent.DrawTheVisuals.Enabled)
                                    {
                                        dist = (int)(GetDistance3D(FMinimalViewInfo_Location, tempVec));
                                        
                                        if (AActorID == SurvID)
                                        {
                                            Vector2 vScreen_h3ad = new Vector2(0, 0);
                                            Vector2 vScreen_f33t = new Vector2(0, 0);
                                            if (Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z + 70.0f), out vScreen_h3ad, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                            {

                                                Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z - 70.0f), out vScreen_f33t, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize);
                                                if (Components.VisualsComponent.DrawBox.Enabled)
                                                {
                                                    Renderer.DrawFPSBox(vScreen_h3ad, vScreen_f33t, Components.VisualsComponent.SurvColor.Color, BoxStance.standing, Components.VisualsComponent.DrawBoxThic.Value, Components.VisualsComponent.DrawBoxBorder.Enabled);
                                                    Renderer.DrawText(survivorname + " [" + dist + "]", vScreen_f33t.X, vScreen_f33t.Y + 5, Components.VisualsComponent.SurvColor.Color, 12, TextAlignment.centered, false);
                                                }
                                            }
                                            continue;
                                        }

                                        if (AActorID == EnemyID && dist > 5)
                                        {
                                            Vector2 vScreen_h3ad = new Vector2(0, 0);
                                            Vector2 vScreen_f33t = new Vector2(0, 0);
                                            if (Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z + 70.0f), out vScreen_h3ad, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                            {

                                                Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z - 70.0f), out vScreen_f33t, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize);
                                                if (Components.VisualsComponent.DrawBox.Enabled)
                                                {
                                                    Renderer.DrawFPSBox(vScreen_h3ad, vScreen_f33t, Components.VisualsComponent.EnemyColor.Color, BoxStance.standing, Components.VisualsComponent.DrawBoxThic.Value, Components.VisualsComponent.DrawBoxBorder.Enabled);
                                                    Renderer.DrawText(survivorname + " [" + dist + "]", vScreen_f33t.X, vScreen_f33t.Y + 5, Components.VisualsComponent.EnemyColor.Color, 12, TextAlignment.centered, false);
                                                }
                                            }
                                            continue;
                                        }


                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
