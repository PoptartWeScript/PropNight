using System;
namespace PropNight
{

    public class Offsets

    {
        public static Int64 UWorld = 0x4D42F18;
        public static Int64 GNames = 0x4BF5140;

        public class UE
        {

            public class UWorld
            {
                public static Int64 ULevel = 0x30;
                public static Int64 OwningGameInstance = 0x180;
            }

            public class ULevel
            {
                public static Int64 AActors = 0x98;
                public static Int64 AActorsCount = 0xA0;
            }

            public class UGameInstance
            {
                public static Int64 LocalPlayers = 0x38;
            }

            public class UPlayer
            {
                public static Int64 PlayerController = 0x30;
            }

            public class APlayerController
            {
                public static Int64 AcknowledgedPawn = 0x2A0;
                public static Int64 PlayerCameraManager = 0x2B8;
            }

            public class APawn
            {
                public static Int64 PlayerState = 0x240;
            }
            public class AActor
            {
                public static Int64 USceneComponent = 0x130;
                public static Int64 tempVec = 0x11C;
            }

            public class APlayerCameraManager
            {
                public static Int64 CameraCachePrivate = 0x1AB0;
            }

        }
    }
}
